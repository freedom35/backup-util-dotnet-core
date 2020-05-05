namespace BackupUtilityCore
{
    /// <summary>
    /// Type of command line argument.
    /// </summary>
    public enum CommandLineArgType
    {
        Unknown,
        Help,
        Version,
        CreateConfig,
        ExecuteBackup
    }

    /// <summary>
    /// Extension methods for CommandLineArgType enum
    /// </summary>
    public static class CommandLineArgTypeEnumExt
    {
        /// <summary>
        /// Determines whether the command requires a filename arg.
        /// </summary>
        public static bool RequiresFilename(this CommandLineArgType type)
        {
            return type == CommandLineArgType.CreateConfig || type == CommandLineArgType.ExecuteBackup;
        }
    }
}
