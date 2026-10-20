# Research: Link Shortener

## Runtime and HTTP boundary

- **Decision**: Target .NET 10 and use ASP.NET Core Minimal APIs, with route handlers delegating
  use cases to `LinkService`.
- **Rationale**: This matches the requested stack and the constitution's service boundary.
  Minimal APIs support OpenAPI integration in the .NET 10 target.
- **Alternatives considered**: MVC controllers; not selected because the user requested
  Minimal APIs and there are only a few resource routes.
- **References**: [Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-10.0),
  [.NET support policy](https://learn.microsoft.com/en-us/dotnet/core/releases-and-support)

## Provider-neutral EF Core persistence

- **Decision**: Use EF Core with SQLite in this release, configure the provider in the
  infrastructure composition root, and expose application-level operations rather than
  `DbContext` or provider-specific types to `LinkService`. Add PostgreSQL with a provider
  package and provider-specific migration set when needed.
- **Rationale**: Provider selection remains an infrastructure concern. EF Core migrations
  are provider-specific, so a future PostgreSQL rollout should maintain its own migration set
  rather than assuming SQLite migrations are portable.
- **Alternatives considered**: Provider-specific SQL spread through application code; rejected
  because it couples business logic to the current database. A single migration set for both
  providers is not assumed portable.
- **References**: [EF Core multiple migration sets](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers),
  [Npgsql EF Core provider](https://www.npgsql.org/efcore/)

## Unique short codes and canonicalization

- **Decision**: Normalize codes to invariant uppercase before lookup/storage and enforce a
  unique index on the canonical code. Keep the service pre-check for friendly conflict errors,
  while treating the database constraint as authoritative under races. Retry generated-code
  collisions with a bounded attempt count. Generate from
  `23456789ABCDEFGHJKMNPQRSTUVWXYZ`.
- **Rationale**: Application pre-checks cannot prevent two concurrent requests from observing
  the same code as unused. Canonicalization provides consistent case-insensitive identity
  across SQLite and PostgreSQL without relying on provider-specific collation behavior. The
  selected uppercase alphabet removes visually ambiguous `0`, `1`, `I`, `L`, and `O`.
- **Alternatives considered**: Provider-specific case-insensitive collations, or a unique
  index on unnormalized values; not selected because their behavior is less portable and may
  permit case variants to collide differently by provider. Custom codes still accept the
  complete ASCII alphanumeric set required by the spec.
- **References**: [EF Core indexes](https://learn.microsoft.com/en-us/ef/core/modeling/indexes),
  [EF Core collations and case sensitivity](https://learn.microsoft.com/en-us/ef/core/miscellaneous/collations)

## Atomic redirect counting

- **Decision**: Execute one conditional database-side update that increments `AccessCount`
  only when the canonical code exists and the link has not expired. Redirect only if one row
  was updated; do not load, increment, and save the count in application memory.
- **Rationale**: A database-side arithmetic update avoids lost increments from concurrent
  redirects. Including the expiration predicate makes the update the eligibility decision
  point. The redirect target can then be read after the successful update; the feature does
  not support deletion.
- **Alternatives considered**: Read the link, increment in memory, and save; rejected because
  concurrent updates can overwrite one another. Provider-specific `UPDATE ... RETURNING`
  could combine target retrieval and update, but would couple persistence behavior to a
  provider; prefer EF Core's translated `ExecuteUpdate` path unless later performance evidence
  justifies that trade-off.
- **References**: [EF Core ExecuteUpdate](https://learn.microsoft.com/en-us/ef/core/saving/execute-insert-update-delete)

## SQLite deployment and persisted values

- **Decision**: Store the expiry instant as UTC `DateTime`, the monotonic count as an integer
  (`long`), and the SQLite database under a named Docker Compose volume. Keep SQLite write
  transactions short; use WAL only as a reader/writer coexistence option, not as a way to
  enable concurrent writers. Run one API instance for the SQLite deployment.
- **Rationale**: EF Core documents SQLite limitations around `DateTimeOffset` comparisons.
  SQLite permits multiple readers but serializes writers, and a named Docker volume preserves
  the database independently of container lifecycle.
- **Alternatives considered**: Store the database only in the container layer; rejected
  because it loses persisted data when the container is removed. Use WAL to increase writer
  concurrency; rejected because WAL does not remove SQLite's single-writer constraint.
- **References**: [SQLite provider limitations](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations),
  [SQLite isolation](https://www.sqlite.org/isolation.html),
  [SQLite WAL](https://www.sqlite.org/wal.html),
  [Docker volumes](https://docs.docker.com/engine/storage/volumes/)

## Creation rate limiting

- **Decision**: Keep timestamped successful creation attempts in the configured relational
  store and atomically enforce the limit of 10 per source address in the preceding 60
  seconds. Remove attempts older than the window. Implement the check, reservation, and link
  insert in one persistence transaction; the application service owns the policy threshold.
  Trust forwarded client addresses only from explicitly configured proxies.
- **Rationale**: A database-backed rolling window remains effective across application
  restarts and can be coordinated across instances when the provider supports the required
  transaction/locking semantics. SQLite serializes writes for the current single-instance
  deployment; a future PostgreSQL adapter must preserve the same atomic reservation behavior.
  Trusting arbitrary forwarding headers would allow clients to choose their own rate-limit
  identity.
- **Alternatives considered**: An in-process limiter; rejected because it resets on restart
  and cannot coordinate instances. A fixed window is not equivalent to a rolling 60-second
  window.

## OpenAPI, Scalar, and tests

- **Decision**: Generate OpenAPI with ASP.NET Core and expose Scalar's API reference in
  Development. Use xUnit for business-rule unit tests and HTTP integration tests with SQLite
  for persistence and endpoint behavior.
- **Rationale**: Scalar's ASP.NET Core integration works with the framework OpenAPI document.
  SQLite integration tests exercise the unique index and atomic update against a relational
  provider rather than relying solely on mocked persistence.
- **Alternatives considered**: Production-visible docs by default; not selected because
  exposing interactive API documentation should be an explicit deployment choice. Mock-only
  persistence tests; insufficient to verify relational constraints and update semantics.
- **References**: [ASP.NET Core OpenAPI](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0),
  [Scalar ASP.NET Core integration](https://scalar.com/products/api-references/integrations/aspnetcore),
  [xUnit getting started](https://xunit.net/docs/getting-started/v3/getting-started)

## Future management-key storage

- **Decision**: Do not add a management key or management endpoints to this feature. If future
  management operations introduce such a key, store only its cryptographic hash and never its
  plaintext value.
- **Rationale**: The current specification makes statistics public to anyone who knows the
  code and excludes editing and deletion. Adding a secret without a protected operation would
  introduce unused state and change scope.
- **Alternatives considered**: Add an unused key field now; rejected because it has no current
  behavior or acceptance criteria.
