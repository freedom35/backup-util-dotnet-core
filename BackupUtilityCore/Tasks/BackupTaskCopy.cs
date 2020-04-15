using System;
using System.IO;

namespace BackupUtilityCore.Tasks
{
    public class BackupTaskCopy : BackupTaskBase
    {
        protected override BackupResult BackupFile(string filename, string sourceSubDir, DirectoryInfo targetDirInfo)
        {
            BackupResult result;

            FileInfo sourceFileInfo = new FileInfo(filename);

            // Get target path
            string targetDir = Path.Combine(targetDirInfo.FullName, sourceSubDir);
            string targetPath = Path.Combine(targetDir, sourceFileInfo.Name);

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
    }
}
