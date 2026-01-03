using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BackupUtilityTest.Helper
{
    /// <summary>
    /// Class to help verify backup results.
    /// </summary>
    public static class TestBackup
    {
        public static int Verify(IEnumerable<string> sourceFiles, string rootTargetDir)
        {
            // Get all the target files
            var targetFiles = Directory.EnumerateFiles(rootTargetDir, "*.*", SearchOption.AllDirectories);

            // Remove target root from paths
            var targetFilesWithoutRoots = targetFiles.Select(f => f[rootTargetDir.Length..].TrimStart('\\', '/')).ToArray();

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

            return targetFiles.Count();
        }
    }
}