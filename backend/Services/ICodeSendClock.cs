namespace Backend.Services;

/// <summary>
/// UTC clock for paid-send daily caps. Tests supply a clock that can move to the next day.
/// </summary>
public interface ICodeSendClock
{
    /// <summary>
    /// Current UTC time.
    /// </summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>
    /// UTC calendar day used as the owner daily-cap key.
    /// </summary>
    DateOnly UtcDate { get; }
}

/// <summary>
/// Clock that reads <see cref="DateTimeOffset.UtcNow"/>.
/// </summary>
public class SystemCodeSendClock : ICodeSendClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public DateOnly UtcDate => DateOnly.FromDateTime(DateTime.UtcNow);
}
