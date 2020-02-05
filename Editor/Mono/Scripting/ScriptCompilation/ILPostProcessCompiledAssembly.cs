// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine.Profiling;

internal class ILPostProcessCompiledAssembly : ICompiledAssembly
{
    readonly string m_AssemblyFilename;
    readonly string m_OutputPath;
    InMemoryAssembly m_InMemoryAssembly;

    public ILPostProcessCompiledAssembly(ScriptAssembly scriptAssembly, string outputPath)
    {
        m_AssemblyFilename = scriptAssembly.Filename;
        Name = Path.GetFileNameWithoutExtension(m_AssemblyFilename);
        References = scriptAssembly.GetAllReferences();
        Defines = scriptAssembly.Defines;

        m_OutputPath = outputPath;
    }

    public ILPostProcessCompiledAssembly(EditorBuildRules.TargetAssembly targetAssembly, string outputPath)
    {
        m_AssemblyFilename = targetAssembly.Filename;

        Name = Path.GetFileNameWithoutExtension(m_AssemblyFilename);

        var precompiledAssemblyReferences = targetAssembly.ExplicitPrecompiledReferences;
        var targetAssemblyReferences = targetAssembly.References.Select(a => a.FullPath(outputPath));

        References = precompiledAssemblyReferences.Concat(targetAssemblyReferences).ToArray();
        Defines = targetAssembly.Defines;

        m_OutputPath = outputPath;
    }

    private InMemoryAssembly CreateOrGetInMemoryAssembly()
    {
        if (m_InMemoryAssembly != null)
        {
            return m_InMemoryAssembly;
        }

        byte[] peData = File.ReadAllBytes(Path.Combine(m_OutputPath, m_AssemblyFilename));

        var pdbFileName = Path.GetFileNameWithoutExtension(m_AssemblyFilename) + ".pdb";
        byte[] pdbData = File.ReadAllBytes(Path.Combine(m_OutputPath, pdbFileName));

        m_InMemoryAssembly = new InMemoryAssembly(peData, pdbData);
        return m_InMemoryAssembly;
    }

    public InMemoryAssembly InMemoryAssembly
    {
        get { return CreateOrGetInMemoryAssembly(); }
        set { m_InMemoryAssembly = value; }
    }

    public string Name { get; set; }
    public string[] References { get; set; }
    public string[] Defines { get; private set; }

    public void WriteAssembly()
    {
        Profiler.BeginSample("ILPostProcessCompiledAssembly.WriteAssembly");

        if (m_InMemoryAssembly == null)
        {
            throw new ArgumentException("InMemoryAssembly has never been accessed or modified");
        }

        Profiler.BeginSample("ILPostProcessCompiledAssembly.WriteAssembly");

        var assemblyPath = Path.Combine(m_OutputPath, m_AssemblyFilename);
        var pdbFileName = Path.GetFileNameWithoutExtension(m_AssemblyFilename) + ".pdb";
        var pdbPath = Path.Combine(m_OutputPath, pdbFileName);

        File.WriteAllBytes(assemblyPath, InMemoryAssembly.PeData);
        File.WriteAllBytes(pdbPath, InMemoryAssembly.PdbData);

        Profiler.EndSample();
    }
}
