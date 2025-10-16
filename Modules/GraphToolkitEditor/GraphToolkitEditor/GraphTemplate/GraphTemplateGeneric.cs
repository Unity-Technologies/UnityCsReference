// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A graph template associated with a graph model type.
    /// </summary>
    /// <typeparam name="TGraphModel">The graph model type.</typeparam>
    [UnityRestricted]
    internal class GraphTemplate<TGraphModel> : GraphTemplate
        where TGraphModel : GraphModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GraphTemplate{TGraphModel}"/> class.
        /// </summary>
        /// <param name="graphTypeName">The name of the type of graph for this template.</param>
        /// <param name="graphFileExtension">Extension for the files used to save the graph.</param>
        public GraphTemplate(string graphTypeName = "Graph", string graphFileExtension = "asset")
            : base(graphTypeName, graphFileExtension) { }

        /// <inheritdoc />
        public override Type GraphModelType => typeof(TGraphModel);
    }
}
