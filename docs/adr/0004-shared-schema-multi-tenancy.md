# 4. Shared-schema multi-tenancy with EF Core query filters

* Status: Accepted
* Date: 2026-11-06

## Context

Many schools use the platform. Options considered:

| Option | Isolation | Cost / ops |
|---|---|---|
| Database per school | Strongest | High - migrations × N |
| Schema per school | Strong | Medium |
| **Shared schema + `SchoolId`** | Logical | Low |

## Decision

Use a shared schema. Every tenant-owned table has `SchoolId`; `AppDbContext` applies global query filters from the caller's `school_id` claim. Unique indexes include `SchoolId`.

## Consequences

* Cheap to run and to migrate.
* Isolation depends on code, so it is enforced in three places: query filters, explicit handler checks, and integration tests that try cross-tenant access.
* A large school could later be moved to its own database by switching the connection string per tenant.
