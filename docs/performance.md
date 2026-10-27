# Performance

Measured on 2026-10-27 against `RequestsManagementDb` with 100,000 requests (fresh run of `database/02_seed.sql`).

## Method

- **Data:** `02_seed.sql` re-run first (100,000 requests: New 35,222, InProgress 30,009, Waiting 20,009,
  Completed 14,760; 4,953 rows contain "VPN").
- **SQL:** exactly the statements EF Core generates (copied from the EF command log), sent the same way:
  `sp_executesql` with the same parameter names and types (`tinyint` for enums, `int` for OFFSET/FETCH,
  `nvarchar(200)` for the search pattern). Queries without parameters (stats, unfiltered COUNT) are sent as plain
  text, like EF does.
- **Statistics:** `SET STATISTICS IO ON` (logical reads = 8 KB pages read from memory) and `SET STATISTICS TIME ON`.
  Each query ran once to warm up (data pages and plan in memory), then 5 times. The table shows the **median** CPU and
  elapsed time. Times are warm cache, on a developer machine (SQL Server LocalDB 17.0), so compare them with each
  other, not with production.
- **Plans:** the actual execution plan (`SET STATISTICS XML ON`) of each query is saved in
  [`docs/performance/`](performance/) as `.sqlplan` files. Open them in SSMS or Azure Data Studio.
- **Index usage:** about 30 typical API calls (every filter and sort the API supports, stats, get by id, history),
  diffing `sys.dm_db_index_usage_stats` before and after.

## Results per operation

| # | Operation | Main operators (actual rows) | Index used | Logical reads | CPU ms | Elapsed ms | Plan file |
|---|-----------|------------------------------|------------|--------------:|-------:|-----------:|-----------|
| 1a | `status=Waiting` COUNT | Index Seek (20,009) → Stream Aggregate | IX_Requests_Status_CreatedAt | 110 (50 after rebuild) | 0 | 3 | `1a_waiting_count` |
| 1b | `status=Waiting` page 1, createdAt desc | Index Seek (20) → Key Lookup (20) → Top | IX_Requests_Status_CreatedAt + PK | 64 | 0 | 0 | `1b_waiting_page` |
| 2 | Stats `GROUP BY Status, Priority` | Clustered Index Scan (100,000) → Hash Match (12) | PK_Requests | 2,355 | 31 | 39 | `2_stats_groupby` |
| 3a | `search=VPN` COUNT | Clustered Index Scan (100,000 read, 4,953 match) | PK_Requests | 2,355 | **468** | **472** | `3a_search_count` |
| 3b | `search=VPN` page 1 | Index Scan backward (285) → Key Lookup → Top | IX_Requests_CreatedAt + PK | 876 | 0 | 3 | `3b_search_page` |
| 4a | No filter COUNT | Index Scan (100,000) → Stream Aggregate | narrowest index (IX_Requests_Priority, later IX_Requests_Status_CreatedAt) | 315 / 237 | 16 | 14 | `4a_all_count` |
| 4b | Page 4000 (OFFSET 79,980), createdAt desc | Clustered Index Scan (100,000) → **Sort** (80,000) → Top | PK_Requests | 2,355 | 204 | 204 | `4b_deep_page_4000` |
| 4c | Page 1, no filter, createdAt desc | Index Scan backward (20) → Key Lookup (20) → Top | IX_Requests_CreatedAt + PK | 64 | 0 | 0 | `4c_first_page_no_filter` |

Extra queries measured as evidence for the index analysis and bottlenecks:

| Operation | Main operators | Index used | Logical reads | CPU ms | Elapsed ms | Plan file |
|-----------|----------------|------------|--------------:|-------:|-----------:|-----------|
| `priority=High` COUNT | Index Seek (24,944) | IX_Requests_Priority | 81 | 0 | 4 | `x_priority_high_count` |
| `priority=High` page 1 | Index Scan backward (75) → Key Lookup | IX_Requests_CreatedAt + PK | 233 | 0 | 0 | `x_priority_high_page` |
| Page 1 sorted by `updatedAt` | Clustered Index Scan (100,000) → Top N Sort | PK_Requests (no index on UpdatedAt) | 2,355 | 171 | 165 | `x_sort_updatedat_page` |
| Page 1 reusing the deep-page plan | Clustered Index Scan → Sort | PK_Requests | 2,355 | 171 | 164 | `4c_..._reusing_deep_page_plan` |
| Page 4000 reusing the page-1 plan | Index Scan (80,000) → **Key Lookup (80,000)** | IX_Requests_CreatedAt + PK | **245,443** | 188 | 192 | `4b_..._reusing_page1_plan` |

