<!--
Sync Impact Report
Version change: unratified scaffold -> 1.0.0
Modified principles: none (initial ratification)
Added sections: eight core principles; Additional Constraints; Development Workflow;
  governance and amendment rules.
Removed sections: unresolved template placeholders and example comments.
Deferred TODOs: none.
-->
# Encurtador API Constitution

## Core Principles

### I. Application Service Boundary
Every HTTP endpoint MUST delegate application work through an application service and MUST NOT
access the database, DbContext, or persistence repositories directly. This keeps transport concerns
independent from data access and makes application behavior testable.

### II. Business Logic Isolation
Business rules MUST reside in the application or domain layer, never in an endpoint or HTTP handler.
Endpoints and handlers MAY perform HTTP-specific binding, validation orchestration, and response
mapping, but MUST NOT decide business outcomes.

### III. EF Core Persistence Boundary
Persistence MUST use Entity Framework Core. Database providers MUST be replaceable through
configuration and dependency injection without changing application services. Provider-specific
SQL, when necessary, MUST remain within the persistence infrastructure rather than being scattered
through the application.

### IV. Unit-Tested Business Rules
Every business rule MUST have unit-test coverage using xUnit. Tests MUST verify the rule's outcomes,
including relevant boundary conditions, without depending on HTTP or a live database.

### V. Endpoint Integration Coverage
Every HTTP endpoint MUST have at least one integration test covering its successful (happy-path)
response. Tests MUST exercise the endpoint through the application's HTTP boundary.

### VI. Validation Problem Details
Validation failures MUST return HTTP 400 with a ProblemDetails response. Validation responses MUST
communicate the input problem clearly to the client.

### VII. English Code, Portuguese Messages
Source filenames, class names, and code identifiers MUST be in English. User-facing error and
informational messages MUST be in Portuguese, so code remains consistent while API communication
serves its intended audience.

### VIII. Requirement-Linked Commits
Every commit MUST use the format `type(scope): description [REQ-123]`, with a meaningful type,
scope, description, and the identifier of the requirement it implements. The bracketed requirement
identifier is part of the required format, not an unresolved template value.

## Additional Constraints

The API MUST be implemented in .NET using C# and Minimal APIs. Application services MUST depend on
application-level abstractions rather than provider-specific persistence details. API validation
and user-facing responses MUST follow the language and error-response principles above.

## Development Workflow

Changes MUST be reviewed for compliance with this constitution. Changes to business rules MUST
include the required xUnit unit tests, and changes that add or modify endpoints MUST include the
required happy-path integration test. These checks MUST pass before the change is accepted.
Commits MUST reference an existing requirement identifier using the format in Principle VIII.

## Governance

This constitution is the authoritative project-wide standard. Amendments MUST include a documented
rationale, maintainer review and approval, a version update, and an updated amendment date.

Version numbers follow semantic versioning: MAJOR for backward-incompatible removal or redefinition
of a principle; MINOR for adding a principle or materially expanding governance; PATCH for
clarifications and non-semantic wording changes. Every change MUST be reviewed against these
principles. A change that conflicts with this constitution MUST be revised or preceded by an
approved amendment; exceptions MUST NOT be introduced informally.

**Version**: 1.0.0 | **Ratified**: 2026-10-20 | **Last Amended**: 2026-10-20
