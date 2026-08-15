# People records

## AgeGroup is not stored

**Status:** active  
**Evidence:** confirmed  
**Source:** maintainer decision after review of eligibility vs leftover v2/v3 metadata  
**Revisit when:** someone proposes restoring a demographic age field separate from eligibility

`Person.AgeGroup` (`A`/`Y`) was a v2/v3 column carried into v4. It did not drive voting or candidacy. Youth who can vote but cannot be elected use eligibility reason **V01** (“Youth aged 18/19/20”). Under-18 uses **X05**. The person form had both controls and they were never synced.

The column, person DTOs, form dropdown, and unused turnout-by-age breakdown were removed. Incoming v2 XML / JSON packages may still contain `AgeGroup`; it is ignored so old files keep importing.

**Rejected alternative:** keep the column for turnout reports. The only consumer grouped `HasOnlineBallot` and the UI never showed it.

**Rejected alternative:** derive V01 from Age Group = Youth. Adult youth (18–20) and under-18 are different eligibility rows; a two-value age flag cannot express that.

## Person eligibility is stored as a short code

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #263  
**Revisit when:** a new reason is added, or an incoming package format no longer carries GUIDs

`Person` stores `IneligibleReasonCode` (`X01`, `V04`, …). Empty/null means fully eligible. Votes already stored the same short code.

GUIDs stay on `IneligibleReasonEnum` only so old JSON packages and v2/v3 XML can still be imported. Incoming files may send a code or a GUID (including legacy v3 sub-reason GUIDs); only the code is persisted. New package export writes the code.

**Rejected alternative:** keep `IneligibleReasonGuid` on Person and derive the code at the API. Create/update, name import, and the person form all used the code already; the GUID column was leftover v3 storage.

**Rejected alternative:** store both columns. Two sources of truth would drift, and the index would still need the GUID.
