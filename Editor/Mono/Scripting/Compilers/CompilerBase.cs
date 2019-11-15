// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.CompilationPipeline.Common;
using UnityEngine;

namespace UnityEditor.Scripting.Compilers
{
    internal abstract class CompilerBase : IDisposable
    {
        public abstract string Version { get; }

        public abstract void Dispose();
        public abstract void WaitForCompilationToFinish();
        public abstract void BeginCompiling(AssemblyInfo assemblyInfo, string[] responseFiles, OperatingSystemFamily operatingSystemFamily, string[] systemReferenceDirectories);
        public abstract bool Poll();
        public abstract UnityEditor.Compilation.CompilerMessage[] GetCompilerMessages();
    }
}
