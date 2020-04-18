using BackupUtilityCore;
using BackupUtilityCore.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace BackupUtilityTest
{
    [TestClass]
    public sealed class TestBackupTestCopy
    {
        private void Task_Log(object sender, MessageEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"COPY-TEST: {e}");
        }

        private void VerifyBackup(string rootTargetDir, int filesCopied, string rootSourceDir, Func<string, bool> sourceFilter)
        {
            // Filter source files that should have been copied
            var sourceFiles = Directory.EnumerateFiles(rootSourceDir, "*.*", SearchOption.AllDirectories).Where(sourceFilter);

            // Get all the target files
            var targetFiles = Directory.EnumerateFiles(rootTargetDir, "*.*", SearchOption.AllDirectories);

            // Check expected number of files were copied
            Assert.AreEqual(sourceFiles.Count(), filesCopied);
            Assert.AreEqual(sourceFiles.Count(), targetFiles.Count());

            // Remove target root from paths
            string[] targetFilesWithoutRoots = targetFiles.Select(f => f.Substring(rootTargetDir.Length).TrimStart('\\', '/')).ToArray();

            // Compare directories (except hidden)
            foreach (string file in sourceFiles.Where(f => !File.GetAttributes(f).HasFlag(FileAttributes.Hidden) && !new DirectoryInfo(Path.GetDirectoryName(f)).Attributes.HasFlag(FileAttributes.Hidden)))
            {
                // Remove source root
                string sourceFileWithoutRoot = file.Substring(Path.GetPathRoot(rootSourceDir).Length);

                // Check it was copied
                Assert.IsTrue(targetFilesWithoutRoots.Contains(sourceFileWithoutRoot));
            }
        }

        [TestMethod]
        public void TestBackupCopy()
        {
            var dirs = BackupDirectory.CreateTest("TestBackupCopyBase");

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

            // Return all
            static bool sourceFilter(string f) => true;

            // Test results
            VerifyBackup(rootTargetDir, filesCopied, rootSourceDir, sourceFilter);

            // END OF TEST
            // Remove test dir (source and target)
            Directory.Delete(rootWorkingDir, true);
        }

        [TestMethod]
        public void TestBackupCopyExcludeHiddenFiles()
        {
            var dirs = BackupDirectory.CreateTest("TestBackupCopyHidden");

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

            // Test results
            VerifyBackup(rootTargetDir, filesCopied, rootSourceDir, sourceFilter);

            // END OF TEST
            // Remove test dir (source and target)
            Directory.Delete(rootWorkingDir, true);
        }

        [TestMethod]
        public void TestBackupCopyExcludeFileTypes()
        {
            var dirs = BackupDirectory.CreateTest("TestBackupCopyExcludeFile");

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

            // Test results
            VerifyBackup(rootTargetDir, filesCopied, rootSourceDir, sourceFilter);

            // END OF TEST
            // Remove test dir (source and target)
            Directory.Delete(rootWorkingDir, true);
        }

        [TestMethod]
        public void TestBackupCopyExcludeDirectories()
        {
            var dirs = BackupDirectory.CreateTest("TestBackupCopyExcludeDir");

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

            // Test results
            VerifyBackup(rootTargetDir, filesCopied, rootSourceDir, sourceFilter);

            // END OF TEST
            // Remove test dir (source and target)
            Directory.Delete(rootWorkingDir, true);
        }
    }
}
