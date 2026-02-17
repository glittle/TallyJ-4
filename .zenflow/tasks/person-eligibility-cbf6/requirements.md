# Person Eligibility - Product Requirements Document

## Overview

Implement a comprehensive person eligibility system that tracks whether each person in an election can vote, can receive votes, or both, along with the specific reason for any restriction. This replaces the v3 hard-coded `IneligibleReasonEnum` with a database-driven, i18n-ready approach using short codes.

## Background

In v3, eligibility reasons were defined in `IneligibleReasonEnum.cs` as a static enumeration with GUIDs. The Person entity already has `IneligibleReasonGuid`, `CanVote`, and `CanReceiveVotes` fields but they are not properly connected to a defined set of reasons in v4.

## Requirements

### R1: Eligibility Reason Database Table

Create a new `IneligibleReason` database table (seeded, not user-editable) with:

| Column | Type | Description |
|--------|------|-------------|
| `ReasonGuid` | `Guid` (PK) | Reuses exact GUIDs from v3 for backward compatibility |
| `ReasonCode` | `string(4)` | Short code like `X01`, `R01`, `V01`, `U01`, `Z01` (see R2) |
| `DescriptionKey` | `string(80)` | i18n key for the description (e.g. `eligibility.X01`) |
| `CanVote` | `bool` | Whether the person can vote |
| `CanReceiveVotes` | `bool` | Whether the person can receive votes |
| `InternalOnly` | `bool` | If true, cannot be used in import files (Unreadable/Unidentifiable reasons) |

### R2: Group Code Scheme

Each reason belongs to a group determined by its CanVote/CanReceiveVotes combination. The code prefix letter reflects the group; the numeric suffix distinguishes reasons within the group.

| Prefix | CanVote | CanReceiveVotes | Meaning |
|--------|---------|-----------------|---------|
| `X` | false | false | Cannot vote, cannot receive votes |
| `R` | true | false | Can vote, cannot receive votes |
| `V` | false | true | Cannot vote, can receive votes |
| `U` | false | false | Unidentifiable (internal only, used during ballot entry) |
| `Z` | false | false | Unreadable (internal only, used during ballot entry) |

Note: `U` and `Z` are subsets of the "cannot vote / cannot receive votes" group, but are separated because they have special semantics: they describe problems with a vote line on a ballot, not a person's civil eligibility. They must be excluded from import files and from the eligibility dropdown on the Person form.

Persons with no `IneligibleReasonGuid` (null) are fully eligible (can vote and receive votes).

### R3: Exact v3 Reason Mapping

Every reason from v3 must be preserved with its exact GUID. The following is the complete mapping:

**Group X (Cannot vote, cannot receive votes):**
| Code | GUID | v3 Description |
|------|------|----------------|
| X01 | D227534D-D7E8-E011-A095-002269C41D11 | Deceased |
| X02 | CF27534D-D7E8-E011-A095-002269C41D11 | Moved elsewhere recently |
| X03 | 2add3a15-ec2d-437c-916f-7c581e693baa | Not in this local unit |
| X04 | D127534D-D7E8-E011-A095-002269C41D11 | Not a registered Baha'i |
| X05 | 32e44592-a7d8-408a-b169-8871800f62aa | Under 18 years old |
| X06 | D327534D-D7E8-E011-A095-002269C41D11 | Resides elsewhere |
| X07 | D027534D-D7E8-E011-A095-002269C41D11 | Rights removed (entirely) |
| X08 | E027534D-D7E8-E011-A095-002269C41D11 | Not a delegate and on other Institution |
| X09 | D527534D-D7E8-E011-A095-002269C41D11 | Other (cannot vote or be voted for) |

**Group R (Can vote, cannot receive votes):**
| Code | GUID | v3 Description |
|------|------|----------------|
| R01 | e6dd1cdd-5da0-4222-9f17-f02ce6313b0a | Youth aged 18/19/20 |
| R02 | C05EAE49-B01B-E111-A7FB-002269C41D11 | By-election: On Institution already |
| R03 | D427534D-D7E8-E011-A095-002269C41D11 | On other Institution (e.g. Counsellor) |
| R04 | 920A1A55-C4A5-42E5-9BCE-31756B6A20B9 | Rights removed (cannot be voted for) |
| R05 | EB159A43-FB09-4FA9-AC12-3F451073010B | Tie-break election: Not tied |
| R06 | 24278180-fe1b-4604-9f86-d453b151d824 | Other (can vote but not be voted for) |

