﻿using System;
using System.Reflection;

namespace BackupUtilityCore
{
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
            // Method responsible for initializing out params
            commandType = CommandLineArgType.Unknown;
            fileArg = "";

            // Support any order of args
            for (int i = 0; i < args.Length; i++)
            {
                CommandLineArgType tempType = GetArgType(args[i]);

                if (tempType == CommandLineArgType.Unknown)
                {
                    // Check if rogue command or filename
                    if (args[i].StartsWith('-'))
                    {
                        // Abort
                        commandType = CommandLineArgType.Unknown;
                        break;
                    }
                    else if (string.IsNullOrEmpty(fileArg))
                    {
                        fileArg = args[i];
                    }
                    else
                    {
                        // Already assigned filename (too many params)
                        break;
                    }
                }
                else if (commandType == CommandLineArgType.Unknown)
                {
                    commandType = tempType;
                }
                else
                {
                    // Multiple commands - abort
                    break;
                }
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
                "Arguments:",
                "  --help, -h, -?                 Displays help info for app.",
                "  --version, -v                  Displays version info for app.",
                "  --create, -c <filename>.yaml   Creates config file with default values.",
                "  --run, -r <filename>.yaml      Path/name of config file to execute.",
                "",
                "Usage:",
                $"  {app} --version",
                $"  {app} --create config1.yaml",
                $"  {app} -c config1.yaml",
                $"  {app} --run config1.yaml",
                $"  {app} -r config1.yaml",
                $"  {app} -r {GetExampleConfigPath()}",
                "",
                "Note: Config files must be in YAML format.",
                "",
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
