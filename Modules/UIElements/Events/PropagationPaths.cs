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

        public struct PropagationPathElement
        {
            public VisualElement m_VisualElement;
            public bool m_IsTarget;
        }

        public readonly List<VisualElement> trickleDownPath;
        public readonly List<PropagationPathElement> targetAndBubblePath;

        const int k_DefaultPropagationDepth = 16;

        public PropagationPaths()
        {
            trickleDownPath = new List<VisualElement>(k_DefaultPropagationDepth);
            targetAndBubblePath = new List<PropagationPathElement>(k_DefaultPropagationDepth);
        }

        public PropagationPaths(PropagationPaths paths)
        {
            trickleDownPath = new List<VisualElement>(paths.trickleDownPath);
            targetAndBubblePath = new List<PropagationPathElement>(paths.targetAndBubblePath);
        }

        public static PropagationPaths Build(VisualElement elem, Type pathTypesRequested)
        {
            if (elem == null || pathTypesRequested == Type.None)
                return null;

            PropagationPaths paths = s_Pool.Get();

            while (elem.hierarchy.parent != null)
            {
                if (elem.hierarchy.parent.enabledInHierarchy)
                {
                    if (elem.hierarchy.parent.isCompositeRoot)
                    {
                        // Callback for elem.hierarchy.parent must be called at the Target phase.
                        var item = new PropagationPathElement
                        {
                            m_VisualElement = elem.hierarchy.parent,
                            m_IsTarget = true
                        };
                        paths.targetAndBubblePath.Add(item);
                    }
                    else
                    {
                        if ((pathTypesRequested & Type.TrickleDown) == Type.TrickleDown && elem.hierarchy.parent.HasTrickleDownHandlers())
                        {
                            paths.trickleDownPath.Add(elem.hierarchy.parent);
                        }

                        if ((pathTypesRequested & Type.BubbleUp) == Type.BubbleUp && elem.hierarchy.parent.HasBubbleUpHandlers())
                        {
                            var item = new PropagationPathElement
                            {
                                m_VisualElement = elem.hierarchy.parent,
                                m_IsTarget = false
                            };
                            paths.targetAndBubblePath.Add(item);
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
            targetAndBubblePath.Clear();
            trickleDownPath.Clear();

            s_Pool.Release(this);
        }
    }
}
