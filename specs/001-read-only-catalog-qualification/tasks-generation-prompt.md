# Copy-paste prompt for a future `$speckit-tasks` run

```text
Prepare `/SpecKit Tasks` only for active feature
`specs/001-read-only-catalog-qualification`. User-facing messages and report are in
Russian.

Before any action read `.specify/feature.json`. Stop if its target is not exactly
`specs/001-read-only-catalog-qualification`; report the actual target and do not
create `tasks.md`.

Before generation read completely: `AGENTS.md`, `.specify/memory/constitution.md`,
`.specify/project.yml`, `.specify/extensions.yml`, immutable common vision
`preparation/docs/product-specs/local-bpmsoft-synchronizer/spec.md`, active `spec.md`
(including Clarifications), approved `implementation-slices.md`,
`HANDOFF.md`, `plan.md`, `research.md`, `data-model.md`, all files
in `contracts/`, `quickstart.md`, `test-plan.md`, and factual code/tests if present.
Also read the latest Critique artifact under `critiques/`, currently
`critiques/critique-20260908-224121.md`, and retain its non-blocking recommendations.
That artifact predates the 2026-09-09 conditional manual live-verification scope
clarification. Treat it as stale: do not create `tasks.md` until Critique has been
rerun against the revised `spec.md`, `plan.md`, `test-plan.md` and slices.
Treat `preparation/docs/product-specs/` as immutable: do not alter it or read stage
source drafts other than the named common vision.

Read the complete `$speckit-tasks` `SKILL.md` before actions. Execute and wait for all
mandatory `hooks.before_tasks` from `.specify/extensions.yml`; after successful task
generation execute and wait for all mandatory `hooks.after_tasks`. Do not skip,
reinterpret or merely print mandatory hooks.

Hard stop: if `implementation-slices.md` lacks explicit human approval or conflicts
with `spec.md`, `plan.md`, or `test-plan.md`, do not create `tasks.md`. Report the
precise missing approval/conflict with paths, sections and affected slice/requirement;
do not silently resolve it.

Only after approval and consistency checks, create `tasks.md`. Every task must state
Slice ID, requirement IDs, exact planned file path/component, dependency, test or safe
evidence, and stop condition. Preserve `requirement → slice → task → check`; no task
may be outside an approved slice. Use strict `$speckit-tasks` checklist format and
user-story phases. Order: setup/test/fixture/security; implementation; per-slice
verification; final solution validation. Use automated tests where practical; any
manual check needs written steps, expected safe result and evidence location.

Retain the Critique recommendations without expanding scope: define and test
`WorkbookScaleForecast/v1` as a safe diagnostic without Excel I/O; include planned
`AuditMetadataTests.cs` and `HandoffPackageTests.cs`; add a conditional manual
read-only verification task that, only after successful offline checks, is started by
the operator manually for exact target/scope or by an agent only after a direct current
user request. It must not require `AuthorizationReference`.
Before that conditional task, include `ManualLiveInvocationTests.cs`: offline and
automatic paths must have zero terminal credential prompts and zero HTTP sends; a manual
interactive `catalog qualify` may reach its terminal credential prompt without a reference.

Never create a task that bypasses gates: `FULL_CATALOG_NOT_QUALIFIED` remains until
the conditional manual run and human review; live execution must never start
automatically, but a manual CLI invocation or direct current user request is sufficient;
`INDEX_SYNC_UNRESOLVED` remains;
Write/Manage permissions are absent; after `TARGET_STATE_CHANGED_DURING_QUALIFICATION`,
automatic repeat double pass, Pass C and retry loops are forbidden.

Do not execute `/SpecKit Analyze` or `/SpecKit Implement`; do not perform live BPMSoft,
credential prompts, write/browser/Git actions or slice-pack generation. Generated tests
may use only offline sanitized fixtures/fake transport.

Finish in Russian with `tasks.md` path, task/story/check summary, hook results and
preserved gates. State that the next separate step is `/SpecKit Analyze`; do not run it.
```
