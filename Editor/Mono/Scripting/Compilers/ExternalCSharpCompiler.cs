// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Unity.CompilationPipeline.Common;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine;

namespace UnityEditor.Scripting.Compilers
{
    internal class ExternalCSharpCompiler : ScriptCompilerBase
    {
        private readonly ScriptAssembly m_Assembly;
        private readonly string m_TempOutputDirectory;
        private CompilerBase m_ExternalCompiler;

        public ExternalCSharpCompiler(ScriptAssembly assembly, string tempOutputDirectory)
            : base(assembly, tempOutputDirectory)
        {
            m_Assembly = assembly;
            m_TempOutputDirectory = tempOutputDirectory;
            m_ExternalCompiler = CreateExternalCompilerBase();
        }

        public static bool HasExternalCompiler()
        {
            return GetExternalCompilerType().Any();
        }

        public static string ExternalCompilerName()
        {
            var typesDerivedFrom = GetExternalCompilerType()[0];

            var creationTime = File.GetCreationTime(typesDerivedFrom.Assembly.Location);
            return typesDerivedFrom.Assembly.FullName + creationTime.Ticks;
        }

        private static CompilerBase CreateExternalCompilerBase()
        {
            var typesDerivedFrom = GetExternalCompilerType();
            if (typesDerivedFrom.Count > 1)
            {
                var compilerBaseImplementations = typesDerivedFrom.Select(x => x.FullName).ToArray();
                throw new Exception($"More than 1 external C# compiler found: {string.Join(",", compilerBaseImplementations)}");
            }

            var externalCompilerType = typesDerivedFrom[0];
            Console.WriteLine($"External compiler found: {externalCompilerType.FullName}");
            return (CompilerBase)Activator.CreateInstance(externalCompilerType);
        }

        private static IList<Type> GetExternalCompilerType()
        {
            var typesDerivedFrom = TypeCache.GetTypesDerivedFrom<CompilerBase>();
            return typesDerivedFrom;
        }

        private static AssemblyInfo ScriptAssemblyToAssemblyInfo(ScriptAssembly scriptAssembly, string outputDir)
        {
            return new AssemblyInfo
            {
                Name = scriptAssembly.Filename,
                Files = scriptAssembly.Files.Select(Path.GetFullPath).ToArray(),
                Defines = scriptAssembly.Defines,
                References = scriptAssembly.GetAllReferences().Select(Path.GetFullPath).ToArray(),
                OutputDirectory = Path.GetFullPath(outputDir),
                AllowUnsafeCode = scriptAssembly.CompilerOptions.AllowUnsafeCode,
            };
        }

        public override void Dispose()
        {
            m_ExternalCompiler.Dispose();
        }

        public override void WaitForCompilationToFinish()
        {
            m_ExternalCompiler.WaitForCompilationToFinish();
        }

        public override void BeginCompiling()
        {
            var assemblyInfo = ScriptAssemblyToAssemblyInfo(m_Assembly, m_TempOutputDirectory);
            var systemReferenceDirectories = MonoLibraryHelpers.GetSystemReferenceDirectories(m_Assembly.CompilerOptions.ApiCompatibilityLevel);
            m_ExternalCompiler.BeginCompiling(assemblyInfo, m_Assembly.CompilerOptions.ResponseFiles, SystemInfo.operatingSystemFamily, systemReferenceDirectories);
        }

        public override bool Poll()
        {
            return m_ExternalCompiler.Poll();
        }

        public override CompilerMessage[] GetCompilerMessages()
        {
            return m_ExternalCompiler.GetCompilerMessages()
                .Select(x => new CompilerMessage()
                {
                    column = x.column,
                    file = x.file,
                    line = x.line,
                    message = x.message,
                    type = x.type == Compilation.CompilerMessageType.Error ? CompilerMessageType.Error : CompilerMessageType.Warning,
                    assemblyName = m_Assembly.Filename,
                })
                .ToArray();
        }

        public override ProcessStartInfo GetProcessStartInfo()
        {
            return null;
        }
    }
}
