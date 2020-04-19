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
    public sealed class TestBackupTaskSync
    {
        private string testRoot;

        [TestInitialize]
        public void InitializeTest()
        {
            testRoot = Path.Combine(Environment.CurrentDirectory, "TestBackupTaskSync");

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

        [TestMethod]
        public void TestBackupSync()
        {
            string testPath = Path.Combine(testRoot, "BackupSync");

            var dirs = TestDirectory.Create(testPath);

            string rootWorkingDir = dirs.Item1;
            string rootSourceDir = dirs.Item2;
            string rootTargetDir = dirs.Item3;

            // Create settings
            BackupSettings settings = new BackupSettings()
            {
                BackupType = BackupType.Sync,
                IgnoreHiddenFiles = false,
                TargetDirectory = rootTargetDir,
                SourceDirectories = new string[] { rootSourceDir }
            };

            BackupTaskSync task = new BackupTaskSync()
            {
                RetryEnabled = false,
                MinFileWriteWaitTime = 0
            };

            // Add handler to main copy test for debugging output
            task.Log += Task_Log;

            /////////////////////////////////////
            // Copy files
            /////////////////////////////////////
            int filesCopied = task.Run(settings);

            // Filter source files that should have been copied
            var sourceFiles = Directory.EnumerateFiles(rootSourceDir, "*.*", SearchOption.AllDirectories);

            // Check task returned expected number of files
            Assert.AreEqual(sourceFiles.Count(), filesCopied);

            // Compare directories
            int targetCount = VerifyBackup(sourceFiles, rootTargetDir);

            // Check expected number of files were copied
            Assert.AreEqual(sourceFiles.Count(), targetCount);

            /////////////////////////////////////
            // Run copy again
            /////////////////////////////////////
            filesCopied = task.Run(settings);

            // Should be no new copies - nothing changed
            Assert.AreEqual(0, filesCopied);

            // Compare directories
            targetCount = VerifyBackup(sourceFiles, rootTargetDir);

            // Check expected number of files were copied
            Assert.AreEqual(sourceFiles.Count(), targetCount);

            /////////////////////////////////////
            // Add files, run copy again
            /////////////////////////////////////
            // Add first file to root
            string addedFile1 = Path.Combine(rootSourceDir, "added-file1.txt");
            TestFile.Create(addedFile1);

            // Add second file to a new sub dir
            DirectoryInfo addedDirInfo = Directory.CreateDirectory(Path.Combine(rootSourceDir, "added-dir"));
            string addedFile2 = Path.Combine(addedDirInfo.FullName, "added-file2.txt");
            TestFile.Create(addedFile2);

            // Run backup again
            filesCopied = task.Run(settings);

            // Only the new files should be copied
            Assert.AreEqual(2, filesCopied);

            // Compare directories
            targetCount = VerifyBackup(sourceFiles, rootTargetDir);

            // Check expected number of files were copied
            Assert.AreEqual(sourceFiles.Count(), targetCount);

            /////////////////////////////////////
            // Modify file, run copy again
            /////////////////////////////////////
            TestFile.Modify(addedFile1);

            filesCopied = task.Run(settings);

            // Should only be modified file copied
            Assert.AreEqual(1, filesCopied);

            // Compare directories
            targetCount = VerifyBackup(sourceFiles, rootTargetDir);

            // Check expected number of files were copied
            Assert.AreEqual(sourceFiles.Count(), targetCount);

            /////////////////////////////////////
            // Delete file, run copy again
            /////////////////////////////////////
            File.Delete(addedFile1);

            filesCopied = task.Run(settings);

            // Should be nothing changed, added file remains in target
            Assert.AreEqual(0, filesCopied);

            // Compare directories
            targetCount = VerifyBackup(sourceFiles, rootTargetDir);

            // File should also have been deleted from target
            Assert.AreEqual(sourceFiles.Count(), targetCount);

            // Remove handler
            task.Log -= Task_Log;
        }

        private int VerifyBackup(IEnumerable<string> sourceFiles, string rootTargetDir)
        {
            // Get all the target files
            var targetFiles = Directory.EnumerateFiles(rootTargetDir, "*.*", SearchOption.AllDirectories);

            // Remove target root from paths
            var targetFilesWithoutRoots = targetFiles.Select(f => f.Substring(rootTargetDir.Length).TrimStart('\\', '/')).ToArray();

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

            return targetFiles.Count();
        }

        private void Task_Log(object sender, MessageEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"COPY-TEST: {e}");
        }
    }
}
