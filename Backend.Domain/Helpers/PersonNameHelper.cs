using Backend.Domain.Entities;

namespace Backend.Domain.Helpers;

/// <summary>
/// Helper class for consistent formatting of person names across the application.
/// </summary>
public static class PersonNameHelper
{
    /// <summary>
    /// Computes the full name in "LastName, FirstName" format with optional name components.
    /// </summary>
    public static string? ComputeFullName(Person person)
    {
        if (person == null) return null;

        var parts = new List<string>();

        // LastName + OtherLastNames
        var lastNamePart = person.LastName ?? "";
        if (!string.IsNullOrEmpty(person.OtherLastNames))
            lastNamePart += $" [{person.OtherLastNames}]";
        if (!string.IsNullOrEmpty(lastNamePart))
            parts.Add(lastNamePart);

        // Add comma separator only if we have both last name and first name
        if (parts.Count > 0 && !string.IsNullOrEmpty(person.FirstName))
            parts.Add(", ");

        // FirstName
        if (!string.IsNullOrEmpty(person.FirstName))
            parts.Add(person.FirstName ?? "");

        // OtherNames
        if (!string.IsNullOrEmpty(person.OtherNames))
            parts.Add($" [{person.OtherNames}]");

        // OtherInfo
        if (!string.IsNullOrEmpty(person.OtherInfo))
            parts.Add($" ({person.OtherInfo})");

        return string.Join("", parts).Trim();
    }

    /// <summary>
    /// Computes the full name in "FirstName LastName" format with optional name components.
    /// Used for sorting and display purposes.
    /// </summary>
    public static string? ComputeFullNameFl(Person person)
    {
        if (person == null) return null;

        var parts = new List<string>();

        // FirstName + space
        if (!string.IsNullOrEmpty(person.FirstName))
            parts.Add($"{person.FirstName} ");

        // LastName
        if (!string.IsNullOrEmpty(person.LastName))
            parts.Add(person.LastName);

        // OtherNames
        if (!string.IsNullOrEmpty(person.OtherNames))
            parts.Add($" [{person.OtherNames}]");

        // OtherLastNames
        if (!string.IsNullOrEmpty(person.OtherLastNames))
            parts.Add($" [{person.OtherLastNames}]");

        // OtherInfo
        if (!string.IsNullOrEmpty(person.OtherInfo))
            parts.Add($" ({person.OtherInfo})");

        return string.Join("", parts).Trim();
    }

    /// <summary>
    /// Gets a sort key for a person, using FullNameFl or falling back to RowId.
    /// </summary>
    public static string GetSortKey(Person person)
    {
        if (person == null) return "0";

        var fullNameFl = ComputeFullNameFl(person);
        return !string.IsNullOrEmpty(fullNameFl) ? fullNameFl : person.RowId.ToString();
    }
}