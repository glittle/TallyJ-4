using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Web;

namespace Backend.Helpers;

public static partial class ExtensionsSimple
{
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
}
