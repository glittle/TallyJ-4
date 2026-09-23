# Rich locale entries

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #359  
**Revisit when:** a translator UI or an external translation memory is added

## Leaves keep text and provenance together

Editable locale JSON stores each message as `{ t, s, w }` so a later AI pass can refresh machine drafts without overwriting a human edit, and so a translation can be seen as stale when English changes. vue-i18n still receives plain strings.

**Decision:** source files under `frontend/src/locales/{lang}/*.json`, and string leaves in root `frontend/src/locales/common.json`, use this shape. `t` is the text. `s` is `source` for English and for those root shared strings. Other locales use `ai`, `human`, or `approved`. `w` is ISO-8601 UTC for when that text was last set. `frontend/src/locales/index.ts` unwraps to `t` before `createI18n` / `setLocaleMessage`. `frontend/merge-locales.js` writes `bundled/{lang}.json` as text only. `JsonLocalizationProvider` reads `t` from the same source files the API already points at; it does not read the frontend bundle.

**Rejected alternative:** ship `s` and `w` in `bundled/*.json`. The bundle stays a string catalog, and a missed unwrap cannot return a metadata object from `$t()`.

**Rejected alternative:** sidecar status files. Text and provenance stay on the same key.

**Rejected alternative:** status labels such as `"AI Draft"` in the JSON. Codes stay stable for tools. This change does not add UI labels.

## AI may refresh only `ai` drafts

When English text changes, bump that key's `w`. An AI sync may rewrite a non-English `t` only when `s` is `ai` and that locale's `w` is older than the English `w`. It must not overwrite `human` or `approved`. Those stay as written and are flagged stale for a person. No sync tool or translator UI is part of this change.

The format migration did not change wording. English and root shared strings were marked `source`. Existing translations were marked `ai` with one shared UTC timestamp, until a person marks them otherwise.

The portable steps for another repo with this layout are in `docs/i18n-rich-entries-upgrade.md`.
