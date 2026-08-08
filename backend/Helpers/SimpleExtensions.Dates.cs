using System.Globalization;

namespace Backend.Helpers;

public static partial class ExtensionsSimple
{
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

    /// <summary>Convert this date to a nullable date.  If value is DateTime.MinValue then returns null</summary>
    public static DateTime? AsNullableDate(this DateTime input)
    {
        return input.HasNoContent() ? null : input;
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

    /// <summary>
    /// Determines whether the specified string can be parsed as a valid DateTime.
    /// </summary>
    /// <param name="value">The string to test for DateTime validity.</param>
    /// <returns>True if the string can be parsed as a DateTime, otherwise false.</returns>
    public static bool IsDate(this string value)
    {
        return DateTime.TryParse(value, out _);
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

    /// <summary>
    /// Returns the first day of the month for the given DateTime.
    /// </summary>
    /// <param name="input">The DateTime to get the first day of the month for.</param>
    /// <returns>A DateTime representing the first day of the month at midnight.</returns>
    public static DateTime FirstDayOfMonth(this DateTime input)
    {
        return new DateTime(input.Year, input.Month, 1, 0, 0, 0);
    }

    /// <summary>
    /// Returns the last moment of the month for the given DateTime.
    /// </summary>
    /// <param name="input">The DateTime to get the end of the month for.</param>
    /// <returns>A DateTime representing the last moment of the month (23:59:59).</returns>
    public static DateTime GetEndOfMonth(this DateTime input)
    {
        input = new DateTime(input.Year, input.Month, input.Day, 23, 59, 59);
        return input.AddDays(1 - input.Day).AddMonths(1).AddDays(-1);
    }
}
