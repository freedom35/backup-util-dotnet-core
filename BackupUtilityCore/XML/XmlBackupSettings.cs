using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Linq;

namespace BackupUtilityCore.XML
{
    public sealed class XmlBackupSettings : IBackupSettings
    {
        #region Constants

        private const string XmlRoot = "Settings";

        private const string XmlTargetDir = "TargetDir";

        private const string XmlSourceDirs = "SourceDirs";
        private const string XmlSourceDir = "SourceDir";

        private const string XmlExcludedTypes = "ExcludedTypes";
        private const string XmlExcludedType = "ExcludedType";
        private const string XmlIgnoreHidden = "IgnoreHidden";

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the name of the settings file.
        /// </summary>
        /// <value>The name of the settings file.</value>
        public string SettingsFileName
        {
            get;
            private set;
        }

        public string[] SourceDirectories
        {
            get;
            set;
        } = new string[0];

        public string TargetDirectory
        {
            get;
            set;
        } = "";

        public string[] ExcludedDirectories
        {
            get;
            set;
        } = new string[0];

        public string[] ExcludedFileTypes
        {
            get;
            set;
        } = new string[0];

        /// <summary>
        /// Gets or sets a value indicating whether to ignore hidden files.
        /// </summary>
        /// <value><c>true</c> to ignore hidden files; otherwise, <c>false</c>.</value>
        public bool IgnoreHiddenFiles
        {
            get;
            set;
        } = true;

        #endregion

        public override string ToString()
        {
            return ToXml().OuterXml;
        }

        public void Parse(string fileName)
        {
            // Update file name
            SettingsFileName = fileName;

            // Load file into memory
            XmlDocument doc = new XmlDocument();
            doc.Load(fileName);

            // Parse settings
            LoadFromXml(doc);
        }

        public void LoadFromXml(XmlDocument doc)
        {
            // Check for something to load
            if (doc != null && doc.HasChildNodes && doc[XmlRoot] != null)
            {
                // Get root node
                XmlElement xeSettings = doc[XmlRoot];

                // Get target dir
                TargetDirectory = xeSettings[XmlTargetDir]?.InnerText ?? "";

                // Get source directories
                if (xeSettings[XmlSourceDirs] != null)
                {
                    // Select all the source dir nodes
                    SourceDirectories = xeSettings[XmlSourceDirs].SelectNodes(XmlSourceDir).Cast<XmlElement>().Select(xe => xe.InnerText).ToArray();
                }
                else
                {
                    // Clear property
                    SourceDirectories = null;
                }

                // Get exluded files
                if (xeSettings[XmlExcludedTypes] != null)
                {
                    // Select all the excluded types
                    ExcludedFileTypes = xeSettings[XmlExcludedTypes].SelectNodes(XmlExcludedType).Cast<XmlElement>().Select(xe => xe.InnerText).ToArray();

                    // Determine whether to ignore hidden files
                    // (Otherwise leave as default)
                    if (xeSettings[XmlExcludedTypes].HasAttribute(XmlIgnoreHidden) && bool.TryParse(xeSettings[XmlExcludedTypes].GetAttribute(XmlIgnoreHidden), out bool ignoreHidden))
                    {
                        IgnoreHiddenFiles = ignoreHidden;
                    }
                }
                else
                {
                    // Clear property
                    ExcludedFileTypes = null;
                }
            }
        }

        public void SaveToFile(string fileName)
        {
            // Update file name
            SettingsFileName = fileName;

            // Create file
            SaveToFile();
        }

        public void SaveToFile()
        {
            // Convert settings to xml
            XmlDocument doc = ToXml();

            // Specify writer settings
            XmlWriterSettings writerSettings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                IndentChars = "    "
            };

            // Save settings to file
            using XmlWriter writer = XmlWriter.Create(SettingsFileName, writerSettings);
            doc.Save(writer);
        }

        public XmlDocument ToXml()
        {
            XmlDocument doc = new XmlDocument();

            // Create xml header for document validation
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));

            // Create document root
            XmlElement xeSettings = doc.CreateElement(XmlRoot);
            doc.AppendChild(xeSettings);

            // Add comment for help
            xeSettings.AppendChild(doc.CreateComment("Specify root target directory for backup"));

            // Add target directory
            XmlElement xeTargetDir = doc.CreateElement(XmlTargetDir);
            xeTargetDir.InnerText = TargetDirectory;
            xeSettings.AppendChild(xeTargetDir);

            // Add comment for help
            xeSettings.AppendChild(doc.CreateComment("Add each source directory to backup"));

            // Create root node for source directories
            XmlElement xeSourceDirs = doc.CreateElement(XmlSourceDirs);
            xeSettings.AppendChild(xeSourceDirs);

            // Add each source directory
            foreach (string sourceDir in SourceDirectories)
            {
                XmlElement xeSourceDir = doc.CreateElement(XmlSourceDir);
                xeSourceDir.InnerText = sourceDir;
                xeSourceDirs.AppendChild(xeSourceDir);
            }

            // Add comment for help
            xeSettings.AppendChild(doc.CreateComment("Add extensions for excluded file types"));

            // Create root node for excluded
            XmlElement xeExcludedTypes = doc.CreateElement(XmlExcludedTypes);
            xeSettings.AppendChild(xeExcludedTypes);

            // Add attribute for hidden files
            XmlAttribute xaIgnoreHidden = doc.CreateAttribute(XmlIgnoreHidden);
            xaIgnoreHidden.Value = IgnoreHiddenFiles.ToString();
            xeExcludedTypes.Attributes.Append(xaIgnoreHidden);

            // Add each excluded type
            foreach (string fileType in ExcludedFileTypes)
            {
                XmlElement xeExcludedType = doc.CreateElement(XmlExcludedType);
                xeExcludedType.InnerText = fileType;
                xeExcludedTypes.AppendChild(xeExcludedType);
            }

            return doc;
        }
    }
}
