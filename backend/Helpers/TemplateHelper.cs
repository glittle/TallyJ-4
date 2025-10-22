using System.Text.RegularExpressions;

namespace TallyJ4.Backend.Helpers
{
    public class TemplateHelper
    {
        private const string TokenPattern = "{{([^{}]+)}}";
        readonly string _template;

        public TemplateHelper(string template)
        {
            _template = template;
        }

        public string FillByName(IDictionary<string, string> dict)
        {
            var result = Replace(dict, _template, TokenPattern);
            var lastResult = "";

            while (lastResult != result && Regex.IsMatch(result, TokenPattern))
            {
                lastResult = result;
                result = Replace(dict, result, TokenPattern);
            }

            return result;
        }

        public string FillByName(IDictionary<string, object> dict)
        {
            var result = Replace(dict, _template, TokenPattern);
            var lastResult = "";

            while (lastResult != result && Regex.IsMatch(result, TokenPattern))
            {
                lastResult = result;
                result = Replace(dict, result, TokenPattern);
            }

            return result;
        }

        /// <summary>
        /// strip all token-like strings out
        /// </summary>
        /// <returns></returns>
        public string RemoveTokens()
        {
            var result = Regex.Replace(_template, TokenPattern, match => "");

            return result;
        }

        static string Replace(IDictionary<string, object> dict, string template, string tokenPattern)
        {
            return Regex.Replace(
              template,
              tokenPattern,
              delegate (Match match)
              {
                  var token = match.Value;
                  var key = token.Substring(2, token.Length - 4);

                  // Try exact case match first for performance
                  if (dict.ContainsKey(key))
                  {
                      var value = dict[key] ?? "";
                      return value?.ToString() ?? "";
                  }

                  // Fall back to case-insensitive search
                  var caseInsensitiveKey = dict.Keys.FirstOrDefault(k =>
              string.Equals(k, key, StringComparison.OrdinalIgnoreCase)
            );
                  if (caseInsensitiveKey != null)
                  {
                      var value = dict[caseInsensitiveKey] ?? "";
                      return value?.ToString() ?? "";
                  }

                  // If no match found (exact or case-insensitive), return empty string
                  // This fixes the issue where unmatched tokens (e.g., typos like {{Nane}} instead of {{Name}})
                  // would appear in the final output instead of being replaced with empty string
                  return "";
              }
            );
        }

        static string Replace(
          IDictionary<string, string> properties,
          string template,
          string tokenPattern
        )
        {
            return Regex.Replace(
              template,
              tokenPattern,
              delegate (Match match)
              {
                  var token = match.Value;
                  var key = token.Substring(2, token.Length - 4);

                  // Try exact case match first for performance
                  if (properties.ContainsKey(key))
                  {
                      var value = properties[key] ?? "";
                      return value;
                  }

                  // Fall back to case-insensitive search
                  var caseInsensitiveKey = properties.Keys.FirstOrDefault(k =>
              string.Equals(k, key, StringComparison.OrdinalIgnoreCase)
            );
                  if (caseInsensitiveKey != null)
                  {
                      var value = properties[caseInsensitiveKey] ?? "";
                      return value;
                  }

                  // If no match found (exact or case-insensitive), return empty string
                  // This fixes the issue where unmatched tokens (e.g., typos like {{Nane}} instead of {{Name}})
                  // would appear in the final output instead of being replaced with empty string
                  return "";
              }
            );
        }

        public string FillByArray<T>(IEnumerable<T> values)
        {
            var array = values.ToArray();
            var lastIndex = array.Length - 1;
            if (lastIndex < 0)
            {
                return _template;
            }

            return Regex.Replace(
              _template,
              TokenPattern,
              delegate (Match match)
              {
                  var token = match.Value;
                  var arrayIndex = token.Substring(2, token.Length - 4).AsInt();
                  if (arrayIndex < 0 || arrayIndex > lastIndex)
                  {
                      return token;
                  }
                  var value = array[arrayIndex];
                  return value?.ToString() ?? "";
              }
            );
        }
    }
}