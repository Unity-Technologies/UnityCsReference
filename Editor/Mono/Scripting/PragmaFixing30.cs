// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor.Scripting
{
    internal class PragmaFixing30
    {
        [RequiredByNativeCode]
        static void FixJavaScriptPragmas()
        {
            string[] filesToFix = CollectBadFiles();
            if (filesToFix.Length == 0)
                return;

            if (!InternalEditorUtility.inBatchMode)
                PragmaFixingWindow.ShowWindow(filesToFix);
            else
                FixFiles(filesToFix);
        }

        public static void FixFiles(string[] filesToFix)
        {
            foreach (string f in filesToFix)
            {
                try
                {
                    FixPragmasInFile(f);
                }
                catch (Exception ex)
                {
                    Debug.LogError("Failed to fix pragmas in file '" + f + "'.\n" + ex.Message);
                }
            }
        }

        static bool FileNeedsPragmaFixing(string fileName)
        {
            return CheckOrFixPragmas(fileName, true);
        }

        static void FixPragmasInFile(string fileName)
        {
            CheckOrFixPragmas(fileName, false);
        }

        static bool CheckOrFixPragmas(string fileName, bool onlyCheck)
        {
            string oldText = File.ReadAllText(fileName);
            StringBuilder text = new StringBuilder(oldText);

            LooseComments(text);

            Match strictMatch = PragmaMatch(text, "strict");

            if (!strictMatch.Success)
                return false;

            bool hasDowncast = PragmaMatch(text, "downcast").Success;
            bool hasImplicit = PragmaMatch(text, "implicit").Success;

            if (hasDowncast && hasImplicit)
                return false;

            if (!onlyCheck)
                DoFixPragmasInFile(fileName, oldText, strictMatch.Index + strictMatch.Length, hasDowncast, hasImplicit);

            return true;
        }

        static void DoFixPragmasInFile(string fileName, string oldText, int fixPos, bool hasDowncast, bool hasImplicit)
        {
            string textToAdd = string.Empty;
            string lineEndings = HasWinLineEndings(oldText) ? "\r\n" : "\n";

            if (!hasImplicit)
                textToAdd += lineEndings + "#pragma implicit";
            if (!hasDowncast)
                textToAdd += lineEndings + "#pragma downcast";

            File.WriteAllText(fileName, oldText.Insert(fixPos, textToAdd));
        }

        static bool HasWinLineEndings(string text)
        {
            return text.IndexOf("\r\n") != -1;
        }

        static IEnumerable<string> SearchRecursive(string dir, string mask)
        {
            foreach (string d in Directory.GetDirectories(dir))
                foreach (string f in SearchRecursive(d, mask))
                    yield return f;
            foreach (string f in Directory.GetFiles(dir, mask))
                yield return f;
        }

        static void LooseComments(StringBuilder sb)
        {
            // TODO: better comment ignoring? this one sort of does the job, it handles //
            // and if it's in multiline comments, our added lines will end up commented as well
            Regex r = new Regex("//");
            foreach (Match m in r.Matches(sb.ToString()))
            {
                int pos = m.Index;
                while (pos < sb.Length && sb[pos] != '\n' && sb[pos] != '\r')
                    sb[pos++] = ' ';
            }
        }

        static Match PragmaMatch(StringBuilder sb, string pragma)
        {
            // unity java script, like regex, treats new line as space character as well
            return new Regex(@"#\s*pragma\s*" + pragma).Match(sb.ToString());
        }

        static string[] CollectBadFiles()
        {
            List<string> filesToFix = new List<string>();

            foreach (string f in SearchRecursive(Path.Combine(Directory.GetCurrentDirectory(), "Assets"), "*.js"))
            {
                try
                {
                    if (FileNeedsPragmaFixing(f))
                        filesToFix.Add(f);
                }
                catch (Exception ex)
                {
                    Debug.LogError("Failed to fix pragmas in file '" + f + "'.\n" + ex.Message);
                }
            }

            return filesToFix.ToArray();
        }
    }
}
