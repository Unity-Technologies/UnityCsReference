using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    class PropagationPaths
    {
        static readonly ObjectPool<PropagationPaths> s_Pool = new ObjectPool<PropagationPaths>();

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

        public PropagationPaths(PropagationPaths paths)
        {
            trickleDownPath = new List<VisualElement>(paths.trickleDownPath);
            targetElements = new List<VisualElement>(paths.targetElements);
            bubbleUpPath = new List<VisualElement>(paths.bubbleUpPath);
        }

        internal static PropagationPaths Copy(PropagationPaths paths)
        {
            PropagationPaths copyPaths = s_Pool.Get();
            copyPaths.trickleDownPath.AddRange(paths.trickleDownPath);
            copyPaths.targetElements.AddRange(paths.targetElements);
            copyPaths.bubbleUpPath.AddRange(paths.bubbleUpPath);

            return copyPaths;
        }

        public static PropagationPaths Build(VisualElement elem, Type pathTypesRequested)
        {
            if (elem == null || pathTypesRequested == Type.None)
                return null;

            PropagationPaths paths = s_Pool.Get();

            paths.targetElements.Add(elem);

            while (elem.hierarchy.parent != null)
            {
                if (elem.hierarchy.parent.enabledInHierarchy)
                {
                    if (elem.hierarchy.parent.isCompositeRoot)
                    {
                        // Callback for elem.hierarchy.parent must be called at the Target phase.
                        paths.targetElements.Add(elem.hierarchy.parent);
                    }
                    else
                    {
                        if ((pathTypesRequested & Type.TrickleDown) == Type.TrickleDown && elem.hierarchy.parent.HasTrickleDownHandlers())
                        {
                            paths.trickleDownPath.Add(elem.hierarchy.parent);
                        }

                        if ((pathTypesRequested & Type.BubbleUp) == Type.BubbleUp && elem.hierarchy.parent.HasBubbleUpHandlers())
                        {
                            paths.bubbleUpPath.Add(elem.hierarchy.parent);
                        }
                    }
                }
                elem = elem.hierarchy.parent;
            }
            return paths;
        }

        public void Release()
        {
            // Empty paths to avoid leaking VisualElements.
            bubbleUpPath.Clear();
            targetElements.Clear();
            trickleDownPath.Clear();

            s_Pool.Release(this);
        }
    }
}
