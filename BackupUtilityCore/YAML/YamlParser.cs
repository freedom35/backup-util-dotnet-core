using System;
using System.Collections.Generic;
using System.IO;

namespace BackupUtilityCore.YAML
{
    /// <summary>
    /// Simple YAML Parser
    /// (Not full YAML spec implementation, just relevant to config.)
    /// </summary>
    public static class YamlParser
    {
        /// <summary>
        /// Parses YAML file into Dictionary of keys/values.
        /// </summary>
        /// <param name="path">Name of YAML file</param>
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
                line = TrimWhiteSpaceChars(line);

                // Check whether to ignore line
                if (IsIgnoreLine(line))
                {
                    continue;
                }
                else if (IsSequenceEntry(line))
                {
                    // Check sequence started/expected
                    if (currentSequence != null)
                    {
                        // Remove sequence char and quotes around directories
                        line = TrimSequenceChars(line);

                        // Add to sequence
                        currentSequence.Add(line);
                    }
                    else
                    {
                        // No sequence started
                        throw new FormatException($"YAML sequence entry without key definition: {line}");
                    }
                }
                else if (TryGetKeyValue(line, out string key, out string val))
                {
                    // Check whether value was found
                    if (string.IsNullOrEmpty(val))
                    {
                        // Start new sequence
                        currentKey = key;
                        currentSequence = new List<string>();

                        // Add to dictionary
                        keyValues.Add(currentKey, currentSequence);
                    }
                    else
                    {
                        // New key/value pair
                        keyValues.Add(key, val);

                        // End any current sequence
                        currentKey = null;
                        currentSequence = null;
                    }
                }
                else
                {
                    // Not expecting this type of entry
                    throw new NotSupportedException($"YAML entry not supported: {line}");
                }
            }

            return keyValues;
        }

        /// <summary>
        /// Trims unwanted whitespace/line chars etc
        /// </summary>
        public static string TrimWhiteSpaceChars(string s)
        {
            return s.Trim(' ', '\n', '\r');
        }

        /// <summary>
        /// Trims sequence char and quotes
        /// </summary>
        public static string TrimSequenceChars(string s)
        {
            return s.TrimStart('-').Trim(' ', '\"', '\'');
        }

        /// <summary>
        /// Checks for empty line/doc start/doc end/comment
        /// </summary>
        public static bool IsIgnoreLine(string line)
        {
            return string.IsNullOrEmpty(line) || line.StartsWith("---") || line.StartsWith("...") || line.StartsWith("#");
        }

        /// <summary>
        /// Determines whether line is a sequence entry.
        /// </summary>
        public static bool IsSequenceEntry(string line)
        {
            return line.StartsWith('-') && !line.StartsWith("--");
        }

        /// <summary>
        /// Attempts to find key/value delim within line.
        /// </summary>
        /// <param name="line">string to parse</param>
        /// <param name="key">key</param>
        /// <param name="val">value (if exists - may be start of sequence)</param>
        /// <returns>true if line contains key/value delim</returns>
        public static bool TryGetKeyValue(string line, out string key, out string val)
        {
            // Check for key/value
            string[] tmp = line.Split(':');

            // Check whether delim was found
            if (tmp.Length > 1)
            {
                // Trim values either side of ':'
                // Convert keys to lowercase for comparison.
                key = tmp[0].TrimEnd().ToLower();
                val = tmp[1].TrimStart();

                return true;
            }
            else
            {
                // Responsible for initializing
                key = "";
                val = "";

                return false;
            }
        }
    }
}
