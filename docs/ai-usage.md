# AI Usage

## My role

I led the project from start to finish and controlled it at every step. I worked with two AI tools: Claude (chat)
for planning, explanations and review, and Claude Code for writing the code and running the checks.
I set the order and pace, sent and reviewed every prompt, reviewed every result before it was committed,
and made the key decisions.

| Tool | What it did |
|------|-------------|
| Claude (chat assistant) | Drafted the work plan and most of the step-by-step prompts, explained concepts, reviewed results with me, and recommended options. |
| Claude Code (terminal) | Wrote the code, scripts and documents, and ran the builds, tests, measurements and checks described below. |

## My working method

- One task at a time, in the order of the work plan, with a commit after each task.
- Every task had to end with evidence before I approved it: the generated SQL, test results, measurements or a real run.
- Before moving on, I made sure I understood the concept behind the task (indexes, optimistic concurrency,
  bulk partial success, cache invalidation, the RxJS search stream, automated tests).
- When a result raised a question, I stopped and asked before continuing.
- Schema changes and scope decisions were made only with my approval.

## Key decisions I made

- The 3-layer architecture (Controller, BL, DAL) with DI, and the stricter, compiler-enforced layer separation.
- A new database name, to protect an existing database.
- Fixing the `createdTo` boundary after it was pointed out in the review of the results.
- Approving the two index changes after reviewing the measurements.
- Not implementing Redis and Full-Text Search, after checking the exam requirements: no over-engineering.
- Restoring the original work plan when Claude Code offered to write one after the fact, and later asking for it
  to simply describe what was built.
- A Hebrew README for the reviewers.
- Getting an external review (ChatGPT), checking each recommendation, accepting the extra integration tests and the
  requirements mapping, and rejecting changes that were not needed.
- Making this AI usage documentation accurate.

## What I tested myself

The 409 conflict in the browser with two tabs: I saw the conflict message and the reload of the row.

## Per step

Most prompts were drafted with Claude (chat), and I sent and reviewed every one of them. The middle column describes the
work Claude Code did for each step. The last column lists my decisions and tests in that step, beyond reviewing the result before
the commit, which I did in every step.

