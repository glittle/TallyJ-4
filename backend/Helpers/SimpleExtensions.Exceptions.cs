namespace Backend.Helpers;

public static partial class ExtensionsSimple
{
    /// <summary>
    /// Gets all exception messages from the exception chain and joins them with a separator.
    /// </summary>
    /// <param name="input">The exception to get messages from.</param>
    /// <param name="separator">The separator to use between messages.</param>
    /// <returns>A string containing all exception messages joined by the separator.</returns>
    public static string GetAllMessages(this Exception input, string separator)
    {
        return input.GetAllMessages().JoinedAsString(separator);
    }

    /// <summary>
    /// Gets all exception messages from the exception chain as an enumerable collection.
    /// </summary>
    /// <param name="input">The exception to get messages from.</param>
    /// <returns>An enumerable collection of all exception messages in the chain.</returns>
    public static IEnumerable<string> GetAllMessages(this Exception? input)
    {
        while (input != null)
        {
            yield return input.Message;
            input = input.InnerException;
        }
    }
}
