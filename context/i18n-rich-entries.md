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

## The brand stays Latin TallyJ

**Status:** active  
**Evidence:** confirmed  
**Revisit when:** the product name itself changes

`TallyJ` is never translated or transliterated. Descriptive words around it are translated (`TallyJ v4`, `TallyJ Logo`, `Welcome to TallyJ`). A non-English string whose English source contains the literal `TallyJ` must contain that same literal. `validateLocaleCatalog` reports `BRAND_NOT_LITERAL` when it does not.

**Rejected alternative:** a local product name (ScrutinJ, EscrutiJ, LaskiJ, شمارَج, and the same pattern in other locales). The election app and the officer app would no longer share one brand.

## Officers glossaries are the shared terminology source

**Status:** active  
**Evidence:** confirmed  
**Revisit when:** Officers stops publishing per-locale glossaries, or v4 adds a concept Officers does not name

Where a concept exists in both this app and TallyJ Officers (election, ballot, teller, voter, Assembly, officer, vote, and the other glossary entries), v4 uses the Officers term. Officers wins when the two translations differ. French is one neutral French and uses `e-mail`, not `courriel`.

The shared-term authority is `docs/i18n/glossary.<locale>.md` in `glittle/TallyJ-Officers-v2` at commit `36298a3` (`#191` on Officers `main`), for `ar`, `de`, `es`, `fa`, `fi`, `fr`, `hi`, `hr`, `ko`, `pt`, `ru`, `sw`, `vi`, and `zh`. Where a glossary row does not name the concept (the person who votes is the usual case), use that commit's `frontend/src/locales/<locale>/*.json`. Confirm common app-title strings there are still Latin `TallyJ`. French is one neutral French and uses `e-mail`, not `courriel`, including where a glossary is silent on email. German strings that Officers does not have were machine-drafted with the German glossary terms.

Re-sync:

1. Read the Officers glossary first, then the locale JSON, for each shared language.
2. Substitute the shared term in v4. Leave the rest of a v4 sentence in place unless the Officers string is the same concept and should be copied.
3. New or changed text is `s: ai` with a new `w`. Do not mark it `human` or `approved`. Officers text is still an AI draft even when copied verbatim.
4. An existing v4 `human` or `approved` string stays as written unless the only edit is that shared-term substitution. If its text changes, set `s` to `ai` and list the key.
5. Run `npm run validate:i18n` from `frontend/`.

**Rejected alternative:** keep v4's earlier term when it disagrees with Officers. The two sites are meant to use the same words for the same ideas.
