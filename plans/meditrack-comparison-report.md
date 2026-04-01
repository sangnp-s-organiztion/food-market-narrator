# MediTrack vs Food Market Narrator — Comparison Report

**Date:** 2026-03-27
**Source:** [github.com/nkmnhan/meditrack](https://github.com/nkmnhan/meditrack)

---

## Project Background

- **MediTrack** — Open-source EMR platform with AI clinical companion "Clara." .NET 10 microservices, React 19, PostgreSQL + pgvector, MCP-based AI, SignalR, RabbitMQ, .NET Aspire.
- **Food Market Narrator** — Location-based mobile app that auto-narrates restaurant info in Vinh Khanh Food Street. .NET MAUI (Android) + ASP.NET Web API + SQL Server + React dashboard.

---

## Feature / Component Comparison

| Category | MediTrack | Food Market Narrator |
|---|---|---|
| **Architecture** | 6 microservices (CQRS, DDD) | 2 services (API + MAUI) |
| **Backend** | ASP.NET Core 10, MediatR, RabbitMQ | ASP.NET Core Web API |
| **Frontend** | React 19 + Vite + TypeScript | React + TypeScript (saler/admin) |
| **Mobile** | None | .NET MAUI (Android) |
| **Database** | PostgreSQL 17 + pgvector | SQL Server |
| **Auth** | Duende IdentityServer (OIDC + SMART on FHIR) | Cookie Auth |
| **AI** | Clara (MCP, Deepgram, pgvector RAG) | None |
| **Real-time** | SignalR Hub | None |
| **Observability** | OpenTelemetry + Jaeger + Prometheus | None |
| **Orchestration** | .NET Aspire (Aspire.Nexus) | None |
| **Messaging** | RabbitMQ (event-driven) | None |
| **Theming** | 10 perceptual color themes | Basic CSS |
| **i18n** | Unknown | Language endpoint (2 languages) |
| **Offline** | Unknown | POI + audio cache |
| **Testing** | 106+ unit tests, TDD mandatory | Minimal |
| **Docs** | 14+ docs (arch, HIPAA, roadmaps) | Basic CLAUDE.md + 3 rule files |
| **Docker** | Full docker-compose + Aspire | None |
| **FHIR** | FHIR R4 models (DDD) | None |
| **Compliance** | HIPAA audit trail documented | None |

---

## MediTrack — Pros (What to Learn From)

### 1. TDD Mandatory
> "TDD is not optional." Every new feature, bug fix, or refactor requires tests first.
> **Your project:** No tests at all. Add at least unit tests for services.

### 2. Observability from Day One
> OpenTelemetry + Jaeger + Prometheus wired up. Every service emits traces, metrics, and structured logs.
> **Your project:** Zero observability. Add structured logging + health checks.

### 3. MCP-based AI Architecture
> Clara.API uses Model Context Protocol — clean separation between AI tools and business logic. Tools like `fhir_*`, `knowledge_*`, `session_*` are defined as MCP tools, making the AI pluggable.
> **Your project:** No AI. Consider MCP if you ever add AI narration suggestions.

### 4. Perceptual Color Theming System
> 100+ CSS variables per theme, semantic token migration, WCAG contrast guards. A production-grade theming system.
> **Your project:** Basic CSS. Consider adopting CSS custom properties with semantic tokens.

### 5. Comprehensive Documentation
> 14+ docs covering architecture, HIPAA compliance, roadmaps, observability, MFA design, token refresh, etc.
> **Your project:** Just CLAUDE.md + 3 rule files. Add architecture docs.

### 6. Structured Rules for AI Assistants
> `.claude/rules/` with specific guidelines for backend, frontend, mobile, and AI behavior.
> **Your project:** Has this (backend-rules, frontend-rules, mobile-rules) — good foundation, but lacks TDD and observability rules.

### 7. pgvector RAG for Knowledge Base
> Embedding search over medical knowledge using PostgreSQL vector extension.
> **Your project:** No vector DB. Fine for current scope, but note it for future AI features.

### 8. CQRS via MediatR
> Clear separation of commands and queries with pipeline behaviors (validation, logging).
> **Your project:** Controller → Service → Repository (simple but works). Consider MediatR if features grow.

### 9. Security-first Design
> HIPAA audit trail, IDOR protection, prompt injection defense, LLM response sanitization.
> **Your project:** Cookie auth + basic role checks. No audit trail for sensitive data.

---

## MediTrack — Cons / Risks

| Issue | Impact |
|---|---|
| **Educational project only** | No production hardening, no real PHI handling guarantees |
| **No CI/CD mentioned** | Manual deployments described |
| **Large scope creep** | 14 phases planned — risk of never finishing |
| **pgvector + LLM costs** | $3K–$18K/month at scale — expensive for a learning project |
| **Complex microservices** | 6 services + RabbitMQ + Aspire — heavy for a small team |
| **AI transcription (Deepgram)** | Adds cost + external dependency + privacy concerns |
| **No real HL7/FHIR integration** | Just models, no actual interoperability |

---

## What Food Market Narrator Should Learn from MediTrack

### High Priority (Do Now)

- [ ] **Add unit tests** — at minimum for `POIService`, `AudioService`, location/distance logic
- [ ] **Add structured logging** — inject `ILogger<T>` with trace IDs
- [ ] **Add health checks** — `/health` endpoint for API and MAUI monitoring
- [ ] **Improve documentation** — add `docs/architecture.md`, `docs/offline-strategy.md`
- [ ] **TDD rule** — mandate tests for services before marking tasks complete

### Medium Priority (When Feature-Growing)

- [ ] **CSS semantic tokens** — adopt a design token system for theming
- [ ] **SignalR for real-time** — for future features like "nearby POI alerts" to web dashboard
- [ ] **Event-driven architecture** — RabbitMQ when you need async notification delivery
- [ ] **MediatR for CQRS** — when controllers get too many actions, split into commands/queries

### Low Priority (Future / Optional)

- [ ] **OpenTelemetry** — when deploying to production with multiple services
- [ ] **.NET Aspire** — when service count grows beyond 3
- [ ] **MCP for AI** — if you ever add AI narration or restaurant recommendations
- [ ] **pgvector RAG** — if you add a "smart search" or AI guide feature

---

## Summary

| | MediTrack | Food Market Narrator |
|---|---|---|
| **Maturity** | Feature-rich, complex | Simple, focused |
| **Learning Curve** | Steep (6 services) | Gentle (2 services) |
| **Production Readiness** | Medium (no CI/CD, HIPAA docs only) | Low (no tests, no observability) |
| **Best for** | AI/clinical domain learning | Location/audio mobile app |

**Bottom line:** MediTrack is a great **learning reference** for architecture, TDD, observability, and AI patterns. Its complexity would be overkill for the Food Market Narrator app. **Steal the TDD mindset and observability basics** — leave the microservices + MCP for when they are needed.
