using Backend.Enumerations;

namespace Backend.Helpers;

public static partial class ExtensionsSimple
{
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
    ///   Blank if string is an empty guid
    /// </summary>
    public static string BlankEmptyGuid(this string input)
    {
        return input == Guid.Empty.ToString() ? string.Empty : input;
    }

    /// <summary>
    ///   String version of Guid. Blank if Guid.Empty.
    /// </summary>
    public static string DefaultBlank(this Guid input)
    {
        return input == Guid.Empty ? string.Empty : input.ToString();
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

    /// <summary>
    ///  Convert a string to an ElectionType enum value by matching the code, ignoring case. If no match is found, returns ElectionTypeEnum.Oth.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static ElectionType AsElectionType(this string? input) =>
        ElectionTypeEnum.All.FirstOrDefault(e => e.Code.Equals(input, StringComparison.OrdinalIgnoreCase)) ?? ElectionTypeEnum.Oth;
}
