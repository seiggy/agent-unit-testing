# Agent Evaluations Workshop

A hands-on workshop for learning how to evaluate AI agents using .NET, Microsoft.Extensions.AI, and Azure AI Foundry. This project demonstrates best practices for testing and evaluating AI agent behavior including retrieval accuracy, tool calling, task adherence, intent resolution, and prompt engineering.

## 🎯 Overview

This workshop teaches you how to build reliable AI agents by implementing structured evaluation patterns. You'll learn to:

- **Evaluate retrieval accuracy** - Ensure your agent retrieves the correct documents from a vector database
- **Validate tool calling** - Verify that agents call the right tools with correct arguments
- **Measure task adherence** - Confirm agents follow instructions and constraints
- **Assess intent resolution** - Test disambiguation of user queries
- **Iterate on prompts** - Use meta-prompt evaluation loops to improve agent behavior

## 🏗️ Architecture

The solution is built using **.NET Aspire** for distributed application orchestration:

```mermaid
flowchart TB
    subgraph AppHost["AppHost (Aspire)"]
        direction LR
        Agent["Agent Service<br/>(ASP.NET Core)"]
        Postgres[("Azure Postgres<br/>(pgvector)")]
        Foundry["Azure AI Foundry<br/>(GPT-4o)"]
        
        Agent <--> Postgres
        Agent <--> Foundry
    end
```

### Projects

| Project | Description |
|---------|-------------|
| `AgentEvalsWorkshop` | Main agent service with retrieval, tools, and agent logic |
| `AgentEvalsWorkshop.AppHost` | .NET Aspire orchestrator for local development |
| `AgentEvalsWorkshop.ServiceDefaults` | Shared service configuration and extensions |
| `AgentEvalsWorkshop.Tests` | Evaluation tests using Microsoft.Extensions.AI.Evaluation |

## 📋 Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for PostgreSQL with pgvector)
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) (for Azure resources)
- An Azure subscription with access to Azure AI Foundry (optional - supports recordings for offline use)

## 🚀 Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/seiggy/agent-unit-testing.git
cd agent-unit-testing
```

### 2. Login to Azure CLI

```bash
az login
```

### 3. Start the Aspire Project

```bash
# Start the Aspire orchestrator
dotnet run --project src/AgentEvalsWorkshop.AppHost
```

At this point, you'll be asked to select a subscription and resource group name. Find your associated subscription, and create a resource group name of your choice (or accept the default).

Stop the server. We won't need it for now.

### 4. Run the Tests

```bash
# Run all evaluation tests
dotnet test tests/AgentEvalsWorkshop.Tests
```

## 📚 Workshop Exercises

The workshop is structured into five progressive exercises:

### US0: Introduction & Environment Setup
**Goal:** Set up your development environment and configure Azure AI Foundry connectivity

- Clone and open the workshop repository
- Understand the solution structure
- Configure Azure AI Foundry credentials
- Verify the Aspire AppHost starts successfully

📄 [Full Instructions](exercises/US0-intro.md)

### US1: TaskAdherenceEvaluator
**Goal:** Learn to use the TaskAdherenceEvaluator to evaluate agent tool usage

- Configure AI evaluation reporting
- Use TaskAdherenceEvaluator to measure agent performance
- Write integration tests for AI agents
- Interpret evaluation metrics and assertions

📄 [Full Instructions](exercises/US1-taskadheranceeval.md)

### US2: Retrieval Evaluation with Built-in Evaluators
**Goal:** Use multiple built-in evaluators (Relevance, Coherence, Groundedness) together

- Use data-driven tests to evaluate multiple scenarios
- Work with GroundednessEvaluatorContext for knowledge base validation
- Interpret evaluation metrics from multiple evaluators simultaneously

📄 [Full Instructions](exercises/US2-retrievalevaluator.md)

### US3: Creating a Custom Evaluator
**Goal:** Build a custom AnswerScoringEvaluator using the LLM-as-Judge pattern

- Implement the `IEvaluator` interface
- Create custom EvaluationContext classes
- Use structured output from LLMs with `GetResponseAsync<T>()`
- Integrate custom evaluators with built-in evaluators

📄 [Full Instructions](exercises/US3-customevaluator.md)

### US4: Meta-Prompt Improvement Loop
**Goal:** Build a PromptImprovementGenerator for evaluation-driven development

- Iterate on prompt structure using AI-generated improvements
- Analyze test failures to automatically suggest improved prompts
- Track improvement trajectory across iterations
- Document prompt engineering decisions

📄 [Full Instructions](exercises/US4-meta-prompt.md)

## 🧪 Evaluation Framework

This workshop uses **Microsoft.Extensions.AI.Evaluation** for testing agent behavior:

```csharp
// Example evaluators
var relevanceEvaluator = new RelevanceEvaluator();
var coherenceEvaluator = new CoherenceEvaluator();
var wordCountEvaluator = new WordCountEvaluator();
```

### Available Evaluators

| Evaluator | Purpose |
|-----------|---------|
| `RelevanceEvaluator` | Measures response relevance to the query |
| `CoherenceEvaluator` | Assesses logical flow and clarity |
| `ToolCallAccuracyEvaluator` | Validates correct tool invocations |
| `TaskAdherenceEvaluator` | Checks compliance with task instructions |
| `IntentResolutionEvaluator` | Measures disambiguation accuracy |


## 📁 Project Structure

```
agent-unit-testing/
├── exercises/                    # Workshop exercise instructions
│   ├── US0-intro.md              # Introduction & Environment Setup
│   ├── US1-taskadheranceeval.md  # TaskAdherenceEvaluator
│   ├── US2-retrievalevaluator.md # Retrieval Evaluation with Built-in Evaluators
│   ├── US3-customevaluator.md    # Creating a Custom Evaluator
│   └── US4-meta-prompt.md        # Meta-Prompt Improvement Loop
├── infra/
│   ├── scripts/                  # Infrastructure scripts
│   └── seed/                     # Seed data for PostgreSQL
├── src/
│   ├── AgentEvalsWorkshop/       # Main agent service
│   │   ├── Agents/               # Agent implementations
│   │   ├── Retrieval/            # Vector retrieval logic
│   │   └── Tools/                # Agent tools
│   ├── AgentEvalsWorkshop.AppHost/        # Aspire orchestrator
│   └── AgentEvalsWorkshop.ServiceDefaults/ # Shared configuration
├── tests/
│   └── AgentEvalsWorkshop.Tests/ # Evaluation tests
└── TestResults/                  # Test output and reports
```

## 🔧 Configuration

### appsettings.json

The application uses standard ASP.NET Core configuration. Key settings:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📖 Resources

- [Microsoft.Extensions.AI Documentation](https://learn.microsoft.com/dotnet/ai/)
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Azure AI Foundry](https://learn.microsoft.com/azure/ai-studio/)
- [pgvector Extension](https://github.com/pgvector/pgvector)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
