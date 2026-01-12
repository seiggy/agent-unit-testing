namespace AgentEvalsWorkshop.Helpers
{
    public static class ConnectionStringParser
    {
        public static Uri GetEndpointFromConnectionString(string connectionString)
        {
            var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var keyValue = part.Split('=', 2);
                if (keyValue.Length == 2 && keyValue[0].Trim().Equals("Endpoint", StringComparison.OrdinalIgnoreCase))
                {
                    if (keyValue[1].Trim().EndsWith("/models"))
                        return new Uri(keyValue[1].Trim().Replace("/models", "").Replace("services.ai", "cognitiveservices"));
                    return new Uri(keyValue[1].Trim());
                }
            }
            throw new ArgumentException("Endpoint not found in connection string.");
        }
    }
}
