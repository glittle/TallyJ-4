namespace Backend.Helpers;

/// <summary>
/// The owner tried to turn on online voting or open its window while the election is flagged.
/// </summary>
public class OnlineVotingSuspendedException : InvalidOperationException
{
    /// <summary>
    /// Phrase key shown to the owner.
    /// </summary>
    public const string MessageKey = "elections.onlineVotingSuspended";

    /// <summary>
    /// Creates the exception with the phrase key as its message.
    /// </summary>
    public OnlineVotingSuspendedException()
        : base(MessageKey)
    {
    }
}
