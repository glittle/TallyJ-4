# Full SDD workflow

## Configuration
- **Artifacts Path**: {@artifacts_path} → `.zenflow/tasks/{task_id}`

---

## Workflow Steps

### [x] Step: Requirements
<!-- chat-id: 058c3498-c5f7-4e4e-8971-1dd62f672efe -->

Create a Product Requirements Document (PRD) based on the feature description.

1. Review existing codebase to understand current architecture and patterns
2. Analyze the feature definition and identify unclear aspects
3. Ask the user for clarifications on aspects that significantly impact scope or user experience
4. Make reasonable decisions for minor details based on context and conventions
5. If user can't clarify, make a decision, state the assumption, and continue

Save the PRD to `{@artifacts_path}/requirements.md`.

### [x] Step: Technical Specification
<!-- chat-id: 16d30dc4-da0a-4137-bdf7-440e78ac328d -->

Create a technical specification based on the PRD in `{@artifacts_path}/requirements.md`.

1. Review existing codebase architecture and identify reusable components
2. Define the implementation approach

Save to `{@artifacts_path}/spec.md` with:
- Technical context (language, dependencies)
- Implementation approach referencing existing code patterns
- Source code structure changes
- Data model / API / interface changes
- Delivery phases (incremental, testable milestones)
- Verification approach using project lint/test commands

### [x] Step: Planning
<!-- chat-id: 11fa28a9-00e0-478b-86d2-63511d36b412 -->

Create a detailed implementation plan based on `{@artifacts_path}/spec.md`.

1. Break down the work into concrete tasks
2. Each task should reference relevant contracts and include verification steps
3. Replace the Implementation step below with the planned tasks

Rule of thumb for step size: each step should represent a coherent unit of work (e.g., implement a component, add an API endpoint). Avoid steps that are too granular (single function) or too broad (entire feature).

Important: unit tests must be part of each implementation task, not separate tasks. Each task should implement the code and its tests together, if relevant.

If the feature is trivial and doesn't warrant full specification, update this workflow to remove unnecessary steps and explain the reasoning to the user.

Save to `{@artifacts_path}/plan.md`.

### [x] Step: Add Element Plus Light Mode Overrides
<!-- chat-id: 62fda063-e6bf-4a68-9576-98b5de65654f -->

**Objective**: Fix text contrast issues in light mode by adding Element Plus CSS variable overrides.

**Files to Modify**:
- [./frontend/src/style.css](./frontend/src/style.css)

**Changes**:
1. Locate the `:root` selector (around line 1-150)
2. Add Element Plus variable overrides before the `.dark` selector (after line 150)
3. Map Element Plus text color variables to our darker gray values for WCAG AA compliance:
   - `--el-text-color-primary`: `var(--color-gray-900)` for primary content
   - `--el-text-color-regular`: `var(--color-gray-700)` for regular text
   - `--el-text-color-secondary`: `var(--color-gray-600)` for secondary text
4. Include background, border, fill, and shadow variables following dark mode pattern

**Expected Result**:
- Light mode text should use darker colors with sufficient contrast (≥4.5:1 ratio)
- All Element Plus components (tables, cards, tags) properly themed
- Subtitle and table text clearly readable

**Verification Steps**:
- [ ] Start dev server: `cd frontend && npm run dev`
- [ ] Load dashboard in light mode
- [ ] Verify subtitle "Modern election management..." is clearly readable
- [ ] Verify table text (election names, dates) has good contrast
- [ ] Check browser console for CSS warnings/errors
- [ ] Use browser DevTools to inspect computed color values

### [x] Step: Fix Stat Card Layout Structure
<!-- chat-id: a346bb55-84fd-4174-bdd0-0aa67e6ccfc5 -->

**Objective**: Make stat card numbers and labels visible by correcting the flex layout structure.

**Files to Modify**:
- [./frontend/src/pages/DashboardPage.vue](./frontend/src/pages/DashboardPage.vue)

**Changes**:
1. Remove the `.stat-card .el-card__body` override (lines 244-251) that sets `padding: 0` and conflicting flex properties
2. Modify `.stat-card` selector (lines 213-221) to simplify structure
3. Add proper flex layout to `.el-card__body` with:
   - `display: flex`
   - `align-items: center`
   - `gap: var(--spacing-4)` for spacing between icon and content
   - `padding: var(--spacing-6)` for internal padding

