using BackupUtilityCore;
using BackupUtilityCore.Tasks;
using BackupUtilityTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BackupUtilityTest
{
    /// <summary>
    /// Test cases for BackupUtilityCore.Tasks.BackupTaskIsolatedCopy
    /// </summary>
    [TestClass]
    public sealed class TestBackupTaskIsolatedCopy
    {
        private static string testRoot;

        [ClassInitialize()]
        public static void InitializeTest(TestContext testContext)
        {
            testRoot = Path.Combine(testContext.TestRunDirectory, "TestBackupIsolatedCopy");
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

            DateTime correctDate = new(2020, 04, 17, 23, 31, 2);

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
            string rootWorkingDir = Path.Combine(testRoot, "BackupCopyIsolated");

            var dirs = TestDirectory.Create(rootWorkingDir);

            string rootSourceDir = dirs.Item1;
            string rootTargetDir = dirs.Item2;

            // Create settings
            BackupSettings settings = new()
            {
                MaxIsololationDays = 0,
                IgnoreHiddenFiles = false,
                TargetDirectory = rootTargetDir,
                SourceDirectories = new string[] { rootSourceDir }
            };

            BackupTaskIsolatedCopy task = new()
            {
                RetryEnabled = false,
                MinFileWriteWaitTime = 0
            };

            // Add handler for debug
            task.Log += Task_Log;

            ///////////////////////////////////////////
            // Copy files
            ///////////////////////////////////////////
            int filesCopied1 = task.Run(settings);

            // Filter source files that should have been copied
            var sourceFiles = Directory.EnumerateFiles(rootSourceDir, "*.*", SearchOption.AllDirectories);
            int sourceCount = sourceFiles.Count();

            // Test results of 1st back
            string dateSubDir = VerifyLatestBackup(sourceFiles, rootTargetDir, filesCopied1);

            // Verify 1 backup
            Assert.AreEqual(sourceCount, Directory.GetFiles(rootTargetDir, "*.*", SearchOption.AllDirectories).Length);

            ///////////////////////////////////////////
            // Run again -
            // should create another directory
            // (double file count)
            ///////////////////////////////////////////
            int filesCopied2 = task.Run(settings);

            // Test results of 2nd backup
            VerifyLatestBackup(sourceFiles, rootTargetDir, filesCopied2);

            // Verify there are 2 backups
            Assert.AreEqual(sourceCount * 2, Directory.GetFiles(rootTargetDir, "*.*", SearchOption.AllDirectories).Length);

            ///////////////////////////////////////////
            // Test deleting old backups
            ///////////////////////////////////////////

            // Rename first backup to be 'older'
            DateTime olderDate = DateTime.Now.AddDays(-5);
            string newDateDir = Path.Combine(rootTargetDir, olderDate.ToString(BackupTaskIsolatedCopy.DirDateFormat));

            // Rename directory
            Directory.Move(Path.Combine(rootTargetDir, dateSubDir), newDateDir);

            // Verify 'old' backup now exists
            Assert.IsTrue(Directory.Exists(newDateDir));

            // Set max isolation days
            settings.MaxIsololationDays = 1;

            // Run backup
            int filesCopied3 = task.Run(settings);

            // Test results of 3rd backup
            VerifyLatestBackup(sourceFiles, rootTargetDir, filesCopied3);

            // Verify there are still 2 backups (old one deleted, and one new one)
            Assert.AreEqual(sourceCount * 2, Directory.GetFiles(rootTargetDir, "*.*", SearchOption.AllDirectories).Length);

            // Verify 'old' backup no longer exists
            Assert.IsFalse(Directory.Exists(newDateDir));

            ///////////////////////////////////////////
            // Cleanup
            ///////////////////////////////////////////

            // Remove handler
            task.Log -= Task_Log;
        }

        private static string VerifyLatestBackup(IEnumerable<string> sourceFiles, string rootTargetDir, int filesCopied)
        {
            // Check expected number of files were copied
            Assert.AreEqual(sourceFiles.Count(), filesCopied);

            // Get all the target files
            var targetFiles = Directory.EnumerateFiles(rootTargetDir, "*.*", SearchOption.AllDirectories);

            // Remove target root from paths
            var isolatedTargetFilesWithoutRoots = targetFiles.Select(f => f[rootTargetDir.Length..].TrimStart('\\', '/'));

            // Get date portion from target root
            var dirDates = isolatedTargetFilesWithoutRoots.Select(f => f.Split(Path.DirectorySeparatorChar).First()).Distinct();

            // Get latest one
            string dateSubDir = dirDates.OrderBy(f => f).Last();

            // Check format is correct
            Assert.IsTrue(BackupTaskIsolatedCopy.TryParseDateFromIsolatedDirectory(dateSubDir, out DateTime dirDate));

            // Get latest and remove date sub-dir
            var targetFilesWithoutRoots = isolatedTargetFilesWithoutRoots.Where(t => t.StartsWith(dateSubDir)).Select(t => t[(dateSubDir.Length + 1)..]);

            // Check expected number of files were copied
            Assert.AreEqual(sourceFiles.Count(), targetFilesWithoutRoots.Count());

            // Get length of root string to be removed
            int rootSourceLength = TestDirectory.IndexOfSourceSubDir(sourceFiles.First(), rootTargetDir);

            // Compare directories
            foreach (string file in sourceFiles)
            {
                // Remove source root
                string sourceFileWithoutRoot = file[rootSourceLength..];

                // Check it was copied
                Assert.IsTrue(targetFilesWithoutRoots.Contains(sourceFileWithoutRoot));
            }

            return dateSubDir;
        }

        private void Task_Log(object sender, MessageEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"ISO-COPY-TEST: {e}");
        }
    }
}
