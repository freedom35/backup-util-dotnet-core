using BackupUtilityCore.YAML;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BackupUtilityTest
{
    /// <summary>
    /// Test cases for YamlParser.
    /// </summary>
    [TestClass]
    public sealed class TestYamlParser
    {
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


        [DataRow("", false, "", "")]
        [DataRow("key", false, "", "")]
        [DataRow("key val", false, "", "")]
        [DataRow("key:", true, "key", "")]
        [DataRow("key: val", true, "key", "val")]
        [DataRow("key:  val", true, "key", "val")]
        [DataRow("KEY: VAL", true, "key", "VAL")]
        [DataRow("key: C:\\dir1", true, "key", "C:\\dir1")]
        [DataTestMethod]
        public void TestTryGetKeyValue(string input, bool containsKey, string expectedKey, string expectedVal)
        {
            bool keyFound = YamlParser.TryGetKeyValue(input, out string key, out string val);

            Assert.AreEqual(containsKey, keyFound);
            Assert.AreEqual(expectedKey, key);
            Assert.AreEqual(expectedVal, val);
        }
    }
}
