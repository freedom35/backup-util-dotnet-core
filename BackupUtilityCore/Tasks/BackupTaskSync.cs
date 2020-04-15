using System;
using System.IO;
using System.Linq;

namespace BackupUtilityCore.Tasks
{
    public sealed class BackupTaskSync : BackupTaskBase
    {
        public override string BackupDescription => "SYNC";

        protected override int PerformBackup()
        {
            //string targetDir = BackupSettings.TargetDirectory;

            //AddToLog("Target DIR", targetDir);

            // Need to check for directories that exist in target but not in source

            // Need to check for files that exist in target but not in source

            // Remove from target if it exists
            //DeleteFile(targetPath);

            return base.PerformBackup();
        }

        /// <summary>
        /// When syncing, only remove files from within source directories, not entire target dir - 
        /// may be other files in there that need to be kept.
        /// </summary>
        /// <param name="sourceSubDir"></param>
        /// <param name="sourceDirInfo"></param>
        /// <param name="targetDirInfo"></param>
        /// <returns></returns>
        //protected override int BackupFiles(string sourceSubDir, DirectoryInfo sourceDirInfo, DirectoryInfo targetDirInfo)
        //{
            

        //    return CopyFiles(sourceSubDir, sourceDirInfo, targetDirInfo);
        //}
    }
}
