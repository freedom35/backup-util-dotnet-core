
namespace BackupUtilityCore
{
    /// <summary>
    /// Enum definition of backup types.
    /// </summary>
    public enum BackupType
    {
        /// <summary>
        /// Copies the contents of the source dir to the target.
        /// (Any files later deleted from the source, will remain in the target.)
        /// </summary>
        Copy = 1,

        /// <summary>
        /// Keeps the target dir in-sync with the source dir.
        /// (Files deleted from the source will also be deleted from the target.)
        /// </summary>
        Sync = 2,

        /// <summary>
        /// Individual backups, separate copies.
        /// (New backup directory created for each copy.)
        /// </summary>
        Isolated = 3
    }
}
