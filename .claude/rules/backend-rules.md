---
paths:
  - "FoodMarketNarrator.Api/**/*.cs"
---

# Backend Rules

## Architecture

**Layered**: Controller → Service → Repository → AppDbContext.

- Controller: thin, validate ModelState, return `ApiResponse<T>`.
- Service: all business logic, ownership checks, orchestration.
- Repository: data access only — no business rules.
- No entity returned directly from endpoints — always map to DTO.

## Routing & Endpoints

- Route naming: `/Restaurant`, `/Language`, `/Auth` (no `/api` prefix).
- New endpoints: add to controller, update `PublicEndpoints` if public, update `api-architecture.md`.
- PublicEndpoints convention: all mobile-facing endpoints must be listed in the `PublicEndpoints` array.

## Auth & Authorization

- **Cookie auth** for saler/admin flows.
- Default fallback policy requires authentication on all endpoints.
- **Seller ownership**: every service method touching a Seller's resource MUST verify `restaurant.UserId == currentUserId`. Never trust the client to pass the right restaurant ID.
- Never strip `[Authorize]` without confirmed business requirement.

## DTO & Validation

```
Request  → [Controller: ModelState validation] → [Service: business validation] → [Repository]
Response ← [Service: map entity → DTO] ← [Repository: return entity]
```

- Use request DTOs with Data Annotations for controller-level validation.
- Map entities to response DTOs in service or with AutoMapper.
- Consistent error format: `{ success: false, message: "..." }`.

## Error Handling

- ProblemDetails middleware for consistent error shape.
- Structured logging for errors (Serilog preferred).
- Never expose stack traces or internal details to clients.
- 400: validation failure. 401: unauthenticated. 403: unauthorized. 404: not found.

## Media Upload

- Images and audio upload must go through existing upload service.
- Maintain static path compatibility:
  - `/maui-images`
  - `/maui-audios`
  - `/uploads/audios`

## Code Quality

- No duplicate logic — reuse existing services/repositories.
- No magic strings — use constants, enums, `nameof()`.
- No changes to API contracts without a migration plan.
- Keep changes small; prefer additive changes for backward compatibility.

## Multi-Step Mutations

When an operation touches multiple entities (e.g., activating a restaurant and deactivating its old audio), consider a service-level orchestrator rather than scattering logic across repositories.

## Adding New Dependencies

1. Check license (prefer MIT, Apache 2.0, BSD, ISC).
2. Prefer stable versions — no alpha, beta, RC.
3. Check if the need already exists in the stack (no duplication).
4. Pin version in `Directory.Packages.props` if available.
