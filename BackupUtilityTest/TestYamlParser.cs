using Microsoft.VisualStudio.TestTools.UnitTesting;

using BackupUtilityCore.YAML;

namespace BackupUtilityTest
{
    /// <summary>
    /// Test cases for YamlParser.
    /// </summary>
    [TestClass]
    public class TestYamlParser
    {
        //[TestMethod]
        //public void TestMethod1()
        //{
        //}

        [DataRow("", "")]
        [DataRow("abc", "abc")]
        [DataRow("abc ", "abc")]
        [DataRow(" abc", "abc")]
        [DataRow("  abc  ", "abc")]
        [DataRow("abc\n", "abc")]
        [DataRow("abc\r", "abc")]
        [DataRow("abc \n\r", "abc")]
        [DataTestMethod]
        public void TestTrimWhiteSpaceChars(string input, string inputTrimmed)
        {
            Assert.AreEqual(inputTrimmed, YamlParser.TrimWhiteSpaceChars(input));
        }

        [DataRow(@"C:\dir1", @"C:\dir1")]
        [DataRow(@"- C:\dir1", @"C:\dir1")]
        [DataRow(@"- C:\dir1", @"C:\dir1")]
        [DataRow("- \"C:\\dir1\"", @"C:\dir1")]
        [DataRow("- \'C:\\dir1\'", @"C:\dir1")]
        [DataTestMethod]
        public void TestTrimSequenceChars(string input, string inputTrimmed)
        {
            Assert.AreEqual(inputTrimmed, YamlParser.TrimSequenceChars(input));
        }

        [DataRow("", true)]
        [DataRow("---", true)]
        [DataRow("...", true)]
        [DataRow("#", true)]
        [DataRow("abc", false)]
        [DataRow("- abc", false)]
        [DataTestMethod]
        public void TestIsIgnoreLine(string input, bool ignoreLine)
        {
            Assert.AreEqual(ignoreLine, YamlParser.IsIgnoreLine(input));
        }

        [DataRow("", false)]
        [DataRow("---", false)]
        [DataRow("abc", false)]
        [DataRow("- abc", true)]
        [DataRow("-abc", true)]
        [DataTestMethod]
        public void TestIsSequenceEntry(string input, bool sequenceEntry)
        {
            Assert.AreEqual(sequenceEntry, YamlParser.IsSequenceEntry(input));
        }
    }
}
