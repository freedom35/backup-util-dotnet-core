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
    /// Test cases for BackupUtilityCore.Tasks.BackupTaskSync
    /// </summary>
    [TestClass]
    public sealed class TestBackupTaskSync
    {
        private static string testRoot;

        [ClassInitialize()]
        public static void InitializeTest(TestContext testContext)
        {
            testRoot = Path.Combine(testContext.TestRunDirectory, "TestBackupTaskSync");
        }

        [TestMethod]
        public void TestBackupSync()
        {
            string rootWorkingDir = Path.Combine(testRoot, "BackupSyncBase");

            var dirs = TestDirectory.Create(rootWorkingDir);

            string rootSourceDir = dirs.Item1;
            string rootTargetDir = dirs.Item2;

            // Create settings
            BackupSettings settings = new BackupSettings()
            {
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

            // Should be nothing added, new file removed from target
            Assert.AreEqual(0, filesCopied);

            // Compare directories
            targetCount = VerifyBackup(sourceFiles, rootTargetDir);

            // File should also have been deleted from target
            Assert.AreEqual(sourceFiles.Count(), targetCount);

            /////////////////////////////////////
            // Delete directory, run copy again
            /////////////////////////////////////
            addedDirInfo.Delete(true);

            filesCopied = task.Run(settings);

            // Should be nothing added
            Assert.AreEqual(0, filesCopied);

            // Compare directories
            targetCount = VerifyBackup(sourceFiles, rootTargetDir);

            // Dir and contents should also have been deleted from target
            Assert.AreEqual(sourceFiles.Count(), targetCount);

            /////////////////////////////////////
            // Change hidden files, run copy again
            /////////////////////////////////////
            settings.IgnoreHiddenFiles = true;

            // Return non-hidden
            static bool sourceFilter(string f) => !File.GetAttributes(f).HasFlag(FileAttributes.Hidden) && !new DirectoryInfo(Path.GetDirectoryName(f)).Attributes.HasFlag(FileAttributes.Hidden);

            // Refresh expected source files
            sourceFiles = Directory.EnumerateFiles(rootSourceDir, "*.*", SearchOption.AllDirectories).Where(sourceFilter);

            // Run task again
            filesCopied = task.Run(settings);

            // Should be nothing added
            Assert.AreEqual(0, filesCopied);

            // Compare directories
            targetCount = VerifyBackup(sourceFiles, rootTargetDir);

            // Dir and contents should also have been deleted from target
            Assert.AreEqual(sourceFiles.Count(), targetCount);

            // Remove handler
            task.Log -= Task_Log;
        }

        [TestMethod]
        public void TestBackupSyncExcludeHiddenFiles()
        {
            string rootWorkingDir = Path.Combine(testRoot, "BackupSyncHidden");

            var dirs = TestDirectory.Create(rootWorkingDir);

            string rootSourceDir = dirs.Item1;
            string rootTargetDir = dirs.Item2;
            int hiddenFileCount = dirs.Item3;

            // Create settings
            BackupSettings settings = new BackupSettings()
            {
                IgnoreHiddenFiles = true,
                TargetDirectory = rootTargetDir,
                SourceDirectories = new string[] { rootSourceDir }
            };

            BackupTaskSync task = new BackupTaskSync()
            {
                RetryEnabled = false,
                MinFileWriteWaitTime = 0
            };

            // Copy files
            int filesCopied = task.Run(settings);

            // Return non-hidden
            static bool sourceFilter(string f) => !File.GetAttributes(f).HasFlag(FileAttributes.Hidden) && !new DirectoryInfo(Path.GetDirectoryName(f)).Attributes.HasFlag(FileAttributes.Hidden);

            // Filter source files that should have been copied
            var sourceFiles = Directory.EnumerateFiles(rootSourceDir, "*.*", SearchOption.AllDirectories).Where(sourceFilter);

            // Check task returned expected number of files
            Assert.AreEqual(sourceFiles.Count(), filesCopied);

            // Compare directories
            int targetCount = VerifyBackup(sourceFiles, rootTargetDir);

            // Check expected number of files were copied
            Assert.AreEqual(sourceFiles.Count(), targetCount);
        }

        [TestMethod]
        public void TestBackupSyncExcludeFileTypes()
        {
            string rootWorkingDir = Path.Combine(testRoot, "BackupSyncExcludeFile");

            var dirs = TestDirectory.Create(rootWorkingDir);

            string rootSourceDir = dirs.Item1;
            string rootTargetDir = dirs.Item2;

            string[] excludedTypes = new string[] { "md", "bmp" };

            // Create settings
            BackupSettings settings = new BackupSettings()
            {
                IgnoreHiddenFiles = false,
                TargetDirectory = rootTargetDir,
                SourceDirectories = new string[] { rootSourceDir },
                ExcludedFileTypes = excludedTypes
            };

            BackupTaskSync task = new BackupTaskSync()
            {
                RetryEnabled = false,
                MinFileWriteWaitTime = 0
            };

            // Copy files
            int filesCopied = task.Run(settings);

            // Return without ext
            bool sourceFilter(string f) => !excludedTypes.Contains(Path.GetExtension(f).TrimStart('.'));

            // Filter source files that should have been copied
            var sourceFiles = Directory.EnumerateFiles(rootSourceDir, "*.*", SearchOption.AllDirectories).Where(sourceFilter);

            // Check task returned expected number of files
            Assert.AreEqual(sourceFiles.Count(), filesCopied);

            // Compare directories
            int targetCount = VerifyBackup(sourceFiles, rootTargetDir);

            // Check expected number of files were copied
            Assert.AreEqual(sourceFiles.Count(), targetCount);
        }

        [TestMethod]
        public void TestBackupSyncExcludeDirectories()
        {
            string rootWorkingDir = Path.Combine(testRoot, "BackupSyncExcludeDir");

            var dirs = TestDirectory.Create(rootWorkingDir);

            string rootSourceDir = dirs.Item1;
            string rootTargetDir = dirs.Item2;

            string[] excludedDirs = new string[] { "SubBeta0", "SubBeta1" };

            // Create settings
            BackupSettings settings = new BackupSettings()
            {
                IgnoreHiddenFiles = false,
                TargetDirectory = rootTargetDir,
                SourceDirectories = new string[] { rootSourceDir },
                ExcludedDirectories = excludedDirs
            };

            BackupTaskSync task = new BackupTaskSync()
            {
                RetryEnabled = false,
                MinFileWriteWaitTime = 0
            };

            // Copy files
            int filesCopied = task.Run(settings);

            // Check none of source directories are excluded
            bool sourceFilter(string f) => f.Split(Path.DirectorySeparatorChar).All(s => !excludedDirs.Contains(s));

            // Filter source files that should have been copied
            var sourceFiles = Directory.EnumerateFiles(rootSourceDir, "*.*", SearchOption.AllDirectories).Where(sourceFilter);

            // Check task returned expected number of files
            Assert.AreEqual(sourceFiles.Count(), filesCopied);

            // Compare directories
            int targetCount = VerifyBackup(sourceFiles, rootTargetDir);

            // Check expected number of files were copied
            Assert.AreEqual(sourceFiles.Count(), targetCount);
        }

        [TestMethod]
        public void TestBackupSyncDelete()
        {
            string rootWorkingDir = Path.Combine(testRoot, "BackupSyncDelete");

            var dirs = TestDirectory.Create(rootWorkingDir, "Source1");

            string rootSourceDir1 = dirs.Item1;
            string rootTargetDir = dirs.Item2;

            // Create 2nd source copy
            var dirs2 = TestDirectory.Create(rootWorkingDir, "Source2");
            string rootSourceDir2 = dirs2.Item1;

            // Create settings
            BackupSettings settings = new BackupSettings()
            {
                IgnoreHiddenFiles = false,
                TargetDirectory = rootTargetDir,
                SourceDirectories = new string[] { rootSourceDir1, rootSourceDir2 }
            };

            BackupTaskSync task = new BackupTaskSync()
            {
                RetryEnabled = false,
                MinFileWriteWaitTime = 0
            };

            /////////////////////////////////////
            // Copy files
            /////////////////////////////////////
            int filesCopied = task.Run(settings);

            // Filter source files that should have been copied
            var sourceFiles1 = Directory.EnumerateFiles(rootSourceDir1, "*.*", SearchOption.AllDirectories);
            var sourceFiles2 = Directory.EnumerateFiles(rootSourceDir2, "*.*", SearchOption.AllDirectories);

            var sourceFiles = sourceFiles1.Concat(sourceFiles2);

            // Check task returned expected number of files
            Assert.AreEqual(sourceFiles.Count(), filesCopied);

            // Compare directories
            int targetCount = VerifyBackup(sourceFiles, rootTargetDir);

            // Check expected number of files were copied
            Assert.AreEqual(sourceFiles.Count(), targetCount);

            /////////////////////////////////////
            // Remove 2nd directory
            /////////////////////////////////////
            settings.SourceDirectories = new string[] { rootSourceDir1 }; 

            filesCopied = task.Run(settings);

            // Should be nothing added
            Assert.AreEqual(0, filesCopied);

            // Compare directories
            targetCount = VerifyBackup(sourceFiles1, rootTargetDir);

            // 2nd dir and contents should also have been deleted from target
            Assert.AreEqual(sourceFiles1.Count(), targetCount);
        }

        private int VerifyBackup(IEnumerable<string> sourceFiles, string rootTargetDir)
        {
            // Get all the target files
            var targetFiles = Directory.EnumerateFiles(rootTargetDir, "*.*", SearchOption.AllDirectories);

            // Remove target root from paths
            var targetFilesWithoutRoots = targetFiles.Select(f => f.Substring(rootTargetDir.Length).TrimStart('\\', '/')).ToArray();

            // Get length of root string to be removed
            int rootSourceLength = TestDirectory.IndexOfSourceSubDir(sourceFiles.First(), rootTargetDir);

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
            System.Diagnostics.Debug.WriteLine($"SYNC-TEST: {e}");
        }
    }
}
