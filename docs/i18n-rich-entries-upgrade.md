# Upgrade: rich i18n locale entries (`t` / `s` / `w`)

Hand this file to an AI coding agent (Claude, Cursor, Copilot, etc.) working in a repo that uses the same locale layout as TallyJ v4: per-locale JSON message files under something like `frontend/src/locales/{lang}/*.json`, loaded via vue-i18n (or equivalent), with an optional production **bundled** merge step.

## Goal

Stop storing translations as bare strings. Store **text + provenance** so human edits are not overwritten by later AI/MT passes, and so stale translations can be detected when English changes.

### Before (leaf values)

```json
{
  "ballots.addBallot": "Agregar Boleta"
}
```

### After (leaf values)

```json
{
  "ballots.addBallot": {
    "t": "Agregar Boleta",
    "s": "ai",
    "w": "2026-01-15T15:35:00Z"
  }
}
```

| Field | Meaning |
|-------|---------|
| `t` | Translation text (required) |
| `s` | Status code (required) |
| `w` | When this translation was last set (ISO-8601 UTC, required) |

### Status codes (`s`)

| Code | Who sets it | AI may overwrite? |
|------|-------------|-------------------|
| `source` | English (source locale) only | N/A — English is the source of truth |
| `ai` | Machine / AI draft | Yes |
| `human` | Human tweak | **No** (flag stale only) |
| `approved` | Human review sign-off | **No** (flag stale only) |

Do **not** use display strings like `"AI Draft"` in the files. Map codes to labels in any UI.

## Runtime contract (critical)

**vue-i18n (and similar) must only see plain strings at runtime.**

1. Editable / source-of-truth files keep `{ t, s, w }`.
2. At load (dev) and in the **bundled** production build, **unwrap** each leaf: replace `{ t, s, w }` with `t`.
3. Production `bundled/*.json` (or equivalent) should contain **text only** — no `s`/`w` — for smaller payloads and so a missed unwrap cannot leak metadata into `$t()`.

Nested namespaces (objects that group keys) stay objects. Only **string leaves** become rich objects; after unwrap, leaves are strings again.

Any other reader of the source JSON must do the same unwrap. A backend string localizer that used to deserialize `Dictionary<string, string>` has to read `t` and ignore `s` / `w`. Point that reader at the source files, not at the text-only frontend bundle, unless the bundle is what you actually deploy beside the API.

A root shared file (for example `locales/common.json`) may mix message strings with non-message config (arrays, numbers, booleans). Convert the string leaves only. Leave the config values as they are, and teach unwrap to pass them through.

## English / source locale

English (or whatever is `fallbackLocale`) must use the **same object shape**:

```json
{
  "ballots.addBallot": {
    "t": "Add Ballot",
    "s": "source",
    "w": "2026-09-23T17:00:00Z"
  }
}
```

Whenever the English **text** changes, bump that key’s `w` to now. That timestamp is what other locales compare against.

Root shared strings that are not translations of English use `s: "source"` as well.

## Stale / retranslate rules (for AI sync tools)

For each key in a non-source locale:

1. If `s` is `human` or `approved`: **never** overwrite `t`. If locale `w` is older than English `w`, mark for human re-review (e.g. set a workflow flag or list in a report). Optionally leave `s` as-is.
2. If `s` is `ai` (or missing after migration defaults to `ai`) **and** locale `w` is older than English `w`: AI may rewrite `t`, set `s` to `ai`, and set `w` to now.
3. New keys with no translation yet: AI may create `{ t, s: "ai", w: now }`.

Comparing `w` is intentional and simple. A content hash of English is an optional later enhancement; do not require it for this upgrade.

## Migration steps (do all of these)

