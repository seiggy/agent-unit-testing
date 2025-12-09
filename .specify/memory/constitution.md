<!--
Sync Impact Report
- Version change: n/a → 1.0.0
- Modified principles: n/a (new document)
- Added sections: Core Principles, Workshop Technology Guardrails, Development Workflow & Quality Gates, Governance
- Removed sections: none
- Templates requiring updates: ✅ .specify/templates/plan-template.md (gates aligned to constitution), ⚠️ .specify/templates/agent-file-template.md (awaiting first plan-derived content), ⚠️ .specify/templates/checklist-template.md (awaiting first checklist generation), ℹ️ .specify/templates/spec-template.md and .specify/templates/tasks-template.md reviewed—no action needed
- Follow-up TODOs: none
-->

# Agent Unit Testing Workshop Constitution

## Core Principles

### Learning-First, Demo-Ready
This repository exists to teach students how to unit test Agent Framework agents using the Microsoft AI Evals SDK. Every artifact must be runnable in a workshop setting within minutes, include clear walk-throughs, and minimize prerequisites so learners can focus on the evaluation workflow. Rationale: rapid, low-friction demos keep attention on concepts rather than environment setup.

### Tests Define Agent Behavior (AI Evals SDK)
Agent behavior is specified through deterministic tests and evals before implementation. Each agent requires Microsoft AI Evals SDK suites that pin inputs, seeds, and scoring baselines; external calls are mocked or recorded; and failing tests precede any new behavior. Rationale: tests-as-contracts anchor learning and prevent silent regressions during refactors.

### Stable Contracts and Safe Isolation
Agents expose explicit contracts (typed request/response objects, schema-stable prompts) and avoid hidden state. Local runs must work without live cloud secrets by default; Azure Foundry calls should be stubbed, recorded, or gated behind opt-in configuration. Rationale: predictable contracts and isolated dependencies keep workshops reproducible and safe for students.

### Traceable Evaluations and Observability
Evaluation runs must emit structured logs (inputs, outputs, scores, seeds, SDK diagnostics) and retain sample transcripts for debugging. No PII is captured in fixtures; any real data is sanitized or synthetic. Rationale: transparent traces help students understand why tests fail and how to iterate safely.

### Stack Alignment and Minimalism
Primary stack is .NET 10 / C#, Agent Framework, Microsoft AI Evals SDK, and Azure Foundry for deployment examples. Prefer the smallest additional dependencies; document any new runtime requirement and provide offline-friendly defaults. Rationale: a focused stack keeps the workshop coherent and easier to support.

## Workshop Technology Guardrails

- Code, samples, and scripts target .NET 10 and C# with Agent Framework primitives; incompatible versions must be justified in PR descriptions.
- AI Evals SDK usage must include pinned versions and sample configs so students can reproduce locally.
- Azure Foundry usage stays optional and opt-in: provide environment templates, avoid committing secrets, and supply local mocks or recordings.
- Example data and fixtures remain synthetic; do not embed real user data or secrets in the repo.
- Generated assets (recordings, model outputs) should be small and clearly labeled to keep the repo lightweight for students.

## Development Workflow & Quality Gates

- Before implementation, define the agent contract, evaluation cases, and expected scoring in specs and plans; reviewers block changes without these.
- Every change must ship with or preserve passing AI Evals SDK tests that exercise the affected agent behaviors; failing tests require fixes or explicit waivers.
- Add structured logging around evaluation flows; PRs must explain new log fields when added.
- Documentation updates accompany feature changes: quickstart steps for students, and comments where code behavior is non-obvious.
- CI (when present) must run the evaluation suite; local scripts should mirror CI commands to keep workshop parity.

## Governance

- Supremacy: This constitution governs all development and teaching materials; conflicts resolve in favor of these principles.
- Amendment: Changes require PRs that describe the governance impact, update version and dates below, and note rationale in the Sync Impact Report comment.
- Versioning: Semantic versioning applies—MAJOR for breaking governance changes, MINOR for new principles/sections, PATCH for clarifications.
- Compliance: Reviews must check test coverage with AI Evals SDK, contract stability, observability, and stack alignment; merge is blocked until compliant.
- Review cadence: Re-confirm constitution fit at the start of each workshop iteration or quarterly, updating the Last Amended date when edits occur.

**Version**: 1.0.0 | **Ratified**: 2025-12-09 | **Last Amended**: 2025-12-09
