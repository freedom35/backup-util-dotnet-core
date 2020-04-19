using BackupUtilityCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BackupUtilityTest
{
    /// <summary>
    /// Test cases for BackupUtilityCore.BackupResultEnum
    /// </summary>
    [TestClass]
    public sealed class TestBackupResultEnum
    {
        [DataRow(BackupResult.OK, false)]
        [DataRow(BackupResult.Ineligible, false)]
        [DataRow(BackupResult.AlreadyBackedUp, false)]
        [DataRow(BackupResult.PathTooLong, false)]
        [DataRow(BackupResult.Exception, true)]
        [DataRow(BackupResult.WriteInProgress, true)]
        [DataTestMethod]
        public void TestCanBeRetried(BackupResult result, bool canBeRetried)
        {
            Assert.AreEqual(canBeRetried, result.CanBeRetried());
        }

        [DataRow(BackupResult.OK, "OK")]
        [DataRow(BackupResult.PathTooLong, "Target path is too long")]
        [DataTestMethod]
        public void TestCanBeRetried(BackupResult result, string description)
        {
            Assert.AreEqual(description, result.GetDescription());
        }
    }
}
