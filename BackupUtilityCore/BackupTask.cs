using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace BackupUtilityCore
{
    public sealed class BackupTask
    {
        public BackupTask() : this(null)
        {
        }

        public BackupTask(BackupSettings backupSettings)
        {
            // Set property
            this.BackupSettings = backupSettings ?? default;

            // Check platform type
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                // Approx - TBD
                MaxLenDir = 1000;
                MaxLenPath = 1000;
            }
            else
            {
                // Default limits based on pre-Win10 Windows APIs (shortest)
                MaxLenDir = 248;
                MaxLenPath = 260;
            }
        }

        #region Members

        private readonly int MaxLenDir;
        private readonly int MaxLenPath;

        private readonly List<BackupErrorInfo> backupErrors = new List<BackupErrorInfo>();

        #endregion

        #region Delegates / Events

        public delegate void MessageDelegate(object sender, MessageEventArgs e);
        public event MessageDelegate Log;

        #endregion

        #region Properties

        public BackupSettings BackupSettings
        {
            get;
            set;
        }

        #endregion

        public int Execute()
        {
            return Execute(BackupSettings);
        }

        public int Execute(BackupSettings backupSettings)
        {
            this.BackupSettings = backupSettings;

            int backupCount = 0;
            
            // Validate settings
            if (backupSettings?.Valid == true)
            {
                AddToLog($"Target DIR: {backupSettings.TargetDirectory}");

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
                    backupCount += BackupDirectory(backupSettings.TargetDirectory, source);
                }

                // Retry if possible
                backupCount += RetryErrors();

                // Log files that couldn't be backed-up
                foreach (BackupErrorInfo retryError in backupErrors)
                {
                    AddToLog($"Unable to backup: {retryError.Filename} ({retryError.Result.GetDescription()})");
                }
            }

            return backupCount;
        }

        private void AddToLog(string message)
        {
            // Check event handled
            Log?.Invoke(this, new MessageEventArgs(message));
        }

        private int BackupDirectory(string targetDir, string sourceDir)
        {
            DirectoryInfo sourceDirInfo = new DirectoryInfo(sourceDir);

            AddToLog($"Source DIR: {sourceDir}");

            int backupCount = 0;

            // Check source exists
            if (sourceDirInfo.Exists)
            {
                DirectoryInfo targetDirInfo = new DirectoryInfo(targetDir);

                // Remove root path
                string rootDir = sourceDirInfo.FullName.Substring(sourceDirInfo.Root.Name.Length);

                backupCount = BackupFiles(rootDir, targetDirInfo, sourceDirInfo);
            }

            AddToLog($"Backed up {backupCount} files");

            return backupCount;
        }

        private int BackupFiles(string rootDir, DirectoryInfo targetDirInfo, DirectoryInfo sourceDirInfo)
        {
            int backupCount = 0;

            // Files in a hidden directory considered hidden.
            if (!BackupSettings.IgnoreHiddenFiles || (sourceDirInfo.Attributes & FileAttributes.Hidden) == 0)
            {
                // Get qualifying files only
                var files = Directory.EnumerateFiles(sourceDirInfo.FullName, "*.*", SearchOption.TopDirectoryOnly).Where(f => !BackupSettings.IsFileExcluded(f));

                foreach (string file in files)
                {
                    BackupResult result = BackupFile(file, rootDir, targetDirInfo);

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
                                RootDir = rootDir,
                                TargetDirInfo = targetDirInfo
                            };

                            // Add to queue for retry
                            backupErrors.Add(errorInfo);

                            // Abort on high error count
                            if (backupErrors.Count > 5)
                            {
                                throw new Exception("Backup aborted due to excessive errors");
                            }

                            break;
                    }
                }

                // Recursive call for sub directories
                foreach (DirectoryInfo subDirInfo in sourceDirInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly).Where(d => !BackupSettings.IsDirectoryExcluded(d.Name)))
                {
                    backupCount += BackupFiles(Path.Combine(rootDir, subDirInfo.Name), targetDirInfo, subDirInfo);
                }
            }

            return backupCount;
        }

        private BackupResult BackupFile(string filename, string rootDir, DirectoryInfo targetDirInfo)
        {
            BackupResult result;

            FileInfo sourceFileInfo = new FileInfo(filename);

            // Get target path
            string targetDir = Path.Combine(targetDirInfo.FullName, rootDir);
            string targetPath = Path.Combine(targetDir, sourceFileInfo.Name);

            // Check fixed length limits for OS APIs
            if (targetDir.Length >= MaxLenDir || targetPath.Length >= MaxLenPath)
            {
                //AddToLog($"Target path too long for file: {sourceFileInfo.Name}");

                result = BackupResult.PathTooLong;
            }
            else if(BackupSettings.IgnoreHiddenFiles && (sourceFileInfo.Attributes & FileAttributes.Hidden) != 0)
            {
                result = BackupResult.Ineligible;
            }
            else if ((DateTime.UtcNow - sourceFileInfo.LastWriteTimeUtc).TotalMilliseconds < 500)
            {
                result = BackupResult.WriteInProgress;
            }
            else
            {
                FileInfo targetFileInfo = new FileInfo(targetPath);

                // Check whether file previously backed up (and not changed)
                if (targetFileInfo.Exists && targetFileInfo.LastWriteTimeUtc.Equals(sourceFileInfo.LastWriteTimeUtc))
                {
                    result = BackupResult.AlreadyBackedUp;
                }
                else
                {
                    AddToLog($"Backing up: {filename}");

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
                    catch (IOException ex)
                    {
                        AddToLog($"ERROR: {ex.Message}");

                        // File may be locked or in-use by another process
                        result = BackupResult.Error;
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
                        if (BackupFile(errorInfo.Filename, errorInfo.RootDir, errorInfo.TargetDirInfo) == BackupResult.OK)
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
