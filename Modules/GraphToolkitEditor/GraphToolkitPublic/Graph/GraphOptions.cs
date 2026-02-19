// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Flags that define configuration options that affect the behavior and capabilities of a <see cref="Graph"/> class.
    /// </summary>
    /// <remarks>
    /// Use the <see cref="GraphOptions"/> enum in conjunction with the <see cref="GraphAttribute"/> to customize how a graph behaves,
    /// including support for subgraphs and automatic node discovery. The default value is <see cref="GraphOptions.Default"/>, which enables
    /// standard behavior such as allowing nodes defined in the same assembly as the graph to be automatically included in the graph item library.
    /// Combine flags to customize behavior. This enum is marked with
    /// <see cref="System.FlagsAttribute"/>, so you can combine values using bitwise operations to enable multiple options.
    /// </remarks>
    /// <remarks>
    /// This example keeps the default behavior and adds support for subgraphs by enabling <see cref="GraphOptions.SupportsSubgraphs"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// [Graph("mygraph", GraphOptions.SupportsSubgraphs)]
    /// public class MyGraph : Graph { }
    /// </code>
    /// </example>
    [Flags]
    public enum GraphOptions
    {
        /// <summary>
        /// Indicates that this graph supports subgraphs.
        /// </summary>
        /// <remarks>
        /// When enabled, the “Convert Selection to Subgraph” item will be available in the right click menu of a selection of elements in the graph.
        /// </remarks>
        SupportsSubgraphs = 1 << 0,

        /// <summary>
        /// Indicates that nodes (i.e., subclasses of <see cref="Node"/>) defined in the same assembly as the graph are not automatically added to the graph item library.
        /// </summary>
        /// <remarks>
        /// By default, this flag is disabled. This allows you to discover nodes without manually annotating each one with <see cref="UseWithGraphAttribute"/>.
        /// Developers who want full control over what appears in the graph item library might choose to enable this option.
        /// </remarks>
        DisableAutoInclusionOfNodesFromGraphAssembly = 1 << 1,

        // -------------
        // If you're adding a new flag, make sure the default is 'false'. This ensures that the user
        // doesn't override defaults by mistake when setting one or more other flags for their graph options.

        /// <summary>
        /// The default graph configuration.
        /// </summary>
        /// <remarks>
        /// This default is helpful for onboarding: if users forget to mark nodes with <see cref="UseWithGraphAttribute"/>, they will still appear in the graph item library
        /// as long as they are defined in the same assembly as the graph.
        /// </remarks>
        Default = 0,

        /// <summary>
        /// No graph options enabled.
        /// </summary>
        /// <remarks>
        /// This disables all optional features, including subgraph support and automatic node inclusion.
        /// </remarks>
        None = 0
    }
}
