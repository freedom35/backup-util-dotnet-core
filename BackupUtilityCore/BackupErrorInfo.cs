namespace BackupUtilityCore
{
    /// <summary>
    /// Information on backup error.
    /// </summary>
    public sealed class BackupErrorInfo(BackupResult result, string sourceFile, string targetDir)
    {
        /// <summary>
        /// Result of backup attempt.
        /// </summary>
        public BackupResult Result
        {
            get;
            set;
        } = result;

        /// <summary>
        /// Name/path of source file.
        /// </summary>
        public string SourceFile
        {
            get;
            private set;
        } = sourceFile;

        /// <summary>
        /// Name/path of target directory.
        /// </summary>
        public string TargetDir
        {
            get;
            private set;
        } = targetDir;
    }
}
