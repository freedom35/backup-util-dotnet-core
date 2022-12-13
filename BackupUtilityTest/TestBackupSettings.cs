using BackupUtilityCore;
using BackupUtilityTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System;

namespace BackupUtilityTest
{
    /// <summary>
    /// Test cases for BackupUtilityCore.BackupSettings
    /// </summary>
    [TestClass]
    public sealed class TestBackupSettings
    {
        private static string testRoot = "";

        [ClassInitialize()]
        public static void InitializeTest(TestContext testContext)
        {
            // Check directory valid
            if (string.IsNullOrEmpty(testContext.TestRunDirectory))
            {
                throw new ArgumentException("Test run directory not specified", nameof(testContext));
            }

            testRoot = Path.Combine(testContext.TestRunDirectory, "TestBackupSettings");

            Directory.CreateDirectory(testRoot);
        }

        [TestMethod]
        public void TestBackupTypeProp()
        {
            BackupType type = BackupType.Sync;

            BackupSettings settings = new()
            {
                BackupType = type
            };

            Assert.AreEqual(type, settings.BackupType);

            // Change and re-test
            type = BackupType.Copy;
            settings.BackupType = type;
            Assert.AreEqual(type, settings.BackupType);
        }

        [TestMethod]
        public void TestSourceDirectoriesProp()
        {
            string[] dirs = new string[]
            {
                "dir1",
                "dir2"
            };

            BackupSettings settings = new()
            {
                SourceDirectories = dirs
            };

            Assert.IsNotNull(settings.SourceDirectories);
            Assert.AreEqual(dirs.Length, settings.SourceDirectories.Length);

            for (int i = 0; i < dirs.Length; i++)
            {
                Assert.AreEqual(dirs[i], settings.SourceDirectories[i]);
            }
        }

        [TestMethod]
        public void TestTargetDirectoryProp()
        {
            string dir = "directory_name1";

            BackupSettings settings = new()
            {
                TargetDirectory = dir
            };

            Assert.AreEqual(dir, settings.TargetDirectory);

            // Change and re-test
            dir = "directory_name2";
            settings.TargetDirectory = dir;
            Assert.AreEqual(dir, settings.TargetDirectory);
        }

        [TestMethod]
        public void TestExcludedDirectoriesProp()
        {
            string[] dirs = new string[]
            {
                "Dir1",
                "Dir2"
            };

            BackupSettings settings = new()
            {
                ExcludedDirectories = dirs
            };

            Assert.IsNotNull(settings.ExcludedDirectories);
            Assert.AreEqual(dirs.Length, settings.ExcludedDirectories.Length);

            for (int i = 0; i < dirs.Length; i++)
            {
                // Property converts to lowercase
                Assert.AreEqual(dirs[i].ToLower(), settings.ExcludedDirectories[i]);
            }
        }

        [TestMethod]
        public void TestHasExcludedDirectoriesProp()
        {
            BackupSettings settings = new()
            {
                ExcludedDirectories = System.Array.Empty<string>()
            };

            Assert.IsFalse(settings.HasExcludedDirectories);

            // Assign some directories
            settings.ExcludedDirectories = new string[]
            {
                "Dir1",
                "Dir2"
            };

            Assert.IsTrue(settings.HasExcludedDirectories);
        }

        [TestMethod]
        public void TestExcludedFileTypesProp()
        {
            string[] types = new string[]
            {
                "txt",
                "exe",
                ".jpg"
            };

            BackupSettings settings = new()
            {
                ExcludedFileTypes = types
            };

            Assert.IsNotNull(settings.ExcludedFileTypes);
            Assert.AreEqual(types.Length, settings.ExcludedFileTypes.Length);

            for (int i = 0; i < types.Length; i++)
            {
                // Property converts to lowercase and strips '.'
                Assert.AreEqual(types[i].ToLower().TrimStart('.'), settings.ExcludedFileTypes[i]);
            }
        }

        [TestMethod]
        public void TestHasExcludedFileTypesProp()
        {
            BackupSettings settings = new()
            {
                ExcludedFileTypes = System.Array.Empty<string>()
            };

            Assert.IsFalse(settings.HasExcludedFileTypes);

            // Assign some files
            settings.ExcludedFileTypes = new string[]
            {
                "md",
                "bmp"
            };

            Assert.IsTrue(settings.HasExcludedFileTypes);
        }

        [TestMethod]
        public void TestIgnoreHiddenFilesProp()
        {
            bool ignore = false;

            BackupSettings settings = new()
            {
                IgnoreHiddenFiles = ignore
            };

            Assert.AreEqual(ignore, settings.IgnoreHiddenFiles);

            // Change and re-test
            ignore = !ignore;
            settings.IgnoreHiddenFiles = ignore;
            Assert.AreEqual(ignore, settings.IgnoreHiddenFiles);
        }

