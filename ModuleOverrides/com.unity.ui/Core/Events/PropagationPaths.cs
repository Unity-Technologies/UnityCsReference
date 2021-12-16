// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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

        public static PropagationPaths Build(VisualElement elem, EventBase evt, Type pathTypesRequested)
        {
            if (elem == null || pathTypesRequested == Type.None)
                return null;

            PropagationPaths paths = s_Pool.Get();

            paths.targetElements.Add(elem);

            while (elem.hierarchy.parent != null)
            {
                elem = elem.hierarchy.parent;
                if (elem.isCompositeRoot && !evt.ignoreCompositeRoots)
                {
                    // Callback for elem must be called at the Target phase.
                    paths.targetElements.Add(elem);
                }
                else
                {
                    if ((pathTypesRequested & Type.TrickleDown) == Type.TrickleDown && elem.HasTrickleDownHandlers())
                    {
                        paths.trickleDownPath.Add(elem);
                    }
                    if ((pathTypesRequested & Type.BubbleUp) == Type.BubbleUp && elem.HasBubbleUpHandlers())
                    {
                        paths.bubbleUpPath.Add(elem);
                    }
                }
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
