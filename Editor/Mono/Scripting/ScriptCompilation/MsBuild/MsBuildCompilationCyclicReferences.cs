// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Unity.BuildService;

namespace UnityEditor.Scripting.ScriptCompilation.MsBuild;

partial class MsBuildCompilation
{
    const string k_CircularDependencyCode = "MSB4251";
    const string k_GeneratedProjectSuffix = ".gen.csproj";
    const string k_CyclicDependenciesMessage = "One or more cyclic dependencies detected between assemblies: ";

    // MSBuild reports an asmdef reference cycle as MSB4251 over the generated projects; report it in the legacy pipeline's words.
    void TranslateCircularDependencyErrors(BuildResultMessage buildResult)
    {
        foreach (var logMessage in buildResult.GetLogMessages())
        {
            if (logMessage.Code != k_CircularDependencyCode)
                continue;

            var assemblies = GetAssembliesInCircularDependency(logMessage.Message);
            if (assemblies.Count == 0)
                continue;

            logMessage.Message = k_CyclicDependenciesMessage + string.Join(", ", assemblies);
        }
    }

    // The message lists one project per line, each but the last followed by "->", and repeats the first one at the end.
    List<string> GetAssembliesInCircularDependency(string message)
    {
        var assemblies = new List<string>();
        foreach (var line in message.Split('\n'))
        {
            var project = line.Trim();
            if (project.EndsWith("->", StringComparison.Ordinal))
                project = project.Substring(0, project.Length - 2).TrimEnd();
            if (!project.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                continue;

            var fileName = Path.GetFileName(project);
            var assemblyName = fileName.EndsWith(k_GeneratedProjectSuffix, StringComparison.OrdinalIgnoreCase)
                ? fileName.Substring(0, fileName.Length - k_GeneratedProjectSuffix.Length)
                : Path.GetFileNameWithoutExtension(fileName);
            var assembly = TryGetAssemblyDefinitionFilePathFromReference(assemblyName) ?? assemblyName;
            if (!assemblies.Contains(assembly))
                assemblies.Add(assembly);
        }
        return assemblies;
    }
}
