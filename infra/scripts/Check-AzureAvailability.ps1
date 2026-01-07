[CmdletBinding()]
param(
    [string]$ModelName = "gpt-5.1-chat",
    [switch]$DebugMode
)

$ErrorActionPreference = 'Stop'

function Normalize-LocationName {
    param([string]$Name)
    return ($Name -replace "[\s-]", "").ToLower()
}

function Ensure-AzLogin {
    try {
        $account = az account show -o json | ConvertFrom-Json
        return $account
    }
    catch {
        Write-Error "Not logged in. Please run 'az login' and try again."
        exit 1
    }
}

function Get-CurrentSubscription {
    $acct = az account show -o json | ConvertFrom-Json
    return [pscustomobject]@{
        Id   = $acct.id
        Name = $acct.name
    }
}

function Get-SubscriptionLocations {
    az account list-locations -o json | ConvertFrom-Json
}

function Map-DisplayToCanonical {
    param($SubscriptionsLocations)
    $map = @{}
    foreach ($loc in $SubscriptionsLocations) {
        $map[(Normalize-LocationName $loc.displayName)] = $loc.name
        $map[(Normalize-LocationName $loc.name)] = $loc.name
    }
    return $map
}

function Get-PostgresAvailableLocations {
    param($SubLocs, $LocMap)
    $provider = az provider show --namespace Microsoft.DBforPostgreSQL -o json | ConvertFrom-Json
    if ($provider.registrationState -ne 'Registered') {
        Write-Warning "Resource provider Microsoft.DBforPostgreSQL is not registered. Run: az provider register --namespace Microsoft.DBforPostgreSQL"
    }

    $flexible = $provider.resourceTypes | Where-Object { $_.resourceType -eq 'flexibleServers' }
    $pgLocationsDisplay = $flexible.locations

    $pgLocations = foreach ($disp in $pgLocationsDisplay) {
        $norm = Normalize-LocationName $disp
        if ($LocMap.ContainsKey($norm)) { $LocMap[$norm] } else { $disp }
    }

    $subLocationNames = $SubLocs.name
    $pgAvailable = $pgLocations | Sort-Object -Unique | Where-Object { $subLocationNames -contains $_ }

    return $pgAvailable
}

function Ensure-CognitiveServicesExtension {
    try {
        az extension show --name cognitiveservices 1>$null 2>$null
    }
    catch {
        az extension add --name cognitiveservices 1>$null
    }
}

function Get-FoundryModelAvailability {
    param([string]$SubId, [string]$ModelName, [string[]]$Locs, [switch]$Debug)

    Ensure-CognitiveServicesExtension

    # Normalize model name for matching (handle variations like gpt-4o, gpt4o, GPT-4o)
    $normalizedName = $ModelName.ToLower() -replace "[^a-z0-9]", ""
    $results = @()

    if ($Debug) {
        Write-Host "[DEBUG] Searching for model: '$ModelName' (normalized: '$normalizedName')" -ForegroundColor Magenta
    }

    $locationsChecked = 0
    $sampleShown = $false

    foreach ($loc in $Locs) {
        try {
            # Use 'usage list' to get quota/capacity info per model per region
            $usageJson = az cognitiveservices usage list --location $loc --subscription $SubId -o json 2>$null
            if (-not $usageJson) { 
                if ($Debug) { Write-Host "[DEBUG] $loc - No response from usage API" -ForegroundColor DarkGray }
                continue 
            }
            $usageItems = $usageJson | ConvertFrom-Json
            $locationsChecked++

            # Filter for OpenAI GlobalStandard entries
            $openAIUsage = $usageItems | Where-Object { $_.name.value -like "OpenAI.GlobalStandard.*" }

            if ($Debug -and (-not $sampleShown) -and $openAIUsage.Count -gt 0) {
                $sampleShown = $true
                Write-Host "[DEBUG] Sample usage entry from $loc :" -ForegroundColor Magenta
                $sample = $openAIUsage | Select-Object -First 1
                Write-Host "  name.value: $($sample.name.value)" -ForegroundColor DarkYellow
                Write-Host "  name.localizedValue: $($sample.name.localizedValue)" -ForegroundColor DarkYellow
                Write-Host "  currentValue: $($sample.currentValue), limit: $($sample.limit)" -ForegroundColor DarkYellow
                Write-Host "[DEBUG] All OpenAI.GlobalStandard models in $loc :" -ForegroundColor Magenta
                $openAIUsage | ForEach-Object { 
                    $modelPart = ($_.name.value -replace "OpenAI\.GlobalStandard\.", "")
                    Write-Host "  - $modelPart (used: $($_.currentValue)/$($_.limit))" -ForegroundColor DarkYellow 
                }
            }

            # Match model name in the usage entry (e.g., OpenAI.GlobalStandard.gpt-4o)
            $matching = $openAIUsage | Where-Object {
                $entryModelName = ($_.name.value -replace "OpenAI\.GlobalStandard\.", "").ToLower() -replace "[^a-z0-9]", ""
                $entryModelName -eq $normalizedName -or 
                $_.name.value -like "*$ModelName*" -or
                $_.name.localizedValue -like "*$ModelName*"
            }

            if ($Debug -and $matching) {
                Write-Host "[DEBUG] $loc - Found $($matching.Count) match(es) for '$ModelName'" -ForegroundColor Green
            }

            foreach ($m in $matching) {
                $availableCapacity = $m.limit - $m.currentValue
                $results += [pscustomobject]@{
                    Location          = $loc
                    Model             = ($m.name.value -replace "OpenAI\.GlobalStandard\.", "")
                    DisplayName       = $m.name.localizedValue
                    SKU               = 'GlobalStandard'
                    UsedCapacity      = $m.currentValue
                    MaxCapacity       = $m.limit
                    AvailableCapacity = $availableCapacity
                }
            }
        }
        catch {
            if ($Debug) { Write-Host "[DEBUG] $loc - Exception: $_" -ForegroundColor Red }
            Write-Verbose "Skipping location $loc (no access or not supported)."
        }
    }

    if ($Debug) {
        Write-Host "[DEBUG] Checked $locationsChecked location(s), found $($results.Count) result(s)" -ForegroundColor Magenta
    }

    return $results
}

