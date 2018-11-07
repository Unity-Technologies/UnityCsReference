// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    public partial class StackNode : Node
    {
        private VisualElement m_ContentContainer;
        private VisualElement m_SeparatorContainer;
        private VisualElement m_PlaceholderContainer;
        private StackNodePlaceholder m_Placeholder;

        public VisualElement headerContainer { get; private set; }
        public override VisualElement contentContainer => m_ContentContainer;

        internal GraphView graphView { get; set; }

        private const string k_SeparatorHeight = "separator-height";
        private const string k_SeparatorExtent = "separator-extent";

        private StyleValue<float> m_SeparatorHeight;
        private float separatorHeight => m_SeparatorHeight.GetSpecifiedValueOrDefault(4f);

        private StyleValue<float> m_SeparatorExtent;
        private float separatorExtent => m_SeparatorExtent.GetSpecifiedValueOrDefault(15f);

        public StackNode() : base("UXML/GraphView/StackNode.uxml")
        {
            this.Q("stackNodeContainers").clippingOptions = ClippingOptions.NoClipping;

            VisualElement stackNodeContentContainerPlaceholder = this.Q("stackNodeContentContainerPlaceholder");
            stackNodeContentContainerPlaceholder.clippingOptions = ClippingOptions.NoClipping;

            headerContainer = this.Q("stackNodeHeaderContainer");
            m_SeparatorContainer = this.Q("stackSeparatorContainer");
            m_PlaceholderContainer = this.Q("stackPlaceholderContainer");
            m_PlaceholderContainer.Add(m_Placeholder = new StackNodePlaceholder("Spacebar to Add Node"));

            m_ContentContainer = new StackNodeContentContainer();
            m_ContentContainer.name = "stackNodeContentContainer";

            stackNodeContentContainerPlaceholder.Add(m_ContentContainer);

            ClearClassList();
            AddToClassList("stack-node");
            AddStyleSheetPath("StyleSheets/GraphView/StackNode.uss");
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt.GetEventTypeId() == GeometryChangedEvent.TypeId())
            {
                UpdateSeparators();
            }
            else if (evt.GetEventTypeId() == DetachFromPanelEvent.TypeId())
            {
                graphView = null;
            }
            else if (evt.GetEventTypeId() == AttachToPanelEvent.TypeId())
            {
                graphView = GetFirstAncestorOfType<GraphView>();
            }
        }

        private bool AcceptsElementInternal(GraphElement element, ref int proposedIndex, int maxIndex)
        {
            return element != null && !(element is Scope)
                && !(element is StackNode) && !(element is TokenNode)
                && (element.GetContainingScope() as Group) == null
                && AcceptsElement(element, ref proposedIndex, maxIndex);
        }

        protected virtual bool AcceptsElement(GraphElement element, ref int proposedIndex, int maxIndex)
        {
            return true;
        }

        public void AddElement(GraphElement element)
        {
            InsertElement(childCount, element);
        }

        public void InsertElement(int index, GraphElement element)
        {
            if (!AcceptsElementInternal(element, ref index, childCount))
            {
                return;
            }

            Insert(index, element);
            OnChildAdded(element);

            if (graphView != null)
            {
                graphView.RestorePersitentSelectionForElement(element);
            }
        }

        public void RemoveElement(GraphElement element)
        {
            Remove(element);
        }

        protected override void OnStyleResolved(ICustomStyle styles)
        {
            base.OnStyleResolved(styles);

            styles.ApplyCustomProperty(k_AnimationDuration, ref m_AnimationDuration);
            styles.ApplyCustomProperty(k_SeparatorHeight, ref m_SeparatorHeight);
            styles.ApplyCustomProperty(k_SeparatorExtent, ref m_SeparatorExtent);

            schedule.Execute(a => UpdateSeparators());
        }

        private void UpdateSeparators()
        {
            int expectedSeparatorCount = childCount > 0 ? childCount + 1 : 0;

            // If there are missing separators then add them
            if (m_SeparatorContainer.childCount < expectedSeparatorCount)
            {
                for (int i = m_SeparatorContainer.childCount; i < expectedSeparatorCount; ++i)
                {
                    var separator = new StackNodeSeparator { menuEvent = ExecuteOnSeparatorContextualMenuEvent };
                    separator.StretchToParentWidth();
                    m_SeparatorContainer.Add(separator);
                }
            }

            // If there are exceeding separators then remove them
            if (m_SeparatorContainer.childCount > expectedSeparatorCount)
            {
                for (int i = m_SeparatorContainer.childCount - 1; i >= expectedSeparatorCount; --i)
                {
                    m_SeparatorContainer[i].RemoveFromHierarchy();
                }
            }

            // Updates the geometry of each separator
            for (int i = 0; i < m_SeparatorContainer.childCount; ++i)
            {
                var separator = m_SeparatorContainer[i] as StackNodeSeparator;

                separator.extent = separatorExtent;
                separator.height = separatorHeight;

                float separatorCenterY = 0;

                // For the first separator, use the top of the first element
                if (i == 0)
                {
                    VisualElement firstElement = this[i];

                    separatorCenterY = separatorHeight / 2;
                }
                // .. for the other separators, use the spacing between the current and the next separators
                else if (i < m_SeparatorContainer.childCount - 1)
                {
                    VisualElement element = this[i - 1];
                    VisualElement nextElement = this[i];

                    separatorCenterY = (nextElement.layout.yMin + element.layout.yMax) / 2;
                }
                // .. for the last separator, use the bottom of the container
                else
                {
                    VisualElement element = this[i - 1];

                    separatorCenterY = m_SeparatorContainer.layout.height - separatorHeight / 2;
                }
                separator.style.positionTop = separatorCenterY - separator.style.height / 2;
            }
        }

        private void OnChildAdded(GraphElement element)
        {
            element.AddToClassList("stack-child-element");
            element.ResetPositionProperties();
            element.RegisterCallback<DetachFromPanelEvent>(OnChildDetachedFromPanel);
            UpdatePlaceholderVisibility();
        }

        private void OnChildRemoved(GraphElement element)
        {
            element.RemoveFromClassList("stack-child-element");
            element.UnregisterCallback<DetachFromPanelEvent>(OnChildDetachedFromPanel);

            // Disable the animation temporarily
            if (m_InstantAdd == false)
            {
                m_InstantAdd = true;
                schedule.Execute(() => m_InstantAdd = false);
            }
            UpdatePlaceholderVisibility();
        }

        private void UpdatePlaceholderVisibility()
        {
            if (childCount != 0)
            {
                m_Placeholder.RemoveFromHierarchy();
            }
            else
            {
                if (m_Placeholder.parent == null)
                {
                    m_PlaceholderContainer.Add(m_Placeholder);
                }
            }
        }

        private void OnChildDetachedFromPanel(DetachFromPanelEvent evt)
        {
            if (panel == null)
                return;

            GraphElement element = evt.target as GraphElement;

            OnChildRemoved(element);
        }

        private void ExecuteOnSeparatorContextualMenuEvent(ContextualMenuPopulateEvent evt, int separatorIndex)
        {
            if (evt.target is StackNodeSeparator)
            {
                OnSeparatorContextualMenuEvent(evt, separatorIndex);
            }
            evt.StopPropagation();
        }

        protected virtual void OnSeparatorContextualMenuEvent(ContextualMenuPopulateEvent evt, int separatorIndex)
        {
        }

        public int GetInsertionIndex(Vector2 worldPosition)
        {
            var ve = graphView.currentInsertLocation as VisualElement;
            if (ve == null)
                return -1;

            // Checking if it's one of our children
            if (this == ve.GetFirstAncestorOfType<StackNode>())
            {
                InsertInfo insertInfo;
                graphView.currentInsertLocation.GetInsertInfo(worldPosition, out insertInfo);
                return insertInfo.index;
            }

            return -1;
        }

        public virtual void OnStartDragging(GraphElement ge)
        {
            var node = ge as Node;
            if (node != null)
            {
                ge.RemoveFromHierarchy();

                graphView.AddElement(ge);
                // Reselect it because RemoveFromHierarchy unselected it
                ge.Select(graphView, true);
            }
        }
    }
}
