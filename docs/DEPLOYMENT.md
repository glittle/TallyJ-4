# Deployment guide

This is the canonical deployment document for the current repository.

## Supported deployment shape

TallyJ 4 is deployed as two pieces:

1. SQL Server database (installed or hosted on its own; Windows or Linux)
2. ASP.NET Core backend — it serves both the API and the compiled frontend static files

Hosted UAT runs on Azure App Service (Linux), web app `tallyj4-uat` in resource group `TallyJ3`, at https://uat.v4.tallyj.com. The Azure DevOps pipelines deploy that site. They deploy UAT only.

Self-hosting the same published process is still supported: Windows IIS (`backend/web.config`) or Linux behind nginx or Apache. See `DEPLOYING_LOCALLY.md`. This repository does not ship container images or compose files.

## Pre-deployment checklist

### Backend

- `dotnet build` passes for `backend/Backend.csproj`
- database migrations are reviewed and ready to apply
- production secrets are stored outside source control
- CORS and frontend base URL are configured for the target domain
- optional integrations are configured only if needed

### Frontend

- `npm run check` passes
- `npm run test:run` passes
- production environment variables are set
- the built app points at the production backend URL

### Infrastructure

- SQL Server is reachable from the backend
- TLS termination is in place
- logs are collected
- database backup and restore procedures are tested

## Required backend configuration

The backend reads standard ASP.NET Core configuration sources. At minimum, set:

- `ConnectionStrings__TallyJ4`
- `Jwt__Key`
- `Jwt__Issuer`
- `Jwt__Audience`
- `ClientEnv__frontendUrl` (public SPA origin used in emails, OAuth redirects, and CORS)
- `ClientEnv__env` (Sentry/client environment advertised at `/clientEnv.json`. Set **`uat`** on UAT and **`production`** on production. If this is unset or still `development` outside Development/Testing, the host infers `uat` when `ClientEnv__frontendUrl` is `https://uat.v4.tallyj.com`, otherwise `production`. Local Development/Testing keep `development`.)

Common optional settings:

- `Google__ClientId`
- `Google__ClientSecret`
- `Email__*`
- `Twilio__*` (optional `Twilio__StatusCallbackUrl` is the full public `POST /api/Public/smsStatus` URL Twilio should call; otherwise `ClientEnv:apiUrl` or `ClientEnv:frontendUrl` plus that path)
- `GreenApi__*`
- `Telegram__*`
- `SuperAdmin__Emails`

Prefer environment variables, secret stores, or deployment-platform secret management.

## Required frontend configuration

At minimum, configure:

```env
VITE_API_URL=https://api.example.com
```

Optional variables such as `VITE_SENTRY_DSN` and analytics flags should be set only when those services are active.

## Backend deployment

### Publish

```bash
dotnet publish backend/Backend.csproj -c Release -o backend/publish
```

### Apply migrations

Run migrations against the production database before or during deployment:

```bash
dotnet ef database update --project backend/Backend.csproj
```

### Host behind a reverse proxy