# Main
$account = Ensure-AzLogin
$subscription = Get-CurrentSubscription
Write-Host "`n== Using subscription: $($subscription.Name) ($($subscription.Id)) ==" -ForegroundColor Cyan

$subscriptionLocations = Get-SubscriptionLocations
$locationMap = Map-DisplayToCanonical -SubscriptionsLocations $subscriptionLocations
$allLocations = $subscriptionLocations.name

Write-Host "`n== Checking Azure Database for PostgreSQL (Flexible Server) availability ==" -ForegroundColor Cyan
$pgAvailable = Get-PostgresAvailableLocations -SubLocs $subscriptionLocations -LocMap $locationMap
if (-not $pgAvailable -or $pgAvailable.Count -eq 0) {
    Write-Host "No available locations found for PostgreSQL flexible servers." -ForegroundColor Yellow
    $pgAvailable = @()
} else {
    Write-Host "Found $($pgAvailable.Count) location(s) with PostgreSQL Flexible Server."
}

Write-Host "`n== Checking Azure Foundry (OpenAI) model availability: $ModelName (GlobalStandard SKU) ==" -ForegroundColor Cyan
$foundryResults = Get-FoundryModelAvailability -SubId $subscription.Id -ModelName $ModelName -Locs $allLocations -Debug:$DebugMode
$foundryLocations = $foundryResults.Location | Sort-Object -Unique
if (-not $foundryResults -or $foundryResults.Count -eq 0) {
    Write-Host "Model '$ModelName' with GlobalStandard SKU not found in any location." -ForegroundColor Yellow
    $foundryLocations = @()
} else {
    Write-Host "Found $($foundryResults.Count) location(s) with model '$ModelName' (GlobalStandard):" -ForegroundColor White
    $foundryResults | Sort-Object Location | Format-Table Location, Model, SKU, UsedCapacity, MaxCapacity, AvailableCapacity -AutoSize
}

# Find intersection - locations where ALL resources are available
$commonLocations = $pgAvailable | Where-Object { $foundryLocations -contains $_ } | Sort-Object -Unique

Write-Host "`n== Locations where ALL resources are available ==" -ForegroundColor Green
if (-not $commonLocations -or $commonLocations.Count -eq 0) {
    Write-Host "No locations found where both PostgreSQL and '$ModelName' are available." -ForegroundColor Red
} else {
    Write-Host "$($commonLocations.Count) location(s) support all required resources:" -ForegroundColor Green
    $commonLocations | ForEach-Object { Write-Host "  - $_" -ForegroundColor White }
}

# Output summary object
$summary = [pscustomobject]@{
    SubscriptionId           = $subscription.Id
    SubscriptionName         = $subscription.Name
    PostgresFlexibleRegions  = $pgAvailable
    FoundryModelDetails      = $foundryResults
    FoundryModelRegions      = $foundryLocations
    CommonRegions            = $commonLocations
}

return $summary
