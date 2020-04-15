using System.IO;

namespace BackupUtilityCore.Tasks
{
    /// <summary>
    /// Same logic as copy, just starting in a new root dir with each copy.
    /// </summary>
    public sealed class BackupTaskIsolatedCopy : BackupTaskCopy
    {
        

        protected override int BackupFiles(string sourceSubDir, DirectoryInfo sourceDirInfo, DirectoryInfo targetDirInfo)
        {
            int _ = BackupSettings.MaxIsololationDays;

            // Delete old backups

            return base.BackupFiles(sourceSubDir, sourceDirInfo, targetDirInfo);
        }
    }
}