1. **Inventory** locale roots (e.g. `frontend/src/locales/`), per-lang folders, `common.json` / shared files, the merge-bundle script, validators, and i18n bootstrap (`createI18n`, `loadLocaleMessages`, `flatToNested`, `deepMerge`). In this layout the merge script is `frontend/merge-locales.js` (`npm run merge-locales`) and the validator is `frontend/src/locales/validate-translations.js` (`npm run validate:i18n`). Also find every runtime reader of those JSON files (vue-i18n load, test setup, API localizer).
2. **Add unwrap helpers** used by load + bundle, for example:
   - `isRichEntry(v)` → `v` is a non-null object with string `t` and no keys other than `t`, `s`, and `w` (so a nested message tree is not treated as a leaf).
   - `unwrapMessages(obj)` → deep walk; rich leaves → `t`; plain string leaves → leave as-is (backward compat during migration); nested objects → recurse; arrays and other config values → leave as-is.
3. **Wire unwrap** into every path that feeds vue-i18n messages (eager English, async locale load, tests that mount i18n) **before** `setLocaleMessage` / the initial `messages` option. Do the same in any backend reader. `deepMerge` must treat a rich entry as one leaf so `t` / `s` / `w` are not merged as child keys.
4. **Update the bundle/merge script** so `bundled/{lang}.json` is written **after** unwrap (text-only). Fail the script if a bundled value is not a string.
5. **Convert all locale JSON leaves** from string → `{ t, s, w }`:
   - Source locale, and string leaves in root shared JSON: `s: "source"`, `w` = migration timestamp (same UTC instant for the batch is fine).
   - Other locales: `s: "ai"`, `w` = same migration timestamp (treat existing text as AI drafts until a human marks otherwise).
   - Preserve key structure and ICU / interpolation placeholders exactly. Do not change wording.
6. **Update validators** (e.g. `validate-translations.js`):
   - Expect rich leaves in source files (require `t` string non-empty; `s` in the allowed set for that locale; `w` parseable ISO-8601 UTC).
   - English (and root shared message leaves) must use `s: "source"`. Other locales must use `ai`, `human`, or `approved`.
   - Reject bare string leaves in source files.
   - Skip generated `bundled/` output; it is text-only on purpose.
   - Key parity across locales still compares keys, not `t` text. A rich entry is one key, not three (`t`, `s`, `w`).
7. **Tests**: any fixture locale JSON used by Vitest/Jest must either be converted or go through unwrap. Assert `$t('some.key')` still returns a string.
8. **Docs**: short project note describing the schema, status codes, unwrap, and “AI must not overwrite human/approved”.
9. **Do not** change call sites (`$t('…')`, `t('…')`) — only storage and load/bundle.

## Optional later (out of scope unless asked)

- Translator UI to set `s` to `human` / `approved` and bump `w`.
- Export/import to Crowdin / Weblate / Phrase using `t`/`s`/`w`.
- Sidecar `_status` files instead of inline objects (not needed if you adopt this inline schema).

## Acceptance checklist

- [ ] All editable locale leaves are `{ t, s, w }` (English has `s: "source"`).
- [ ] Runtime / `$t` returns strings only.
- [ ] Bundled production locale files are text-only (no `s`/`w`).
- [ ] Validator passes on rich source files.
- [ ] Existing unit tests that exercise i18n still pass.
- [ ] Brief project doc describes schema + AI overwrite rules.
- [ ] No intentional copy changes to translation text during the format migration.

## Anti-patterns

- Putting `{ t, s, w }` objects into vue-i18n without unwrap (breaks `$t`).
- Shipping `s`/`w` in production bundles “for convenience”.
- Leaving the API localizer on `Dictionary<string, string>` after the source files change (the file fails to load and every key disappears).
- Inferring “human” from git blame.
- Using free-text status labels in JSON instead of codes.
- Overwriting `human` / `approved` when English `w` is newer.

## Naming note

This upgrade is format-only. Keep existing file naming (`en/ballots.json`, `fr/ballots.json`, etc.) and merge tooling unless the target repo already differs.
