// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.IMGUI.Controls;

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    /// <summary>
    /// Provides type discovery and dropdown menu building for block types.
    /// Used by block selection UI to populate block type dropdowns.
    /// </summary>
    internal interface IBlockDropdownItemCache
    {
        /// <summary>
        /// Builds a hierarchical dropdown menu of block types that implement the specified interface.
        /// Results are cached for performance across multiple calls.
        /// </summary>
        /// <param name="requiredInterface">The interface that block types must implement (e.g., typeof(IBlock))</param>
        /// <returns>Root dropdown item containing all discovered block types organized hierarchically</returns>
        AdvancedDropdownItem GetOrBuildDropdownRoot(Type requiredInterface);
    }
}
