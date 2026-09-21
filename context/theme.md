# Theme tokens

## Dark hairlines must win over later `:root` remaps

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #285 leftover after PR #293; `style.less` import order is `tokens.less` → `tokens-dark.less` → `element-plus.less`  
**Revisit when:** Element Plus official `dark/css-vars.css` is adopted, or another `:root` block is added after the dark tokens

PR #293 set dark `--el-border-color*` on `.dark`, but `element-plus.less` then redeclared the same names on `:root` (`gray-300` / `gray-200` / `gray-100`). `:root` and `.dark` are both specificity 0,1,0, so source order won and the later gray remaps painted light hairlines on navy. Cards, table-v2 row rules, inputs, and stage-chip outlines all read those Element Plus variables.

Dark tokens now live on `html.dark` (0,1,1). The later `:root` block no longer restates `--el-border-color*`; light values stay in `tokens.less` (`--el-border-color` remains `gray-300`). Utility `.border*` classes and TipsPanel use `--color-border` instead of a frozen gray or a `#e4e7ed` fallback.

**Rejected alternative:** remap the later `:root` block to `--color-border`. That would also change light `--el-border-color` from gray-300 to gray-200. Light mode is the #285 reference; dropping the duplicate keeps that hairline.

**Rejected alternative:** import Element Plus `theme-chalk/dark/css-vars.css`. It would fight the custom navy sidebar and restyle the whole app, which is out of scope for this contrast pass.

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

`--el-color-primary` is `#2563a8` in both themes. That hue pops on white and goes muddy on `#0e2040` / `#111827`. Brightening `--el-color-primary` in dark would also recolor solid primary actions (Add Person). `--color-text-link` is primary-500 in light (same as today) and primary-200 in dark. `PeopleTable` name buttons set `--el-button-text-color` and `color` from that token. Global `a` uses the same pair so other lists do not stay on the muted primary.

**Rejected alternative:** change dark `--el-color-primary`. Out of scope for this dashboard slice and would shift every primary fill.

## Leftover screens use the same tokens (Front Desk / voter / profile / join)

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #285 follow-up after PR #354; local hex on Front Desk filters, voter elections/ballot, Profile QR, Teller join, CardSkeleton, tie cards  
**Revisit when:** a designer pass restyles dark chrome, or another page still shows a light-only surface

#354 fixed the cascade and dashboard/setup screenshot pages. Remaining leftovers were page-local `#fff` / `#dcdfe6` / `#fffbe6` / `#ebeef5` / `background: white` that never read the shared tokens.

This slice maps those surfaces onto tokens that already flip in `html.dark`:

- Front Desk inactive method/flag chips use `--el-fill-color-blank` (same “unselected control” as stage chips). Active chip text uses `--color-frontdesk-filter-active-text`. The registration panel stays `--color-orange-50`; dark now sets that to `#3a2706` (same family as `--color-stage-gather-bg`) so the cream sheet does not sit on navy.
- Voter elections open-row highlight is `--color-warning-50` (already dark `#1f1300`). Ballot filled/duplicate slots use `--color-success-50` / `--color-error-50` instead of Element Plus `*-light-9` (those generate near-white tints unless remapped). Dark also points `--el-color-success-light-9` / warning / danger at the 50 tokens so header status chips and utilities follow.
- Teller join select border is `--el-border-color`. CardSkeleton fill is `--el-fill-color-blank`. Tie cards use `--el-border-color` / `--el-color-danger`. Presentation person cards already used the 50 tokens; the extra `.dark { rgba(...) }` overrides were dropped so one token path wins.
- Profile 2FA and guest-teller QR pads use `--color-qr-pad` (`#ffffff` in light, not remapped in dark). A white pad is required for scanners; filling with `--el-fill-color-blank` would make the code unreadable on navy.

Light `--color-orange-50` stays `#fff5eb`. Light chip inactive fill is still white via `--el-fill-color-blank` → `--color-bg-primary`.

**Rejected alternative:** `color-mix` orange onto `--el-fill-color-blank` for the registration panel. Matching the existing cream (`#fff5eb`) would need a guessed mix percentage, and `--color-orange-50` already means “pale orange surface.”

**Rejected alternative:** a `--color-frontdesk-registration-bg` pair. The overlay was the only `--color-orange-50` consumer; remapping that token is enough.

**Still deferred:** designer’s-eye pass; AppSidebar / LandingPage / LanguageFlagsSelector `#fff4e5` warning banners; AuditLogs `#f5f7fa`; Facebook/Kakao brand button hex; teller BallotVotesPanel inverse `#fff` on a solid primary; Element Plus official `dark/css-vars.css`; #192 / #182 / #168 / #336 product work.
