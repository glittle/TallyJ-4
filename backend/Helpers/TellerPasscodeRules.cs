namespace Backend.Helpers;

/// <summary>
/// Length rule for teller passcodes an owner creates or changes.
/// Login does not use this rule, so a passcode already stored can be shorter.
/// </summary>
public static class TellerPasscodeRules
{
    /// <summary>
    /// i18n key for the minimum-length validation message.
    /// </summary>
    public const string MinLengthMessageKey = "elections.form.electionPasscodeMinLength";

    /// <summary>
    /// True when <paramref name="submitted"/> may be saved.
    /// Null or empty means the passcode is not being set. A value equal to
    /// <paramref name="stored"/> is allowed on update even when it is shorter
    /// than <paramref name="minimumLength"/>.
    /// </summary>
    public static bool IsAcceptableValue(
        string? submitted,
        string? stored,
        int minimumLength,
        bool isCreate)
    {
        if (string.IsNullOrEmpty(submitted))
        {
            return true;
        }

        if (submitted.Length >= minimumLength)
        {
            return true;
        }

        return !isCreate && stored != null && submitted == stored;
    }
}
