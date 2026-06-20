# Frontend developer guide

This package contains the TallyJ 4 Vue 3 single-page application.

## Stack

- Vue 3 with Composition API
- TypeScript
- Vite
- Pinia
- Vue Router
- Element Plus
- Vue I18n
- SignalR client

## Getting started

```bash
cd frontend
npm install
npm run dev
```

> **Note on first install**: You may see a warning about `esbuild` having an install script.
> This is expected (Vite uses esbuild). Run this command once to approve it:
> ```bash
> npm approve-scripts esbuild
> ```
> Then re-run `npm install`. The warning should not appear again on this machine.

Default local URL: `http://localhost:8095`

## Environment

Copy `.env.example` to `.env.development` or `.env.production` and adjust values as needed.

Minimum useful local setup:

```env
VITE_API_URL=http://localhost:5016
```

Common variables:

- `VITE_API_URL` - backend base URL
- `VITE_APP_NAME` - app title shown by the frontend
- `VITE_ENV` - environment label
- `VITE_ENABLE_ANALYTICS` - analytics toggle
- `VITE_ENABLE_ERROR_REPORTING` - error reporting toggle
- `VITE_SENTRY_DSN` - Sentry DSN when error reporting is enabled
- `VITE_TELEGRAM_BOT_USERNAME` - optional Telegram integration setting

## Scripts

| Command                 | Purpose                                    |
| ----------------------- | ------------------------------------------ |
| `npm run dev`           | Start Vite dev server                      |
| `npm run start`         | Regenerate OpenAPI client, then start Vite |
| `npm run gen`           | Regenerate the generated API client        |
| `npm run tsc`           | Run `vue-tsc --noEmit`                     |
| `npm run lint`          | Run ESLint over `src/`                     |
| `npm run check`         | Run typecheck and lint                     |
| `npm run test`          | Start Vitest in watch mode                 |
| `npm run test:run`      | Run Vitest once                            |
| `npm run test:coverage` | Run Vitest with coverage                   |
| `npm run validate:i18n` | Validate locale file consistency           |

### API client regeneration (OpenAPI / TypeScript client)

Backend changes to DTOs, controllers, or new endpoints require regenerating the generated client that lives in `src/api/gen/configService/`.

**Quick steps**:

1. Make sure the backend is running in the Development environment (it automatically writes the current OpenAPI contract into `frontend/openApi/tallyj.json` on startup).
2. `cd frontend && npm run gen` (or invoke the CLI directly with `./openApi/config.backend.ts` — the npm script currently has a path limitation; see `AGENTS.md` "Regenerating the OpenAPI / TypeScript client" for the exact workaround and full verification steps).
3. Update any thin wrapper logic, date converters, or custom response mappings in the corresponding `src/services/*Service.ts` file(s) and the Pinia stores that consume them.

Full details, common pitfalls, and verification guidance are in `AGENTS.md`. Always treat the running Swagger UI as the source of truth for the live contract.

## Project structure

- `src/pages/` - route-level pages
- `src/components/` - reusable UI components
- `src/composables/` - reusable Vue composition functions
- `src/stores/` - Pinia state containers
- `src/services/` - service wrappers around API calls
- `src/api/gen/configService/` - generated API client
- `src/locales/` - shared translation files used by frontend and backend

## Composables

### `useViewportTableHeight` — viewport-filling `el-table` height

**File:** `src/composables/useViewportTableHeight.ts`

Use this when a page has an Element Plus table (or similar fixed-height scroll region) that should fill the remaining visible area inside the main layout **without** introducing a page-level scrollbar.

The composable measures from an **anchor** element (top of the table wrapper) down to the bottom of a layout **container** (default `#main-content`, i.e. the scrollport below the 60px app header). It subtracts:

- footer element(s) below the table (hints, toolbars, pagination bars, etc.)
- `padding-bottom` on ancestors between the padding root and the container
- an optional `bottomMargin` safety gap

