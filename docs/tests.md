# Tests

## How to run the tests

Requirements: .NET 8 SDK and SQL Server LocalDB (`(localdb)\MSSQLLocalDB`).

```
cd backend
dotnet test
```

The tests create their own database, `RequestsDb_Tests`. The development database (`RequestsManagementDb`)
is never used.

- At the start of the run, `RequestsDb_Tests` is dropped and recreated from the EF migrations, so it always has
  the current schema.
- Before **every** integration test, all rows are deleted and a small, known data set of 12 requests with fixed ids
  is inserted (`Infrastructure/TestData.cs`), and the stats cache is emptied. Each test starts from the same state,
  so the tests do not depend on each other or on their order, and the expected results are exact.
- The fixture refuses to run if the configured database is not `RequestsDb_Tests`.
- The API runs in memory with `WebApplicationFactory<Program>`. The full pipeline runs: routing, validation,
  exception handler, BL, EF Core and SQL Server.
- All integration tests share one fixture and run one after another, because they share one database.
  The unit tests can run in parallel with them.

A full run takes about 10 seconds (most of it is starting the test host and recreating the database).

## What each test covers

| Test | Type | What it proves |
|------|------|----------------|
| `GetRequests_CombinedFiltersAndPaging_ReturnsExactPageAndTotalCount` | Integration | status + priority + organization + createdFrom + sorting + page 2 return exactly ids 4 and 5, with totalCount 4. Each of the other seeded rows differs in only one field, so every filter must work. |
| `GetRequests_InvalidQueryParameter_Returns400WithFieldError` (pageSize=500, status=Done) | Integration | Invalid query parameters return 400 ProblemDetails with an error for exactly that field. |
| `UpdateStatus_TwoConcurrentUpdatesWithSameRowVersion_OneSucceedsAndOneConflicts` | Integration | Two PATCHes sent at the same time with the same rowVersion: exactly one 200 and one 409, and exactly one history row. |
| `UpdateStatus_UnknownId_Returns404` | Integration | PATCH for an id that does not exist returns 404 ProblemDetails. |
| `UpdateStatus_InvalidTransition_Returns400AndChangesNothing` | Integration | Completed → New with the current rowVersion returns 400 with a `status` error. In the database the status, RowVersion and UpdatedAt are unchanged, and no history row was added. |
| `GetHistory_AfterSuccessfulUpdate_ReturnsEntryWithAllFields` | Integration | After a successful PATCH, the history has exactly one entry with the right previousStatus, newStatus and changedBy, and a UTC changedAt within the time of the update. |
| `GetHistory_UnknownId_Returns404` | Integration | History of an id that does not exist returns 404 ProblemDetails. |
| `BulkUpdateStatus_MixedItems_ReturnsOutcomePerItemAndChangesOnlyValidItems` | Integration | A bulk with valid, not found, stale rowVersion and invalid transition items returns the right outcome per item, in order. In the database, only the valid items changed, each with exactly one history row. |
| `BulkUpdateStatus_InvalidRequest_Returns400AndChangesNothing` (101 items, duplicate ids, empty list) | Integration | An invalid bulk request returns 400 with an `items` error and is not processed at all: the items carry real, current rowVersions, yet no request changed (status, RowVersion, UpdatedAt) and no history row was added. |
| `GetStats_AfterSuccessfulStatusUpdate_ReturnsUpdatedCountsImmediately` | Integration | The stats are cached, and a successful PATCH clears the cache: the next call already shows the new counts. |
| `GetError_Transition_ReturnsNullOnlyWhenAllowed` (14 cases) | Unit | The status workflow: the 4 allowed transitions, and forbidden ones (skipping steps, going back, leaving Completed, same status). |
| `GetAllowedNext_Status_ReturnsExactlyTheAllowedTransitions` (4 cases) | Unit | `allowedNextStatuses` comes from the same rules: exactly the allowed next statuses for each status, empty for Completed. |

In total: 31 test cases (13 integration, 18 unit), all passing.

## Angular tests

```
cd frontend/requests-app
npm test -- --watch=false
```

Vitest, through the Angular CLI test runner (`ng test`). No server is needed: HTTP calls are answered by
`HttpTestingController`, and the debounce time is controlled with Vitest fake timers. No production code was changed
for these tests.

| Test | File | What it proves |
|------|------|----------------|
| `getRequests sends the query as URL parameters and leaves out empty values` | `core/services/requests-api.service.spec.ts` | Every non-empty query field is sent as a URL parameter with the right value; `null` and `''` values (including an empty search) are not sent at all. |
| `sends only one GET for several quick keystrokes, after the debounce time` | `features/requests/requests-page/requests-page.spec.ts` | Three keystrokes typed into the real search input within 200 ms: no GET at 299 ms after the last one, exactly one GET at 300 ms, with the last text and page 1. |
| `cancels a pending GET when the search changes and shows only the newest result` | same | While the GET for the first search is pending, a new search starts a new GET and the first one is cancelled (`switchMap`). The table shows only the newest result. |
| `replaces the row at once, then reloads the current page with the same query and reloads the stats` | same | After a successful PATCH, the row shows the returned version at once. Then a new list GET is sent with exactly the same query parameters as before, and the stats are reloaded. When it returns, the table shows the server's page. Without the reload, this test fails. |
| `loads only the changed row, replaces it in the table and shows the conflict message` | same | The PATCH (with the row's rowVersion and the user name) returns 409. The page sends `GET /api/requests/{id}`, replaces only that row with the returned version (the other row is the same object), does not reload the list or the stats, and shows the conflict message. |
| `should create the app`, `should render the app name in the toolbar` | `app.spec.ts` | The existing tests: the root component is created and the toolbar shows the app name. |

`HttpTestingController.verify()` runs after each test, so any unexpected extra request (for example a list reload
after the 409) fails the test. The search tests were also run against deliberately broken code (`mergeMap` instead
of `switchMap`, and no debounce): both failed, then the code was restored.

In total: 7 Angular tests in 3 files, all passing.

## Why a real database instead of the EF Core InMemory provider

- **Concurrency:** InMemory does not support SQL Server `rowversion`. The value is never generated, and the
  `WHERE RowVersion = @old` check is not enforced like in SQL Server, so the concurrency and stale rowVersion tests
  would pass or fail for the wrong reasons.
- **Transactions:** InMemory ignores transactions, so "the update and the history row are saved together" could not
  be tested.
- **Real SQL:** filtering, sorting, paging, `GROUP BY` and the `IN (OPENJSON ...)` query must be translated to SQL.
  InMemory runs LINQ in memory and can hide translation errors or behave differently (for example string comparison
  and case sensitivity).

LocalDB is installed with Visual Studio and is fast enough: the database is created once per run, and the reset
before each test only touches 12 rows.
