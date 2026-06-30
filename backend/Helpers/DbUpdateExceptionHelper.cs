using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Backend.Helpers;

/// <summary>
/// Helpers for interpreting database update failures across supported providers.
/// </summary>
public static class DbUpdateExceptionHelper
{
    private const int SqliteConstraintUnique = 2067;

    /// <summary>
    /// Returns true when the exception was caused by a unique-index or unique-constraint violation.
    /// </summary>
    public static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        for (var current = ex.InnerException; current != null; current = current.InnerException)
        {
            if (current is SqlException sqlEx)
            {
                return sqlEx.Number is 2601 or 2627;
            }

            if (current.GetType().FullName == "Microsoft.Data.Sqlite.SqliteException")
            {
                var extendedErrorCode = current.GetType().GetProperty("SqliteExtendedErrorCode")?.GetValue(current);
                if (extendedErrorCode is int code && code == SqliteConstraintUnique)
                {
                    return true;
                }
            }
        }

        return false;
    }
}