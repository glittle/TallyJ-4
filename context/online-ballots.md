# Online Ballot Acceptance & Name Resolution

## Status: active

## Evidence: confirmed (issues #188 / #169 / #187)

Highest-risk functionality. Random name resolution and online acceptance introduce failure modes that did not exist (or were rare) in paper-only flows.

### Design posture

Treat online ballot paths with the same rigor as core analysis. Prefer explicit failure and recovery over silent best-effort behavior.

### Related

- Ballot validation
- Election state management
