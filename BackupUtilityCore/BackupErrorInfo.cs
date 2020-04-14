using System;
using System.IO;

namespace BackupUtilityCore
{
    public sealed class BackupErrorInfo
    {
        public BackupErrorInfo(BackupResult result)
        {
            Result = result;
        }

        public BackupResult Result
        {
            get;
            private set;
        }

        public DateTime ErrorTime
        {
            get;
        } = DateTime.Now;

        public double MillisecondsSinceError
        {
            get => (DateTime.Now - ErrorTime).TotalMilliseconds;
        }

        public string Filename
        {
            get;
            set;
        } = "";

        public string SourceSubDir
        {
            get;
            set;
        } = "";

        public DirectoryInfo TargetDirInfo
        {
            get;
            set;
        } = null;
    }
}
