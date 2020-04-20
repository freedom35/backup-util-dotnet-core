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
        [TestMethod]
        public void TestConstructor()
        {
            const string Message = "MSG-TEST";
            const string Arg = "ARG-TEST";

            // Properties readonly - assigned in constructor
            MessageEventArgs e = new MessageEventArgs(Message, Arg);

            // Check properties match
            Assert.AreEqual(Message, e.Message);
            Assert.AreEqual(Arg, e.Arg);
        }

        [TestMethod]
        public void TestToString()
        {
            // Property readonly - assigned in constructor
            MessageEventArgs e;

            // Check not truncated for base to string
            e = new MessageEventArgs("Message", "ARG");
            Assert.AreEqual("Message:    ARG", e.ToString());

            // Check not truncated
            e = new MessageEventArgs("Message", "###############");
            Assert.AreEqual("Message:    ###############", e.ToString());
        }

        [TestMethod]
        public void TestToStringWithMaxLength()
        {
            // Property readonly - assigned in constructor
            MessageEventArgs e;

            // Check not truncated when no arg
            e = new MessageEventArgs("Message", "");
            Assert.AreEqual("Message", e.ToString(5));

            // Check truncated from start
            e = new MessageEventArgs("Message", "1234567890");
            Assert.AreEqual("Message:    ~4567890", e.ToString(20));

            // Check not truncated When below max
            e = new MessageEventArgs("Message", "12");
            Assert.AreEqual("Message:    12", e.ToString(30));
        }
    }
}
