using Backend.Entities;

namespace Backend.Helpers;

/// <summary>
/// Whether saved tie-break counts decide the seats a tie group still has to fill.
/// Results, the tie list, and reports use this so a stored Tied / Tie-break Required
/// bit left behind by an older analysis does not keep the group unresolved.
/// </summary>
public static class TieBreakResolution
{
    public readonly record struct GroupStatus(bool IsTied, bool TieBreakRequired);

    /// <summary>
    /// True when this group crosses a section boundary or sits inside Extra,
    /// so a runoff is required until the counts decide those positions.
    /// A tie that is only inside Elected or only inside Other does not need one.
    /// </summary>
    public static bool NeedsTieBreak(IReadOnlyList<Result> members)
    {
        var inTop = false;
        var inExtra = false;
        var inOther = false;

        foreach (var member in members)
        {
            switch (member.Section)
            {
                case "E":
                    inTop = true;
                    break;
                case "X":
                    inExtra = true;
                    break;
                case "O":
                    inOther = true;
                    break;
            }
        }

        var onlyInTop = inTop && !(inExtra || inOther);
        var onlyInOther = inOther && !(inTop || inExtra);
        return !(onlyInTop || onlyInOther);
    }

    /// <summary>
    /// True when no two members still share a tie-break count (unset counts as 0)
    /// across a section boundary or inside Extra. That is enough to place the
    /// seats this group has to fill. Equal counts inside Elected do not block it.
    /// </summary>
    public static bool CountsDecidePositions(IReadOnlyList<Result> members)
    {
        foreach (var member in members)
        {
            var stillTied = members.Any(other => other != member
                && (other.TieBreakCount ?? 0) == (member.TieBreakCount ?? 0)
                && (other.Section != member.Section || member.Section == "X"));
            if (stillTied)
            {
                return false;
            }
        }

        return true;
    }

    public static Dictionary<int, GroupStatus> Evaluate(IEnumerable<Result> results)
    {
        var map = new Dictionary<int, GroupStatus>();
        foreach (var group in results.Where(r => r.TieBreakGroup.HasValue).GroupBy(r => r.TieBreakGroup!.Value))
        {
            var members = group.ToList();
            if (!NeedsTieBreak(members))
            {
                map[group.Key] = new GroupStatus(members.Any(r => r.IsTied == true), false);
                continue;
            }

            var decided = CountsDecidePositions(members);
            map[group.Key] = new GroupStatus(!decided, !decided);
        }

        return map;
    }

    public static bool AnyUnresolved(IEnumerable<Result> results)
    {
        var list = results.ToList();
        var groups = Evaluate(list);
        if (groups.Values.Any(status => status.TieBreakRequired))
        {
            return true;
        }

        // A stored bit with no group, or a group the counts did not decide, is still unresolved.
        // A group whose counts already place the seats ignores a stale bit.
        foreach (var result in list)
        {
            if (result.TieBreakRequired != true)
            {
                continue;
            }

            if (result.TieBreakGroup is int group
                && groups.TryGetValue(group, out var status)
                && !status.TieBreakRequired)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    public static (bool IsTied, bool TieBreakRequired) FlagsFor(
        Result result,
        IReadOnlyDictionary<int, GroupStatus> groups)
    {
        if (result.TieBreakGroup is int group && groups.TryGetValue(group, out var status))
        {
            return (status.IsTied, status.TieBreakRequired);
        }

        return (result.IsTied == true, result.TieBreakRequired == true);
    }
}
