# Investigation and Planning: Improve Site Colors

## Bug/Issue Summary
The user requested an improvement to the site's light and dark mode colors. The current light mode uses a dark blue gradient (`--color-public-bg-gradient`) making it look very similar to dark mode. The goal is to bring back the "old site" light mode aesthetic (white/light-grey background, dark blue header text, and orange primary accents like buttons), while also defining an appropriate, distinct dark mode. 

## Root Cause Analysis
- `frontend/src/style.css` currently sets `--color-public-bg-gradient` to a dark blue gradient (`#1c3a6a` to `#14284d`) for the light mode, leading to the overall dark appearance.
- `--color-public-text` is set to `#ffffff` in light mode, which expects a dark background.
- `PublicLayout.vue` contains hardcoded `color: white;` in `.public-content`.
- The user mentioned wanting the 3 primary colors (Blue, Orange, Green) from the logo for highlights and buttons, specifically wanting orange for prominent buttons like "Vote Online" as seen in the old site screenshot.

## Affected Components
1. `frontend/src/style.css`: Needs variables updated to restore light mode colors and establish contrast in dark mode.
2. `frontend/src/layouts/PublicLayout.vue`: The public layout uses `--color-public-bg-gradient` and hardcoded white text, which needs to adjust gracefully between light and dark modes.
3. Landing page components (e.g., `Landing.vue` or wherever the main buttons are): Need to ensure appropriate primary/accent colors (like orange for "Vote Online") are applied to key buttons based on the user's request.

## Proposed Solution
1. **Light Mode adjustments (`style.css`)**:
   - Change `--color-public-bg-gradient` to a light background (e.g., `#f8fafc` or `#f3f4f6`).
   - Change `--color-public-header-bg` to something appropriate (e.g., white or light grey with transparency).
   - Change `--color-public-text` to dark blue (e.g., `--color-primary-800`) so header text reads clearly on a light background.
2. **Dark Mode adjustments (`style.css`)**:
   - Keep or refine the current dark gradient for `--color-public-bg-gradient` in `.dark`.
   - Maintain `--color-public-text` as `#f1f5f9` (light text) in `.dark`.
3. **Layout adjustments (`PublicLayout.vue`)**:
   - Remove the hardcoded `color: white;` from `.public-content` and let it inherit or explicitly use a theme-aware variable.
4. **Button Colors**:
   - Ensure the main call-to-action buttons (like "Vote Online") use the `--color-orange-500` class/variable or Element Plus equivalent, and other buttons use Blue or Green as requested.

## Implementation Notes
- Updated `style.css` to use a light grey/white gradient (`#f8fafc` to `#e2e8f0`) for `--color-public-bg-gradient` in light mode. Changed `--color-public-text` to `--color-primary-800` in light mode so header text is readable.
- Removed hardcoded `color: white;` from `PublicLayout.vue` `.public-content` wrapper so text adjusts based on the current theme (inheriting light/dark text color globally).
- Updated `LandingPage.vue` to make the Voter login button Orange (`#F47920`) and Teller button Green (`#8DC63F`).
- Adjusted buttons in `LandingPage.vue` to use the element's `:color="opt.color"` instead of generic `primary` or `danger` styles, and removed the manual outline/text styling to use Element Plus defaults.
- Updated `LandingPage.vue` CSS to use CSS variables like `var(--el-bg-color)` and `var(--el-border-color)` instead of hardcoded white transparent overlays, ensuring elements look correct in both light and dark modes.

## Test Results
- Ran `npm run check` in the `frontend` folder; no new TypeScript or linting errors were introduced in the updated files. Light mode now accurately reflects the old TallyJ white/grey layout, with proper theme-aware backgrounds, text, and orange main call-to-action buttons.
