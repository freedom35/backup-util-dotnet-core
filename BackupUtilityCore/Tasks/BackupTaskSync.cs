using System;
using System.Collections.Generic;
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
            AddToLog("TARGET", BackupSettings.TargetDirectory);

            DirectoryInfo targetDirInfo = new(BackupSettings.TargetDirectory);

            // Check to create target directory
            if (!targetDirInfo.Exists)
            {
                targetDirInfo.Create();
            }

            int backupCount = 0;

            // Sync each source directory
            foreach (string sourceDir in BackupSettings.SourceDirectories)
            {
                AddToLog("SOURCE", sourceDir);

                string targetDir = GetTargetDirForSourceDir(sourceDir, BackupSettings.TargetDirectory);

                backupCount += SyncDirectories(sourceDir, targetDir);
            }

            return backupCount;
        }

        /// <summary>
        /// Remove directories/files from target that are not in source,
        /// then copies in new/updated files.
        /// </summary>
        private int SyncDirectories(string sourceDir, string targetDir)
        {
            DirectoryInfo sourceDirInfo = new(sourceDir);
            DirectoryInfo targetDirInfo = new(targetDir);

            // Get qualifying files only
            var sourceFiles = Directory.EnumerateFiles(sourceDirInfo.FullName, "*.*", SearchOption.TopDirectoryOnly).Where(f => !BackupSettings.IsFileTypeExcluded(f));

            // Check if previous backup taken place
            if (targetDirInfo.Exists)
            {
                //////////////////////////////////////////////////////
                // Remove directories from target not in source
                // (Remove before copying new files to free up space)
                //////////////////////////////////////////////////////
                DeleteSourceDirectoriesFromTarget(sourceDirInfo, targetDirInfo);

                //////////////////////////////////////////////////////
                // Remove files from target not in source
                // (Remove before copying new files to free up space)
                //////////////////////////////////////////////////////                
                DeleteSourceFilesFromTarget(sourceFiles, targetDirInfo);
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

        /// <summary>
        /// Deletes any directories in the target directory that are not in the source directory.
        /// </summary>
        private void DeleteSourceDirectoriesFromTarget(DirectoryInfo sourceDirInfo, DirectoryInfo targetDirInfo)
        {
            // Get current source sub directories
            DirectoryInfo[] sourceDirectories = [.. sourceDirInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly)];

            // Get current target sub directories
            DirectoryInfo[] targetDirectories = [.. targetDirInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly)];

            // Get any source directories that are sub directories of the current source dir
            var directoryOverrides = BackupSettings.SourceDirectories.Where(s => s.Length > sourceDirInfo.FullName.Length && s.StartsWith(sourceDirInfo.FullName, StringComparison.OrdinalIgnoreCase))
                        // Convert to target paths for comparison
                        .Select(s => GetTargetDirForSourceDir(s, BackupSettings.TargetDirectory));

            // Need to check for directories that exist in target but not in source
            foreach (DirectoryInfo target in targetDirectories)
            {
                // Only sub-dir name will match (ignore case), full paths are from different locations
                bool remove = !sourceDirectories.Any(source => string.Compare(source.Name, target.Name, true) == 0);

                // Remove if directory is in exclusion list,
                // unless the dir is configured directly as a source directory (overriding the exclusion)
                remove |= BackupSettings.IsDirectoryExcluded(target.Name) && !directoryOverrides.Any(s => s.StartsWith(target.FullName, StringComparison.OrdinalIgnoreCase));

                // Remove if hidden options changed
                remove |= BackupSettings.IgnoreHiddenFiles && (target.Attributes & FileAttributes.Hidden) > 0;

                if (remove)
                {
                    DeleteDirectory(target);
                }
            }
        }

        /// <summary>
        /// Deletes any target files that are not listed in the source array.
        /// </summary>
        private void DeleteSourceFilesFromTarget(IEnumerable<string> sourceFiles, DirectoryInfo targetDirInfo)
        {
            // Get file names for comparison
            string[] sourceFileNames = [.. sourceFiles.Select(f => Path.GetFileName(f))];

            // Get full paths to target (maintain case, UNIX names can be case sensitive.)
            string[] targetFiles = [.. Directory.EnumerateFiles(targetDirInfo.FullName, "*.*", SearchOption.TopDirectoryOnly)];

            // Need to check for files that exist in target but not in source
            foreach (string file in targetFiles)
            {
                // Compare names as lowercase
                string nameOnly = Path.GetFileName(file);

                // Only name will match, full paths are from different locations
                bool remove = !sourceFileNames.Contains(nameOnly);

                // Remove if ext is now excluded
                remove |= BackupSettings.IsFileTypeExcluded(file);

                // Remove file if hidden options changed and should now be ignored
                if (!remove && BackupSettings.IgnoreHiddenFiles)
                {
                    // Check with path separator to avoid partial name matches (e.g. "file1.txt" matching "myfile1.txt")
                    string nameWithPathChar = Path.DirectorySeparatorChar + nameOnly;

                    // Check flag on source file, as target file may be automatically flagged as hidden by UNIX file systems when copied, even if source file is not hidden.
                    // (i.e. if file is prefixed with a dot)
                    if (sourceFiles.FirstOrDefault(f => f.EndsWith(nameWithPathChar)) is string sourcePath)
                    {
                        remove = (File.GetAttributes(sourcePath) & FileAttributes.Hidden) > 0;
                    }
                }

                if (remove)
                {
                    // Delete using full path
                    DeleteFile(file);
                }
            }
        }
    }
}
