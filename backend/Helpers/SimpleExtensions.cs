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

    public static bool AsBoolean(this XAttribute input)
    {
        return input.AsString().AsBoolean();
    }

    public static bool AsBoolean(this bool? input)
    {
        return input.HasValue && input.Value;
    }

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

    /// <summary>Attempt to convert this input to a date</summary>
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

    /// <summary>Get the date from this Nullable date. If it is null, return default value.</summary>
    public static DateTime AsDate(this DateTime? input, DateTime defaultValue)
    {
        return input ?? defaultValue;
    }

    /// <summary>Get the date from this Nullable date. If it is null, return DateTime.MinValue.</summary>
    public static DateTime AsDate(this DateTime? input)
    {
        return input ?? DateTime.MinValue;
    }

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

    /// <summary>Attempt to convert this input to a date</summary>
    public static DateTime AsDateTime(this object input)
    {
        return input.AsDate();
    }

    //[Obsolete("Use AsDouble for most cases")]
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

    public static double AsDouble(this object input, IFormatProvider formatProvider)
    {
        return Convert.ToDouble(input.NullIfEmptyString(), formatProvider);
    }

    public static T AsEnum<T>(this byte input, T defaultValue)
    {
        return ((int)input).AsEnum(defaultValue);
    }

    public static T AsEnum<T>(this int input, T defaultValue)
    {
        var enumType = typeof(T);

        if (Enum.IsDefined(enumType, input))
        {
            return (T)Enum.Parse(enumType, input.ToString());
        }

        return defaultValue;
    }

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

    public static int AsInt(this object input)
    {
        return AsInt(input, 0);
    }

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

    public static int AsInt32(this object input)
    {
        return AsInt(input, 0);
    }

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

    public static string FilledWithDict(this string input, IDictionary<string, string> value)
    {
        if (input.HasNoContent())
        {
            return string.Empty;
        }

        return new TemplateHelper(input).FillByName(value);
    }

    public static string FilledWithDict(this string input, IDictionary<string, object> value)
    {
        if (input.HasNoContent())
        {
            return string.Empty;
        }

        return new TemplateHelper(input).FillByName(value);
    }

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

    public static string GetAllMessages(this Exception input, string separator)
    {
        return input.GetAllMessages().JoinedAsString(separator);
    }

    public static IEnumerable<string> GetAllMessages(this Exception? input)
    {
        while (input != null)
        {
            yield return input.Message;
            input = input.InnerException;
        }
    }

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

    public static TV? GetValueOrDefault<TK, TV>(this IDictionary<TK, TV?> dict, TK key, TV defVal)
    {
        return dict.GetValueOrDefault(key, () => defVal);
    }

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

    public static TimeSpan Hour(this int input)
    {
        return input.Hours();
    }

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

    public static TimeSpan Minute(this int input)
    {
        return input.Minutes();
    }

    public static TimeSpan Minutes(this int input)
    {
        return new TimeSpan(0, input, 0);
    }

    public static DateTimeOffset? NullIfEmpty(this DateTimeOffset? input)
    {
        if (input == null)
            return null;

        return input == DateTimeOffset.MinValue ? null : input;
    }

    public static string? NullIfEmpty(this string? input)
    {
        return string.IsNullOrWhiteSpace(input) ? null : input;
    }

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

    public static double Rounded(this double input, int decimals)
    {
        return Math.Round(input, decimals);
    }

    public static TimeSpan Second(this int input)
    {
        return input.Seconds();
    }

    public static TimeSpan Seconds(this int input)
    {
        return new TimeSpan(0, 0, input);
    }

    private static readonly Regex s_mustachesRegex =
      new(@"{{.*?}}", RegexOptions.Singleline | RegexOptions.Compiled);

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

    public static string SurroundWith(this string input, string bothSides)
    {
        return SurroundWith(input, bothSides, bothSides);
    }

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

    public static string HtmlDecoded(this string input)
    {
        return HttpUtility.HtmlDecode(input);
    }

    public static string HtmlEncoded(this string input)
    {
        return HttpUtility.HtmlEncode(input);
    }

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