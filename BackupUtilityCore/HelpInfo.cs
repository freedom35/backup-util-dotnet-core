using System;

namespace BackupUtilityCore
{
    internal static class HelpInfo
    {
        /// <summary>
        /// Displays app help info in console/terminal.
        /// </summary>
        public static void Display()
        {
            Console.WriteLine("Help:");
            Console.WriteLine("  [<file name>], Name of non-default config file.");
            Console.WriteLine("  [-c], Creates default config file if non-existent.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  backup");
            Console.WriteLine("  backup -c");
            Console.WriteLine("  backup config1.yaml");
            Console.WriteLine("  backup config1.yaml -c");

            // Tailor help based on platform
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                Console.WriteLine("  backup /user/default/Configs/config1.yaml");
            }
            else
            {
                Console.WriteLine("  backup C:\\Configs\\config1.yaml");
            }
        }
    }
}
