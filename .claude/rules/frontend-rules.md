---
paths:
  - "admin/**"
  - "saler/**"
---

# Frontend Rules (React + TypeScript + Vite)

## Principles

- **KISS, YAGNI, DRY** — see root `CLAUDE.md`.
- TypeScript strict mode. No `any` unless truly unavoidable.
- Unidirectional data flow: props down, callbacks up. Never mutate props.
- Declarative over imperative: describe what the UI looks like for a given state.

## Project Structure

```
admin/ or saler/
├── src/
│   ├── features/
│   │   ├── restaurants/       # Restaurant CRUD
│   │   ├── dishes/           # Menu management
│   │   ├── audios/           # Narration upload
│   │   └── auth/             # Login, session
│   │       ├── components/    # Presentational only
│   │       ├── hooks/        # State, API calls
│   │       └── types.ts      # Feature-specific types
│   ├── shared/
│   │   ├── api/              # Axios/fetch wrapper, interceptors
│   │   ├── components/       # Reusable UI
│   │   └── types/            # Shared types
│   └── App.tsx
```

## API Layer

- All API calls go through `shared/api/`. Never call `fetch`/`axios` directly in components.
- Use `credentials: 'include'` for cookie-authenticated requests.
- Standardize error handling in the API layer:
  - 401 → redirect to login
  - 403 → show "Permission denied"
  - 404 → show "Not found"
  - 500 → show "Server error"
- API base URL from environment config, not hardcoded.

## State Management

- Local state (useState) for component-only state.
- Lift state when sibling or parent needs it.
- No prop-drilling more than 2 levels — use context or a simple store.

## Component Rules

- Presentational components: render UI, accept props/callbacks. No API calls.
- Container/hook pattern: fetch data, manage loading/error state.
- Feature-based folders — group by domain (restaurants, dishes, audios), not by type (all components together).
- Keep components small. Split if > 150 lines.

## Forms & Upload

- **Image/audio upload**: always show upload state (idle → uploading → success/error).
- Disable submit during upload. Re-enable on error so user can retry.
- Show file name and size before upload. Show progress if possible.
- Validate file type and size client-side before sending.

## TypeScript

- Shared types (DTO shapes) in `shared/types/`. Reuse across features.
- Define API response envelope once:
  ```typescript
  interface ApiResponse<T> { success: boolean; data: T; message: string; }
  ```
- Use `unknown` for untyped API responses, not `any`.

## Styling

- Use Tailwind CSS with semantic color tokens if defined, or sensible hex values.
- Mobile-first responsive design (base → `sm:` → `md:` → `lg:`).
- Minimum touch target: `min-h-10 min-w-10` (40px) for mobile.
- Lucide React for icons (`h-5 w-5` UI, `h-4 w-4` inline).

## Seller vs Admin

| Feature | Seller (saler/) | Admin (admin/) |
|---------|-----------------|----------------|
| Scope | Own restaurants only | All restaurants |
| User creation | No | Yes |
| Data access filter | Always filter by `user_id` | No filter |

## Quality

- Run lint before finishing changes.
- Check for existing hooks/utils before writing new ones.
- No large refactors in a single PR.
