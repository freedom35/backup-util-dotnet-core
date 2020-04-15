using System;
using System.IO;

namespace BackupUtilityCore.Tasks
{
    public sealed class BackupTaskSync : BackupTaskBase
    {
        protected override BackupResult BackupFile(string filename, string rootDir, DirectoryInfo targetDirInfo)
        {
            throw new NotImplementedException();
        }
    }
}
