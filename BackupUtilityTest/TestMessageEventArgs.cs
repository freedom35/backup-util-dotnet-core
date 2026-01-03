using BackupUtilityCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BackupUtilityTest
{
    /// <summary>
    /// Test cases for BackupUtilityCore.MessageEventArgs
    /// </summary>
    [TestClass]
    public sealed class TestMessageEventArgs
    {
        [DataRow("message0", "")]
        [DataRow("message1", "argA")]
        [DataRow("message2", "argB")]
        [TestMethod]
        public void TestConstructor(string message, string arg)
        {
            // Properties readonly - assigned in constructor
            MessageEventArgs e = new(message, arg);

            // Check properties match
            Assert.AreEqual(message, e.Message);
            Assert.AreEqual(arg, e.Arg);
        }

        [DataRow("message0", "", "message0")]
        [DataRow("min", "padding", "min      - padding")]
        [DataRow("message", "arg", "message  - arg")]
        [DataRow("message1", "argA", "message1 - argA")]
        [DataRow("message2", "argB", "message2 - argB")]
        [DataRow("message3", "argSomethingLonger", "message3 - argSomethingLonger")]
        [TestMethod]
        public void TestToString(string message, string arg, string expectedString)
        {
            MessageEventArgs e = new(message, arg);

            // Check string not truncated by default
            Assert.AreEqual(expectedString, e.ToString());
        }

        [DataRow("message0", "", 5, "message0")]
        [DataRow("message12", "12", 30, "message12 - 12")]
        [DataRow("message", "1234567890", 20, "message  - ~34567890")]
        [DataRow("message1", "1234567890", 20, "message1 - ~34567890")]
        [DataRow("message10", "1234567890", 20, "message10 - ~4567890")]
        [DataRow("message", "arg", 0, "message  - arg")]
        [DataRow("message", "arg", 1, "message  - arg")]
        [DataRow("message", "arg", -1, "message  - arg")]
        [DataRow("message", "arg", int.MaxValue - 1, "message  - arg")]
        [DataRow("message", "arg", int.MaxValue, "message  - arg")]
        [TestMethod]
        public void TestToStringWithMaxLength(string message, string arg, int maxLength, string expectedString)
        {
            MessageEventArgs e = new(message, arg);

            // Check truncated from start when length exceeded
            Assert.AreEqual(expectedString, e.ToString(maxLength));
        }
    }
}
