using System;
using System.IO;

namespace BackupUtilityCore
{
    internal static class EmbeddedResource
    {
        public static bool CreateLocalCopy(string resourceName)
        {
            byte[] resourceBytes = GetResourceBytes(resourceName);

            // Save file in local directory
            string path = Path.Combine(Environment.CurrentDirectory, resourceName);

            // Create stream for writing to a file
            using FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);

            fs.Write(resourceBytes, 0, resourceBytes.Length);

            // Verify file written
            return (fs.Length == resourceBytes.Length);
        }

        public static byte[] GetResourceBytes(string resourceName)
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
