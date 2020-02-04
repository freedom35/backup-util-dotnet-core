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
            BackupSettings settings = new BackupSettings();

            return settings;
        }
    }
}
