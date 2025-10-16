// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.InternalBridge;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Attach an element to a target. Whenever the target position changes, the element will follow the target.
    /// </summary>
    [UnityRestricted]
    internal class Attacher
    {
        List<VisualElement> m_WatchedObjects;
        Vector2 m_Offset;
        SpriteAlignment m_Alignment;
        Vector2 m_Distance;

        /// <summary>
        /// The target to which the element is attached.
        /// </summary>
        public VisualElement Target { get; }

        /// <summary>
        /// The element to attach to a target.
        /// </summary>
        public VisualElement Element { get; }

        /// <summary>
        /// The element used as a reference point to align the element to the target. By default, it consists of the element itself.
        /// </summary>
        public VisualElement ReferenceElement { get; set; }

        public SpriteAlignment Alignment
        {
            get => m_Alignment;
            set
            {
                if (m_Alignment != value)
                {
                    m_Alignment = value;
                    if (IsAttached)
                    {
                        AlignOnTarget();
                    }
                }
            }
        }

        public Vector2 Offset
        {
            get => m_Offset;
            set
            {
                if (m_Offset != value)
                {
                    m_Offset = value;
                    if (IsAttached)
                    {
                        AlignOnTarget();
                    }
                }
            }
        }

        public Vector2 Distance
        {
            get => m_Distance;
            set
            {
                if (m_Distance != value)
                {
                    m_Distance = value;
                    if (IsAttached)
                    {
                        AlignOnTarget();
                    }
                }
            }
        }

        bool IsAttached => Target != null && Element != null && m_WatchedObjects != null && m_WatchedObjects.Count > 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="Attacher"/> class.
        /// </summary>
        /// <param name="anchored">The element that will follow the target.</param>
        /// <param name="target">The target to follow.</param>
        /// <param name="alignment">How to align <paramref name="anchored"/> relative to <paramref name="target"/>.</param>
        public Attacher(VisualElement anchored, VisualElement target, SpriteAlignment alignment)
        {
            Distance = new Vector2(6.0f, 6.0f);
            Target = target;
            Element = anchored;
            ReferenceElement = Element;
            Alignment = alignment;

            Reattach();
        }

        public void Detach()
        {
            UnregisterCallbacks();
        }

        public void Reattach()
        {
            RegisterCallbacks();
            AlignOnTarget();
        }

        void RegisterCallbacks()
        {
            UnregisterCallbacks();

            VisualElement commonAncestor = Target.FindCommonAncestor(Element);

            if (commonAncestor == Target)
            {
                Debug.Log("Attacher: Target is already parent of anchored element.");
            }
            else if (commonAncestor == Element)
            {
                Debug.Log("Attacher: An element can't be anchored to one of its descendants");
            }
            else if (commonAncestor == null)
            {
                Debug.Log("Attacher: The element and its target must be in the same visual tree hierarchy");
            }
            else
            {
                if (m_WatchedObjects == null)
                    m_WatchedObjects = new List<VisualElement>();

                VisualElement v = Target;
                while (v != commonAncestor)
                {
                    m_WatchedObjects.Add(v);
                    v.RegisterCallback<GeometryChangedEvent>(OnTargetLayout);
                    v = v.hierarchy.parent;
                }

                v = Element;

                while (v != commonAncestor)
                {
                    m_WatchedObjects.Add(v);
                    v.RegisterCallback<GeometryChangedEvent>(OnTargetLayout);
                    v = v.hierarchy.parent;
                }
            }
        }

        void UnregisterCallbacks()
        {
            if (m_WatchedObjects == null || m_WatchedObjects.Count == 0)
                return;

            foreach (VisualElement v in m_WatchedObjects)
            {
                v.UnregisterCallback<GeometryChangedEvent>(OnTargetLayout);
            }

            m_WatchedObjects.Clear();
        }

        void OnTargetLayout(GeometryChangedEvent evt)
        {
            AlignOnTarget();
        }

        void AlignOnTarget()
        {
            // Dimensions of the element to attach.
            var elementHeight = ReferenceElement == null ? Element.resolvedStyle.height : ReferenceElement.resolvedStyle.height;
            var elementWidth = ReferenceElement == null ? Element.resolvedStyle.width : ReferenceElement.resolvedStyle.width;

            // Offset used to attach to a specific part of the element (ReferenceElement), if applicable.
            var offsetX = ReferenceElement == null ? 0 : ReferenceElement.resolvedStyle.left - Element.resolvedStyle.left;
            var offsetY = ReferenceElement == null ? 0 : ReferenceElement.resolvedStyle.top - Element.resolvedStyle.top;

            var targetRect = Target.parent.ChangeCoordinatesTo(Element.hierarchy.parent, Target.layout);

            float newX;
            float newY;

            // Align horizontally
            switch (Alignment)
            {
                case SpriteAlignment.RightCenter:
                    newX = targetRect.xMax + Distance.x;
                    break;
                case SpriteAlignment.TopRight:
                case SpriteAlignment.BottomRight:
                    newX = targetRect.xMax - (elementWidth + offsetX) + Distance.x;
                    break;
                case SpriteAlignment.LeftCenter:
                    newX = targetRect.xMin - (elementWidth + offsetX) - Distance.x;
                    break;
                case SpriteAlignment.TopLeft:
                case SpriteAlignment.BottomLeft:
                    newX = targetRect.xMin - Distance.x;
                    break;
                default:
                    newX = targetRect.center.x - (elementWidth * 0.5f + offsetX);
                    break;
            }

            // Align vertically
            switch (Alignment)
            {
                case SpriteAlignment.TopRight:
                case SpriteAlignment.TopLeft:
                case SpriteAlignment.TopCenter:
                    newY = targetRect.yMin - (elementHeight + offsetY) - Distance.y;
                    break;
                case SpriteAlignment.BottomRight:
                case SpriteAlignment.BottomLeft:
                case SpriteAlignment.BottomCenter:
                    newY = targetRect.yMax + Distance.y;
                    break;
                default:
                    newY = targetRect.center.y - (elementHeight * 0.5f + offsetY);
                    break;
            }

            var newPos = new Vector2(newX, newY) + Offset;
            var previousPos = new Vector2(Element.style.left.value.value, Element.style.top.value.value);

            Element.style.position = Position.Absolute;
            Element.style.translate = new StyleTranslate(new Translate(newPos.x - previousPos.x, newPos.y - previousPos.y));
        }
    }
}
