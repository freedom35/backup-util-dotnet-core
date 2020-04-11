using BackupUtilityCore.YAML;
using System;
using System.Linq;

namespace BackupUtilityCore
{
    sealed class Program
    {
        /// <summary>
        /// Version info for app
        /// </summary>
        private static string AppVersion => $"Backup Utility v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}";

        /// <summary>
        /// Entry point for program.
        /// </summary>
        static int Main(string[] args)
        {
            // Default to OK
            int returnCode = 0;

            try
            {
                // If no arguments specified, display help 
                // (1st arg will be path/app name)
                if (args.Length <= 1 || args.Any(arg => HelpInfo.IsHelpArg(arg)))
                {
                    // Include version of app DLL in help
                    string helpTitle = $"Help for {AppVersion}";

                    AddToLog("".PadRight(helpTitle.Length, '-'));
                    AddToLog(helpTitle);
                    AddToLog("".PadRight(helpTitle.Length, '-'));

                    // Display help
                    foreach (string s in HelpInfo.GetAppUsage())
                    {
                        AddToLog(s);
                    }
                }
                else if (args.Any(arg => arg.ToLower() == "--version"))
                {
                    AddToLog(AppVersion);
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
                        backup.Log += AddToLog;

                        try
                        {
                            // Execute backup
                            int backupCount = backup.Execute();

                            // Report total
                            AddToLog($"Total files backed up: {backupCount}");

                            // Return error if backup had issues
                            returnCode = backup.ErrorCount > 0 ? 1 : 0;
                        }
                        finally
                        {
                            // Remove handler
                            backup.Log -= AddToLog;
                        }
                    }
                    else
                    {
                        AddToLog("Config file is not valid, target or source settings are missing.");
                    }
                }
            }
            catch (Exception ex)
            {
                // Report error
                AddToLog($"\nError:\n{ex.Message}");

                // Check if more details available
                if (ex.StackTrace != null)
                {
                    AddToLog($"\nStack Trace:\n{ex.StackTrace}");
                }

                returnCode = 1;
            }

            return returnCode;
        }

        private static void AddToLog(object _, MessageEventArgs e)
        {
            AddToLog(e.Message);
        }

        private static void AddToLog(string message)
        {
            Console.WriteLine(message);
        }

        private static bool TryGetSettingsPath(string[] args, out string settingsPath)
        {
            const string DefaultFile = "backup-config.yaml";

            // Get settings file name.
            string settingsFileArg = args.ElementAtOrDefault(0) ?? DefaultFile;

            // Add default yaml extension if none given
            if (string.IsNullOrEmpty(System.IO.Path.GetExtension(settingsFileArg)))
            {
                // GetExtension also returns empty string if file ends in '.'
                settingsFileArg = settingsFileArg.TrimEnd('.') + ".yaml";
            }

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
                AddToLog($"Default config file created: {DefaultFile}");
                AddToLog("*** UPDATE CONFIGURATION BEFORE RUNNING APP ***");
            }
            else
            {
                AddToLog($"Config file does not exist: {settingsFileArg}");
            }

            // Return false if not specified or newly created.
            // (May be dangerous to use default settings)
            return false;
        }

        private static BackupSettings ParseSettings(string settingsPath)
        {
            // Other file formats could be supported in future
            ISettingsParser backupSettings = new YamlSettingsParser();

            return backupSettings.Parse(settingsPath);
        }
    }
}
