using BackupUtilityCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace BackupUtilityTest
{
    [TestClass]
    public sealed class TestEmbeddedResource
    {
        private string targetPath;

        [TestInitialize]
        public void InitializeTest()
        {
            string now = DateTime.Now.ToString("yyyyMMddHHmmssfff");

            // Output path for testing
            targetPath = Path.Combine(Environment.CurrentDirectory, $"test-embedded-{now}.yaml");

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
            // Internal resource path
            const string ResourcePath = "BackupUtilityTest.Resources.test-config.yaml";

            // Check method finds resource and writes file
            Assert.IsTrue(EmbeddedResource.CreateCopyFromPath(ResourcePath, targetPath));

            // Verify file does exist
            Assert.IsTrue(File.Exists(targetPath));
        }
    }
}
