// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using static Unity.U2D.Physics.Scripting2D;

namespace Unity.U2D.Physics
{
    /// <summary>
    /// This provides a common method to retrieving layer information.
    /// If a <see cref="PhysicsCoreSettings2D"/> asset is assigned then the full layers (<see cref="PhysicsCoreSettings2D.physicsLayerNames"/>) will be used if <see cref="PhysicsCoreSettings2D.usePhysicsLayers"/> is also active.
    /// If no <see cref="PhysicsCoreSettings2D"/> asset is assigned then the global layers (See <see cref="UnityEngine.LayerMask"/>) will be used.
    /// </summary>
    [MovedFrom(autoUpdateAPI: ScriptUpdateConstants.AutoUpdateAPI, sourceNamespace: ScriptUpdateConstants.SourceNamespace, sourceAssembly: ScriptUpdateConstants.SourceAssembly)]
    public readonly record struct PhysicsLayers
    {
        /// <summary>
        /// Get a <see cref="PhysicsMask"/> for the specified layer name(s).
        /// </summary>
        /// <remarks>
        /// Use this method to build the category and contact masks for a <see cref="PhysicsShape.ContactFilter"/> so a shape only collides with the layers it needs to.
        /// Create the layers in a <see cref="PhysicsCoreSettings2D"/> asset first.
        /// When physics layers are active and more than one layer has the same name, the mask includes every layer with that name.
        /// </remarks>
        /// <param name="layerNames">The layer names (case sensitive) to find a combined physics mask for.</param>
        /// <returns>The combined physics mask for the layer names that were found. Unity produces a console warning for each layer name that isn't found and leaves it out of the mask, so if no names are found, <see cref="PhysicsMask.None"/> is returned.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="layerNames"/> or any of the layer names is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown if no layer names are provided.</exception>
        /// <example>
        /// <code lang="cs">
        /// <![CDATA[
        /// // Get a physics mask for a single layer, and another for a group of layers
        /// // that the object should collide with.
        /// using UnityEngine;
        /// using Unity.U2D.Physics;
        ///
        /// public class GetLayerMaskExample : MonoBehaviour
        /// {
        ///     void Start()
        ///     {
        ///         PhysicsMask objectLayer = PhysicsLayers.GetLayerMask("Car");
        ///         PhysicsMask contactLayer = PhysicsLayers.GetLayerMask("Walls");
        ///     }
        /// }
        /// ]]>
        /// </code>
        /// </example>
        public static PhysicsMask GetLayerMask(params string[] layerNames)
        {
            // Throw if not layer names provided.
            if (layerNames == null)
                throw new ArgumentNullException(nameof(layerNames));

            if (layerNames.Length == 0)
                throw new ArgumentException("No Layer Names provided.", nameof(layerNames));

            // Throw if any layer name is null.
            foreach (var name in layerNames)
            {
                if (name == null)
                    throw new ArgumentNullException(nameof(layerNames), "A layer name cannot be null.");
            }

            // 64-bit layers (if available and active).
            if (PhysicsWorld.usePhysicsLayers && PhysicsGlobal_GetPhysicsLayers() is LayerNames physicsLayerNames)
            {
                PhysicsMask physicsMask = PhysicsMask.None;
                foreach (var name in layerNames)
                {
                    var layerMask = physicsLayerNames.GetLayerMask(name);
                    if (layerMask != PhysicsMask.None)
                    {
                        physicsMask |= layerMask;
                        continue;
                    }

                    // Warn if not found.
                    Debug.LogWarning($"The layer name '{name}' could not be found in the full 64-bit layers. Note that the name(s) provided are case-sensitive.");
                }

                return physicsMask;
            }

            // 32-bit layers.
            {
                PhysicsMask physicsMask = PhysicsMask.None;
                foreach (var name in layerNames)
                {
                    // An empty name is never a layer name, but NameToLayer matches it to the first unnamed layer.
                    var layer = name.Length == 0 ? InvalidLayerOrdinal : LayerMask.NameToLayer(name);
                    if (layer != PhysicsLayers.InvalidLayerOrdinal)
                    {
                        physicsMask |= new PhysicsMask(layer);
                        continue;
                    }

                    // Warn if not found.
                    Debug.LogWarning($"The layer name '{name}' could not be found in the standard 32-bit layers. Note that the name(s) provided are case-sensitive.");
                }

                return physicsMask;
            }
        }

        /// <summary>
        /// Get a layer ordinal (index) for the specified layer name. This is not a 32-bit mask but simply the layer ordinal (index) associated with the specified layer name.
        /// </summary>
        /// <remarks>
        /// When physics layers are active and more than one layer has the same name, only the lowest-numbered layer with that name is found, so the ordinal of any other layer with that name is never returned.
        /// </remarks>
        /// <param name="layerName">The layer name (case sensitive) to find the layer ordinal for.</param>
        /// <returns>The layer ordinal associated with the specified layer name or, if not found, <see cref="PhysicsLayers.InvalidLayerOrdinal"/> will be returned in which case a console warning will also be produced.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="layerName"/> is null.</exception>
        public static int GetLayerOrdinal(string layerName)
        {
            if (layerName == null)
                throw new ArgumentNullException(nameof(layerName));

            // 64-bit layers (if available and active).
            if (PhysicsWorld.usePhysicsLayers && PhysicsGlobal_GetPhysicsLayers() is LayerNames physicsLayerNames)
            {
                var layerOrdinal = physicsLayerNames.GetLayerOrdinal(layerName);
                if (layerOrdinal != InvalidLayerOrdinal)
                    return layerOrdinal;

                // Warn if not found.
                Debug.LogWarning($"The layer name '{layerName}' could not be found in the full 64-bit layers. Note that the name provided is case-sensitive.");

                return InvalidLayerOrdinal;
            }

            // 32-bit layers.
            {
                // An empty name is never a layer name, but NameToLayer matches it to the first unnamed layer.
                var layerOrdinal = layerName.Length == 0 ? InvalidLayerOrdinal : LayerMask.NameToLayer(layerName);
                if (layerOrdinal != InvalidLayerOrdinal)
                    return layerOrdinal;

                // Warn if not found.
                Debug.LogWarning($"The layer name '{layerName}' could not be found in the standard 32-bit layers. Note that the name provided is case-sensitive.");

                return InvalidLayerOrdinal;
            }
        }

        /// <summary>
        /// Get a layer name for the specified layer ordinal (index).
        /// </summary>
        /// <param name="layerOrdinal">The layer ordinal (index). When using the full layers this should be within the range [0, 63] however if not then the range must be [0, 31].</param>
        /// <returns>The layer name. If no layer name is present then <see cref="String.Empty"/> is returned.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">If the specified layer ordinal is out of range.</exception>
        public static string GetLayerName(int layerOrdinal)
        {
            // 64-bit layers (if available and active).
            if (PhysicsWorld.usePhysicsLayers && PhysicsGlobal_GetPhysicsLayers() is LayerNames physicsLayerNames)
            {
                if (layerOrdinal < 0 || layerOrdinal > 63)
                    throw new ArgumentOutOfRangeException(nameof(layerOrdinal), $"The layer ordinal '{layerOrdinal}' is out of the valid range [0, 63].");

                return physicsLayerNames.GetLayerName(layerOrdinal);
            }

            // 32-bit layers.
            {
                if (layerOrdinal < 0 || layerOrdinal > 31)
                    throw new ArgumentOutOfRangeException(nameof(layerOrdinal), $"The layer ordinal '{layerOrdinal}' is out of the valid range [0, 31].");

                return LayerMask.LayerToName(layerOrdinal);
            }
        }

        /// <summary>
        /// Indicates an invalid layer ordinal. This is typically used when retrieving a layer ordinal but a name could not be found.
        /// </summary>
        public const int InvalidLayerOrdinal = -1;

        /// <undoc/>
        internal static void GetLayerNamesAndMasks(List<String> layerNames, List<UInt64> layerMasks)
        {
            layerNames.Clear();
            layerMasks.Clear();

            if (PhysicsGlobal_GetPhysicsLayers() is LayerNames physicsLayerNames)
            {
                var names = physicsLayerNames.m_Names;
                if (names.Length == 64)
                {
                    for (var i = 0; i < 64; ++i)
                    {
                        var name = names[i];
                        if (string.IsNullOrEmpty(name))
                            continue;

                        layerNames.Add($"{name} [{i}]");
                        layerMasks.Add((UInt64)1 << i);
                    }
                }
            }
        }

        /// <undoc/>
        internal static void GetBitNamesAndMasks(List<String> layerNames, List<UInt64> layerMasks)
        {
            layerNames.Clear();
            layerMasks.Clear();

            for (var i = 0; i < 64; ++i)
            {
                layerNames.Add($"{i}");
                layerMasks.Add((UInt64)1 << i);
            }
        }

        /// <undoc/>
        [Serializable]
        public class LayerNames : ISerializationCallbackReceiver
        {
            [SerializeField]
            internal string[] m_Names;
            private string[] Names
            {
                get
                {
                    if (m_Names == null || m_Names.Length != 64)
                        m_Names = new string[64];

                    return m_Names;
                }
            }

            private Dictionary<string, int> m_NameMap;
            private Dictionary<string, int> NameMap
            {
                get
                {
                    if (m_NameMap == null)
                        m_NameMap = new Dictionary<string, int>(capacity: 64);

                    return m_NameMap;
                }
            }

            // The combined mask of every layer with each name, as more than one layer can have the same name.
            private Dictionary<string, PhysicsMask> m_MaskMap;
            private Dictionary<string, PhysicsMask> MaskMap
            {
                get
                {
                    if (m_MaskMap == null)
                        m_MaskMap = new Dictionary<string, PhysicsMask>(capacity: 64);

                    return m_MaskMap;
                }
            }

            /// <undoc/>
            public void OnBeforeSerialize() { }

            /// <undoc/>
            public void OnAfterDeserialize()
            {
                var names = Names;
                var nameMap = NameMap;
                var maskMap = MaskMap;
                nameMap.Clear();
                maskMap.Clear();

                for (var i = 0; i < 64; ++i)
                {
                    var name = names[i];
                    if (string.IsNullOrEmpty(name))
                        continue;

                    // The ordinal is the lowest-numbered layer with the name.
                    nameMap.TryAdd(name, i);

                    maskMap.TryGetValue(name, out var layerMask);
                    maskMap[name] = layerMask | ((UInt64)1 << i);
                }
            }

            /// <undoc/>
            internal static LayerNames DefaultLayerNames
            {
                get
                {
                    var layerNames = new LayerNames();

                    var names = layerNames.Names;
                    names[0] = "Default";
                    layerNames.NameMap.Add(names[0], 0);
                    layerNames.MaskMap.Add(names[0], PhysicsMask.One);

                    return layerNames;
                }
            }

            /// <undoc/>
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            internal int GetLayerOrdinal(string layerName)
            {
                if (NameMap.TryGetValue(layerName, out var layer))
                    return layer;

                return InvalidLayerOrdinal;
            }
            
            /// <undoc/>
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            internal PhysicsMask GetLayerMask(string layerName)
            {
                if (MaskMap.TryGetValue(layerName, out var layerMask))
                    return layerMask;

                return default;
            }

            /// <undoc/>
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            internal string GetLayerName(int layerOrdinal)
            {
                if (layerOrdinal < 0 || layerOrdinal > 63)
                    throw new ArgumentOutOfRangeException(nameof(layerOrdinal), $"The layer ordinal '{layerOrdinal}' is out of the valid range [0, 63].");

                return Names[layerOrdinal] ?? string.Empty;
            }
        }
    }
}
