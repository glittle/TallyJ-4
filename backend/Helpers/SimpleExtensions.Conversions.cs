using System.Diagnostics;
using System.Globalization;
using System.Web;
using System.Xml.Linq;
using static System.Threading.Thread;

namespace Backend.Helpers;

public static partial class ExtensionsSimple
{
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

    /// <summary>
    /// Converts an IHtmlString to its string representation.
    /// </summary>
    /// <param name="input">The IHtmlString to convert.</param>
    /// <returns>The string representation of the IHtmlString, or null if the input is null.</returns>
    public static string? AsString(this IHtmlString? input)
    {
        return input?.ToString();
    }
}
