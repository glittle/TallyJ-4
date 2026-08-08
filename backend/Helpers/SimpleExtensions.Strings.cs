using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Backend.Helpers;

public static partial class ExtensionsSimple
{
    /// <summary>
    ///   Returns the <paramref name="additionalString" /> appended to this one, with a separator if there
    ///   is already
    ///   content
    /// </summary>
    public static string Append(this string input, string additionalString, string separator)
    {
        return input.HasContent() ? input + separator + additionalString : additionalString;
    }

    /// <summary>
    /// Truncates a string to a specified maximum length, optionally adding ellipsis and handling line breaks.
    /// </summary>
    /// <param name="input">The string to truncate.</param>
    /// <param name="maxLength">The maximum length of the resulting string.</param>
    /// <param name="addEllipsis">Whether to add ellipsis ("...") when truncating.</param>
    /// <param name="afterLineBreak">Whether to truncate after the first line break.</param>
    /// <param name="returnNullIfEmpty">Whether to return null instead of empty string when input is null or empty.</param>
    /// <returns>The truncated string, or null/empty string based on parameters.</returns>
    public static string? ChopAfter(
      this string? input,
      int maxLength,
      bool addEllipsis = true,
      bool afterLineBreak = false,
      bool returnNullIfEmpty = false
    )
    {
        if (input == null || maxLength <= 0)
        {
            return returnNullIfEmpty ? null : string.Empty;
        }

        var forceEllipsis = false;

        if (afterLineBreak)
        {
            var pos1 = input.IndexOf("\r", StringComparison.Ordinal);
            var pos2 = input.IndexOf("\n", StringComparison.Ordinal);
            var pos = pos1 != -1 ? pos1 : pos2;
            if (pos != -1)
            {
                input = input.Substring(0, pos);
                addEllipsis = true;
                forceEllipsis = true;
            }
        }

        if (input.Length > maxLength)
        {
            if (addEllipsis && input.Length + 3 > maxLength)
            {
                return input.Substring(0, maxLength - 3) + "...";
            }

            input = input.Substring(0, maxLength);
        }

        if (forceEllipsis)
        {
            if (input.Length + 4 > maxLength)
            {
                return input.Substring(0, maxLength - 4) + " ...";
            }

            return input + " ...";
        }

        return input;
    }

    /// <summary>
    ///   If input is empty, use <paramref name="defaultValue" />
    /// </summary>
    public static string DefaultTo(this string? input, string defaultValue)
    {
        return input.HasNoContent() ? defaultValue : input;
    }

    /// <summary>
    /// Returns the input string if it has content, otherwise returns the specified default value (which can be null).
    /// </summary>
    /// <param name="input">The input string to check.</param>
    /// <param name="defaultValue">The default value to return if the input has no content.</param>
    /// <returns>The input string or the default value.</returns>
    public static string? DefaultToNullable(this string? input, string? defaultValue)
    {
        return input.HasNoContent() ? defaultValue : input;
    }

    /// <summary>
    ///   If input is 0, use <paramref name="defaultValue" />
    /// </summary>
    public static int DefaultTo(this int input, int defaultValue)
    {
        return input == 0 ? defaultValue : input;
    }


    /// <summary>
    /// Formats a code into a human-readable label with proper capitalization and spacing.
    /// </summary>
    /// <param name="code">The status code to format (e.g., "ReadyForSubmission")</param>
    /// <returns>A formatted string (e.g., "Ready For Submission")</returns>
    public static string SplitByCaps(this string code)
    {
        if (string.IsNullOrEmpty(code))
            return "Unknown";

        // Add spaces before capital letters, trim, and capitalize first letter
        var result = System.Text.RegularExpressions.Regex.Replace(code, "([A-Z])", " $1").Trim();

        // Capitalize the first letter if it's not already
        if (result.Length > 0)
        {
            result = char.ToUpper(result[0]) + result.Substring(1);
        }

        return result;
    }

    /// <summary>
    ///   Use the input string as the format with string.Format
    /// </summary>
    public static string FilledWith(this string input, params object[] values)
    {
        if (input.HasNoContent())
        {
            return string.Empty;
        }

        return string.Format(input, values);
    }

