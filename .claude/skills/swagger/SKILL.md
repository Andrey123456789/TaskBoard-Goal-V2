---
name: swagger
description: >
  Swagger UI and API documentation guidance for ASP.NET Core Controller APIs.
  Uses Swashbuckle.AspNetCore as the default interactive documentation/testing
  surface. Covers Swagger generation, Development-only UI exposure, controller
  response metadata, ProblemDetails, authentication schemes, descriptions,
  launch settings, and contract verification.
---

# Swagger

## Core Principles

1. Swagger UI is the default interactive API documentation and manual testing
   surface for new Controller-based APIs in this template.
2. Swagger documentation must describe the API that actually runs.
3. Follow the `http-api` skill for status-code semantics.
4. Follow the `error-handling` skill for ProblemDetails/error behavior.
5. Enable Swagger UI in Development by default; expose it elsewhere only when a
   deployment requirement explicitly justifies it.
6. Do not add response codes or schemas merely to make documentation appear more
   comprehensive.

Swagger tooling describes the API using the OpenAPI specification internally.

The developer-facing requirement in this template is the interactive Swagger UI,
not a standalone raw OpenAPI endpoint.

## Baseline Package

Use:

```text
Swashbuckle.AspNetCore
```

Select the latest stable compatible version according to
`.claude/rules/dependencies.md`.

## Basic Setup

For a Controller API, a normal setup is:

```csharp
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
```

Keep Swagger configuration in the API/composition layer.

Do not make built-in `AddOpenApi()` / `MapOpenApi()` the project default when the
requirement is Swagger UI for interactive testing.

## Development Launch

For a new local Development API, configure the launch profile so the browser can
open Swagger directly when that improves developer experience:

```json
{
  "launchBrowser": true,
  "launchUrl": "swagger"
}
```

The actual HTTP/HTTPS application URLs must agree with the launch profile.

Do not configure a browser launch URL that points to HTTPS on an HTTP-only port.

## Controller Response Metadata

Document important response types when useful and keep metadata synchronized with
runtime behavior.

Example:

```csharp
[HttpGet("{id:guid}")]
[ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public async Task<ActionResult<OrderResponse>> GetById(
    Guid id,
    CancellationToken cancellationToken)
{
    // ...
}
```

Do not document `201` when the endpoint actually returns `200`.

Do not document `204` when the endpoint returns a response body.

Document only status codes that are part of the endpoint's real runtime contract.

When determining possible responses, consider the complete HTTP pipeline, not
only explicit branches inside the Controller action. Relevant responses may also
come from model binding, validation, authentication/authorization, rate limiting,
centralized exception handling, or other configured middleware.

Do not add speculative `ProducesResponseType` entries merely because a status
code is theoretically possible.

## Creation Responses

For resource creation, document `201 Created` when that is the runtime contract.
Prefer `CreatedAtAction` / `CreatedAtRoute` when a canonical resource URL exists.

## Error Responses

When runtime failures use ProblemDetails or ValidationProblemDetails, Swagger
metadata should describe those contracts where useful.
Do not expose internal exception types, stack traces, SQL details, connection
strings, filesystem paths, or other implementation details in Swagger examples
or descriptions.

## Authentication Metadata

When the API actually uses authentication, configure the matching Swagger
security definition and requirements.
For Bearer authentication, expose an appropriate Bearer scheme so authenticated
endpoints can be exercised through Swagger UI.
Do not add an authentication scheme merely because an authentication package is
installed.
Swagger must reflect configured runtime behavior.

## Descriptions

Document behavior that helps an API consumer understand the contract, including
where relevant:
- important preconditions;
- idempotency semantics;
- pagination/filtering behavior;
- meaningful response outcomes.
Avoid descriptions that simply repeat action or property names.

## XML Documentation

XML comments may be useful for public API contracts when the project chooses
them.
Do not add XML comments to every internal type solely for Swagger generation.

## Multiple Documents

Use multiple Swagger documents only when there is a real contract separation,
for example API versions or distinct public/internal surfaces.
Do not split documents merely because tooling supports it.
API versioning strategy belongs to the api-versioning skill.

## Generated Clients

If the API contract is used for Angular or other client generation:
- treat breaking schema changes seriously;
- keep response metadata accurate;
- avoid unstable anonymous response shapes;
- use explicit request/response contracts;
- review generated-client changes when the API contract changes.

## Anti-Patterns

Avoid:
- Swagger metadata that disagrees with runtime responses;
- exposing Swagger UI publicly without a reason;
- treating Swagger as a substitute for integration tests;
- globally applying authentication requirements to anonymous endpoints;
- using anonymous objects as important long-lived API contracts;
- adding a second competing API-documentation stack without a concrete need.

## Verification

When API behavior changes:
1. verify Controller behavior;
2. verify relevant integration tests;
3. verify response metadata;
4. start the API when the change affects startup/composition;
5. open the Development Swagger UI and confirm it loads when Swagger is part of
   the application baseline;
6. exercise representative endpoints through Swagger when doing so provides a
   meaningful behavioral check;
7. preserve newly verified behavior in maintained automated tests when required
   by the testing policy.
