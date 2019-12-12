// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Experimental.GraphView
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

        private static CustomStyleProperty<float> s_SeparatorHeight = new CustomStyleProperty<float>("--separator-height");
        private static CustomStyleProperty<float> s_SeparatorExtent = new CustomStyleProperty<float>("--separator-extent");

        private float m_SeparatorHeight = 4f;
        private float separatorHeight => m_SeparatorHeight;

        private float m_SeparatorExtent = 15f;
        private float separatorExtent => m_SeparatorExtent;

        public StackNode() : base("UXML/GraphView/StackNode.uxml")
        {
            VisualElement stackNodeContentContainerPlaceholder = this.Q("stackNodeContentContainerPlaceholder");

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

        protected override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt.eventTypeId == GeometryChangedEvent.TypeId())
            {
                UpdateSeparators();
            }
            else if (evt.eventTypeId == DetachFromPanelEvent.TypeId())
            {
                graphView = null;
            }
            else if (evt.eventTypeId == AttachToPanelEvent.TypeId())
            {
                graphView = GetFirstAncestorOfType<GraphView>();

                if (graphView != null)
                {
                    // Restore selections on children.
                    foreach (var child in Children().OfType<GraphElement>())
                    {
                        graphView.RestorePersitentSelectionForElement(child);
                    }
                }
            }
        }

        private bool AcceptsElementInternal(GraphElement element, ref int proposedIndex, int maxIndex)
        {
            // TODO: we probably need a "Stackable" capability
            return element != null && !(element is Scope)
                && !(element is StackNode) && !(element is TokenNode)
                && !(element is Placemat)
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

        protected override void OnCustomStyleResolved(ICustomStyle styles)
        {
            base.OnCustomStyleResolved(styles);

            int animDurationValue = 0;
            float heightValue = 0f;
            float extentValue = 0f;

            if (styles.TryGetValue(s_AnimationDuration, out animDurationValue))
                m_AnimationDuration = animDurationValue;

            if (styles.TryGetValue(s_SeparatorHeight, out heightValue))
                m_SeparatorHeight = heightValue;

            if (styles.TryGetValue(s_SeparatorExtent, out extentValue))
                m_SeparatorExtent = extentValue;

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
                    separatorCenterY = m_SeparatorContainer.layout.height - separatorHeight / 2;
                }
                separator.style.top = separatorCenterY - separator.resolvedStyle.height / 2;
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

        public virtual int GetInsertionIndex(Vector2 worldPosition)
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

        public override void CollectElements(HashSet<GraphElement> collectedElementSet, Func<GraphElement, bool> conditionFunc)
        {
            base.CollectElements(collectedElementSet, conditionFunc);
            GraphView.CollectElements(Children().OfType<GraphElement>(), collectedElementSet, conditionFunc);
        }
    }
}
