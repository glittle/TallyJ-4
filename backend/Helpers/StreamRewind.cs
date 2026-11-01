namespace Backend.Helpers;

/// <summary>
/// Puts a seekable stream back at the start so a SQL retry can read it again.
/// </summary>
internal static class StreamRewind
{
    public static void RewindIfSeekable(Stream stream)
    {
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }
    }
}
