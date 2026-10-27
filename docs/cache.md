# Cache

## What is cached and why

Only the dashboard aggregations from `GET /api/requests/stats` (the count of requests per status and per priority).

- It is the most expensive read: a `GROUP BY` over the whole `Requests` table (100,000 rows), while every
  other read returns one page through an index.
- It is global (no filters, no paging), so one cache entry (one key) serves every user.
- The dashboard is opened often, while statuses change much less often than they are read.
- Being a few seconds old is acceptable for a dashboard, unlike a single request that a user is about to edit.

Lists, single requests and history are **not** cached: they depend on filters, page and id (many keys), and
they must be exact, because the client needs the current `rowVersion` to update a request.

## Expiration and invalidation

- `IMemoryCache` (in-process memory), registered in `AddBusinessLayer`. The key is one constant in `RequestsService`.
- **Absolute expiration of 60 seconds:** the entry is dropped 60 s after it was stored, even if it is read all
  the time, so the numbers are never more than 60 s old.
- **Invalidation:** after every successful status change, the entry is removed. Both the single PATCH and the bulk
  update go through the same code path, so a bulk invalidates only if at least one item was updated.
  A failed update (conflict, invalid transition, not found) does not invalidate. The next stats call
  recalculates from the database.
- Cache hit and miss are logged at Debug level.

**Remaining risk (small):** a stats calculation can start *before* an update, and finish *after* the update has
removed the entry. It then stores numbers that do not include that update. They stay in the cache until the next
status change or until the 60 s expiration, whichever comes first. The window is a few milliseconds (the time of one
`GROUP BY`), the result is only off by the changes made in that window, and the 60 s expiration limits how long it lasts.

## Several server instances

`IMemoryCache` lives in the memory of one process. With several instances behind a load balancer, each instance
has its own copy, and an invalidation on one instance does not clear the others. A user could see the new numbers
on one request and the old numbers on the next one, for up to 60 seconds.

Options:

1. **Shared cache:** Redis (or SQL Server) through `IDistributedCache`. All instances read and remove the same entry,
   so one invalidation is seen everywhere. Costs a network call per read and an extra service to run.
2. **Notify all instances:** keep `IMemoryCache`, and publish an "invalidate stats" message (for example
   Redis pub/sub) that every instance listens to and removes its local entry.
3. **Accept short expiration only:** drop the invalidation and rely on a short expiration (for example 10-60 s).
   Simplest, and often enough for a dashboard, but numbers can be stale for the whole expiration time.
