using BackupUtilityCore.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BackupUtilityCore
{
    /// <summary>
    /// Backup Utility App
    /// Created by Alan Barr (GitHub: freedom35)
    /// </summary>
    sealed class Program
    {
        #region Return Codes

        private const int ReturnOK = 0;
        private const int ReturnError = 1;

        #endregion

        /// <summary>
        /// Entry point for program.
        /// </summary>
        public static int Main(string[] args)
        {
            int returnCode;

            try
            {
                // Parse/Verify args
                if (!CommandLineArgs.TryParseArgs(args, out CommandLineArgType type, out string fileArg))
                {
                    type = CommandLineArgType.Unknown;
                }

                // Execute command
                switch (type)
                {
                    case CommandLineArgType.Help:
                        DisplayHelp();
                        returnCode = ReturnOK;
                        break;

                    case CommandLineArgType.Version:
                        AddToLog(Assembly.GetExecutingAssembly().GetName().Version.ToString());
                        returnCode = ReturnOK;
                        break;

                    case CommandLineArgType.CreateConfig:
                        returnCode = CreateDefaultConfig(fileArg) ? ReturnOK : ReturnError;
                        break;

                    case CommandLineArgType.ExecuteBackup:
                        returnCode = ExecuteBackupConfig(fileArg) ? ReturnOK : ReturnError;
                        break;

                    default:
                        // Parse failed
                        DisplayHelp();
                        returnCode = ReturnError;

                        // Unknown command
                        if (args.Length > 0)
                        {
                            // Limit to first few args (prevent abuse)
                            AddToLog($"{Environment.NewLine}Illegal option: {string.Join(' ', args.Take(5))}");
                        }

                        break;
                }
            }
            catch (Exception ex)
            {
                // Report error
                AddToLog($"{Environment.NewLine}Error:{Environment.NewLine}{ex.Message}");

                // Check if more details available
                if (ex.StackTrace != null)
                {
                    AddToLog($"{Environment.NewLine}Stack Trace:{Environment.NewLine}{ex.StackTrace}");
                }

                returnCode = ReturnError;
            }

            return returnCode;
        }

        /// <summary>
        /// Event handler for backup task logging.
        /// </summary>
        private static void AddToLog(object _, MessageEventArgs e)
        {
            // Make 1 less that buffer width to ensure it fits 
            // (may not be quite enough room for the entire last char)
            AddToLog(e.ToString(Console.BufferWidth - 1));
        }

        /// <summary>
        /// Log output is console window.
        /// </summary>
        private static void AddToLog(string message)
        {
            Console.WriteLine(message);
        }

        private static void DisplayHelp()
        {
            // Include version of app DLL in help
            string helpTitle = $"Help for Backup Utility v{Assembly.GetExecutingAssembly().GetName().Version}";

            // Include copyright info, convert '©' to plain ASCII for console output.
            string copyright = Assembly.GetExecutingAssembly().GetCustomAttributes(false).OfType<AssemblyCopyrightAttribute>().First().Copyright.Replace("©", "(c)");

            // Get max length for border
            int borderLen = Math.Max(helpTitle.Length, copyright.Length);

            // Add header
            AddToLog("".PadRight(borderLen, '-'));
            AddToLog(CenterText(helpTitle, borderLen));
            AddToLog(CenterText(copyright, borderLen));
            AddToLog("".PadRight(borderLen, '-'));

            // Display help
            foreach (string s in CommandLineArgs.GetHelpInfo())
            {
                AddToLog(s);
            }
        }

        /// <summary>
        /// Centers text by padding left.
        /// </summary>
        private static string CenterText(string text, int borderLen)
        {
            if (borderLen > text.Length)
            {
                return text.PadLeft(((borderLen - text.Length) / 2) + text.Length);
            }
            else
            {
                return text;
            }
        }

        /// <summary>
        /// Gets full path for the config name, appends yaml extension if missing.
        /// </summary>
        private static string GetConfigPath(string configName)
        {
            // Add default yaml extension if none given
            if (string.IsNullOrEmpty(System.IO.Path.GetExtension(configName)))
            {
                // GetExtension also returns empty string if file ends in '.'
                configName = configName.TrimEnd('.') + ".yaml";
            }

            // Check whether full path or just file supplied.
            if (System.IO.Path.IsPathRooted(configName))
            {
                // Already contains path
                return configName;
            }
            else
            {
                // Add current directory to path.
                return System.IO.Path.Combine(Environment.CurrentDirectory, configName);
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
            else if (EmbeddedResource.CreateDefaultConfig(configPath))
            {
                // Report that file created.
                AddToLog($"Default config file created: {configName}");
                AddToLog("*** UPDATE CONFIGURATION BEFORE RUNNING APP ***");

                return true;
            }
            else
            {
                AddToLog($"FAILED to create config: {configName}");
                return false;
            }
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
            if (BackupSettings.TryParseFromYaml(configPath, out BackupSettings backupSettings))
            {
                // Create backup object
                BackupTaskBase backupTask = CreateBackupTask(backupSettings.BackupType);

                // Add handler for output
                backupTask.Log += AddToLog;

                try
                {
                    // Execute backup
                    backupTask.Run(backupSettings);
                }
                finally
                {
                    // Remove handler
                    backupTask.Log -= AddToLog;
                }

                // Return error if backup had issues
                return backupTask.ErrorCount == 0;
            }
            else
            {
                AddToLog($"Config file {backupSettings.SettingsFilename} is not valid.");

                // Add some additional info to log...
                foreach (KeyValuePair<string, string> invalidSetting in backupSettings.GetInvalidSettings())
                {
                    AddToLog($"{invalidSetting.Key}: {invalidSetting.Value}");
                }

                return false;
            }
        }

        /// <summary>
        /// Returns appropriate sub-class based on backup type.
        /// </summary>
        private static BackupTaskBase CreateBackupTask(BackupType backupType)
        {
            return backupType switch
            {
                BackupType.Copy => new BackupTaskCopy(),
                BackupType.Sync => new BackupTaskSync(),
                BackupType.Isolated => new BackupTaskIsolatedCopy(),
                _ => throw new NotImplementedException($"Backup task not implemented for '{backupType}'.")
            };
        }
    }
}
