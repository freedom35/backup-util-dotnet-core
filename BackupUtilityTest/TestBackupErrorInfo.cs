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
            // Property read-only (set in constructor)
            BackupErrorInfo info = new BackupErrorInfo(result, sourceFile, targetDir);

            Assert.AreEqual(result, info.Result);
            Assert.AreEqual(sourceFile, info.SourceFile);
            Assert.AreEqual(targetDir, info.TargetDir);
        }
    }
}
