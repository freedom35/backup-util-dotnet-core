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

        /// <summary>
        /// Modifies contents of test file.
        /// </summary>
        public static void Modify(string path)
        {
            using FileStream f = new(path, FileMode.Append);

            byte[] testBytes = new byte[]
            {
                0x4d,
                0x4f,
                0x44
            };

            f.Write(testBytes, 0, testBytes.Length);
            f.Close();
        }
    }
}
