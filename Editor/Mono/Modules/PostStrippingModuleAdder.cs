// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditorInternal;
namespace UnityEditor.Modules;

internal class PostStrippingModuleAdder : IPostStrippingModuleAdder
{
    readonly LinkerToEditorData m_targetData;

    public PostStrippingModuleAdder(LinkerToEditorData targetData)
    {
        m_targetData = targetData;
    }

    public void AddModule(string moduleName)
    {
        if (IsModuleIncluded(moduleName))
            return;

        // Check that the module has no managed component
        var coreModulePath = InternalEditorUtility.GetEngineCoreModuleAssemblyPath();
        var moduleAssemblyPath = Path.Combine(Path.GetDirectoryName(coreModulePath), $"UnityEngine.{moduleName}Module.dll");

        //a managed assembly is still getting generated for this module, check if the assembly contains no types
        if (File.Exists(moduleAssemblyPath))
        {
            var assembly = System.Reflection.Assembly.LoadFrom(moduleAssemblyPath);
            var types = assembly.GetTypes();
            if (types.Length > 0)
                throw new ArgumentException($"Cannot add {moduleName} after UnityLinker has run because it has a non-empty managed assembly. Types found: {string.Join<Type>(", ", types)}.");
        }

        // Check that all the module's dependencies are already on the list
        var moduleDependencyNames = ModuleMetadata.GetModuleDependencies(moduleName);
        var moduleDependencies = new List<LinkerToEditorData.ReportData.Dependency>();

        var missingDependencyNames = new List<string>();
        foreach (var dependency in moduleDependencyNames)
        {
            if (!IsModuleIncluded(dependency))
            {
                missingDependencyNames.Add(dependency);

                //no need to process this entry further as it is missing from the included module list
                continue;
            }

            moduleDependencies.Add(new LinkerToEditorData.ReportData.Dependency
            {
                name = dependency,
                dependencyType = LinkerToEditorData.ReportData.DependencyType.Custom,
                scenes = Array.Empty<string>(),
            });
        }

        if (missingDependencyNames.Count > 0)
            throw new ArgumentException(
                $"Cannot add module {moduleName} because it has dependencies on other modules that are not present: {string.Join(", ", missingDependencyNames)}");

        m_targetData.report.modules.Add(new LinkerToEditorData.ReportData.Module
        {
            name = moduleName,
            dependencies = moduleDependencies
        });
    }

    public bool IsModuleIncluded(string moduleName) => m_targetData.report.modules.Find(m => m.name == moduleName) != null;
}
