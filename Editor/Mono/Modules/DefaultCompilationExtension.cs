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

        public virtual IEnumerable<string> GetWindowsMetadataReferences()
        {
            return Array.Empty<string>();
        }

        public virtual IEnumerable<string> GetAdditionalAssemblyReferences()
        {
            return Array.Empty<string>();
        }

        public virtual IEnumerable<string> GetAdditionalDefines()
        {
            return Array.Empty<string>();
        }

        public virtual IEnumerable<string> GetAdditionalEditorDefines()
        {
            return Array.Empty<string>();
        }

        public virtual IEnumerable<string> GetAdditionalSourceFiles()
        {
            return Array.Empty<string>();
        }
    }
}
