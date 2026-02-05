# Product Requirements Document: Dashboard UI Cleanup

## Overview
Fix critical visual issues in the TallyJ 4 dashboard affecting both light and dark mode usability, with primary focus on text contrast and component sizing problems.

## Problem Statement

### Current Issues
Based on analysis of the dashboard in both light and dark modes, the following critical issues need resolution:

#### 1. **Light Mode Text Contrast Issues**
- **Subtitle text**: "Modern election management and ballot tallying system" appears very faint/washed out on white background
- **Table content**: Election names, dates, and other table data have insufficient contrast against the white background
- **Root cause**: Element Plus component library is not properly inheriting custom theme variables in light mode, resulting in default light gray text that doesn't meet WCAG AA contrast requirements

#### 2. **Stat Card Layout Issues**
- **Icon sizing**: The gradient-colored stat card icons appear disproportionately sized relative to card dimensions
- **Card proportions**: Overall stat card layout needs refinement for better visual balance
- **Spacing**: Internal spacing within cards could be optimized for better readability

#### 3. **Missing Element Plus Theme Integration**
- **Dark mode**: Has explicit Element Plus variable overrides (working correctly)
- **Light mode**: Lacks corresponding Element Plus variable overrides, causing the library to use defaults
- **Result**: Inconsistent theming between light and dark modes

## Success Criteria

### Text Contrast
- [ ] All text in light mode meets WCAG AA contrast ratio (4.5:1 for normal text, 3:1 for large text)
- [ ] Subtitle text is clearly readable on white background
- [ ] Table text (election names, dates, types) is clearly readable
- [ ] Status tags maintain good contrast while preserving color-coding

### Visual Hierarchy
- [ ] Primary content (headings, election names) uses appropriately bold/dark text colors
- [ ] Secondary content (dates, labels) uses distinguishable but less prominent colors
- [ ] Tertiary content (helper text, metadata) uses lightest acceptable contrast

### Component Sizing
- [ ] Stat cards have balanced proportions between icon size, value size, and card dimensions
- [ ] Icons are appropriately scaled and centered within their gradient backgrounds
- [ ] Text within stat cards is properly sized for hierarchy (large values, smaller labels)

### Cross-Theme Consistency
- [ ] Both light and dark modes use consistent color semantic mapping
- [ ] Theme switching maintains visual hierarchy and readability
- [ ] Element Plus components properly inherit theme variables in both modes

## User Impact

### Who is Affected
- **All users**: Dashboard is the landing page for authenticated users
- **Accessibility users**: Low vision users particularly affected by poor contrast
- **Theme preference users**: Users preferring light mode have degraded experience

### Usage Context
- Dashboard is viewed on every login
- Contains critical election status information
- Used for quick navigation to elections
- Statistics provide at-a-glance system overview

## Technical Context

### Current Implementation
- **Frontend**: Vue 3 with Element Plus UI library
- **Styling**: CSS custom properties (CSS variables) for theming
- **Theme switching**: Handled by `themeStore.ts` which adds/removes `.dark` class

### Affected Files
- `frontend/src/style.css`: Global theme variables and Element Plus overrides
- `frontend/src/pages/DashboardPage.vue`: Dashboard component and styles
- Potentially other components using Element Plus table, card, tag components

### Color System
Current color variables (from `style.css`):
```css
/* Light mode (default) */
--color-text-primary: var(--color-gray-900);   /* #111827 - darkest */
--color-text-secondary: var(--color-gray-600); /* #4b5563 - medium */
--color-text-tertiary: var(--color-gray-500);  /* #6b7280 - lighter */

/* Dark mode */
--color-text-primary: var(--color-gray-50);    /* #f9fafb - lightest */
--color-text-secondary: var(--color-gray-300); /* #d1d5db - medium */
--color-text-tertiary: var(--color-gray-400);  /* #9ca3af - lighter */
```

Element Plus is currently only properly themed for dark mode (lines 178-214 in `style.css`).

## Scope

### In Scope
1. Fix text contrast in light mode by adding Element Plus variable overrides
2. Optimize stat card sizing and proportions
3. Ensure table text uses appropriate contrast colors
4. Verify subtitle and heading readability
5. Test both light and dark modes for consistency

### Out of Scope
- Complete redesign of dashboard layout
- Changes to dark mode (already working correctly)
- Modifications to other pages beyond dashboard
- Addition of new features or functionality
- Changes to color palette values themselves

## Design Decisions

### Text Color Mapping
For **light mode**, Element Plus variables should map to:
- `--el-text-color-primary`: Use `--color-gray-900` (darkest) for high contrast
- `--el-text-color-regular`: Use `--color-gray-700` or `--color-gray-800` (darker than current)
- `--el-text-color-secondary`: Use `--color-gray-600` for supporting text

### Stat Card Proportions (Proposed)
Based on analysis of current 4rem × 4rem icons:
- Consider reducing to 3.5rem × 3.5rem for better proportion
- Or increase card padding to balance the 4rem icons
- Ensure consistent spacing between icon and content
- Verify value text size creates clear hierarchy over label text

### Element Plus Component Priority
Focus fixes on these Element Plus components used in dashboard:
1. **el-table**: Election list (highest priority - most content)
2. **el-card**: Welcome card, recent elections card, stat cards
3. **el-tag**: Status badges (Tallying, Finalized, etc.)
4. **el-button**: Action buttons (Create Election, View)

## Acceptance Testing

### Test Scenarios
1. **Light Mode Dashboard Load**
   - Verify subtitle is clearly readable
   - Verify all table text meets contrast requirements
   - Check stat card visual balance

2. **Dark Mode Dashboard Load** (regression test)
   - Verify no visual regressions
   - Confirm existing contrast is maintained

3. **Theme Toggle**
   - Switch from light to dark and back
   - Verify smooth transition without visual breaks
   - Confirm both modes maintain readability

4. **Accessibility Check**
   - Run automated contrast checker (e.g., axe DevTools)
   - Verify WCAG AA compliance for all text
   - Test with browser zoom at 150% and 200%

## Implementation Notes

### Element Plus Variable Strategy
Element Plus uses its own CSS variable system. The fix requires:
1. Adding light mode overrides in `:root` (similar to existing dark mode overrides in `.dark`)
2. Mapping Element Plus text color variables to our theme's appropriate gray values
3. Ensuring background variables work with the new text colors

### Potential Risks
- Changes to Element Plus variables might affect other pages using the same components
- Need to verify text colors work on different background colors (cards, tables, etc.)
- Over-darkening text could create too harsh an appearance

### Mitigation
- Test changes across multiple pages/components
- Use semantic color variables for easy adjustment
- Reference dark mode overrides as a template for consistency

## References

### Related Files
- [./frontend/src/pages/DashboardPage.vue](./frontend/src/pages/DashboardPage.vue) - Dashboard component
- [./frontend/src/style.css](./frontend/src/style.css) - Global theme variables
- [./frontend/src/stores/themeStore.ts](./frontend/src/stores/themeStore.ts) - Theme switching logic

### Design Resources
- Current light mode screenshot (see task description)
- WCAG 2.1 Contrast Guidelines: https://www.w3.org/WAI/WCAG21/Understanding/contrast-minimum.html
- Element Plus theme customization: https://element-plus.org/en-US/guide/theming.html

### Similar Patterns in Codebase
The dark mode implementation (`.dark` class in `style.css` lines 153-215) serves as a reference for the light mode fixes needed.
