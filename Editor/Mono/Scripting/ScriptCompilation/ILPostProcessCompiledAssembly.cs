// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using UnityEditor.Scripting.ScriptCompilation;

internal class ILPostProcessCompiledAssembly : ICompiledAssembly
{
    readonly ScriptAssembly m_ScriptAssembly;
    readonly string m_OutputPath;
    readonly FindReferences m_FindReferences;
    InMemoryAssembly m_InMemoryAssembly;

    public ILPostProcessCompiledAssembly(ScriptAssembly scriptAssembly, string outputPath, FindReferences findReferences)
    {
        m_ScriptAssembly = scriptAssembly;
        Name = Path.GetFileNameWithoutExtension(scriptAssembly.Filename);
        References = scriptAssembly.GetAllReferences().Select(Path.GetFileName).ToArray();

        m_OutputPath = outputPath;
        m_FindReferences = findReferences;
    }

    private InMemoryAssembly CreateOrGetInMemoryAssembly()
    {
        if (m_InMemoryAssembly != null)
        {
            return m_InMemoryAssembly;
        }

        byte[] peData = File.ReadAllBytes(Path.Combine(m_OutputPath, m_ScriptAssembly.Filename));

        var pdbFileName = Path.GetFileNameWithoutExtension(m_ScriptAssembly.Filename) + ".pdb";
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

    public ReferenceQueryResult HasReferences(ReferenceQueryInput input)
    {
        if (input.References == null)
        {
            throw new ArgumentNullException(nameof(input.References));
        }

        HashSet<string> result = m_FindReferences.Execute(m_ScriptAssembly.Filename, input.References, (FindReferencesQueryOptions)input.Options);
        var found = new bool[input.References.Length];
        var isAllReferencesFound = result.Any();
        for (int i = 0; i < input.References.Length; i++)
        {
            found[i] = result.Contains(input.References[i]);
            isAllReferencesFound &= found[i];
        }

        return new ReferenceQueryResult(found, isAllReferencesFound);
    }

    public bool HasReference(string reference, ReferencesQueryOptions options = ReferencesQueryOptions.Direct)
    {
        if (string.IsNullOrEmpty(reference))
        {
            throw new ArgumentException(nameof(reference));
        }

        var hasReferencesResult = HasReferences(new ReferenceQueryInput()
        {
            References = new string[] { reference },
            Options = options
        });
        return hasReferencesResult.HasAllReferences && hasReferencesResult.HasReference.All(x => x);
    }

    public void WriteAssembly()
    {
        if (m_InMemoryAssembly == null)
        {
            throw new ArgumentException("InMemoryAssembly has never been accessed or modified");
        }

        var assemblyPath = Path.Combine(m_OutputPath, m_ScriptAssembly.Filename);
        var pdbFileName = Path.GetFileNameWithoutExtension(m_ScriptAssembly.Filename) + ".pdb";
        var pdbPath = Path.Combine(m_OutputPath, pdbFileName);

        File.WriteAllBytes(assemblyPath, InMemoryAssembly.PeData);
        File.WriteAllBytes(pdbPath, InMemoryAssembly.PdbData);
    }
}
