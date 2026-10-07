using Backend.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Backend.Authorization;

/// <summary>
/// How an action identifies the election when the route is not the election guid.
/// </summary>
public enum ElectionResourceKind
{
    /// <summary>Route or argument guid of a person.</summary>
    PersonGuid,

    /// <summary>Route or argument guid of a ballot.</summary>
    BallotGuid,

    /// <summary>Route or argument row id of a vote.</summary>
    VoteId,

    /// <summary>Route, argument, or body <c>LocationGuid</c>.</summary>
    LocationGuid,

    /// <summary>Route or argument teller row id.</summary>
    TellerId,

    /// <summary>Body <c>ElectionGuid</c>, or an action argument that is itself a <see cref="Guid"/>.</summary>
    BodyElectionGuid,

    /// <summary>Body <c>BallotGuid</c> (create or reorder a vote).</summary>
    BodyBallotGuid
}

/// <summary>
/// Resolves an entity or body guid to an election and authorizes that election.
/// Entity and body checks return 404 when the caller has no access, so the
/// response matches "not found" and does not confirm the guid.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RequireElectionAccessAttribute : Attribute, IFilterFactory, IOrderedFilter
{
    /// <summary>
    /// Before <c>ModelStateInvalidFilter</c> (-2000) so a cross-election body
    /// is 404 even when other fields would have been 400. The pipeline orders
    /// by this attribute, not by the filter instance created later.
    /// </summary>
    public int Order => -2500;

    public RequireElectionAccessAttribute(ElectionResourceKind kind)
    {
        Kind = kind;
    }

    public ElectionResourceKind Kind { get; }

    public string Policy { get; set; } = ElectionAccessPolicies.ElectionAccess;

    /// <summary>
    /// Action argument name. When empty, the filter uses the usual name for <see cref="Kind"/>.
    /// </summary>
    public string Argument { get; set; } = "";

    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        return new RequireElectionAccessFilter(
            serviceProvider.GetRequiredService<IAuthorizationService>(),
            serviceProvider.GetRequiredService<MainDbContext>(),
            Kind,
            Policy,
            Argument);
    }
}

/// <summary>
/// Runs before model-state short-circuit so a cross-election body is 404
/// even when other fields would have been 400.
/// </summary>
public sealed class RequireElectionAccessFilter : IAsyncActionFilter, IOrderedFilter
{
    private readonly IAuthorizationService _authorization;
    private readonly MainDbContext _db;
    private readonly ElectionResourceKind _kind;
    private readonly string _policy;
    private readonly string _argument;

    public RequireElectionAccessFilter(
        IAuthorizationService authorization,
        MainDbContext db,
        ElectionResourceKind kind,
        string policy,
        string argument)
    {
        _authorization = authorization;
        _db = db;
        _kind = kind;
        _policy = policy;
        _argument = argument;
        Order = -2500;
    }

    public int Order { get; }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var electionGuid = await ResolveElectionGuidAsync(context);
        if (electionGuid == null)
        {
            context.Result = NotFoundResult();
            return;
        }

        var allowed = await _authorization.AuthorizeAsync(context.HttpContext.User, electionGuid.Value, _policy);
        if (!allowed.Succeeded)
        {
            context.Result = NotFoundResult();
            return;
        }

