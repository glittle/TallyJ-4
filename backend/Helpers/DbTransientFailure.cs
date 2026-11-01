using Microsoft.Data.SqlClient;

namespace Backend.Helpers;

/// <summary>
/// Matches the failures <see cref="Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy"/> will retry.
/// The strategy reports an exhausted retry as <c>RetryLimitExceededException</c> whose inner exception
/// is the original SQL error, so callers walk the chain.
/// </summary>
internal static class DbTransientFailure
{
    public static bool IsTransient(Exception? exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is TimeoutException)
            {
                return true;
            }

            if (current is SqlException sql && sql.IsTransient)
            {
                return true;
            }
        }

        return false;
    }
}
