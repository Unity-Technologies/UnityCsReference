// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Collections.Generic;

namespace UnityEditor.Utils
{
    internal static class Paths
    {
        public static string Combine(params string[] components)
        {
            if (components.Length < 1)
                throw new ArgumentException("At least one component must be provided!");

            string path = components[0];
            for (int i = 1; i < components.Length; i++)
                path = Path.Combine(path, components[i]);
            return path;
        }

        public static string[] Split(string path)
        {
            List<string> result = new List<string>(path.Split(Path.DirectorySeparatorChar));

            for (int i = 0; i < result.Count;)
            {
                result[i] = result[i].Trim();
                if (result[i].Equals(""))
                {
                    result.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }

            return result.ToArray();
        }

        public static string GetFileOrFolderName(string path)
        {
            string name;

            if (File.Exists(path))
            {
                name = Path.GetFileName(path);
            }
            else if (Directory.Exists(path))
            {
                string[] pathSplit = Split(path);
                name = pathSplit[pathSplit.Length - 1];
            }
            else
            {
                throw new ArgumentException("Target '" + path + "' does not exist.");
            }

            return name;
        }

        public static string CreateTempDirectory()
        {
            for (int i = 0; i < 32; ++i)
            {
                string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

                if (File.Exists(tempDirectory) || Directory.Exists(tempDirectory))
                    continue;

                Directory.CreateDirectory(tempDirectory);

                return tempDirectory;
            }
            throw new IOException("CreateTempDirectory failed after 32 tries");
        }

        public static string NormalizePath(this string path)
        {
            if (Path.DirectorySeparatorChar == '\\')
                return path.Replace('/', Path.DirectorySeparatorChar);
            return path.Replace('\\', Path.DirectorySeparatorChar);
        }

        public static string ConvertSeparatorsToUnity(this string path)
        {
            return path.Replace('\\', '/');
        }

        public static string UnifyDirectorySeparator(string path)
        {
            return path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        }

        public static bool AreEqual(string pathA, string pathB, bool ignoreCase)
        {
            if (pathA == "" && pathB == "")
                return true;

            if (String.IsNullOrEmpty(pathA) || String.IsNullOrEmpty(pathB))
            {
                // GetFullPath will throw otherwise.
                return false;
            }

            return string.Compare(Path.GetFullPath(pathA), Path.GetFullPath(pathB), ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) == 0;
        }

        public static bool IsValidAssetPathWithErrorLogging(string assetPath, string requiredExtensionWithDot)
        {
            string errorMsg;
            if (!IsValidAssetPath(assetPath, requiredExtensionWithDot, out errorMsg))
            {
                UnityEngine.Debug.LogError(errorMsg);
                return false;
            }
            return true;
        }

        public static bool IsValidAssetPath(string assetPath)
        {
            const string requiredExtension = null;
            return IsValidAssetPath(assetPath, requiredExtension);
        }

        public static bool IsValidAssetPath(string assetPath, string requiredExtensionWithDot)
        {
            string errorMsg = null;
            return CheckIfAssetPathIsValid(assetPath, requiredExtensionWithDot, ref errorMsg);
        }

        public static bool IsValidAssetPath(string assetPath, string requiredExtensionWithDot, out string errorMsg)
        {
            errorMsg = string.Empty;
            return CheckIfAssetPathIsValid(assetPath, requiredExtensionWithDot, ref errorMsg);
        }

        // If an error message is wanted then input an empty string for 'errorMsg'. If 'errorMsg' is null then no error string is allocated.
        static bool CheckIfAssetPathIsValid(string assetPath, string requiredExtensionWithDot, ref string errorMsg)
        {
            try
            {
                if (string.IsNullOrEmpty(assetPath))
                {
                    if (errorMsg != null)
                        SetFullErrorMessage("Asset path is empty", assetPath, ref errorMsg);
                    return false;
                }

                string fileName = Path.GetFileName(assetPath); // Will throw exception if the filePath is not valid

                if (fileName.StartsWith("."))
                {
                    if (errorMsg != null)
                        SetFullErrorMessage("Do not prefix asset name with '.'", assetPath, ref errorMsg);
                    return false;
                }

                if (fileName.StartsWith(" "))
                {
                    if (errorMsg != null)
                        SetFullErrorMessage("Do not prefix asset name with white space", assetPath, ref errorMsg);
                    return false;
                }

                if (!string.IsNullOrEmpty(requiredExtensionWithDot))
                {
                    string extension = Path.GetExtension(assetPath);
                    if (!String.Equals(extension, requiredExtensionWithDot, StringComparison.OrdinalIgnoreCase))
                    {
                        if (errorMsg != null)
                            SetFullErrorMessage(string.Format("Incorrect extension. Required extension is: '{0}'", requiredExtensionWithDot), assetPath, ref errorMsg);
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                if (errorMsg != null)
                    SetFullErrorMessage(e.Message, assetPath, ref errorMsg);
                return false;
            }
            return true;
        }

        static void SetFullErrorMessage(string error, string assetPath, ref string errorMsg)
        {
            errorMsg = string.Format("Asset path error: '{0}' is not valid: {1}", ToLiteral(assetPath), error);
        }

        /// Escapes a string so that escape sequences of ASCII and unicode characters are shown. Taken from http://stackoverflow.com/a/14087738
        static string ToLiteral(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var literal = new System.Text.StringBuilder(input.Length + 2);
            foreach (var c in input)
            {
                switch (c)
                {
                    case '\'': literal.Append(@"\'"); break;
                    case '\"': literal.Append("\\\""); break;
                    case '\\': literal.Append(@"\\"); break;
                    case '\0': literal.Append(@"\0"); break;
                    case '\a': literal.Append(@"\a"); break;
                    case '\b': literal.Append(@"\b"); break;
                    case '\f': literal.Append(@"\f"); break;
                    case '\n': literal.Append(@"\n"); break;
                    case '\r': literal.Append(@"\r"); break;
                    case '\t': literal.Append(@"\t"); break;
                    case '\v': literal.Append(@"\v"); break;
                    default:
                        // ASCII printable character
                        if (c >= 0x20 && c <= 0x7e)
                        {
                            literal.Append(c);
                        }
                        // As UTF16 escaped character
                        else
                        {
                            literal.Append(@"\u");
                            literal.Append(((int)c).ToString("x4"));
                        }
                        break;
                }
            }
            return literal.ToString();
        }
    }
}
