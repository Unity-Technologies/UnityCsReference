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

        [Flags]
        public enum Type
        {
            None = 0,
            TrickleDown = 1,
            BubbleUp = 2
        }

        public readonly List<VisualElement> trickleDownPath;
        public readonly List<VisualElement> targetElements;
        public readonly List<VisualElement> bubbleUpPath;

        const int k_DefaultPropagationDepth = 16;
        const int k_DefaultTargetCount = 4;

        public PropagationPaths()
        {
            trickleDownPath = new List<VisualElement>(k_DefaultPropagationDepth);
            targetElements = new List<VisualElement>(k_DefaultTargetCount);
            bubbleUpPath = new List<VisualElement>(k_DefaultPropagationDepth);
        }

        public PropagationPaths(PropagationPaths paths) : this()
        {
            if (paths != null)
            {
                trickleDownPath.AddRange(paths.trickleDownPath);
                targetElements.AddRange(paths.targetElements);
                bubbleUpPath.AddRange(paths.bubbleUpPath);
            }
        }

        [NotNull] public static PropagationPaths Build(VisualElement elem, EventBase evt)
        {
            PropagationPaths paths = s_Pool.Get();
            var eventCategory = evt.eventCategory;

            // Skip element if it has no event callbacks, default action, or default action at target
            if (elem.HasEventCallbacksOrDefaultActions(eventCategory))
                paths.targetElements.Add(elem);

            // Go through the entire hierarchy. Don't bother checking elem.HasParentEventCallbacks because
            // 1. It too goes through the entire parent hierarchy, and
            // 2. It would require dirtying the parent categories when we set isCompositeRoot, so more overhead
            for (var ve = elem.nextParentWithEventCallback; ve != null; ve = ve.nextParentWithEventCallback)
            {
                if (ve.isCompositeRoot && !evt.ignoreCompositeRoots)
                {
                    // Callback for elem must be called at the Target phase. Skip if no callback.
                    if (ve.HasEventCallbacksOrDefaultActions(eventCategory))
                        paths.targetElements.Add(ve);
                }
                else if (ve.HasEventCallbacks(eventCategory))
                {
                    if (evt.tricklesDown && ve.HasTrickleDownHandlers())
                    {
                        paths.trickleDownPath.Add(ve);
                    }
                    if (evt.bubbles && ve.HasBubbleUpHandlers())
                    {
                        paths.bubbleUpPath.Add(ve);
                    }
                }
            }

            EventDebugger.LogPropagationPaths(evt, paths);

            return paths;
        }

        [NotNull] public static PropagationPaths BuildAtTarget(VisualElement elem, EventBase evt)
        {
            PropagationPaths paths = s_Pool.Get();
            var eventCategory = evt.eventCategory;

            // Skip element if it has no event callbacks, default action, or default action at target
            if (elem.HasEventCallbacksOrDefaultActions(eventCategory))
                paths.targetElements.Add(elem);

            // Go through the entire hierarchy. Don't bother checking elem.HasParentEventCallbacks because
            // 1. It too goes through the entire parent hierarchy, and
            // 2. It would require dirtying the parent categories when we set isCompositeRoot, so more overhead
            for (var ve = elem.nextParentWithEventCallback; ve != null; ve = ve.nextParentWithEventCallback)
            {
                if (ve.isCompositeRoot && !evt.ignoreCompositeRoots)
                {
                    // Callback for elem must be called at the Target phase. Skip if no callback.
                    if (ve.HasEventCallbacksOrDefaultActions(eventCategory))
                        paths.targetElements.Add(ve);
                }
            }

            EventDebugger.LogPropagationPaths(evt, paths);

            return paths;
        }

        public void Dispose()
        {
            // Empty paths to avoid leaking VisualElements.
            bubbleUpPath.Clear();
            targetElements.Clear();
            trickleDownPath.Clear();

            s_Pool.Release(this);
        }
    }
}