**Group V (Cannot vote, can receive votes):**
| Code | GUID | v3 Description |
|------|------|----------------|
| V01 | 4B2B0F32-4E14-43A4-9103-C5E9C81E8783 | Not a delegate in this election |
| V02 | 84FA30C9-F007-44E8-B097-CCA430AAA3AA | Rights removed (cannot vote) |
| V03 | f4c7de9e-d487-49ae-9868-5cd208cd863a | Other (cannot vote but can be voted for) |

**Group U (Unidentifiable - internal only):**
| Code | GUID | v3 Description |
|------|------|----------------|
| U01 | D927534D-D7E8-E011-A095-002269C41D11 | Could refer to more than one person |
| U02 | D727534D-D7E8-E011-A095-002269C41D11 | Multiple people with identical name |
| U03 | D827534D-D7E8-E011-A095-002269C41D11 | Name is a mix of multiple people |
| U04 | CE27534D-D7E8-E011-A095-002269C41D11 | Unknown person |

**Group Z (Unreadable - internal only):**
| Code | GUID | v3 Description |
|------|------|----------------|
| Z01 | D627534D-D7E8-E011-A095-002269C41D11 | In an unknown language |
| Z02 | 86DDBE4A-841D-E111-A7FB-002269C41D11 | Not a complete name |
| Z03 | CD27534D-D7E8-E011-A095-002269C41D11 | Writing is illegible |

### R4: Backend API

- **GET `/api/eligibility-reasons`** - Returns all reasons (for UI dropdowns, reports). No auth required beyond election access.
- The Person entity's `CanVote` and `CanReceiveVotes` fields should be computed from the `IneligibleReasonGuid` rather than stored independently. When `IneligibleReasonGuid` is null, the person is fully eligible. When set, look up the reason to determine CanVote/CanReceiveVotes.
  - **Decision**: Keep `CanVote` and `CanReceiveVotes` as stored DB columns for query performance (indexes exist on them). They are set automatically when `IneligibleReasonGuid` changes. The backend enforces consistency.

### R5: Frontend - Person Form Eligibility

Replace the current two independent toggle switches (`canVote`, `canReceiveVotes`) with a single eligibility dropdown:

- Default: "Eligible" (no reason, null IneligibleReasonGuid)
- Options grouped by code prefix (X, R, V)
- U and Z reasons excluded from the person form (they apply to vote lines, not people)
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
  - A reason code (`X01`, `R03`, etc.)
  - The English description text (for v3 backward compatibility, matched case-insensitively)
  - Empty/missing = fully eligible
- U and Z codes are rejected in import files with an error message
- Invalid codes produce an import error for that row (row is skipped, import continues)

### R8: Seed Data

Seed the `IneligibleReason` table with all reasons from R3 in `DbSeeder.cs`, following the existing idempotent pattern. This seed data is application-level reference data, not election-specific.

### R9: Backward Compatibility

- Existing `Person.IneligibleReasonGuid` values from v3 data must continue to work since the GUIDs are preserved
- The `Person.CanVote` and `Person.CanReceiveVotes` columns remain for index/query performance
- Existing import files using English descriptions remain supported

## Assumptions

- The IneligibleReason table is global (not per-election). All elections share the same set of reasons.
- Reasons are not user-editable. New reasons would be added via migrations/seed updates.
- The `CanVote`/`CanReceiveVotes` booleans on Person are treated as denormalized cache of the reason's flags. Backend enforces they stay in sync.
- French translations for reason descriptions will need to be provided (can use placeholder translations initially).

## Out of Scope

- UI for managing/adding custom eligibility reasons
- Per-election custom reasons
- Audit trail of eligibility changes (covered by existing log system)
- Eligibility reason display in vote/ballot entry UI (U/Z reasons are used there but that is a separate feature)
