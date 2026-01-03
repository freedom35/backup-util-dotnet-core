using BackupUtilityCore;
using BackupUtilityCore.Tasks;
using BackupUtilityTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace BackupUtilityTest.Tasks
{
    /// <summary>
    /// Test cases for BackupUtilityCore.Tasks.BackupTaskSync
    /// </summary>
    [TestClass]
    public sealed class TestBackupTaskSync
    {
        private static string testRoot = "";

        [ClassInitialize()]
        public static void InitializeTest(TestContext testContext)
        {
            // Check directory valid
            if (string.IsNullOrEmpty(testContext.TestRunDirectory))
            {
                throw new ArgumentException("Test run directory not specified", nameof(testContext));
            }

            testRoot = Path.Combine(testContext.TestRunDirectory, "TestBackupTaskSync");
        }

        [ClassCleanup()]
        public static void CleanupTest()
        {
            Directory.Delete(testRoot, true);
        }

        [TestMethod]
        public void TestBackupSync()
        {
            string rootWorkingDir = Path.Combine(testRoot, "BackupSyncBase");

            var dirs = TestDirectory.Create(rootWorkingDir);

            string rootSourceDir = dirs.Item1;
            string rootTargetDir = dirs.Item2;

            // Create settings
            BackupSettings settings = new()
            {
                IgnoreHiddenFiles = false,
                TargetDirectory = rootTargetDir,
                SourceDirectories = [rootSourceDir]
            };

            BackupTaskSync task = new()
            {
                RetryEnabled = false,
                MinFileWriteWaitTime = 0
            };

            /////////////////////////////////////
            // Copy files
            /////////////////////////////////////
            int filesCopied = task.Run(settings);

            // Filter source files that should have been copied
            var sourceFiles = Directory.EnumerateFiles(rootSourceDir, "*.*", SearchOption.AllDirectories);

            // Check task returned expected number of files
            Assert.AreEqual(sourceFiles.Count(), filesCopied);

            // Compare directories
            int targetCount = TestBackup.Verify(sourceFiles, rootTargetDir);

            // Check expected number of files were copied
            Assert.AreEqual(sourceFiles.Count(), targetCount);

            /////////////////////////////////////
            // Run sync again
            /////////////////////////////////////
            filesCopied = task.Run(settings);

            // Should be no new copies - nothing changed
            Assert.AreEqual(0, filesCopied);

            // Compare directories
            targetCount = TestBackup.Verify(sourceFiles, rootTargetDir);

            // Check expected number of files were copied
            Assert.AreEqual(sourceFiles.Count(), targetCount);

            /////////////////////////////////////
            // Add files, run sync again
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
            targetCount = TestBackup.Verify(sourceFiles, rootTargetDir);

            // Check expected number of files were copied
            Assert.AreEqual(sourceFiles.Count(), targetCount);

            /////////////////////////////////////
            // Modify file, run sync again
            /////////////////////////////////////
            TestFile.Modify(addedFile1);

            filesCopied = task.Run(settings);

            // Should only be modified file copied
            Assert.AreEqual(1, filesCopied);

            // Compare directories
            targetCount = TestBackup.Verify(sourceFiles, rootTargetDir);

            // Check expected number of files were copied
            Assert.AreEqual(sourceFiles.Count(), targetCount);

            /////////////////////////////////////
            // Delete file, run sync again
            /////////////////////////////////////
            File.Delete(addedFile1);

            filesCopied = task.Run(settings);

            // Should be nothing added, new file removed from target
            Assert.AreEqual(0, filesCopied);

            // Compare directories
            targetCount = TestBackup.Verify(sourceFiles, rootTargetDir);

            // File should also have been deleted from target
            Assert.AreEqual(sourceFiles.Count(), targetCount);

            /////////////////////////////////////
            // Delete directory, run sync again
            /////////////////////////////////////
            addedDirInfo.Delete(true);

            filesCopied = task.Run(settings);

            // Should be nothing added
            Assert.AreEqual(0, filesCopied);

            // Compare directories
            targetCount = TestBackup.Verify(sourceFiles, rootTargetDir);

            // Dir and contents should also have been deleted from target
            Assert.AreEqual(sourceFiles.Count(), targetCount);

            /////////////////////////////////////
            // Change hidden files, run sync again
            /////////////////////////////////////
            settings.IgnoreHiddenFiles = true;

            // Return non-hidden
            static bool sourceFilter(string f) => !File.GetAttributes(f).HasFlag(FileAttributes.Hidden) && !new DirectoryInfo(Path.GetDirectoryName(f)!).Attributes.HasFlag(FileAttributes.Hidden);

            // Refresh expected source files
            sourceFiles = Directory.EnumerateFiles(rootSourceDir, "*.*", SearchOption.AllDirectories).Where(sourceFilter);

            // Run task again
            filesCopied = task.Run(settings);

            // Should be nothing added
            Assert.AreEqual(0, filesCopied);

            // Compare directories
            targetCount = TestBackup.Verify(sourceFiles, rootTargetDir);

            // Dir and contents should also have been deleted from target
            Assert.AreEqual(sourceFiles.Count(), targetCount);
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
            BackupSettings settings = new()
            {
                IgnoreHiddenFiles = true,
                TargetDirectory = rootTargetDir,
                SourceDirectories = [rootSourceDir]
            };

            BackupTaskSync task = new()
            {
                RetryEnabled = false,
                MinFileWriteWaitTime = 0
            };

            // Copy files
            int filesCopied = task.Run(settings);

            // Return non-hidden
            static bool sourceFilter(string f) => !File.GetAttributes(f).HasFlag(FileAttributes.Hidden) && !new DirectoryInfo(Path.GetDirectoryName(f)!).Attributes.HasFlag(FileAttributes.Hidden);

            // Filter source files that should have been copied
            var sourceFiles = Directory.EnumerateFiles(rootSourceDir, "*.*", SearchOption.AllDirectories).Where(sourceFilter);

            // Check task returned expected number of files
            Assert.AreEqual(sourceFiles.Count(), filesCopied);

            // Compare directories
            int targetCount = TestBackup.Verify(sourceFiles, rootTargetDir);

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

            string[] excludedTypes = ["md", "bmp"];

            // Create settings
            BackupSettings settings = new()
            {
                IgnoreHiddenFiles = false,
                TargetDirectory = rootTargetDir,
                SourceDirectories = [rootSourceDir],
                ExcludedFileTypes = excludedTypes
            };

            BackupTaskSync task = new()
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
            int targetCount = TestBackup.Verify(sourceFiles, rootTargetDir);

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

            string[] excludedDirs = ["SubBeta0", "SubBeta1"];

            // Create settings
            BackupSettings settings = new()
            {
                IgnoreHiddenFiles = false,
                TargetDirectory = rootTargetDir,
                SourceDirectories = [rootSourceDir],
                ExcludedDirectories = excludedDirs
            };

            BackupTaskSync task = new()
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
            int targetCount = TestBackup.Verify(sourceFiles, rootTargetDir);

            // Check expected number of files were copied
            Assert.AreEqual(sourceFiles.Count(), targetCount);
        }

        /// <summary>
        /// Tests excluded directories in config are overridden from direct source directories.
        /// </summary>
        [TestMethod]
        public void TestBackupSyncExcludeDirectoryOverride()
        {
            string rootWorkingDir = Path.Combine(testRoot, "BackupSyncExcludeDirOverride");

            var dirs = TestDirectory.Create(rootWorkingDir);

            string rootSourceDir = dirs.Item1;
            string rootTargetDir = dirs.Item2;

            // Create file in root of bin
            string rootSourceDirBin = Path.Combine(rootSourceDir, "bin");
            Directory.CreateDirectory(rootSourceDirBin);
            TestFile.Create(Path.Combine(rootSourceDirBin, $"bin-file.txt"));

            // Create sub folder/file deeper within bin
            string rootSourceDirBinDebug = Path.Combine(rootSourceDirBin, "debug");
            Directory.CreateDirectory(rootSourceDirBinDebug);
            TestFile.Create(Path.Combine(rootSourceDirBinDebug, $"debug-file.txt"));

            // Create settings
            BackupSettings settings = new()
            {
                IgnoreHiddenFiles = false,
                TargetDirectory = rootTargetDir,
                SourceDirectories = [rootSourceDir],
                ExcludedDirectories = []
            };

            BackupTaskSync task = new()
            {
                RetryEnabled = false,
                MinFileWriteWaitTime = 0
            };

            // Copy files
            int filesCopied = task.Run(settings);

            // All source files should have been copied
            var sourceFiles = Directory.EnumerateFiles(rootSourceDir, "*.*", SearchOption.AllDirectories);

            // Check task returned expected number of files
            Assert.AreEqual(sourceFiles.Count(), filesCopied);

            // Compare directories
            int targetCount = TestBackup.Verify(sourceFiles, rootTargetDir);

            // Check expected number of files were copied
            Assert.AreEqual(sourceFiles.Count(), targetCount);

            ////////////////////////////////////////////////////////////
            // Exclude bin, but override with source dir to retain it
            ////////////////////////////////////////////////////////////
            settings.ExcludedDirectories = ["bin"];
            settings.SourceDirectories = [rootSourceDir, rootSourceDirBin];

            // Shoould be 0, as all files already copied
            filesCopied = task.Run(settings);

            // All source files should have been copied
            sourceFiles = Directory.EnumerateFiles(rootSourceDir, "*.*", SearchOption.AllDirectories);

            // Check task returned expected number of files
            Assert.AreEqual(0, filesCopied);

            // Compare directories
            targetCount = TestBackup.Verify(sourceFiles, rootTargetDir);

            // Check expected number of files were copied
            Assert.AreEqual(sourceFiles.Count(), targetCount);
        }

        [TestMethod]
        public void TestBackupSyncRemovedFromConfig()
        {
            string rootWorkingDir = Path.Combine(testRoot, "BackupSyncRemovedFromConfig");

            var dirs = TestDirectory.Create(rootWorkingDir, "Source1");

            string rootSourceDir1 = dirs.Item1;
            string rootTargetDir = dirs.Item2;

            // Create 2nd source copy
            var dirs2 = TestDirectory.Create(rootWorkingDir, "Source2");
            string rootSourceDir2 = dirs2.Item1;

            // Create settings
            BackupSettings settings = new()
            {
                IgnoreHiddenFiles = false,
                TargetDirectory = rootTargetDir,
                SourceDirectories = [rootSourceDir1, rootSourceDir2]
            };

            BackupTaskSync task = new()
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
            int targetCount = TestBackup.Verify(sourceFiles, rootTargetDir);

            // Check expected number of files were copied
            Assert.AreEqual(sourceFiles.Count(), targetCount);

            /////////////////////////////////////
            // Remove 2nd directory
            /////////////////////////////////////
            settings.SourceDirectories = [rootSourceDir1];

            filesCopied = task.Run(settings);

            // Should be nothing added
            Assert.AreEqual(0, filesCopied);

            // Compare directories
            targetCount = TestBackup.Verify(sourceFiles1, rootTargetDir);

            // 2nd dir (no longer in config) and contents should have been preserved - not deleted from target
            Assert.AreEqual(sourceFiles.Count(), targetCount);
        }
    }
}
