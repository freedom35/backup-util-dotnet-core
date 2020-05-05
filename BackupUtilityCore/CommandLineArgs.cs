using System;
using System.Linq;
using System.Reflection;

namespace BackupUtilityCore
{
    /// <summary>
    /// Class for handling command line args relating to the app.
    /// </summary>
    public static class CommandLineArgs
    {
        /// <summary>
        /// Attempts to parses command line args and returns command type.
        /// </summary>
        /// <param name="args">Args to parse</param>
        /// <param name="commandType">Parsed command type</param>
        /// <param name="fileArg">Parsed filename (optional arg)</param>
        /// <returns>true if parse is valid</returns>
        public static bool TryParseArgs(string[] args, out CommandLineArgType commandType, out string fileArg)
        {
            // First arg is command
            commandType = GetArgType(args.ElementAtOrDefault(0) ?? "");

            // Optional filename arg
            string filename = args.ElementAtOrDefault(1);

            // Check for value, and not a rogue command
            if (!string.IsNullOrEmpty(filename) && !filename.StartsWith('-'))
            {
                fileArg = filename;
            }
            else
            {
                // Method responsible for initializing to something
                fileArg = "";
            }

            // Validate parse
            switch (commandType)
            {
                case CommandLineArgType.CreateConfig:
                case CommandLineArgType.ExecuteBackup:
                    return args.Length == 2 && !string.IsNullOrEmpty(fileArg);

                case CommandLineArgType.Help:
                case CommandLineArgType.Version:
                    return args.Length == 1;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines type of command line arg.
        /// </summary>
        public static CommandLineArgType GetArgType(string arg)
        {
            CommandLineArgType type;

            if (IsHelpArg(arg))
            {
                type = CommandLineArgType.Help;
            }
            else if (IsVersionArg(arg))
            {
                type = CommandLineArgType.Version;
            }
            else if (IsCreateConfigArg(arg))
            {
                type = CommandLineArgType.CreateConfig;
            }
            else if (IsExecuteArg(arg))
            {
                type = CommandLineArgType.ExecuteBackup;
            }
            else
            {
                type = CommandLineArgType.Unknown;
            }

            return type;
        }

        /// <summary>
        /// Determines whether argument is requesting help.
        /// </summary>
        public static bool IsHelpArg(string arg)
        {
            return arg.ToLower() == "--help" || arg == "-h" || arg == "-?";
        }

        /// <summary>
        /// Determines whether argument is requesting the version.
        /// </summary>
        public static bool IsVersionArg(string arg)
        {
            return arg.ToLower() == "--version" || arg == "-v";
        }

        /// <summary>
        /// Determines whether argument is requesting to create a config.
        /// </summary>
        public static bool IsCreateConfigArg(string arg)
        {
            return arg.ToLower() == "--create" || arg == "-c";
        }

        /// <summary>
        /// Determines whether argument is requesting to create a config.
        /// </summary>
        public static bool IsExecuteArg(string arg)
        {
            return arg.ToLower() == "--run" || arg == "-r";
        }

        /// <summary>
        /// Displays app help info in console/terminal.
        /// </summary>
        public static string[] GetHelpInfo()
        {
            // Get exe name of app (project output on build)
            string name = Assembly.GetExecutingAssembly().GetName().Name;

            // Platform agnostic call
            string app = $"dotnet {name}.dll";

            return new string[] {
                "",
                "Usage:",
                $"  {app} [option] [<filename>]",
                "",
                "Options:",
                "  --help, -h, -?   Displays help info for app.",
                "  --version, -v    Displays version info for app.",
                "  --create, -c     Creates config file with default values.",
                "  --run, -r        Path/name of config file to execute.",
                "",
                "Filename:",
                "  Name of config file, required for create/run options.",
                "  Config files must be in YAML format.",
                "",
                "Examples:",
                $"  {app} --help",
                $"  {app} --version",
                $"  {app} --create config1.yaml",
                $"  {app} --run config1.yaml",
                $"  {app} -r config1.yaml",
                $"  {app} -r {GetExampleConfigPath()}"
            };
        }

        private static string GetExampleConfigPath()
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
