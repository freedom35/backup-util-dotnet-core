using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace BackupUtilityCore
{
    public sealed class BackupSettings
    {
        #region Members

        private string[] excludedDirectories = {
            "obj",
            "bin",
            "_sgbak",
            "ipch",
            ".dropbox.cache",
            ".git",
            ".vs"
        };

        private string[] excludedFileTypes = {
            "exe",
            "dll",
            "pdb",
            "zip",
            "scc",
            "obj",
            "sbr",
            "ilk",
            "pch",
            "bsc",
            "tlog",
            "idb",
            "lastbuildstate",
            "manifest",
            "cache",
            "log",
            "exp",
            "tlh",
            "tli",
            "sdf",
            "aps"
        };

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the source directories.
        /// </summary>
        /// <value>The source directories.</value>
        public string[] SourceDirectories
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the target directory.
        /// </summary>
        /// <value>The target directory.</value>
        public string TargetDirectory
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the excluded directories.
        /// </summary>
        /// <value>The excluded directories.</value>
        public string[] ExcludedDirectories
        {
            // Ensure non-null returned
            get => excludedDirectories ?? new string[0];

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
            get => excludedFileTypes ?? new string[0];

            // Format file types to be consistent
            set => excludedFileTypes = value.Select(file => file.TrimStart('.').ToLower()).ToArray();
        }

        /// <summary>
        /// Determines whether any excluded file types are defined.
        /// </summary>
        /// <value><c>true</c> if has excluded file types; otherwise, <c>false</c>.</value>
        public bool HasExcludedFileTypes
        {
            get => IgnoreHiddenFiles || excludedFileTypes?.Length > 0;
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
        /// Gets or sets a value indicating whether to keep a recent version for rollback.
        /// </summary>
        /// <value><c>true</c> if keep rollback version; otherwise, <c>false</c>.</value>
        public bool KeepRollbackVersion
        {
            get;    // Keep in separate sub dir so not all mixed in - pain for recovery?
            set;    // Keep files from day before
        } = false;

        /// <summary>
        /// Determines whether the current settings are valid.
        /// </summary>
        /// <value><c>true</c> if valid; otherwise, <c>false</c>.</value>
        public bool Valid
        {
            get => SourceDirectories?.Length > 0 && !string.IsNullOrEmpty(TargetDirectory);
        }

        /// <summary>
        /// Gets or sets the name of the settings file.
        /// </summary>
        /// <value>The name of the settings file.</value>
        //public string SettingsFileName
        //{
        //    get;
        //    private set;
        //}

        #endregion

        public bool IsFileExcluded(string fileName)
        {
            // Check whether any specific files or file typesare excluded.
            return !HasExcludedFileTypes || ExcludedFileTypes.Contains(System.IO.Path.GetExtension(fileName.ToLower()));
        }

        public bool IsDirectoryExcluded(string directoryName)
        {
            return !HasExcludedDirectories || ExcludedDirectories.Contains(directoryName.ToLower());
        }
    }
}
