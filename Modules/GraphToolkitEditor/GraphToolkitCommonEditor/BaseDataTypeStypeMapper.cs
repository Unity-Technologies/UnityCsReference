// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolkit.Editor;

partial class BaseDataTypeStyleMapper
{
    public static readonly string k_BuiltInTypeOverrideWarning = "Attempting to override built-in data type style for type {0}. This is not supported and will be ignored.";

    [AutoStaticsCleanupOnCodeReload]
    static readonly Dictionary<Type, (Texture2D icon, Color color)> k_TypeStylesForAllGraphTypes = new();
    [AutoStaticsCleanupOnCodeReload]
    static readonly Dictionary<Type, Dictionary<Type, (Texture2D icon, Color color)>> k_TypeStylesPerGraphType = new();

    [AutoStaticsCleanupOnCodeReload]
    static bool s_Initialized;

    static void EnsureInitialized()
    {
        if (s_Initialized)
            return;
        s_Initialized = true;
        foreach (var type in TypeCache.GetTypesDerivedFrom<BaseDataTypeStyleMapper>())
        {
            if (!type.IsAbstract)
                TryInstantiate(type);
        }
        foreach (var type in TypeCache.GetTypesDerivedFrom<DataTypeStyleMapper>())
        {
            if (!type.IsAbstract)
                TryInstantiate(type);
        }
    }

    static void TryInstantiate(Type type)
    {
        try
        {
            Activator.CreateInstance(type, nonPublic: true);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    internal static class TestAccess
    {
        public static void Reset()
        {
            s_Initialized = false;
            k_TypeStylesForAllGraphTypes.Clear();
            k_TypeStylesPerGraphType.Clear();
        }
    }

    /// <summary>
    /// Registers an icon and color style for a given data type, optionally scoped to specific graph types.
    /// </summary>
    /// <param name="dataType">The type of data to associate with the style.</param>
    /// <param name="icon">The icon representing the data type.</param>
    /// <param name="color">The color representing the data type.</param>
    /// <param name="graphTypes">Array of graph types to scope the style registration. If null or empty, the style applies to all graph types.</param>
    internal static void Register(Type dataType, Texture2D icon, Color color, Type[] graphTypes)
    {
        if (dataType == null)
            throw new ArgumentNullException(nameof(dataType));

        // Prevent overriding built-in types.
        if (CustomizableModelPropertyField.DefaultSupportedTypes.Contains(dataType))
        {
            Debug.LogWarning(string.Format(k_BuiltInTypeOverrideWarning, dataType.Name));
            return;
        }

        // If provided, register only for the specified graph types.
        if (graphTypes is { Length: > 0 })
        {
            foreach (var graphType in graphTypes)
            {
                if (!k_TypeStylesPerGraphType.ContainsKey(graphType))
                    k_TypeStylesPerGraphType.Add(graphType, new Dictionary<Type, (Texture2D icon, Color color)>());

                if (k_TypeStylesPerGraphType.ContainsKey(dataType))
                    Debug.LogWarning("Attempting to override existing data type style for type " + dataType.Name + " in graph type " + graphType.Name);

                k_TypeStylesPerGraphType[graphType][dataType] = (icon, color);
            }

            return;
        }

        // Otherwise, register for all graph types.
        if (k_TypeStylesForAllGraphTypes.ContainsKey(dataType))
            Debug.LogWarning("Attempting to override existing data type style for type " + dataType.Name + " for all graph types.");

        k_TypeStylesForAllGraphTypes[dataType] = (icon, color);
    }

    /// <summary>
    /// Retrieves the icon and color style associated with a specific data type.
    /// </summary>
    /// <param name="dataType">The type of data to get the style for.</param>
    /// <param name="graphType">The graph type to search for a style. If null, returns the style registered for all graph types.</param>
    /// <returns>The icon and color for the specified data type and graph type, if available.</returns>
    internal static (Texture2D icon, Color color)? GetDataTypeStyle(Type dataType, Type graphType = null)
    {
        if (dataType == null)
            return null;

        EnsureInitialized();

        // 1. Check for registered styles specific to that graph.
        if (graphType != null && k_TypeStylesPerGraphType.TryGetValue(graphType, out var perGraphStyles) && perGraphStyles.TryGetValue(dataType, out var stylePerGraph))
            return stylePerGraph;

        // 2. Check registered styles for all graph types.
        if (k_TypeStylesForAllGraphTypes.TryGetValue(dataType, out var style))
            return style;

        return null;
    }
}
