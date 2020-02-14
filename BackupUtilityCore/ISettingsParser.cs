namespace BackupUtilityCore
{
    interface ISettingsParser
    {
        BackupSettings Parse(string settingsPath);
    }
}
