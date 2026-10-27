# AI Usage

| Step | What AI generated | What I checked or changed |
|------|-------------------|---------------------------|
| T0.2 | 3-layer backend skeleton | Verified build, approved the stricter layer separation and checked the compiler blocks API access to the DAL |
| T1.1-T1.2 | Entities, configurations, indexes, migration, schema script | Reviewed tables and indexes against the spec, chose a new DB name to protect an existing database |
| T1.3 | Seed script for 100,000 rows | Checked counts, distributions, sample rows and repeatability |
| T2.1 | Global exception handler, custom BL exceptions, ProblemDetails | Reviewed the status code and body for each exception type, checked that no stack trace or secret reaches the client |
| T2.2-T2.4 | GET endpoints with filtering, search, sorting, paging and validation | Reviewed the generated SQL, checked all validation errors, found and fixed the createdTo boundary so the whole last day is included |
| T2.5 | Aggregations by status and priority | Checked that the counts add up to the total and reviewed the generated SQL |
| T3.1-T3.2 | Status update with optimistic concurrency (rowVersion), transition rules, audit history in the same transaction | Checked the UPDATE SQL has RowVersion in the WHERE clause, ran parallel updates (exactly one 200 and one 409 each time, one history row), checked 400/404 cases and the history endpoint |
| T3.3 | Bulk status update with partial success (per-item transaction and result), shared update logic with the single update | Ran a mixed bulk (Updated, NotFound, Conflict, InvalidTransition) and checked in the DB that only valid items changed with one history row each, checked the 400 cases, timed 100 items, found and fixed ChangeTracker.Clear() after a conflict (it would have detached the remaining bulk items) by detaching only the failed entities, and tested a database-detected conflict in the middle of a bulk |
| T4.1 | IMemoryCache for the stats with 60 s absolute expiration and invalidation on every successful status change, docs/cache.md | Checked miss then hit (only one GROUP BY query in the log), that stats change immediately after a single PATCH and after a bulk with updates, and that a bulk with zero successful items keeps the cache |
