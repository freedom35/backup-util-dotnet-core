using System;
using System.Collections.Generic;
using System.Text;

namespace BackupUtilityCore.YAML
{
    public sealed class YamlBackupSettings : IBackupSettings
    {
        #region Properties

        public string[] SourceDirectories
        {
            get;
            set;
        } = new string[0];

        public string TargetDirectory
        {
            get;
            set;
        } = "";

        public string[] ExcludedDirectories
        {
            get;
            set;
        } = new string[0];

        public string[] ExcludedFileTypes
        {
            get;
            set;
        } = new string[0];

        /// <summary>
        /// Gets or sets a value indicating whether to ignore hidden files.
        /// </summary>
        /// <value><c>true</c> to ignore hidden files; otherwise, <c>false</c>.</value>
        public bool IgnoreHiddenFiles
        {
            get;
            set;
        } = true;

        #endregion

        public void Parse(string fileName)
        {
            
        }

        public void SaveToFile(string fileName)
        {

        }
    }
}
