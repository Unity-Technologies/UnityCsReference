// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Scripting.ScriptCompilation;

using System;
using System.Text;
using System.Security.Cryptography;

namespace UnityEditorInternal.APIUpdaterExtensions
{
    static class APIUpdaterUtils
    {
        public static string CalculateSHA256(this string data)
        {
            using (var hasher = SHA256.Create())
            {
                return BitConverter.ToString(hasher.ComputeHash(ASCIIEncoding.ASCII.GetBytes(data))).Replace("-", "");
            }
        }

        public static bool IsInPackage(this string filePath)
        {
            return EditorCompilationInterface.Instance.IsPathInPackageDirectory(filePath);
        }

        public static bool IsInAssetsFolder(this string filePath)
        {
            return filePath.StartsWith("Assets/", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
