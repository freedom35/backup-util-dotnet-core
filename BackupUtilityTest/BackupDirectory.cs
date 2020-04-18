using System;
using System.IO;

namespace BackupUtilityTest
{
    /// <summary>
    /// Class to assist testing.
    /// </summary>
    internal static class BackupDirectory
    {
        /// <summary>
        /// Creates a test directory with dummy files.
        /// </summary>
        /// <param name="name">Name of test</param>
        /// <returns>workingDir, sourceDir, targetDir</returns>
        public static Tuple<string, string, string> CreateTest(string name)
        {
            string now = DateTime.Now.ToString("yyyyMMddHHmmssfff");

            // Initialize root dirs
            string rootWorkingDir = Path.Combine(Environment.CurrentDirectory, $"{name}-{now}");
            string rootSourceDir = Path.Combine(rootWorkingDir, "Source");
            string rootTargetDir = Path.Combine(rootWorkingDir, "Target");

            // Ensure any previous test removed so starting fresh
            if (Directory.Exists(rootWorkingDir))
            {
                Directory.Delete(rootWorkingDir, true);
            }

            // Create root target
            //Directory.CreateDirectory(rootTargetDir);

            // Setup source test structure
            DirectoryInfo sourceDirInfo = Directory.CreateDirectory(rootSourceDir);

            // Create test source
            for (int i = 0; i < 3; i++)
            {
                // Create test files in root
                CreateTestFile(Path.Combine(rootSourceDir, $"root-file{i}.txt"));

                // Create some sub dirs with a file in
                DirectoryInfo alphaInfo = sourceDirInfo.CreateSubdirectory($"SubAlpha{i}");

                // Create some files in sub dir
                for (int j = 0; j < 2; j++)
                {
                    CreateTestFile(Path.Combine(alphaInfo.FullName, $"alpha-file{i}{j}.txt"));
                }

                // Create some sub-sub dirs with a file in
                for (int k = 0; k < 2; k++)
                {
                    string betaPath = alphaInfo.CreateSubdirectory($"SubBeta{k}").FullName;

                    CreateTestFile(Path.Combine(betaPath, $"beta-file{i}{k}.txt"));
                }
            }
            
            return new Tuple<string, string, string>(rootWorkingDir, rootSourceDir, rootTargetDir);
        }

        /// <summary>
        /// Creates a small dummy test file.
        /// </summary>
        private static void CreateTestFile(string path)
        {
            using FileStream f = new FileStream(path, FileMode.Create);

            byte[] testBytes = new byte[]
            {
                0x54,
                0x45,
                0x53,
                0x54
            };

            f.Write(testBytes, 0, testBytes.Length);
            f.Close();
        }
    }
}
