// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Security.Cryptography;
using System.Text;
using Unity.CodeEditor;
using UnityEditorInternal;

namespace UnityEditor
{
    partial class SyncVS
    {
        public static void SyncSolution()
        {
            // Ensure that the mono islands are up-to-date
            AssetDatabase.Refresh();

            // TODO: Rider and possibly other code editors, use reflection to call this method.
            // To avoid conflicts and null reference exception, this is left as a dummy method.
            Unity.CodeEditor.CodeEditor.Editor.Current.SyncAll();

            #pragma warning disable 618
            if (ScriptEditorUtility.GetScriptEditorFromPath(CodeEditor.CurrentEditorInstallation) != ScriptEditorUtility.ScriptEditor.Other)
            {
                Synchronizer.Sync();
            }
        }
    }

    namespace VisualStudioIntegration
    {
        public static class SolutionGuidGenerator
        {
            public static string GuidForProject(string projectName)
            {
                return ComputeGuidHashFor(projectName + "salt");
            }

            public static string GuidForSolution(string projectName, string sourceFileExtension)
            {
                if (sourceFileExtension.ToLower() == "cs")
                    // GUID for a C# class library: http://www.codeproject.com/Reference/720512/List-of-Visual-Studio-Project-Type-GUIDs
                    return "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC";
                return ComputeGuidHashFor(projectName);
            }

            private static string ComputeGuidHashFor(string input)
            {
                var hash = MD5.Create().ComputeHash(Encoding.Default.GetBytes(input));
                return HashAsGuid(HashToString(hash));
            }

            private static string HashAsGuid(string hash)
            {
                var guid = hash.Substring(0, 8) + "-" + hash.Substring(8, 4) + "-" + hash.Substring(12, 4) + "-" + hash.Substring(16, 4) + "-" + hash.Substring(20, 12);
                return guid.ToUpper();
            }

            private static string HashToString(byte[] bs)
            {
                var sb = new StringBuilder();
                foreach (byte b in bs)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
