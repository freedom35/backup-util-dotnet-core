using System;
using System.IO;

namespace BackupUtilityCore
{
    /// <summary>
    /// Class to access embedded resources within project.
    /// </summary>
    public static class EmbeddedResource
    {
        /// <summary>
        /// Creates a platform independent copy of the default config in the target directory.
        /// (Returns a different resource based on current platform.)
        /// </summary>
        /// <param name="targetPath">Target path where resource is created</param>
        /// <returns>true if file written ok</returns>
        public static bool CreateDefaultConfig(string targetPath)
        {
            const string EmbeddedConfigName = "backup-config.yaml";

            // Different file based on platform
            string resourceDir = Environment.OSVersion.Platform == PlatformID.Unix ? "Unix" : "Windows";

            // Build local resource path
            string resourcePath = $"BackupUtilityCore.Resources.{resourceDir}.{EmbeddedConfigName}";

            return CreateCopyFromPath(resourcePath, targetPath);
        }

        /// <summary>
        /// Creates a copy of the specified resource path in target directory.
        /// </summary>
        /// <param name="resourcePath">Full path to resource</param>
        /// <param name="targetPath">Target path where resource is created</param>
        /// <returns>true if file written ok</returns>
        public static bool CreateCopyFromPath(string resourcePath, string targetPath)
        {
            // Open resource as stream
            using Stream resourceStream = System.Reflection.Assembly.GetCallingAssembly().GetManifestResourceStream(resourcePath);

            // Read bytes from stream
            if (TryGetResourceBytes(resourceStream, out byte[] resourceBytes))
            {
                // Create stream for writing to a file
                using FileStream fs = new(targetPath, FileMode.Create, FileAccess.Write);

                fs.Write(resourceBytes, 0, resourceBytes.Length);

                // Verify file written
                return (fs.Length == resourceBytes.Length);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets byte array from stream.
        /// </summary>
        private static bool TryGetResourceBytes(Stream resourceStream, out byte[] resourceBytes)
        {
            // Verify stream
            if (resourceStream != null)
            {
                // Read bytes from stream
                resourceBytes = new byte[resourceStream.Length];
                resourceStream.Read(resourceBytes, 0, resourceBytes.Length);

                // Verify all bytes read
                return (resourceBytes.Length == resourceStream.Length);
            }
            else
            {
                resourceBytes = null;
                return false;
            }
        }
    }
}