Notes:
- The page query for a filter that matches many rows (1b, 3b, priority page) does not need a filter index: SQL Server reads
  `IX_Requests_CreatedAt` backwards in the requested order and stops as soon as it has 20 matching rows (Top). That is
  why the search **page** costs 3 ms while the search **COUNT** costs 472 ms.
- Key Lookups are fine on a page of 20 rows (20 lookups ≈ 60 reads). They become a problem only on deep pages (see below).

## Before / after: IX_Requests_Status_CreatedAt (operation 1)

The index was dropped, the queries were measured, and it was re-created exactly as in the migration
(`CREATE INDEX [IX_Requests_Status_CreatedAt] ON [Requests] ([Status], [CreatedAt])`; checked in `sys.indexes`:
nonclustered, not unique, keys Status, CreatedAt).

| Query | Without the index | With the index |
|-------|-------------------|----------------|
| COUNT `status=Waiting` | Clustered Index Scan of all 100,000 rows; **2,355 reads**, 17 ms | Index Seek on `Status = 2`, only the 20,009 Waiting entries; **110 reads** (as seeded), **50 reads** after re-create, 2 ms |
| Page 1 `status=Waiting` | Index Scan of IX_Requests_CreatedAt backwards, 102 rows read to find 20 Waiting + 20 Key Lookups; **316 reads** | Index Seek on `Status = 2`, rows already in CreatedAt order, reads exactly 20 + 20 Key Lookups; **63 reads** |

Plans: `1a_waiting_count[_without_index].sqlplan`, `1b_waiting_page[_without_index].sqlplan`.

- The COUNT gains the most: **47x fewer reads**. The COUNT runs on every list request.
- The page gains 5x. Without the index it is still fast for Waiting (20% of rows), because the CreatedAt index finds
  20 matches quickly. For a rare status (for example 1%), it would read about 2,000 rows. The composite index always
  reads exactly 20.
- The re-created index has 237 pages instead of about 550. See "Fragmentation after the seed" below.

## Indexes

| Index | Keys | Size | Why it exists | Used in the usage check (seeks / scans / lookups) |
|-------|------|------|---------------|---------------------------------------------------|
| PK_Requests (clustered) | Id | 2,355 pages, 18.4 MB | Get by id, Key Lookups, full scans | 2 / 11 / 19 |
| IX_Requests_Status_CreatedAt | Status, CreatedAt | 237 pages, 1.9 MB | Status filter + date sort/range: the most common list query; also the narrowest index for an unfiltered COUNT | 9 / 15 / 0 |
| IX_Requests_CreatedAt | CreatedAt | 558 pages, 4.4 MB | Default sort (createdAt desc) for any filter, date ranges, early stop for search | 2 / 10 / 0 |
| IX_Requests_OrganizationName | OrganizationName | 847 pages, 6.6 MB | Organization filter (equality) | 2 / 2 / 0 |
| IX_Requests_AssignedTo | AssignedTo | 620 pages, 4.8 MB | Assignee filter ("my requests") | 1 / 0 / 0 |
| IX_Requests_Priority | Priority | 315 pages, 2.5 MB | Priority filter | 3 / 2 / 0 |
| IX_RequestStatusHistory_RequestId_ChangedAt | RequestId, ChangedAt | small | History of one request, newest first; FK | 1 / 0 / 0 |
| PK_RequestStatusHistory (clustered) | Id | small | Key | 0 / 0 / 1 |

**Every index was used** at least once. None is unused.

**IX_Requests_Priority (low selectivity, 3 values):** I looked at it specifically.
- It **is** used, but only for the **COUNT** with a priority filter: 81 reads instead of 2,355 for a clustered scan (about 16 ms → about 0 ms).
- The priority **page** query does not use it. It uses IX_Requests_CreatedAt, because about 25% of rows match, so the 20 rows are found within the first 75.
- Its cost is low: 2.5 MB, and it is never updated by status changes, because Priority does not change.
- So it has a small but real benefit. I am **not** proposing to remove it.
- A possible improvement (not done, needs your approval): replace it with `(Priority, CreatedAt)`, like the status index. Then the priority page query could also seek directly.

