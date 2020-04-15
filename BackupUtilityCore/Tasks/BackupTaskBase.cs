using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace BackupUtilityCore.Tasks
{
    public abstract class BackupTaskBase
    {
        public BackupTaskBase() : this(null)
        {
        }

        public BackupTaskBase(BackupSettings backupSettings)
        {
            // Set property
            this.BackupSettings = backupSettings ?? default;
        }

        #region Members

        protected readonly List<BackupErrorInfo> backupErrors = new List<BackupErrorInfo>();

        #endregion

        #region Delegates / Events

        public delegate void MessageDelegate(object sender, MessageEventArgs e);
        public event MessageDelegate Log;

        #endregion

        #region Properties

        /// <summary>
        /// Settings associated with backup task.
        /// </summary>
        public BackupSettings BackupSettings
        {
            get;
            set;
        }

        /// <summary>
        /// Number of errors occurred during backup task.
        /// </summary>
        public int ErrorCount
        {
            get => backupErrors.Count;
        }

        #endregion

        /// <summary>
        /// Executes backup using settings in BackupSettings property.
        /// </summary>
        /// <returns>Number of files backed up</returns>
        public int Execute()
        {
            return Execute(BackupSettings);
        }

        /// <summary>
        /// Executes backup using specified settings.
        /// </summary>
        /// <param name="backupSettings">Settings to use for backup.</param>
        /// <returns>Number of files backed up</returns>
        public int Execute(BackupSettings backupSettings)
        {
            this.BackupSettings = backupSettings;

            int backupCount = 0;

            // Validate settings
            if (backupSettings?.Valid == true)
            {
                AddToLog("Target DIR", backupSettings.TargetDirectory);

                // Check target directory
                if (!Directory.Exists(backupSettings.TargetDirectory))
                {
                    Directory.CreateDirectory(backupSettings.TargetDirectory);
                }

                // Ensure reset
                backupErrors.Clear();

                // Backup each source directory
                foreach (string source in backupSettings.SourceDirectories)
                {
                    AddToLog("Source DIR", source);

                    backupCount += BackupDirectory(source, backupSettings.TargetDirectory);
                }

                // Retry errors if possible (depends on issue)
                backupCount += RetryErrors();

                // Log any files that couldn't be backed-up (or failed retry)
                foreach (IGrouping<BackupResult, BackupErrorInfo> retryGroup in backupErrors.GroupBy(e => e.Result))
                {
                    AddToLog($"Unable to backup ({retryGroup.Key.GetDescription()}):");

                    foreach (BackupErrorInfo retryError in retryGroup)
                    {
                        AddToLog($"{retryError.Filename}");
                    }
                }

                AddToLog("COMPLETE", $"Backed up {backupCount} files");
            }

            return backupCount;
        }

        protected void AddToLog(string message)
        {
            AddToLog(message, "");
        }

        protected void AddToLog(string message, string arg)
        {
            // Check event handled
            Log?.Invoke(this, new MessageEventArgs(message, arg));
        }

        /// <summary>
        /// Backs-up directory based on sub-class logic.
        /// (Pure/Abstract - must override)
        /// </summary>
        /// <param name="targetDir">Root target directory</param>
        /// <param name="sourceDir">Source directory</param>
        /// <returns>Number of files backed up</returns>
        private int BackupDirectory(string sourceDir, string targetDir)
        {
            DirectoryInfo sourceDirInfo = new DirectoryInfo(sourceDir);

            int backupCount = 0;

            // Check source exists
            if (sourceDirInfo.Exists)
            {
                DirectoryInfo targetDirInfo = new DirectoryInfo(targetDir);

                // Remove root path
                string sourceSubDir = sourceDirInfo.FullName.Substring(sourceDirInfo.Root.Name.Length);

                backupCount = BackupFiles(sourceSubDir, sourceDirInfo, targetDirInfo);
            }

            return backupCount;
        }

        private int BackupFiles(string sourceSubDir, DirectoryInfo sourceDirInfo, DirectoryInfo targetDirInfo)
        {
            int backupCount = 0;

            // Files in a hidden directory considered hidden.
            if (!BackupSettings.IgnoreHiddenFiles || (sourceDirInfo.Attributes & FileAttributes.Hidden) == 0)
            {
                AddToLog("Backing up DIR", sourceDirInfo.FullName);

                // Get qualifying files only
                var files = Directory.EnumerateFiles(sourceDirInfo.FullName, "*.*", SearchOption.TopDirectoryOnly).Where(f => !BackupSettings.IsFileExcluded(f));

                // Reset error count between each directory
                int errorCount = 0;

                foreach (string file in files)
                {
                    BackupResult result = BackupFile(file, sourceSubDir, targetDirInfo);

                    switch (result)
                    {
                        case BackupResult.OK:
                            // Keep track of (new) files backed up
                            backupCount++;
                            break;

                        case BackupResult.AlreadyBackedUp:
                        case BackupResult.Ineligible:
                            // Do nothing
                            break;

                        default:

                            BackupErrorInfo errorInfo = new BackupErrorInfo(result)
                            {
                                Filename = file,
                                SourceSubDir = sourceSubDir,
                                TargetDirInfo = targetDirInfo
                            };

                            // Add to list for retry
                            backupErrors.Add(errorInfo);

                            // Abort on high error count
                            if (++errorCount > 3)
                            {
                                throw new Exception("Backup aborted due to excessive errors");
                            }

                            break;
                    }
                }

                // Recursive call for sub directories
                foreach (DirectoryInfo subDirInfo in sourceDirInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly).Where(d => !BackupSettings.IsDirectoryExcluded(d.Name)))
                {
                    backupCount += BackupFiles(Path.Combine(sourceSubDir, subDirInfo.Name), subDirInfo, targetDirInfo);
                }
            }

            return backupCount;
        }

        /// <summary>
        /// Backs-up file based on sub-class logic.
        /// (Pure/Abstract - must override)
        /// </summary>
        /// <param name="filename">Name of file to back up</param>
        /// <param name="sourceSubDir">Sub directory of source being backed-up</param>
        /// <param name="targetDirInfo">Target directory where backup to take place</param>
        /// <returns>Result of backup attempt</returns>
        protected abstract BackupResult BackupFile(string filename, string sourceSubDir, DirectoryInfo targetDirInfo);


        private int RetryErrors()
        {
            int backupCount = 0;

            // Get the errors that can be retried
            List<BackupErrorInfo> retryErrors = backupErrors.Where(err => err.Result.CanBeRetried()).ToList();

            if (retryErrors.Count > 0)
            {
                AddToLog("Re-attempting errors...");

                // Times in milliseconds
                const int MaxRetryTime = 3000;
                const int RetryInterval = 500;

                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();

                // Retry while still files remaining, but eventually time-out
                while (retryErrors.Count > 0 && sw.ElapsedMilliseconds < MaxRetryTime)
                {
                    // File likely in-use, wait before re-attempt
                    Thread.Sleep(RetryInterval);

                    // Retry certain errors, check if files finished writing
                    for (int i = 0; i < retryErrors.Count; i++)
                    {
                        BackupErrorInfo errorInfo = retryErrors[0];

                        // Retry backup
                        if (BackupFile(errorInfo.Filename, errorInfo.SourceSubDir, errorInfo.TargetDirInfo) == BackupResult.OK)
                        {
                            // Success
                            backupCount++;
                            retryErrors.RemoveAt(i--);

                            // Also remove from master list
                            backupErrors.Remove(errorInfo);
                        }
                    }
                }

                sw.Stop();
            }

            return backupCount;
        }
    }
}
