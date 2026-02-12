# Step Report: Frontend Super Admin Dashboard Page, Routing, and Navigation

## Summary

Implemented the Super Admin Dashboard frontend page, routing with SA guard, and conditional sidebar navigation.

## Changes Made

### New Files

| File | Description |
|------|-------------|
| `frontend/src/pages/SuperAdminDashboardPage.vue` | SA dashboard page with summary stat cards, filterable/sortable election table, pagination, and side drawer for election detail |

### Modified Files

| File | Change |
|------|--------|
| `frontend/src/router/router.ts` | Added `/super-admin` route with `requiresSuperAdmin` meta; added route guard that redirects non-SA users to `/dashboard` |
| `frontend/src/components/AppSidebar.vue` | Added conditional "Super Admin" menu item (visible only when `isSuperAdmin` is true); imported `Setting` icon and `superAdminStore` |
| `frontend/src/vite-env.d.ts` | Augmented `RouteMeta` interface with `requiresAuth`, `requiresSuperAdmin`, and `title` fields for type safety |

## Key Design Decisions

- **Route guard**: The `beforeEach` guard checks `to.meta.requiresSuperAdmin` and redirects non-SA users to `/dashboard`. The SA status check is already performed earlier in the same guard.
- **Page structure**: Follows the same stat card layout pattern as `DashboardPage.vue` but with SA-specific stats (total, open, upcoming, completed elections).
- **Election table**: Server-side sorting and pagination via the store. Row click opens a side drawer with election detail and owner info.
- **Filter bar**: Search input, status dropdown, and type dropdown with `watch` triggering automatic re-fetch.
- **Drawer**: Uses `el-drawer` with `el-descriptions` for election details and a sub-table for owners.
- **CSS**: Follows the nested LESS pattern per project conventions (no scoped styles). All styles nested inside `.sa-dashboard-page`.

## Verification

- `npx vue-tsc --noEmit` passes with exit code 0.
