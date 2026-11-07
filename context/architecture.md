# Architecture

## Single backend host after domain consolidation

**Status:** active  
**Evidence:** confirmed  
**Source:** project agent notes (`AGENTS.md`); domain consolidation, May 2025  
**Revisit when:** a second C# application project is introduced, or domain is split out again

The previous `Backend.Domain` and `Backend.Application` projects were fully merged into the `backend/` host project. There is effectively one C# application project plus `Backend.Tests/` and the Vue SPA under `frontend/`.

**Reason:** agents and contributors kept assuming a multi-project layered layout that no longer exists. Feature work (controllers, services, DTOs, entities, validators, Mapster profiles, SignalR hubs, EF) almost always belongs in `backend/`. DI registration for new services goes in `backend/Program.ServiceRegistration.cs` (`ProgramServiceRegistration.RegisterApplicationServices`, `RegisterAuthServices`, `RegisterBackgroundServices`).

**Rejected alternative:** keep or reintroduce separate Domain/Application class libraries. Rejected for this codebase’s current size and ownership model — the split had become dead weight and produced duplicate or stale guidance.

## Published process, not container images

**Status:** active  
**Evidence:** confirmed (maintainer: the image and compose definitions were never tested)  
**Revisit when:** a container deployment is actually tested and adopted

The app is hosted as a published ASP.NET Core process, not as a container image. Hosted UAT is Linux Azure App Service (`dotnet Backend.dll` on `tallyj4-uat`). Self-hosting is Windows IIS (`backend/web.config`) or the same process on Linux behind nginx or Apache (`DEPLOYING_LOCALLY.md`). SQL Server is installed or reached as its own server. This repository does not include image or compose definitions.

**Rejected alternative:** keep those untested image and compose definitions, including an nginx config that existed only to serve the frontend image, as a starting point. Rejected — they were never tested, the backend image still targeted a legacy `TallyJ4` assembly name, and the supported IIS and Linux publish path does not use them.

## UAT zip deploy is one combined site

**Status:** active  
**Evidence:** confirmed  
**Source:** `.azure/pipelines/templates/deploy-uat-appservice.yml`; `scripts/deployment/select-sibling-run.py` (Builds List returns `sourceVersion` and does not filter on it)  
**Revisit when:** hosted UAT leaves `tallyj4-uat`, zip deploy stops replacing the whole site, or the pipelines start deploying production

Hosted UAT is the Linux App Service web app `tallyj4-uat` (resource group `TallyJ3`) at https://uat.v4.tallyj.com. The two Azure DevOps pipelines still build only their own half (backend definition 3 publishes `backend_pkg` / `Backend.zip`; frontend definition 4 publishes `frontend_pkgs` / `frontend.zip`). Each deploy puts the frontend dist inside the backend publish `wwwroot/` and zip-deploys that one package with `AzureWebApp@1`. The shared steps are `.azure/pipelines/templates/deploy-uat-appservice.yml`. The app's own settings stay on the site, including the startup command `dotnet Backend.dll`. These pipelines deploy UAT only. Self-hosting on IIS or Linux is unchanged.

A push can start both pipelines (shared `version.json` trigger, or both trees in one commit). Each deploy looks up the other definition's run for `Build.SourceVersion`. While that run is queued or in progress, it polls until the artifact exists (30 seconds, 40 minutes), then downloads that run by id. It does not wait for the other deploy to finish. If that run failed, timed out, or never published the artifact, it warns and uses the latest **succeeded** run on `main`. A run whose build published an artifact and whose deploy then failed is result `failed`, so it is not that latest success. `scripts/deployment/select-sibling-run.py` is the lookup. `lockBehavior: sequential` is on the `DeployUat` stage (the deployment-job schema has no such property). That lock queues later runs of the same pipeline. It does not span the two pipelines.

**Reason:** App Service zip deploy replaces the whole site. The IIS layout deployed the backend to the site and the frontend dist into that site's `wwwroot` as a second package. A second zip deploy would delete whichever half was not in the latest zip. The SPA is served from `wwwroot` next to `Backend.dll`. Pairing on the commit is required because `latestFromBranch` only sees a completed success, so overlapping deploys would each package the other half's previous artifact and the last upload would leave one half stale.

**Rejected alternative:** deploy each pipeline's zip on its own, as the IIS virtual-application deploy did. The later deploy would wipe the earlier one.

**Rejected alternative:** a third pipeline that only combines and deploys. Each existing pipeline already runs when its half changes, and either change has to publish a full site. The template is the shared part.

**Rejected alternative:** send app settings or `startUpCommand` from the deploy task. The site already has them. A deploy that rewrites them can clear the startup command or the host configuration.

