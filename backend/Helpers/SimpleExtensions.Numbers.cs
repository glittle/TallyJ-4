using System.Globalization;

namespace Backend.Helpers;

public static partial class ExtensionsSimple
{
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
    /// Calculates the specified percentage of an integer value.
    /// </summary>
    /// <param name="input">The base value.</param>
    /// <param name="percentage">The percentage to calculate (0-100).</param>
    /// <returns>The calculated percentage of the input value.</returns>
    public static int TakingPercent(this int input, int percentage)
    {
        if (percentage == 0)
        {
            return 0;
        }

        return input * percentage / 100;
    }
}
