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
        /// Target location of source directories
        /// </summary>
        private string[] sourceDirsInTarget = Array.Empty<string>();

        /// <summary>
        /// Syncs target directory with source directories.
        /// </summary>
        /// <returns>Number of new files backed up</returns>
        protected override int PerformBackup()
        {
            string targetDir = BackupSettings.TargetDirectory;

            // Get target location of source directories
            sourceDirsInTarget = GetSourceSubDirs(BackupSettings.SourceDirectories, targetDir);

            AddToLog("TARGET", targetDir);

            DirectoryInfo targetDirInfo = new(targetDir);

            // Check to create target directory
            if (!targetDirInfo.Exists)
            {
                targetDirInfo.Create();
            }
            else
            {
                // Remove directories no longer in config
                RemoveOrphanedBranches(sourceDirsInTarget, targetDirInfo);
            }

            int backupCount = 0;

            // Should be same, but safety check
            int maxSource = Math.Min(BackupSettings.SourceDirectories.Length, sourceDirsInTarget.Length);

            // Sync each source directory
            for (int i = 0; i < maxSource; i++)
            {
                string sourceDir = BackupSettings.SourceDirectories[i];

                AddToLog("SOURCE", sourceDir);

                // Sync within the source directories
                backupCount += SyncDirectories(sourceDir, sourceDirsInTarget[i]);
            }

            return backupCount;
        }

        /// <summary>
        /// Gets the sub directory where each source will be located in the target directory.
        /// </summary>
        private static string[] GetSourceSubDirs(string[] sourceDirectories, string targetDir)
        {
            List<string> dirs = new();

            foreach (string source in sourceDirectories)
            {
                // Remove common root path
                string sourceSubDir = GetSourceSubDir(source, targetDir);

                // Append source sub-dir to target
                string sourceDirInTarget = Path.Combine(targetDir, sourceSubDir);

                dirs.Add(sourceDirInTarget);
            }

            return dirs.ToArray();
        }

        /// <summary>
        /// Removes directories from branches in target that are no longer in any source directories.
        /// </summary>
        private void RemoveOrphanedBranches(string[] sourceSubDirs, DirectoryInfo targetDirInfo)
        {
            // Get current target directories
            DirectoryInfo[] targetDirectories = targetDirInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly).ToArray();

            // Need to check for directories that exist in target but not in source
            foreach (DirectoryInfo targetSubInfo in targetDirectories)
            {
                // Check for sub directory in sources
                string? targetSub = sourceSubDirs.FirstOrDefault(s => s.StartsWith(targetSubInfo.FullName, StringComparison.OrdinalIgnoreCase));

                // If directory not found, then orphaned - delete
                // Otherwise, keep searching branch until full directories match
                if (string.IsNullOrEmpty(targetSub))
                {
                    DeleteDirectory(targetSubInfo);
                }
                else if (targetSub.Length != targetSubInfo.FullName.Length)
                {
                    // if not the same dir as source, keep checking sub directories
                    RemoveOrphanedBranches(sourceSubDirs, targetSubInfo);
                }
            }
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
            // Get current source directories
            DirectoryInfo[] sourceDirectories = sourceDirInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly).ToArray();

            // Delete missing ones
            DeleteSourceDirectoriesFromTarget(sourceDirectories, targetDirInfo);
        }

        /// <summary>
        /// Deletes any directories in the target directory that are not listed in source array.
        /// </summary>
        private void DeleteSourceDirectoriesFromTarget(DirectoryInfo[] sourceDirectories, DirectoryInfo targetDirInfo)
        {
            // Get current target directories
            DirectoryInfo[] targetDirectories = targetDirInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly).ToArray();

            // Need to check for directories that exist in target but not in source
            foreach (DirectoryInfo target in targetDirectories)
            {
                // Only sub-dir name will match (ignore case), full paths are from different locations
                bool remove = !sourceDirectories.Any(source => string.Compare(source.Name, target.Name, true) == 0);

                // Remove if directory is now excluded, but not overridden by a source
                remove |= BackupSettings.IsDirectoryExcluded(target.Name) && !sourceDirsInTarget.Any(s => s.StartsWith(target.FullName, StringComparison.OrdinalIgnoreCase));

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
                    // Delete using full path
                    DeleteFile(file);
                }
            }
        }
    }
}
