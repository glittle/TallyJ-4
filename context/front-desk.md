# Front Desk

## Ballot Not Received is a hide-received filter

**Status:** active  
**Evidence:** confirmed (v3 Front Desk `#ifNoBallot`)  
**Source:** TallyJ-3.0 `Site/Views/Before/FrontDesk.cshtml` + `.cshtml.less` (`.NoBallot .Voter:not(.VM-)`)  
**Revisit when:** election setup exposes checklist flags as first-class mailed-ballot tracking

v3’s **Ballot Not Received** checkbox hid everyone whose voting method was set (ballot received / recorded). Combined with a checklist/flag, that showed who was flagged (for example mailed a ballot) but still had no method.

v4 already had All / Not registered / Registered radios and method/flag filters. Those cover the same workflow when the default is Not registered, but they did not name the v3 control or apply it on the All list.

The checkbox is an extra filter on the current list: hide rows with a voting method. Flag filters still apply. Clearing filters turns it off.

## Front Desk method codes and mixed-method switch

**Status:** active  
**Evidence:** inferred (issue #194; Person.VotingMethod varchar(1); reports/analyzer already used P/M/D/O/K/I)  
**Revisit when:** election setup exposes VotingMethods as first-class checkboxes instead of a string

`Person.VotingMethod` is a single letter: **P** in person, **M** mailed, **D** dropped off, **C** called in, **O** online, **K** kiosk, **I** imported, **1/2/3** custom. Election setup may store the same letters concatenated (`PMD`) or comma-separated aliases (`IP,OL`). Front Desk buttons come from that string except **O** — Online is voter-initiated, so tellers cannot check anyone in as Online (that would count a Front Desk registration with no ballot). Empty setup defaults to P/M/D. The English word “In Person” is not code `I` — `I` is Imported.

Online ballots stay voter-initiated. Tellers do not create ballots at the Online location. Checking in with a method other than Online while the person has a **Draft** or **Submitted** online row discards that pending row so Accept-all cannot create a second ballot. **Processed** (and **Processing**) online already means they have an accepted/claimed online ballot — Front Desk refuses another method. Draft is not Accept-all pending and does not lock cannot-vote.

**Rejected alternative:** let tellers record `O` at Front Desk when election setup lists Online. Rejected — that marks the person as voted with no Ballot, breaks Front Desk vs ballots reconciliation, and contradicts voter-initiated online.

**Rejected alternative:** keep Front Desk writing `I` for in person. Rejected — reports and analysis already count `I` as Imported and `P` as In Person.

**Rejected alternative:** leave a Submitted row in place after an in-person/mail/kiosk check-in. Rejected — Analyze stays blocked while any live Submitted exists, and Accept-all could mint a second ballot.

**Reason:** mixed-method elections need the same letters everywhere, and a switch of method must not count two ballots.

**Rejected alternative:** treat the Not registered radio as enough and skip the named checkbox. Rejected — #171 names the control, and operators combine it with flags on the full list.

## Roll Call and envelope pages are not v4 product pages

**Status:** active  
**Evidence:** inferred (v3 menu vs v4 routes; `docs/Hubs-v3-vs-v4.md`)  
**Source:** TallyJ-3.0 `Site/Views/menu.xml` (Roll Call / Sort Envelopes require `BallotProcess=Roll`); v4 has no `BallotProcess` setting  
**Revisit when:** a projector roll-call display or envelope-sort workflow is requested

v3 Roll Call and Sort Envelopes existed only for the Roll ballot process. Count Envelopes was `Ballots/Reconcile`. v4 has no Roll process, no Front Desk roll-call or envelope-count routes, and **RollCallHub is deferred**.

`GET .../frontdesk/rollCall` is a leftover that returns eligible voters plus check-in stats. Envelope numbers live on Front Desk (`ENABLE_ENVELOPE_NUMBERS` is still off). Envelope *counting* is the Analyze count-reconciliation report, not a Gathering page.

**Rejected alternative:** rebuild the v3 projector and envelope pages for #171. Rejected — they depended on a process v4 does not have; the remaining API is covered by tests, not a new UI.
