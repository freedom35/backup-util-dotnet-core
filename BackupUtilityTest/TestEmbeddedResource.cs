using BackupUtilityCore;
using BackupUtilityTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace BackupUtilityTest
{
    /// <summary>
    /// Test cases for BackupUtilityCore.EmbeddedResource
    /// </summary>
    [TestClass]
    public sealed class TestEmbeddedResource
    {
        private static string testRoot;

        [ClassInitialize()]
        public static void InitializeTest(TestContext testContext)
        {
            testRoot = Path.Combine(testContext.TestRunDirectory, "TestEmbeddedResource");

            Directory.CreateDirectory(testRoot);
        }

        [TestMethod]
        public void TestCreateCopyFromPath()
        {
            // Output path for testing
            string targetPath = TestConfig.CreateNewOutputPath(testRoot);

            // Check method finds resource and writes file
            Assert.IsTrue(EmbeddedResource.CreateCopyFromPath(TestConfig.ResourcePath, targetPath));

            // Verify file does exist
            Assert.IsTrue(File.Exists(targetPath));
        }
    }
}
