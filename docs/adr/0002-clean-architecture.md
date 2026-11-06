# 2. Clean Architecture with a pure domain

* Status: Accepted
* Date: 2026-11-06

## Context

School platforms live for years and outlast UI frameworks and databases. Business rules (who can publish, when a parent can acknowledge, when reminders go out) must be testable without a database or web server.

## Decision

* Split into Domain, Application, Infrastructure, Api, Web and a small Contracts project.
* The Domain project has **no package or framework references**. Entities expose behaviour (`Publish`, `Acknowledge`) instead of public setters.
* Expected failures are returned as `Result`/`Error` values; exceptions are reserved for the unexpected.
* Errors carry an `ErrorType` that the Api maps to RFC 9457 Problem Details (400/403/404/409).

## Consequences

* Domain and handler tests run in milliseconds with in-memory fakes.
* Slightly more files than a single-project app - acceptable for a long-lived product.
