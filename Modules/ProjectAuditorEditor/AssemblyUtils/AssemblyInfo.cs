// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;

namespace Unity.ProjectAuditor.Editor.AssemblyUtils
{
    class AssemblyInfo
    {
        public const string DefaultAssemblyFileName = "Assembly-CSharp.dll";
        public static string DefaultAssemblyName => System.IO.Path.GetFileNameWithoutExtension(DefaultAssemblyFileName);

        public string Name;            // assembly name without extension
        public string Path;            // absolute path
        public string AsmDefPath;
        public string RelativePath;    // asmdef containing folder, relative to the project

        public bool IsPackageReadOnly;
        public string PackageResolvedPath;
    }
}
