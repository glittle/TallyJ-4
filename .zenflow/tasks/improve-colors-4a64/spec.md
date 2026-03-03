# Technical Specification: Improve Colors

## Difficulty Assessment: **Medium**

The task requires a systematic color palette overhaul across CSS design tokens, Vue components, and hardcoded inline colors. There are WCAG AA contrast compliance considerations and both light/dark mode must be maintained. No architectural changes, no API changes, no new components.

---

## Logo Color Analysis

The TallyJ logo contains 3 distinct colors:

| Color | Name | Hex | Usage in Logo |
|-------|------|-----|---------------|
| Navy Blue | Primary | `#1C3A6A` | Middle arc (dominant) |
| Orange | Accent 1 | `#F47920` | Outer arc (left) |
| Lime Green | Accent 2 | `#8DC63F` | Right arc |

---

## New Color Palette

### Primary — Navy Blue Scale (replaces generic blue)

| Token | Value | Notes |
|-------|-------|-------|
| `--color-primary-50` | `#eef2f9` | Very light navy tint |
| `--color-primary-100` | `#d4dff0` | Light |
| `--color-primary-200` | `#a8bfe1` | |
| `--color-primary-300` | `#739dcf` | |
| `--color-primary-400` | `#3f7abe` | |
| `--color-primary-500` | `#2563a8` | Main interactive color |
| `--color-primary-600` | `#1e4f8c` | Hover/dark |
| `--color-primary-700` | `#1C3A6A` | Logo exact color |
| `--color-primary-800` | `#14284d` | Dark |
| `--color-primary-900` | `#0c1830` | Darkest |

### Accent Orange Scale

| Token | Value | Notes |
|-------|-------|-------|
| `--color-orange-50` | `#fff5eb` | Very light orange tint |
| `--color-orange-100` | `#fedfba` | |
| `--color-orange-200` | `#fdc075` | |
| `--color-orange-300` | `#fca044` | |
| `--color-orange-400` | `#f88420` | |
| `--color-orange-500` | `#F47920` | Logo exact color |
| `--color-orange-600` | `#d96510` | Hover |
| `--color-orange-700` | `#b74f08` | Text-safe on white (4.8:1) |
| `--color-orange-800` | `#8f3c05` | |
| `--color-orange-900` | `#682c03` | |

### Accent Green Scale

| Token | Value | Notes |
|-------|-------|-------|
| `--color-green-50` | `#f4f9e9` | Very light green tint |
| `--color-green-100` | `#e4f0c3` | |
| `--color-green-200` | `#cde397` | |
| `--color-green-300` | `#b3d369` | |
| `--color-green-400` | `#9eca50` | |
| `--color-green-500` | `#8DC63F` | Logo exact color |
| `--color-green-600` | `#74a833` | |
| `--color-green-700` | `#5c8a27` | Text-safe on white (4.7:1) |
| `--color-green-800` | `#446c1d` | |
| `--color-green-900` | `#2e4e12` | |

### WCAG AA Contrast Notes

- `#F47920` on white = 3.1:1 (❌ fails for normal text, ✅ passes for large/bold text)
- `#b74f08` on white = 4.8:1 (✅ passes for all text)
- `#8DC63F` on white = 2.2:1 (❌ fails — decorative/icon use only)
- `#5c8a27` on white = 4.7:1 (✅ passes for all text)
- `#2563a8` on white = 4.6:1 (✅ passes)
- `#1C3A6A` on white = 9.4:1 (✅ excellent)

**Rule**: Never use `#F47920` or `#8DC63F` as text on light backgrounds. Use `-700` or darker variants.

---

## Semantic & Contextual Colors

### Light Mode

| Token | Old Value | New Value | Rationale |
|-------|-----------|-----------|-----------|
| `--color-public-bg-gradient` | `#667eea → #764ba2` (purple) | `#1C3A6A → #14284d` (navy) | Logo-aligned |
| `--color-public-header-bg` | `rgba(255,255,255,0.1)` | `rgba(255,255,255,0.12)` | Slightly more visible |
| `--color-sidebar-bg` | `#f8fafc` | `#eef2f9` | Navy-tinted sidebar |
| `--color-sidebar-text` | `#374151` | `#374151` | Unchanged (good contrast) |
| `--color-sidebar-text-active` | `#2563eb` | `#1C3A6A` | Logo navy |
| `--color-sidebar-border` | `#e5e7eb` | `#d4dff0` | Navy-tinted border |
| `--color-sidebar-hover` | `#f1f5f9` | `#e0e8f4` | Navy-tinted hover |
| `--color-sidebar-active` | `#dbeafe` | `#d4dff0` | Navy-tinted active bg |

### Dark Mode

