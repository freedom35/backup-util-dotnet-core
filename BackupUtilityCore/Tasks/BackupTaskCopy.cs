using System;
using System.IO;
using System.Linq;

namespace BackupUtilityCore.Tasks
{
    public class BackupTaskCopy : BackupTaskBase
    {
        protected override int BackupFiles(string sourceSubDir, DirectoryInfo sourceDirInfo, DirectoryInfo targetDirInfo)
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
