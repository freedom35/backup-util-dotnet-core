using BackupUtilityCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BackupUtilityTest
{
    [TestClass]
    public sealed class TestMessageEventArgs
    {
        //[TestMethod]
        //public void TestMessageProp()
        //{
        //    const string Message = "TEST";

        //    // Property readonly - assigned in constructor
        //    MessageEventArgs e = new MessageEventArgs(Message);

        //    // Check property matches
        //    Assert.AreEqual(Message, e.Message);
        //}

        [TestMethod]
        public void TestConstructor()
        {
            const string Category = "CAT-TEST";
            const string Message = "MSG-TEST";

            // Properties readonly - assigned in constructor
            MessageEventArgs e = new MessageEventArgs(Category, Message);

            // Check properties match
            Assert.AreEqual(Category, e.Category);
            Assert.AreEqual(Message, e.Message);
        }
    }
}
