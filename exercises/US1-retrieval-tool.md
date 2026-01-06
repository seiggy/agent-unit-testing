# Exercise Instructions: Retrieval & Tool Accuracy (US1)

## Goal
Make the provided agent pass RetrievalEvaluator and ToolCallAccuracyEvaluator using seeded Postgres data and the `support_tool` stub.

## Prereqs
- Docker running; `infra/docker-compose.yml` up (Postgres seeded, pgvector enabled)
- Env: `EVAL_SEED=1234`, `RETRIEVAL_TOPK=3`, `USE_RECORDINGS=true` (defaults)
- Optional Azure Foundry (disables recordings): `AZURE_FOUNDRY_ENDPOINT`, `AZURE_FOUNDRY_API_KEY`

## Steps
1) Start infra: `docker compose -f infra/docker-compose.yml up -d`
2) (Optional) Run Aspire: `dotnet run --project infra/aspire` to coordinate agent+db
3) Inspect corpus: connect to Postgres and check `documents` (id, title, body, tags, embedding vector(1536))
4) Run retrieval eval (expected to fail first): `dotnet test tests/evals/RetrievalEvaluatorTests.cs`
5) Run tool accuracy eval (expected to fail first): `dotnet test tests/evals/ToolCallAccuracyEvaluatorTests.cs`
6) Fix retrieval:
   - Ensure retriever uses seeded embeddings and `top_k=3`
   - Verify returned doc IDs match fixtures; adjust scoring tolerance only if aligned with plan (≥0.80 ±0.05)
7) Fix tool calls:
   - Ensure tool name `support_tool`, args `{ action, target }`, exactly one call
   - Validate arg order and values against oracle fixtures
8) Re-run both evals until passing; commit changes to agent/retriever/tool only after tests are green

## What Good Looks Like
- Retrieval eval reports expected doc IDs with scores above threshold, deterministic across reruns
- Tool accuracy eval shows single call with exact name/args, no extra calls

## Troubleshooting
- If scores drift: confirm `USE_RECORDINGS=true`, seed=1234, and no live Azure calls
- If Postgres empty: rerun seed script in `infra/seed/`
- If embeddings mismatch: regenerate with `text-embedding-3-small` and reseed
