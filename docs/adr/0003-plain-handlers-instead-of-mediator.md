# 3. Plain handler classes instead of a mediator library

* Status: Accepted
* Date: 2026-11-06

## Context

MediatR-style libraries are common in Clean Architecture samples, but they add indirection, reflection-based dispatch and (since 2025) commercial licensing considerations for MediatR.

## Decision

Each use case is a small handler class (`CreateAnnouncementHandler`) injected directly into its Minimal API endpoint. Cross-cutting concerns use ASP.NET Core middleware and endpoint filters.

## Consequences

* "Go to definition" from an endpoint lands on the code that runs.
* No pipeline behaviours; if those become necessary, a decorator can be added per handler.
