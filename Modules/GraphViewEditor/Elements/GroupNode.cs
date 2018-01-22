// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal class GroupNodeDropArea : VisualElement, IDropTarget
    {
        public GroupNodeDropArea()
        {
            name = "GroupNodeDropAreaName";
        }

        public bool CanAcceptDrop(List<ISelectable> selection)
        {
            if (selection.Count == 0)
                return false;

            foreach (ISelectable selectable in selection)
            {
                var selectedElement = selectable as GraphElement;

                if (selectedElement == null || selectedElement is GroupNode)
                {
                    return false;
                }
            }

            return true;
        }

        public EventPropagation DragExited()
        {
            RemoveFromClassList("dragEntered");

            return EventPropagation.Continue;
        }

        public EventPropagation DragPerform(IMGUIEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget)
        {
            GroupNode group = parent.GetFirstAncestorOfType<GroupNode>();

            foreach (ISelectable selectedElement in selection)
            {
                if (selectedElement != group)
                {
                    var selectedGraphElement = selectedElement as GraphElement;

                    if (group.ContainsElement(selectedGraphElement) || selectedGraphElement.GetContainingGroupNode() != null)
                        continue;

                    group.AddElement(selectedGraphElement);
                }
            }

            RemoveFromClassList("dragEntered");

            return EventPropagation.Stop;
        }

        public EventPropagation DragUpdated(IMGUIEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget)
        {
            GroupNode group = parent.GetFirstAncestorOfType<GroupNode>();
            bool canDrop = false;

            foreach (ISelectable selectedElement in selection)
            {
                if (selectedElement == group)
                    continue;

                var selectedGraphElement = selectedElement as GraphElement;
                Event e = evt.imguiEvent;

                if (e.shift)
                {
                    if (group.ContainsElement(selectedGraphElement))
                    {
                        group.RemoveElement(selectedGraphElement);
                    }
                }
                else
                {
                    if (!group.ContainsElement(selectedGraphElement) && selectedGraphElement.GetContainingGroupNode() == null)
                    {
                        canDrop = true;
                    }
                }
            }

            if (canDrop)
            {
                AddToClassList("dragEntered");
            }
            else
            {
                RemoveFromClassList("dragEntered");
            }

            return EventPropagation.Stop;
        }
    }

    public class GroupNode : GraphElement
    {
        private const int k_TitleItemMinWidth = 10;
        private VisualElement m_MainContainer;
        private VisualElement m_HeaderItem;
        private Label m_TitleItem;
        private TextField m_TitleEditor;
        private VisualElement m_ContentItem;
        static readonly List<GraphElement> s_EmptyList = new List<GraphElement>();
        private List<GraphElement> m_ContainedElements;
        private Rect m_ContainedElementsRect;
        private bool m_IsUpdatingGeometryFromContent = false;
        private bool m_IsMovingElements = false;
        bool m_Initialized = false;
        bool m_FirstRepaint = true;
        bool m_HeaderSizeIsValid = false;
        bool m_EditTitleCancelled = false;
        Vector2 mPreviousPosInCanvasSpace = new Vector2();

        public string title
        {
            get { return m_TitleItem.text; }
            set
            {
                if (!m_TitleItem.Equals(value))
                {
                    m_TitleItem.text = value;

                    GraphView gv = GetFirstAncestorOfType<GraphView>();

                    if (gv != null && gv.groupNodeTitleChanged != null)
                    {
                        gv.groupNodeTitleChanged(this, value);
                    }

                    UpdateGeometryFromContent();
                }
            }
        }

        public List<GraphElement> containedElements { get { return m_ContainedElements != null ? m_ContainedElements : s_EmptyList; } }
        public Rect containedElementsRect { get { return m_ContainedElementsRect; } }

        public GroupNode()
        {
            m_ContentItem = new GroupNodeDropArea();
            m_ContentItem.ClearClassList();
            m_ContentItem.AddToClassList("content");

            var visualTree = EditorGUIUtility.Load("UXML/GraphView/GroupNode.uxml") as VisualTreeAsset;

            m_MainContainer = visualTree.CloneTree(null);
            m_MainContainer.AddToClassList("mainContainer");

            m_HeaderItem = m_MainContainer.Q(name: "header");
            m_HeaderItem.AddToClassList("header");

            m_TitleItem = m_MainContainer.Q<Label>(name: "titleLabel");
            m_TitleItem.AddToClassList("label");

            m_TitleEditor = m_MainContainer.Q(name: "titleField") as TextField;

            m_TitleEditor.AddToClassList("textfield");
            m_TitleEditor.visible = false;

            m_TitleEditor.RegisterCallback<FocusOutEvent>(e => { OnEditTitleFinished(); });
            m_TitleEditor.RegisterCallback<KeyDownEvent>(OnKeyPressed);

            VisualElement contentPlaceholder = m_MainContainer.Q(name: "contentPlaceholder");

            contentPlaceholder.Add(m_ContentItem);

            Add(m_MainContainer);

            ClearClassList();
            AddToClassList("groupNode");

            clippingOptions = ClippingOptions.ClipAndCacheContents;
            capabilities |= Capabilities.Selectable | Capabilities.Movable | Capabilities.Deletable;

            m_HeaderItem.RegisterCallback<PostLayoutEvent>(OnHeaderSizeChanged);
            RegisterCallback<PostLayoutEvent>(e => { MoveElements(); });
            RegisterCallback<MouseDownEvent>(OnMouseUpEvent);

            this.schedule.Execute(e => {
                    if (visible && (m_Initialized == false))
                    {
                        m_Initialized = true;

                        UpdateGeometryFromContent();
                    }
                });
        }

        void OnSubElementPostLayout(PostLayoutEvent e)
        {
            if (this.IsSelected(GetFirstAncestorOfType<GraphView>()) == false)
            {
                UpdateGeometryFromContent();
            }
        }

        void OnSubElementDetachedFromPanel(DetachFromPanelEvent evt)
        {
            // Do nothing if the group is not in a panel
            if (panel == null)
                return;

            GraphElement element = evt.target as GraphElement;

            RemoveElement(element);
        }

        private static bool IsValidSize(Vector2 size)
        {
            return size.x > 0 && !Single.IsNaN(size.x) && size.y > 0 && !Single.IsNaN(size.y);
        }

        static bool IsValidRect(Rect rect)
        {
            return !Single.IsNaN(rect.x)  && !Single.IsNaN(rect.y)  && rect.width > 0 && !Single.IsNaN(rect.width) && rect.height > 0 && !Single.IsNaN(rect.height);
        }

        private void OnHeaderSizeChanged(PostLayoutEvent e)
        {
            if (!m_HeaderSizeIsValid && (IsValidRect(m_HeaderItem.layout)))
            {
                UpdateGeometryFromContent();

                m_HeaderSizeIsValid = true;

                m_HeaderItem.UnregisterCallback<PostLayoutEvent>(OnHeaderSizeChanged);
            }
        }

        private void OnKeyPressed(KeyDownEvent e)
        {
            switch (e.keyCode)
            {
                case KeyCode.Escape:
                    m_EditTitleCancelled = true;
                    m_TitleEditor.Blur();
                    break;
                case KeyCode.Return:
                    m_TitleEditor.Blur();
                    break;
                default:
                    break;
            }
        }

        private void OnEditTitleFinished()
        {
            m_TitleItem.visible = true;
            m_TitleEditor.visible = false;

            if (!m_EditTitleCancelled)
            {
                if (title != m_TitleEditor.text)
                {
                    title = m_TitleEditor.text;
                    UpdateGeometryFromContent();
                }
            }

            m_EditTitleCancelled = false;
        }

        private void OnMouseUpEvent(MouseDownEvent e)
        {
            if (e.clickCount == 2)
            {
                if (HitTest(e.localMousePosition))
                {
                    m_TitleEditor.text = title;
                    m_TitleEditor.visible = true;
                    m_TitleItem.visible = false;
                    // Workaround: Wait for a delay before giving focus to the newly shown title editor
                    this.schedule.Execute(GiveFocusToTitleEditor).StartingIn(300);
                }
            }
        }

        private void GiveFocusToTitleEditor()
        {
            m_TitleEditor.SelectAll();
            m_TitleEditor.Focus();
        }

        public override bool HitTest(Vector2 localPoint)
        {
            Vector2 mappedPoint = this.ChangeCoordinatesTo(m_HeaderItem, localPoint);

            return m_HeaderItem.ContainsPoint(mappedPoint);
        }

        public override bool Overlaps(Rect rectangle)
        {
            Rect mappedRect = this.ChangeCoordinatesTo(m_HeaderItem, rectangle);

            return m_HeaderItem.Overlaps(mappedRect);
        }

        public bool ContainsElement(GraphElement element)
        {
            return (m_ContainedElements != null) ? m_ContainedElements.Contains(element) : false;
        }

        public void AddElement(GraphElement element)
        {
            if (element == null)
                throw new ArgumentException("Cannot add null element");

            if (element is GroupNode)
                throw new ArgumentException("Nested group node is not supported yet.");

            if (m_ContainedElements == null)
            {
                m_ContainedElements = new List<GraphElement>();
            }
            else if (m_ContainedElements.Contains(element))
            {
                throw new ArgumentException("The element is already contained in this group node.");
            }

            // Removes the element from its current group
            GroupNode currentGroup = element.GetContainingGroupNode();

            if (currentGroup != null)
            {
                currentGroup.RemoveElement(element);
            }

            m_ContainedElements.Add(element);

            // To update the group geometry whenever the added element's geometry changes
            element.RegisterCallback<PostLayoutEvent>(OnSubElementPostLayout);
            element.RegisterCallback<DetachFromPanelEvent>(OnSubElementDetachedFromPanel);

            UpdateGeometryFromContent();

            GraphView gv = GetFirstAncestorOfType<GraphView>();

            if (gv != null && gv.elementAddedToGroupNode != null)
            {
                gv.elementAddedToGroupNode(this, element);
            }
        }

        public void RemoveElement(GraphElement element)
        {
            if (element == null)
                throw new ArgumentException("Cannot remove null element from this group");

            if (m_ContainedElements == null)
                return;

            if (!m_ContainedElements.Contains(element))
                throw new ArgumentException("This element is not contained in this group");

            m_ContainedElements.Remove(element);
            element.UnregisterCallback<PostLayoutEvent>(OnSubElementPostLayout);
            element.UnregisterCallback<DetachFromPanelEvent>(OnSubElementDetachedFromPanel);
            UpdateGeometryFromContent();

            GraphView gv = GetFirstAncestorOfType<GraphView>();

            if (gv != null && gv.elementRemovedFromGroupNode != null)
            {
                gv.elementRemovedFromGroupNode(this, element);
            }
        }

        void MoveElements()
        {
            if (panel == null || !m_Initialized)
                return;

            GraphView graphView = GetFirstAncestorOfType<GraphView>();
            VisualElement viewport = graphView.contentViewContainer;
            Vector2 newPosInCanvasSpace = this.ChangeCoordinatesTo(viewport, new Vector2(0, 0));

            if (mPreviousPosInCanvasSpace == newPosInCanvasSpace)
            {
                return;
            }

            float dX = newPosInCanvasSpace.x - mPreviousPosInCanvasSpace.x;
            float dY = newPosInCanvasSpace.y - mPreviousPosInCanvasSpace.y;

            mPreviousPosInCanvasSpace = newPosInCanvasSpace;

            MoveElements(dX, dY);
        }

        void MoveElements(float deltaX, float deltaY)
        {
            if (m_ContainedElements != null)
            {
                m_IsMovingElements = true;

                for (int i = 0; i < m_ContainedElements.Count; ++i)
                {
                    GraphElement subElement = m_ContainedElements[i];

                    if (m_IsUpdatingGeometryFromContent == false)
                    {
                        Rect currentPosition = subElement.GetPosition();

                        subElement.SetPosition(new Rect(currentPosition.x + deltaX, currentPosition.y + deltaY, currentPosition.width, currentPosition.height));
                    }
                }

                m_IsMovingElements = false;
            }
        }

        public void OnPositionChanged(VisualElement ve)
        {
            if (ve == this)
            {
                MoveElements();
            }
            else
            {
                UpdateGeometryFromContent();
            }
        }

        public override void DoRepaint()
        {
            if (m_FirstRepaint)
            {
                m_FirstRepaint = false;
                m_Initialized = true;

                UpdateGeometryFromContent();
            }
            base.DoRepaint();
        }

        public void UpdateGeometryFromContent()
        {
            if (panel == null || !m_Initialized || m_IsUpdatingGeometryFromContent || m_IsMovingElements)
            {
                return;
            }

            GraphView graphView = GetFirstAncestorOfType<GraphView>();
            if (graphView == null)
            {
                return;
            }

            m_IsUpdatingGeometryFromContent = true;

            VisualElement viewport = graphView.contentViewContainer;
            Rect contentRectInViewportSpace = Rect.zero;

            // Compute the bounding box of the content of the group in viewport space (because nodes are not parented by the group that contains them)
            if (m_ContainedElements != null)
            {
                for (int i = 0; i < m_ContainedElements.Count; ++i)
                {
                    GraphElement subElement = m_ContainedElements[i];

                    if (subElement.panel != panel)
                        continue;

                    Rect boundingRect = new Rect(0, 0, subElement.GetPosition().width, subElement.GetPosition().height);

                    if (IsValidRect(boundingRect))
                    {
                        boundingRect = subElement.ChangeCoordinatesTo(viewport, boundingRect);

                        // Use the first element with a valid geometry as reference to compute the bounding box of contained elements
                        if (!IsValidRect(contentRectInViewportSpace))
                        {
                            contentRectInViewportSpace = boundingRect;
                        }
                        else
                        {
                            contentRectInViewportSpace = RectUtils.Encompass(contentRectInViewportSpace, boundingRect);
                        }
                    }
                }
            }

            if ((m_ContainedElements == null) || (m_ContainedElements.Count == 0))
            {
                float contentX = m_ContentItem.style.borderLeftWidth.value + m_ContentItem.style.paddingLeft.value;
                float contentY = m_HeaderItem.layout.height + m_ContentItem.style.borderTopWidth.value + m_ContentItem.style.paddingTop.value;

                contentRectInViewportSpace = this.ChangeCoordinatesTo(viewport, new Rect(contentX, contentY, 0, 0));
            }

            float titleItemImplicitWidth = k_TitleItemMinWidth;

            if (m_HeaderItem != null)
            {
                Vector2 implicitSize = m_TitleItem.DoMeasure(100, MeasureMode.Undefined, 100, MeasureMode.Undefined);

                if (IsValidSize(implicitSize))
                {
                    titleItemImplicitWidth = implicitSize.x + m_TitleItem.style.marginLeft.value + m_TitleItem.style.paddingLeft.value
                        + m_TitleItem.style.paddingRight.value + m_TitleItem.style.marginRight.value;
                }
            }

            float headerItemImplicitWidth = titleItemImplicitWidth  + m_HeaderItem.style.paddingLeft.value + m_HeaderItem.style.paddingRight.value;

            Vector2 contentRectSize = contentRectInViewportSpace.size;

            contentRectSize.x += m_ContentItem.style.borderLeftWidth.value + m_ContentItem.style.paddingLeft.value + m_ContentItem.style.paddingRight.value + m_ContentItem.style.borderRightWidth.value;
            contentRectSize.y += m_ContentItem.style.borderTopWidth.value + m_ContentItem.style.paddingTop.value + m_ContentItem.style.paddingBottom.value + m_ContentItem.style.borderBottomWidth.value;

            Rect groupGeometry = new Rect();

            groupGeometry.position = viewport.ChangeCoordinatesTo(parent, contentRectInViewportSpace.position);
            groupGeometry.width = Math.Max(contentRectSize.x, headerItemImplicitWidth) + style.borderLeftWidth.value + style.borderRightWidth.value; // Ensure that the title is always visible
            groupGeometry.height = contentRectSize.y + m_HeaderItem.layout.height + style.borderTopWidth.value + style.borderBottomWidth.value;

            groupGeometry.x -= m_ContentItem.style.paddingLeft.value + style.borderLeftWidth.value;
            groupGeometry.y -= m_ContentItem.style.paddingTop.value + m_HeaderItem.layout.height + style.borderTopWidth.value;

            SetPosition(groupGeometry);

            Vector2 newPosInCanvasSpace = this.ChangeCoordinatesTo(viewport, new Vector2(0, 0));
            mPreviousPosInCanvasSpace = newPosInCanvasSpace;
            m_ContainedElementsRect = viewport.ChangeCoordinatesTo(this, contentRectInViewportSpace);
            m_IsUpdatingGeometryFromContent = false;
        }
    }
}
