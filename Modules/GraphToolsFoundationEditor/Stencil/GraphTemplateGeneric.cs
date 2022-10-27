// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A graph template associated with a stencil type.
    /// </summary>
    /// <typeparam name="TStencil">The stencil type.</typeparam>
    class GraphTemplate<TStencil> : GraphTemplate where TStencil : Stencil
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GraphTemplate{TStencil}"/> class.
        /// </summary>
        /// <param name="graphTypeName">The name of the type of graph for this template.</param>
        /// <param name="graphFileExtension">Extension for the files used to save the graph.</param>
        public GraphTemplate(string graphTypeName = "Graph", string graphFileExtension = "asset")
            : base(graphTypeName, graphFileExtension) { }

        /// <inheritdoc />
        public override Type StencilType => typeof(TStencil);
    }
}
