using System.IO;

namespace BackupUtilityTest.Helper
{
    /// <summary>
    /// Class to assist testing.
    /// </summary>
    internal static class TestFile
    {
        /// <summary>
        /// Creates a small dummy test file.
        /// </summary>
        public static void Create(string path)
        {
            using FileStream f = new(path, FileMode.Create);

            byte[] testBytes = "TEST"u8.ToArray();

            f.Write(testBytes, 0, testBytes.Length);
            f.Close();
        }

        /// <summary>
        /// Modifies contents of test file.
        /// </summary>
        public static void Modify(string path)
        {
            using FileStream f = new(path, FileMode.Append);

            byte[] testBytes = "MOD"u8.ToArray();

            f.Write(testBytes, 0, testBytes.Length);
            f.Close();
        }
    }
}
