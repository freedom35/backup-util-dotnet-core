using BackupUtilityCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BackupUtilityTest
{
    [TestClass]
    public sealed class TestHelpInfo
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
        public void TestIsHelpArg(string arg, bool isHelpArg)
        {
            Assert.AreEqual(isHelpArg, HelpInfo.IsHelpArg(arg));
        }
    }
}
