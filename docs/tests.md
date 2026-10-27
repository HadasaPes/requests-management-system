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

A full run takes about 5 seconds.

## What each test covers

| Test | Type | What it proves |
|------|------|----------------|
| `GetRequests_CombinedFiltersAndPaging_ReturnsExactPageAndTotalCount` | Integration | status + priority + organization + createdFrom + sorting + page 2 return exactly ids 4 and 5, with totalCount 4. Each of the other seeded rows differs in only one field, so every filter must work. |
| `GetRequests_InvalidQueryParameter_Returns400WithFieldError` (pageSize=500, status=Done) | Integration | Invalid query parameters return 400 ProblemDetails with an error for exactly that field. |
| `UpdateStatus_TwoConcurrentUpdatesWithSameRowVersion_OneSucceedsAndOneConflicts` | Integration | Two PATCHes sent at the same time with the same rowVersion: exactly one 200 and one 409, and exactly one history row. |
| `UpdateStatus_UnknownId_Returns404` | Integration | PATCH for an id that does not exist returns 404 ProblemDetails. |
| `BulkUpdateStatus_MixedItems_ReturnsOutcomePerItemAndChangesOnlyValidItems` | Integration | A bulk with valid, not found, stale rowVersion and invalid transition items returns the right outcome per item, in order. In the database, only the valid items changed, each with exactly one history row. |
| `GetStats_AfterSuccessfulStatusUpdate_ReturnsUpdatedCountsImmediately` | Integration | The stats are cached, and a successful PATCH clears the cache: the next call already shows the new counts. |
| `GetError_Transition_ReturnsNullOnlyWhenAllowed` (14 cases) | Unit | The status workflow: the 4 allowed transitions, and forbidden ones (skipping steps, going back, leaving Completed, same status). |

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
