using System;
using System.IO;

namespace BackupUtilityCore
{
    /// <summary>
    /// Class to access embedded resources within project.
    /// </summary>
    internal static class EmbeddedResource
    {
        /// <summary>
        /// Creates a copy of the specified resource in the current directory.
        /// </summary>
        public static bool CreateLocalCopy(string resourceName, string copyPath)
        {
            byte[] resourceBytes = GetResourceBytes(resourceName);

            // Create stream for writing to a file
            using FileStream fs = new FileStream(copyPath, FileMode.Create, FileAccess.Write);

            fs.Write(resourceBytes, 0, resourceBytes.Length);

            // Verify file written
            return (fs.Length == resourceBytes.Length);
        }

        /// <summary>
        /// Gets byte array for specified resource.
        /// (Returns a different resource based on current platform.)
        /// </summary>
        private static byte[] GetResourceBytes(string resourceName)
        {
            // Different file based on platform
            string resourceDir = Environment.OSVersion.Platform == PlatformID.Unix ? "Unix" : "Windows";

            // Open resource as stream
            using Stream s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream($"BackupUtilityCore.Resources.{resourceDir}.{resourceName}");

            // Read bytes from stream
            byte[] resourceBytes = new byte[s.Length];
            s.Read(resourceBytes, 0, resourceBytes.Length);

            return resourceBytes;
        }
    }
}
