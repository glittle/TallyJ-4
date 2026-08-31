# Theme tokens

## Dark theme hairlines follow the light-mode scale

**Status:** active  
**Evidence:** inferred  
**Source:** issue #285 (dashboard/setup screenshots); `tokens-dark.less` previously mapped `--el-border-color-extra-light` to `--color-gray-400`  
**Revisit when:** another page still has dominating chrome after this token change, or Element Plus starts shipping a first-class dark theme we adopt

Light-mode borders are almost invisible (`--color-gray-200` / `--color-gray-100`). Dark mode had the scale inverted: “lighter” Element Plus border tokens were *brighter* grays, and `.el-card` used raw `--color-gray-200` (`#e5e7eb`) which stays light on navy. Cards and table rules therefore framed every block.

Dark `--color-border` / `--color-border-subtle` are low-opacity primary-200 so hairlines sit on navy instead of reading as white lines. `--el-border-color*` and `--el-table-border-color` point at those tokens. Light `--color-border` is still `--color-gray-200`, so the card rule change is a rename. `--color-sidebar-active` is `#2563a8` so the current page is a real chip, not the same navy as the sidebar border.

**Rejected alternative:** restyle only People Management. The dominating lines come from shared tokens those controls already use; page-local hex would fight the next dashboard screen.

**Rejected alternative:** keep the gray-700→gray-400 dark scale and only dim `.el-card`. Table-v2 row rules use `--el-border-color-lighter`, which would stay loud.

## Election-stage chips use fill tokens, not light-only white

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #285; `StageControl.vue` had `#fff` / `#dcdfe6` / `#606266`  
**Revisit when:** stage chips gain a selected-outline treatment that needs its own token

Unselected stage buttons are `<button class="stage-control__seg">` with CSS fills. Hardcoded white/gray made them look like light-theme leftovers on the navy sidebar. They now use `--el-fill-color-blank`, `--el-border-color`, and `--el-text-color-regular` so dark mode gets gray-900 chips. Selected still paints `--color-stage-*` inline.

**Rejected alternative:** a `--color-stage-chip-*` pair. The Element Plus fill/border tokens already mean “unselected control surface.”

## Names use `--color-text-link`, not `--el-color-primary`

**Status:** active  
**Evidence:** inferred  
**Source:** issue #285 names-list contrast; People table uses `el-button type="primary" link`  
**Revisit when:** primary solid buttons also need a lighter dark fill

`--el-color-primary` is `#2563a8` in both themes. That hue pops on white and goes muddy on `#0e2040` / `#111827`. Brightening `--el-color-primary` in dark would also recolor solid primary actions (Add Person). `--color-text-link` is primary-500 in light (same as today) and primary-200 in dark. `PeopleTable` name buttons set `--el-button-text-color` from that token.

**Rejected alternative:** change dark `--el-color-primary`. Out of scope for this dashboard slice and would shift every primary fill.
