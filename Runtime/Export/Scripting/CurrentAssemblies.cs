// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;


namespace UnityEngine.Assemblies;

/// <summary>
/// Provides utility methods to enumerate assemblies loaded and managed by Unity.
/// </summary>
[VisibleToOtherModules]
public static partial class CurrentAssemblies
{

    /// <summary>
    /// Gets the assemblies that have been loaded by Unity into the current execution context.
    /// </summary>
    /// <returns>A collection of assemblies.</returns>
    /// <remarks>
    /// Use CurrentAssemblies.GetLoadedAssemblies() to get a list of assemblies which are loaded and can be used for reflection purposes.
    /// Unity uses AssemblyLoadContext mechanism for code reload. This means that assemblies can be loaded and unloaded at runtime.
    /// AppDomain.CurrentDomain.GetAssemblies() returns a list of all loaded assemblies which may include assemblies in the unloaded state and
    /// lead to exceptions and logical errors in the Editor code.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeReloadSafety", "UAC0006:AppDomain usage", Justification = "Used as fallback for IL2CPP")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeReloadSafety", "UAC0005:AppDomain.GetAssemblies()", Justification = "Used as fallback for IL2CPP")]
    [VisibleToOtherModules]
    public static IReadOnlyList<Assembly> GetLoadedAssemblies()
    {

        // Fallback to AppDomain.CurrentDomain.GetAssemblies() if we are not running in ALC mode.
        var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        return allAssemblies;
    }

    /// <summary>
    /// Use to load an assembly into the current context.
    /// </summary>
    /// <param name="assemblyPath">Path to assembly</param>
    /// <returns>Loaded assembly</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeReloadSafety", "UAC0020:Assembly.Load usage", Justification = "il2cpp fallback")]
    public static Assembly LoadFromPath(string assemblyPath)
    {
        if (!Path.IsPathFullyQualified(assemblyPath))
        {
            throw new ArgumentException($"Assembly path must be fully qualified", nameof(assemblyPath));
        }


        return Assembly.LoadFrom(assemblyPath);
    }

    /// <summary>
    /// Use to load an assembly into the current context.
    /// </summary>
    /// <param name="rawAssembly">Binary assembly data</param>
    /// <returns>Loaded assembly</returns>
    public static Assembly LoadFromBytes(byte[] rawAssembly)
    {
        return LoadFromBytes(rawAssembly, null);
    }

    /// <summary>
    /// Use to load an assembly into the current context.
    /// </summary>
    /// <param name="rawAssembly">Binary assembly data</param>
    /// <param name="rawSymbolStore">Binary assembly symbols data</param>
    /// <returns>Loaded assembly</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeReloadSafety", "UAC0020:Assembly.Load usage", Justification = "il2cpp fallback")]
    public static Assembly LoadFromBytes(byte[] rawAssembly, byte[] rawSymbolStore)
    {
        if (rawAssembly == null)
            throw new ArgumentNullException(nameof(rawAssembly));

        if (rawAssembly.Length == 0)
            throw new BadImageFormatException("Empty raw assembly byte array");

        if (rawSymbolStore != null && rawSymbolStore.Length == 0)
            throw new BadImageFormatException("Empty raw assembly symbols byte array");


        var assemblyName = GetAssemblyNameFromBytes(rawAssembly);

        if (TryGetLoadedAssembly(assemblyName, out var loadedAssembly))
        {
            return loadedAssembly;
        }

        return Assembly.Load(rawAssembly, rawSymbolStore);
    }

    // This code is a copy of AssemblyMetaData.GetAssemblyNameFromStream,
    // because we don't (yet, at least) have access to that from here.
    private static AssemblyName GetAssemblyNameFromBytes(byte[] rawAssemblyBytes)
    {
        try
        {
            using var rawAssemblyStream = new MemoryStream(rawAssemblyBytes);
            using var peReader = new PEReader(rawAssemblyStream);

            var metadataReader = peReader.GetMetadataReader(MetadataReaderOptions.None);

            return metadataReader.GetAssemblyDefinition().GetAssemblyName();
        }
        catch (InvalidOperationException ex)
        {
            throw new BadImageFormatException(ex.Message, ex);
        }
    }

    private static bool TryGetLoadedAssembly(AssemblyName assemblyName, out Assembly assembly)
    {
        var loadedAssemblies = GetLoadedAssemblies();
        foreach (var loadedAssembly in loadedAssemblies)
        {
            if (loadedAssembly.GetName().Name == assemblyName.Name)
            {
                assembly = loadedAssembly;
                return true;
            }
        }

        assembly = null;
        return false;
    }
}
