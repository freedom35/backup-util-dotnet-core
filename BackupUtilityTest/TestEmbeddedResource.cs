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
        private string targetPath;

        [TestInitialize]
        public void InitializeTest()
        {
            // Output path for testing
            targetPath = TestConfig.CreateNewOutputPath();

            // Delete file from any previous test
            File.Delete(targetPath);
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Remove file after test
            File.Delete(targetPath);
        }

        [TestMethod]
        public void TestCreateCopyFromPath()
        {
            // Check method finds resource and writes file
            Assert.IsTrue(EmbeddedResource.CreateCopyFromPath(TestConfig.ResourcePath, targetPath));

            // Verify file does exist
            Assert.IsTrue(File.Exists(targetPath));
        }
    }
}
