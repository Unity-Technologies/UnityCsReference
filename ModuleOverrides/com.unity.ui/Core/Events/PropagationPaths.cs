// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.UIElements.Experimental;

namespace UnityEngine.UIElements
{
    class PropagationPaths : IDisposable
    {
        static readonly ObjectPool<PropagationPaths> s_Pool = new ObjectPool<PropagationPaths>(() => new PropagationPaths());

        public readonly List<VisualElement> trickleDownPath;
        public readonly List<VisualElement> bubbleUpPath;

        const int k_DefaultPropagationDepth = 8;

        public PropagationPaths()
        {
            trickleDownPath = new List<VisualElement>(k_DefaultPropagationDepth);
            bubbleUpPath = new List<VisualElement>(k_DefaultPropagationDepth);
        }

        public PropagationPaths(PropagationPaths paths) : this()
        {
            if (paths != null)
            {
                trickleDownPath.AddRange(paths.trickleDownPath);
                bubbleUpPath.AddRange(paths.bubbleUpPath);
            }
        }

        [NotNull] public static PropagationPaths Build(VisualElement elem, EventBase evt, int eventCategories)
        {
            PropagationPaths paths = s_Pool.Get();

            // Skip element if it has no event callbacks and no HandleEventTrickleDown/BubbleUp override.
            // Assume elem.HasParentEventInterests has already been called upstream if it was relevant.
            if (elem.HasTrickleDownEventInterests(eventCategories))
            {
                paths.trickleDownPath.Add(elem);
            }
            if (elem.HasBubbleUpEventInterests(eventCategories))
            {
                paths.bubbleUpPath.Add(elem);
            }

            // Go through the entire hierarchy. Early out if the entire parent chain has no interest for the event.
            for (var ve = elem.nextParentWithEventInterests; ve != null; ve = ve.nextParentWithEventInterests)
            {
                if (!ve.HasParentEventInterests(eventCategories))
                    break;

                if (evt.tricklesDown && ve.HasTrickleDownEventInterests(eventCategories))
                {
                    paths.trickleDownPath.Add(ve);
                }
                if (evt.bubbles && ve.HasBubbleUpEventInterests(eventCategories))
                {
                    paths.bubbleUpPath.Add(ve);
                }
            }

            EventDebugger.LogPropagationPaths(evt, paths);

            return paths;
        }

        public void Dispose()
        {
            // Empty paths to avoid leaking VisualElements.
            bubbleUpPath.Clear();
            trickleDownPath.Clear();

            s_Pool.Release(this);
        }
    }
}
