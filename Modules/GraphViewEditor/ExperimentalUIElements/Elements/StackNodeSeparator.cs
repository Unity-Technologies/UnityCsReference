// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal abstract class StackNodeInserter : VisualElement, IInsertLocation
    {
        public VisualElement stack
        {
            get
            {
                return GetFirstAncestorOfType<StackNode>();
            }
        }

        public abstract void GetInsertInfo(Vector2 worldPosition, out InsertInfo insert);

        public StackNodeInserter()
        {
            this.AddManipulator(new Inserter());
        }
    }

    internal class StackNodePlaceholder : StackNodeInserter
    {
        private Label m_Label;

        public StackNodePlaceholder(string text)
        {
            m_Label = new Label(text);
            Add(m_Label);

            ClearClassList();
            AddToClassList("stack-node-placeholder");
        }

        public override void GetInsertInfo(Vector2 worldPosition, out InsertInfo insertInfo)
        {
            insertInfo = new InsertInfo { target = stack, index = 0, localPosition = this.ChangeCoordinatesTo(stack, rect.center) };
        }
    }

    internal class StackNodeSeparator : StackNodeInserter
    {
        private VisualElement m_HighlightItem;
        private float m_Extent;
        private float m_Height;

        public Action<ContextualMenuPopulateEvent, int> menuEvent { get; set; }

        public float extent
        {
            get
            {
                return m_Extent;
            }
            set
            {
                if (m_Extent == value)
                    return;
                m_Extent = value;
                UpdateHeight();
            }
        }

        public float height
        {
            get
            {
                return m_Height;
            }
            set
            {
                if (m_Height == value)
                    return;
                m_Height = value;
                UpdateHeight();
            }
        }

        public override void GetInsertInfo(Vector2 worldPosition, out InsertInfo insertInfo)
        {
            insertInfo = new InsertInfo { target = stack, index = parent.IndexOf(this), localPosition = this.ChangeCoordinatesTo(stack, rect.center) };
        }

        void UpdateHeight()
        {
            style.positionType = PositionType.Absolute;
            style.height = 2 * extent + height;
            m_HighlightItem.style.positionTop = extent;
            m_HighlightItem.style.positionBottom = extent;
        }

        public StackNodeSeparator()
        {
            m_HighlightItem = new VisualElement { name = "highlight" };
            m_HighlightItem.StretchToParentWidth();
            Add(m_HighlightItem);

            this.AddManipulator(new ContextualMenuManipulator(OnContextualMenuEvent));

            ClearClassList();
            AddToClassList("stack-node-separator");
        }

        void OnContextualMenuEvent(ContextualMenuPopulateEvent evt)
        {
            if (menuEvent != null)
            {
                InsertInfo insertInfo;
                GetInsertInfo(evt.mousePosition, out insertInfo);
                menuEvent(evt, insertInfo.index);
            }
        }
    }

    internal class StackNodeContentContainer : StackNodeInserter
    {
        public StackNodeContentContainer()
        {
            clippingOptions = ClippingOptions.NoClipping;
        }

        public override void GetInsertInfo(Vector2 worldPosition, out InsertInfo insertInfo)
        {
            insertInfo = new InsertInfo { target = stack, index = 0, localPosition = Vector2.zero };

            foreach (VisualElement child in stack.Children())
            {
                Vector2 localPos = child.WorldToLocal(worldPosition);

                if (child.ContainsPoint(localPos))
                {
                    insertInfo.index = stack.IndexOf(child);
                    insertInfo.localPosition = child.ChangeCoordinatesTo(stack, child.rect.center);
                }
            }
        }
    }
}
