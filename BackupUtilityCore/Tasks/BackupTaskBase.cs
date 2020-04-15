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

        public abstract string BackupDescription
        {
            get;
        }

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
            // Update property value
            BackupSettings = backupSettings;

            // Ensure reset
            backupErrors.Clear();

            int backupCount = 0;

            // Validate settings
            if (backupSettings?.Valid == true)
            {
                AddToLog($"Running backup ({BackupDescription})");

                PerformBackup();

                // Retry errors if possible (depends on issue)
                backupCount += RetryErrors();

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

        protected virtual int PerformBackup()
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

                backupCount += BackupDirectory(source, targetDir);
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

        /// <summary>
        /// Backs-up directory based on sub-class logic.
        /// (Pure/Abstract - must override)
        /// </summary>
        /// <param name="targetDir">Root target directory</param>
        /// <param name="sourceDir">Source directory</param>
        /// <returns>Number of files backed up</returns>
        protected int BackupDirectory(string sourceDir, string targetDir)
        {
            DirectoryInfo sourceDirInfo = new DirectoryInfo(sourceDir);

            int backupCount = 0;

            // Check source exists
            if (sourceDirInfo.Exists)
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
            int backupCount = 0;

            // Files in a hidden directory considered hidden.
            if (!BackupSettings.IgnoreHiddenFiles || (sourceDirInfo.Attributes & FileAttributes.Hidden) == 0)
            {
                AddToLog("Backing up DIR", sourceDirInfo.FullName);

                // Get target path
                string targetDir = Path.Combine(targetDirInfo.FullName, sourceSubDir);

                // Get qualifying files only
                var files = Directory.EnumerateFiles(sourceDirInfo.FullName, "*.*", SearchOption.TopDirectoryOnly).Where(f => !BackupSettings.IsFileExcluded(f));

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

                // Recursive call for sub directories
                foreach (DirectoryInfo subDirInfo in sourceDirInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly).Where(d => !BackupSettings.IsDirectoryExcluded(d.Name)))
                {
                    backupCount += CopyFiles(Path.Combine(sourceSubDir, subDirInfo.Name), subDirInfo, targetDirInfo);
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
            else if ((DateTime.UtcNow - sourceFileInfo.LastWriteTimeUtc).TotalMilliseconds < 500)
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

        protected void DeleteFile(string filename)
        {
            DeleteFile(new FileInfo(filename));
        }

        protected void DeleteFile(FileInfo fileInfo)
        {
            if (fileInfo.Exists)
            {
                // Make sure file is not read-only before we delete it.
                fileInfo.Attributes &= ~FileAttributes.ReadOnly;
                fileInfo.Delete();
            }
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
