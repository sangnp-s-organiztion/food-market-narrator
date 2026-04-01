---
paths:
  - "FoodMarketNarrator.Api/**/*.cs"
---

# Backend Architecture

## Layer Flow

```
HTTP Request
    │
    ▼
[Controller]   ─ thin: validate, delegate
    │
    ▼
[Service]      ─ business logic, orchestration
    │                │
    ▼                ▼
[Repository]   [DTOs / Mapping]
    │
    ▼
[AppDbContext] ─ EF Core → SQL Server
```

**Rule**: Controllers are thin. Business logic lives in Services. Repositories handle data access only.

## Project Structure

```
FoodMarketNarrator.Api/
├── Controllers/          # HTTP endpoints
├── Services/             # Business logic
├── Repositories/         # Data access
├── Models/               # EF Core entities (DB schema)
├── DTOs/                 # Request & response objects
├── Data/                 # AppDbContext, configurations
├── Middleware/           # Auth, error handling
├── Extensions/           # DI registration, app setup
└── Program.cs
```

## Authentication

- **Cookie Authentication** for saler/admin dashboards.
- **Fallback authorization policy**: all endpoints require authentication by default.
- **Public endpoints** are whitelisted via `PublicEndpoints` + `PublicEndpointConvention` middleware.
- **Never remove `[Authorize]`** without confirming the business requirement.

## PublicEndpoints Convention

Endpoints that serve visitors (mobile app) must be explicitly listed:

```csharp
// Current public endpoints — UPDATE THIS when adding new public APIs
public static readonly string[] PublicEndpoints =
{
    "/Auth/login",
    "/Language",
    "/Language/{languageCode}",
    "/Restaurant",
    "/Restaurant/{id}",
    "/public/Restaurant/{restaurantId}/images",
    "/public/Restaurant/{restaurantId}/dishes",
    "/public/Restaurant/{restaurantId}/audios",
    // Add new public endpoints here
};
```

## Response Standard

```csharp
// Always wrap — do NOT return raw entities
return Ok(new ApiResponse<T> { Success = true, Data = dto, Message = "" });
return BadRequest(new ApiResponse<T> { Success = false, Message = "..." });
```

## DTO Rules

- **Request DTOs**: for input validation at controller level (ModelState).
- **Response DTOs**: mapped from entities before returning.
- **No entity leaks** — audit new endpoints to confirm DTO usage.
- Use manual mapping or AutoMapper; keep it consistent within a service.

## Error Handling

- Use `ProblemDetails` middleware — no stack traces to clients.
- Standard error response:
  ```json
  { "success": false, "data": null, "message": "Human-readable error" }
  ```
- Log errors server-side with structured logging.

## Static Media

- `/maui-images/{fileName}` — app images (MAUI Resources/Images)
- `/maui-audios/{fileName}` — packaged narration audio
- `/uploads/audios/{fileName}` — seller-uploaded audio

## Adding a New Endpoint

1. Add to Controller (declare public/private via `PublicEndpoints` if needed).
2. Add corresponding method in Service.
3. Add repository method if new data access needed.
4. Update `DTOs/` with request/response types.
5. Update `PublicEndpoints` if public.
6. Update `.claude/architecture/api-architecture.md`.
7. Run `dotnet build` to verify.

## Adding a New Entity

1. Add to `Models/` as EF Core entity.
2. Add DbSet in `AppDbContext`.
3. Add migration (`dotnet ef migrations add ...`).
4. Create Repository if needed.
5. Create Service.
6. Create Controller with DTOs.
7. Document in `business-domain.md`.
