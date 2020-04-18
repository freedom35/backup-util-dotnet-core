using System.IO;
using System.Linq;

namespace BackupUtilityCore.Tasks
{
    public class BackupTaskCopy : BackupTaskBase
    {
        protected override string BackupDescription => "COPY";

        protected override int PerformBackup()
        {
            return CopyDirectoryTo(BackupSettings.TargetDirectory);
        }

        protected int CopyDirectoryTo(string targetDir)
        {
            AddToLog("Target DIR", targetDir);

            // Check target directory
            DirectoryInfo targetDirInfo = new DirectoryInfo(targetDir);

            if (!targetDirInfo.Exists)
            {
                targetDirInfo.Create();
            }

            int backupCount = 0;

            // Backup each source directory
            foreach (string sourceDir in BackupSettings.SourceDirectories)
            {
                AddToLog("Source DIR", sourceDir);

                DirectoryInfo sourceDirInfo = new DirectoryInfo(sourceDir);

                // Backup source dir
                backupCount += CopyFiles(sourceDirInfo, targetDirInfo);
            }

            return backupCount;
        }

        /// <summary>
        /// Copies directory from source to target.
        /// </summary>
        /// <param name="sourceDirInfo">Source directory to copy</param>
        /// <param name="targetDirInfo">Target directory where backup is to take place</param>
        /// <returns>Number of files backed up</returns>
        protected int CopyFiles(DirectoryInfo sourceDirInfo, DirectoryInfo targetDirInfo)
        {
            int backupCount = 0;

            // Check source exists and whether hidden should be backup up.
            // (Files within a hidden directory are also considered hidden.)
            if (sourceDirInfo.Exists && (!BackupSettings.IgnoreHiddenFiles || (sourceDirInfo.Attributes & FileAttributes.Hidden) == 0))
            {
                AddToLog("Backing up DIR", sourceDirInfo.FullName);

                // Remove root path
                string sourceSubDir = sourceDirInfo.FullName.Substring(sourceDirInfo.Root.Name.Length);

                // Get target path
                string targetDir = Path.Combine(targetDirInfo.FullName, sourceSubDir);

                // Get qualifying files only
                var files = Directory.EnumerateFiles(sourceDirInfo.FullName, "*.*", SearchOption.TopDirectoryOnly).Where(f => !BackupSettings.IsFileExcluded(f));

                // Copy files in current directory
                backupCount = CopyFiles(files, targetDir);

                // Recursive call for sub directories
                foreach (DirectoryInfo subDirInfo in sourceDirInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly).Where(d => !BackupSettings.IsDirectoryExcluded(d.Name)))
                {
                    backupCount += CopyFiles(subDirInfo, targetDirInfo);
                }
            }

            return backupCount;
        }
    }
}
