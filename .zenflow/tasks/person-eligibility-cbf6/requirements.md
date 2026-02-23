# Person Eligibility - Product Requirements Document

## Overview

Implement a comprehensive person eligibility system that tracks whether each person in an election can vote, can receive votes, or both, along with the specific reason for any restriction. This replaces the v3 hard-coded `IneligibleReasonEnum` with a clean static class using short codes and i18n support.

## Background

In v3, eligibility reasons were defined in `IneligibleReasonEnum.cs` as a static enumeration with GUIDs. The Person entity already has `IneligibleReasonGuid`, `CanVote`, and `CanReceiveVotes` fields but they are not properly connected to a defined set of reasons in v4.

## Requirements

### R1: Static Eligibility Reason Class

Define all eligibility reasons as a static class in the backend domain layer (e.g. `IneligibleReasonEnum` in `Backend.Domain/Enumerations/`). Each reason has:

- **ReasonGuid** (`Guid`) - Reuses exact GUIDs from v3 for backward compatibility
- **ReasonCode** (`string`) - Short code like `X01`, `V01`, `R01`, `U01`, `U02` (see R2)
- **CanVote** (`bool`) - Whether the person can vote
- **CanReceiveVotes** (`bool`) - Whether the person can receive votes
- **InternalOnly** (`bool`) - If true, cannot be used in import files (Unreadable/Unidentifiable reasons)

No database table. The data is static reference data loaded in memory. Lookup methods provided by GUID, by code, and by description (case-insensitive, for v3 import compatibility).

### R2: Group Code Scheme

Each reason belongs to a group determined by its CanVote/CanReceiveVotes combination. The code prefix letter reflects the group; the numeric suffix distinguishes reasons within the group.

| Prefix | CanVote | CanReceiveVotes | Meaning                                                         |
| ------ | ------- | --------------- | --------------------------------------------------------------- |
| `X`    | false   | false           | Cannot vote, cannot receive votes                               |
| `V`    | true    | false           | Can **V**ote only (cannot receive votes)                        |
| `R`    | false   | true            | Can **R**eceive votes only (cannot vote)                        |
| `U`    | false   | false           | Internal only (used during ballot entry for problem vote lines) |

Note: `U` reasons describe problems with a vote line on a ballot (unidentifiable or unreadable), not a person's civil eligibility. They are applied to votes, not to people. They must be excluded from import files and from the eligibility dropdown on the Person form.

Persons with no `IneligibleReasonGuid` (null) are fully eligible (can vote and receive votes).

### R3: Exact v3 Reason Mapping

Every reason from v3 must be preserved with its exact GUID. The following is the complete mapping:

**Group X (Cannot vote, cannot receive votes):**
| Code | GUID | v3 Description |
|------|------|----------------|
| X01 | D227534D-D7E8-E011-A095-002269C41D11 | Deceased |
| X02 | CF27534D-D7E8-E011-A095-002269C41D11 | Moved elsewhere recently |
| X03 | 2add3a15-ec2d-437c-916f-7c581e693baa | Not in this local unit |
| X04 | D127534D-D7E8-E011-A095-002269C41D11 | Not a registered Bahá’í |
| X05 | 32e44592-a7d8-408a-b169-8871800f62aa | Under 18 years old |
| X06 | D327534D-D7E8-E011-A095-002269C41D11 | Resides elsewhere |
| X07 | D027534D-D7E8-E011-A095-002269C41D11 | Rights removed (entirely) |
| X08 | E027534D-D7E8-E011-A095-002269C41D11 | Not a delegate and on other Institution |
| X09 | D527534D-D7E8-E011-A095-002269C41D11 | Other (cannot vote or be voted for) |

**Group V (Can vote only, cannot receive votes):**
| Code | GUID | v3 Description |
|------|------|----------------|
| V01 | e6dd1cdd-5da0-4222-9f17-f02ce6313b0a | Youth aged 18/19/20 |
| V02 | C05EAE49-B01B-E111-A7FB-002269C41D11 | By-election: On Institution already |
| V03 | D427534D-D7E8-E011-A095-002269C41D11 | On other Institution (e.g. Counsellor) |
| V04 | 920A1A55-C4A5-42E5-9BCE-31756B6A20B9 | Rights removed (cannot be voted for) |
| V05 | EB159A43-FB09-4FA9-AC12-3F451073010B | Tie-break election: Not tied |
| V06 | 24278180-fe1b-4604-9f86-d453b151d824 | Other (can vote but not be voted for) |

