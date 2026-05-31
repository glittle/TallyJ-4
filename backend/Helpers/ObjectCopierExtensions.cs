using System.Collections.Concurrent;
using System.Reflection;

namespace Backend.Helpers;

/// <summary>
/// Transparent, cached object-to-object property copier.
/// Copies properties by matching name (case-sensitive) and compatible type.
/// Designed to be easy to understand for humans and AI — no hidden mapping engine.
/// </summary>
public static class ObjectCopierExtensions
{
    /// <summary>
    /// Cached property mappings between a source type and target type.
    /// </summary>
    private sealed record PropertyMapping(PropertyInfo SourceProperty, PropertyInfo TargetProperty);

    /// <summary>
    /// Cache of property mappings keyed by (sourceType, targetType).
    /// This avoids repeated reflection on hot paths (e.g. listing people, ballots, etc.).
    /// </summary>
    private static readonly ConcurrentDictionary<(Type Source, Type Target), PropertyMapping[]> Cache = new();

    /// <summary>
    /// Copies all matching properties (by name) from the source object to the target object.
    /// 
    /// Matching rules:
    /// - Property names must match exactly (case-sensitive).
    /// - Source property must be readable and target property must be writable.
    /// - Types must be compatible (exact match, or source can be converted to target via Convert.ChangeType).
    /// 
    /// When ignoreNulls is true, null values in the source are skipped (they do not overwrite the target).
    /// This is the equivalent of Mapster's IgnoreNullValues(true) behavior on update mappings.
    /// </summary>
    /// <param name="source">The object to read values from.</param>
    /// <param name="target">The object to write values into.</param>
    /// <param name="ignoreNulls">When true, null values from the source are not copied.</param>
    public static void CopyMatchingPropertiesTo(this object? source, object target, bool ignoreNulls = false)
    {
        if (source is null || target is null)
            return;

        var mappings = GetOrCreateMappings(source.GetType(), target.GetType());

        foreach (var mapping in mappings)
        {
            var value = mapping.SourceProperty.GetValue(source);

            if (ignoreNulls && value is null)
                continue;

            // Only set if the value is actually different (minor optimization + fewer change trackers)
            var currentValue = mapping.TargetProperty.GetValue(target);
            if (Equals(value, currentValue))
                continue;

            try
            {
                var convertedValue = ConvertValue(value, mapping.TargetProperty.PropertyType);
                mapping.TargetProperty.SetValue(target, convertedValue);
            }
            catch
            {
                // Silently skip properties that cannot be converted. This keeps the copier robust
                // while remaining completely transparent (no magic, just defensive copying).
            }
        }
    }

    /// <summary>
    /// Creates a new instance of <typeparamref name="TTarget"/> and copies all matching properties
    /// from the source into it.
    /// </summary>
    public static TTarget CopyMatchingPropertiesToNew<TTarget>(this object? source)
        where TTarget : new()
    {
        var target = new TTarget();
        source.CopyMatchingPropertiesTo(target, ignoreNulls: false);
        return target;
    }

    /// <summary>
    /// Gets (or creates and caches) the list of matching property mappings between two types.
    /// </summary>
    private static PropertyMapping[] GetOrCreateMappings(Type sourceType, Type targetType)
    {
        return Cache.GetOrAdd((sourceType, targetType), static key =>
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            var sourceProps = key.Source.GetProperties(flags)
                .Where(p => p.CanRead)
                .ToDictionary(p => p.Name, p => p);

            var targetProps = key.Target.GetProperties(flags)
                .Where(p => p.CanWrite)
                .ToArray();

            var mappings = new List<PropertyMapping>();

            foreach (var targetProp in targetProps)
            {
                if (sourceProps.TryGetValue(targetProp.Name, out var sourceProp))
                {
                    if (AreTypesCompatible(sourceProp.PropertyType, targetProp.PropertyType))
                    {
                        mappings.Add(new PropertyMapping(sourceProp, targetProp));
                    }
                }
            }

            return mappings.ToArray();
        });
    }

    /// <summary>
    /// Determines whether a value from the source type can reasonably be assigned or converted
    /// into the target property type.
    /// </summary>
    private static bool AreTypesCompatible(Type sourceType, Type targetType)
    {
        if (targetType.IsAssignableFrom(sourceType))
            return true;

        // Nullable handling
        var underlyingTarget = Nullable.GetUnderlyingType(targetType);
        if (underlyingTarget != null)
            targetType = underlyingTarget;

        var underlyingSource = Nullable.GetUnderlyingType(sourceType);
        if (underlyingSource != null)
            sourceType = underlyingSource;

        // Allow common conversions that Convert.ChangeType supports
        if (sourceType == typeof(string) || targetType == typeof(string))
            return true;

        if (sourceType.IsPrimitive || sourceType.IsEnum || IsNumericType(sourceType))
            return IsNumericType(targetType) || targetType == typeof(string) || targetType.IsEnum;

        return false;
    }

    private static bool IsNumericType(Type type) =>
        type == typeof(byte) || type == typeof(sbyte) ||
        type == typeof(short) || type == typeof(ushort) ||
        type == typeof(int) || type == typeof(uint) ||
        type == typeof(long) || type == typeof(ulong) ||
        type == typeof(float) || type == typeof(double) ||
        type == typeof(decimal);

    /// <summary>
    /// Attempts to convert the source value to the target type.
    /// Falls back to direct assignment when possible.
    /// </summary>
    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value is null)
            return null;

        var actualTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (actualTargetType.IsAssignableFrom(value.GetType()))
            return value;

        // Handle enum from string or int
        if (actualTargetType.IsEnum)
        {
            if (value is string s)
                return Enum.Parse(actualTargetType, s, ignoreCase: true);
            return Enum.ToObject(actualTargetType, value);
        }

        // Handle string to Guid (common in our DTOs)
        if (actualTargetType == typeof(Guid) && value is string guidString)
        {
            return Guid.TryParse(guidString, out var g) ? g : Guid.Empty;
        }

        try
        {
            return Convert.ChangeType(value, actualTargetType);
        }
        catch
        {
            return value; // Last resort - let the caller decide
        }
    }
}
