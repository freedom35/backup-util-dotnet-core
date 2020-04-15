using System;
using System.IO;

namespace BackupUtilityCore.Tasks
{
    /// <summary>
    /// Same logic as copy, just starting in a new root dir with each copy.
    /// </summary>
    public sealed class BackupTaskIsolatedCopy : BackupTaskCopy
    {
        /// <summary>
        /// Date format used for directory names.
        /// </summary>
        private const string DirDateFormat = "yyyy-MM-dd HHmmss";

        protected override int PerformBackup()
        {
            string targetDir = GetBackupLocation();

            AddToLog("Target DIR", targetDir);

            // Delete old backups first before backup - free up space.
            DeleteOldBackups();

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
        /// Task creates a separate directory for each backup.
        /// </summary>
        private string GetBackupLocation()
        {
            // Get original target root
            string targetRoot = BackupSettings.TargetDirectory;

            // Create unique(ish) sub directory based on time
            string targetSub = DateTime.Now.ToString(DirDateFormat);

            string targetPath = Path.Combine(targetRoot, targetSub);

            // Unlikely going to be another backup in less than a second, but verify
            int index = 1;
            while (Directory.Exists(targetPath))
            {
                targetPath = Path.Combine(targetRoot, $"{targetSub}-{index++}");
            }

            return targetPath;
        }

        private void DeleteOldBackups()
        {
            AddToLog("Deleting old backups...");

            int maxAgeDays = BackupSettings.MaxIsololationDays;
            DateTime now = DateTime.Now;

            DirectoryInfo targetRoot = new DirectoryInfo(BackupSettings.TargetDirectory);

            if (targetRoot.Exists)
            {
                // Enumerate directories in root
                foreach (DirectoryInfo dirInfo in targetRoot.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    // Get name only, not full path
                    string name = dirInfo.Name;

                    // Check for additional identifier (and remove)
                    if (name.Length > DirDateFormat.Length)
                    {
                        name = name.Substring(0, DirDateFormat.Length);
                    }

                    // TryParse directory names for ones with date formats
                    if (DateTime.TryParseExact(name, DirDateFormat, null, System.Globalization.DateTimeStyles.None, out DateTime dirDate))
                    {
                        // Check age of backup
                        if ((now - dirDate).TotalDays > maxAgeDays)
                        {
                            try
                            {
                                AddToLog($"Deleting: {dirInfo.FullName}");

                                // Delete recursively
                                dirInfo.Delete(true);
                            }
                            catch (IOException ie)
                            {
                                AddToLog("I/O ERROR", ie.Message);
                            }
                        }
                    }
                }
            }
        }
    }
}
