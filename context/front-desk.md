# Front Desk

## Ballot Not Received is a hide-received filter

**Status:** active  
**Evidence:** confirmed (v3 Front Desk `#ifNoBallot`)  
**Source:** TallyJ-3.0 `Site/Views/Before/FrontDesk.cshtml` + `.cshtml.less` (`.NoBallot .Voter:not(.VM-)`)  
**Revisit when:** election setup exposes checklist flags as first-class mailed-ballot tracking

v3’s **Ballot Not Received** checkbox hid everyone whose voting method was set (ballot received / recorded). Combined with a checklist/flag, that showed who was flagged (for example mailed a ballot) but still had no method.

v4 already had All / Not registered / Registered radios and method/flag filters. Those cover the same workflow when the default is Not registered, but they did not name the v3 control or apply it on the All list.

The checkbox is an extra filter on the current list: hide rows with a voting method. Flag filters still apply. Clearing filters turns it off.

**Rejected alternative:** treat the Not registered radio as enough and skip the named checkbox. Rejected — #171 names the control, and operators combine it with flags on the full list.

## Roll Call and envelope pages are not v4 product pages

**Status:** active  
**Evidence:** inferred (v3 menu vs v4 routes; `docs/Hubs-v3-vs-v4.md`)  
**Source:** TallyJ-3.0 `Site/Views/menu.xml` (Roll Call / Sort Envelopes require `BallotProcess=Roll`); v4 has no `BallotProcess` setting  
**Revisit when:** a projector roll-call display or envelope-sort workflow is requested

v3 Roll Call and Sort Envelopes existed only for the Roll ballot process. Count Envelopes was `Ballots/Reconcile`. v4 has no Roll process, no Front Desk roll-call or envelope-count routes, and **RollCallHub is deferred**.

`GET .../frontdesk/rollCall` is a leftover that returns eligible voters plus check-in stats. Envelope numbers live on Front Desk (`ENABLE_ENVELOPE_NUMBERS` is still off). Envelope *counting* is the Analyze count-reconciliation report, not a Gathering page.

**Rejected alternative:** rebuild the v3 projector and envelope pages for #171. Rejected — they depended on a process v4 does not have; the remaining API is covered by tests, not a new UI.
