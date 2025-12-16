// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics;

namespace Unity.ProjectAuditor.Editor.AssemblyUtils
{
    [DebuggerDisplay("{Name}")]
    class AssemblyInfo
    {
        public const string DefaultAssemblyFileName = "Assembly-CSharp.dll";
        public const string DefaultEditorAssemblyFileName = "Assembly-CSharp-Editor.dll";

        public static string DefaultAssemblyName => System.IO.Path.GetFileNameWithoutExtension(DefaultAssemblyFileName);
        public static string DefaultEditorAssemblyName => System.IO.Path.GetFileNameWithoutExtension(DefaultEditorAssemblyFileName);

        public string Name;            // assembly name without extension
        public string Path;            // absolute path
        public string AsmDefPath;
        public string RelativePath;    // asmdef containing folder, relative to the project

        public bool IsTestAssembly;
        public bool? IsEditorAssembly; // optional, not always set
        public bool IsReadOnly;
        public bool IsUnityInternalAssembly;
        public bool IsUnityOwned;      // includes internal precompiled assemblies, but also unity packages
        public string PackageResolvedPath;

        public string GetTypeString()
        {
            var result = "";

            if (IsEditorAssembly.HasValue)
            {
                if (IsEditorAssembly.Value)
                    result = "Editor";
                else
                    result = "Player";
            }

            if (PackageResolvedPath != null)
            {
                if (result.Length > 0)
                    result += " ";
                result += "Package";
            }

            if (IsTestAssembly)
            {
                if (result.Length > 0)
                    result += " ";
                result += "Tests";
            }

            return result;
        }
    }
}
