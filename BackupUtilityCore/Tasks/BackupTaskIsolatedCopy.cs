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
        public const string DirDateFormat = "yyyy-MM-dd HHmmss";

        protected override string BackupDescription => "ISO-COPY";

        protected override int PerformBackup()
        {
            // Delete old backups first before backup - free up space.
            if (BackupSettings.MaxIsololationDays > 0)
            {
                DeleteOldBackups();
            }

            string isolatedTargetDir = GetBackupLocation();

            return CopyDirectoryTo(isolatedTargetDir);
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

        /// <summary>
        /// Tries to parse isolated date/time from directory name.
        /// </summary>
        /// <param name="dir">Name of directory</param>
        /// <param name="dirDate">Parsed date/time from directory</param>
        /// <returns>true if parse successful</returns>
        public static bool TryParseDateFromIsolatedDirectory(string dir, out DateTime dirDate)
        {
            // Check for additional identifier (and remove)
            if (dir.Length > DirDateFormat.Length)
            {
                dir = dir.Substring(0, DirDateFormat.Length);
            }

            // TryParse directory names for ones with date formats
            return DateTime.TryParseExact(dir, DirDateFormat, null, System.Globalization.DateTimeStyles.None, out dirDate);
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

                    // TryParse directory names for ones with date formats
                    if (TryParseDateFromIsolatedDirectory(name, out DateTime dirDate))
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
