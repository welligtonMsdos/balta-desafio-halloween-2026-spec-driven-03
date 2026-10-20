# Implementation Plan: Link Shortener

**Branch**: `001-link-shortener` | **Date**: 2026-10-20 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-link-shortener/spec.md`

## Summary

Deliver a persistent link-shortening API with link creation, redirects, and public access
statistics. The .NET 10 Minimal API is a transport adapter over an application `LinkService`;
EF Core persists links to SQLite while keeping provider selection in infrastructure
configuration so PostgreSQL can be added without changing application rules. A unique index
protects code uniqueness under concurrent creation, and successful redirect eligibility and
counter increments are decided by a single conditional database-side update.

## Technical Context

**Language/Version**: C# / .NET 10

**Primary Dependencies**: ASP.NET Core Minimal APIs, EF Core, EF Core SQLite provider, Scalar
ASP.NET Core integration, xUnit

**Storage**: SQLite for this release; EF Core provider configured through infrastructure
dependency injection, with PostgreSQL as a future replaceable provider

**Testing**: xUnit unit tests for application/domain rules and HTTP integration tests using the
application boundary and SQLite

**Target Platform**: .NET 10 container and local development environments

**Project Type**: HTTP web service

**Performance Goals**: Correctly serve and count redirects for up to 100 simultaneous visitors
(SC-007); create a valid shareable link in under one minute (SC-004)

**Constraints**: Generated codes have exactly 7 characters from an unambiguous ASCII
alphanumeric alphabet; custom codes accept 4–20 ASCII letters or digits and are canonicalized
case-insensitively. Destinations are HTTP(S), at most 2048 characters. Creation is limited to
10 creations per source address in any rolling 60-second period. Persist recent creation
attempt timestamps so this limit remains effective across restarts; expire old records and
keep write transactions short. SQLite serializes writes, so deploy a single API instance with
the SQLite file. Persist the database directory on a named Docker Compose volume.

**Scale/Scope**: Three user journeys (create, redirect, consult statistics); no registration,
authentication, link editing/deletion, custom domains, or geographic analytics. No management
key is added in this feature. If protected management is introduced later, persist only a
cryptographic hash of its key, never plaintext.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Gate | Plan |
|-----------|------|------|
| I. Application Service Boundary | Endpoints must not access persistence directly | Pass: each endpoint delegates to `LinkService`. |
| II. Business Logic Isolation | Rules remain outside HTTP handlers | Pass: `LinkService` owns creation, code, expiration, and redirect rules. |
| III. EF Core Persistence Boundary | EF Core and replaceable provider | Pass: EF Core stays behind infrastructure adapters and DI provider configuration. |
| IV. Unit-Tested Business Rules | xUnit coverage including boundaries | Pass: unit tests cover validation, expiration, uniqueness outcomes, and rate-limit behavior. |
| V. Endpoint Integration Coverage | Happy-path test per endpoint | Pass: integration tests cover creation, redirect, and statistics through HTTP. |
| VI. Validation Problem Details | Invalid input is HTTP 400 ProblemDetails | Pass: contract defines Portuguese ProblemDetails for validation errors. |
| VII. English Code, Portuguese Messages | Source identifiers English; messages Portuguese | Pass: plan and contract preserve this convention. |
| VIII. Requirement-Linked Commits | Commit format references requirement IDs | Pass: implementation commits must use the constitution's required format. |

No gate violations identified.

## Project Structure

### Documentation (this feature)

```text
specs/001-link-shortener/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── openapi.yaml
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── LinkShortener.Api/
│   ├── Endpoints/
│   │   └── LinkEndpoints.cs
│   ├── Program.cs
│   └── appsettings.json
├── LinkShortener.Application/
│   ├── LinkService.cs
│   └── Abstractions/
├── LinkShortener.Domain/
│   └── ShortLink.cs
└── LinkShortener.Infrastructure/
    ├── Persistence/
    └── DependencyInjection.cs
tests/
├── LinkShortener.UnitTests/
└── LinkShortener.IntegrationTests/
Dockerfile
docker-compose.yml
LinkShortener.sln
```

**Structure Decision**: A layered .NET solution separates HTTP transport, application rules,
domain entities, and EF Core/provider-specific persistence. All link HTTP routes are grouped
in one endpoint file for the resource. Unit and integration tests are separate so business
rules remain testable without HTTP while each public route is verified end-to-end.

## Design Decisions

- Check a requested code in `LinkService` to return a clear conflict, but rely on a unique
  database index over the canonical code as the final concurrency guarantee. Retry generated
  collisions a bounded number of times; reject a custom-code collision.
- Canonicalize ASCII codes to invariant uppercase and index that value, avoiding provider
  collation differences. Generated codes use `23456789ABCDEFGHJKMNPQRSTUVWXYZ`, which omits
  visually ambiguous `0`, `1`, `I`, `L`, and `O`. Custom codes retain the specification's
  full ASCII alphanumeric allowance.
- Increment access count with a conditional atomic database update; never read the count and
  write a computed replacement from application memory. The update also checks expiry, and a
  redirect is returned only when exactly one row was updated.
- Define a counted access as a request for which the service issues a redirect response; a
  server cannot establish whether a remote client received or followed that response.
- Apply the exact per-source rolling-window policy in the application layer: only successful
  creations count toward the maximum of 10 in the preceding 60 seconds. Invalid input and
  requests rejected for a duplicate custom code do not consume capacity. Use a persistence
  abstraction that atomically checks the recent successful-creation window, reserves an
  allowed creation, and saves the link. Keep only attempts from the last 60 seconds. Resolve
  client addresses only from configured trusted proxies, not arbitrary forwarding headers.
  A future multi-instance deployment must retain shared transaction/locking semantics for
  this policy.
- Expose OpenAPI and Scalar in Development by default. Keep deployment configuration and
  SQLite data outside the container's ephemeral filesystem.

## Complexity Tracking

No constitution violations or additional architectural complexity require justification.

## Post-Design Constitution Check

All design artifacts preserve the application-service boundary, provider-neutral application
logic, xUnit unit and HTTP integration coverage, Portuguese ProblemDetails, and the English
source naming convention. The proposed additional solution projects directly support the
constitution's required separation; no exception or amendment is needed.
