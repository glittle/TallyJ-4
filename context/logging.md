# Logging

## Unmatched probe requests are counted, then summarized

**Status:** active  
**Evidence:** confirmed  
**Revisit when:** the SPA history fallback stops being the handler for non-API routes, or probe 404s need to be stored outside the process

Bot traffic requests paths that are not TallyJ endpoints. Logging each miss as its own warning or error, and sending each one to Sentry, hides real failures.

A request is unmatched only when the pipeline never ran an endpoint (framework 404, `HttpContext.Items["__RequestUnhandled"]`) or ASP.NET Core's method-rejection endpoint ran (405, display name `405 HTTP Method Not Supported`). Those hits are counted in memory per path: lowercase with the invariant culture, query and fragment removed, duplicate slashes collapsed. At most 200 distinct paths are kept; further paths and paths longer than 300 characters fold into an `other` bucket. Every five minutes, and again on shutdown, one Information line is written if the window was not empty: `Unmatched request summary (last 5 min): /index (5 calls), /account/billing (2 calls)`, highest count first. The hosting "request was not handled" line is filtered out. Warning and error logs emitted while that request is finishing are filtered out. A Sentry `BeforeSend` hook drops a non-exception event tagged for that request.

**Reason:** the noise is the path, not the individual call. A fixed-size counter is enough to see which probes are active without a database or a log line per hit. Invariant case folding matters on Windows hosts whose culture would lowercase `I` differently.

**Rejected alternative:** log every unmatched 404 at warning or error. That is the v3 noise this replaces.

**Rejected alternative:** treat every HTTP 404 as unmatched. Controller actions return 404 when an election, person, or similar row is missing. Those responses are handled by a matched endpoint and stay on the normal log and Sentry path. An exception that ends as 404 is an application error and is not counted.

**Rejected alternative:** count client routes such as `/account/billing`, `/index`, or `/wp-login.php`. `MapFallbackToFile` is the endpoint for those URLs (anything that is not `api/`, `systemhealth`, `hubs/`, `assets/`, `config.json`, or `og-image`). It returns the SPA shell so Vue Router can run. Counting it would record ordinary teller and voter navigation. Paths the fallback deliberately does not cover, such as `/systemhealth` or `/api/account/billing`, are the probes.

**Rejected alternative:** a denylist of known scanner paths. New probe URLs would still log one by one, and the denylist would fight the SPA fallback.
