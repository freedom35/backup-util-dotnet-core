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
    }
}
