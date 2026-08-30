# Context index

Lean index of project *why* knowledge. Load a topic file only when the work touches that area.

- [architecture.md](architecture.md) — single backend host after domain consolidation; where code lives
- [auth.md](auth.md) — JWT identity claims (`sub` / `NameIdentifier`); teller vs online-voter cookie transport
- [realtime.md](realtime.md) — SignalR hub group naming (not a single `election-{guid}` pattern)
- [api-contracts.md](api-contracts.md) — dual API response wrappers and OpenAPI client regeneration
- [election-analysis.md](election-analysis.md) — core analysis engine; risk-first correctness vs v3
- [ballot-validation.md](ballot-validation.md) — pre-finalization integrity; fail explicitly before analysis
- [election-state.md](election-state.md) — teller coordination and high-consequence state transitions
- [online-ballots.md](online-ballots.md) — online acceptance and random name resolution
- [people.md](people.md) — person fields; AgeGroup removed (eligibility is V01/X05)
- [people-import.md](people-import.md) — three-action import pipeline (not a Next/Previous wizard)
- [sms-eligibility.md](sms-eligibility.md) — reject reserved/fictional/malformed phones before paid SMS/voice/WhatsApp; OnlineVoter.SmsStatus; ensure phone row on Person write
