# Election Analysis Engine

## Status: active
## Evidence: confirmed (maintainer + issue #168)

The core analysis engine is the highest-risk component in TallyJ v4. It must produce correct results against known-good v3 data and handle ties, mixed voting methods, and edge cases.

### Why this is treated as Critical
- Incorrect analysis directly corrupts election results.
- Historical source of subtle bugs in earlier versions.
- Must be validated with dedicated test elections before other work is considered complete.

### Design posture
Risk-first: prove analysis correctness before polishing secondary features or UI.

### Related
- Ballot validation (must catch problems before analysis)
- Election state management (analysis only runs in appropriate states)
