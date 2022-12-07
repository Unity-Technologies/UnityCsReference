// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Class that contains graph processors used to process a graph.
    /// </summary>
    class GraphProcessorContainer
    {
        List<GraphProcessor> m_GraphProcessors;

        /// <summary>
        /// Adds a graph processor to the container.
        /// </summary>
        /// <param name="graphProcessor">The graph processor.</param>
        public void AddGraphProcessor(GraphProcessor graphProcessor)
        {
            m_GraphProcessors ??= new List<GraphProcessor>();
            m_GraphProcessors.Add(graphProcessor);
        }

        /// <summary>
        /// Performs tasks that need to be done when the <see cref="GraphModel"/> is enabled.
        /// </summary>
        public void OnGraphModelEnabled()
        {
            if (m_GraphProcessors == null)
            {
                return;
            }

            foreach (var processor in m_GraphProcessors)
            {
                processor.OnGraphModelEnabled();
            }
        }

        /// <summary>
        /// Performs tasks that need to be done when the <see cref="GraphModel"/> is disabled.
        /// </summary>
        public void OnGraphModelDisabled()
        {
            if (m_GraphProcessors == null)
            {
                return;
            }

            foreach (var processor in m_GraphProcessors)
            {
                processor.OnGraphModelDisabled();
            }
        }

        /// <summary>
        /// Processes a graph using the container's graph processors.
        /// </summary>
        /// <param name="graphModel">The graph to process.</param>
        /// <param name="changes">A description of what changed in the graph. If null, the method assumes everything changed.</param>
        /// <returns>A list of <see cref="GraphProcessingResult"/>, one for each <see cref="GraphProcessor"/>.</returns>
        public IReadOnlyList<GraphProcessingResult> ProcessGraph(GraphModel graphModel, GraphChangeDescription changes)
        {
            var results = new List<GraphProcessingResult>();

            if (m_GraphProcessors != null)
            {
                foreach (var graphProcessor in m_GraphProcessors)
                {
                    results.Add(graphProcessor.ProcessGraph(graphModel, changes));
                }
            }

            return results;
        }
    }
}
