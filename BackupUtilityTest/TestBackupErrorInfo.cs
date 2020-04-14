using BackupUtilityCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BackupUtilityTest
{
    [TestClass]
    public sealed class TestBackupErrorInfo
    {
        [TestMethod]
        public void TestConstructor()
        {
            BackupResult result;
            BackupErrorInfo info;

            // Property read-only (set in constructor)
            result = BackupResult.AlreadyBackedUp;
            info = new BackupErrorInfo(result);
            Assert.AreEqual(result, info.Result);

            result = BackupResult.OK;
            info = new BackupErrorInfo(result);
            Assert.AreEqual(result, info.Result);
        }

        [TestMethod]
        public void TestFilenameProp()
        {
            string name = "test1.txt";

            BackupErrorInfo info = new BackupErrorInfo(BackupResult.OK)
            {
                Filename = name
            };

            Assert.AreEqual(name, info.Filename);

            // Change and re-test
            name = "test2.txt";
            info.Filename = name;
            Assert.AreEqual(name, info.Filename);
        }

        [TestMethod]
        public void TestSourceSubDirProp()
        {
            string name = @"test\1";

            BackupErrorInfo info = new BackupErrorInfo(BackupResult.OK)
            {
                SourceSubDir = name
            };

            Assert.AreEqual(name, info.SourceSubDir);

            // Change and re-test
            name = @"test\2";
            info.SourceSubDir = name;
            Assert.AreEqual(name, info.SourceSubDir);
        }

        [TestMethod]
        public void TestTargetDirInfoProp()
        {
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(@"C:\test\1");

            BackupErrorInfo info = new BackupErrorInfo(BackupResult.OK)
            {
                TargetDirInfo = di
            };

            Assert.IsNotNull(info.TargetDirInfo);
            Assert.AreSame(di, info.TargetDirInfo);
            Assert.AreEqual(di.FullName, info.TargetDirInfo.FullName);

            // Change and re-test
            di = new System.IO.DirectoryInfo(@"/test/2");
            info.TargetDirInfo = di;
            Assert.IsNotNull(info.TargetDirInfo);
            Assert.AreSame(di, info.TargetDirInfo);
            Assert.AreEqual(di.FullName, info.TargetDirInfo.FullName);
        }
    }
}
