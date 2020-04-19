using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace BackupUtilityCore.Tasks
{
    public abstract class BackupTaskBase
    {
        #region Members

        private readonly List<BackupErrorInfo> backupErrors = new List<BackupErrorInfo>();

        #endregion

        #region Delegates / Events

        public delegate void MessageDelegate(object sender, MessageEventArgs e);
        public event MessageDelegate Log;

        #endregion

        #region Properties

        /// <summary>
        /// Description of task.
        /// </summary>
        protected abstract string BackupDescription
        {
            get;
        }

        /// <summary>
        /// Settings associated with backup task.
        /// </summary>
        protected BackupSettings BackupSettings
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

        /// <summary>
        /// Determines whether retry is enabled (if initial backup fails).
        /// </summary>
        public bool RetryEnabled
        {
            get;
            set;
        } = true;

        /// <summary>
        /// Determines the minimum wait time from when file was last written before backing up.
        /// Value in milliseconds.
        /// </summary>
        public int MinFileWriteWaitTime
        {
            get;
            set;
        } = 500;

        #endregion

        /// <summary>
        /// Perform backup based on sub-class logic.
        /// (Pure/Abstract - must override)
        /// </summary>
        /// <returns></returns>
        protected abstract int PerformBackup();

        /// <summary>
        /// Executes backup using specified settings.
        /// </summary>
        /// <param name="backupSettings">Settings to use for backup.</param>
        /// <returns>Number of files backed up</returns>
        public int Run(BackupSettings backupSettings)
        {
            // Update property value
            BackupSettings = backupSettings ?? default;

            // Ensure reset
            backupErrors.Clear();

            int backupCount = 0;

            // Validate settings
            if (BackupSettings?.Valid == true)
            {
                AddToLog($"Running backup ({BackupDescription})...");

                // Run sub-class logic
                backupCount = PerformBackup();

                // Retry errors if possible (depends on issue)
                if (RetryEnabled)
                {
                    backupCount += RetryErrors();
                }

                // Log any files that couldn't be backed-up (or failed retry)
                foreach (IGrouping<BackupResult, BackupErrorInfo> retryGroup in backupErrors.GroupBy(e => e.Result))
                {
                    AddToLog($"Unable to backup ({retryGroup.Key.GetDescription()}):");

                    foreach (BackupErrorInfo retryError in retryGroup)
                    {
                        AddToLog($"{retryError.SourceFile}");
                    }
                }

                AddToLog("COMPLETE", $"Backed up {backupCount} files");
            }

            return backupCount;
        }

        protected void AddToLog(string message)
        {
            AddToLog(message, string.Empty);
        }

        protected void AddToLog(string message, string arg)
        {
            // Check event handled
            Log?.Invoke(this, new MessageEventArgs(message, arg));
        }

        protected int CopyFiles(IEnumerable<string> files, string targetDir)
        {
            int backupCount = 0;

            // Reset error count between each directory
            int errorCount = 0;

            // Copy each file
            foreach (string file in files)
            {
                BackupResult result = CopyFile(file, targetDir);

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
                        // Add file to list for retry
                        backupErrors.Add(new BackupErrorInfo(result, file, targetDir));

                        // Abort on high error count
                        if (++errorCount > 3)
                        {
                            throw new Exception("Backup aborted due to excessive errors");
                        }

                        break;
                }
            }

            return backupCount;
        }

        /// <summary>
        /// Copies file to target directory.
        /// </summary>
        /// <param name="filename">Name of file to back up</param>
        /// <param name="targetDir">Target directory where backup is to take place</param>
        /// <returns>Result of backup attempt</returns>
        protected BackupResult CopyFile(string filename, string targetDir)
        {
            BackupResult result;

            FileInfo sourceFileInfo = new FileInfo(filename);

            // Check whether file eligible
            if (BackupSettings.IgnoreHiddenFiles && (sourceFileInfo.Attributes & FileAttributes.Hidden) != 0)
            {
                result = BackupResult.Ineligible;
            }
            else if ((DateTime.UtcNow - sourceFileInfo.LastWriteTimeUtc).TotalMilliseconds < MinFileWriteWaitTime)
            {
                result = BackupResult.WriteInProgress;
            }
            else
            {
                string targetPath = Path.Combine(targetDir, sourceFileInfo.Name);

                FileInfo targetFileInfo = new FileInfo(targetPath);

                // Check whether file previously backed up (and not changed)
                if (targetFileInfo.Exists && targetFileInfo.LastWriteTimeUtc.Equals(sourceFileInfo.LastWriteTimeUtc))
                {
                    result = BackupResult.AlreadyBackedUp;
                }
                else
                {
                    AddToLog("Backing up file", sourceFileInfo.FullName);

                    try
                    {
                        // Check target directory
                        if (!targetFileInfo.Directory.Exists)
                        {
                            targetFileInfo.Directory.Create();
                        }
                        else if (targetFileInfo.Exists && targetFileInfo.IsReadOnly)
                        {
                            // Modify attributes - ensure can overwrite it.
                            targetFileInfo.Attributes &= ~FileAttributes.ReadOnly;
                        }

                        // Backup file
                        sourceFileInfo.CopyTo(targetFileInfo.FullName, true);

                        // Confirm backed up
                        result = BackupResult.OK;
                    }
                    catch (PathTooLongException pe)
                    {
                        AddToLog("PATH ERROR", pe.Message);

                        // Max length will vary by OS and environment settings.
                        result = BackupResult.PathTooLong;
                    }
                    catch (IOException ie)
                    {
                        AddToLog("I/O ERROR", ie.Message);

                        // File may be locked or in-use by another process
                        result = BackupResult.Exception;
                    }
                }
            }

            return result;
        }

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
                        if (CopyFile(errorInfo.SourceFile, errorInfo.TargetDir) == BackupResult.OK)
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
