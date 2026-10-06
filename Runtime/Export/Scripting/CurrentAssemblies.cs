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

using Unity.Scripting.AssemblyManagement;

namespace UnityEngine.Assemblies;

/// <summary>
/// Provides utility methods to enumerate assemblies loaded and managed by Unity.
/// </summary>
[VisibleToOtherModules]
public static partial class CurrentAssemblies
{
    private struct AssemblyLoadContextStateHelper
    {
        public MethodInfo GetAssemblyLoadContextMethod;
        public FieldInfo AssemblyLoadContextStateField;
    }

    [NoAutoStaticsCleanup] //k_AssemblyLoadContextStateHelper can be kept alive accross code reloads
    private static readonly AssemblyLoadContextStateHelper k_AssemblyLoadContextStateHelper = GetAssemblyLoadContextStateHelperImpl();
    private static AssemblyLoadContextStateHelper GetAssemblyLoadContextStateHelperImpl()
    {
        var method = Type.GetType("System.Runtime.Loader.AssemblyLoadContext")?.GetMethod("GetLoadContext", BindingFlags.Static | BindingFlags.Public);
        if (method == null)
            return default;

        var field = method.DeclaringType?.GetField("_state", BindingFlags.Instance | BindingFlags.NonPublic);
        var result = new AssemblyLoadContextStateHelper
        {
            GetAssemblyLoadContextMethod = method,
            AssemblyLoadContextStateField = field
        };
        return result;
    }

    /// <summary>
    /// Validate whether the assembly is loaded from still live AssemblyLoadContext.
    /// On NETCore when AssemblyLoadContexts are being used it is possible that some assemblies are in the "being unloaded" state.
    /// That causes exceptions in code which uses such assemblies and that code should be wrapped in try/catch.
    /// Alternatively we can filter out assemblies which are not in the "live" state and return only valid for iteration assemblies.
    /// This allows to have a filtering boilerplate in a common utility method which can be used when code is used within Unity Editor
    /// (and thus relying on AssemblyLoader infrastructure) and out-of-process (e.g. ILPP).
    /// Since we still use Mono (ENABLE_CORECLR_FIXME) which requires netstandard we can't use AssemblyLoadContext API directly and use reflection.
    /// </summary>
    /// <param name="assembly">Assembly instance to check a liveness state</param>
    /// <returns>True is assembly belongs to live context, false otherwise</returns>
    private static bool IsFromLiveAssemblyLoadContext(Assembly assembly)
    {
        var assemblyLoadContext = k_AssemblyLoadContextStateHelper.GetAssemblyLoadContextMethod.Invoke(null, new object[] { assembly });
        if (assemblyLoadContext == null)
            return true;

        var state = k_AssemblyLoadContextStateHelper.AssemblyLoadContextStateField?.GetValue(assemblyLoadContext);
        var result = 0 == Convert.ToInt32(state);
        return result;
    }

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
        // The code path only relevant when running in ALC mode in Unity Editor or Player.
        if (ICurrentAssemblyLoadContext.Instance != null)
            return ICurrentAssemblyLoadContext.Instance.GetLoadedAssemblies();

        // Fallback to AppDomain.CurrentDomain.GetAssemblies() if we are not running in ALC mode.
        var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        if (k_AssemblyLoadContextStateHelper.GetAssemblyLoadContextMethod == null)
            return allAssemblies;

        // But filter to only return assemblies that are loaded from still live ALC.
        // This is done to be able to use the GetLoadedAssemblies method in out-of-process code such as ILPP.
        var liveAssemblies = new List<Assembly>();
        foreach (var assembly in allAssemblies) // and we don't use LINQ
        {
            if (IsFromLiveAssemblyLoadContext(assembly))
                liveAssemblies.Add(assembly);
        }

        return liveAssemblies;
    }

    /// <summary>
    /// Raised for an assembly Unity has no resolution for, when the load originates from the current contextual
    /// reflection context. A handler returns the assembly to use, or null to decline.
    /// </summary>
    [VisibleToOtherModules]
    internal static event Func<AssemblyName, Assembly> AssemblyResolve
    {
        add => ICurrentAssemblyLoadContext.Instance.AssemblyResolve += value;
        remove => ICurrentAssemblyLoadContext.Instance.AssemblyResolve -= value;
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

        // The code path only relevant when running in ALC mode in Unity Editor or Player.
        if (ICurrentAssemblyLoadContext.Instance != null)
        {
            return ICurrentAssemblyLoadContext.Instance.LoadFromPath(assemblyPath);
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

        // The code path only relevant when running in ALC mode in Unity Editor or Player.
        if (ICurrentAssemblyLoadContext.Instance != null)
        {
            using var assemblyStream = new MemoryStream(rawAssembly);
            if (rawSymbolStore != null)
            {
                using var symbolsStream = new MemoryStream(rawSymbolStore);
                return ICurrentAssemblyLoadContext.Instance.LoadFromStream(assemblyStream, symbolsStream);
            }

            return ICurrentAssemblyLoadContext.Instance.LoadFromStream(assemblyStream, null);
        }

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
