# Paid sends and voter-list abuse

## Owner paid-send gate (issue #371 slice 3a)

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #371 comments of 2026-10-06 (owner abuse, flagged-upload threshold, block Online)  
**Revisit when:** a second paid provider is added, or email codes should also wait for approval

An owner who creates an election can upload any phone list and then trigger Twilio (SMS and voice) or GreenAPI (WhatsApp). Checking the request against that list does not help, because the attacker owns the list.

SMS, voice, and WhatsApp login codes wait until a super admin approves every owner and admin on the election. The flag is `OwnerSendControls.PaidSendsApproved`. Email codes are not gated by that approval and are not counted against the caps. Voice is included because it is a paid Twilio channel, same as SMS.

The owner is a `JoinElectionUsers` row with role `Owner` or `Admin`. `CreateElection` stores `Admin`, not `Owner`. An election with no such row cannot send a paid code. When several owners or admins exist, paid codes go out only if every one of them is approved. The send is counted against the first of those accounts that is still under its daily cap. One approved account does not carry an unapproved co-owner.

A blocked `requestCode` returns the same phrase key as a successful send (`voting.auth.requestCode.sent`), with no verification code and no delivery-status channel. The election screen shows `elections.paidChannels.*` so the owner can see why SMS, voice, and WhatsApp are unavailable.

**Grandfather:** migration `20261007052253_AddPaidSendControls` inserts an approved `OwnerSendControls` row, once, for each `Owner` or `Admin` on an election that already has `UseOnlineVoting`, and for a user whose email matches `Elections.OwnerLoginId` on such an election (`TRY_CAST` of `AspNetUsers.Id`). A later restart does not re-approve anyone. Seed data calls `ApproveSeededOnlineOwnersIfMissingAsync` only for `SpringfieldLSA2024`, `OnlineVotingRandom2024`, and `OnlineVotingBoth2024`, and only inserts a row when that user has none. A super admin who clears approval is not undone by seed.

**Rejected alternative:** approve every owner who turns on online voting, including on the next process start. That would approve a new attacker election after a restart and would undo a super-admin revoke.

**Rejected alternative:** let a paid send proceed when any one owner or admin is approved. An unapproved account added as co-owner would keep sending, and an approved account added onto an unapproved election would do the same.

## Caps and the kill switch

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #371 slice 3a  
**Revisit when:** the daily cap should follow the election timezone instead of UTC

Each election has a free SMS/voice/WhatsApp allowance (`AntiAbuse:ElectionPaidSendAllowance`, default 25). Each owner has a daily cap across their elections (`AntiAbuse:OwnerDailyPaidSendCap`, default 100). The daily row is keyed by UTC date. Raising the cap unblocks the same day. The next UTC day starts at 0. A super admin raises an allowance or daily cap only to a number strictly above the one in effect. The config default counts as the current cap when no override is stored.

Counters are `ElectionSendControls.PaidSendsUsed` and `OwnerDailyPaidSends.SendCount`. On SQL Server the increment is one `UPDATE ... OUTPUT` that matches only while the count is under the limit, the same shape as `TellerLoginLockoutService`. The slot is taken before the provider is called. A failed provider send is not refunded. If the election slot is taken and the owner day is already full, the election slot is given back.

`SendsFrozen` on the election stops every login code, including email. `SendsFrozen` on an owner or admin does the same for every election they belong to, even when another owner or admin on that election is not frozen. The voter still sees the neutral sent reply.

Every outcome is a `CodeSendLogs` row: election, owner, channel, masked destination, and outcome. `SmsLog` stays the phone-delivery log; it has no owner or channel column.

## Alerts

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #371; Sentry ASP.NET Core default `MinimumEventLevel` is Error  
**Revisit when:** Sentry is configured to capture warnings as events

A flagged election, a cap hit, and an owner's first paid send email `AntiAbuse:AlertEmails`, or `SuperAdmin:Emails` when that list is empty. The From address is `Email:FromAddress` and the From name is `Email:FromName`, the same pair the other mail uses. A recipient that is not an email address is skipped and logged. An alert failure, including a lost throttle-row insert, is logged and does not fail `requestCode`. The same events call `SentrySdk.CaptureMessage` at warning. An `ILogger` warning is only a Sentry breadcrumb with the default event level, so the explicit capture is what becomes an event. `AbuseAlertStates` keeps one row per alert key and skips another email inside `AntiAbuse:AlertThrottleHours` (default 24). The first paid send alerts once per owner. A cap alerts once per election, or once per owner per UTC day. The logged destination is masked.

**Rejected alternative:** set the global Sentry minimum event level to Warning. Ordinary warnings would become events.

## Voter-list flag

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #371 slice 3a  
**Revisit when:** send-time prefix limits are added, or the disposable-domain file is refreshed

Every path that writes people rechecks the election's phones and emails: CSV/Excel import, JSON import, v2/v3 package import, Duplicate, and manual add or edit. `libphonenumber` flags a number that does not parse, and a valid number whose region is not in `Elections.ExpectedPhoneRegions`. When that column is empty the check uses `AntiAbuse:DefaultPhoneRegionCode` (default `CA`). A run of `AntiAbuse:ConsecutivePhoneRunLength` sequential valid numbers (default 4) flags each number in the run. An email is flagged when its domain is in `backend/Data/disposable-email-domains.txt` (source note is the first lines of that file) or when the domain has no MX or only the Null MX from RFC 7505. An MX timeout or DNS failure is unknown and is not flagged. Domains are looked up once per review, at most `AntiAbuse:MxLookupParallelism` at a time (default 8), and successful answers are cached for six hours.

Active rows are stored in `VoterContactFlags` (masked value, reason, file row number). The election is flagged when the number of distinct contacts is greater than `AntiAbuse:FlaggedEntryThreshold` (default 3). Flagging writes `ElectionFlagged`, emails the super admin with the masked rows, and sets `ElectionSendControls.Flagged`. That stops online voting and every login code (email, SMS, voice, and WhatsApp). The owner cannot turn `UseOnlineVoting` on or save an open online window (`elections.onlineVotingSuspended`). A window that is already open stays on the row, but `requestCode` sends nothing, sign-in is refused with `voting.auth.noOpenElections` when every matching open election is flagged, and ballot submit returns `voting.submit.notOpen`. Teller ballot entry is unchanged. Ballots already stored are not deleted. Clearing the flag deactivates the rows and writes `ElectionFlagCleared`. The next review flags the election again if the list is still over the threshold.

**Rejected alternative:** treat a DNS timeout as no MX. A slow resolver would flag real addresses.

**Rejected alternative:** auto-clear the flag when a later review is under the threshold. Only a super admin clears it.
