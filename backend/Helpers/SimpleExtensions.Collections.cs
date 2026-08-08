using Microsoft.EntityFrameworkCore;

namespace Backend.Helpers;

public static partial class ExtensionsSimple
{
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
    /// Calculates the number of pages needed to display a total number of items with a given page size.
    /// </summary>
    /// <param name="total">The total number of items.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>The number of pages required to display all items.</returns>
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
