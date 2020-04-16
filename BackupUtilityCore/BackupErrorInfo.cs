namespace BackupUtilityCore
{
    /// <summary>
    /// Information on backup error.
    /// </summary>
    public sealed class BackupErrorInfo
    {
        public BackupErrorInfo(BackupResult result, string sourceFile, string targetDir)
        {
            Result = result;
            SourceFile = sourceFile;
            TargetDir = targetDir;
        }

        /// <summary>
        /// Result of backup attempt.
        /// </summary>
        public BackupResult Result
        {
            get;
            private set;
        }

        /// <summary>
        /// Name/path of source file.
        /// </summary>
        public string SourceFile
        {
            get;
            private set;
        }

        /// <summary>
        /// Name/path of target directory.
        /// </summary>
        public string TargetDir
        {
            get;
            private set;
        }
    }
}
