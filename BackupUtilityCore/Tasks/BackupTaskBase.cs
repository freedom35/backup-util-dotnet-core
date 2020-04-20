using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace BackupUtilityCore.Tasks
{
    /// <summary>
    /// Base class for backup task.
    /// </summary>
    public abstract class BackupTaskBase
    {
        #region Members

        private readonly List<BackupErrorInfo> backupCopyErrors = new List<BackupErrorInfo>();

        #endregion

        #region Delegates / Events

        public delegate void MessageDelegate(object sender, MessageEventArgs e);
        public event MessageDelegate Log;

        #endregion

        #region Properties

        /// <summary>
        /// Type of backup.
        /// </summary>
        protected abstract BackupType BackupType
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
        /// Determines whether an error occurred during backup task.
        /// </summary>
        public bool CompletedWithoutError
        {
            get => backupCopyErrors.Count == 0;
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
            // Checks before running backup
            CheckSettings(backupSettings);

            // Update property value
            BackupSettings = backupSettings;

            AddToLog($"Running backup ({BackupType.ToString().ToUpper()})...");

            // Ensure reset
            backupCopyErrors.Clear();

            // Run sub-class logic
            int backupCount = PerformBackup();

            // Retry errors if possible (depends on issue)
            if (RetryEnabled)
            {
                backupCount += RetryCopyErrors();
            }

            // Log any files that couldn't be backed-up (or failed retry)
            foreach (IGrouping<BackupResult, BackupErrorInfo> retryGroup in backupCopyErrors.GroupBy(e => e.Result))
            {
                AddToLog($"Unable to backup ({retryGroup.Key.GetDescription()}):");

                foreach (BackupErrorInfo retryError in retryGroup)
                {
                    AddToLog(string.Empty, retryError.SourceFile);
                }
            }

            AddToLog("COMPLETE", $"Backed up {backupCount} new files");

            return backupCount;
        }

        /// <summary>
        /// Throws exception if settings check fails.
        /// </summary>
        private void CheckSettings(BackupSettings backupSettings)
        {
            if (backupSettings == null)
            {
                throw new ArgumentNullException("backupSettings");
            }

            if (Enum.IsDefined(typeof(BackupType), backupSettings.BackupType) && backupSettings.BackupType != BackupType)
            {
                // Something not right
                throw new ArgumentException($"Backup type in settings ({backupSettings.BackupType}) does not match backup task type ({BackupType})");
            }

            // Verify all sources exist before proceeding
            string missingSource = backupSettings.SourceDirectories.FirstOrDefault(d => !Directory.Exists(d));

            // Warn - maybe typo in config
            if (!string.IsNullOrEmpty(missingSource))
            {
                throw new DirectoryNotFoundException($"Source directory does not exist: {missingSource}");
            }
        }

        protected void AddToLog(string message)
        {
            AddToLog(message, string.Empty);
        }

        /// <summary>
        /// Raises logging event.
        /// </summary>
        protected void AddToLog(string message, string arg)
        {
            // Check event handled
            Log?.Invoke(this, new MessageEventArgs(message, arg));
        }

        /// <summary>
        /// Copies source files to target directory.
        /// </summary>
        /// <param name="sourceFiles">File paths to copy</param>
        /// <param name="targetDir">Target directory where to copy files</param>
        /// <returns>Number of files copied</returns>
        protected int CopyFiles(IEnumerable<string> sourceFiles, string targetDir)
        {
            int backupCount = 0;

            // Reset error count between each directory
            int errorCount = 0;

            // Copy each file
            foreach (string file in sourceFiles)
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
                        backupCopyErrors.Add(new BackupErrorInfo(result, file, targetDir));

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
                if (targetFileInfo.Exists && targetFileInfo.Length == sourceFileInfo.Length && targetFileInfo.LastWriteTimeUtc.Equals(sourceFileInfo.LastWriteTimeUtc))
                {
                    result = BackupResult.AlreadyBackedUp;
                }
                else
                {
                    AddToLog("COPYING", sourceFileInfo.FullName);

                    try
                    {
                        // Check target directory
                        if (!targetFileInfo.Directory.Exists)
                        {
                            targetFileInfo.Directory.Create();

                            // Preserve source directory attributes (hidden etc.)
                            targetFileInfo.Directory.Attributes = sourceFileInfo.Directory.Attributes;
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
                        AddToLog("ERROR", pe.Message);

                        // Max length will vary by OS and environment settings.
                        result = BackupResult.PathTooLong;
                    }
                    catch (IOException ie)
                    {
                        AddToLog("ERROR", ie.Message);

                        // File may be locked or in-use by another process
                        result = BackupResult.Exception;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Deletes file, even if read-only.
        /// </summary>
        protected void DeleteFile(string filename)
        {
            FileInfo fileInfo = new FileInfo(filename);

            if (fileInfo.Exists)
            {
                AddToLog($"DELETING", fileInfo.FullName);

                try
                {
                    // Make sure file is not read-only before we delete it.
                    fileInfo.Attributes = FileAttributes.Normal;
                    fileInfo.Delete();
                }
                catch (UnauthorizedAccessException ue)
                {
                    AddToLog("ERROR", ue.Message);
                }
                catch (IOException ie)
                {
                    AddToLog("ERROR", ie.Message);
                }
            }
        }

        /// <summary>
        /// Deletes directory, even if flagged as read-only.
        /// </summary>
        protected void DeleteDirectory(DirectoryInfo directoryInfo)
        {
            AddToLog($"DELETING", directoryInfo.FullName);

            // Remove read-only from sub directories
            RemoveReadOnlyAttributes(directoryInfo.GetDirectories());

            // Remove from root
            RemoveReadOnlyFromDirectory(directoryInfo);

            try
            {
                // Delete recursively
                directoryInfo.Delete(true);
            }
            catch (UnauthorizedAccessException ue)
            {
                AddToLog("ERROR", ue.Message);
            }
            catch (IOException ie)
            {
                AddToLog("ERROR", ie.Message);
            }
        }

        /// <summary>
        /// Removes read-only attribute from directories and their sub-directories. 
        /// </summary>
        private void RemoveReadOnlyAttributes(IEnumerable<DirectoryInfo> directories)
        {
            foreach (DirectoryInfo di in directories)
            {
                RemoveReadOnlyFromDirectory(di);

                // Recursive - remove from sub dirs too
                RemoveReadOnlyAttributes(di.EnumerateDirectories());
            }
        }

        /// <summary>
        /// Removes read-only attribute from directory.
        /// (Also removed from files in directory)
        /// </summary>
        private void RemoveReadOnlyFromDirectory(DirectoryInfo di)
        {
            // Ensure not read-only so can be deleted 
            di.Attributes = FileAttributes.Normal;

            // Remove from files too
            foreach (FileInfo fi in di.EnumerateFiles())
            {
                fi.Attributes = FileAttributes.Normal;
            }
        }

        /// <summary>
        /// Will re-attempt to backup files that failed during original backup.
        /// (Retry depends on reason they failed)
        /// </summary>
        /// <returns>Number of files successfully backed-up by retry</returns>
        private int RetryCopyErrors()
        {
            int backupCount = 0;

            // Get the errors that can be retried
            List<BackupErrorInfo> retryErrors = backupCopyErrors.Where(err => err.Result.CanBeRetried()).ToList();

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
                            backupCopyErrors.Remove(errorInfo);
                        }
                    }
                }

                sw.Stop();
            }

            return backupCount;
        }

        /// <summary>
        /// Gets the directory name where the source starts.
        /// </summary>
        /// <param name="sourceDir">Name of source directory</param>
        /// <param name="targetDir">Name of target directory</param>
        /// <returns>Part of source directory where source/target directories differ</returns>
        protected string GetSourceSubDir(string sourceDir, string targetDir)
        {
            // Ensure stay within array bounds
            int maxLen = Math.Min(sourceDir.Length, targetDir.Length);

            // Don't return index at root
            int i = Path.GetPathRoot(sourceDir).Length;

            // Find first char where directories differ
            for (; i < maxLen; i++)
            {
                if (sourceDir[i] != targetDir[i])
                {
                    break;
                }
            }

            // Return string after position where they differ
            return sourceDir.Substring(i);
        }
    }
}
