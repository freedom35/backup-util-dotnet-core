using BackupUtilityCore.YAML;
using System;
using System.Linq;

namespace BackupUtilityCore
{
    sealed class Program
    {
        /// <summary>
        /// Entry point for program.
        /// </summary>
        static int Main(string[] args)
        {
            // Default to error
            int returnCode = 1;

            try
            {
                Console.WriteLine("Backup Utility v{0}\n", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);

                // Check if help args supplied
                if (args.Any(arg => arg == "-h" || arg.ToLower() == "--help"))
                {
                    HelpInfo.Display();
                }
                else if (TryGetSettingsPath(args, out string settingsPath))
                {
                    // Parse args for backup settings
                    BackupSettings backupSettings = ParseSettings(settingsPath);

                    // Check config parsed ok
                    if (backupSettings.Valid)
                    {
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

                            // Backup ran OK
                            returnCode = 0;
                        }
                        finally
                        {
                            // Remove handler
                            backup.Log -= Console.WriteLine;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Config file is not valid, target or source settings are missing.");
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
            }

            return returnCode;
        }

        static bool TryGetSettingsPath(string[] args, out string settingsPath)
        {
            const string DefaultFile = "backup-config.yaml";

            // Get settings file name.
            string settingsFileArg = args.ElementAtOrDefault(0) ?? DefaultFile;

            // Check whether full path or just file supplied.
            if (!System.IO.Path.IsPathRooted(settingsFileArg))
            {
                // Add current directory to path.
                settingsPath = System.IO.Path.Combine(Environment.CurrentDirectory, settingsFileArg);
            }
            else
            {
                // Already contains path
                settingsPath = settingsFileArg;
            }

            // Check file exists.
            if (System.IO.File.Exists(settingsPath))
            {
                // File should be used.
                return true;
            }

            // Check for flag to create missing file.
            if (args.Contains("-c"))
            {
                // Create file using defaults.
                EmbeddedResource.CreateLocalCopy(DefaultFile);

                // Report that file created.
                Console.WriteLine($"Default config file created: {DefaultFile}");
                Console.WriteLine("*** UPDATE CONFIGURATION BEFORE RUNNING APP ***");
            }
            else
            {
                Console.WriteLine($"Config file does not exist: {settingsFileArg}");
            }

            // Return false if not specified or newly created.
            // (May be dangerous to use default settings)
            return false;
        }

        static BackupSettings ParseSettings(string settingsPath)
        {
            // Other file formats could be supported in future
            ISettingsParser backupSettings = new YamlSettingsParser();

            return backupSettings.Parse(settingsPath);
        }
    }
}
