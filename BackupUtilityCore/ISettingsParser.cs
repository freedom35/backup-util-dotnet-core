namespace BackupUtilityCore
{
    interface ISettingsParser
    {
        BackupSettings Parse(string fileName);
    }
}
