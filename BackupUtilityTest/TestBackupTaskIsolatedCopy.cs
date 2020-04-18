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
    [TestClass]
    public sealed class TestBackupTaskIsolatedCopy
    {
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
            string testRoot = Path.Combine(Environment.CurrentDirectory, "TestBackupIsolatedCopy");

            // Ensure removed from previous test
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, true);
            }

            string testPath = Path.Combine(testRoot, "BackupCopyIsolated");

            var dirs = TestDirectory.Create(testPath);

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

            // Remove all files (source and target)
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, true);
            }
        }

        private string VerifyLatestBackup(IEnumerable<string> sourceFiles, string rootTargetDir, int filesCopied)
        {
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

            // Get length of root string to be removed
            int rootSourceLength = Path.GetPathRoot(sourceFiles.First()).Length;

            // Compare directories
            foreach (string file in sourceFiles)
            {
                // Remove source root
                string sourceFileWithoutRoot = file.Substring(rootSourceLength);

                // Check it was copied
                Assert.IsTrue(targetFilesWithoutRoots.Contains(sourceFileWithoutRoot));
            }

            return dateSubDir;
        }

        private void Task_Log(object sender, MessageEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"COPY-ISO-TEST: {e}");
        }
    }
}
