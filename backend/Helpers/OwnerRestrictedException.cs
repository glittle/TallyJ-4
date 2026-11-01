namespace Backend.Helpers;

/// <summary>
/// The owner tried to turn on online voting, or open its window, while their account
/// is restricted, waiting for review, or rejected.
/// </summary>
public class OwnerRestrictedException : InvalidOperationException
{
    /// <summary>
    /// Phrase key shown to the owner.
    /// </summary>
    public const string MessageKey = "elections.ownerRestricted.onlineBlocked";

    /// <summary>
    /// Creates the exception with the phrase key as its message.
    /// </summary>
    public OwnerRestrictedException()
        : base(MessageKey)
    {
    }
}
