// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Attribute used to declare a graph type by associating it with a file extension and optional configuration options.
    /// </summary>
    /// <remarks>
    /// Use this attribute to associate a custom <see cref="Graph"/> class with a unique file extension and <see cref="GraphOptions"/>.
    /// The <c>extension</c> parameter defines the file extension for the graph assets. This extension must be unique across the project
    /// because Unity uses it to select the correct importer. You can also configure additional options using <see cref="GraphOptions"/>.
    /// This attribute is required for any class that inherits from <see cref="Graph"/> and serves as the entry point for enabling
    /// editor support for the graph tool.
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
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class GraphAttribute : Attribute
    {
        /// <summary>
        /// Gets the file extension associated with the <see cref="Graph"/>.
        /// </summary>
        /// <remarks>
        /// The extension must be unique across all asset types. Unity uses this extension to determine which importer to use
        /// for the asset. For example, if the extension is ".mygraph", Unity uses the importer linked to this <see cref="Graph"/>
        /// type for all files with that extension.
        /// </remarks>
        public string Extension { get; }

        /// <summary>
        /// Gets the graph configuration options.
        /// </summary>
        /// <remarks>
        /// These options define specific behaviors of the graph, such as <see cref="GraphOptions.SupportsSubgraphs"/> or <see cref="GraphOptions.DisableAutoInclusionOfNodesFromGraphAssembly"/>.
        /// </remarks>
        public GraphOptions Options { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphAttribute"/> class with a file extension and optional graph options.
        /// </summary>
        /// <param name="extension">
        /// The file extension to associate with assets of the graph type. This value must be unique because Unity uses it to select the correct importer.
        /// </param>
        /// <param name="options">
        /// The configuration options for the graph. Defaults to <see cref="GraphOptions.Default"/> if not specified.
        /// </param>
        /// <remarks>
        /// Use this constructor to define the asset extension and configure the graph. This allows for proper asset recognition and import handling by Unity.
        /// The values in <see cref="GraphOptions"/> support bitwise combination. Combine multiple flags to configure the graph with custom behavior.
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
        public GraphAttribute(string extension, GraphOptions options = GraphOptions.Default)
        {
            this.Extension = extension;
            this.Options = options;
        }
    }
}
