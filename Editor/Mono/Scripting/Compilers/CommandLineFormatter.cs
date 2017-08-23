// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEditor.Scripting.Compilers
{
    static class CommandLineFormatter
    {
        private static readonly Regex UnsafeCharsWindows = new Regex(@"[^A-Za-z0-9_\-\.\:\,\/\@\\]");
        private static readonly Regex UnescapeableChars  = new Regex(@"[\x00-\x08\x10-\x1a\x1c-\x1f\x7f\xff]");
        private static readonly Regex Quotes  = new Regex("\"");

        public static string EscapeCharsQuote(string input)
        {
            if (input.IndexOf('\'') == -1)
                return "'" + input + "'";
            if (input.IndexOf('"') == -1)
                return "\"" + input + "\"";
            return null;
        }

        public static string PrepareFileName(string input)
        {
            input = FileUtil.ResolveSymlinks(input);
            if (Application.platform == RuntimePlatform.WindowsEditor)
                return EscapeCharsWindows(input);
            return EscapeCharsQuote(input);
        }

        public static string EscapeCharsWindows(string input)
        {
            if (input.Length == 0)
                return "\"\"";
            if (UnescapeableChars.IsMatch(input))
            {
                Debug.LogWarning("Cannot escape control characters in string");
                return "\"\"";
            }
            if (UnsafeCharsWindows.IsMatch(input))
            {
                return "\"" + Quotes.Replace(input, "\"\"") + "\"";
            }
            return input;
        }

        internal static string GenerateResponseFile(IEnumerable<string> arguments)
        {
            string tempFile = FileUtil.GetUniqueTempPathInProject();
            using (var writer = new System.IO.StreamWriter(tempFile))
            {
                foreach (var arg in arguments.Where(a => a != null))
                    writer.WriteLine(arg);
            }
            //Debug.Log("ResponseFile: "+System.IO.File.ReadAllText(tempFile));
            return tempFile;
        }
    }
}
