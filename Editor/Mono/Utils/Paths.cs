// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace UnityEditor.Utils
{
    internal static class Paths
    {
        static char[] invalidFilenameChars;

        static Paths()
        {
            var uniqueChars = new HashSet<char>(Path.GetInvalidFileNameChars());
            uniqueChars.Add(Path.DirectorySeparatorChar);
            uniqueChars.Add(Path.AltDirectorySeparatorChar);
            invalidFilenameChars = uniqueChars.ToArray();
        }

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
            throw new IOException("CreateTempDirectory failed");
        }

        public static string NormalizePath(this string path)
        {
            if (Path.DirectorySeparatorChar == '\\')
                return path.Replace('/', Path.DirectorySeparatorChar);
            return path.Replace('\\', Path.DirectorySeparatorChar);
        }

        public static string ConvertSeparatorsToWindows(this string path)
        {
            return path.Replace('/', '\\');
        }

        public static string ConvertSeparatorsToUnity(this string path)
        {
            return path.Replace('\\', '/');
        }

        public static string TrimTrailingSlashes(this string path)
        {
            return path.TrimEnd(new[] { '/', '\\' });
        }

        public static string SkipPathPrefix(string path, string prefix)
        {
            if (path.StartsWith(prefix))
                return path.Substring(prefix.Length + 1);
            return path;
        }

        public static string UnifyDirectorySeparator(string path)
        {
            return path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        }

        public static bool AreEqual(string pathA, string pathB, bool ignoreCase)
        {
            if (pathA == "" && pathB == "" ||
                pathA == null && pathB == null)
                return true;

            if (String.IsNullOrEmpty(pathA) || String.IsNullOrEmpty(pathB))
            {
                // GetFullPath will throw otherwise.
                return false;
            }

            return string.Compare(Path.GetFullPath(pathA), Path.GetFullPath(pathB), ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) == 0;
        }

        public static bool CheckValidAssetPathAndThatDirectoryExists(string assetFilePath, string requiredExtensionWithDot)
        {
            if (!IsValidAssetPathWithErrorLogging(assetFilePath, requiredExtensionWithDot))
                return false;

            string dir = Path.GetDirectoryName(assetFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                UnityEngine.Debug.LogError("Parent directory must exist before creating asset at: " + assetFilePath);
                return false;
            }

            return true;
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
            string fileName;
            try
            {
                if (string.IsNullOrEmpty(assetPath))
                {
                    if (errorMsg != null)
                        SetFullErrorMessage("Asset path is empty", assetPath, ref errorMsg);
                    return false;
                }

                fileName = Path.GetFileName(assetPath); // Will throw an ArgumentException if the path contains one or more of the invalid characters defined in GetInvalidPathChars
            }
            catch (Exception e)
            {
                if (errorMsg != null)
                    SetFullErrorMessage(e.Message, assetPath, ref errorMsg);
                return false;
            }

            return CheckIfFilenameIsValid(fileName, "asset name", requiredExtensionWithDot, ref errorMsg);
        }

        public static bool IsValidFilename(string assetPath)
        {
            const string requiredExtension = null;
            return IsValidFilename(assetPath, requiredExtension);
        }

        public static bool IsValidFilename(string assetPath, string requiredExtensionWithDot)
        {
            string errorMsg = null;
            return CheckIfFilenameIsValid(assetPath, "filename", requiredExtensionWithDot, ref errorMsg);
        }

        public static bool IsValidFilename(string assetPath, string requiredExtensionWithDot, out string errorMsg)
        {
            errorMsg = string.Empty;
            return CheckIfFilenameIsValid(assetPath, "filename", requiredExtensionWithDot, ref errorMsg);
        }

        static bool CheckIfFilenameIsValid(string fileName, string argumentName, string requiredExtensionWithDot, ref string errorMsg)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    if (errorMsg != null)
                        SetFullErrorMessage(string.Format("The {0} is empty", argumentName), fileName, ref errorMsg);
                    return false;
                }

                if (fileName.IndexOfAny(invalidFilenameChars) >= 0)
                {
                    if (errorMsg != null)
                        SetFullErrorMessage(string.Format("The {0} contains invalid characters", argumentName), fileName, ref errorMsg);
                    return false;
                }

                if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    if (errorMsg != null)
                        SetFullErrorMessage("Invalid characters in file name: ", fileName, ref errorMsg);
                    return false;
                }

                if (fileName.StartsWith("."))
                {
                    if (errorMsg != null)
                        SetFullErrorMessage(string.Format("Do not prefix {0} with '.'", argumentName), fileName, ref errorMsg);
                    return false;
                }

                if (fileName.StartsWith(" "))
                {
                    if (errorMsg != null)
                        SetFullErrorMessage(string.Format("Do not prefix {0} with white space", argumentName), fileName, ref errorMsg);
                    return false;
                }

                if (!string.IsNullOrEmpty(requiredExtensionWithDot))
                {
                    string extension = Path.GetExtension(fileName);
                    if (!String.Equals(extension, requiredExtensionWithDot, StringComparison.OrdinalIgnoreCase))
                    {
                        if (errorMsg != null)
                            SetFullErrorMessage(string.Format("Incorrect extension. Required extension is: '{0}'", requiredExtensionWithDot), fileName, ref errorMsg);
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                if (errorMsg != null)
                    SetFullErrorMessage(e.Message, fileName, ref errorMsg);
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

        public static string MakeValidFileName(string unsafeFileName)
        {
            var normalizedFileName = unsafeFileName.Trim().Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            // We need to use a TextElementEmumerator in order to support UTF16 characters that may take up more than one char(case 1169358)
            TextElementEnumerator charEnum = StringInfo.GetTextElementEnumerator(normalizedFileName);
            while (charEnum.MoveNext())
            {
                var c = charEnum.GetTextElement();
                if (c.Length == 1 && invalidFilenameChars.Contains(c[0]))
                    continue;
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c, 0);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                    stringBuilder.Append(c);
            }
            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        static readonly char[] PathSeparators = new[] { '\\', '/' };

        public static string MakeRelativePath(string basePath, string filePath)
        {
            var basePathComponents = basePath.Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries);
            var filePathComponents = filePath.Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries);

            int commonParts;

            for (commonParts = 0; commonParts < basePathComponents.Length && commonParts < filePathComponents.Length; commonParts++)
            {
                if (basePathComponents[commonParts].ToLower() != filePathComponents[commonParts].ToLower())
                    break;
            }

            var relativePathParts = new List<string>();
            var parentDirCount = basePathComponents.Length - commonParts;

            for (int i = 0; i < parentDirCount; i++)
                relativePathParts.Add("..");

            for (int i = commonParts; i < filePathComponents.Length; i++)
                relativePathParts.Add(filePathComponents[i]);

            return string.Join("\\", relativePathParts.ToArray());
        }
    }
}
