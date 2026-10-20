# Quickstart: Link Shortener

This guide validates the public contract end-to-end after implementation. See
[openapi.yaml](./contracts/openapi.yaml) for request/response shapes and
[data-model.md](./data-model.md) for persistence rules.

## Prerequisites

- .NET 10 SDK for local validation, or Docker with Compose for container validation.
- PowerShell 7 or Windows PowerShell for the sample commands below.

## Run tests

From the repository root:

```powershell
dotnet test .\LinkShortener.sln
```

Expected: unit and HTTP integration tests pass. Integration tests use SQLite and verify
the unique code index, persistent records, and atomic access counting.

## Run with Docker Compose

From the repository root:

```powershell
docker compose up --build
```

Expected: the API starts on `http://localhost:8080` in Development mode, with Scalar at
`http://localhost:8080/scalar` and OpenAPI at `http://localhost:8080/openapi/v1.json`.
SQLite data is stored in the Compose named volume and survives container recreation.
`docker compose down -v` intentionally removes that volume and its data.

## Create, redirect, and inspect a link

Create a link:

```powershell
$created = Invoke-RestMethod -Method Post `
  -Uri 'http://localhost:8080/api/links' `
  -ContentType 'application/json' `
  -Body '{"destinationUrl":"https://example.com/path","customCode":"Halloween26"}'
$created
```

Expected: HTTP 201, canonical code `HALLOWEEN26`, and a shareable `shortUrl`. In a
healthy local Docker Compose environment with no competing creation requests, the complete
response arrives in less than 60 seconds after the request is sent.

Open the short URL without automatically following the redirect:

```powershell
$response = Invoke-WebRequest -Uri $created.shortUrl -MaximumRedirection 0 `
  -SkipHttpErrorCheck
$response.StatusCode
$response.Headers.Location
```

Expected: HTTP 302 with `Location: https://example.com/path`.

Consult the count:

```powershell
Invoke-RestMethod -Uri "http://localhost:8080/api/links/$($created.code)/stats"
```

Expected: `accessCount` is `1`; another successful redirect raises it to `2`. Restarting
the application and consulting again returns the same persisted link and count.

## Verify errors and expiry

- Submit a malformed, non-HTTP(S), or over-2048-character destination: expect HTTP 400 and
  Portuguese ProblemDetails; no record is created.
- Submit an already-used custom code: expect HTTP 409; the original link remains unchanged.
- Submit an expiration instant at or before the current time: expect HTTP 400.
- Open an unknown code: expect HTTP 404. Open an expired link: expect HTTP 410. Neither
  response increments a count or redirects to the destination.
- Submit an eleventh otherwise-valid creation from the same source address during a rolling
  60-second interval: expect HTTP 429 and no link creation. Verify invalid input and duplicate
  custom-code requests do not consume the limit. Restart the application within that interval
  and verify the limit is still enforced.

## Verify generated codes and concurrency

- Create a link without `customCode`: expect a 7-character code containing only symbols
  from `23456789ABCDEFGHJKMNPQRSTUVWXYZ`.
- Release exactly 100 requests for one active code simultaneously, then consult stats: all
  100 requests must return HTTP 302 and the reported count must increase by exactly 100.
- Recreate the container without removing the Compose volume and verify links and counts
  remain unchanged.
