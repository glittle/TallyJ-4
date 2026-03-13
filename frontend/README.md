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
- Axios
- SignalR client

## Getting started

```bash
cd frontend
npm install
npm run dev
```

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

| Command | Purpose |
| --- | --- |
| `npm run dev` | Start Vite dev server |
| `npm run start` | Regenerate OpenAPI client, then start Vite |
| `npm run gen` | Regenerate the generated API client |
| `npm run tsc` | Run `vue-tsc --noEmit` |
| `npm run lint` | Run ESLint over `src/` |
| `npm run check` | Run typecheck and lint |
| `npm run test` | Start Vitest in watch mode |
| `npm run test:run` | Run Vitest once |
| `npm run test:coverage` | Run Vitest with coverage |
| `npm run validate:i18n` | Validate locale file consistency |

## Project structure

- `src/pages/` - route-level pages
- `src/components/` - reusable UI components
- `src/stores/` - Pinia state containers
- `src/services/` - service wrappers around API calls
- `src/api/gen/configService/` - generated API client
- `src/locales/` - shared translation files used by frontend and backend

## Conventions

### Vue file order

Vue single-file components should use this order:

1. `<script setup lang="ts">`
2. `<template>`
3. `<style lang="less">`

### Styling

- Do not use `<style scoped>` in this repo
- Nest styles under the component root selector
- Prefer existing tokens and patterns over one-off styling

### State and services

The standard data flow is:

`component -> Pinia store -> service -> generated API client -> backend API`

### Localization

- All user-facing strings should go through `$t()`
- Update both `en` and `fr` locale files together
- Run `npm run validate:i18n` after translation changes

## Validation workflow

For frontend changes, the expected validation commands are:

```bash
npm run check
npm run test:run
```

Use `npm run build` only when you explicitly need a production build artifact or production-build verification.
