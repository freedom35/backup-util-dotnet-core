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
        /// Creates a platform independent copy of the specified resource in target directory.
        /// </summary>
        /// <param name="resourceName">Name of resource</param>
        /// <param name="targetPath">Target path where resource is created</param>
        /// <returns>true if file written of</returns>
        public static bool CreateCopyFromName(string resourceName, string targetPath)
        {
            // Different file based on platform
            string resourceDir = Environment.OSVersion.Platform == PlatformID.Unix ? "Unix" : "Windows";

            string resourcePath = $"BackupUtilityCore.Resources.{resourceDir}.{resourceName}";

            return CreateCopyFromPath(resourcePath, targetPath);
        }

        /// <summary>
        /// Creates a copy of the specified resource path in target directory.
        /// </summary>
        /// <param name="resourcePath">Full path to resource</param>
        /// <param name="targetPath">Target path where resource is created</param>
        /// <returns>true if file written of</returns>
        public static bool CreateCopyFromPath(string resourcePath, string targetPath)
        {
            byte[] resourceBytes = GetResourceBytes(resourcePath);

            // Create stream for writing to a file
            using FileStream fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write);

            fs.Write(resourceBytes, 0, resourceBytes.Length);

            // Verify file written
            return (fs.Length == resourceBytes.Length);
        }

        /// <summary>
        /// Gets byte array for specified resource.
        /// (Returns a different resource based on current platform.)
        /// </summary>
        private static byte[] GetResourceBytes(string resourcePath)
        {
            // Open resource as stream
            using Stream s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcePath);

            // Read bytes from stream
            byte[] resourceBytes = new byte[s.Length];
            s.Read(resourceBytes, 0, resourceBytes.Length);

            return resourceBytes;
        }
    }
}
