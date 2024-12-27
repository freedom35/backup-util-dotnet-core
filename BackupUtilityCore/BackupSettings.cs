using System;
using System.Collections.Generic;
using System.Linq;

namespace BackupUtilityCore
{
    /// <summary>
    /// Settings for a backup task.
    /// </summary>
    public sealed class BackupSettings
    {
        #region Members

        private string[] excludedDirectories = [];
        private string[] excludedFileTypes = [];

        #endregion

        #region Properties

        /// <summary>
        /// Determines type of backup.
        /// </summary>
        public BackupType BackupType
        {
            get;
            set;
        } = (BackupType)(-1);

        /// <summary>
        /// Gets or sets the source directories.
        /// </summary>
        /// <value>The source directories.</value>
        public string[] SourceDirectories
        {
            get;
            set;
        } = [];

        /// <summary>
        /// Gets or sets the target directory.
        /// </summary>
        /// <value>The target directory.</value>
        public string TargetDirectory
        {
            get;
            set;
        } = "";

        /// <summary>
        /// Gets or sets the excluded directories.
        /// </summary>
        /// <value>The excluded directories.</value>
        public string[] ExcludedDirectories
        {
            // Ensure non-null returned
            get => excludedDirectories ?? [];

            // Format to be consistent
            set => excludedDirectories = value.Select(dir => dir.ToLower()).ToArray();
        }

        /// <summary>
        /// Determines whether any excluded directories are defined.
        /// </summary>
        /// <value><c>true</c> if has excluded directories; otherwise, <c>false</c>.</value>
        public bool HasExcludedDirectories
        {
            get => excludedDirectories?.Length > 0;
        }

        /// <summary>
        /// Gets or sets the excluded file types.
        /// </summary>
        /// <value>The excluded file types.</value>
        public string[] ExcludedFileTypes
        {
            // Ensure non-null returned
            get => excludedFileTypes ?? [];

            // Format file types to be consistent
            set => excludedFileTypes = value.Select(file => file.TrimStart('.').ToLower()).ToArray();
        }

        /// <summary>
        /// Determines whether any excluded file types are defined.
        /// </summary>
        /// <value><c>true</c> if has excluded file types; otherwise, <c>false</c>.</value>
        public bool HasExcludedFileTypes
        {
            get => excludedFileTypes?.Length > 0;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to ignore hidden files.
        /// </summary>
        /// <value><c>true</c> to ignore hidden files; otherwise, <c>false</c>.</value>
        public bool IgnoreHiddenFiles
        {
            get;
            set;
        } = true;

        /// <summary>
        /// Gets or sets the max number of days to keep isolated backups.
        /// </summary>
        /// <value>Number of days.</value>
        public int MaxIsololationDays
        {
            get;
            set;
        } = 0;

        /// <summary>
        /// Determines whether the current settings are valid.
        /// </summary>
        /// <value><c>true</c> if valid, otherwise <c>false</c>.</value>
        public bool Valid
        {
            get => GetInvalidSettings().Count == 0;
        }

        /// <summary>
        /// Gets or sets the name of the settings file (if created via parse).
        /// </summary>
        /// <value>The name of the settings file.</value>
        public string SettingsFilename
        {
            get;
            private set;
        } = "";

        #endregion

        /// <summary>
        /// Checks whether a file is excluded from backup.
        /// </summary>
        /// <param name="filename">Name of file to check</param>
        /// <returns>true if excluded</returns>
        public bool IsFileTypeExcluded(string filename)
        {
            return excludedFileTypes?.Contains(System.IO.Path.GetExtension(filename).TrimStart('.').ToLower()) == true;
        }

        /// <summary>
        /// Checks whether a directory is excluded from backup.
        /// </summary>
        /// <param name="directoryName">Name of directory to check</param>
        /// <returns>true if excluded</returns>
        public bool IsDirectoryExcluded(string directoryName)
        {
            return excludedDirectories?.Contains(directoryName.ToLower()) == true;
        }

        /// <summary>
        /// Parses backup settings from a YAML file.
        /// </summary>
        /// <param name="settingsPath">Path of config file</param>
        /// <param name="settings">BackupSettings object</param>
        /// <returns>true if parsed ok<returns>
        public static bool TryParseFromYaml(string settingsPath, out BackupSettings settings)
        {
            ///////////////////////////////////////////
            // Parse config from file
            ///////////////////////////////////////////
            Dictionary<string, object> keyValuePairs = YAML.YamlParser.ParseFile(settingsPath);

            ///////////////////////////////////////////
            // Initialize settings object
            ///////////////////////////////////////////
            settings = new BackupSettings()
            {
                SettingsFilename = System.IO.Path.GetFileName(settingsPath)
            };

            ///////////////////////////////////////////
            // Check key/values for expected settings
            ///////////////////////////////////////////

            if (keyValuePairs.TryGetValue("backup_type", out object? configBackupType) && Enum.TryParse(configBackupType.ToString(), true, out BackupType type))
            {
                settings.BackupType = type;
            }

            if (keyValuePairs.TryGetValue("target_dir", out object? targetDir) && targetDir is string targetDirAsString)
            {
                settings.TargetDirectory = targetDirAsString;
            }

            if (keyValuePairs.TryGetValue("source_dirs", out object? sourceDirs))
            {
                settings.SourceDirectories = (sourceDirs as IEnumerable<string>)?.ToArray() ?? [];
            }

            // Optional
            if (keyValuePairs.TryGetValue("excluded_dirs", out object? excludedDirs))
            {
                settings.ExcludedDirectories = (excludedDirs as IEnumerable<string>)?.ToArray() ?? [];
            }

            // Optional
            if (keyValuePairs.TryGetValue("excluded_types", out object? excludedTypes))
            {
                settings.ExcludedFileTypes = (excludedTypes as IEnumerable<string>)?.ToArray() ?? [];
            }

            // Optional
            if (keyValuePairs.TryGetValue("ignore_hidden_files", out object? ignoreHiddenFilesStr) && bool.TryParse(ignoreHiddenFilesStr?.ToString()?.ToLower(), out bool ignore))
            {
                settings.IgnoreHiddenFiles = ignore;
            }

            // Used for isolated backup type
            if (keyValuePairs.TryGetValue("max_isolation_days", out object? daysAsString) && int.TryParse(daysAsString.ToString(), out int daysAsInt))
            {
                settings.MaxIsololationDays = daysAsInt;
            }

            return settings.Valid;
        }

        /// <summary>
        /// Gets info on invalid settings.
        /// </summary>
        public Dictionary<string, string> GetInvalidSettings()
        {
            Dictionary<string, string> invalidSettings = [];

            // Enum parse will work for string or int, but any integer will enum parse ok, check value is valid
            if (!Enum.IsDefined(BackupType))
            {
                invalidSettings.Add("backup_type", $"setting is missing or associated value is invalid, valid values are: {string.Join(" / ", Enum.GetNames<BackupType>())}");
            }

            // Must have a target
            if (string.IsNullOrEmpty(TargetDirectory))
            {
                invalidSettings.Add("target_dir", "setting or associated value is missing.");
            }

            // Must have at least one source
            if (SourceDirectories == null || SourceDirectories.Length == 0)
            {
                invalidSettings.Add("source_dirs", "setting or associated values are missing.");
            }

            return invalidSettings;
        }
    }
}
