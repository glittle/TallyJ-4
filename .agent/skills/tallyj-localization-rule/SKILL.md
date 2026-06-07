---
name: tallyj-localization-rule
description: Strict rules for handling text, strings, and localization files in the TallyJ project.
version: 1.0
tags: [localization, i18n, en, locales]
---

# TallyJ Localization Skill

## Core Rule (Always Follow)

**Never modify any locale file except the English one (`en`) unless the user directly instructs you to edit a specific non-English locale file, naming either the language or the file path (e.g. "update the French locale", "edit fr/messages.json"). Indirect or ambiguous requests do not qualify.**

**If the user requests a translation without specifying the target language or file, ask: "Which language or locale file should I update? (e.g. fr, es, ko)" before making any changes.**

### Specific Guidelines

1. **English-only changes by default**
   - All new text, labels, messages, tooltips, button text, error messages, etc. must be added **only** to the English locale files (under `/frontend/src/locales/en`).
   - Do **not** modify any other language folders (e.g. `fr/`, `es/`, `ko/`, `fa/`, etc.) unless the user has directly requested a translation for a specific language or file.
   - Removing or renaming keys in the English locale file is permitted when the user requests it. Do not remove or rename the corresponding keys in non-English locale files unless the user explicitly requests that as well; instead, note in your response that non-English locale files may still contain the old key and will need manual cleanup.

2. **Ignore other locale files**
   - When making changes, do not modify non-`en` locale files unless a translation has been directly requested.
   - You may read and display the contents of non-English locale files when the user asks to inspect or compare them. The restriction applies only to writing or modifying those files, not to reading them.
   - Do not add English strings to them.
   - Do not try to "keep them in sync" unless the user specifically requests a translation task.

3. **When a translation is requested**
   - Only then may you edit other locale files.
   - At the end of your response, include a section titled "Files Updated for Translation" listing each modified file path as a bullet point, e.g. `- /frontend/src/locales/fr/messages.json`.

4. **Why this rule exists**
   - Previously, English text was being added to other locale files, making it very difficult for maintainers to know which strings actually need professional translation.
   - Keeping English as the single source of truth greatly simplifies the localization workflow.

### File Location Patterns (for reference)

- Look for English strings in: `**/locales/en/**`
- Ignore: `**/locales/**` except the `en` subdirectory.

### Enforcement

- By default, do not edit non-English locale files. If the user has directly requested a translation for a named language or file, proceed without additional confirmation. In all other cases, refuse the edit.
- Always prioritize the English locale when adding or updating any user-facing text.

This rule takes precedence over any general i18n best practices the model may have learned.
