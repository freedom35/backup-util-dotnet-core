using BackupUtilityCore;
using BackupUtilityCore.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace BackupUtilityTest
{
    [TestClass]
    public sealed class TestBackupIsolatedCopy
    {
        private string testRoot;

        [TestInitialize]
        public void InitializeTest()
        {
            testRoot = Path.Combine(Environment.CurrentDirectory, "TestBackupIsolatedCopy");

            // Ensure removed from previous test
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, true);
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Remove all files (source and target)
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, true);
            }
        }

        [DataRow("2020-04-17 233102", true)]
        [DataRow("2021-01-16 105104-1", true)]
        [DataRow("2019-12-23 015959-2", true)]
        [DataRow("SubDir1", false)]
        [DataRow("junk", false)]
        [DataTestMethod]
        public void TestTryParseDateFromIsolatedDirectory(string dir, bool shouldParse)
        {
            Assert.AreEqual(shouldParse, BackupTaskIsolatedCopy.TryParseDateFromIsolatedDirectory(dir, out DateTime _));
        }

        [TestMethod]
        public void TestTryParseCorrectDate()
        {
            Assert.IsTrue(BackupTaskIsolatedCopy.TryParseDateFromIsolatedDirectory("2020-04-17 233102", out DateTime dirDate));

            DateTime correctDate = new DateTime(2020, 04, 17, 23, 31, 2);

            Assert.AreEqual(correctDate.Year, dirDate.Year);
            Assert.AreEqual(correctDate.Month, dirDate.Month);
            Assert.AreEqual(correctDate.Day, dirDate.Day);
            Assert.AreEqual(correctDate.Hour, dirDate.Hour);
            Assert.AreEqual(correctDate.Minute, dirDate.Minute);
            Assert.AreEqual(correctDate.Second, dirDate.Second);
        }

        [TestMethod]
        public void TestBackupCopyIsolated()
        {
            string testPath = Path.Combine(testRoot, "BackupCopyIsolated");

            var dirs = BackupDirectory.CreateTest(testPath);

            string rootWorkingDir = dirs.Item1;
            string rootSourceDir = dirs.Item2;
            string rootTargetDir = dirs.Item3;

            // Create settings
            BackupSettings settings = new BackupSettings()
            {
                BackupType = BackupType.Isolated,
                MaxIsololationDays = 0,
                IgnoreHiddenFiles = false,
                TargetDirectory = rootTargetDir,
                SourceDirectories = new string[] { rootSourceDir }
            };

            BackupTaskIsolatedCopy task = new BackupTaskIsolatedCopy()
            {
                RetryEnabled = false,
                MinFileWriteWaitTime = 0
            };

            task.Log += Task_Log;

            // Copy files
            int filesCopied1 = task.Run(settings);

            int sourceCount = Directory.GetFiles(rootSourceDir, "*.*", SearchOption.AllDirectories).Length;

            // Test results of 1st back
            VerifyLatestBackup(rootTargetDir, filesCopied1, rootSourceDir);

            // Verify 1 backup
            Assert.AreEqual(sourceCount, Directory.GetFiles(rootTargetDir, "*.*", SearchOption.AllDirectories).Length);

            // Run again - should create another directory (double file count)
            int filesCopied2 = task.Run(settings);

            // Test results of 2nd backup
            VerifyLatestBackup(rootTargetDir, filesCopied2, rootSourceDir);

            // Verify there are 2 backups
            Assert.AreEqual(sourceCount * 2, Directory.GetFiles(rootTargetDir, "*.*", SearchOption.AllDirectories).Length);

            // Remove handler
            task.Log -= Task_Log;
        }

        private void VerifyLatestBackup(string rootTargetDir, int filesCopied, string rootSourceDir)
        {
            // Filter source files that should have been copied
            var sourceFiles = Directory.EnumerateFiles(rootSourceDir, "*.*", SearchOption.AllDirectories);

            // Check expected number of files were copied
            Assert.AreEqual(sourceFiles.Count(), filesCopied);

            // Get all the target files
            var targetFiles = Directory.EnumerateFiles(rootTargetDir, "*.*", SearchOption.AllDirectories);

            // Remove target root from paths
            var isolatedTargetFilesWithoutRoots = targetFiles.Select(f => f.Substring(rootTargetDir.Length).TrimStart('\\', '/'));

            // Target will have date root - get latest one
            string dateSubDir = isolatedTargetFilesWithoutRoots.Last().Split(Path.DirectorySeparatorChar).First();

            // Check format is correct
            Assert.IsTrue(BackupTaskIsolatedCopy.TryParseDateFromIsolatedDirectory(dateSubDir, out DateTime dirDate));

            // Get latest and remove date sub-dir
            var targetFilesWithoutRoots = isolatedTargetFilesWithoutRoots.Where(t => t.StartsWith(dateSubDir)).Select(t => t.Substring(dateSubDir.Length + 1));

            // Check expected number of files were copied
            Assert.AreEqual(sourceFiles.Count(), targetFilesWithoutRoots.Count());

            // Compare directories
            foreach (string file in sourceFiles)
            {
                // Remove source root
                string sourceFileWithoutRoot = file.Substring(Path.GetPathRoot(rootSourceDir).Length);

                // Check it was copied
                Assert.IsTrue(targetFilesWithoutRoots.Contains(sourceFileWithoutRoot));
            }
        }

        private void Task_Log(object sender, MessageEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"COPY-ISO-TEST: {e}");
        }
    }
}
