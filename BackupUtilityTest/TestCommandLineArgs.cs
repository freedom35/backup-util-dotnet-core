using BackupUtilityCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BackupUtilityTest
{
    [TestClass]
    public sealed class TestCommandLineArgs
    {
        [DataRow("--help", true)]
        [DataRow("--Help", true)]
        [DataRow("--HELP", true)]
        [DataRow("-h", true)]
        [DataRow("-?", true)]
        [DataRow("", false)]
        [DataRow("-c", false)]
        [DataRow("help.yaml", false)]
        [DataRow("arg!", false)]
        [DataTestMethod]
        public void TestIsHelpArg(string arg, bool isCorrectArg)
        {
            Assert.AreEqual(isCorrectArg, CommandLineArgs.IsHelpArg(arg));
        }

        [DataRow("--version", true)]
        [DataRow("--VERSION", true)]
        [DataRow("-v", true)]
        [DataRow("", false)]
        [DataRow("-c", false)]
        [DataRow("--version.yaml", false)]
        [DataRow("arg!", false)]
        [DataTestMethod]
        public void TestIsVersionArg(string arg, bool isCorrectArg)
        {
            Assert.AreEqual(isCorrectArg, CommandLineArgs.IsVersionArg(arg));
        }

        [DataRow("--create", true)]
        [DataRow("--CReate", true)]
        [DataRow("-c", true)]
        [DataRow("", false)]
        [DataRow("-r", false)]
        [DataRow("--create.yaml", false)]
        [DataRow("-c.yaml", false)]
        [DataRow("arg!", false)]
        [DataTestMethod]
        public void TestCreateConfigArg(string arg, bool isCorrectArg)
        {
            Assert.AreEqual(isCorrectArg, CommandLineArgs.IsCreateConfigArg(arg));
        }

        [DataRow("-r", true)]
        [DataRow("", false)]
        [DataRow("-c", false)]
        [DataRow("execute.yaml", false)]
        [DataRow("-r.yaml", false)]
        [DataRow("arg!", false)]
        [DataTestMethod]
        public void TestExecuteArg(string arg, bool isCorrectArg)
        {
            Assert.AreEqual(isCorrectArg, CommandLineArgs.IsExecuteArg(arg));
        }

        [DataRow("-r", CommandLineArgType.ExecuteBackup)]
        [DataRow("--run", CommandLineArgType.ExecuteBackup)]
        [DataRow("", CommandLineArgType.Unknown)]
        [DataRow("-c", CommandLineArgType.CreateConfig)]
        [DataRow("--create", CommandLineArgType.CreateConfig)]
        [DataRow("-v", CommandLineArgType.Version)]
        [DataRow("--version", CommandLineArgType.Version)]
        [DataRow("-h", CommandLineArgType.Help)]
        [DataRow("-?", CommandLineArgType.Help)]
        [DataRow("--help", CommandLineArgType.Help)]
        [DataRow("execute.yaml", CommandLineArgType.Unknown)]
        [DataRow("-f", CommandLineArgType.Unknown)]
        [DataRow("arg!", CommandLineArgType.Unknown)]
        [DataTestMethod]
        public void TestGetArgType(string arg, CommandLineArgType correctType)
        {
            Assert.AreEqual(correctType, CommandLineArgs.GetArgType(arg));
        }

        [DataRow("-h", true, CommandLineArgType.Help, "")]
        [DataRow("-h -r", false, CommandLineArgType.Help, "")]
        [DataRow("-v", true, CommandLineArgType.Version, "")]
        [DataRow("-v -u", false, CommandLineArgType.Version, "")]
        [DataRow("-c", false, CommandLineArgType.CreateConfig, "")]
        [DataRow("-c config1.yaml", true, CommandLineArgType.CreateConfig, "config1.yaml")]
        [DataRow("-r", false, CommandLineArgType.ExecuteBackup, "")]
        [DataRow("-r config2.yaml", true, CommandLineArgType.ExecuteBackup, "config2.yaml")]
        [DataRow("-r config2.yaml -r", false, CommandLineArgType.ExecuteBackup, "config2.yaml")]
        [DataRow("-r config1.yaml config2.yaml", false, CommandLineArgType.ExecuteBackup, "config1.yaml")]
        [DataRow("config1.yaml -c", false, CommandLineArgType.Unknown, "")]
        [DataRow("config2.yaml -r", false, CommandLineArgType.Unknown, "")]
        [DataRow("-u", false, CommandLineArgType.Unknown, "")]
        [DataRow("just a bunch of junk", false, CommandLineArgType.Unknown, "a")]
        [DataRow("", false, CommandLineArgType.Unknown, "")]
        [DataTestMethod]
        public void TestTryParseArgs(string argsAsString, bool parseValid, CommandLineArgType correctType, string correctFileArg)
        {
            string[] args = argsAsString.Split(' ');

            bool parsed = CommandLineArgs.TryParseArgs(args, out CommandLineArgType type, out string fileArg);

            Assert.AreEqual(parseValid, parsed);
            Assert.AreEqual(correctType, type);
            Assert.AreEqual(correctFileArg, fileArg);
        }
    }
}
