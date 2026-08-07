# Ballot Validation & Pre-Finalization Integrity

## Status: active
## Evidence: confirmed (issues #189 / #190)

Validation must catch problems *before* analysis or finalization. Catching errors only at report time is too late.

### Key constraint
Once an election moves past certain states, correcting bad ballot data becomes expensive or impossible without breaking the audit trail.

### Design posture
Prefer explicit failure and clear recovery paths over silent best-effort acceptance of questionable ballots.

### Related
- Front Desk flows
- Online ballot acceptance
- Election analysis engine
