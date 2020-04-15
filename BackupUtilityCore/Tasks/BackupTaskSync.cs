using System;
using System.IO;
using System.Linq;

namespace BackupUtilityCore.Tasks
{
    public sealed class BackupTaskSync : BackupTaskBase
    {
        /// <summary>
        /// When syncing, only remove files from within source directories, not entire target dir - 
        /// may be other files in there that need to be kept.
        /// </summary>
        /// <param name="sourceSubDir"></param>
        /// <param name="sourceDirInfo"></param>
        /// <param name="targetDirInfo"></param>
        /// <returns></returns>
        protected override int BackupFiles(string sourceSubDir, DirectoryInfo sourceDirInfo, DirectoryInfo targetDirInfo)
        {
            int backupCount = 0;

            // Need to check for directories that exist in target but not in source

            // Need to check for files that exist in target but not in source

            // Remove from target if it exists
            //DeleteFile(targetPath);

            // Files in a hidden directory considered hidden.
            if (!BackupSettings.IgnoreHiddenFiles || (sourceDirInfo.Attributes & FileAttributes.Hidden) == 0)
            {
                AddToLog("Syncing DIR", sourceDirInfo.FullName);

                // Get qualifying files only
                var files = Directory.EnumerateFiles(sourceDirInfo.FullName, "*.*", SearchOption.TopDirectoryOnly).Where(f => !BackupSettings.IsFileExcluded(f));

                // Reset error count between each directory
                int errorCount = 0;

                foreach (string file in files)
                {
                    BackupResult result = CopyFile(file, sourceSubDir, targetDirInfo);

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
    }
}
