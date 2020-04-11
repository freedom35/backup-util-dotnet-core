using System;

namespace BackupUtilityCore
{
    public static class HelpInfo
    {
        /// <summary>
        /// Determines whether argument is requesting help.
        /// </summary>
        public static bool IsHelpArg(string arg)
        {
            return arg.ToLower() == "--help" || arg == "-h" || arg == "-?";
        }

        /// <summary>
        /// Displays app help info in console/terminal.
        /// </summary>
        public static string[] GetAppUsage()
        {
            return new string[] {
                "",
                "Config files must be in YAML format.",
                "",
                "Arguments:",
                "  --help, -h, -?        Displays help info for app.",
                "  --version             Displays version info for app.",
                "  -c <filename>.yaml    Creates config file (if non-existent) with default values.",
                "  <filename>.yaml       Path/name of config file to execute.",
                "",
                "Usage:",
                "  dotnet backuputil --version",
                "  dotnet backuputil -c config1.yaml",
                "  dotnet backuputil config1.yaml",
                "  dotnet backuputil " + GetConfigPath()
            };
        }

        private static string GetConfigPath()
        {
            // Tailor help based on platform
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                return @"/Users/freedom35/Configs/config1.yaml";
            }
            else
            {
                return @"C:\Configs\config1.yaml";
            }
        }
    }
}
