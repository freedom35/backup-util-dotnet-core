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

            // Get source files
            var sourceFiles = Directory.EnumerateFiles(rootSourceDir, "*.*", SearchOption.AllDirectories);
            var targetFiles = Directory.EnumerateFiles(rootTargetDir, "*.*", SearchOption.AllDirectories);

            // Check expected number of files were copied
            Assert.AreEqual(sourceFiles.Count(), filesCopied);
            Assert.AreEqual(sourceFiles.Count(), targetFiles.Count());

            // Remove target root from paths
            string[] targetFilesWithoutRoots = targetFiles.Select(f => f.Substring(rootTargetDir.Length).TrimStart('\\', '/')).ToArray();

            // Compare directories
            foreach (string file in sourceFiles)
            {
                // Remove source root
                string sourceFileWithoutRoot = file.Substring(Path.GetPathRoot(rootSourceDir).Length);

                // Check it was copied
                Assert.IsTrue(targetFilesWithoutRoots.Contains(sourceFileWithoutRoot));
            }

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

            task.Log += Task_Log;

            // Copy files
            int filesCopied = task.Run(settings);

            // Remove handler
            task.Log -= Task_Log;

            // Get source files
            var sourceFiles = Directory.EnumerateFiles(rootSourceDir, "*.*", SearchOption.AllDirectories);
            var targetFiles = Directory.EnumerateFiles(rootTargetDir, "*.*", SearchOption.AllDirectories);

            // Check expected number of files were copied
            Assert.AreEqual(sourceFiles.Count() - hiddenFileCount, filesCopied);
            Assert.AreEqual(sourceFiles.Count() - hiddenFileCount, targetFiles.Count());

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

            // END OF TEST
            // Remove test dir (source and target)
            Directory.Delete(rootWorkingDir, true);
        }

        [TestMethod]
        public void TestBackupCopyExcludeFileTypes()
        {
            // Create settings

            // Run task

            // Compare directories
        }

        [TestMethod]
        public void TestBackupCopyExcludeDirectories()
        {
            // Create settings

            // Run task

            // Compare directories
        }
    }
}
