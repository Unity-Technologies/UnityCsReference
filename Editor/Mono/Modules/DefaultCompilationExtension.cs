// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Mono.Cecil;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine;

namespace UnityEditor.Modules
{
    internal class DefaultCompilationExtension
        : ICompilationExtension
    {
        public virtual string[] GetCompilerExtraAssemblyPaths(bool isEditor, string assemblyPathName)
        {
            return new string[] {};
        }

        public virtual IEnumerable<string> GetWindowsMetadataReferences()
        {
            return new string[0];
        }

        public virtual IEnumerable<string> GetAdditionalAssemblyReferences()
        {
            return new string[0];
        }

        public virtual IEnumerable<string> GetAdditionalDefines()
        {
            return new string[0];
        }

        public virtual IEnumerable<string> GetAdditionalEditorDefines()
        {
            return new string[0];
        }

        public virtual IEnumerable<string> GetAdditionalSourceFiles()
        {
            return new string[0];
        }
    }
}
