// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Base class for graph processors.
    /// </summary>
    abstract class GraphProcessor
    {
        /// <summary>
        /// Processes the graph.
        /// </summary>
        /// <param name="graphModel">The graph to process.</param>
        /// <param name="changes">A description of what changed in the graph. If null, the method assumes everything changed.</param>
        /// <returns>The results of the processing.</returns>
        public abstract GraphProcessingResult ProcessGraph(GraphModel graphModel, GraphChangeDescription changes);
    }
}