| Step | What the AI tools did | What I decided, reviewed or tested |
|------|------------------------|------------------------------------|
| T0.1 Work plan | Claude (chat) drafted the work plan: Epics, tasks, dependencies, estimates and the mapping to the exam requirements. | – |
| T0.2 Solution structure | Claude Code created the solution and the four projects, the DI extension methods (`AddBusinessLayer` calls `AddDataAccessLayer`), `DisableTransitiveProjectReferences` in the API project, and `CLAUDE.md` from the provided content. It verified the build and that the API project can no longer compile code that uses DAL types. | Chose the 3-layer architecture (Controller, BL, DAL) with DI, and approved the stricter, compiler-enforced layer separation. |
| T1.1-T1.2 Data model, migration, schema | Claude Code wrote the entities, Fluent configurations, indexes, the `InitialCreate` migration and the idempotent `01_schema.sql`. It found that a database named `RequestsManagement` already existed with another project's data, and stopped to ask. It verified the tables and indexes in SQL Server. | Chose a new database name, to protect the existing database. |
| T1.3 Seed | Claude Code wrote `02_seed.sql` (set-based, deterministic, 100,000 rows) and made it fast (an integer hash instead of `HASHBYTES`, `TRUNCATE` instead of `DELETE`). It ran it twice and checked counts, distributions, sample rows and run time. | – |
| T2.1 Error handling | Claude Code wrote the global `IExceptionHandler`, the BL exceptions and the ProblemDetails setup. It tested every exception type with a temporary controller, then deleted it. It found that .NET 8 logged every handled exception, including 4xx, at error level, and turned that log category off. It checked that no stack trace or secret reaches the client. | – |
| T2.2-T2.4 GET endpoints | Claude Code wrote the filtering, search, sorting, paging and validation, and the GET endpoints. It verified results, validation errors, timings and the generated SQL. After the `createdTo` boundary problem (a date-only `createdTo` left out the last day) was pointed out in the review of the results, it fixed it. | Decided to fix the `createdTo` boundary. |
| T2.5 Aggregations | Claude Code wrote `GET /api/requests/stats` with one `GROUP BY`, checked that the counts add up to the total, and showed the SQL. | – |
| T3.1-T3.2 Status update and history | Claude Code wrote the transition rules, optimistic concurrency with `rowversion`, the audit row in the same transaction, and the history endpoint. It checked that the `UPDATE` has `RowVersion` in its `WHERE` clause, ran truly parallel requests (one 200 and one 409 every time, and one history row), and checked the 400/404 cases. | – |
| T3.3 Bulk update | Claude Code wrote partial success with one transaction per item, sharing the single-update logic. It found that `ChangeTracker.Clear()` after a conflict would have detached the remaining bulk items, and fixed it to detach only the failed entities. It tested a mixed bulk, the validation cases, 100 items, and a conflict caught by the database in the middle of a bulk. | – |
| T4.1 Cache | Claude Code wrote the `IMemoryCache` for the stats (60 s absolute expiration, invalidation after every successful status change) and `docs/cache.md`. It checked miss and hit, immediate updates after a PATCH and after a bulk, and that a bulk with no updates keeps the cache. | Decided not to implement Redis, after checking the exam requirements. |
| Epic 5 Tests | Claude Code wrote the test fixture (WebApplicationFactory, a dedicated LocalDB database built from the migrations, known data reset before each test), the integration tests, the unit `[Theory]`, and `docs/tests.md`. It checked that the tests never touch the development database, ran the suite several times, and broke the cache invalidation on purpose to confirm the cache test fails. | – |
| T6.1 Performance | Claude Code wrote the measurement scripts (the same `sp_executesql` as EF Core, STATISTICS IO/TIME, actual plans), saved the `.sqlplan` files, ran the drop/re-create index comparison and the index usage check, and wrote `docs/performance.md`. It found parameter sniffing between page 1 and deep pages, and index fragmentation after the seed, and proposed improvements without implementing them. | Decided not to implement Full-Text Search, after checking the exam requirements. |
| T6.2-T6.3 Index changes | Claude Code added the index rebuild at the end of the seed and the migration that replaces `IX_Requests_Priority` with `(Priority, CreatedAt)`. It re-measured, including the old index after the rebuild for a fair comparison, regenerated `01_schema.sql`, checked that it is still idempotent, and ran the tests. | Approved the two index changes after reviewing the measurements. |
| T7.1-T7.3 Angular list | Claude Code created the Angular app (Material): models, the API service, the page with one RxJS stream, and the filters/table/stats components. It added CORS for `localhost:4200` in Development, before the exception handler so error bodies are readable, and fixed a compile error. It checked the CORS headers and loaded the app in headless Chrome against the real API. | – |
| T7.4-T7.5 Status update and history in Angular | Claude Code added `allowedNextStatuses` from the existing rules, the status editor with 409 handling, the history dialog, `CurrentUserService`, and `docs/known-limitations.md`. It fixed a template binding error, found that the running dev server was serving a stale bundle, and restarted it. It drove the real UI in headless Chrome: an update, then a check of the row, the stats and the history; and a change by another user, then the 409 message and a reload of only that row. | Tested the 409 conflict myself in the browser with two tabs, and saw the conflict message and the reload of the row. |
| Angular audit | Claude Code checked the Angular code against each requirement with evidence, ran the builds and tests, and made browser-like HTTP calls with the Angular origin. No fixes were needed. | – |
| T8.1 README | Claude Code wrote the Hebrew README and moved the work plan file into `docs/`. It followed the run instructions from a fresh `git clone` in a temporary folder (both database options, seed, API, Angular, tests), and corrected the seed time. | Chose a Hebrew README for the reviewers. Restored the original work plan when Claude Code offered to write one after the fact. |
| Work plan update | Claude Code rewrote `docs/work-plan.md` to describe the work as it was done (tasks changed, added and removed, and new estimates). | Asked for the work plan to simply describe what was built. |
| Final check | Claude Code checked repository hygiene (no build output, secrets, personal data or local paths), every link and anchor, the requirements, the builds and tests, and git status. No fixes were needed. | – |
| Requirements mapping | Claude Code wrote the requirements mapping section of the README. It ran every fact in it again from a fresh clone (tests, production build, seed count, run instructions, a real PATCH), and corrected claims in its own draft that were wrong or not verified (the number of plan files, and browser checks that had not covered the Empty and Error states). | Got an external review (ChatGPT), checked each recommendation, accepted the requirements mapping, and rejected changes that were not needed. |
| Extra integration tests | Claude Code wrote tests for an invalid transition, the history endpoint and bulk request validation (101 items, duplicate ids, empty list), with shared test helpers. No production code changed. It ran the full build, the 31 tests and the production Angular build. | Accepted the extra integration tests from the external review. |
| AI usage documentation | Claude Code rewrote this file and README section 14 according to my instructions. | Made this AI usage documentation accurate. |
| Angular tests | Claude Code wrote Vitest tests for the API service (query parameters, with `HttpTestingController`), the search stream (debounce with fake timers, cancellation by `switchMap`) and the 409 handling on the page, without changing production code. It broke the search code on purpose to confirm the tests fail, restored it, and ran `npm test` (6 tests) and the production build. | Asked for these tests and set the rule that production code must not change for them. |
| Stats cache race | Claude Code added a static generation counter and lock to `RequestsService`, so a stats calculation that started before an invalidation does not store old numbers. It kept the expiration, key and logging, ran the build and the 31 tests, and updated `docs/cache.md` and the README. | Asked for the smallest possible fix of the race that `docs/cache.md` described as a remaining risk. |
| Reload after a status update | Claude Code changed the success branch of `onStatusChange` to reload the current page (the existing `reload$`) after replacing the row, so a row that no longer matches the filters leaves the list, and the sort order and `totalCount` stay correct. The 409 branch is unchanged. It added an Angular test, confirmed the test fails without the fix, and ran `npm test` (7 tests) and the production build. | Asked for this fix: after an update, the list could keep a row that no longer matches the filters. |
| List state tests | Claude Code added Angular tests for the Empty, Error (500) and Retry states of the list, without changing production code. It confirmed the Retry test fails when `retry()` does nothing, restored the code, and ran `npm test` (10 tests) and the production build. The reload after a status update was already in the previous commit, so it was not changed again. | Asked for these tests, so the list states are covered by tests and not only checked in the browser. |
