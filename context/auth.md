# Auth

## JWT user id claim lookup on .NET 10

**Status:** active  
**Evidence:** confirmed  
**Source:** project agent notes (`AGENTS.md`)  
**Revisit when:** identity/JWT middleware or claim mapping changes, or .NET major upgrade changes claim defaults again

User IDs are stored in JWT `sub` claims. Code that reads the current user ID must check both:

```csharp
User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value
```

**Reason:** on .NET 10, claim type mapping for the subject claim is not always the same as older stacks; reading only `ClaimTypes.NameIdentifier` or only `"sub"` fails depending on token and middleware configuration.

**Rejected alternative:** assume a single claim type everywhere. That breaks in one of the two common configurations and produces hard-to-spot auth bugs (null user id, wrong scoping).
