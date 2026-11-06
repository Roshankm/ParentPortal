# 5. Microsoft Entra ID, with a demo scheme for zero-setup runs

* Status: Accepted
* Date: 2026-11-06

## Context

Real deployments authenticate staff and parents through Microsoft Entra ID (or Entra External ID for parents). Reviewers cloning the repository should still be able to run it in one command without creating an app registration.

## Decision

* When `AzureAd:ClientId` is configured, the Api validates Entra ID JWT bearer tokens. Roles come from app roles; the school comes from a `school_id` claim (directory extension or claims-mapping policy).
* When it is not configured, a **Demo** authentication scheme accepts an `X-Demo-User` header naming one of the seeded fictional users. The `/api/demo/users` endpoint is disabled whenever Entra ID is on.

## Consequences

* `dotnet run` works immediately; integration tests use the same scheme.
* The demo scheme must never be enabled in production - it is only registered when Entra ID is absent.
* Next step: OpenID Connect sign-in in the Blazor app (see roadmap).