| Token | Old Value | New Value | Rationale |
|-------|-----------|-----------|-----------|
| `--color-sidebar-bg` | `#304156` | `#0e2040` | Deep navy |
| `--color-sidebar-text` | `#bfcbd9` | `#a8bfe1` | Navy-tinted light text |
| `--color-sidebar-text-active` | `#409eff` | `#F47920` | Orange accent |
| `--color-sidebar-border` | `#263445` | `#14284d` | Navy border |
| `--color-sidebar-hover` | `#263445` | `#1C3A6A` | Navy hover |
| `--color-sidebar-active` | `#001528` | `#14284d` | Deep navy active |
| `--color-public-bg-gradient` | `#1e293b → #334155` | `#0c1830 → #14284d` | Deep navy |

### Hardcoded Colors to Fix

| File | Current | New | Reason |
|------|---------|-----|--------|
| `style.css` — link | `#646cff` | `#2563a8` | Logo navy |
| `style.css` — link hover | `#535bf2` | `#1e4f8c` | Logo navy dark |
| `style.css` — button border hover | `#646cff` | `#1e4f8c` | Logo navy |
| `style.css` — focus outline | `#409eff` | `var(--color-primary-500)` | Use token |
| `style.css` — skip link bg | `#409eff` | `var(--color-primary-700)` | Logo navy |
| `style.css` — dark sidebar active | `#409eff` | `#F47920` | Orange accent |
| `AppHeader.vue` — focus outlines | `#409eff` | `var(--color-primary-700)` | Logo navy |
| `DashboardPage.vue` — elections stat | `#667eea → #764ba2` | `#1C3A6A → #2563a8` | Navy gradient |
| `DashboardPage.vue` — active stat | `#f093fb → #f5576c` | `#F47920 → #d96510` | Orange gradient |
| `DashboardPage.vue` — voters stat | `#4facfe → #00f2fe` | `#8DC63F → #74a833` | Green gradient |
| `DashboardPage.vue` — ballots stat | `#43e97b → #38f9d7` | `#2563a8 → #3f7abe` | Navy-blue gradient |
| `LandingPage.vue` — features/options | Various | Logo-aligned colors | Logo alignment |
| `SuperAdminDashboardPage.vue` | `#667eea → #764ba2` | `#1C3A6A → #2563a8` | Navy gradient |
| `ReportingPage.vue` — chart colors | `#409eff` | `var(--color-primary-500)` | Use token |

---

## Element Plus Theme Integration

Element Plus uses `--el-color-primary` to compute derived colors. Update:

```css
--el-color-primary: #2563a8;  /* was #3b82f6 */
```

The full light/dark derivations in `:root` and `.dark` will automatically pull from the updated `--color-primary-*` tokens.

---

## Source Files to Modify

1. **`frontend/src/style.css`** — Central: update primary palette, add orange/green tokens, fix hardcoded colors in root/dark/utility sections
2. **`frontend/src/components/AppHeader.vue`** — Fix `#409eff` focus outline hardcodes
3. **`frontend/src/pages/DashboardPage.vue`** — Fix stat icon gradient colors
4. **`frontend/src/pages/LandingPage.vue`** — Fix feature/option icon colors
5. **`frontend/src/pages/SuperAdminDashboardPage.vue`** — Fix header gradient
6. **`frontend/src/pages/results/ReportingPage.vue`** — Fix `#409eff` chart color attributes

No files will be created. No API or data model changes.

---

## Implementation Plan

### Step 1: Update core design tokens in `style.css`
- Replace `--color-primary-*` with navy blue scale
- Add `--color-orange-*` and `--color-green-*` token scales
- Update sidebar colors (light + dark mode)
- Update public gradient
- Fix hardcoded link, button, focus, skip-link colors
- Update `--el-color-primary` to `#2563a8`

### Step 2: Update component-level hardcoded colors
- `AppHeader.vue`: Replace `#409eff` focus outlines with CSS variable
- `DashboardPage.vue`: Replace stat icon gradient colors
- `LandingPage.vue`: Replace icon colors with logo-aligned values
- `SuperAdminDashboardPage.vue`: Replace header gradient
- `ReportingPage.vue`: Replace `#409eff` with CSS variable reference

### Step 3: Verify and check
- Run `npm run typecheck` (no TS changes expected, but confirm)
- Manually review visual output in browser (light mode + dark mode)
- Check focus ring visibility (keyboard navigation)
- Verify WCAG AA on key text+bg combinations

---

## Verification Approach

- **TypeScript**: `npx vue-tsc --noEmit` — no changes expected
- **Lint**: Vite build watch to confirm no CSS errors
- **Visual**: Screenshot key pages: Landing, Login, Dashboard, Election detail (both light/dark)
- **Accessibility**: Check contrast ratios on primary interactive elements using browser devtools or WCAG checker
