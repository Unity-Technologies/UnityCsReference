// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Base class for graph templates.
    /// </summary>
    abstract class GraphTemplate
    {
        /// <summary>
        /// The stencil type.
        /// </summary>
        public abstract Type StencilType { get; }

        /// <summary>
        /// The graph type name.
        /// </summary>
        public virtual string GraphTypeName { get; }

        /// <summary>
        /// Default name for the graph asset.
        /// </summary>
        public virtual string DefaultAssetName => GraphTypeName;

        /// <summary>
        /// The extension of the graph asset file.
        /// </summary>
        public virtual string GraphFileExtension { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphTemplate{TStencil}"/> class.
        /// </summary>
        /// <param name="graphTypeName">The name of the type of graph for this template.</param>
        /// <param name="graphFileExtension">Extension for the files used to save the graph.</param>
        protected GraphTemplate(string graphTypeName = "Graph", string graphFileExtension = "asset")
        {
            GraphTypeName = graphTypeName;
            GraphFileExtension = graphFileExtension;
        }

        /// <summary>
        /// Callback to initialize a new graph. Implement this method to add nodes and wires to new graphs.
        /// </summary>
        /// <param name="graphModel">The graph model to initialize.</param>
        public virtual void InitBasicGraph(GraphModel graphModel) { }
    }
}