        await next();
    }

    private async Task<Guid?> ResolveElectionGuidAsync(ActionExecutingContext context)
    {
        switch (_kind)
        {
            case ElectionResourceKind.PersonGuid:
            {
                if (!TryReadGuid(context, FirstName("guid"), out var personGuid))
                {
                    return null;
                }

                return await _db.People.AsNoTracking()
                    .Where(p => p.PersonGuid == personGuid)
                    .Select(p => (Guid?)p.ElectionGuid)
                    .FirstOrDefaultAsync();
            }
            case ElectionResourceKind.BallotGuid:
            {
                if (!TryReadGuid(context, FirstName("guid"), out var ballotGuid))
                {
                    return null;
                }

                return await BallotElectionAsync(ballotGuid);
            }
            case ElectionResourceKind.BodyBallotGuid:
            {
                if (!TryReadPropertyGuid(context, "BallotGuid", out var ballotGuid))
                {
                    return null;
                }

                return await BallotElectionAsync(ballotGuid);
            }
            case ElectionResourceKind.VoteId:
            {
                if (!TryReadInt(context, FirstName("id"), out var voteId))
                {
                    return null;
                }

                return await (
                    from vote in _db.Votes.AsNoTracking()
                    join ballot in _db.Ballots.AsNoTracking() on vote.BallotGuid equals ballot.BallotGuid
                    join location in _db.Locations.AsNoTracking() on ballot.LocationGuid equals location.LocationGuid
                    where vote.RowId == voteId
                    select (Guid?)location.ElectionGuid).FirstOrDefaultAsync();
            }
            case ElectionResourceKind.LocationGuid:
            {
                if (!TryReadGuid(context, FirstName("locationGuid"), out var locationGuid)
                    && !TryReadPropertyGuid(context, "LocationGuid", out locationGuid))
                {
                    return null;
                }

                return await _db.Locations.AsNoTracking()
                    .Where(l => l.LocationGuid == locationGuid)
                    .Select(l => (Guid?)l.ElectionGuid)
                    .FirstOrDefaultAsync();
            }
            case ElectionResourceKind.TellerId:
            {
                if (!TryReadInt(context, FirstName("rowId"), out var rowId))
                {
                    return null;
                }

                return await _db.Tellers.AsNoTracking()
                    .Where(t => t.RowId == rowId)
                    .Select(t => (Guid?)t.ElectionGuid)
                    .FirstOrDefaultAsync();
            }
            case ElectionResourceKind.BodyElectionGuid:
            {
                if (TryReadGuid(context, string.IsNullOrEmpty(_argument) ? "electionGuid" : _argument, out var direct)
                    && context.ActionArguments.Values.Any(v => v is Guid))
                {
                    return direct;
                }

                if (!TryReadPropertyGuid(context, "ElectionGuid", out var fromBody))
                {
                    return null;
                }

                return fromBody;
            }
            default:
                return null;
        }
    }

    private async Task<Guid?> BallotElectionAsync(Guid ballotGuid)
    {
        return await (
            from ballot in _db.Ballots.AsNoTracking()
            join location in _db.Locations.AsNoTracking() on ballot.LocationGuid equals location.LocationGuid
            where ballot.BallotGuid == ballotGuid
            select (Guid?)location.ElectionGuid).FirstOrDefaultAsync();
    }

    private string FirstName(string fallback) =>
        string.IsNullOrEmpty(_argument) ? fallback : _argument;

    private static bool TryReadGuid(ActionExecutingContext context, string name, out Guid guid)
    {
        guid = Guid.Empty;
        if (!context.ActionArguments.TryGetValue(name, out var value) || value is not Guid parsed || parsed == Guid.Empty)
        {
            return false;
        }

        guid = parsed;
        return true;
    }

    private static bool TryReadInt(ActionExecutingContext context, string name, out int id)
    {
        id = 0;
        if (!context.ActionArguments.TryGetValue(name, out var value) || value is not int parsed)
        {
            return false;
        }

        id = parsed;
        return true;
    }

    private static bool TryReadPropertyGuid(ActionExecutingContext context, string propertyName, out Guid guid)
    {
        foreach (var value in context.ActionArguments.Values)
        {
            if (value == null)
            {
                continue;
            }

            var property = value.GetType().GetProperty(propertyName);
            if (property?.PropertyType == typeof(Guid) && property.GetValue(value) is Guid parsed && parsed != Guid.Empty)
            {
                guid = parsed;
                return true;
            }
        }

        guid = Guid.Empty;
        return false;
    }

    private static NotFoundObjectResult NotFoundResult() =>
        new(new { error = "error.notFound" });
}
