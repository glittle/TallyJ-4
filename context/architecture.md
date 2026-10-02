# Architecture

## Single backend host after domain consolidation

**Status:** active  
**Evidence:** confirmed  
**Source:** project agent notes (`AGENTS.md`); domain consolidation, May 2025  
**Revisit when:** a second C# application project is introduced, or domain is split out again

The previous `Backend.Domain` and `Backend.Application` projects were fully merged into the `backend/` host project. There is effectively one C# application project plus `Backend.Tests/` and the Vue SPA under `frontend/`.

**Reason:** agents and contributors kept assuming a multi-project layered layout that no longer exists. Feature work (controllers, services, DTOs, entities, validators, Mapster profiles, SignalR hubs, EF) almost always belongs in `backend/`. DI registration for new services goes in `backend/Program.ServiceRegistration.cs` (`ProgramServiceRegistration.RegisterApplicationServices`, `RegisterAuthServices`, `RegisterBackgroundServices`).

**Rejected alternative:** keep or reintroduce separate Domain/Application class libraries. Rejected for this codebase’s current size and ownership model — the split had become dead weight and produced duplicate or stale guidance.

## Social preview URLs are static and pinned to production

**Status:** active  
**Evidence:** confirmed  
**Source:** production host in `ClientEnvResolver` tests (`https://v4.tallyj.com`); UAT host `https://uat.v4.tallyj.com`; `MapFallbackToFile` serves `index.html` for unknown routes  
**Revisit when:** v4 production moves off `v4.tallyj.com`, or UAT and production stop sharing one frontend build

Link previews (LinkedIn, Facebook, Slack, X) read `frontend/index.html`. They do not run the SPA. `/` is that file via static files; deep links are the same file via `MapFallbackToFile`, so the head does not vary by route. `og:url` and `og:image` are absolute `https://v4.tallyj.com` URLs in source. The Azure frontend pipeline rewrites that origin to the UAT host only in the UAT zip.

**Reason:** one Vite build is copied to UAT and production, and a relative image URL is not reliable for those crawlers. Pinning production means an unmodified `frontend/dist` is correct for production. UAT still previews, because the pipeline that actually deploys there substitutes the origin before packaging.

**Rejected alternative:** set the tags from Vue after load. Crawlers would publish an empty card.

**Rejected alternative:** bake the UAT origin into source. Production shares would point crawlers at UAT.

### Practical layout (where to look)

- **Data / persistence:** `backend/Context/MainDbContext.cs`, `Entities/`, `Enumerations/`, `Identity/`, `Interfaces/`
- **Auth:** `DTOs/Auth/`, `Services/Auth/`, related controllers and validators
- **Domain features:** `Controllers/`, `DTOs/`, `Services/`, `Validators/`, `Mappings/`, `Hubs/`