It re-measures on window resize, `ResizeObserver` layout changes, and when observed refs attach. Call `remeasure()` after async layout shifts (e.g. alerts or filter panels appearing).

**Reference implementation:** `src/pages/frontdesk/FrontDeskPage.vue`

#### Typical DOM shape

```html
<div ref="sectionRef" class="my-list-section">
  <div ref="tableWrapperRef" class="table-wrapper">
    <el-table :height="tableHeight" ... />
  </div>
  <div ref="footerRef" class="list-footer">...</div>
</div>
```

#### Script usage

```typescript
import { useViewportTableHeight } from "@/composables/useViewportTableHeight";

const tableWrapperRef = ref<HTMLElement | null>(null);
const sectionRef = ref<HTMLElement | null>(null);
const footerRef = ref<HTMLElement | null>(null);

const { height: tableHeight, remeasure } = useViewportTableHeight(
  tableWrapperRef,
  {
    paddingRootRef: sectionRef, // wrapper around table + footer siblings
    bottomRef: footerRef,         // content directly below the table
    min: 200,
  },
);

// Bind to el-table
// :height="tableHeight"

// After layout-affecting changes (optional)
watch(someLayoutFlag, () => nextTick(remeasure));
```

If the table component exposes `doLayout()`, re-run it when `tableHeight` changes (see Front Desk page).

#### Options

| Option | Default | Purpose |
| ------ | ------- | ------- |
| `anchorRef` | (required) | Top of the scroll region; usually the element wrapping `el-table` |
| `paddingRootRef` | anchor parent | Element wrapping anchor + footer siblings; ancestor padding is subtracted from here up to the container |
| `bottomRef` | — | Single footer element below the anchor |
| `bottomRefs` | — | Multiple footer elements below the anchor |
| `containerRef` / `containerSelector` | `#main-content` | Scrollport whose bottom edge caps available height |
| `observeSelectors` | `[".main-layout .el-header"]` | Extra nodes to watch for layout changes |
| `min` | `200` | Minimum table height in pixels |
| `bottomMargin` | `8` | Extra pixels left below footer content |

#### When to use

- Long data tables on election-scoped pages inside `MainLayout`
- Any page where a fixed `height="600"` (or similar) causes overflow or wasted space

#### When not to use

- Short tables that should grow with content and let the page scroll normally
- Tables inside dialogs, drawers, or nested scroll areas — pass a closer `containerRef` / `containerSelector` if needed
- Public layout pages without `#main-content` — set `containerRef` or `containerSelector` explicitly

#### Related composables

- `useLocalStorage` (`src/composables/useLocalStorage.ts`) — minimal `useStorage` replacement for persisting UI prefs (e.g. Front Desk registration filter)

## Conventions

### Vue file order

Vue single-file components should use this order:

1. `<script setup lang="ts">`
2. `<template>`
3. `<style lang="less">`

### Styling

- Do not use scoped `<style>` in this repo
- Nest styles under the component root selector
- Prefer existing tokens and patterns over one-off styling

### State and services

The standard data flow is:

`component -> Pinia store -> service -> generated API client -> backend API`

### Localization

- All user-facing strings should go through `$t()` (or `useI18n().t()`).
- When adding **new** user-facing strings, **only add them to the English locale** (`src/locales/en/`). Other languages are updated separately in periodic review cycles — do not add placeholder or machine-translated strings to non-English files.
- Run `npm run validate:i18n` whenever you touch locale files.
- **Never edit anything under `src/locales/bundled/`** — those files are auto-generated by `merge-locales.js` (run automatically on `npm run build`).
- The authoritative current policy (including rationale) lives in `AGENTS.md` under the "Locales" section. The rules there take precedence.

## Validation workflow

For frontend changes, the expected validation commands are:

```bash
npm run check
npm run test:run
```

Use `npm run build` only when you explicitly need a production build artifact or production-build verification.
