namespace Backend.Models;

/// <summary>
/// Represents a raw vote entry from imported ballot data.
/// </summary>
public class OnlineRawVote
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OnlineRawVote"/> class.
    /// </summary>
    public OnlineRawVote()
    {
        // Need this for JSON deserializing
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OnlineRawVote"/> class with text to parse.
    /// </summary>
    /// <param name="text">The text to parse for first and last names.</param>
    public OnlineRawVote(string text)
    {
        // This constructor used by CDN ballot importer

        OtherInfo = text;

        // Do a rough guess at first and last name
        First = "";
        Last = "";

        // Likely   first last
        //     or   last, first

        if (text.Contains(","))
        {
            var split = text.Split(new[] { ',' }, 2);
            Last = split[0].Trim();
            First = split[1].Trim();
        }
        else
        {
            var split = text.Split(' ');
            var numWords = split.Length;

            // If > 2 words, cannot guess which are for first name or last name. Default to last word --> Last
            Last = split.Last();
            First = string.Join(" ", split.Reverse().Skip(1).Reverse());
        }
    }

    /// <summary>
    /// Gets or sets the ID of the vote.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the first name extracted from the vote text.
    /// </summary>
    public string First { get; set; } = "";

    /// <summary>
    /// Gets or sets the last name extracted from the vote text.
    /// </summary>
    public string Last { get; set; } = "";

    /// <summary>
    /// Gets or sets the original vote text.
    /// </summary>
    public string OtherInfo { get; set; } = "";
}