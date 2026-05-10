---
name: TallyJ Localization Rules
description: Strict rules for handling text, strings, and localization files in the TallyJ project.
version: 1.0
tags: [localization, i18n, en, locales]
---

# TallyJ Localization Skill

## Core Rule (Always Follow)

**Never modify any locale file except the English one (`en`) unless the user explicitly asks for a translation.**

### Specific Guidelines

1. **English-only changes by default**
   - All new text, labels, messages, tooltips, button text, error messages, etc. must be added **only** to the English locale files (under `/frontend/src/locales/en`).
   - Do **not** touch any other language folders (e.g. `fr/`, `es/`, `ko/`, `fa/`, etc.).

2. **Ignore other locale files**
   - When scanning the project or making changes, ignore all non-`en` locale files completely.
   - Do not add English strings to them.
   - Do not try to "keep them in sync" unless the user specifically requests a translation task.

3. **When a translation is requested**
   - Only then may you edit other locale files.
   - Clearly mark in your response which files were updated for translation.

4. **Why this rule exists**
   - Previously, English text was being added to other locale files, making it very difficult for maintainers to know which strings actually need professional translation.
   - Keeping English as the single source of truth greatly simplifies the localization workflow.

### File Location Patterns (for reference)

- Look for English strings in: `**/locales/en/**`
- Ignore: `**/locales/**` except the `en` subdirectory.

### Enforcement

- If you are about to edit a non-English locale file, **stop and ask for confirmation** first.
- Always prioritize the English locale when adding or updating any user-facing text.

This rule takes precedence over any general i18n best practices the model may have learned.
