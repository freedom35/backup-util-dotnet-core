using System;
using System.IO;
using System.Linq;

namespace BackupUtilityCore.Tasks
{
    /// <summary>
    /// Backup task for syncing target directory with source directories.
    /// </summary>
    public sealed class BackupTaskSync : BackupTaskBase
    {
        protected override BackupType BackupType => BackupType.Sync;

        /// <summary>
        /// Syncs target directory with source directories.
        /// </summary>
        /// <returns>Number of new files backed up</returns>
        protected override int PerformBackup()
        {
            string targetDir = BackupSettings.TargetDirectory;

            AddToLog("Target DIR", targetDir);

            DirectoryInfo targetDirInfo = new DirectoryInfo(targetDir);

            // Check to create target directory
            if (!targetDirInfo.Exists)
            {
                targetDirInfo.Create();
            }

            int backupCount = 0;

            // Sync each source directory
            foreach (string sourceDir in BackupSettings.SourceDirectories)
            {
                AddToLog("Source DIR", sourceDir);

                // Get source path without root
                DirectoryInfo sourceDirInfo = new DirectoryInfo(sourceDir);

                // Remove common root path
                string sourceSubDir = GetSourceSubDir(sourceDirInfo.FullName, targetDirInfo.FullName);

                // Append source sub-dir to target
                string targetSubDir = Path.Combine(targetDir, sourceSubDir);

                // Only remove files from within source directories, not entire target dir - 
                // may be other files in there that need to be kept.
                backupCount += SyncDirectories(sourceDir, targetSubDir);
            }

            return backupCount;
        }

        /// <summary>
        /// Remove directories/files from target that are not in source,
        /// then copies in new/updated files.
        /// </summary>
        private int SyncDirectories(string sourceDir, string targetDir)
        {
            AddToLog("Syncing DIR", sourceDir);

            DirectoryInfo sourceDirInfo = new DirectoryInfo(sourceDir);
            DirectoryInfo targetDirInfo = new DirectoryInfo(targetDir);

            // Get qualifying files only
            var sourceFiles = Directory.EnumerateFiles(sourceDirInfo.FullName, "*.*", SearchOption.TopDirectoryOnly).Where(f => !BackupSettings.IsFileTypeExcluded(f));

            // Check if previous backup taken place
            if (targetDirInfo.Exists)
            {
                //////////////////////////////////////////////////////
                // Remove directories from target not in source
                // (Remove before copying new files to free up space)
                //////////////////////////////////////////////////////

                // Get current source/target directories
                DirectoryInfo[] sourceDirectories = sourceDirInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly).ToArray();
                DirectoryInfo[] targetDirectories = targetDirInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly).ToArray();

                // Need to check for directories that exist in target but not in source
                foreach (DirectoryInfo target in targetDirectories)
                {
                    // Only sub-dir name will match (ignore case), full paths are from different locations
                    bool remove = !sourceDirectories.Any(source => string.Compare(source.Name, target.Name, true) == 0);

                    // Remove if directory is now excluded
                    remove |= BackupSettings.IsDirectoryExcluded(target.Name);

                    // Remove if hidden options changed
                    remove |= BackupSettings.IgnoreHiddenFiles && (target.Attributes & FileAttributes.Hidden) > 0;

                    if (remove)
                    {
                        try
                        {
                            DeleteDirectory(target);
                        }
                        catch (IOException ie)
                        {
                            AddToLog("SYNC ERROR", ie.Message);
                        }
                    }
                }

                //////////////////////////////////////////////////////
                // Remove files from target not in source
                // (Remove before copying new files to free up space)
                //////////////////////////////////////////////////////                

                // Get names only as lowercase for comparison
                string[] sourceFileNames = sourceFiles.Select(f => Path.GetFileName(f).ToLower()).ToArray();

                // Get full paths to target (maintain case, UNIX names can be case sensitive.)
                string[] targetFiles = Directory.EnumerateFiles(targetDirInfo.FullName, "*.*", SearchOption.TopDirectoryOnly).ToArray();

                // Need to check for files that exist in target but not in source
                foreach (string file in targetFiles)
                {
                    // Compare names as lowercase
                    string nameOnly = Path.GetFileName(file).ToLower();

                    // Only name will match, full paths are from different locations
                    bool remove = !sourceFileNames.Contains(nameOnly);

                    // Remove if ext is now excluded
                    remove |= BackupSettings.IsFileTypeExcluded(file);

                    // Remove if hidden options changed
                    remove |= BackupSettings.IgnoreHiddenFiles && (File.GetAttributes(file) & FileAttributes.Hidden) > 0;

                    if (remove)
                    {
                        try
                        {
                            // Delete using full path
                            DeleteFile(file);
                        }
                        catch (IOException ie)
                        {
                            AddToLog("SYNC ERROR", ie.Message);
                        }
                    }
                }
            }

            int backupCount = 0;

            // Check whether backing up hidden directories
            if (!BackupSettings.IgnoreHiddenFiles || (sourceDirInfo.Attributes & FileAttributes.Hidden) == 0)
            {
                //////////////////////////////////////////////////////
                // Copy files from source not in target
                // (Or replace old files)
                //////////////////////////////////////////////////////
                backupCount = CopyFiles(sourceFiles, targetDir);

                /////////////////////////////////////////////
                // Sync sub directories
                /////////////////////////////////////////////
                foreach (DirectoryInfo subDirInfo in sourceDirInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly).Where(d => !BackupSettings.IsDirectoryExcluded(d.Name)))
                {
                    // Get sub-directories
                    string subSourceDir = Path.Combine(sourceDir, subDirInfo.Name);
                    string subTargetDir = Path.Combine(targetDir, subDirInfo.Name);

                    // Recursive call
                    backupCount += SyncDirectories(subSourceDir, subTargetDir);
                }
            }

            return backupCount;
        }
    }
}
