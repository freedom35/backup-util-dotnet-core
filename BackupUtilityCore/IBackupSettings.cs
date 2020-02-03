using System;
using System.Collections.Generic;
using System.Text;

namespace BackupUtilityCore
{
    public interface IBackupSettings
    {
        string[] SourceDirectories
        {
            get;
            set;
        }

        string TargetDirectory
        {
            get;
            set;
        }

        string[] ExcludedDirectories
        {
            get;
            set;
        }

        string[] ExcludedFileTypes
        {
            get;
            set;
        }

        bool IgnoreHiddenFiles
        {
            get;
            set;
        }

        void Parse(string fileName);

        void SaveToFile(string fileName);
    }
}
