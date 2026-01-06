# Exercise Instructions: Task Adherence & Intent Resolution (US2)

## Goal
Reach ≥0.90 on TaskAdheranceEvaluator and IntentResolutionEvaluator for the defined task brief using the provided agent and fixtures.

## Prereqs
- Foundation complete (US1 passing)
- Env: `EVAL_SEED=1234`, `USE_RECORDINGS=true` (defaults)
- Task brief and ambiguous inputs from fixtures

## Steps
1) Run adherence eval (expected to fail first): `dotnet test tests/evals/TaskAdherenceEvaluatorTests.cs`
2) Run intent eval (expected to fail first): `dotnet test tests/evals/IntentResolutionEvaluatorTests.cs`
3) Improve adherence:
   - Align agent response format to task brief
   - Cover required constraints; add rationale/steps in prompt or orchestration
4) Improve intent resolution:
   - Map ambiguous inputs to expected intent labels from fixtures
   - Add disambiguation prompts/rules; ensure deterministic selection
5) Re-run both evals until scores ≥0.90; keep `USE_RECORDINGS=true` to avoid drift

## What Good Looks Like
- Adherence eval passes with diagnostics clean; responses meet brief
- Intent eval passes with correct intents for all ambiguous cases

## Troubleshooting
- If scores hover below 0.90: review diagnostics for missing criteria or misclassified intents
- If outputs change between runs: confirm seed and recordings; avoid live calls unless explicitly enabled
