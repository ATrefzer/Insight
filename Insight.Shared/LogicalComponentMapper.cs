using System.Collections.Generic;
using System.Xml;

namespace Insight.Shared
{
    /// <summary>
    /// Maps a local path name to a logical component like ui, database, model, etc.
    /// The key in the definition file must appear in the local path for a mapping to apply.
    /// If more than one key matches a local path the longest key wins.
    /// <Mappings>
    ///    <Mapping key="" value="" />
    /// </Mappings>
    /// 
    /// </summary>
    public class LogicalComponentMapper
    {
        Dictionary<string, string> _mappings;

        public bool LowerCase { get; private set; }

        public void ReadDefinitionFile(string path, bool lowerCase = false)
        {
            LowerCase = lowerCase;
            _mappings = new Dictionary<string, string>();
            using (var reader = XmlReader.Create(path))
            {
                while (reader.Read())
                {
                    if (reader.Name == "Mapping")
                    {
                        var key = reader.GetAttribute("key");
                        var value = reader.GetAttribute("value");

                        if (LowerCase)
                        {
                            key = key.ToLowerInvariant();
                        }
                        _mappings.Add(key, value);
                    }
                }
            }
        }

        public string MapLocalPathToLogicalComponent(string localPath)
        {
            int longestMatch = 0;
            var mapping = string.Empty; // Not used if this is returned.
            foreach (var key in _mappings.Keys)
            {
                var lowerLocalPath = localPath.ToLowerInvariant();
                if (lowerLocalPath.Contains(key))
                {
                    if (key.Length > longestMatch)
                    {
                        // Allow mapping keys like "Insight" and "Insight.Shared"
                        // take the longest, most specific key.
                        mapping = _mappings[key];
                        longestMatch = key.Length;
                    }
                }
            }

            return mapping;
        }

        public IEnumerable<string> GetKeys()
        {
            return _mappings.Keys;
        }

        public string GetMapping(string key)
        {
            if (LowerCase)
            {
                key = key.ToLowerInvariant();
            }
            return _mappings[key];
        }
    }
}