**Group R (Can receive votes only, cannot vote):**
| Code | GUID | v3 Description |
|------|------|----------------|
| R01 | 4B2B0F32-4E14-43A4-9103-C5E9C81E8783 | Not a delegate in this election |
| R02 | 84FA30C9-F007-44E8-B097-CCA430AAA3AA | Rights removed (cannot vote) |
| R03 | f4c7de9e-d487-49ae-9868-5cd208cd863a | Other (cannot vote but can be voted for) |

**Group U (Internal only - ballot entry):**
| Code | GUID | Description |
|------|------|-------------|
| U01 | CE27534D-D7E8-E011-A095-002269C41D11 | Unidentifiable |
| U02 | CD27534D-D7E8-E011-A095-002269C41D11 | Unreadable |

v3 had multiple sub-reasons for Unidentifiable (4 reasons) and Unreadable (3 reasons). In v4 these are collapsed into U01 and U02. The static class includes a mapping of all legacy v3 GUIDs to the corresponding U01/U02 code for backward compatibility.

### R4: Backend API

- **GET `/api/eligibility-reasons`** - Returns all reasons from the static class (for UI dropdowns, reports). No auth required beyond election access.
- The Person entity's `CanVote` and `CanReceiveVotes` fields are kept as stored DB columns for query performance (indexes exist on them). They are set automatically by the backend when `IneligibleReasonGuid` changes, using the static class lookup. The backend enforces consistency.

### R5: Frontend - Person Form Eligibility

Replace the current two independent toggle switches (`canVote`, `canReceiveVotes`) with a single eligibility dropdown:

- Default: "Eligible" (no reason, null IneligibleReasonGuid)
- Options grouped by code prefix (X, V, R)
- U reasons excluded from the person form (they apply to vote lines, not people)
- Selecting a reason automatically determines CanVote and CanReceiveVotes
- Display the localized description from i18n

### R6: i18n Support

Add entries to `en/*.json` and `fr/*.json` locale files for:

- Each reason code (e.g. `"eligibility.X01": "Deceased"`)
- Group labels (e.g. `"eligibility.groupX": "Cannot vote, cannot receive votes"`)
- UI labels for the eligibility dropdown

Import files can reference reasons by code (e.g. `X01`) or by the English description for backward compatibility.

### R7: Name Import Integration

When importing people from CSV/Excel:

- Accept an `Eligibility` (or `IneligibleReason`) column
- Column values can be:
  - A reason code (`X01`, `V03`, etc.)
  - The English description text (for v3 backward compatibility, matched case-insensitively)
  - Empty/missing = fully eligible
- U codes are rejected in import files with an error message
- Invalid codes produce an import error for that row (row is skipped, import continues)

### R8: Backward Compatibility

- Existing `Person.IneligibleReasonGuid` values from v3 data continue to work since the GUIDs are preserved
- The `Person.CanVote` and `Person.CanReceiveVotes` columns remain for index/query performance
- Existing import files using English descriptions remain supported

## Edge Cases

- **Unknown GUID in `IneligibleReasonGuid`**: If a Person record contains a GUID not in the static class (e.g. corrupted data), treat as ineligible (CanVote=false, CanReceiveVotes=false) and log a warning. During import, reject the row with an error.
- **Inconsistent `CanVote`/`CanReceiveVotes`**: The backend auto-heals on save. When a Person is loaded or saved, if the stored CanVote/CanReceiveVotes values don't match what the `IneligibleReasonGuid` dictates, correct them silently.
- **U reasons during ballot entry**: A vote line marked U01 or U02 is not associated with a person. The vote record has the `IneligibleReasonGuid` set but `PersonGuid` is null. This is out of scope for this feature but the static class must support it.

## Assumptions

- Reasons are static, global reference data defined in code. Not per-election, not user-editable.
- The `CanVote`/`CanReceiveVotes` booleans on Person are treated as denormalized cache of the reason's flags. Backend enforces they stay in sync.
- French translations for reason descriptions will need to be provided (can use placeholder translations initially).

## Out of Scope

- UI for managing/adding custom eligibility reasons
- Per-election custom reasons
- Audit trail of eligibility changes (covered by existing log system)
- Eligibility reason display in vote/ballot entry UI (U01/U02 reasons are used there but that is a separate feature)
