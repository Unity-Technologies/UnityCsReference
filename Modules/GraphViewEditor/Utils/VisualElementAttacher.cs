// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    public class Attacher
    {
        public VisualElement target { get; private set; }
        public VisualElement element { get; private set; }

        public SpriteAlignment alignment
        {
            get { return m_Alignment; }
            set
            {
                if (m_Alignment != value)
                {
                    m_Alignment = value;
                    if (isAttached)
                    {
                        AlignOnTarget();
                    }
                }
            }
        }

        public Vector2 offset
        {
            get { return m_Offset; }
            set
            {
                if (m_Offset != value)
                {
                    m_Offset = value;
                    if (isAttached)
                    {
                        AlignOnTarget();
                    }
                }
            }
        }

        public float distance
        {
            get { return m_Distance; }
            set
            {
                if (m_Distance != value)
                {
                    m_Distance = value;
                    if (isAttached)
                    {
                        AlignOnTarget();
                    }
                }
            }
        }

        private bool isAttached
        {
            get { return target != null && element != null && m_WatchedObjects != null && m_WatchedObjects.Count > 0; }
        }

        private Rect m_LastTargetWorldBounds = Rect.zero;
        private Rect m_ElementSize = Rect.zero;
        private List<VisualElement> m_WatchedObjects;
        private Vector2 m_Offset;
        private SpriteAlignment m_Alignment;
        private float m_Distance;

        public Attacher(VisualElement anchored, VisualElement target, SpriteAlignment alignment)
        {
            this.distance = 6.0f;
            this.target = target;
            this.element = anchored;
            this.alignment = alignment;

            Reattach();
        }

        public void Detach()
        {
            UnregisterCallbacks();
            m_ElementSize = m_LastTargetWorldBounds = Rect.zero;
        }

        public void Reattach()
        {
            RegisterCallbacks();
            UpdateTargetBounds();
            AlignOnTarget();
        }

        private void RegisterCallbacks()
        {
            if (m_WatchedObjects != null && m_WatchedObjects.Count > 0)
            {
                UnregisterCallbacks();
            }

            VisualElement commonAncestor = target.FindCommonAncestor(element);

            if (commonAncestor == target)
            {
                Debug.Log("Attacher: Target is already parent of anchored element.");
            }
            else if (commonAncestor == element)
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

                VisualElement v = target;

                while (v != commonAncestor)
                {
                    m_WatchedObjects.Add(v);
                    v.RegisterCallback<PostLayoutEvent>(OnTargetLayout);
                    v = v.shadow.parent;
                }

                v = element;

                while (v != commonAncestor)
                {
                    m_WatchedObjects.Add(v);
                    v.RegisterCallback<PostLayoutEvent>(OnTargetLayout);
                    v = v.shadow.parent;
                }
            }
        }

        private void UnregisterCallbacks()
        {
            foreach (VisualElement v in m_WatchedObjects)
            {
                {
                    v.UnregisterCallback<PostLayoutEvent>(OnTargetLayout);
                }
            }

            m_WatchedObjects.Clear();
        }

        private void OnTargetLayout(PostLayoutEvent evt)
        {
            if (UpdateTargetBounds() || UpdateElementSize())
            {
                AlignOnTarget();
            }
        }

        private bool UpdateTargetBounds()
        {
            Rect targetRect = target.worldBound;

            if (m_LastTargetWorldBounds == targetRect)
            {
                return false;
            }

            m_LastTargetWorldBounds = targetRect;
            return true;
        }

        private bool UpdateElementSize()
        {
            Rect elemRect = element.worldBound;

            elemRect.position = Vector2.zero;
            if (m_ElementSize == elemRect)
            {
                return false;
            }

            m_ElementSize = elemRect;
            return true;
        }

        private void AlignOnTarget()
        {
            Rect currentRect = new Rect(element.style.positionLeft, element.style.positionTop, element.style.width, element.style.height);
            Rect targetRect = target.rect;
            targetRect = target.ChangeCoordinatesTo(element.shadow.parent, targetRect);

            float centerY = 0;
            //align Vertically
            switch (alignment)
            {
                case SpriteAlignment.TopLeft:
                case SpriteAlignment.TopCenter:
                case SpriteAlignment.TopRight:
                    centerY = targetRect.y - currentRect.height * 0.5f - distance;
                    break;
                case SpriteAlignment.LeftCenter:
                case SpriteAlignment.RightCenter:
                case SpriteAlignment.Center:
                    centerY = targetRect.center.y;
                    break;
                case SpriteAlignment.BottomLeft:
                case SpriteAlignment.BottomCenter:
                case SpriteAlignment.BottomRight:
                    centerY = targetRect.yMax + currentRect.height * 0.5f + distance;
                    break;
            }

            float centerX = 0;
            //alignHorizontally
            switch (alignment)
            {
                case SpriteAlignment.TopLeft:
                case SpriteAlignment.LeftCenter:
                case SpriteAlignment.BottomLeft:
                    centerX = targetRect.x - currentRect.width * 0.5f - distance;
                    break;
                case SpriteAlignment.TopCenter:
                case SpriteAlignment.Center:
                case SpriteAlignment.BottomCenter:
                    centerX = targetRect.center.x;
                    break;
                case SpriteAlignment.TopRight:
                case SpriteAlignment.RightCenter:
                case SpriteAlignment.BottomRight:
                    centerX = targetRect.xMax + currentRect.width * 0.5f + distance;
                    break;
            }

            currentRect.center = new Vector2(centerX, centerY) + offset;

            m_ElementSize.width = currentRect.width;
            m_ElementSize.height = currentRect.height;

            //we don't want the layout to be overwritten before styling has been applied
            if (currentRect.width > 0)
            {
                element.layout = currentRect;
            }
            else
            {
                element.style.positionLeft = currentRect.xMin;
                element.style.positionTop = currentRect.yMin;
            }

            m_LastTargetWorldBounds = target.worldBound;
        }
    }
}