**Fragmentation after the seed:**
- All nonclustered indexes on Requests are **93-99% fragmented** right after `02_seed.sql`, because the seed inserts rows in Id order, so every secondary index receives keys in random order and splits pages. They are about 2x larger than needed.
- IX_Requests_Status_CreatedAt shows the effect: 110 reads before re-create, 50 after.
- A proposed fix (not done, needs your approval): add `ALTER INDEX ALL ON Requests REBUILD;` at the end of the seed script.

**LocalDB note:** the database has `AUTO_CLOSE ON` (LocalDB default). It closes when the last connection ends, which
resets `sys.dm_db_index_usage_stats` and the plan cache. The usage check therefore kept one connection open. A real
server has AUTO_CLOSE off.

## Bottlenecks and proposed improvements (not implemented)

| # | Bottleneck | Evidence | Proposed improvement |
|---|------------|----------|----------------------|
| 1 | **Text search `LIKE '%VPN%'`** cannot use an index (leading wildcard). Every search scans the whole table and compares each Title and OrganizationName, which is expensive in a Unicode SQL collation. | 3a: clustered scan, 2,355 reads, **468 ms CPU**. It is the slowest query measured and grows linearly with the table. | **Full-Text Search**: a full-text index on Title and OrganizationName, and query with `CONTAINS` (word/prefix search instead of any substring). Cheaper alternatives: a binary collation for the comparison (`COLLATE Latin1_General_100_BIN2` on upper-cased values) to cut CPU, or prefix-only search (`LIKE 'VPN%'`) that can seek. |
| 2 | **COUNT on every page request.** Each list call runs two queries; the COUNT must read every matching row, while the page query stops after 20. | Search: page 3 ms vs COUNT 472 ms. Unfiltered list: COUNT 237-315 reads vs page 64 reads. | Count only for page 1, and let the client keep `totalCount` for the next pages. Or return `hasMore` (fetch pageSize + 1 rows) instead of an exact total. Or cache the count per filter for a few seconds, like the stats. |
| 3 | **OFFSET on deep pages.** SQL Server must produce and skip all previous rows. | 4b: page 4000 sorts 80,000 rows, 2,355 reads, 204 ms. Page 1 costs 64 reads, 0 ms. | **Keyset (seek) pagination:** `WHERE (CreatedAt < @lastCreatedAt) OR (CreatedAt = @lastCreatedAt AND Id < @lastId) ORDER BY CreatedAt DESC, Id DESC` with `TOP 20`. It seeks IX_Requests_CreatedAt and costs about 64 reads on any page. The API would return a cursor instead of a page number. Also consider a maximum page number for the UI. |
| 4 | **Parameter sniffing on the OFFSET query.** Page 1 and page 4000 use the same SQL text, so they share one cached plan, compiled for whichever page ran first. | Page-1 plan reused for page 4000: **245,443 reads** (80,000 Key Lookups). Deep-page plan reused for page 1: 2,355 reads and 164 ms instead of 64 reads and 0 ms. | Keyset pagination (3) removes the problem: every page has the same cost. Until then, `OPTION (RECOMPILE)` for large offsets (via an EF interceptor or a separate query path), or `OPTIMIZE FOR (@p = 0)` so the plan stays good for the common first pages. |
| 5 | **Sorting by a column without an index** (updatedAt, title, organizationName desc with filters). | Page 1 by updatedAt: clustered scan + Top N Sort, 2,355 reads, 165 ms vs 64 reads for createdAt. | Add an index on `UpdatedAt` if "recently updated" is a common view, or limit the UI to the indexed sorts. Each extra index slows status updates (UpdatedAt changes on every update), so it should be justified by real usage. |
| 6 | **Stats GROUP BY** scans the whole table. | 2: 2,355 reads, 39 ms. | Already mitigated by the 60 s cache with invalidation (T4.1). If needed: an **indexed view** with `COUNT_BIG(*)` grouped by Status and Priority. SQL Server keeps it up to date and the query reads one page, at a small cost on every insert and status update. |
