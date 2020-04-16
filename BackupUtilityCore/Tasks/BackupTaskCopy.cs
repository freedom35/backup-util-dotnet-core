using System;
using System.IO;
using System.Linq;

namespace BackupUtilityCore.Tasks
{
    public class BackupTaskCopy : BackupTaskBase
    {
        public override string BackupDescription => "COPY";
    }
}