The backend is a standard ASP.NET Core application and can be hosted behind IIS, nginx, or another reverse proxy. Windows IIS stays in-process (`backend/web.config`, `hostingModel="inprocess"`). Linux is the same published process on Azure App Service, or behind nginx or Apache. Hosted UAT uses Azure App Service. See [Hosted UAT](#hosted-uat-azure-app-service).

`ForwardedHeaders:TrustPlatformProxy` defaults to true. The app applies `X-Forwarded-For` and `X-Forwarded-Proto` from one hop only when the TCP peer is an infrastructure address: loopback (nginx or Apache on the same machine), RFC 1918, link-local `169.254.0.0/16` (Azure App Service's `169.254.129.1`, including the IPv4-mapped form), or IPv6 unique-local / link-local. A public client that connects directly is not a trusted proxy. IIS in-process (`hostingModel="inprocess"`) does not add `X-Forwarded-For`, so a browser cannot spoof the client IP used for rate limits and audit logs. Add a public load-balancer address under `ForwardedHeaders:KnownProxies` (an IP or a CIDR). Set `TrustPlatformProxy` to false to ignore the headers entirely. See `backend/appsettings.json`.

The important requirement is that the externally visible frontend origin and the backend CORS configuration agree.

## Frontend deployment

The backend serves the compiled frontend from `wwwroot` next to `Backend.dll`. That is the ASP.NET Core default web root (`UseStaticFiles` / `UseDefaultFiles` / `MapFallbackToFile`). Site type (Dev, UAT, or Prod) comes from the process command line and selects `appsettings.{SiteType}.json`. Static files always come from `wwwroot`. Each host gets its own publish directory.

### Build

```bash
cd frontend
npm ci

# Build (includes merging per-locale JSONs into bundled/ for production language support)
npm run build
```

### Copy assets into the backend publish output

After building, copy the contents of `frontend/dist/` into `wwwroot` inside the backend publish directory **before** deploying:

```bash
# From the repository root.
# UAT only — point the preview card at the UAT origin first.
# A production copy keeps https://v4.tallyj.com in index.html and skips this.
node frontend/scripts/set-og-origin.mjs https://uat.v4.tallyj.com frontend/dist/index.html

mkdir -p backend/publish/wwwroot
cp -a frontend/dist/. backend/publish/wwwroot/
```

Then deploy `backend/publish/` to the server. Hosted UAT does this inside the pipeline. See [Hosted UAT](#hosted-uat-azure-app-service).

### Social preview origin

`frontend/index.html` pins `og:url` and `og:image` at `https://v4.tallyj.com` (production). UAT is `https://uat.v4.tallyj.com`. Crawlers do not run JavaScript, so the origin is not chosen in the browser. A production publish of `frontend/dist` should keep those production URLs. The Azure frontend pipeline deploys only to UAT and, from the `frontend` directory, rewrites those URL attributes in `dist/index.html` before it zips the dist (`node scripts/set-og-origin.mjs`). The UAT deploy copies that dist into `wwwroot` of the combined site package. Do not deploy that UAT zip to production. See `frontend/README.md` (Social link previews) for how to refresh LinkedIn, Facebook, and X caches.

> **Why this matters:** if the frontend files are missing from `wwwroot/`, or are placed in a different directory, the backend will serve `index.html` as a fallback for all asset requests (`.js`, `.css`, etc.), which causes the application to fail silently in the browser.

## Hosted UAT (Azure App Service)

UAT is the Linux App Service web app `tallyj4-uat` in resource group `TallyJ3`, at https://uat.v4.tallyj.com. Zip deploy replaces the entire site, so each pipeline deploys one combined package: the backend publish output with the frontend `dist` contents in `wwwroot/`. The site keeps its own application settings. The startup command `dotnet Backend.dll` is already set on the app. The pipelines do not send app settings, configuration strings, a runtime stack, or a startup command.

Two pipelines, both push-to-`main` only (`pr: none`):

| Pipeline | Definition | Builds | Other half |
| --- | --- | --- | --- |
| `.azure/pipelines/azure-back-end-pipeline.yml` | 3 | `backend_pkg` (`Backend.zip`) | definition 4, artifact `frontend_pkgs` |
| `.azure/pipelines/azure-front-end-pipeline.yml` | 4 | `frontend_pkgs` (`frontend.zip` of `dist`) | definition 3, artifact `backend_pkg` |

The shared steps are `.azure/pipelines/templates/deploy-uat-appservice.yml`. The deploy stage runs on a Microsoft-hosted `ubuntu-latest` agent, in environment `tallyj4-uat` (no VM resource), and deploys with `AzureWebApp@1` (`azureSubscription: azure-tallyj`, `appType: webAppLinux`, `deploymentMethod: zipDeploy`). Build stages stay on `windows-latest`. Tests are unchanged. The deploy job timeout is 90 minutes so the wait below still fits.

Both pipelines trigger on `version.json`, and a push often changes `backend/**` and `frontend/**` together, so one commit can start both runs. Each deploy has to ship **both new halves**. Taking the other pipeline's latest completed success would still be the previous commit, and the zip that lands last would leave one half stale.

At deploy time the job lists the other definition's recent builds on this branch (`GET .../_apis/build/builds`, `queryOrder=queueTimeDescending`, `$top=50`, `api-version=7.1`) and keeps the ones whose `sourceVersion` equals `Build.SourceVersion`. The list API returns `sourceVersion` on each build and does not filter by it. The call uses the job OAuth token `System.AccessToken`. That variable is secret, so the step maps it to `SYSTEM_ACCESSTOKEN`. Other predefined variables (`System.CollectionUri`, `System.TeamProjectId`, `Build.SourceVersion`, `Build.SourceBranch`) are already in the process environment. The token can read builds in this project (the project build service, or the collection build service if job authorization scope is the collection). `DownloadPipelineArtifact@2` uses the same token. A 401 or 403 fails the deploy.

If that run is queued (`notStarted`, `postponed`) or in progress and has not published its artifact, the deploy polls every 30 seconds for up to 40 minutes. As soon as the Artifacts API lists the artifact, the deploy downloads **that run** (`buildType: specific`, `buildVersionToDownload: specific`, `pipelineId` set to the build id) even while the run is still in progress. Waiting for the other run to finish would deadlock, because each deploy is part of the run the other side would be waiting on. The artifact is available once the build job publishes it.

If that run has already failed or been canceled, the wait times out, the succeeded run has no artifact, or the specific download does not leave the zip, the deploy logs a warning and downloads the latest successful run of that definition on `refs/heads/main`. Latest successful means a completed run whose result is `succeeded`. `allowFailedBuilds` and `allowPartiallySucceededBuilds` are false. A run whose build job published an artifact and whose deploy job then failed has result `failed`, so the other pipeline does not pick it up as the latest successful run. A push that only triggers one pipeline finds no sibling run and uses that latest successful artifact. If that artifact has never been published, the deploy stops and names the missing zip.

`lockBehavior: sequential` is set on the `DeployUat` stage. The deployment-job schema has no `lockBehavior` property; Azure DevOps accepts it on the stage or the pipeline. A stage with an id and `lockBehavior` creates a lock for that stage, and `sequential` queues later runs of **this** pipeline instead of the default `runLatest`, which cancels them. The lock does not span the two pipelines. An Exclusive Lock check on the environment would, and these pipelines do not add that check.

The frontend build rewrites Open Graph URLs in `dist/index.html` to `https://uat.v4.tallyj.com` before it zips `dist`. Do not reuse that zip for production.

## Smoke tests after deployment

### Backend

- Open Swagger at `/swagger`
- verify login via `POST /api/Auth/login`
- verify an authenticated request such as election listing

### Frontend

- load the landing page
- log in with a valid account
- open an election
- verify at least one CRUD workflow relevant to your deployment

## Operational notes

- The backend currently exposes Swagger, but no dedicated health-check route is documented in the active API host
- Development seeding should remain disabled in production
- The frontend and backend share localization assets during development; production deployments should ensure the backend has access to the required locale files if server-side localization is used

## Keep this file current when

Update this document whenever any of the following changes:

- publish command or project names
- required environment variables
- CORS or frontend base URL behavior
- deployment topology (hosted UAT on Azure App Service, self-hosted IIS on Windows, or the published process behind nginx/Apache on Linux)
