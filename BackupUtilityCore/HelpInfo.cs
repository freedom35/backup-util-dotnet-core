using System;

namespace BackupUtilityCore
{
    internal static class HelpInfo
    {
        /// <summary>
        /// Displays app help info in console/terminal.
        /// </summary>
        public static string[] GetAppUsage()
        {
            return new string[] {
                "Help:",
                "  [<file name>], Name of non-default config file to run.",
                "  [-c], Creates default config file if non-existent.",
                "",
                "Usage:",
                "  backup",
                "  backup -c",
                "  backup config1.yaml",
                "  backup " + GetConfigPath()
            };
        }

        private static string GetConfigPath()
        {
            // Tailor help based on platform
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                return "/user/default/Configs/config1.yaml";
            }
            else
            {
                return "C:\\Configs\\config1.yaml";
            }
        }
    }
}
