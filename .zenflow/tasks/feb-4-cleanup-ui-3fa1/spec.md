# Technical Specification: Dashboard UI Cleanup

## Technical Context

### Language & Dependencies
- **Frontend Framework**: Vue 3 (Composition API) with TypeScript
- **UI Library**: Element Plus v2.11.5
- **Build Tool**: Vite
- **Styling**: CSS with Less preprocessor, CSS custom properties (variables)
- **State Management**: Pinia

### Affected Components
- **[./frontend/src/pages/DashboardPage.vue](./frontend/src/pages/DashboardPage.vue)**: Dashboard component (523 lines)
- **[./frontend/src/style.css](./frontend/src/style.css)**: Global theme variables and Element Plus overrides (1376 lines)
- **[./frontend/src/stores/themeStore.ts](./frontend/src/stores/themeStore.ts)**: Theme switching logic (37 lines)

### Root Cause Analysis

#### Issue 1: Light Mode Text Contrast
**Location**: `frontend/src/style.css`, lines 1-150

**Problem**: Element Plus component variables (`--el-*`) are only defined within the `.dark` class (lines 178-214). Light mode has no Element Plus variable overrides in `:root`, causing Element Plus components to use their default light gray text colors instead of our custom theme colors.

**Evidence**:
- `:root` defines custom theme variables (`--color-text-primary`, `--color-text-secondary`, etc.) at lines 87-90
- `.dark` class properly overrides Element Plus variables at lines 178-214
- No corresponding Element Plus overrides exist for light mode in `:root`
- Result: Tables, cards, and text use Element Plus defaults (~#606266 for regular text) instead of our darker grays

**Contrast Analysis**:
- Current light mode: `#606266` (gray) on `#ffffff` (white) = ~3.9:1 contrast ratio ❌ (fails WCAG AA for normal text)
- Required for WCAG AA: 4.5:1 for normal text, 3:1 for large text
- Our `--color-gray-900` (#111827) on white = ~16:1 contrast ratio ✅

#### Issue 2: Stat Card Layout Breakdown
**Location**: `frontend/src/pages/DashboardPage.vue`, lines 213-315

**Problem**: The `.stat-card` component uses nested Element Plus card body structure, but the flex layout is applied at the wrong level, causing the `.stat-content` div (containing values and labels) to not display properly.

**Evidence**:
```css
/* Line 244-251: Card body has display: flex but padding: 0 */
.stat-card .el-card__body {
  display: flex;
  align-items: center;
  width: 100%;
  padding: 0;  /* ← Removes Element Plus default padding */
  position: relative;
  z-index: 1;
}
```

**Analysis**:
- `.stat-card` itself has `display: flex` (line 214) and `padding` (line 216)
- But Element Plus wraps content in `.el-card__body` which has its own styling
- The nested structure creates conflicting flex containers
- Setting `.el-card__body` padding to 0 removes space, but content may still be hidden due to overflow/positioning issues

#### Issue 3: Icon Sizing and Card Proportions
**Location**: `frontend/src/pages/DashboardPage.vue`, lines 253-289

**Problem**: 4rem × 4rem icons may be disproportionate to card content when cards are narrow (on mobile or in 4-column layout).

**Current Sizing**:
- Desktop: 4rem × 4rem icons (line 254-255)
- Mobile (≤768px): 3.5rem × 3.5rem (line 384-386)
- Mobile (≤480px): 3rem × 3rem (line 438-441)

**Grid Layout**: Lines 207-211
```css
.stats-row {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
  gap: var(--spacing-6);
}
```

At 280px minimum width, 4rem (64px) icons take up ~23% of the card width, leaving limited space for stat content.

## Implementation Approach

### Phase 1: Add Element Plus Light Mode Overrides

**Objective**: Ensure Element Plus components inherit custom theme colors in light mode for proper text contrast.

**Approach**: Add Element Plus CSS variable overrides to `:root` selector (light mode) following the same pattern used in `.dark` selector.

**Changes to `frontend/src/style.css`**:

Add after line 150 (before `.dark` selector):

```css
/* Element Plus light mode theme variables */
--el-color-primary: var(--color-primary-500);
--el-color-primary-dark-2: var(--color-primary-600);
--el-color-primary-light-3: var(--color-primary-400);
--el-color-primary-light-5: var(--color-primary-300);
--el-color-primary-light-7: var(--color-primary-200);
--el-color-primary-light-8: var(--color-primary-100);
--el-color-primary-light-9: var(--color-primary-50);

--el-bg-color: var(--color-bg-primary);
--el-bg-color-page: var(--color-bg-secondary);
--el-bg-color-overlay: var(--color-bg-overlay);

--el-text-color-primary: var(--color-gray-900);
--el-text-color-regular: var(--color-gray-700);
--el-text-color-secondary: var(--color-gray-600);
--el-text-color-placeholder: var(--color-gray-400);
--el-text-color-disabled: var(--color-gray-500);
--el-input-text-color: var(--color-gray-900);

--el-border-color: var(--color-gray-200);
--el-border-color-light: var(--color-gray-300);
--el-border-color-lighter: var(--color-gray-200);
--el-border-color-extra-light: var(--color-gray-100);

--el-fill-color-blank: #ffffff;
--el-fill-color-light: var(--color-gray-50);
--el-fill-color-lighter: var(--color-gray-100);
--el-fill-color-extra-light: var(--color-gray-50);
--el-fill-color-dark: var(--color-gray-100);
--el-fill-color-darker: var(--color-gray-200);

--el-box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.12);
--el-box-shadow-light: 0 2px 4px rgba(0, 0, 0, 0.12), 0 0 6px rgba(0, 0, 0, 0.04);
--el-box-shadow-lighter: 0 2px 12px 0 rgba(0, 0, 0, 0.08);
--el-box-shadow-dark: 0 2px 12px 0 rgba(0, 0, 0, 0.24);
```

**Rationale**:
- Maps Element Plus text variables to our darker gray values for sufficient contrast
- `--el-text-color-primary`: #111827 (gray-900) for highest contrast on primary content
- `--el-text-color-regular`: #374151 (gray-700) for regular text content (~10.4:1 contrast)
- `--el-text-color-secondary`: #4b5563 (gray-600) for secondary text (~7.2:1 contrast)
- All values meet or exceed WCAG AA requirements

### Phase 2: Fix Stat Card Layout

**Objective**: Make stat card numbers and labels visible by correcting the flex layout structure.

**Root Cause**: Element Plus `el-card` component wraps content in `.el-card__body` div, and the current styling doesn't account for this nesting properly.

**Approach**: Simplify the stat card structure by removing the nested flex override on `.el-card__body` and ensuring content flows naturally.

**Changes to `frontend/src/pages/DashboardPage.vue`** (styles section):

**Remove** lines 244-251:
```css
.stat-card .el-card__body {
  display: flex;
  align-items: center;
  width: 100%;
  padding: 0;
  position: relative;
  z-index: 1;
}
```

**Modify** `.stat-card` selector (line 213-221) to:
```css
.stat-card {
  transition: var(--transition-normal);
  cursor: pointer;
  position: relative;
  overflow: hidden;
}

.stat-card .el-card__body {
  display: flex;
  align-items: center;
  gap: var(--spacing-4);
  padding: var(--spacing-6);
}
```

**Rationale**:
- Let Element Plus handle the card structure naturally
- Apply flex layout to `.el-card__body` where the content actually lives
- Use `gap` property instead of `margin-right` on icon for cleaner spacing
- Maintain padding at the body level rather than external card level

### Phase 3: Optimize Stat Card Proportions

**Objective**: Ensure stat cards have balanced proportions between icons, values, and labels.

**Approach**: Adjust icon sizing and ensure proper text hierarchy.

**Changes to `frontend/src/pages/DashboardPage.vue`**:

**Modify** `.stat-icon` (lines 253-263):
```css
.stat-icon {
  width: 3.5rem;
  height: 3.5rem;
  border-radius: var(--radius-lg);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  transition: var(--transition-normal);
}
```

**Modify** `.stat-icon .el-icon` (lines 285-289):
```css
.stat-icon .el-icon {
  font-size: 1.5rem;
  color: white;
  transition: var(--transition-normal);
}
```

**Rationale**:
- Reduce icon size from 4rem to 3.5rem (~20% smaller) for better proportion at 280px card width
- Reduce icon font size from 1.75rem to 1.5rem to maintain visual balance
- At 280px card width: 3.5rem (56px) = 20% of width, leaving 80% for content
- Maintains visual hierarchy while improving content space
- Mobile breakpoints already use smaller sizes, so desktop now aligns better with mobile progression

### Phase 4: Verification

**Testing Strategy**:
1. **Visual Regression Testing**: Compare screenshots before/after in both light and dark modes
2. **Contrast Testing**: Use browser DevTools or automated tools (axe DevTools) to verify WCAG AA compliance
3. **Cross-browser Testing**: Test in Chrome, Firefox, Safari, Edge
4. **Responsive Testing**: Test at 280px, 768px, 1024px, and 1440px viewport widths
5. **Theme Toggle Testing**: Rapidly switch between light/dark modes to ensure no flash of unstyled content

**Verification Checklist**:
- [ ] Light mode: All table text meets WCAG AA (4.5:1 minimum)
- [ ] Light mode: Subtitle text is clearly readable
- [ ] Light mode: Stat card numbers and labels are visible
- [ ] Dark mode: No visual regressions
- [ ] Stat cards: Balanced proportions in all viewport sizes
- [ ] Stat cards: Icon and content properly aligned side-by-side
- [ ] Theme toggle: Smooth transition, no layout shift
- [ ] All Element Plus components (table, card, tag, button) properly themed

**Build & Test Commands**:
```bash
cd frontend
npm run dev          # Development server
npm run build        # Production build
npm run test         # Run Vitest tests
npx vue-tsc --noEmit # Type checking
```

**Manual Test Scenarios**:
1. Load dashboard in light mode → verify text contrast
2. Load dashboard in dark mode → verify no regressions
3. Toggle theme while on dashboard → verify smooth transition
4. Resize browser from 1440px down to 280px → verify stat card layout
5. Check browser console for CSS warnings/errors

## Data Model / API / Interface Changes

**None required** - This is a pure UI/CSS fix with no backend or data model changes.

## Source Code Structure Changes

### Modified Files
1. **`frontend/src/style.css`**
   - Add Element Plus light mode variable overrides in `:root` (after line 150)
   - Estimated addition: ~35 lines

2. **`frontend/src/pages/DashboardPage.vue`**
   - Modify `.stat-card` structure and `.el-card__body` styling (lines 213-251)
   - Reduce `.stat-icon` size from 4rem to 3.5rem (lines 253-263)
   - Reduce `.stat-icon .el-icon` size from 1.75rem to 1.5rem (lines 285-289)
   - Estimated changes: ~15 lines modified

### No New Files Required

### No Deleted Files

## Delivery Phases

### Phase A: Light Mode Text Contrast ✓
**Deliverable**: All text in light mode meets WCAG AA standards
**Verification**: Use axe DevTools or WebAIM Contrast Checker
**Effort**: 30 minutes

### Phase B: Stat Card Layout Fix ✓
**Deliverable**: Stat card numbers and labels visible in both themes
**Verification**: Visual inspection of all 4 stat cards
**Effort**: 45 minutes

### Phase C: Stat Card Proportions ✓
**Deliverable**: Balanced icon-to-content ratio across all viewport sizes
**Verification**: Responsive testing at key breakpoints
**Effort**: 30 minutes

### Phase D: Cross-theme Testing ✓
**Deliverable**: Both themes work correctly with no regressions
**Verification**: Full manual test suite execution
**Effort**: 30 minutes

**Total Estimated Effort**: ~2.5 hours

## Verification Approach

### Automated Testing
```bash
# Type checking
cd frontend
npx vue-tsc --noEmit

# Unit tests (if any theme-related tests exist)
npm run test

# Build verification
npm run build
```

### Manual Testing
1. **Contrast Testing**:
   - Use browser DevTools → Inspect element → Check computed color values
   - Use axe DevTools extension → Run accessibility scan
   - Verify WCAG AA compliance for all text elements

2. **Visual Testing**:
   - Light mode: Check subtitle, table text, stat cards, buttons, tags
   - Dark mode: Verify no regressions from current state
   - Theme toggle: No flash of unstyled content or layout shift

3. **Responsive Testing**:
   - Use Chrome DevTools device toolbar
   - Test at: 280px, 375px, 768px, 1024px, 1440px widths
   - Verify stat card grid and icon sizing at each breakpoint

### Acceptance Criteria
- [ ] **Light mode text**: All text achieves minimum 4.5:1 contrast ratio (WCAG AA)
- [ ] **Dark mode**: No visual regressions from current implementation
- [ ] **Stat cards**: Numbers and labels visible in both themes
- [ ] **Stat cards**: Icons proportional to card size at all breakpoints
- [ ] **Element Plus components**: Table, card, tag, button properly themed in both modes
- [ ] **Theme toggle**: Smooth transition without visual breaks
- [ ] **No TypeScript errors**: `npx vue-tsc --noEmit` passes
- [ ] **Build succeeds**: `npm run build` completes without errors

## Implementation Notes

### Consistency with Existing Patterns
- Element Plus variable overrides follow the exact same structure as dark mode (lines 178-214)
- Stat card sizing uses existing responsive breakpoints (768px, 480px)
- All spacing uses existing CSS custom properties (--spacing-*)
- Color values reference existing color palette variables

### Potential Risks & Mitigation

**Risk 1**: Element Plus overrides might affect other pages
- **Mitigation**: All changes are global theme improvements that should enhance consistency across all pages
- **Validation**: Spot-check other pages (Elections list, People list, Settings) after changes

**Risk 2**: Icon size reduction might make them too small
- **Mitigation**: 3.5rem is still 56px, well above minimum touch target size (44px)
- **Validation**: Test on actual mobile devices, not just browser DevTools

**Risk 3**: Stat card layout changes might break on some browsers
- **Mitigation**: Flexbox with gap is well-supported (Chrome 84+, Firefox 63+, Safari 14.1+)
- **Validation**: Test on target browsers during verification phase

### Dependencies
- No new npm packages required
- No backend changes required
- No database migrations required
- Compatible with existing Element Plus version (2.11.5)

## References

### Internal Documentation
- [Product Requirements Document](./requirements.md)
- [Implementation Plan](./plan.md)

### External Resources
- [Element Plus Theme Customization](https://element-plus.org/en-US/guide/theming.html)
- [WCAG 2.1 Contrast Guidelines](https://www.w3.org/WAI/WCAG21/Understanding/contrast-minimum.html)
- [CSS Custom Properties (MDN)](https://developer.mozilla.org/en-US/docs/Web/CSS/Using_CSS_custom_properties)

### Related Code
- [./frontend/src/pages/DashboardPage.vue](./frontend/src/pages/DashboardPage.vue) - Dashboard component
- [./frontend/src/style.css](./frontend/src/style.css) - Global theme variables
- [./frontend/src/stores/themeStore.ts](./frontend/src/stores/themeStore.ts) - Theme switching logic
