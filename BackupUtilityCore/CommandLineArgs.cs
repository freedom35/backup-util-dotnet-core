using System;
using System.Reflection;

namespace BackupUtilityCore
{
    public static class CommandLineArgs
    {
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
            string app = Assembly.GetExecutingAssembly().GetName().Name;

            return new string[] {
                "",
                "Arguments:",
                "  --help, -h, -?                 Displays help info for app.",
                "  --version, -v                  Displays version info for app.",
                "  --create, -c <filename>.yaml   Creates config file with default values.",
                "  --run, -r <filename>.yaml      Path/name of config file to execute.",
                "",
                "Usage:",
                $"  dotnet {app} --version",
                $"  dotnet {app} --create config1.yaml",
                $"  dotnet {app} -c config1.yaml",
                $"  dotnet {app} --run config1.yaml",
                $"  dotnet {app} -r config1.yaml",
                $"  dotnet {app} -r {GetExampleConfigPath()}",
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
