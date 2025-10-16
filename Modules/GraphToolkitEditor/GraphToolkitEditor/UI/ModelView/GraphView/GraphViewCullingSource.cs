// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Sources of culling in a <see cref="GraphView"/>.
    /// </summary>
    /// <remarks>A tool can declare new culling sources by declaring new static fields of this type.</remarks>
    [UnityRestricted]
    internal class GraphViewCullingSource : Enumeration
    {
        static int s_NextId;

        /// <summary>
        /// Culling when outside the viewport.
        /// </summary>
        public static readonly GraphViewCullingSource OutOfView;

        /// <summary>
        /// Culling at small zoom levels.
        /// </summary>
        public static readonly GraphViewCullingSource Zoom;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphViewCullingSource"/> class.
        /// </summary>
        public GraphViewCullingSource(string name)
            : base(s_NextId++, name) { }

        static GraphViewCullingSource()
        {
            s_NextId = 0;

            OutOfView = new GraphViewCullingSource(nameof(OutOfView));
            Zoom = new GraphViewCullingSource(nameof(Zoom));
        }
    }

    [UnityRestricted]
    internal static class GraphViewCullingSourceExtensions
    {
        /// <summary>
        /// Sets the culling state of a culling source on the active culling source list.
        /// </summary>
        /// <param name="activeCullingSources">The current active culling sources.</param>
        /// <param name="cullingState">The new culling state.</param>
        /// <param name="cullingSource">The culling source.</param>
        public static void SetCullingSourceState(this List<GraphViewCullingSource> activeCullingSources, GraphViewCullingState cullingState, GraphViewCullingSource cullingSource)
        {
            if (cullingState == GraphViewCullingState.Disabled)
            {
                activeCullingSources.RemoveAll(source => source == cullingSource);
            }
            else
            {
                if (!activeCullingSources.HasCullingSource(cullingSource))
                    activeCullingSources.Add(cullingSource);
            }
        }

        /// <summary>
        /// Sets the culling state of a list of culling sources on the active culling source list.
        /// </summary>
        /// <param name="activeCullingSources">The current active culling sources.</param>
        /// <param name="cullingState">The new culling state.</param>
        /// <param name="cullingSources">The culling sources.</param>
        public static void SetCullingSourcesState(this List<GraphViewCullingSource> activeCullingSources, GraphViewCullingState cullingState, IReadOnlyList<GraphViewCullingSource> cullingSources)
        {
            if (cullingState == GraphViewCullingState.Disabled)
            {
                activeCullingSources.RemoveAll(type => cullingSources.HasCullingSource(type));
            }
            else
            {
                for (var i = 0; i < cullingSources.Count; ++i)
                {
                    var cullingSource = cullingSources[i];
                    if (!activeCullingSources.HasCullingSource(cullingSource))
                        activeCullingSources.Add(cullingSource);
                }
            }
        }

        /// <summary>
        /// Checks if a culling source is in the active culling source list.
        /// </summary>
        /// <param name="activeCullingSources">The current active culling sources.</param>
        /// <param name="cullingSource">The culling source.</param>
        /// <returns>True if the culling source is found, false otherwise.</returns>
        public static bool HasCullingSource(this IReadOnlyList<GraphViewCullingSource> activeCullingSources, GraphViewCullingSource cullingSource)
        {
            for (var i = 0; i < activeCullingSources.Count; i++)
            {
                if (activeCullingSources[i] == cullingSource)
                    return true;
            }

            return false;
        }
    }
}
