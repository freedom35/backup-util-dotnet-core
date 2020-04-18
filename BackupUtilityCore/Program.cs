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

        private static void AddToLog(object _, MessageEventArgs e)
        {
            AddToLog(e.ToString(Console.BufferWidth));
        }

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
                const string EmbeddedConfigName = "backup-config.yaml";

                // Create file using defaults.
                EmbeddedResource.CreateCopyFromName(EmbeddedConfigName, configPath);

                // Report that file created.
                AddToLog($"Default config file created: {configName}");
                AddToLog("*** UPDATE CONFIGURATION BEFORE RUNNING APP ***");

                return true;
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
            BackupSettings backupSettings = BackupSettings.ParseFromYaml(configPath);

            // Check config parsed ok
            if (backupSettings.Valid)
            {
                // Create backup object
                BackupTaskBase backupTask = CreateBackupTask(backupSettings.BackupType);

                // Add handler for output
                backupTask.Log += AddToLog;

                try
                {
                    // Execute backup
                    int backupCount = backupTask.Run(backupSettings);

                    // Report total
                    AddToLog($"Total files backed up: {backupCount}");
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

        private static BackupTaskBase CreateBackupTask(BackupType backupType)
        {
            return backupType switch
            {
                BackupType.Copy => new BackupTaskCopy(),
                BackupType.Sync => new BackupTaskSync(),
                BackupType.Isolated => new BackupTaskIsolatedCopy(),
                _ => throw new NotImplementedException($"Backup task not implemented for '{backupType}'."),
            };
        }
    }
}