    /// <summary>
    /// Fills a template string with values from a dictionary using string keys.
    /// </summary>
    /// <param name="input">The template string containing placeholders.</param>
    /// <param name="value">The dictionary containing the replacement values.</param>
    /// <returns>The template string with placeholders replaced by dictionary values.</returns>
    public static string FilledWithDict(this string input, IDictionary<string, string> value)
    {
        if (input.HasNoContent())
        {
            return string.Empty;
        }

        return new TemplateHelper(input).FillByName(value);
    }

    /// <summary>
    /// Fills a template string with values from a dictionary using string keys and object values.
    /// </summary>
    /// <param name="input">The template string containing placeholders.</param>
    /// <param name="value">The dictionary containing the replacement values.</param>
    /// <returns>The template string with placeholders replaced by dictionary values.</returns>
    public static string FilledWithDict(this string input, IDictionary<string, object> value)
    {
        if (input.HasNoContent())
        {
            return string.Empty;
        }

        return new TemplateHelper(input).FillByName(value);
    }


    /// <summary>
    /// Returns the last character of a string, or an empty string if the input is null or empty.
    /// </summary>
    /// <param name="value">The string to get the last character from.</param>
    /// <returns>The last character as a string, or empty string if input is null/empty.</returns>
    public static string LastCharacter(this string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var length = value.Length;
        return value[length - 1].ToString();
    }

    /// <summary>
    ///   Return the first <paramref name="length" /> characters in ths string. If string is shorter,
    ///   return the string.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static string Left(this string input, int length)
    {
        if (input.HasNoContent())
        {
            return "";
        }

        return input.Length <= length ? input : input.Substring(0, length);
    }

    /// <summary>
    /// Returns null if the string is null, empty, or consists only of white-space characters, otherwise returns the input.
    /// </summary>
    /// <param name="input">The string to check.</param>
    /// <returns>The input string or null if it is null or whitespace.</returns>
    public static string? NullIfEmpty(this string? input)
    {
        return string.IsNullOrWhiteSpace(input) ? null : input;
    }

    /// <summary>
    /// Returns null if the object is null or represents an empty string, otherwise returns the string representation of the object.
    /// </summary>
    /// <param name="input">The object to convert to string.</param>
    /// <returns>The string representation of the object or null if it represents an empty string.</returns>
    public static string? NullIfEmptyString(this object? input)
    {
        switch (input)
        {
            case null:
            case string s when s.HasNoContent():
                return null;
            default:
                return input.ToString();
        }
    }

    /// <summary>Convert all control characters and quotes to entities</summary>
    /// <param name="sText"></param>
    /// <returns></returns>
    public static string PrepareForXml(this string sText)
    {
        var sb = new StringBuilder(sText);
        sb = sb.Replace("&", "&amp;"); // ' do this first!
        sb = sb.Replace("<", "&lt;");
        sb = sb.Replace(">", "&gt;");
        sb = sb.Replace("'", "&apos;");
        sb = sb.Replace("\"", "&quot;");
        sb = sb.Replace(((char)9).ToString(), "&#x9;");
        sb = sb.Replace(((char)10).ToString(), "&#xA;");
        sb = sb.Replace(((char)13).ToString(), "&#xD;");

        return sb.ToString();
    }

    /// <summary>Replace any and all of the find strings with the replace string</summary>
    public static string ReplaceMany(this string input, string[] find, string replace)
    {
        if (input.HasNoContent())
        {
            return "";
        }

        return find.Aggregate(input, (current, s) => current.Replace(s, replace));
    }