**Expected Result**:
- Stat card numbers (e.g., "12", "156") visible in both themes
- Stat card labels (e.g., "Active Elections", "Total People") visible in both themes
- Icon and content displayed side-by-side
- Proper spacing between icon and text content

**Verification Steps**:
- [ ] Reload dashboard in browser (dev server should hot-reload)
- [ ] Verify all 4 stat cards show: icon + number + label
- [ ] Check both light and dark modes
- [ ] Verify icon is on the left, content (number + label) on the right
- [ ] Check that content is properly aligned and not overflowing

### [x] Step: Optimize Stat Card Icon Sizing
<!-- chat-id: d41afbd6-532d-4cca-9192-7ee2c4b33a13 -->

**Objective**: Improve stat card proportions by reducing icon size for better balance.

**Files to Modify**:
- [./frontend/src/pages/DashboardPage.vue](./frontend/src/pages/DashboardPage.vue)

**Changes**:
1. Modify `.stat-icon` (lines 253-263):
   - Reduce `width` and `height` from `4rem` to `3.5rem`
2. Modify `.stat-icon .el-icon` (lines 285-289):
   - Reduce `font-size` from `1.75rem` to `1.5rem`

**Rationale**:
- At 280px minimum card width, 3.5rem (56px) icons = 20% of width vs 23% with 4rem
- Leaves more space for stat numbers and labels
- Better visual balance between icon and content
- Aligns better with existing mobile breakpoint sizes

**Expected Result**:
- Icons are proportional to card size
- Stat cards have balanced layout at all viewport sizes
- Icons remain above minimum touch target size (44px)

**Verification Steps**:
- [ ] Reload dashboard
- [ ] Check stat cards at desktop size (1440px)
- [ ] Use browser DevTools responsive mode to test at:
   - 1024px (tablet landscape)
   - 768px (tablet portrait)
   - 375px (mobile)
   - 280px (minimum grid width)
- [ ] Verify icons are appropriately sized at each breakpoint
- [ ] Confirm content has adequate space and doesn't overflow

### [ ] Step: Final Verification and Testing

**Objective**: Comprehensive testing across themes, browsers, and viewport sizes to ensure all fixes work correctly.

**Automated Checks**:
```bash
cd frontend
npx vue-tsc --noEmit  # Type checking
npm run build         # Production build verification
```

**Manual Testing Checklist**:
- [ ] **Light Mode Dashboard**:
  - [ ] Subtitle clearly readable
  - [ ] All table text meets WCAG AA contrast (4.5:1 minimum)
  - [ ] Stat card numbers and labels visible
  - [ ] Stat cards have balanced proportions
  - [ ] All Element Plus components properly themed
  
- [ ] **Dark Mode Dashboard** (regression test):
  - [ ] No visual regressions from current state
  - [ ] Stat card numbers and labels visible
  - [ ] Text contrast still excellent
  
- [ ] **Theme Toggle**:
  - [ ] Switch between light and dark modes smoothly
  - [ ] No flash of unstyled content
  - [ ] No layout shift during transition
  - [ ] Stat cards remain properly rendered
  
- [ ] **Responsive Testing** (use Chrome DevTools):
  - [ ] 1440px: Desktop layout works correctly
  - [ ] 1024px: Tablet landscape, cards resize appropriately
  - [ ] 768px: Tablet portrait, mobile breakpoint kicks in
  - [ ] 375px: Mobile, stat cards stack or shrink properly
  - [ ] 280px: Minimum grid width, content doesn't overflow
  
- [ ] **Accessibility Testing**:
  - [ ] Use axe DevTools extension to scan for contrast issues
  - [ ] Verify WCAG AA compliance for all text
  - [ ] Test browser zoom at 150% and 200%
  
- [ ] **Cross-browser Spot Check** (if possible):
  - [ ] Chrome/Edge
  - [ ] Firefox
  - [ ] Safari (if on Mac)

**Verification Results**:
Record any issues found and resolution status here.

**Acceptance Criteria**:
- [ ] No TypeScript errors
- [ ] Production build succeeds
- [ ] All manual test scenarios pass
- [ ] Light mode text contrast meets WCAG AA
- [ ] Stat cards display correctly in both themes
- [ ] No regressions in dark mode
- [ ] Responsive layout works at all breakpoints
