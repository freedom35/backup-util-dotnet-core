using System;
using System.Collections.Generic;
using System.Text;

namespace BackupUtilityCore.YAML
{
    /// <summary>
    /// Basic YAML parser for backup settings.
    /// </summary>
    public sealed class YamlSettingsParser : ISettingsParser
    {
        public BackupSettings Parse(string fileName)
        {
            // Parse config from file
            Dictionary<string, object> keyValuePairs = YamlParser.ParseFile(fileName);

            BackupSettings settings = new BackupSettings();

            return settings;
        }
    }
}
