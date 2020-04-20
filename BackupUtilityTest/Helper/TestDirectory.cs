using System;
using System.IO;

namespace BackupUtilityTest.Helper
{
    /// <summary>
    /// Class to assist testing.
    /// </summary>
    internal static class TestDirectory
    {
        /// <summary>
        /// Creates a test directory with dummy files.
        /// </summary>
        /// <param name="rootWorkingDir">Root directory for test</param>
        /// <param name="sourceSubDir">Sub directory for test</param>
        /// <returns>sourceDir, targetDir, hiddenFileCount</returns>
        public static Tuple<string, string, int> Create(string rootWorkingDir, string sourceSubDir = "Source")
        {
            ///////////////////////////////////
            // Initialize root dirs
            ///////////////////////////////////
            string rootSourceDir = Path.Combine(rootWorkingDir, sourceSubDir);
            string rootTargetDir = Path.Combine(rootWorkingDir, "Target");

            ///////////////////////////////////
            // Create root target - created by task
            ///////////////////////////////////
            //Directory.CreateDirectory(rootTargetDir);

            ///////////////////////////////////
            // Setup source test structure
            ///////////////////////////////////
            DirectoryInfo sourceDirInfo = Directory.CreateDirectory(rootSourceDir);

            // Create test source
            for (int i = 0; i < 3; i++)
            {
                // Create test files in root
                TestFile.Create(Path.Combine(rootSourceDir, $"root-file{i}.txt"));

                // Create some sub dirs with a file in
                DirectoryInfo alphaInfo = sourceDirInfo.CreateSubdirectory($"SubAlpha{i}");

                // Create some files in sub dir
                for (int j = 0; j < 2; j++)
                {
                    TestFile.Create(Path.Combine(alphaInfo.FullName, $"alpha-file{i}{j}.txt"));
                }

                // Create some sub-sub dirs with a file in
                for (int k = 0; k < 2; k++)
                {
                    string betaPath = alphaInfo.CreateSubdirectory($"SubBeta{k}").FullName;

                    TestFile.Create(Path.Combine(betaPath, $"beta-file{i}{k}.md"));
                }
            }

            ///////////////////////////////////
            // Add some hidden files
            ///////////////////////////////////
            int hiddenFileCount = 0;

            // Add hidden file to source root
            string hiddenFile = Path.Combine(rootSourceDir, ".hidden-file1.txt");
            TestFile.Create(hiddenFile);
            File.SetAttributes(hiddenFile, FileAttributes.Hidden);
            hiddenFileCount++;

            // Add hidden directory
            string hiddenDir = Path.Combine(rootSourceDir, ".hidden-dir");
            DirectoryInfo hiddenDirInfo = Directory.CreateDirectory(hiddenDir);
            hiddenDirInfo.Attributes = FileAttributes.Directory | FileAttributes.Hidden;

            // Add file to hidden directory
            string hiddenFile2 = Path.Combine(hiddenDir, ".hidden-file2.txt");
            TestFile.Create(hiddenFile2);
            //File.SetAttributes(hiddenFile2, FileAttributes.Hidden); // App considers files in hidden dir also hidden
            hiddenFileCount++;

            return new Tuple<string, string, int>(rootSourceDir, rootTargetDir, hiddenFileCount);
        }

        /// <summary>
        /// Gets the index where the source directory differs from the target.
        /// </summary>
        /// <param name="sourceDir">Name of source dir</param>
        /// <param name="targetDir">Name of target dir</param>
        /// <returns>Index position where they differ</returns>
        public static int IndexOfSourceSubDir(string sourceDir, string targetDir)
        {
            // Ensure stay within array bounds
            int maxLen = Math.Min(sourceDir.Length, targetDir.Length);

            // Don't return index at root
            int i = 0;

            // Find first char where directories differ
            for (; i < maxLen; i++)
            {
                if (sourceDir[i] != targetDir[i])
                {
                    break;
                }
            }

            // Return position where they differ
            return i;
        }
    }
}