    private static readonly Regex s_mustachesRegex =
      new(@"{{.*?}}", RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>
    /// Removes mustache-style template placeholders ({{...}}) from a string.
    /// </summary>
    /// <param name="input">The string to process.</param>
    /// <returns>The string with all mustache placeholders removed.</returns>
    public static string StripMustaches(this string input)
    {
        // remove {{ and }} and any content between them
        return s_mustachesRegex.Replace(input, "");
    }

    /// <summary>
    ///   Split a string by commas. Between each comma is a KeyValuePair separated by a colon. For example:
    ///   "key1:value1,
    ///   key2:value2".
    /// </summary>
    public static IEnumerable<KeyValuePair<string, string>> SplitIntoKeyValuePairs(
      this string input,
      string majorSep = ",",
      string minorSep = ":"
    )
    {
        return input.HasNoContent()
          ? new KeyValuePair<string, string>[0]
          : input
            .SplitWithString(majorSep, StringSplitOptions.RemoveEmptyEntries)
            .Select(s =>
            {
                var parts = s.SplitWithString(minorSep, StringSplitOptions.RemoveEmptyEntries);
                switch (parts.Length)
                {
                    case 0:
                        return new KeyValuePair<string, string>(string.Empty, string.Empty);

                    case 1:
                        return new KeyValuePair<string, string>(parts[0].Trim(), parts[0].Trim());

                    default:
                        // combine last parts together
                        return new KeyValuePair<string, string>(
                      parts[0].Trim(),
                      parts.Skip(1).JoinedAsString(minorSep).Trim()
                    );
                }
            });
    }

    /// <summary>
    ///   Split using a single separator
    /// </summary>
    public static string[] SplitWithString(this string input, string separator)
    {
        return SplitWithString(input, separator, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    ///   Split using a single separator
    /// </summary>
    public static string[] SplitWithString(
      this string? input,
      string separator,
      StringSplitOptions stringSplitOptions
    )
    {
        return input == null ? [] : input.Split(new[] { separator }, stringSplitOptions);
    }

    /// <summary>
    ///   Surround with left and right strings. If the input has no content, an empty string is returned.
    /// </summary>
    public static string SurroundContentWith(this string input, string left, string? right = null)
    {
        if (input.HasNoContent())
        {
            return string.Empty;
        }

        return left + input + (right ?? left);
    }

    /// <summary>
    /// Surrounds a string with the same string on both sides.
    /// </summary>
    /// <param name="input">The string to surround.</param>
    /// <param name="bothSides">The string to place on both sides of the input.</param>
    /// <returns>The input string surrounded by the specified string.</returns>
    public static string SurroundWith(this string input, string bothSides)
    {
        return SurroundWith(input, bothSides, bothSides);
    }

    /// <summary>
    /// Surrounds a string with different strings on the left and right sides.
    /// </summary>
    /// <param name="input">The string to surround.</param>
    /// <param name="left">The string to place on the left side.</param>
    /// <param name="right">The string to place on the right side.</param>
    /// <returns>The input string surrounded by the specified left and right strings.</returns>
    public static string SurroundWith(this string input, string left, string right)
    {
        return left + input + right;
    }

    /// <summary>
    ///   Used primarily in reading from XmlResource files for use in websites.
    ///   Replace any Cr or Lf or Tab with a space.
    ///   Then reduce multiple leading or trailing spaces to a single space.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string TrimCrLfAndExcessSpaces(this string input)
    {
        var s = input.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');

        var main = s.TrimStart();
        if (main.Length != s.Length)
        {
            s = " " + main;
        }

        main = s.TrimEnd();
        if (main.Length != s.Length)
        {
            s = main + " ";
        }

        return s;
    }


    /// <summary>Undo the conversion of characters... then we will use HTML encode on the result</summary>
    /// <param name="sText"></param>
    /// <returns></returns>
    public static string UndoPrepareForXml(this string sText)
    {
        var sb = new StringBuilder(sText);
        sb = sb.Replace("&amp;", "&");
        sb = sb.Replace("&lt;", "<");
        sb = sb.Replace("&gt;", ">");
        sb = sb.Replace("&apos;", "'");
        sb = sb.Replace("&quot;", "\"\"");
        sb = sb.Replace("&#x9;", ((char)9).ToString());
        sb = sb.Replace("&#xA;", ((char)10).ToString());
        sb = sb.Replace("&#xD;", ((char)13).ToString());

        return sb.ToString();
    }

    /// <summary>Use this to unescape after using javascript encodeURIComponent</summary>
    public static string Unencoded(this string input)
    {
        return Uri.UnescapeDataString(input);
    }

    /// <summary>
    /// Decodes HTML-encoded characters in a string.
    /// </summary>
    /// <param name="input">The string containing HTML-encoded characters.</param>
    /// <returns>The string with HTML entities decoded.</returns>
    public static string HtmlDecoded(this string input)
    {
        return HttpUtility.HtmlDecode(input);
    }

    /// <summary>
    /// Encodes special characters in a string to HTML entities.
    /// </summary>
    /// <param name="input">The string to HTML-encode.</param>
    /// <returns>The string with special characters encoded as HTML entities.</returns>
    public static string HtmlEncoded(this string input)
    {
        return HttpUtility.HtmlEncode(input);
    }


}
