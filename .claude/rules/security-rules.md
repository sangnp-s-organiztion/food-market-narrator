---
paths:
  - "FoodMarketNarrator.Api/**/*.cs"
  - "FoodMarketNarrator.Maui/**/*.cs"
  - "admin/**"
  - "saler/**"
---

# Security Rules (OWASP Top 10:2025)

## A01 — Broken Access Control

- Every API endpoint defaults to `[Authorize]`. Public endpoints must be explicitly added to `PublicEndpoints`.
- **Seller ownership**: backend MUST enforce `restaurant.UserId == currentUserId` in service layer. Client-side filtering is UX only.
- Validate resource ownership on every mutation (IDOR prevention).
- No `AllowAnonymous` unless the endpoint is in `PublicEndpoints` AND business requirement is confirmed.
- Redirects: validate against an allowlist.

## A02 — Security Misconfiguration

- `AllowedHosts` in `appsettings.json` must be specific hosts — never `*` in production.
- Security headers on all API responses:
  - `X-Content-Type-Options: nosniff`
  - `X-Frame-Options: DENY`
  - `Strict-Transport-Security: max-age=31536000; includeSubDomains`
  - `Referrer-Policy: strict-origin-when-cross-origin`
- No stack traces or error details to clients in production.

## A03 — Software Supply Chain

- `npm audit` and `dotnet list package --vulnerable` regularly.
- Pin dependency versions (use `Directory.Packages.props` for .NET).
- No preview, alpha, beta, or RC packages in production.
- Check license before adding: prefer MIT, Apache 2.0, BSD, ISC. Flag paid/commercial packages for review.

## A04 — Cryptographic Failures

- Passwords hashed with bcrypt/Argon2 — never plain text or reversible encryption.
- Never hardcode secrets, keys, or connection strings in code. Use environment variables or Key Vault.
- TLS on all external connections.
- No secrets in URL query strings (they appear in logs).

## A05 — Injection

- **EF Core**: use LINQ — parameterized by default. Never concatenate user input into queries.
- Never use `FromSqlRaw` with string concatenation.
- **React**: never use `dangerouslySetInnerHTML`. Sanitize all user-generated text.
- **MAUI**: validate all data from API before displaying.

## A06 — Vulnerable Components

- Keep .NET, NuGet packages, and npm packages updated.
- Monitor CVE advisories for direct and transitive dependencies.
- Automated scanning in CI pipeline.

## A07 — Authentication Failures

- Strong password policy (enforced server-side).
- Rate limiting on `/Auth/login` endpoint.
- Account lockout after failed attempts (configurable threshold).
- Session invalidated on logout (server-side cookie deletion).
- No silent session extension.

## A08 — Software & Data Integrity Failures

- File uploads: validate type by content (magic bytes), not just extension.
- Audio files: check format before storing. Reject executables.
- CI/CD pipeline integrity: no bypass of integrity checks.

## A09 — Security Logging & Monitoring

- Log authentication events (login success/failure, logout).
- Log authorization failures (403 attempts).
- Never log: passwords, tokens, sensitive PII.
- Structured logging format (machine-parseable).
- Alert on unusual patterns (e.g., many failed logins from one IP).

## A10 — Missing Error Handling

- All exceptions caught at boundary (middleware, controller), never leak to client.
- Fail-secure: errors must not bypass security controls.
- Validate error paths maintain authorization.

## Secrets Management

- `appsettings.json` contains placeholder values.
- Override via environment variables or `appsettings.{Environment}.json`.
- `settings.local.json` and `.env` files are in `.gitignore`.
