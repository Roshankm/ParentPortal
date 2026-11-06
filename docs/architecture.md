# Architecture

ParentPortal follows **Clean Architecture**: dependencies point inwards, and the domain has no
framework or package references at all.

```mermaid
flowchart LR
    subgraph Presentation
        Web["ParentPortal.Web<br/>Blazor (Interactive Server)"]
        Api["ParentPortal.Api<br/>Minimal APIs · SignalR · Auth"]
    end
    subgraph Core
        App["ParentPortal.Application<br/>Use-case handlers"]
        Domain["ParentPortal.Domain<br/>Entities · rules · Result"]
    end
    Infra["ParentPortal.Infrastructure<br/>EF Core · jobs · email"]
    Contracts["ParentPortal.Contracts<br/>DTOs shared by Api and Web"]
    DB[(SQLite / SQL Server)]

    Web -- HTTP + SignalR --> Api
    Web --> Contracts
    Api --> Infra
    Api --> App
    Infra --> App
    App --> Domain
    App --> Contracts
    Infra --> DB
```

## Rules

| Layer | May reference | Never references |
|---|---|---|
| Domain | nothing | EF Core, ASP.NET Core, any package |
| Application | Domain, Contracts | EF Core, HTTP, SignalR |
| Infrastructure | Application | Api, Web |
| Api | Application, Infrastructure | Web |
| Web | Contracts | Domain, Application, Infrastructure |

## Request flow: a teacher publishes an announcement

```mermaid
sequenceDiagram
    participant T as Teacher (Blazor)
    participant A as Api
    participant H as CreateAnnouncementHandler
    participant D as Announcement (domain)
    participant DB as AppDbContext
    participant S as SignalR hub
    participant P as Parent (Blazor)

    T->>A: POST /api/announcements (X-Demo-User / Bearer token)
    A->>H: HandleAsync(request)
    H->>D: Announcement.Create(...) + Publish()
    D-->>H: Result (validated)
    H->>DB: Add + SaveChanges (school_id applied)
    H->>S: AnnouncementPublished → group "school:{id}"
    S-->>P: real-time event
    A-->>T: 201 Created
```

## Multi-tenancy

* Every tenant-owned entity implements `ITenantOwned` (`SchoolId`).
* `AppDbContext` applies a **global query filter** using the current user's `school_id` claim,
  so a query can never return another school's rows - even if a handler forgets to filter.
* Handlers also check `SchoolId` explicitly (defence in depth); the integration tests prove isolation.
* SignalR connections join a per-school group, so real-time events never cross tenants.
* The reminder job runs across tenants and therefore calls `IgnoreQueryFilters()` deliberately.
