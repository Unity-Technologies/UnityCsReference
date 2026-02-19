// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.Modules
{
    internal class DefaultCompilationExtension
        : ICompilationExtension
    {
        public virtual string[] GetCompilerExtraAssemblyPaths(bool isEditor, string assemblyPathName)
        {
            return Array.Empty<string>();
        }

        public virtual string[] GetWindowsMetadataReferences()
        {
            return Array.Empty<string>();
        }

        public virtual string[] GetAdditionalAssemblyReferences()
        {
            return Array.Empty<string>();
        }

        public virtual string[] GetAdditionalDefines()
        {
            return Array.Empty<string>();
        }

        public virtual string[] GetAdditionalEditorDefines()
        {
            return Array.Empty<string>();
        }

        public virtual string[] GetAdditionalSourceFiles()
        {
            return Array.Empty<string>();
        }
    }
}
