# Project: Requests Management System (Full Stack exam)

## Stack
- Backend: .NET 8 Web API, EF Core, SQL Server
- Frontend: Angular, standalone components, RxJS
- Tests: xUnit, WebApplicationFactory (integration)

## Structure
- /backend/RequestsManagement.sln (3-layer architecture, .NET 8)
  - /backend/RequestsApi: Web API (Controllers, Middleware). HTTP only: routing, input, status codes. References RequestsBL only.
  - /backend/RequestsBL: business logic (Services, Dtos, Validation). Services exposed via interfaces (e.g. IRequestsService). References RequestsDAL only.
  - /backend/RequestsDAL: data access (Data/DbContext, Entities, Repositories). Repositories exposed via interfaces (e.g. IRequestsRepository).
  - /backend/RequestsApi.Tests: xUnit + WebApplicationFactory integration tests. References RequestsApi.
- /frontend/requests-app
- /docs (work-plan.md, performance.md, ai-usage.md)

## Dependency rules
- The API never references the DAL directly; controllers never use the DbContext
  - Enforced by the compiler: RequestsApi.csproj sets DisableTransitiveProjectReferences=true, so RequestsDAL types are not visible in the API (the DLL is still deployed at runtime)
- Each layer depends on interfaces, not concrete classes
- DI: each layer has an extension method. Program.cs calls only AddBusinessLayer(connectionString), which calls AddDataAccessLayer(connectionString) from the DAL
- Scoped lifetime for DbContext, repositories and services
- Filtering, sorting and paging always run in the database inside the DAL; repositories never return all rows to the BL

## Key decisions
- Optimistic concurrency via SQL Server rowversion (EF [Timestamp]); conflict -> 409
- Never load all data: filtering, sorting, paging done in SQL (IQueryable)
- Return DTOs only, never entities
- Page size max 100, validate all query params -> 400 with ProblemDetails
- Global exception handling with ProblemDetails
- Every status change writes an audit row in the same transaction
- Bulk update: up to 100 ids, PARTIAL SUCCESS, per-item result (Updated/NotFound/Conflict/InvalidTransition)
- Cache: IMemoryCache for aggregations, short expiration + invalidation on every status update
- CancellationToken on all async I/O
- Status and Priority are exposed in BL DTOs as strings, because the API cannot reference DAL types. The BL validates and converts them.

## Rules
- Keep it simple, no over-engineering (no CQRS, no MediatR)
- Work on one step at a time, don't modify unrelated code
- After each step, summarize key decisions in a few bullets
