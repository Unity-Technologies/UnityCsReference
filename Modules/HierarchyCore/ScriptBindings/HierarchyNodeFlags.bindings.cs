// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Bit flags used to describe the state of a hierarchy node.
    /// </summary>
    /// <remarks>
    /// You can define custom flags by casting an unused bit position to this enum.
    /// The built-in values occupy bits 0–3, so bits 4 and above are available for extension.
    /// Use <see cref="HierarchyViewModel.SetFlags(in HierarchyNode, HierarchyNodeFlags)"/>, <see cref="HierarchyViewModel.SetFlagsRecursive(in HierarchyNode, HierarchyNodeFlags, HierarchyTraversalDirection)"/>,
    /// <see cref="HierarchyViewModel.ClearFlags(in HierarchyNode, HierarchyNodeFlags)"/>, and <see cref="HierarchyViewModel.ClearFlagsRecursive(in HierarchyNode, HierarchyNodeFlags, HierarchyTraversalDirection)"/>
    /// with your custom flags to track additional per-node states without affecting built-in behavior.
    /// </remarks>
    /// <example>
    /// The following example draws visual connector lines in the Hierarchy window to show the parent and child relationships between GameObjects. It uses `HierarchyNodeFlags` to define a custom flag at bit 4 to track the hover state for visual connector lines. The example sets and clears the flag recursively on an ancestor and its descendants when your mouse enters or leaves a connector line. 
    ///
    /// The example requires three USS files: `Connectors.uss` for the base styles, `Connectors_dark.uss` for the Dark theme, and `Connectors_light.uss` for the Light theme.
    ///
    /// To use this example, save the script and USS files in a folder called `Assets/Editor/Connectors`. Scripts in an `Editor` folder can use the Hierarchy module API without additional setup. If you save the script outside of an `Editor` folder, you must enable the Hierarchy built-in module in the **Package Manager** window, which also adds the module to your Player builds.
    /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/Connectors/Connectors.cs"/>
    /// </example>
    /// <example>
    /// The following example shows how to style `Connectors.uss`.
    /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/Connectors/Connectors.uss"/>
    /// </example>
    /// <example>
    /// The following example shows how to style `Connectors_dark.uss`.
    /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/Connectors/Connectors_dark.uss"/>
    /// </example>
    /// <example>
    /// The following example shows how to style `Connectors_light.uss`.
    /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/Connectors/Connectors_light.uss"/>
    /// </example>
    [Flags, NativeHeader("Modules/HierarchyCore/Public/HierarchyNodeFlags.h")]
    public enum HierarchyNodeFlags : uint
    {
        /// <summary>
        /// The hierarchy node has no flags.
        /// </summary>
        None = 0,
        /// <summary>
        /// The hierarchy node is expanded.
        /// </summary>
        Expanded = 1 << 0,
        /// <summary>
        /// The hierarchy node is selected.
        /// </summary>
        Selected = 1 << 1,
        /// <summary>
        /// The hierarchy node is cut.
        /// </summary>
        Cut = 1 << 2,
        /// <summary>
        /// The hierarchy node is hidden (also hides children).
        /// </summary>
        Hidden = 1 << 3,
    }
}
