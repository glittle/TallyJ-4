using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace Backend.Helpers;

public static partial class ExtensionsSimple
{
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
    ///   Converts this object to a JSON string
    /// </summary>
    /// <param name="input"></param>
    /// <param name="indented">When true, produces pretty-printed JSON</param>
    /// <param name="forHtml">If set and indented is true, use &lt;BR /&gt; and &amp;nbsp; for display</param>
    /// <returns></returns>
    public static string ForJson(
      this object input,
      bool indented = false,
      bool forHtml = false
    )
    {
        var options = indented
            ? new JsonSerializerOptions { WriteIndented = true }
            : null;

        var s = JsonSerializer.Serialize(input, options);
        if (forHtml)
        {
            s = s.Replace("\r\n", "<br>").Replace(" ", "&nbsp;");
        }

        return s;
    }

    /// <summary>Input:  ["1","2"] --> 1,2</summary>
    public static T? FromJson<T>(this string input)
    {
        return JsonSerializer.Deserialize<T>(input);
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

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return new List<string>();
            }

            var keys = new List<string>();
            foreach (var property in doc.RootElement.EnumerateObject())
            {
                keys.Add(property.Name);
            }

            return keys;
        }
        catch (JsonException)
        {
            return new List<string>();
        }
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
}
