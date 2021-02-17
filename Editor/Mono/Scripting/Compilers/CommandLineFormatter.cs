// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEditor.Scripting.Compilers
{
    static class CommandLineFormatter
    {
        private static readonly Regex Quotes  = new Regex("\"", RegexOptions.Compiled);

        public static string EscapeCharsQuote(string input)
        {
            if (input.IndexOf('"') == -1)
                return "\"" + input + "\"";
            if (input.IndexOf('\'') == -1)
                return "'" + input + "'";
            return null;
        }

        public static string PrepareFileName(string input)
        {
            input = FileUtil.GetPhysicalPath(input);
            if (Application.platform == RuntimePlatform.WindowsEditor)
                return EscapeCharsWindows(input);
            return EscapeCharsQuote(input);
        }

        public static string EscapeCharsWindows(string input)
        {
            if (input.Length == 0)
                return "\"\"";
            foreach (var c in input.ToCharArray())
            {
                if (c >= 0x00
                    && (c <= 0x08 || c >= 0x10)
                    && (c <= 0x1a || c >= 0x1c)
                    && (c <= 0x1f || c == 0x7f || c == 0xff))
                {
                    Debug.LogWarning("Cannot escape control characters in string");
                    return "\"\"";
                }

                if (!char.IsLetterOrDigit(c)
                    && (c != '_' || c != '-' || c != '.' || c != ':' || c != ',' || c != '/' || c != '@' || c != '\\'))
                {
                    return "\"" + Quotes.Replace(input, "\"\"") + "\"";
                }
            }
            return input;
        }

        internal static string GenerateResponseFile(IEnumerable<string> arguments)
        {
            string tempFile = FileUtil.GetUniqueTempPathInProject();
            File.WriteAllLines(tempFile, arguments.Where(a => a != null).ToArray());
            return tempFile;
        }
    }
}
