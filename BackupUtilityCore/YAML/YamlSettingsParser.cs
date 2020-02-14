using System.Collections.Generic;
using System.Linq;

namespace BackupUtilityCore.YAML
{
    /// <summary>
    /// Basic YAML parser for backup settings.
    /// </summary>
    public sealed class YamlSettingsParser : ISettingsParser
    {
        public BackupSettings Parse(string fileName)
        {
            ///////////////////////////////////////////
            // Parse config from file
            ///////////////////////////////////////////
            Dictionary<string, object> keyValuePairs = YamlParser.ParseFile(fileName);

            BackupSettings settings = new BackupSettings();

            ///////////////////////////////////////////
            // Check key/values for expected settings
            ///////////////////////////////////////////

            if (keyValuePairs.TryGetValue("target_dir", out object targetDir))
            {
                settings.TargetDirectory = targetDir as string;
            }

            if (keyValuePairs.TryGetValue("source_dirs", out object sourceDirs))
            {
                settings.SourceDirectories = (sourceDirs as IEnumerable<string>)?.ToArray();
            }

            if (keyValuePairs.TryGetValue("excluded_dirs", out object excludedDirs))
            {
                settings.ExcludedDirectories = (excludedDirs as IEnumerable<string>)?.ToArray();
            }

            if (keyValuePairs.TryGetValue("excluded_types", out object excludedTypes))
            {
                settings.ExcludedFileTypes = (excludedTypes as IEnumerable<string>)?.ToArray();
            }

            if (keyValuePairs.TryGetValue("ignore_hidden_files", out object ignoreHiddenFilesStr) && bool.TryParse(ignoreHiddenFilesStr.ToString(), out bool ignore))
            {
                settings.IgnoreHiddenFiles = ignore;
            }

            return settings;
        }
    }
}
