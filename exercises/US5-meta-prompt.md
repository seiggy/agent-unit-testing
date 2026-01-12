# Exercise Instructions: Meta-Prompt Improvement Loop (US3)

## Goal
Improve the baseline prompt until the meta-prompt eval reaches ≥0.80, or plateau <0.01 over 3 iterations (max 10 iterations), while recording the trajectory.

## Prereqs
- US1/US2 foundations in place
- Env: `EVAL_SEED=1234`, `USE_RECORDINGS=true`

## Steps
1) Run meta-prompt eval (expected to fail/low score): `dotnet test tests/evals/MetaPromptEvaluatorTests.cs`
2) Inspect baseline prompt in `src/Agents/Prompts/BasePrompt.cs`
3) Iterate:
   - Adjust prompt (structure, instructions, examples) to improve adherence/intent signals
   - Keep deterministic seeds; avoid adding nondeterministic content
   - Re-run eval; record score and prompt variant
4) Stop when score ≥0.80 or improvement <0.01 over 3 runs, or after 10 iterations
5) Persist trajectory in `tests/recordings/meta/` (scores + prompt snapshot per iteration)

## What Good Looks Like
- Clear prompt structure; eval scores climb to ≥0.80 or plateau documented
- Artifacts stored with iteration history for review

## Troubleshooting
- If scores regress: revert to last good prompt snapshot
- If runs differ: ensure recordings are used; confirm seed=1234 and no live calls unless intended
