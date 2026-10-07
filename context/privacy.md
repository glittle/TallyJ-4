# Privacy of page loads and voter screens

## Lora is self-hosted

**Status:** active  
**Evidence:** confirmed  
**Revisit when:** the landing page stops using Lora, or a font host is added that does not receive the visitor IP

The landing page uses Lora. Loading it from Google's font servers sends every visitor's IP address to Google. A Munich court ruling in 2022 treated that as a GDPR disclosure. The faces are the same ones the page already asked for: Lora 400, 500, 600, and 700, normal and italic, `font-display: swap`, family name `Lora`. They come from `@fontsource/lora` in `main.ts`. `tokens.less` and `style.css` no longer point at `fonts.googleapis.com` or `fonts.gstatic.com`.

**Rejected alternative:** keep the CSS `@import` and only self-host the font files. The stylesheet request itself is the disclosure, and the frontend build was also failing when that fetch flaked.

## Session Replay stays off online-voter routes

**Status:** active  
**Evidence:** confirmed  
**Revisit when:** voter ballot choices are no longer a list of names in a stable order, or Session Replay is removed

Teller and admin routes still use Session Replay (session sample 0.1 in production, 1 in development, on-error sample 1) with text, inputs, and media masked. Online-voter routes do not: sign-in (`/voter-auth`), election list (`/voter-elections`), ballot (`/vote/:electionId`), confirmation (`/vote-confirmation`). Kiosk voting is those same pages with `?kiosk=1`. Vote status is shown there, not on its own route. Teller ballot entry (`/elections/:id/ballots/:ballotId/entry`) is not a voter route.

Replay is omitted from `Sentry.init`, so a direct load of a voter URL does not start a session or an error buffer. The first teller or admin navigation installs `replayIntegration`. Entering a voter route calls `stop({ flush: false })` before that page renders, which ends the replay and discards the pending segment. A buffer that started on a teller page therefore cannot keep recording the ballot, and an error on the voter page cannot upload one. Masking stays explicit (`maskAllText`, `maskAllInputs`, `blockAllMedia`) because it is not sufficient on its own: click positions over a name list in the same order can still show who was selected.

**Rejected alternative:** leave sample rates above 0 and only stop inside a route guard. The SDK would buffer as soon as the page loaded, including a voter deep link, and an on-error sample could upload that buffer.

**Rejected alternative:** call `stop()` without `{ flush: false }`. In session mode the default stop uploads the pending segment. Discarding it avoids attaching voter-page events to a replay that started earlier.
