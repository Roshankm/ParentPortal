# 1. Record architecture decisions

* Status: Accepted
* Date: 2026-11-06

## Context

Design decisions get lost in chat threads and commit messages. New contributors (and future me) need to know *why*, not just *what*.

## Decision

Keep lightweight Architecture Decision Records in `docs/adr`, one Markdown file per decision, numbered sequentially, using the format: Context → Decision → Consequences.

## Consequences

* Every significant change to structure, persistence, auth or hosting gets an ADR in the same PR.
* Superseded ADRs are kept and marked `Superseded by N`.
