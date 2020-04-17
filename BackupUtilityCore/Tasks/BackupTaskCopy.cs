using System.IO;
using System.Linq;

namespace BackupUtilityCore.Tasks
{
    public class BackupTaskCopy : BackupTaskBase
    {
        protected override string BackupDescription => "COPY";

        protected override int PerformBackup()
        {
            string targetDir = BackupSettings.TargetDirectory;

            AddToLog("Target DIR", targetDir);

            // Check target directory
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            int backupCount = 0;

            // Backup each source directory
            foreach (string source in BackupSettings.SourceDirectories)
            {
                AddToLog("Source DIR", source);

                backupCount += CopyDirectory(source, targetDir);
            }

            return backupCount;
        }

        /// <summary>
        /// Copies directory from source to target.
        /// </summary>
        /// <param name="targetDir">Root target directory</param>
        /// <param name="sourceDir">Source directory</param>
        /// <returns>Number of files backed up</returns>
        protected int CopyDirectory(string sourceDir, string targetDir)
        {
            DirectoryInfo sourceDirInfo = new DirectoryInfo(sourceDir);

            int backupCount = 0;

            // Check source exists and whether hidden should be backup up.
            // (Files within a hidden directory are also considered hidden.)
            if (sourceDirInfo.Exists && (!BackupSettings.IgnoreHiddenFiles || (sourceDirInfo.Attributes & FileAttributes.Hidden) == 0))
            {
                DirectoryInfo targetDirInfo = new DirectoryInfo(targetDir);

                // Remove root path
                string sourceSubDir = sourceDirInfo.FullName.Substring(sourceDirInfo.Root.Name.Length);

                backupCount = CopyFiles(sourceSubDir, sourceDirInfo, targetDirInfo);
            }

            return backupCount;
        }

        /// <summary>
        /// Copies directory from source to target.
        /// </summary>
        /// <param name="sourceSubDir">Sub directory of source being backed-up</param>
        /// <param name="sourceDirInfo">Source directory to copy</param>
        /// <param name="targetDirInfo">Target directory where backup is to take place</param>
        /// <returns>Number of files backed up</returns>
        protected int CopyFiles(string sourceSubDir, DirectoryInfo sourceDirInfo, DirectoryInfo targetDirInfo)
        {
            AddToLog("Backing up DIR", sourceDirInfo.FullName);

            // Get target path
            string targetDir = Path.Combine(targetDirInfo.FullName, sourceSubDir);

            // Get qualifying files only
            var files = Directory.EnumerateFiles(sourceDirInfo.FullName, "*.*", SearchOption.TopDirectoryOnly).Where(f => !BackupSettings.IsFileExcluded(f));

            // Copy files in current directory
            int backupCount = CopyFiles(files, targetDir);

            // Recursive call for sub directories
            foreach (DirectoryInfo subDirInfo in sourceDirInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly).Where(d => !BackupSettings.IsDirectoryExcluded(d.Name)))
            {
                backupCount += CopyFiles(Path.Combine(sourceSubDir, subDirInfo.Name), subDirInfo, targetDirInfo);
            }

            return backupCount;
        }
    }
}
