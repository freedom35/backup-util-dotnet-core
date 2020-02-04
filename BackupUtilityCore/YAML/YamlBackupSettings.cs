using System;
using System.Collections.Generic;
using System.Text;

namespace BackupUtilityCore.YAML
{
    public sealed class YamlBackupSettings : ISettingsParser
    {
        public BackupSettings Parse(string fileName)
        {
            BackupSettings settings = new BackupSettings();

            return settings;
        }
    }
}
