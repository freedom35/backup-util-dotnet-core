using System;
using System.IO;
using System.Linq;

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

                // Backup each source directory
                foreach (string source in backupSettings.SourceDirectories)
                {
                    try
                    {
                        backupCount += BackupDirectory(backupSettings.TargetDirectory, source);
                    }
                    catch (Exception ex)
                    {
                        AddToLog($"Error backing up {source}: {ex.Message}");
                    }
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

            AddToLog($"Backing up: {sourceDir}");

            int backupCount = 0;

            // Check source exists
            if (sourceDirInfo.Exists)
            {
                DirectoryInfo targetDirInfo = new DirectoryInfo(targetDir);

                // Check target directory
                if (!targetDirInfo.Exists)
                {
                    targetDirInfo.Create();
                }

                backupCount = BackupFiles(sourceDirInfo.Name, targetDirInfo, sourceDirInfo);
            }

            AddToLog($"Backed up {backupCount} files");

            return backupCount;
        }

        private int BackupFiles(string rootDir, DirectoryInfo targetDirInfo, DirectoryInfo sourceDirInfo)
        {
            int backupCount = 0;

            // Check fixed length limits for Windows APIs.
            if (rootDir.Length >= MaxLenDir)
            {
                AddToLog($"Root directory too long: {rootDir}");
            }
            // Files in a hidden directory considered hidden.
            else if (!BackupSettings.IgnoreHiddenFiles || (sourceDirInfo.Attributes & FileAttributes.Hidden) == 0)
            {
                // Get qualifying files only
                var files = Directory.EnumerateFiles(sourceDirInfo.FullName, "*.*", SearchOption.TopDirectoryOnly).Where(f => !BackupSettings.IsFileExcluded(f));

                // Check each file
                foreach (string file in files)
                {
                    // Check fixed length limits for Windows APIs
                    if (file.Length >= MaxLenPath)
                    {
                        AddToLog($"File path too long: {file}");
                    }
                    else if (BackupFile(file, rootDir, targetDirInfo))
                    {
                        // Keep track of files backed up
                        backupCount++;
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

        private bool BackupFile(string fileName, string rootDir, DirectoryInfo targetDirInfo)
        {
            FileInfo sourceFileInfo = new FileInfo(fileName);

            string targetDir = Path.Combine(targetDirInfo.FullName, rootDir);
            string targetPath = Path.Combine(targetDir, sourceFileInfo.Name);

            // Check fixed length limits for OS APIs
            if (targetDir.Length >= MaxLenDir || targetPath.Length >= MaxLenPath)
            {
                AddToLog($"Target path too long: {sourceFileInfo.Name}");
            }
            else
            {
                FileInfo targetFileInfo = new FileInfo(targetPath);

                // Check whether file eligible
                bool eligible = !BackupSettings.IgnoreHiddenFiles || (targetFileInfo.Attributes & FileAttributes.Hidden) != 0;

                // Check if not already backed up, or source file changed.
                if (eligible && (!targetFileInfo.Exists || !targetFileInfo.LastWriteTimeUtc.Equals(sourceFileInfo.LastWriteTimeUtc)))
                {
                    AddToLog($"Backing up: {rootDir}\\{sourceFileInfo.Name}");

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
                    return true;
                }
            }

            // Not backed up
            return false;
        }
    }
}
