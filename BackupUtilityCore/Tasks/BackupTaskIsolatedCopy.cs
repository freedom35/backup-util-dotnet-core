using System;
using System.IO;

namespace BackupUtilityCore.Tasks
{
    /// <summary>
    /// Same logic as copy, just starting in a new root dir with each copy.
    /// </summary>
    public sealed class BackupTaskIsolatedCopy : BackupTaskCopy
    {
        protected override BackupResult BackupFile(string filename, string sourceSubDir, DirectoryInfo targetDirInfo)
        {
            int _ = BackupSettings.MaxIsololationDays;

            return base.BackupFile(filename, sourceSubDir, targetDirInfo);
        }
    }
}
