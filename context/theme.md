# Theme tokens

## Dark theme hairlines follow the light-mode scale

**Status:** active  
**Evidence:** inferred  
**Source:** issue #285 (dashboard/setup screenshots); `tokens-dark.less` previously mapped `--el-border-color-extra-light` to `--color-gray-400`  
**Revisit when:** another page still has dominating chrome after this token change, or Element Plus starts shipping a first-class dark theme we adopt

Light-mode borders are almost invisible (`--color-gray-200` / `--color-gray-100`). Dark mode had the scale inverted: “lighter” Element Plus border tokens were *brighter* grays, and `.el-card` used raw `--color-gray-200` (`#e5e7eb`) which stays light on navy. Cards and table rules therefore framed every block.

Dark `--color-border` / `--color-border-subtle` are low-opacity primary-200 so hairlines sit on navy instead of reading as white lines. `--el-border-color*` and `--el-table-border-color` point at those tokens. Light `--color-border` is still `--color-gray-200`, so the card rule change is a rename.

## Dark current-page chip is fill plus light text

**Status:** active  
**Evidence:** inferred  
**Source:** issue #285 current-menu highlight; PR review of the `#2563a8` + orange pairing  
**Revisit when:** the dark sidebar palette is retuned

`.stage-group__page.is-active` and Dashboard `.el-menu-item.is-active` both use `--color-sidebar-active` + `--color-sidebar-text-active`. The loud chip is `#2563a8`. Orange `#f47920` on that fill is about 2.2:1 (below WCAG AA 4.5:1 for 14px menu text). Active text is `#eef2f9` (primary-50 / light sidebar background) so the label stays light-on-chip, about 5.4:1, the same idea as light mode’s dark navy on `#d4dff0`.

**Rejected alternative:** keep orange and darken the fill. Orange needs a fill near the old `#14284d` to stay at ~5.4:1. That fill is the weak chip, and it is not distinct enough from `--color-sidebar-hover` (`#1c3a6a`; orange on hover is only ~4.1:1).

**Rejected alternative:** restyle only People Management. The dominating lines come from shared tokens those controls already use; page-local hex would fight the next dashboard screen.

**Rejected alternative:** keep the gray-700→gray-400 dark scale and only dim `.el-card`. Table-v2 row rules use `--el-border-color-lighter`, which would stay loud.

## Election-stage chips use fill tokens, not light-only white

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #285; `StageControl.vue` had `#fff` / `#dcdfe6` / `#606266`  
**Revisit when:** stage chips gain a selected-outline treatment that needs its own token

Unselected stage buttons are `<button class="stage-control__seg">` with CSS fills. Hardcoded white/gray made them look like light-theme leftovers on the navy sidebar. Fill and hairline are `--el-fill-color-blank` (white in light, gray-900 in dark) and `--el-border-color`. Unselected text is `--el-text-color-regular` in light; `html.dark .stage-control__seg:not(.is-selected)` sets `--color-sidebar-text` (`#a8bfe1`), not `--el-text-color-regular` (`--color-gray-300`). Selected still paints `--color-stage-*` inline.

**Rejected alternative:** a `--color-stage-chip-*` pair. The Element Plus fill/border tokens already mean “unselected control surface.”

## Names use `--color-text-link`, not `--el-color-primary`

**Status:** active  
**Evidence:** inferred  
**Source:** issue #285 names-list contrast; People table uses `el-button type="primary" link`  
**Revisit when:** primary solid buttons also need a lighter dark fill

`--el-color-primary` is `#2563a8` in both themes. That hue pops on white and goes muddy on `#0e2040` / `#111827`. Brightening `--el-color-primary` in dark would also recolor solid primary actions (Add Person). `--color-text-link` is primary-500 in light (same as today) and primary-200 in dark. `PeopleTable` name buttons set `--el-button-text-color` from that token.

**Rejected alternative:** change dark `--el-color-primary`. Out of scope for this dashboard slice and would shift every primary fill.
