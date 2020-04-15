using System;
using System.IO;
using System.Linq;

namespace BackupUtilityCore.Tasks
{
    public class BackupTaskCopy : BackupTaskBase
    {
        public override string BackupDescription => "COPY";

        //protected override int BackupFiles(string sourceSubDir, DirectoryInfo sourceDirInfo, DirectoryInfo targetDirInfo)
        //{
        //    return CopyFiles(sourceSubDir, sourceDirInfo, targetDirInfo);
        //}
    }
}
