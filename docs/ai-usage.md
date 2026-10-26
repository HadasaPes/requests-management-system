# AI Usage

| Step | What AI generated | What I checked or changed |
|------|-------------------|---------------------------|
| T0.2 | 3-layer backend skeleton | Verified build, approved the stricter layer separation and checked the compiler blocks API access to the DAL |
| T1.1-T1.2 | Entities, configurations, indexes, migration, schema script | Reviewed tables and indexes against the spec, chose a new DB name to protect an existing database |
| T1.3 | Seed script for 100,000 rows | Checked counts, distributions, sample rows and repeatability |
| T2.2-T2.4 | GET endpoints with filtering, search, sorting, paging and validation | Reviewed the generated SQL, checked all validation errors, found and fixed the createdTo boundary so the whole last day is included |