**Rejected alternative:** an Exclusive Lock check on the `tallyj4-uat` environment. That check is created on the environment, and the pipeline token cannot manage environments. `lockBehavior` alone does not create a cross-pipeline lock.

**Rejected alternative:** wait until the other run's status is completed, then download it. Each deploy is part of the run the other side is waiting on, so both sides sit until the 40-minute timeout and then fall back to the previous success.

## SQL retries and single-query splitting

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #410  
**Revisit when:** the host stops using SQL Server, or startup migrations move out of the process

Every `UseSqlServer` registration calls `EnableRetryOnFailure` and `UseQuerySplittingBehavior(SingleQuery)`. Single query is what the app already did when the mode was unset. `SuperAdminService` is the query that calls `AsSplitQuery()`; that call still overrides the default. Setting the mode explicitly keeps that behavior and stops the EF warning that fires when the mode is unset.

Startup migrate and seed run inside `Database.CreateExecutionStrategy().ExecuteAsync`. The SQL Server strategy rejects a user transaction started outside that delegate, so each `BeginTransactionAsync` path runs inside the strategy as well. SQLite tests use a non-retrying strategy; the wrapper does not add retries there.

**Rejected alternative:** `QuerySplittingBehavior.SplitQuery` as the default. The rest of the queries were written as one round trip. Changing them was not required to silence the warning.

**Rejected alternative:** enable retries but leave `MigrateAsync` and `SeedAsync` outside the strategy. The migration's own transaction then fails the startup check.

## Controller registrations are resolved in CI

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #410 — `POST /api/report-exports/{electionGuid}` returned 500 because `IReportExportService` was not registered  
**Revisit when:** controllers stop being constructed from the application service collection

`IReportExportService` is registered with the other application services. A test builds that service collection and activates every controller, so a missing registration fails before deploy. PDF export uses iText's built-in Helvetica (`StandardFonts`). Excel export uses ClosedXML 0.105, which measures column widths with its embedded Carlito metric font when the host has no system font such as Microsoft Sans Serif. Neither path uses System.Drawing.

**Rejected alternative:** ship a Windows font or take a dependency on System.Drawing / libgdiplus. Both libraries already draw without them.

## Social preview URLs are static and pinned to production

**Status:** active  
**Evidence:** confirmed  
**Source:** production host in `ClientEnvResolver` tests (`https://v4.tallyj.com`); UAT host `https://uat.v4.tallyj.com`; `MapFallbackToFile` serves `index.html` for unknown routes  
**Revisit when:** v4 production moves off `v4.tallyj.com`, or UAT and production stop sharing one frontend build

Link previews (LinkedIn, Facebook, Slack, X) read `frontend/index.html`. They do not run the SPA. `/` is that file via static files; deep links are the same file via `MapFallbackToFile`, so the head does not vary by route. `og:url` and `og:image` are absolute `https://v4.tallyj.com` URLs in source. The Azure frontend pipeline rewrites that origin to the UAT host in `dist/index.html` before it publishes `frontend.zip`. The UAT deploy copies that dist into `wwwroot` of the combined App Service package.

The rewrite (`frontend/scripts/set-og-origin.mjs`) changes `og:url`, `og:image`, `twitter:url`, and `twitter:image` only when `new URL(content).origin` is exactly `https://v4.tallyj.com`. A second run whose tags are already the target origin leaves the file unchanged.

**Reason:** one Vite build is copied to UAT and production, and a relative image URL is not reliable for those crawlers. Pinning production means an unmodified `frontend/dist` is correct for production. UAT still previews, because the pipeline that actually deploys there substitutes the origin before packaging. Comparing parsed origins keeps a comment, a non-URL meta value, and a lookalike host (one that only begins with the production origin) out of the substitution. A substring check of the raw origin is the CodeQL incomplete-URL-sanitization finding.

**Rejected alternative:** set the tags from Vue after load. Crawlers would publish an empty card.

**Rejected alternative:** bake the UAT origin into source. Production shares would point crawlers at UAT.

**Rejected alternative:** replace every occurrence of the production origin string. That rewrites comments and lookalike hosts, and the presence check is a URL substring test.

### Practical layout (where to look)

- **Data / persistence:** `backend/Context/MainDbContext.cs`, `Entities/`, `Enumerations/`, `Identity/`, `Interfaces/`
- **Auth:** `DTOs/Auth/`, `Services/Auth/`, related controllers and validators
- **Domain features:** `Controllers/`, `DTOs/`, `Services/`, `Validators/`, `Mappings/`, `Hubs/`
