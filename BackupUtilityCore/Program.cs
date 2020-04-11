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
        public static int Main(string[] args)
        {
            // Default to OK
            int returnCode = 0;

            try
            {
                // On UNIX, 1st arg will be app path/name
                int initialIndex = Environment.OSVersion.Platform == PlatformID.Unix ? 1 : 0;

                // Get args of interest
                string commandArg = args.ElementAtOrDefault(initialIndex);
                string fileArg = args.ElementAtOrDefault(initialIndex + 1);

                // If no arguments specified, display help 
                if (string.IsNullOrEmpty(commandArg) || CommandLineArgs.IsHelpArg(commandArg))
                {
                    DisplayHelp();
                }
                else if (CommandLineArgs.IsVersionArg(commandArg))
                {
                    AddToLog(AppVersion);
                }
                else if (CommandLineArgs.IsCreateConfigArg(commandArg))
                {
                    if (!string.IsNullOrEmpty(fileArg))
                    {
                        returnCode = CreateDefaultConfig(fileArg) ? 0 : 1;
                    }
                    else
                    {
                        DisplayHelp();
                        returnCode = 1;
                    }
                }
                else if (CommandLineArgs.IsExecuteArg(commandArg))
                {
                    if (!string.IsNullOrEmpty(fileArg))
                    {
                        returnCode = ExecuteBackupConfig(fileArg) ? 0 : 1;
                    }
                    else
                    {
                        DisplayHelp();
                        returnCode = 1;
                    }
                }
                else
                {
                    // Unknown command
                    DisplayHelp();
                    returnCode = 1;
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

        private static void DisplayHelp()
        {
            // Include version of app DLL in help
            string helpTitle = $"Help for {AppVersion}";

            AddToLog("".PadRight(helpTitle.Length, '-'));
            AddToLog(helpTitle);
            AddToLog("".PadRight(helpTitle.Length, '-'));

            // Display help
            foreach (string s in CommandLineArgs.GetHelpInfo())
            {
                AddToLog(s);
            }
        }

        private static string GetConfigPath(string configName)
        {
            // Add default yaml extension if none given
            if (string.IsNullOrEmpty(System.IO.Path.GetExtension(configName)))
            {
                // GetExtension also returns empty string if file ends in '.'
                configName = configName.TrimEnd('.') + ".yaml";
            }

            // Check whether full path or just file supplied.
            if (!System.IO.Path.IsPathRooted(configName))
            {
                // Add current directory to path.
                return System.IO.Path.Combine(Environment.CurrentDirectory, configName);
            }
            else
            {
                // Already contains path
                return configName;
            }
        }

        private static bool CreateDefaultConfig(string configName)
        {
            // Get full path for config
            string configPath = GetConfigPath(configName);

            // Check file exists.
            if (System.IO.File.Exists(configPath))
            {
                AddToLog($"Config file already exists: {configName}");
                return false;
            }
            else
            {
                const string EmbeddedConfig = "backup-config.yaml";

                // Create file using defaults.
                EmbeddedResource.CreateLocalCopy(EmbeddedConfig, configPath);

                // Report that file created.
                AddToLog($"Default config file created: {configName}");
                AddToLog("*** UPDATE CONFIGURATION BEFORE RUNNING APP ***");

                return true;
            }
        }

        private static BackupSettings ParseConfig(string settingsPath)
        {
            // Other file formats could be supported in future
            ISettingsParser backupSettings = new YamlSettingsParser();

            return backupSettings.Parse(settingsPath);
        }

        private static bool ExecuteBackupConfig(string configName)
        {
            // Get full path for config
            string configPath = GetConfigPath(configName);

            // Check file exists.
            if (!System.IO.File.Exists(configPath))
            {
                AddToLog($"Config file does not exist: {configName}");
                return false;
            }

            // Parse args for backup settings
            BackupSettings backupSettings = ParseConfig(configPath);

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
                }
                finally
                {
                    // Remove handler
                    backup.Log -= AddToLog;
                }

                // Return error if backup had issues
                return backup.ErrorCount == 0;
            }
            else
            {
                AddToLog("Config file is not valid, target or source settings are missing.");
                return false;
            }
        }
    }
}
