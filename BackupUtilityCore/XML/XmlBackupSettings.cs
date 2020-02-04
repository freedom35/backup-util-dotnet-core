using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Linq;

namespace BackupUtilityCore.XML
{
    public sealed class XmlBackupSettings : ISettingsParser
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

        //public override string ToString()
        //{
        //    return ToXml().OuterXml;
        //}

        public BackupSettings Parse(string fileName)
        {
            // Load file into memory
            XmlDocument doc = new XmlDocument();
            doc.Load(fileName);

            BackupSettings settings = new BackupSettings();

            // Parse settings
            LoadFromXml(doc, settings);

            return settings;
        }

        private void LoadFromXml(XmlDocument doc, BackupSettings settings)
        {
            // Check for something to load
            if (doc != null && doc.HasChildNodes && doc[XmlRoot] != null)
            {
                // Get root node
                XmlElement xeSettings = doc[XmlRoot];

                // Get target dir
                settings.TargetDirectory = xeSettings[XmlTargetDir]?.InnerText ?? "";

                // Get source directories
                if (xeSettings[XmlSourceDirs] != null)
                {
                    // Select all the source dir nodes
                    settings.SourceDirectories = xeSettings[XmlSourceDirs].SelectNodes(XmlSourceDir).Cast<XmlElement>().Select(xe => xe.InnerText).ToArray();
                }
                else
                {
                    // Clear property
                    settings.SourceDirectories = null;
                }

                // Get exluded files
                if (xeSettings[XmlExcludedTypes] != null)
                {
                    // Select all the excluded types
                    settings.ExcludedFileTypes = xeSettings[XmlExcludedTypes].SelectNodes(XmlExcludedType).Cast<XmlElement>().Select(xe => xe.InnerText).ToArray();

                    // Determine whether to ignore hidden files
                    // (Otherwise leave as default)
                    if (xeSettings[XmlExcludedTypes].HasAttribute(XmlIgnoreHidden) && bool.TryParse(xeSettings[XmlExcludedTypes].GetAttribute(XmlIgnoreHidden), out bool ignoreHidden))
                    {
                        settings.IgnoreHiddenFiles = ignoreHidden;
                    }
                }
                else
                {
                    // Clear property
                    settings.ExcludedFileTypes = null;
                }
            }
        }

        public void SaveToFile(string fileName, BackupSettings settings)
        {
            // Convert settings to xml
            XmlDocument doc = ToXml(settings);

            // Specify writer settings
            XmlWriterSettings writerSettings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                IndentChars = "    "
            };

            // Save settings to file
            using XmlWriter writer = XmlWriter.Create(fileName, writerSettings);
            doc.Save(writer);
        }

        public XmlDocument ToXml(BackupSettings settings)
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
            xeTargetDir.InnerText = settings.TargetDirectory;
            xeSettings.AppendChild(xeTargetDir);

            // Add comment for help
            xeSettings.AppendChild(doc.CreateComment("Add each source directory to backup"));

            // Create root node for source directories
            XmlElement xeSourceDirs = doc.CreateElement(XmlSourceDirs);
            xeSettings.AppendChild(xeSourceDirs);

            // Add each source directory
            foreach (string sourceDir in settings.SourceDirectories)
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
            xaIgnoreHidden.Value = settings.IgnoreHiddenFiles.ToString();
            xeExcludedTypes.Attributes.Append(xaIgnoreHidden);

            // Add each excluded type
            foreach (string fileType in settings.ExcludedFileTypes)
            {
                XmlElement xeExcludedType = doc.CreateElement(XmlExcludedType);
                xeExcludedType.InnerText = fileType;
                xeExcludedTypes.AppendChild(xeExcludedType);
            }

            return doc;
        }
    }
}
