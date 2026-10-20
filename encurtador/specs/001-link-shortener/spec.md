# Feature Specification: Link Shortener

**Feature Branch**: `001-link-shortener`

**Created**: 2026-10-20

**Status**: Draft

**Input**: User description: "A link shortener where a creator submits a long URL and receives a short link to share. Visitors who open the short link are redirected to its destination, and each access is counted. The creator can consult the access count. Automatically generated codes have 7 alphanumeric characters; creators may choose a unique 4–20-character code. Destinations must use HTTP or HTTPS and contain at most 2048 characters. Creators may set an expiration date; expired links do not redirect or count accesses. The count never decreases, and links remain valid after application restarts. Out of scope: user registration and login, custom domains, geographic reports, and editing an existing link's destination."

## Clarifications

### Session 2026-10-20

- Q: A consulta de acessos deve estar disponível a qualquer pessoa que conheça o código curto, ou deve exigir um segredo separado do link compartilhável? → A: Qualquer pessoa com o código pode consultar.
- Q: A especificação ainda não define a carga inicial, que influencia decisões e testes de desempenho. Qual número de visitantes simultâneos o serviço deve suportar no lançamento? → A: Até 100 visitantes simultâneos.
- Q: Como a criação de links não exige login, o serviço deve limitar quantos links um mesmo endereço de origem pode criar para reduzir spam e abuso automatizado? → A: No máximo 10 criações por minuto por endereço de origem.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create a shareable short link (Priority: P1)

As a link creator, I want to submit a destination URL and receive a short link, so that I can share a compact address instead of the long destination.

**Why this priority**: Link creation is the essential action that makes the service useful.

**Independent Test**: Submit a valid destination with and without a custom code and verify that each accepted request returns a usable, unique short link.

**Acceptance Scenarios**:

1. **Given** a valid HTTP or HTTPS destination and no requested code, **When** the creator creates a link, **Then** the service returns a short link with a unique 7-character alphanumeric code.
2. **Given** a valid destination and an unused custom code of 4–20 alphanumeric characters, **When** the creator creates a link, **Then** the returned short link uses that code.
3. **Given** an invalid destination or an invalid custom code, **When** the creator submits the request, **Then** the link is not created and the input problem is clearly reported.
4. **Given** a custom code already in use, **When** another creator requests that code, **Then** the request is rejected and the existing link remains unchanged.
5. **Given** a valid destination and a future expiration date, **When** the creator creates a link, **Then** the short link remains active only until that expiration date.
6. **Given** an address of origin that has already created 10 links in the last 60 seconds, **When** it attempts to create another link, **Then** the service rejects the request, clearly explains that the limit was reached, and creates no link.

---

### User Story 2 - Open a short link (Priority: P1)

As a visitor, I want to open a short link and reach its destination, so that I can follow a link shared with me.

**Why this priority**: Redirecting visitors is the core value delivered by a created link.

**Independent Test**: Create an active link, open it, and verify that the browser is directed to the exact destination and that the access count increases by one.

**Acceptance Scenarios**:

1. **Given** an active short link, **When** a visitor opens it, **Then** the visitor is redirected to its saved destination and its access count increases by exactly one.
2. **Given** an expired short link, **When** a visitor opens it, **Then** the visitor is not redirected to its destination and its access count does not increase.
3. **Given** an unknown short code, **When** a visitor opens it, **Then** the service reports that no link exists for that code and does not record an access.
4. **Given** an active short link, **When** multiple visitors open it, **Then** every successful redirect is counted, including concurrent visits, and the count does not lose or reverse any access.

---

### User Story 3 - Consult link access count (Priority: P2)

As a link creator, I want to consult how many times my short link has been opened, so that I can understand its reach.

**Why this priority**: Access statistics provide value after a link is shared, but depend on the creation and redirect journeys.

**Independent Test**: Create a link, perform a known number of successful opens, and consult its count using its short code.

**Acceptance Scenarios**:

1. **Given** a short code for an existing link, **When** the creator consults its statistics, **Then** the service returns the total number of counted accesses.
2. **Given** a link with no counted accesses, **When** the creator consults its statistics, **Then** the returned count is zero.
3. **Given** a link whose count was previously consulted, **When** additional successful opens occur and the count is consulted again, **Then** the new count is at least the previous count and reflects every additional successful open.
4. **Given** a link that remains stored, **When** the application is restarted and the creator consults its count, **Then** the link and its accumulated count are still available.

### Edge Cases