        [TestMethod]
        public void TestMaxIsolationDaysProp()
        {
            int age = 30;

            BackupSettings settings = new()
            {
                MaxIsololationDays = age
            };

            Assert.AreEqual(age, settings.MaxIsololationDays);

            // Change and re-test
            age = 45;
            settings.MaxIsololationDays = age;
            Assert.AreEqual(age, settings.MaxIsololationDays);
        }

        [TestMethod]
        public void TestValidProp()
        {
            BackupSettings settings = new();

            Assert.IsFalse(settings.Valid);

            settings.TargetDirectory = "dir1";

            Assert.IsFalse(settings.Valid);

            settings.SourceDirectories = new string[] { "dir2" };

            Assert.IsFalse(settings.Valid);

            settings.BackupType = BackupType.Sync;

            // All requirements now set
            Assert.IsTrue(settings.Valid);

            settings.TargetDirectory = "";

            Assert.IsFalse(settings.Valid);

            settings.TargetDirectory = "dir3";

            Assert.IsTrue(settings.Valid);

            settings.SourceDirectories = System.Array.Empty<string>();

            Assert.IsFalse(settings.Valid);
        }

        [TestMethod]
        public void TestIsFileExcludedMethod()
        {
            string[] types = new string[]
            {
                "txt",
                "md",
                ".jpg"
            };

            BackupSettings settings = new()
            {
                ExcludedFileTypes = types
            };

            Assert.IsTrue(settings.IsFileTypeExcluded("readme.txt"));
            Assert.IsTrue(settings.IsFileTypeExcluded("README.MD"));

            Assert.IsFalse(settings.IsFileTypeExcluded("Program.cs"));
        }

        [TestMethod]
        public void TestIsDirectoryExcludedMethod()
        {
            string[] dirs = new string[]
            {
                "git",
                "vs",
                "release"
            };

            BackupSettings settings = new()
            {
                ExcludedDirectories = dirs
            };

            Assert.IsTrue(settings.IsDirectoryExcluded("git"));
            Assert.IsTrue(settings.IsDirectoryExcluded("RELEASE"));

            Assert.IsFalse(settings.IsDirectoryExcluded("bin"));
        }

        [TestMethod]
        public void TestGetInvalidSettingsMethod()
        {
            BackupSettings settings = new();

            // Critical settings not valid by default
            Assert.AreEqual(3, settings.GetInvalidSettings().Count);

            settings.BackupType = BackupType.Copy;
            Assert.AreEqual(2, settings.GetInvalidSettings().Count);

            settings.TargetDirectory = "dir1";
            Assert.AreEqual(1, settings.GetInvalidSettings().Count);

            settings.SourceDirectories = new string[] { "dir2" };
            Assert.AreEqual(0, settings.GetInvalidSettings().Count);
        }

        [TestMethod]
        public void TestParseFromYaml()
        {
            string targetPath = Path.Combine(testRoot, "test-parse-config.yaml");

            // Create copy of local resource
            EmbeddedResource.CreateCopyFromPath(TestConfig.ResourcePath, targetPath);

            // Parse newly created file
            Assert.IsTrue(BackupSettings.TryParseFromYaml(targetPath, out BackupSettings settings));

            // Verify parsed file
            Assert.IsNotNull(settings);
            Assert.IsTrue(settings.Valid);
            Assert.AreEqual(Path.GetFileName(targetPath), settings.SettingsFilename);

            // Compare values to test-config.yaml (embedded resource)
            Assert.AreEqual(BackupType.Sync, settings.BackupType);
            Assert.AreEqual(false, settings.IgnoreHiddenFiles);
            Assert.AreEqual(14, settings.MaxIsololationDays);
            Assert.AreEqual(@"C:\Target\Test", settings.TargetDirectory);

            string[] testSource = new string[]
            {
                @"C:\Source\Projects",
                @"C:\Source\Documents"
            };

            CompareArrays(testSource, settings.SourceDirectories);

            CompareArrays(System.Array.Empty<string>(), settings.ExcludedDirectories);

            string[] testExcludedDirs = new string[]
            {
                "zip"
            };

            CompareArrays(testExcludedDirs, settings.ExcludedFileTypes);
        }

        private static void CompareArrays(string[] source, string[] target)
        {
            Assert.AreEqual(source.Length, target.Length);

            for (int i = 0; i < source.Length; i++)
            {
                Assert.AreEqual(source[i], target[i]);
            }
        }
    }
}
