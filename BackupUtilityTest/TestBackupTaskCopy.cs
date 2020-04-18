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
    public sealed class TestBackupTaskCopy
    {
        private string testRoot;

        [TestInitialize]
        public void InitializeTest()
        {
            testRoot = Path.Combine(Environment.CurrentDirectory, "TestBackupTaskCopy");

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
        public void TestBackupCopy()
        {
            string testPath = Path.Combine(testRoot, "BackupCopyBase");

            var dirs = TestDirectory.Create(testPath);

            string rootWorkingDir = dirs.Item1;
            string rootSourceDir = dirs.Item2;
            string rootTargetDir = dirs.Item3;

            // Create settings
            BackupSettings settings = new BackupSettings()
            {
                BackupType = BackupType.Copy,
                IgnoreHiddenFiles = false,
                TargetDirectory = rootTargetDir,
                SourceDirectories = new string[] { rootSourceDir }
            };

            BackupTaskCopy task = new BackupTaskCopy()
            {
                RetryEnabled = false,
                MinFileWriteWaitTime = 0
            };

            task.Log += Task_Log;

            // Copy files
            int filesCopied = task.Run(settings);

            // Remove handler
            task.Log -= Task_Log;

            // Filter source files that should have been copied
            var sourceFiles = Directory.EnumerateFiles(rootSourceDir, "*.*", SearchOption.AllDirectories);

            // Check task returned expected number of files
            Assert.AreEqual(sourceFiles.Count(), filesCopied);

            // Compare directories
            VerifyBackup(sourceFiles, rootTargetDir);

            // Run copy again
            filesCopied = task.Run(settings);

            // Should be no new copies - nothing changed
            Assert.AreEqual(0, filesCopied);

            // Compare directories
            VerifyBackup(sourceFiles, rootTargetDir);
        }

        [TestMethod]
        public void TestBackupCopyExcludeHiddenFiles()
        {
            string testPath = Path.Combine(testRoot, "BackupCopyHidden");

            var dirs = TestDirectory.Create(testPath);

            string rootWorkingDir = dirs.Item1;
            string rootSourceDir = dirs.Item2;
            string rootTargetDir = dirs.Item3;
            int hiddenFileCount = dirs.Item4;

            // Create settings
            BackupSettings settings = new BackupSettings()
            {
                BackupType = BackupType.Copy,
                IgnoreHiddenFiles = true,
                TargetDirectory = rootTargetDir,
                SourceDirectories = new string[] { rootSourceDir }
            };

            BackupTaskCopy task = new BackupTaskCopy()
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
            VerifyBackup(sourceFiles, rootTargetDir);
        }

        [TestMethod]
        public void TestBackupCopyExcludeFileTypes()
        {
            string testPath = Path.Combine(testRoot, "BackupCopyExcludeFile");

            var dirs = TestDirectory.Create(testPath);

            string rootWorkingDir = dirs.Item1;
            string rootSourceDir = dirs.Item2;
            string rootTargetDir = dirs.Item3;

            string[] excludedTypes = new string[] { "md", "bmp" };

            // Create settings
            BackupSettings settings = new BackupSettings()
            {
                BackupType = BackupType.Copy,
                IgnoreHiddenFiles = false,
                TargetDirectory = rootTargetDir,
                SourceDirectories = new string[] { rootSourceDir },
                ExcludedFileTypes = excludedTypes
            };

            BackupTaskCopy task = new BackupTaskCopy()
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
            VerifyBackup(sourceFiles, rootTargetDir);
        }

        [TestMethod]
        public void TestBackupCopyExcludeDirectories()
        {
            string testPath = Path.Combine(testRoot, "BackupCopyExcludeDir");

            var dirs = TestDirectory.Create(testPath);

            string rootWorkingDir = dirs.Item1;
            string rootSourceDir = dirs.Item2;
            string rootTargetDir = dirs.Item3;

            string[] excludedDirs = new string[] { "SubBeta0", "SubBeta1" };

            // Create settings
            BackupSettings settings = new BackupSettings()
            {
                BackupType = BackupType.Copy,
                IgnoreHiddenFiles = false,
                TargetDirectory = rootTargetDir,
                SourceDirectories = new string[] { rootSourceDir },
                ExcludedDirectories = excludedDirs
            };

            BackupTaskCopy task = new BackupTaskCopy()
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
            VerifyBackup(sourceFiles, rootTargetDir);
        }

        private void VerifyBackup(IEnumerable<string> sourceFiles, string rootTargetDir)
        {
            // Get all the target files
            var targetFiles = Directory.EnumerateFiles(rootTargetDir, "*.*", SearchOption.AllDirectories);

            // Check expected number of files were copied
            Assert.AreEqual(sourceFiles.Count(), targetFiles.Count());

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
        }

        private void Task_Log(object sender, MessageEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"COPY-TEST: {e}");
        }
    }
}