- A destination exactly 2048 characters long is accepted; a destination longer than 2048 characters is rejected.
- Destination schemes are accepted without regard to letter casing only when they identify HTTP or HTTPS; other schemes and malformed URLs are rejected.
- Custom codes shorter than 4 or longer than 20 characters, or containing characters other than ASCII letters and digits, are rejected.
- A link is expired at its expiration instant; attempts at or after that instant are not redirected or counted.
- An expiration date must be in the future when a link is created; a past or current expiration date is rejected.
- A generated code collision is resolved by generating another code before returning a short link; no duplicate code is ever assigned.
- An address of origin may create at most 10 successfully created links during any rolling 60-second period. Invalid requests and requests rejected because a custom code is already in use do not consume the limit; requests rejected by the limit do not create a link.
- Repeated visits count as separate accesses when each visit results in a redirect, regardless of who opens the link.
- An expired link remains identifiable for statistics, but subsequent attempts to open it do not increase its count.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The service MUST allow a creator to submit a destination URL and receive a short link.
- **FR-002**: When the creator does not supply a code, the service MUST generate a code containing exactly 7 alphanumeric characters.
- **FR-003**: The service MUST allow the creator to supply a code containing 4–20 ASCII letters or digits.
- **FR-004**: The service MUST ensure that every code is unique across all links, regardless of whether it was generated or supplied. A request to use an existing custom code MUST be rejected without changing the existing link.
- **FR-005**: The service MUST accept only well-formed destination URLs using HTTP or HTTPS, with a total length of no more than 2048 characters.
- **FR-006**: The service MUST allow a creator to set an optional future expiration date for a link.
- **FR-007**: The service MUST redirect visitors who open an active short link to that link's saved destination.
- **FR-008**: The service MUST increase a link's access count by exactly one for each successful redirect and MUST NOT count requests that do not result in a redirect.
- **FR-009**: The service MUST NOT redirect or increase the access count for an expired link.
- **FR-010**: The service MUST make the current access count available to anyone who provides an existing short code; no separate secret is required.
- **FR-011**: The access count MUST never decrease, including when accesses occur concurrently.
- **FR-012**: The service MUST retain links, expiration dates, and accumulated access counts across application restarts.
- **FR-013**: The service MUST clearly report invalid input, an already-used custom code, or a short code for which no link exists in Portuguese, without creating or changing a link as a side effect.
- **FR-014**: The service MUST allow no more than 10 successfully created links from a single source address during any rolling 60-second period. Invalid requests and requests rejected because a custom code is already in use MUST NOT consume the limit. Further otherwise-valid requests MUST be rejected with a clear Portuguese message and MUST NOT create a link.

### Key Entities *(include if feature involves data)*

- **Short Link**: A shareable code associated with a destination URL, its creation and optional expiration dates, and an accumulated access count.
- **Access**: A successful opening of an active short link that results in a redirect and contributes one to that link's count.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In acceptance testing, 100% of valid link-creation requests return a short link with the requested code or an automatically generated 7-character alphanumeric code.
- **SC-002**: In acceptance testing, every valid active-link visit redirects to the saved destination and increases the reported count by exactly one.
- **SC-003**: In acceptance testing, 100% of visits to expired links are neither redirected to the saved destination nor included in the access count.
- **SC-004**: In a healthy local Docker Compose environment with no competing creation requests, a valid `POST /api/links` request MUST receive its HTTP 201 response with a shareable link in less than 60 seconds, measured from sending the request until receiving the complete response.
- **SC-005**: After an application restart, 100% of links and access counts created before restart remain available and unchanged.
- **SC-006**: At least 90% of first-time users in usability testing can create a shareable link and understand its access count without assistance.
- **SC-007**: The service successfully redirects up to 100 simultaneous visitors to active links and records each successful access exactly once.

## Assumptions

- Custom codes use the same alphanumeric character set as generated codes: ASCII letters and digits. Code uniqueness is case-insensitive so that codes differing only by letter casing cannot refer to different links.
- Expiration dates represent an instant in time; provided time-zone information is honored, and a date without a time-zone offset is interpreted as UTC.
- Anyone who knows a short code can consult its access count; no separate secret, creator identity, or private link listing is provided.
- Each request that results in a redirect is one access, including repeated visits by the same person. Requests for expired or unknown codes are not accesses.
- Short links and their counts are retained indefinitely for this feature; link deletion and destination editing are not provided.
- The short link uses the service's standard public origin; custom domains are not supported.
- User-facing error and informational messages are in Portuguese.
