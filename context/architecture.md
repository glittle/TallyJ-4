# Architecture

## Single backend host after domain consolidation

**Status:** active  
**Evidence:** confirmed  
**Source:** project agent notes (`AGENTS.md`); domain consolidation, May 2025  
**Revisit when:** a second C# application project is introduced, or domain is split out again

The previous `Backend.Domain` and `Backend.Application` projects were fully merged into the `backend/` host project. There is effectively one C# application project plus `Backend.Tests/` and the Vue SPA under `frontend/`.

**Reason:** agents and contributors kept assuming a multi-project layered layout that no longer exists. Feature work (controllers, services, DTOs, entities, validators, Mapster profiles, SignalR hubs, EF) almost always belongs in `backend/`. DI registration for new services goes in `backend/Program.ServiceRegistration.cs` (`ProgramServiceRegistration.RegisterApplicationServices`, `RegisterAuthServices`, `RegisterBackgroundServices`).

**Rejected alternative:** keep or reintroduce separate Domain/Application class libraries. Rejected for this codebase’s current size and ownership model — the split had become dead weight and produced duplicate or stale guidance.

### Practical layout (where to look)

- **Data / persistence:** `backend/Context/MainDbContext.cs`, `Entities/`, `Enumerations/`, `Identity/`, `Interfaces/`
- **Auth:** `DTOs/Auth/`, `Services/Auth/`, related controllers and validators
- **Domain features:** `Controllers/`, `DTOs/`, `Services/`, `Validators/`, `Mappings/`, `Hubs/`
