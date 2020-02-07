using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

namespace BackupUtilityCore.YAML
{
    /// <summary>
    /// Simple YAML Parser
    /// (Not full YAML spec implementation, just relevant to config.)
    /// </summary>
    public static class YamlParser
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="path">NAme of YAML file</param>
        /// <returns>Key/Value pairs from parsed file.</returns>
        public static Dictionary<string, object> ParseFile(string path)
        {
            Dictionary<string, object> keyValues = new Dictionary<string, object>();

            // Open file for reading
            using StreamReader streamReader = new StreamReader(path);

            string line;
            string currentKey = null;
            List<string> currentSequence = null;

            // Read each line
            while ((line = streamReader.ReadLine()) != null)
            {
                line = TrimUnwantedChars(line);

                // Check for empty line/doc start/doc end/comment
                if (string.IsNullOrEmpty(line) || line.StartsWith("---") || line.StartsWith("...") || line.StartsWith("#"))
                {
                    continue;
                }
                else if (line.StartsWith('-'))
                {
                    // Sequence entry
                    if (currentSequence != null)
                    {
                        // Remove sequence char and quotes around directories
                        line = line.TrimStart('-').Trim('\"', '\'');

                        // Add to sequence
                        currentSequence.Add(line);
                    }
                    else
                    {
                        // No sequence started
                        throw new FormatException($"YAML sequence entry without key definition: {line}");
                    }
                }
                else
                {
                    // Check for key/value
                    string[] tmp = line.Split(':');

                    // Check whether key was found
                    if (tmp.Length > 1)
                    {
                        // Trim values either side of ':'
                        tmp[0] = tmp[0].TrimEnd();
                        tmp[1] = tmp[1].TrimStart();

                        // Check whether value was found
                        if (string.IsNullOrEmpty(tmp[1]))
                        {
                            // Start new sequence
                            currentKey = tmp[0];
                            currentSequence = new List<string>();
                        }
                        else
                        {
                            // New key/value pair
                            keyValues.Add(tmp[0], tmp[1]);

                            // End any current sequence
                            currentKey = null;
                            currentSequence = null;
                        }
                    }
                    else
                    {
                        // Not expecting this entry
                        throw new NotSupportedException($"YAML entry not supported: {line}");
                    }
                }
            }

            // Return true if 
            return keyValues;
        }

        /// <summary>
        /// Trims unwanted whitespace/line chars etc
        /// </summary>
        private static string TrimUnwantedChars(string s)
        {
            return s.Trim(' ', '\n', '\r');
        }
    }
}
