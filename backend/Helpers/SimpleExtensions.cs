using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static System.Threading.Thread;
using Formatting = Newtonsoft.Json.Formatting;

namespace TallyJ4.Backend.Helpers;

/// <summary>
///   This file contains extensions that do not change the type of the input, or simply answer a
///   question about the input.
/// </summary>
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
    /// Converts an XAttribute to a boolean value.
    /// </summary>
    /// <param name="input">The XAttribute to convert.</param>
    /// <returns>The boolean value of the attribute.</returns>
    public static bool AsBoolean(this XAttribute input)
    {
        return input.AsString().AsBoolean();
    }

    /// <summary>
    /// Converts a nullable boolean to a boolean value.
    /// </summary>
    /// <param name="input">The nullable boolean to convert.</param>
    /// <returns>True if the input has a value and is true, otherwise false.</returns>
    public static bool AsBoolean(this bool? input)
    {
        return input.HasValue && input.Value;
    }

    /// <summary>
    /// Converts an object to a boolean value with flexible parsing.
    /// Supports boolean values, strings ("yes", "no", "1", "0"), and standard boolean parsing.
    /// </summary>
    /// <param name="input">The object to convert.</param>
    /// <param name="defaultValue">The default value to return if conversion fails.</param>
    /// <returns>The boolean representation of the input, or the default value if conversion fails.</returns>
    public static bool AsBoolean(this object? input, bool defaultValue = false)
    {
        if (input == null)
        {
            return defaultValue;
        }

        if (input is bool b)
        {
            return b;
        }

        var s = Convert.ToString(input)?.ToLower();
        if (s == "yes")
        {
            return true;
        }

        if (s == "no")
        {
            return true;
        }

        if (s == "1")
        {
            return true;
        }

        if (s == "0")
        {
            return false;
        }

        if (s == "")
        {
            return defaultValue;
        }

        return bool.TryParse(s, out var result) && result;
    }

    /// <summary>
    /// Converts an object to a byte value, clamping the result to the valid byte range (0-255).
    /// </summary>
    /// <param name="input">The object to convert.</param>
    /// <returns>The byte representation of the input, clamped to 0-255 range.</returns>
    public static byte AsByte(this object input)
    {
        var value = input.AsInt();

        return (byte)(value < 0 || value > 255 ? 0 : value);
    }

    /// <summary>
    ///   Format number as currency and replace space with &#160;
    /// </summary>
    /// <param name="input"></param>
    /// <param name="format">Usually C0 or C2</param>
    /// <param name="formatForHtml"></param>
    /// <returns></returns>
    public static string AsCurrencyStr(
      this double input,
      string format = "C2",
      bool formatForHtml = true
    )
    {
        var s = input.ToString(format);
        return formatForHtml ? s.Replace(" ", "&nbsp;") : s;
    }

    /// <summary>
    ///   Format number as currency and replace space with &#160;
    /// </summary>
    /// <param name="input"></param>
    /// <param name="format">Usually C0 or C2</param>
    /// <param name="formatForHtml"></param>
    /// <returns></returns>
    public static string AsCurrencyStr(
      this object input,
      string format = "C2",
      bool formatForHtml = true
    )
    {
        return input.AsDouble().AsCurrencyStr(format, formatForHtml);
    }

    /// <summary>
    /// Attempts to convert a string to a DateTime.
    /// Supports standard date formats and YYYYMMDD format.
    /// </summary>
    /// <param name="input">The string to convert to a date.</param>
    /// <returns>The parsed DateTime, or DateTime.MinValue if parsing fails.</returns>
    public static DateTime AsDate(this string? input)
    {
        input = input.NullIfEmptyString();
        if (input.HasContent())
        {
            try
            {
                return Convert.ToDateTime(input);
            }
            catch
            {
                // ignored
            }

            if (input.Length == 8)
            {
                input = $"{input.Substring(0, 4)}-{input.Substring(4, 2)}-{input.Substring(6, 2)}";

                try
                {
                    return Convert.ToDateTime(input);
                }
                catch
                {
                    // ignored
                }
            }
        }

        return DateTime.MinValue;
    }

    /// <summary>
    /// Converts an object to a DateTime.
    /// Supports Unix timestamp (milliseconds since 1970-01-01) or standard date formats.
    /// </summary>
    /// <param name="input">The object to convert to a date.</param>
    /// <returns>The parsed DateTime.</returns>
    public static DateTime AsDate(this object input)
    {
        if (long.TryParse(input.NullIfEmptyString(), out var date))
        {
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            dateTime = dateTime.AddMilliseconds(date);
            return dateTime;
        }

        return Convert.ToDateTime(input.NullIfEmptyString());
    }

    /// <summary>
    /// Converts an object to a DateTime using the specified format provider.
    /// Supports Unix timestamp (milliseconds since 1970-01-01) or standard date formats.
    /// </summary>
    /// <param name="input">The object to convert to a date.</param>
    /// <param name="formatProvider">The format provider to use for parsing.</param>
    /// <returns>The parsed DateTime.</returns>
    public static DateTime AsDate(this object input, IFormatProvider formatProvider)
    {
        if (long.TryParse(input.NullIfEmptyString(), NumberStyles.Any, formatProvider, out var date))
        {
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            dateTime = dateTime.AddMilliseconds(date);
            return dateTime;
        }

        return Convert.ToDateTime(input.NullIfEmptyString());
    }

    /// <summary>
    /// Gets the DateTime value from a nullable DateTime, or returns the specified default value if null.
    /// </summary>
    /// <param name="input">The nullable DateTime to convert.</param>
    /// <param name="defaultValue">The default value to return if the input is null.</param>
    /// <returns>The DateTime value or the default value.</returns>
    public static DateTime AsDate(this DateTime? input, DateTime defaultValue)
    {
        return input ?? defaultValue;
    }

    /// <summary>
    /// Gets the DateTime value from a nullable DateTime, or returns DateTime.MinValue if null.
    /// </summary>
    /// <param name="input">The nullable DateTime to convert.</param>
    /// <returns>The DateTime value or DateTime.MinValue.</returns>
    public static DateTime AsDate(this DateTime? input)
    {
        return input ?? DateTime.MinValue;
    }

    /// <summary>
    /// Converts an object to a formatted date string, or returns an empty string if conversion fails.
    /// </summary>
    /// <param name="input">The object to convert to a date string.</param>
    /// <param name="format">The date format string to use.</param>
    /// <returns>The formatted date string, or empty string if parsing fails.</returns>
    public static string AsDateStringOrBlank(this object input, string format)
    {
        if (DateTime.TryParse(input.NullIfEmptyString(), out var date))
        {
            if (date.HasContent())
            {
                return date.ToString(format);
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Attempts to convert an object to a DateTime.
    /// </summary>
    /// <param name="input">The object to convert to a DateTime.</param>
    /// <returns>The parsed DateTime.</returns>
    public static DateTime AsDateTime(this object input)
    {
        return input.AsDate();
    }

    /// <summary>
    /// Converts an object to a decimal value.
    /// Handles culture-specific parsing, with special handling for French-Canadian culture.
    /// </summary>
    /// <param name="input">The object to convert to a decimal.</param>
    /// <returns>The decimal representation of the input, or 0 if conversion fails.</returns>
    public static decimal AsDecimal(this object input)
    {
        decimal result;

        try
        {
            result = Convert.ToDecimal(input.NullIfEmptyString());
            return result;
        }
        catch
        {
            result = 0;

            // if we are in French, try English too
            var culture = CurrentThread.CurrentCulture;
            if (culture.Name == "fr-CA")
            {
                CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en");

                try
                {
                    result = Convert.ToDecimal(input.NullIfEmptyString());
                }
                catch
                {
                    // ignored
                }

                CurrentThread.CurrentCulture = culture;
            }

            return result;
        }
    }

    /// <summary>
    /// Converts an object to a double value.
    /// Handles culture-specific parsing, with special handling for French-Canadian culture.
    /// </summary>
    /// <param name="input">The object to convert to a double.</param>
    /// <returns>The double representation of the input, or 0 if conversion fails.</returns>
    public static double AsDouble(this object input)
    {
        double result;

        try
        {
            result = Convert.ToDouble(input.NullIfEmptyString());
            return result;
        }
        catch
        {
            result = 0;

            // if we are in French, try English too
            var culture = CurrentThread.CurrentCulture;
            if (culture.Name == "fr-CA")
            {
                CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en");

                try
                {
                    result = Convert.ToDouble(input.NullIfEmptyString());
                }
                catch
                {
                    // ignored
                }

                CurrentThread.CurrentCulture = culture;
            }

            return result;
        }
    }

    /// <summary>
    /// Converts an object to a double value using the specified format provider.
    /// </summary>
    /// <param name="input">The object to convert to a double.</param>
    /// <param name="formatProvider">The format provider to use for parsing.</param>
    /// <returns>The double representation of the input.</returns>
    public static double AsDouble(this object input, IFormatProvider formatProvider)
    {
        return Convert.ToDouble(input.NullIfEmptyString(), formatProvider);
    }

    /// <summary>
    /// Converts a byte value to an enum value.
    /// </summary>
    /// <typeparam name="T">The enum type to convert to.</typeparam>
    /// <param name="input">The byte value to convert.</param>
    /// <param name="defaultValue">The default value to return if conversion fails.</param>
    /// <returns>The enum value, or the default value if conversion fails.</returns>
    public static T AsEnum<T>(this byte input, T defaultValue)
    {
        return ((int)input).AsEnum(defaultValue);
    }

    /// <summary>
    /// Converts an integer value to an enum value.
    /// </summary>
    /// <typeparam name="T">The enum type to convert to.</typeparam>
    /// <param name="input">The integer value to convert.</param>
    /// <param name="defaultValue">The default value to return if conversion fails.</param>
    /// <returns>The enum value, or the default value if conversion fails.</returns>
    public static T AsEnum<T>(this int input, T defaultValue)
    {
        var enumType = typeof(T);

        if (Enum.IsDefined(enumType, input))
        {
            return (T)Enum.Parse(enumType, input.ToString());
        }

        return defaultValue;
    }

    /// <summary>
    /// Converts a string value to an enum value with flexible parsing.
    /// Supports case-sensitive matching, case-insensitive matching, integer parsing, and partial matching.
    /// </summary>
    /// <typeparam name="T">The enum type to convert to.</typeparam>
    /// <param name="input">The string value to convert.</param>
    /// <param name="defaultValue">The default value to return if conversion fails.</param>
    /// <returns>The enum value, or the default value if conversion fails.</returns>
    /// <exception cref="ArgumentException">Thrown when T is not an enumeration type.</exception>
    public static T AsEnum<T>(this string input, T defaultValue)
    {
        var enumType = typeof(T);
        if (!enumType.IsEnum)
        {
            throw new ArgumentException(enumType + " is not an enumeration.");
        }

        // abort if no value given
        if (string.IsNullOrEmpty(input))
        {
            return defaultValue;
        }

        // see if the text is valid for this enumeration (case-sensitive)
        if (Enum.IsDefined(enumType, input))
        {
            return (T)Enum.Parse(enumType, input);
        }

        if (int.TryParse(input, out var asInt))
        {
            if (Enum.IsDefined(enumType, asInt))
            {
                return (T)Enum.Parse(enumType, asInt.ToString());
            }
        }

        // see if the text is valid for this enumeration (case-insensitive)
        var names = Enum.GetNames(enumType);
        if (Array.IndexOf(names, input) != -1)
        {
            // case insensitive...
            return (T)Enum.Parse(enumType, input, true);
        }

        // do partial matching...
        var match = names.FirstOrDefault(name =>
          name.StartsWith(input, StringComparison.InvariantCultureIgnoreCase)
        );
        if (match != null)
        {
            return (T)Enum.Parse(enumType, match);
        }

        // didn't find one
        return defaultValue;
    }

    /// <summary>
    ///   Converts a valid string to a Guid.
    /// </summary>
    public static Guid AsGuid(this object? input)
    {
        if (input is Guid guid)
        {
            return guid;
        }

        if (input == null || input.NullIfEmptyString() == null)
        {
            return Guid.Empty;
        }

        try
        {
            return new Guid(input.ToString() ?? string.Empty);
        }
        catch (FormatException)
        {
            return Guid.Empty;
        }
    }

    /// <summary>
    ///   Convert string to Guid. If fails, get Guid.Empty.
    /// </summary>
    public static Guid AsGuid(this Guid? input)
    {
        return input ?? Guid.Empty;
    }

    /// <summary>
    ///   Return random ID (must start with letter). For one page, limit the size - very unlikely to have
    ///   repeats!
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string AsHtmlId(this Guid input)
    {
        return "id" + input.ToString().Substring(0, 5).Replace("-", "");
    }

    /// <summary>
    /// Converts an object to an integer value, returning 0 if conversion fails.
    /// </summary>
    /// <param name="input">The object to convert to an integer.</param>
    /// <returns>The integer representation of the input, or 0 if conversion fails.</returns>
    public static int AsInt(this object input)
    {
        return AsInt(input, 0);
    }

    /// <summary>
    /// Converts an object to an integer value, returning the specified default value if conversion fails.
    /// </summary>
    /// <param name="input">The object to convert to an integer.</param>
    /// <param name="defaultValue">The default value to return if conversion fails.</param>
    /// <returns>The integer representation of the input, or the default value if conversion fails.</returns>
    public static int AsInt(this object? input, int defaultValue)
    {
        if (input == null)
        {
            return defaultValue;
        }

        if (input == DBNull.Value)
        {
            return defaultValue;
        }

        try
        {
            return (int)Math.Truncate(Convert.ToDouble(input));
        }
        catch (Exception)
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Converts an object to an integer value using the specified format provider, returning the specified default value if conversion fails.
    /// </summary>
    /// <param name="input">The object to convert to an integer.</param>
    /// <param name="defaultValue">The default value to return if conversion fails.</param>
    /// <param name="formatProvider">The format provider to use for parsing.</param>
    /// <returns>The integer representation of the input, or the default value if conversion fails.</returns>
    public static int AsInt(this object? input, int defaultValue, IFormatProvider formatProvider)
    {
        if (input == null)
        {
            return defaultValue;
        }

        if (input == DBNull.Value)
        {
            return defaultValue;
        }

        try
        {
            return (int)Math.Truncate(Convert.ToDouble(input, formatProvider));
        }
        catch (Exception)
        {
            return defaultValue;
        }
        //return Util.Strings.Coalesce(input, 0);
    }

    /// <summary>
    /// Converts an object to a 32-bit integer value, returning 0 if conversion fails.
    /// </summary>
    /// <param name="input">The object to convert to an integer.</param>
    /// <returns>The 32-bit integer representation of the input, or 0 if conversion fails.</returns>
    public static int AsInt32(this object input)
    {
        return AsInt(input, 0);
    }

    /// <summary>
    /// Converts an object to a 64-bit integer value, returning the specified default value if conversion fails.
    /// </summary>
    /// <param name="input">The object to convert to a long integer.</param>
    /// <param name="defaultValue">The default value to return if conversion fails.</param>
    /// <returns>The 64-bit integer representation of the input, or the default value if conversion fails.</returns>
    public static long AsInt64(this object? input, long defaultValue = 0)
    {
        if (input == null)
        {
            return defaultValue;
        }

        if (input == DBNull.Value)
        {
            return defaultValue;
        }

        try
        {
            return (long)Math.Truncate(Convert.ToDouble(input));
        }
        catch (Exception)
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Converts an object to a long integer value.
    /// </summary>
    /// <param name="input">The object to convert to a long integer.</param>
    /// <returns>The long integer representation of the input.</returns>
    public static long AsLong(this object input)
    {
        return Convert.ToInt64(input.NullIfEmptyString());
    }

    /// <summary>Convert this date to a nullable date.  If value is DateTime.MinValue then returns null</summary>
    public static DateTime? AsNullableDate(this DateTime input)
    {
        return input.HasNoContent() ? null : input;
    }

    /// <summary>
    ///   Format number as %
    /// </summary>
    /// <param name="input"></param>
    /// <param name="format">Usually P0 or P2</param>
    /// <param name="divideBy100">If true, also divides by 100 to get a percentage</param>
    /// <param name="formatForHtml"></param>
    /// <returns></returns>
    public static string AsPercent(
      this object input,
      string format = "P0",
      bool divideBy100 = false,
      bool formatForHtml = true
    )
    {
        var num = divideBy100 ? input.AsDouble() / 100 : input.AsDouble();
        var s = num.ToString(format);
        return formatForHtml ? s.Replace(" ", "&nbsp;") : s;
    }

    /// <summary>
    ///   Converts to a string. If 0, returns empty string.
    /// </summary>
    public static string AsString(this int input)
    {
        return input == 0 ? string.Empty : input.ToString();
    }

    /// <summary>
    /// Converts a boolean value to a string, returning one of two specified strings based on the boolean value.
    /// </summary>
    /// <param name="input">The boolean value to convert.</param>
    /// <param name="ifTrue">The string to return if the input is true.</param>
    /// <param name="ifFalse">The string to return if the input is false.</param>
    /// <returns>The specified string based on the boolean value.</returns>
    public static string AsString(this bool input, string ifTrue, string ifFalse)
    {
        return input ? ifTrue : ifFalse;
    }

    /// <summary>
    ///   Similar to ToString, but returns "" for nulls and DBNulls
    /// </summary>
    [DebuggerHidden]
    public static string AsString(this object? input)
    {
        if (
          input == null
          || input == DBNull.Value
          || (input is DateTime dateTime && dateTime == DateTime.MinValue)
        )
        {
            return string.Empty;
        }

        return input.ToString() ?? string.Empty;
    }

    /// <summary>Return the Value from this attribute, or "" if attribute is null</summary>
    public static string AsString(this XAttribute? input)
    {
        return input == null ? "" : input.Value;
    }

    /// <summary>Return the Value from this attribute, or "" if attribute is null</summary>
    public static string AsString(this XElement? input)
    {
        return input == null ? "" : input.Value;
    }

    /// <summary>
    ///   Similar to ToString, but returns "" for dates == MinValue.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="format"></param>
    /// <returns></returns>
    public static string AsString(this DateTime input, string format)
    {
        if (input == DateTime.MinValue)
        {
            return string.Empty;
        }

        return input.ToString(format);
    }

    /// <summary>
    ///   Similar to ToString, but returns "" for dates == MinValue.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="format"></param>
    /// <returns></returns>
    public static string AsString(this DateTime? input, string format)
    {
        if (input == null || input.Value == DateTime.MinValue)
        {
            return string.Empty;
        }

        return input.Value.ToString(format);
    }

    /// <summary>
    ///   Blank if string is an empty guid
    /// </summary>
    public static string BlankEmptyGuid(this string input)
    {
        return input == Guid.Empty.ToString() ? string.Empty : input;
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
    ///   String version of Guid. Blank if Guid.Empty.
    /// </summary>
    public static string DefaultBlank(this Guid input)
    {
        return input == Guid.Empty ? string.Empty : input.ToString();
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
    ///   Determine if Live, Preview or Dev.  Defaults to Live.
    /// </summary>
    /// <param name="path"></param>
    /// <returns>The site type</returns>
    public static string DetermineSiteType(this string path)
    {
        var pathParts = path.Split('\\');
        if (pathParts.Contains("Debug") || pathParts.Contains("Release"))
        {
            return "Dev";
        }

        if (pathParts.Contains("Preview"))
        {
            return "Preview";
        }

        return "Live";
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
    ///   Return the number of digits to the left of the decimal.
    /// </summary>
    public static int Digits(this double input)
    {
        if (Math.Abs(input) < 1)
        {
            return 1;
        }

        return input.ToString("e").Split('e')[1].Substring(1, 3).AsInt() + 1;
    }

    /// <summary>
    /// Compares two double values for equality within a specified tolerance.
    /// </summary>
    /// <param name="input">The first double value to compare.</param>
    /// <param name="input2">The second double value to compare.</param>
    /// <param name="tolerance">The tolerance for the comparison (default is 0.01).</param>
    /// <returns>True if the absolute difference between the values is less than the tolerance, otherwise false.</returns>
    public static bool EqualsWithTolerance(this double input, double input2, double tolerance = 0.01)
    {
        return Math.Abs(input - input2) < tolerance;
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
    /// Performs the specified action on each element of the sequence.
    /// </summary>
    /// <typeparam name="TItem">The type of elements in the sequence.</typeparam>
    /// <param name="sequence">The sequence to iterate over.</param>
    /// <param name="action">The action to perform on each element.</param>
    public static void ForEach<TItem>(this IEnumerable<TItem>? sequence, Action<TItem> action)
    {
        if (sequence == null)
        {
            return;
        }

        foreach (var obj in sequence)
        {
            action(obj);
        }
    }

    /// <summary>
    ///   Converts this object to a JSON string
    /// </summary>
    /// <param name="input"></param>
    /// <param name="formatting"></param>
    /// <param name="forHtml">If set and formatting is Indented, use <BR /> tags</param>
    /// <returns></returns>
    public static string ForJson(
      this object input,
      Formatting formatting = Formatting.None,
      bool forHtml = false
    )
    {
        var s = JsonConvert.SerializeObject(input, formatting);
        if (forHtml)
        {
            s = s.Replace("\r\n", "<br>").Replace(" ", "&nbsp;");
        }

        return s;
    }

    /// <summary>Input:  ["1","2"] --> 1,2</summary>
    public static T? FromJson<T>(this string input)
    {
        return JsonConvert.DeserializeObject<T>(input);
    }

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

    /// <summary>
    /// Attempts to extract the browser name from a user agent string.
    /// </summary>
    /// <param name="input">The user agent string to parse.</param>
    /// <param name="defaultValue">The default value to return if browser cannot be identified.</param>
    /// <returns>The browser name if identified, otherwise the default value.</returns>
    public static string GetBrowserName(this string input, string defaultValue)
    {
        if (input.HasNoContent())
        {
            return defaultValue;
        }

        // try to get the name of the browser
        if (input.Contains("Edg/"))
        {
            return "Edge";
        }

        if (input.Contains("Edge/"))
        {
            return "Edge (old)";
        }

        // let .NET guess the rest
        return defaultValue;
    }

    /// <summary>Return the text content of this named element.  Works even if text is in CDATA markup.</summary>
    public static string GetElementValue(
      this XmlElement input,
      string elementName,
      string defaultValue = ""
    )
    {
        var node = input.SelectSingleNode(elementName);
        if (node == null)
        {
            return defaultValue;
        }

        return node.InnerText;
    }

    /// <summary>
    /// Extracts all top-level keys from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <returns>A list of all top-level keys in the JSON object, or an empty list if parsing fails or input is null/empty.</returns>
    public static List<string> GetKeysFromJsonString(this string json)
    {
        if (json.IsNullOrEmpty())
        {
            return new List<string>();
        }

        var jsonObj = JObject.Parse(json);

        var keys = new List<string>();

        foreach (var keyValuePair in jsonObj)
        {
            keys.Add(keyValuePair.Key);
        }

        return keys;
    }

    /// <summary>
    ///   Return the value from the dictionary, or the default value.
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <param name="input"></param>
    /// <param name="key"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public static T2? GetValue<T1, T2>(
      this Dictionary<T1, T2>? input,
      T1 key,
      T2? defaultValue = default
    )
      where T1 : notnull
    {
        return input == null ? defaultValue : input!.GetValueOrDefault(key, defaultValue);
    }

    /// <summary>
    ///   Get value from IDictionary
    /// </summary>
    /// <remarks>https://stackoverflow.com/a/18910179/32429</remarks>
    /// <typeparam name="TK"></typeparam>
    /// <typeparam name="TV"></typeparam>
    /// <param name="dict"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static TV? GetValueOrDefault<TK, TV>(this IDictionary<TK, TV> dict, TK key)
    {
        return dict!.GetValueOrDefault(key, default(TV));
    }

    /// <summary>
    /// Gets the value associated with the specified key from the dictionary, or returns the specified default value if the key is not found.
    /// </summary>
    /// <typeparam name="TK">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TV">The type of the values in the dictionary.</typeparam>
    /// <param name="dict">The dictionary to search.</param>
    /// <param name="key">The key to locate.</param>
    /// <param name="defVal">The default value to return if the key is not found.</param>
    /// <returns>The value associated with the key, or the default value if the key is not found.</returns>
    public static TV? GetValueOrDefault<TK, TV>(this IDictionary<TK, TV?> dict, TK key, TV defVal)
    {
        return dict.GetValueOrDefault(key, () => defVal);
    }

    /// <summary>
    /// Gets the value associated with the specified key from the dictionary, or returns the value produced by the default value selector function if the key is not found.
    /// </summary>
    /// <typeparam name="TK">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TV">The type of the values in the dictionary.</typeparam>
    /// <param name="dict">The dictionary to search.</param>
    /// <param name="key">The key to locate.</param>
    /// <param name="defValSelector">A function that produces the default value if the key is not found.</param>
    /// <returns>The value associated with the key, or the value produced by the default value selector if the key is not found.</returns>
    public static TV? GetValueOrDefault<TK, TV>(
      this IDictionary<TK, TV?> dict,
      TK key,
      Func<TV> defValSelector
    )
    {
        return dict.TryGetValue(key, out var value) ? value : defValSelector();
    }

    /// <summary>
    ///   Determine if input is DateTime.MinValue
    /// </summary>
    public static bool HasContent(this DateTime input)
    {
        return input != DateTime.MinValue;
    }

    /// <summary>
    ///   Not IsNullOrEmpty
    /// </summary>
    [DebuggerStepThrough]
    public static bool HasContent([NotNullWhen(true)] this string? input)
    {
        return !string.IsNullOrEmpty(input);
    }

    /// <summary>
    ///   Not IsNullOrEmpty
    /// </summary>
    public static bool HasContent([NotNullWhen(true)] this int? input)
    {
        return input.HasValue && input.Value != 0;
    }

    /// <summary>
    /// Determines whether the specified object is not null.
    /// </summary>
    /// <param name="input">The object to test.</param>
    /// <returns>True if the object is not null, otherwise false.</returns>
    public static bool HasContent([NotNullWhen(true)] this object? input)
    {
        return input != null;
    }

    /// <summary>
    ///   Not IsNullOrEmpty
    /// </summary>
    public static bool HasContent(this StringBuilder input)
    {
        return input.Length > 0;
    }

    /// <summary>
    ///   Check whether a Guid is empty
    /// </summary>
    public static bool HasContent(this Guid input)
    {
        return input != Guid.Empty;
    }

    /// <summary>
    ///   Check if an enumeration has at least one item
    /// </summary>
    public static bool HasContent<T>(this IEnumerable<T>? input)
    {
        return input != null && input.Count() != 0;
    }

    /// <summary>
    /// Determines whether the specified nullable DateTime has a value and is not equal to DateTime.MinValue.
    /// </summary>
    /// <param name="input">The nullable DateTime to test.</param>
    /// <returns>True if the DateTime has a value and is not MinValue, otherwise false.</returns>
    public static bool HasContent([NotNullWhen(true)] this DateTime? input)
    {
        if (input == null)
        {
            return false;
        }

        return input != DateTime.MinValue;
    }

    /// <summary>
    ///   Determine if input is not DateTime.MinValue
    /// </summary>
    public static bool HasNoContent(this DateTime input)
    {
        return !input.HasContent();
    }

    /// <summary>
    ///   Return true if the input is empty or null.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static bool HasNoContent([NotNullWhen(false)] this string? input)
    {
        return string.IsNullOrEmpty(input);
    }

    /// <summary>
    /// Determines whether the specified object is null.
    /// </summary>
    /// <param name="input">The object to test.</param>
    /// <returns>True if the object is null, otherwise false.</returns>
    public static bool HasNoContent([NotNullWhen(false)] this object? input)
    {
        return input == null;
    }

    /// <summary>
    ///   Check whether a Guid is not empty
    /// </summary>
    public static bool HasNoContent(this Guid input)
    {
        return !input.HasContent();
    }

    /// <summary>
    /// Converts an integer representing hours into a TimeSpan.
    /// </summary>
    /// <param name="input">The number of hours.</param>
    /// <returns>A TimeSpan representing the specified number of hours.</returns>
    public static TimeSpan Hour(this int input)
    {
        return input.Hours();
    }

    /// <summary>
    /// Converts an integer representing hours into a TimeSpan.
    /// </summary>
    /// <param name="input">The number of hours.</param>
    /// <returns>A TimeSpan representing the specified number of hours.</returns>
    public static TimeSpan Hours(this int input)
    {
        return new TimeSpan(input, 0, 0);
    }

    public static bool IsDate(this string value)
    {
        return DateTime.TryParse(value, out _);
    }

    /// <summary>
    ///   IsNullOrEmpty as extension
    /// </summary>
    public static bool IsNullOrEmpty(this string input)
    {
        return string.IsNullOrEmpty(input);
    }

    /// <summary>
    ///   Check if this object is a number or looks like a number
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IsNumeric(this object? value)
    {
        return IsNumeric(value, NumberStyles.Any);
    }

    /// <summary>
    ///   Check if this object is a number or looks like a number
    /// </summary>
    /// <param name="value"></param>
    /// <param name="numberStyle">Test to see if it is this sort of number</param>
    /// <returns></returns>
    public static bool IsNumeric(this object? value, NumberStyles numberStyle)
    {
        return value != null
          && double.TryParse(value.ToString(), numberStyle, CultureInfo.CurrentCulture, out _);
    }

    /// <summary>
    ///   For an array of strings, join them.
    /// </summary>
    public static string JoinedAsString(
      this IEnumerable<string> array,
      string separator,
      Func<string, string> converter
    )
    {
        return array
          .AsEnumerable()
          .Select(converter)
          .JoinedAsString(separator, string.Empty, string.Empty);
    }

    /// <summary>
    /// Joins an enumerable of strings with a separator, optionally skipping blank strings.
    /// </summary>
    /// <param name="array">The enumerable of strings to join.</param>
    /// <param name="separator">The separator to use between strings.</param>
    /// <param name="skipBlanks">Whether to skip blank strings in the join operation.</param>
    /// <returns>The joined string.</returns>
    public static string JoinedAsString(
      this IEnumerable<string> array,
      string separator,
      bool skipBlanks
    )
    {
        return array
          .AsEnumerable()
          .Where(s => !skipBlanks || s.HasContent())
          .JoinedAsString(separator, string.Empty, string.Empty);
    }

    /// <summary>
    ///   For an enumeration of strings, join them.
    /// </summary>
    public static string JoinedAsString(this IEnumerable<string> list)
    {
        return JoinedAsString(list, string.Empty);
    }

    /// <summary>
    ///   For an enumeration of strings, join them.
    /// </summary>
    public static string JoinedAsString(this IEnumerable<string>? list, string separator)
    {
        return list.JoinedAsString(separator, string.Empty, string.Empty);
    }

    /// <summary>
    ///   For an enumeration of strings, join them. Each item has itemLeft and itemRight added.
    /// </summary>
    public static string JoinedAsString(
      this IEnumerable<string>? list,
      string separator,
      string itemLeft,
      string itemRight,
      bool skipBlanks = false
    )
    {
        if (list == null)
        {
            return string.Empty;
        }

        var list2 = list.ToList();
        return list2.Any()
          ? string.Join(
            separator,
            list2
              .Where(s => !skipBlanks || s.HasContent())
              .Select(s => itemLeft + s + itemRight)
              .ToArray()
          )
          : "";
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
    /// Converts an integer representing minutes into a TimeSpan.
    /// </summary>
    /// <param name="input">The number of minutes.</param>
    /// <returns>A TimeSpan representing the specified number of minutes.</returns>
    public static TimeSpan Minute(this int input)
    {
        return input.Minutes();
    }

    /// <summary>
    /// Converts an integer representing minutes into a TimeSpan.
    /// </summary>
    /// <param name="input">The number of minutes.</param>
    /// <returns>A TimeSpan representing the specified number of minutes.</returns>
    public static TimeSpan Minutes(this int input)
    {
        return new TimeSpan(0, input, 0);
    }

    /// <summary>
    /// Returns null if the DateTimeOffset is null or equals DateTimeOffset.MinValue, otherwise returns the input.
    /// </summary>
    /// <param name="input">The DateTimeOffset to check.</param>
    /// <returns>The input DateTimeOffset or null if it is MinValue.</returns>
    public static DateTimeOffset? NullIfEmpty(this DateTimeOffset? input)
    {
        if (input == null)
            return null;

        return input == DateTimeOffset.MinValue ? null : input;
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

    /// <summary>
    /// Returns the ordinal suffix (st, nd, rd, th) for a number, optionally including the number and formatting as HTML superscript.
    /// </summary>
    /// <param name="input">The number to get the ordinal suffix for.</param>
    /// <param name="includeNumber">Whether to include the number in the result.</param>
    /// <param name="htmlSuperscript">Whether to format the suffix as HTML superscript.</param>
    /// <returns>The ordinal suffix, optionally with the number and HTML formatting.</returns>
    public static string Ordinal(
      this int input,
      bool includeNumber = false,
      bool htmlSuperscript = false
    )
    {
        var suffix = "th";

        switch (input % 100)
        {
            case 11:
            case 12:
            case 13:
                suffix = "th";
                break;

            default:
                switch (input % 10)
                {
                    case 1:
                        suffix = "st";
                        break;
                    case 2:
                        suffix = "nd";
                        break;

                    case 3:
                        suffix = "rd";
                        break;
                }

                break;
        }

        if (htmlSuperscript)
        {
            suffix = "<sup>" + suffix + "</sup>";
        }

        return includeNumber ? input + suffix : suffix;
    }

    /// <summary>
    ///   Returns "s" if input is not 1, empty string if it is.
    /// </summary>
    public static string Plural(this int input, string pluralOrZero = "s")
    {
        return Plural(input, pluralOrZero, string.Empty);
    }

    /// <summary>
    ///   Returns
    ///   <paramref name="input"/> name="pluralOrZero" />
    ///   if input is not 1,
    ///   <paramref name="single" />
    ///   if it is.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="pluralOrZero"></param>
    /// <param name="single"></param>
    public static string Plural(this int input, string pluralOrZero, string single)
    {
        return Plural(input, pluralOrZero, single, pluralOrZero);
    }

    /// <summary>
    ///   Returns
    ///   <paramref name="plural" />
    ///   if input is > 1,
    ///   <paramref name="single" />
    ///   if it is 1,
    ///   <paramref name="zero" />
    ///   if it is 0.
    /// </summary>
    public static string Plural(this int input, string plural, string single, string zero)
    {
        switch (input)
        {
            case 0:
                return zero;
            case 1:
                return single;
            default:
                return plural;
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

    /// <summary>
    ///   Make a random number with this many digits
    /// </summary>
    /// <param name="input">Number of digits</param>
    /// <returns></returns>
    public static string RandomDigits(this int input)
    {
        if (input > 9)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "Max is 9");
        }

        var min = Math.Pow(10, input - 1).AsInt();
        var max = Math.Pow(10, input).AsInt();
        return new Random().Next(min, max).ToString();
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

    /// <summary>
    /// Rounds a double value to the specified number of decimal places.
    /// </summary>
    /// <param name="input">The double value to round.</param>
    /// <param name="decimals">The number of decimal places to round to.</param>
    /// <returns>The rounded double value.</returns>
    public static double Rounded(this double input, int decimals)
    {
        return Math.Round(input, decimals);
    }

    /// <summary>
    /// Converts an integer representing seconds into a TimeSpan.
    /// </summary>
    /// <param name="input">The number of seconds.</param>
    /// <returns>A TimeSpan representing the specified number of seconds.</returns>
    public static TimeSpan Second(this int input)
    {
        return input.Seconds();
    }

    /// <summary>
    /// Converts an integer representing seconds into a TimeSpan.
    /// </summary>
    /// <param name="input">The number of seconds.</param>
    /// <returns>A TimeSpan representing the specified number of seconds.</returns>
    public static TimeSpan Seconds(this int input)
    {
        return new TimeSpan(0, 0, input);
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
    ///   Must run result through substitution with XmlResources
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public static string ToAgeDescription(this TimeSpan t)
    {
        if (t.TotalSeconds < 1)
        {
            return $@"{t:s\.f} " + "{seconds}";
        }

        if (t.TotalMinutes < 1)
        {
            return $"{t:%s} " + "{seconds}";
        }

        if (t.TotalHours < 1)
        {
            return $"{t:%m} {{minute}}{(t.TotalMinutes < 2 ? "" : "s")}";
        }

        if (t.TotalDays < 1)
        {
            return $"{t:%h} {{hour}}{(t.TotalHours < 2 ? "" : "s")}";
        }

        return $@"{t:%d} {{day}}{(t.Hours < 2 ? "" : "s")}";
    }

    /// <summary>
    /// Converts a zero-based column index to an Excel-style column name (A, B, C, ..., Z, AA, AB, etc.).
    /// </summary>
    /// <param name="input">The zero-based column index.</param>
    /// <returns>The Excel column name corresponding to the index.</returns>
    public static string ToColumnNameForExcel(this int input)
    {
        var dividend = input;
        var columnName = string.Empty;
        while (dividend > 0)
        {
            var modulo = (dividend - 1) % 26;
            columnName = Convert.ToChar(65 + modulo) + columnName;
            dividend = (dividend - modulo) / 26;
        }

        return columnName;
    }

    /// <summary>
    /// Converts an object's properties to a dictionary with property names as keys and string representations of property values as values.
    /// </summary>
    /// <typeparam name="T">The type of the object to convert.</typeparam>
    /// <param name="obj">The object to convert to a dictionary.</param>
    /// <returns>A dictionary containing the object's properties, or an empty dictionary if the object is null.</returns>
    public static Dictionary<string, string> ToDictionary<T>(this T obj)
    {
        if (obj == null)
        {
            return new Dictionary<string, string>();
        }

        // convert the properties to a dictionary
        return obj.GetType()
          .GetProperties()
          .ToDictionary(p => p.Name, p => p.GetValue(obj, null)?.ToString() ?? "");
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

    /// <summary>
    /// Returns the MIME type for an image file extension, suitable for use in base64 data URIs.
    /// </summary>
    /// <param name="extension">The file extension (with or without leading dot).</param>
    /// <returns>The MIME type string for the image format, or "image/*" if unrecognized.</returns>
    public static string TypeForBase64Image(this string extension)
    {
        switch (extension.ToLower().Replace(".", ""))
        {
            case "svg":
                return "image/svg+xml";

            case "jpg":
            case "jpeg":
                return "image/jpeg";

            case "png":
                return "image/png";
        }

        return "image/*";
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

    /// <summary>
    ///   Convert string to nullable Guid. If fails, returns null.
    /// </summary>
    public static Guid? AsNullableGuid(this string? input)
    {
        Guid? guid = input.AsGuid();
        return guid == Guid.Empty ? null : guid;
    }

    // /// <summary>Coalesce for Guids. Converts (string) value into a Guid. If invalid, Guid.Empty is returned</summary>
    //    public static Guid AsGuid(this object value)
    //    {
    //      return value.AsGuid(Guid.Empty);
    //    }

    /// <summary>Coalesce for Guids. Converts (string) value into a Guid. If invalid, alternativeValue is returned</summary>
    public static Guid AsGuid(this object value, Guid alternativeValue)
    {
        if (value is Guid)
        {
            return (Guid)value;
        }

        try
        {
            return new Guid((string)Convert.ChangeType(value, typeof(string)));
        }
        catch (Exception)
        {
            return alternativeValue;
        }
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

    /// <summary>
    /// Formats a DateTime as a human-readable relative time string (e.g., "2 hours ago", "yesterday").
    /// </summary>
    /// <param name="recentTime">The DateTime to format.</param>
    /// <param name="now">The current DateTime for comparison.</param>
    /// <param name="shortText">Whether to use abbreviated text formats.</param>
    /// <returns>A human-readable string describing how long ago the time occurred.</returns>
    public static string AsRecentTimeString(
      this DateTime recentTime,
      DateTime now,
      bool shortText = false
    )
    {
        if (recentTime == DateTime.MinValue)
        {
            return "-";
        }

        if (now < recentTime)
        {
            return shortText ? "future" : "in the future!";
        }

        var diff = now - recentTime;

        if (diff.TotalMinutes < 1.0)
        {
            return shortText ? "just now" : "A few moments ago";
        }

        if (diff.TotalMinutes < 2.0)
        {
            return shortText ? "1 min" : "1 minute ago";
        }

        if (diff.TotalMinutes < 11.0)
        {
            var minutes = diff.TotalMinutes.ToString("0");
            return (shortText ? "{0} min" : "{0} minutes ago").FilledWith(minutes);
        }

        if (diff.TotalMinutes < 55.0)
        {
            var x = diff.TotalMinutes % 10;
            var roundUp = x > 8;
            var minutes = (diff.TotalMinutes + (roundUp ? 10 - x : 0)).ToString("0");
            if (shortText)
            {
                return "{0} min".FilledWith(minutes);
            }

            var about = roundUp ? "about " : string.Empty;
            return about + "{0} minutes ago".FilledWith(minutes);
        }

        if (diff.TotalMinutes.AsInt() == 60)
        {
            return shortText ? "1 hr" : "1 hour ago";
        }

        if (diff.TotalHours < 1.2)
        {
            return shortText ? "1 hr" : "about 1 hour ago";
        }

        if (diff.TotalHours < 18.0)
        {
            var x = diff.TotalMinutes % 60;
            var roundUp = x > 50;
            var hours = (diff.TotalMinutes + (roundUp ? 60 - x : 0)) / 60;
            if (shortText)
            {
                return "{0:0.#} hrs".FilledWith(hours);
            }

            var about = roundUp ? "about " : string.Empty;
            return about + "{0:0.#} hours ago".FilledWith(hours);
        }

        var time = recentTime.ToString("h:mmtt").ToLower();

        if (recentTime.Date == now.Date)
        {
            return time;
        }

        if (diff.TotalDays < 2.0)
        {
            return shortText ? "yesterday" : "yesterday at " + time;
        }

        if (diff.TotalDays < 200)
        {
            return shortText
              ? recentTime.ToString("MMM d")
              : recentTime.ToString("MMM d") + " at " + time;
        }

        return shortText
          ? recentTime.ToString("d MMM yyyy")
          : recentTime.ToString("d MMM yyyy") + " at " + time;
    }

    public static DateTime FirstDayOfMonth(this DateTime input)
    {
        return new DateTime(input.Year, input.Month, 1, 0, 0, 0);
    }

    public static DateTime GetEndOfMonth(this DateTime input)
    {
        input = new DateTime(input.Year, input.Month, input.Day, 23, 59, 59);
        return input.AddDays(1 - input.Day).AddMonths(1).AddDays(-1);
    }

    public static int DeterminePagesCount(this int total, int pageSize)
    {
        if (total == 0)
        {
            return 0;
        }

        switch (pageSize)
        {
            case 0:
                return 1;

            case 1:
                return total;

            default:
                if (total % pageSize == 0)
                {
                    return total / pageSize;
                }

                return total / pageSize + 1;
        }
    }

    public static string? ZeroToDash(this string? input)
    {
        return input == "0" ? "-" : input;
    }

    /// <summary>
    ///   Not IsNullOrEmpty
    /// </summary>
    public static bool HasContent(this IHtmlString? input)
    {
        if (input == null)
        {
            return false;
        }

        return !string.IsNullOrEmpty(input.ToString());
    }

    /// <summary>
    ///   Not IsNullOrEmpty
    /// </summary>
    public static bool HasNoContent(this IHtmlString input)
    {
        return !input.HasContent();
    }

    public static string? AsString(this IHtmlString? input)
    {
        return input?.ToString();
    }

    public static int TakingPercent(this int input, int percentage)
    {
        if (percentage == 0)
        {
            return 0;
        }

        return input * percentage / 100;
    }

    public static int[] FromHexListAsIntArray(this string input)
    {
        return input
          .Split(',')
          .Select(s => int.Parse(s.Trim().Replace("#", ""), NumberStyles.HexNumber))
          .ToArray();
    }

    /// <summary>
    ///   Given an xml element (usually a root element), find the named child element and return its text content. Replace any
    ///   \n with br for HTML.
    /// </summary>
    public static string GetChildElementTextForHtml(
      this XmlElement input,
      string elementName,
      string defaultText
    )
    {
        var child = input.SelectSingleNode(elementName);
        return child == null ? defaultText : child.InnerText.Replace("\\n", "<br>");
    }

    /// <summary>
    ///   For EF classes
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entity"></param>
    /// <param name="dbSet"></param>
    /// <returns></returns>
    public static T AddTo<T>(this T entity, DbSet<T> dbSet)
      where T : class
    {
        dbSet.Add(entity);
        return entity;
    }
}