using BackupUtilityCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BackupUtilityTest
{
    /// <summary>
    /// Test cases for BackupUtilityCore.BackupErrorInfo
    /// </summary>
    [TestClass]
    public sealed class TestBackupErrorInfo
    {
        [DataRow(BackupResult.OK, "C:\\file1.txt", "C:\\backups")]
        [DataRow(BackupResult.AlreadyBackedUp, "/users/freedom35/file1.txt", "/users/freedom35/backups")]
        [DataRow(BackupResult.Exception, "", "")]
        [DataTestMethod]
        public void TestConstructor(BackupResult result, string sourceFile, string targetDir)
        {
            // Source/Target properties read-only (set in constructor)
            BackupErrorInfo info = new BackupErrorInfo(result, sourceFile, targetDir);

            Assert.AreEqual(result, info.Result);
            Assert.AreEqual(sourceFile, info.SourceFile);
            Assert.AreEqual(targetDir, info.TargetDir);
        }

        [TestMethod]
        public void TestResultProperty()
        {
            BackupErrorInfo info = new BackupErrorInfo(BackupResult.WriteInProgress, "C:\\file1.txt", "C:\\backups");

            Assert.AreEqual(BackupResult.WriteInProgress, info.Result);

            // Change property and re-test
            info.Result = BackupResult.OK;
            Assert.AreEqual(BackupResult.OK, info.Result);
        }
    }
}
