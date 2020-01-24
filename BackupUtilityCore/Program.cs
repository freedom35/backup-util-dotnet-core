using System;
using System.Linq;

namespace BackupUtilityCore
{
    sealed class Program
    {
        static int Main(string[] args)
        {
            // Default to OK
            int returnCode = 0;

            try
            {
                Console.WriteLine("Backup Utility v{0}\n", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);

                // Check if help args supplied
                if (args.Any(arg => arg == "-h" || arg.ToLower() == "--help"))
                {
                    DisplayHelp();
                }
                else if (TryGetSettingsFile(args, out string settingsFile))
                {
                    // Parse args for backup settings
                    BackupSettings backupSettings = ParseSettings(settingsFile);

                    // Create backup object
                    BackupTask backup = new BackupTask(backupSettings);

                    // Add handler for output
                    backup.Log += Console.WriteLine;

                    try
                    {
                        // Execute backup
                        int backupCount = backup.Execute();

                        // Report total
                        Console.WriteLine($"Total files backed up: {backupCount}");
                    }
                    finally
                    {
                        // Remove handler
                        backup.Log -= Console.WriteLine;
                    }
                }
            }
            catch (Exception ex)
            {
                // Report error
                Console.WriteLine("\nError:\n{0}", ex.Message);

                // Check if more details available
                if (ex.StackTrace != null)
                {
                    Console.WriteLine("\nStack Trace:\n{0}", ex.StackTrace);
                }

                // Return error
                returnCode = -1;
            }

            return returnCode;
        }

        static void DisplayHelp()
        {
            Console.WriteLine("Help:");
            Console.WriteLine("  [<file name>], Name of non-default config file.");
            Console.WriteLine("  [-c], Creates config file if non-existent.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  backup");
            Console.WriteLine("  backup -c");
            Console.WriteLine("  backup config1.xml");
            Console.WriteLine("  backup config1.xml -c");
            Console.WriteLine("  backup C:\\Configs\\config1.xml");
        }

        static bool TryGetSettingsFile(string[] args, out string settingsFile)
        {
            // Get settings file name.
            settingsFile = args.ElementAtOrDefault(0) ?? "backup-config.xml";

            // Check whether full path or just file supplied.
            if (!System.IO.Path.IsPathRooted(settingsFile))
            {
                // Add current directory to path.
                settingsFile = System.IO.Path.Combine(Environment.CurrentDirectory, settingsFile);
            }

            // Check file exists.
            if (System.IO.File.Exists(settingsFile))
            {
                // File should be used.
                return true;
            }

            // Check for flag to create missing file.
            if (args.Contains("-c"))
            {
                // Create file using defaults.
                new BackupSettings().SaveToFile(settingsFile);

                // Report that file created.
                Console.WriteLine($"Config file created: {settingsFile}");
            }
            else
            {
                Console.WriteLine($"Config file does not exist: {settingsFile}");
            }

            // Return false if not specified or newly created.
            // (May be dangerous to use default settings)
            return false;
        }

        static BackupSettings ParseSettings(string settingsFile)
        {
            BackupSettings backupSettings = new BackupSettings();

            // Attempt to load file
            backupSettings.LoadFromFile(settingsFile);

            return backupSettings;
        }
    }
}
