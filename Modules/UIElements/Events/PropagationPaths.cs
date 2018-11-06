// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    class PropagationPaths : IDisposable
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
        public readonly List<VisualElement> bubblePath;

        const int k_DefaultPropagationDepth = 16;

        public PropagationPaths()
        {
            trickleDownPath = new List<VisualElement>(k_DefaultPropagationDepth);
            bubblePath = new List<VisualElement>(k_DefaultPropagationDepth);
        }

        public static PropagationPaths Build(VisualElement elem, PropagationPaths.Type pathTypesRequested)
        {
            if (elem == null || pathTypesRequested == PropagationPaths.Type.None)
                return null;

            PropagationPaths paths = s_Pool.Get();

            while (elem.hierarchy.parent != null)
            {
                if (elem.hierarchy.parent.enabledInHierarchy)
                {
                    if ((pathTypesRequested & PropagationPaths.Type.TrickleDown) == PropagationPaths.Type.TrickleDown && elem.hierarchy.parent.HasTrickleDownHandlers())
                        paths.trickleDownPath.Add(elem.hierarchy.parent);
                    if ((pathTypesRequested & PropagationPaths.Type.BubbleUp) == PropagationPaths.Type.BubbleUp && elem.hierarchy.parent.HasBubbleUpHandlers())
                        paths.bubblePath.Add(elem.hierarchy.parent);
                }
                elem = elem.hierarchy.parent;
            }
            return paths;
        }

        public void Dispose()
        {
            // Empty paths to avoid leaking VisualElements.
            bubblePath.Clear();
            trickleDownPath.Clear();

            s_Pool.Release(this);
        }
    }
}
