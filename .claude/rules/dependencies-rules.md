---
paths:
  - "**/*.csproj"
  - "**/package.json"
  - "Directory.Packages.props"
  - "FoodMarketNarrator.Api/**/*.csproj"
  - "FoodMarketNarrator.Maui/**/*.csproj"
  - "admin/package.json"
  - "saler/package.json"
---

# Dependencies Rules

## Before Adding Any Package

1. **License check first** — prefer MIT, Apache 2.0, BSD, ISC, MPL 2.0.
2. **Flag for review if commercial** — stop and ask before adding.
3. **Never silently add commercially-licensed packages.**

## Evaluation Criteria

- **Actively maintained?** Last commit < 6 months. Abandoned packages are security risks.
- **Duplication check** — does it overlap with an existing dependency?
- **Simplicity** — is it the simplest tool for the job? (KISS/YAGNI)
- **Stable version** — no `-preview`, `-alpha`, `-beta`, `-rc`, `-nightly` in production.
- **Size** — consider impact on MAUI app size and Android APK.
- **Dependencies** — review transitive dependencies for license conflicts or bloat.

## NuGet (.NET)

- Pin versions in `Directory.Packages.props` if the project uses Central Package Management.
- Run `dotnet outdated` periodically to check for updates.
- Use the `latest` tag only for patch updates. Prefer explicit `major.minor.patch`.

## npm (React)

- Use `package-lock.json` or `pnpm-lock.yaml` — never commit with only `package.json`.
- `npm audit fix` in CI or pre-commit hooks.
- Avoid `latest` in `package.json` — use exact versions or tight ranges.

## MAUI-Specific

- Any new platform feature (camera, location, Bluetooth) requires a MAUI plugin — prefer packages already compatible with MAUI.
- Avoid packages that pull in large native dependencies (impacts APK size).
- Test on a physical Android device, not just the emulator.

## Current Stack — Do Not Duplicate

| Category | In Use | Don't Add |
|----------|--------|-----------|
| HTTP client | `HttpClient` (built-in) | Don't add Refit, RestSharp, etc. |
| Mapping | AutoMapper or manual | Don't add Mapster unless justified |
| Validation | Data Annotations | Don't add FluentValidation unless complex |
| React HTTP | fetch or axios | Keep consistent with existing choice |
| MAUI audio | Built-in or known plugin | Verify before adding |
| MAUI location | `CommunityToolkit.Mvvm` or `Geolocator` | Verify plugin compatibility |
