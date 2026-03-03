# Spec and build

## Configuration
- **Artifacts Path**: `.zenflow/tasks/improve-colors-4a64`

---

## Agent Instructions

Ask the user questions when anything is unclear or needs their input. This includes:
- Ambiguous or incomplete requirements
- Technical decisions that affect architecture or user experience
- Trade-offs that require business context

Do not make assumptions on important decisions — get clarification first.

If you are blocked and need user clarification, mark the current step with `[!]` in plan.md before stopping.

---

## Workflow Steps

### [x] Step: Technical Specification

Analyzed logo colors (navy #1C3A6A, orange #F47920, lime green #8DC63F), mapped current palette,
identified all files with hardcoded colors not matching logo, and created a WCAG AA-aware implementation plan.
See `spec.md` for full details.

---

### [ ] Step: Update core design tokens in style.css

Update `frontend/src/style.css`:
- Replace `--color-primary-*` scale with navy blue derived from logo `#1C3A6A`
- Add `--color-orange-*` and `--color-green-*` token scales for logo accent colors
- Update sidebar CSS variables (light and dark mode)
- Update public layout gradient to navy (replace purple `#667eea`/`#764ba2`)
- Fix hardcoded link colors (`#646cff`, `#535bf2` → navy tokens)
- Fix hardcoded button border hover (`#646cff`)
- Fix hardcoded focus outline colors (`#409eff` → CSS variable)
- Fix hardcoded skip-link background (`#409eff`)
- Update `--el-color-primary` to `#2563a8` (navy, AA compliant)

---

### [ ] Step: Update component hardcoded colors

Update these files to replace off-brand hardcoded colors:
- `frontend/src/components/AppHeader.vue`: Replace `#409eff` focus outlines with `var(--color-primary-700)`
- `frontend/src/pages/DashboardPage.vue`: Replace 4 stat icon gradient colors with logo-aligned gradients (navy, orange, green, blue)
- `frontend/src/pages/LandingPage.vue`: Replace feature/option icon `color` props with logo-aligned values (`#F47920`, `#8DC63F`, `#2563a8`, etc.)
- `frontend/src/pages/SuperAdminDashboardPage.vue`: Replace purple gradient with navy gradient
- `frontend/src/pages/results/ReportingPage.vue`: Replace `#409eff` color attributes with `var(--color-primary-500)` or `#2563a8`

---

### [ ] Step: Verify changes

- Run `npx vue-tsc --noEmit` from `frontend/` directory to confirm no TypeScript errors
- Confirm no CSS syntax errors in Vite build output
- Write report to `.zenflow/tasks/improve-colors-4a64/report.md`
