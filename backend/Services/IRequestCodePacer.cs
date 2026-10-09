namespace Backend.Services;

/// <summary>
/// Holds a <c>requestCode</c> reply until a minimum time has passed.
/// A fast "not on the list" path would otherwise return sooner than a listed voter.
/// </summary>
public interface IRequestCodePacer
{
    /// <summary>
    /// Waits out whatever remains of the configured minimum, measured from
    /// <paramref name="startedTimestamp"/> (<see cref="System.Diagnostics.Stopwatch.GetTimestamp"/>).
    /// </summary>
    Task PaceAsync(long startedTimestamp, CancellationToken cancellationToken = default);
}
