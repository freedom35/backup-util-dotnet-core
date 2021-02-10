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
        [DataRow(BackupResult.Exception, false)]
        [DataRow(BackupResult.UnauthorizedAccess, false)]
        [DataRow(BackupResult.UnableToAccess, true)]
        [DataRow(BackupResult.WriteInProgress, true)]
        [DataTestMethod]
        public void TestCanBeRetried(BackupResult result, bool canBeRetried)
        {
            Assert.AreEqual(canBeRetried, result.CanBeRetried());
        }

        [DataRow(BackupResult.OK, false)]
        [DataRow(BackupResult.Ineligible, false)]
        [DataRow(BackupResult.AlreadyBackedUp, false)]
        [DataRow(BackupResult.PathTooLong, true)]
        [DataRow(BackupResult.Exception, true)]
        [DataRow(BackupResult.UnauthorizedAccess, true)]
        [DataRow(BackupResult.UnableToAccess, true)]
        [DataRow(BackupResult.WriteInProgress, true)]
        [DataTestMethod]
        public void TestIsError(BackupResult result, bool canBeRetried)
        {
            Assert.AreEqual(canBeRetried, result.IsError());
        }

        [DataRow(BackupResult.OK, "OK")]
        [DataRow(BackupResult.PathTooLong, "Target path is too long")]
        [DataTestMethod]
        public void TestGetDescription(BackupResult result, string description)
        {
            Assert.AreEqual(description, result.GetDescription());
        }
    }
}
