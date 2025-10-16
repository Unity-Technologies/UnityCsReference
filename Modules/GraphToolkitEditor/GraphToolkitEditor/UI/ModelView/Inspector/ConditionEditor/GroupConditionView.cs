// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.CSO;
using Unity.GraphToolkit.InternalBridge;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The view for a <see cref="GroupConditionModel"/>.
    /// </summary>
    [UnityRestricted]
    internal class GroupConditionView : ConditionView
    {
        /// <summary>
        /// The USS class name added to this element.
        /// </summary>
        public new static readonly string ussClassName = "ge-group-condition-view";

        /// <summary>
        /// The USS class name added to this element when it displays an and condition.
        /// </summary>
        public static readonly string andUssClassName = ussClassName.WithUssModifier("and");

        /// <summary>
        /// The USS class name added to this element when it displays an or condition.
        /// </summary>
        public static readonly string orUssClassName = ussClassName.WithUssModifier("or");

        /// <summary>
        /// The USS class name added to the title.
        /// </summary>
        public static readonly string titleUssClassName = ussClassName.WithUssElement(GraphElementHelper.titleName);

        /// <summary>
        /// The USS class name added to the title container.
        /// </summary>
        public static readonly string titleContainerUssClassName = ussClassName.WithUssElement(GraphElementHelper.titleContainerName);

        /// <summary>
        /// The USS class name added to the condition operation drop down.
        /// </summary>
        public static readonly string conditionPopupUssClassName = ussClassName.WithUssElement("condition-popup");

        /// <summary>
        /// The USS class name added to the container.
        /// </summary>
        public static readonly string containerUssClassName = ussClassName.WithUssElement(GraphElementHelper.containerName);

        /// <summary>
        /// The USS class name added to the drag block.
        /// </summary>
        public static readonly string dragBlockUssClassName = ussClassName.WithUssElement("drag-block");

        /// <summary>
        /// The USS class name added to the lines layer.
        /// </summary>
        public static readonly string linesLayerUssName = ussClassName.WithUssElement("lines-layer");

        /// <summary>
        /// The USS class name added to the title when it is hovered.
        /// </summary>
        public static readonly string hoveredTitleUssClassName = titleUssClassName.WithUssModifier("hover");

        /// <summary>
        /// The title container element.
        /// </summary>
        protected VisualElement m_TitleContainer;

        /// <summary>
        /// The title element.
        /// </summary>
        VisualElement m_Title;

        /// <summary>
        /// The empty warning element.
        /// </summary>
        protected VisualElement m_EmptyWarning;

        /// <summary>
        /// The empty label element.
        /// </summary>
        protected Label m_EmptyLabel;

        EnumField m_ConditionPopup;
        VisualElement m_LinesLayer;
        VisualElement m_DragBlock;
        VisualElement m_Container;
        VisualElement m_IndentationSpacer;

        const float k_XOffset = 9.0f;
        const float k_ConditionTitleHeight = 14.0f;
        bool IsClone => Context is DragCloneCreationContext;

        List<ConditionView> m_SubConditions;

        /// <summary>
        /// The list of sub-condition views.
        /// </summary>
        public IReadOnlyList<ConditionView> SubConditions => m_SubConditions;

        /// <summary>
        /// The <see cref="GroupConditionModel"/> displayed by this view.
        /// </summary>
        public GroupConditionModel GroupConditionModel => (GroupConditionModel)Model;

        /// <summary>
        /// The condition editor for the view.
        /// </summary>
        protected ConditionEditor ConditionEditor { get; }

        /// <inheritdoc />
        protected override VisualElement IndentationSpacer => m_IndentationSpacer;

        /// <summary>
        /// Creates an instance of <see cref="GroupConditionView"/>.
        /// </summary>
        /// <param name="conditionEditor">The containing <see cref="ConditionEditor"/>.</param>
        public GroupConditionView(ConditionEditor conditionEditor)
        {
            ConditionEditor = conditionEditor;
            m_SubConditions = new List<ConditionView>();
        }

        protected override void BuildUI()
        {
            base.BuildUI();
            m_Title = new VisualElement();
            m_Title.AddToClassList(titleUssClassName);
            Add(m_Title);

            m_TitleContainer = new VisualElement();
            m_TitleContainer.AddToClassList(titleContainerUssClassName);

            m_TitleContainer.Add(m_Icon);

            var dragHandle = new VisualElement();
            dragHandle.AddToClassList(dragHandleUssClassName);
            m_TitleContainer.Add(dragHandle);


            m_IndentationSpacer = new VisualElement();
            m_TitleContainer.Add(m_IndentationSpacer);

            var label = new Label("Logic Group");
            label.AddToClassList(ussClassName.WithUssElement(GraphElementHelper.labelName));
            m_TitleContainer.Add(label);

            m_ConditionPopup = new EnumField(GroupConditionModel.Operation.And);
            m_ConditionPopup.AddToClassList(conditionPopupUssClassName);
            m_ConditionPopup.RegisterValueChangedCallback(
                _ =>
                {
                    RootView.Dispatch(new SetGroupConditionOperationCommand(
                        GroupConditionModel,
                        (GroupConditionModel.Operation)m_ConditionPopup.value));
                });
            m_TitleContainer.Add(m_ConditionPopup);

            var spacer = new VisualElement();
            spacer.AddToClassList("spacer");
            m_TitleContainer.Add(spacer);

            m_Title.Add(m_TitleContainer);

            m_Container = new VisualElement();
            m_Container.AddToClassList(containerUssClassName);
            Add(m_Container);

            var doubleContainer = new VisualElement();
            doubleContainer.AddToClassList(ussClassName.WithUssElement("double-container"));
            Add(doubleContainer);

            m_DragBlock = new VisualElement() { name = "drag-block" };
            m_DragBlock.AddToClassList(dragBlockUssClassName);
            var dragMarker = new VisualElement();
            dragMarker.AddToClassList(BaseVerticalCollectionView.dragHoverMarkerUssClassName);
            m_DragBlock.Add(dragMarker);

            m_LinesLayer = new VisualElement() { name = "lines-layer" };
            m_LinesLayer.AddToClassList(linesLayerUssName);
            m_LinesLayer.pickingMode = PickingMode.Ignore;
            doubleContainer.Add(m_Container);
            doubleContainer.Add(m_LinesLayer);

            m_LinesLayer.generateVisualContent += OnGenerateVisualContent;

            m_EmptyWarning = new VisualElement();
            m_EmptyWarning.AddToClassList(ussClassName.WithUssElement("empty-warning"));

            var emptyIcon = new Image();
            emptyIcon.AddToClassList(ussClassName.WithUssElement("empty-warning-icon"));
            m_EmptyWarning.Add(emptyIcon);

            m_EmptyLabel = new Label(L10n.Tr("There are no conditions in this group."));
            m_EmptyWarning.Add(m_EmptyLabel);
        }

        ConditionView CreateSubConditionView(ConditionModel condition)
        {
            var childElement = ModelViewFactory.CreateUI<ConditionView>(RootView, condition, IsClone ? Context : null, ConditionEditor);

            return childElement;
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();
            AddToClassList(ussClassName);
        }

        void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            var p2d = mgc.painter2D;

            var layout = m_LinesLayer.layout;

            var color = m_LinesLayer.resolvedStyle.color;

            color *= this.GetPlayModeTintColor();

            p2d.strokeColor = color;

            if (SubConditions.Count > 0 && m_Container.childCount > 0)
            {
                p2d.BeginPath();
                if (GroupConditionModel.GroupOperation == GroupConditionModel.Operation.And)
                {
                    p2d.MoveTo(layout.position);
                    p2d.LineTo(layout.position + new Vector2(0, m_Container[m_Container.childCount - 1].layout.position.y + k_ConditionTitleHeight + p2d.lineWidth * 0.5f));
                }

                const float k_OrMargin = 5;

                for (int i = 0; i < m_Container.childCount; ++i)
                {
                    var element = m_Container.ElementAt(i);
                    var elementLayout = element.layout;
                    if (GroupConditionModel.GroupOperation == GroupConditionModel.Operation.Or)
                    {
                        p2d.MoveTo(new Vector2(layout.position.x, elementLayout.position.y + k_OrMargin));
                        if (i == m_Container.childCount - 1)
                            p2d.LineTo(new Vector2(layout.position.x, elementLayout.y + k_ConditionTitleHeight));
                        else
                            p2d.LineTo(new Vector2(layout.position.x, elementLayout.yMax - k_OrMargin));
                    }

                    float y = elementLayout.position.y + k_ConditionTitleHeight;

                    p2d.MoveTo(new Vector2(layout.position.x, y));
                    p2d.LineTo(new Vector2(layout.position.x + k_XOffset, y));
                }
                p2d.ClosePath();
                p2d.Stroke();
            }
        }

        internal void StartBlockHoveringOver()
        {
            if (SubConditions.Count > 0)
                m_Container.Add(m_DragBlock);
            MarkDirtyRepaint();
        }

        int GetBlockIndex(Vector2 posInContext)
        {
            if (SubConditions.Count > 0)
            {
                int i = 0;
                for (; i < SubConditions.Count; i++)
                {
                    float blockY = SubConditions[i].layout.center.y;
                    if (blockY > posInContext.y)
                        return i;
                }

                return SubConditions.Count;
            }

            return 0;
        }

        internal void BlockHoveringOver(Vector2 posInContext, IReadOnlyList<ConditionView> conditions)
        {
            if (SubConditions.Count > 0)
            {
                var index = GetBlockIndex(posInContext);

                if (index >= m_Container.childCount - 1)
                    m_DragBlock.style.top = m_Container.layout.yMax;
                else
                    m_DragBlock.style.top = m_Container.ElementAt(index).layout.y;
            }
            else
            {
                m_Title.AddToClassList(hoveredTitleUssClassName);
            }

            MarkDirtyRepaint();
        }

        internal void BlockDropped(Vector2 posInContext, IReadOnlyList<ConditionView> conditions)
        {
            var index = GetBlockIndex(posInContext);

            RootView.Dispatch(new MoveConditionCommand(
                GroupConditionModel,
                conditions.SelectToList(t => t.ConditionModel), index));

            StopBlockHoveringOver();
        }

        internal void StopBlockHoveringOver()
        {
            m_Title.RemoveFromClassList(hoveredTitleUssClassName);
            m_DragBlock.RemoveFromHierarchy();
            MarkDirtyRepaint();
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            base.UpdateUIFromModel(visitor);

            m_ConditionPopup.SetValueWithoutNotify(GroupConditionModel.GroupOperation);

            if (GroupConditionModel.GroupOperation == GroupConditionModel.Operation.And)
            {
                AddToClassList(andUssClassName);
                RemoveFromClassList(orUssClassName);
            }
            else
            {
                AddToClassList(orUssClassName);
                RemoveFromClassList(andUssClassName);
            }

            CleanupElements();
            ReorderElements();

            int rank = 0;
            GroupConditionView view = GetFirstAncestorOfType<GroupConditionView>();
            while (view != null)
            {
                ++rank;
                view = view.GetFirstAncestorOfType<GroupConditionView>();
            }

            UpdateSubIndentation(rank);
        }

        void UpdateSubIndentation(int rank)
        {
            UpdateIndentation(rank);
            foreach (var subCondition in SubConditions)
            {
                subCondition.UpdateIndentation(rank + 1);
                if (subCondition is GroupConditionView groupConditionView)
                    groupConditionView.UpdateSubIndentation(rank + 1);
            }
        }

        /// <inheritdoc />
        public override void UpdateIndentation(int rank)
        {
            base.UpdateIndentation(rank);

            m_LinesLayer.style.marginLeft = k_IndentationWidth * rank * 0.5f;
            m_DragBlock.style.marginLeft = k_IndentationWidth * rank;
            m_EmptyWarning.style.marginLeft = k_IndentationWidth * (rank + 2);
        }

        void CleanupElements()
        {
            m_LinesLayer.MarkDirtyRepaint();
            var subConditionModels = GroupConditionModel.SubConditions;
            for (int i = 0; i < SubConditions.Count;)
            {
                if (!subConditionModels.Contains(SubConditions[i].ConditionModel))
                {
                    SubConditions[i].RemoveFromHierarchy();
                    if (!IsClone)
                        ConditionEditor.UnregisterConditionView(SubConditions[i]);
                    m_SubConditions.RemoveAt(i);
                }
                else
                {
                    if (SubConditions[i] is GroupConditionView groupConditionView)
                        groupConditionView.CleanupElements();
                    ++i;
                }
            }
        }

        void ReorderElements()
        {
            RefreshEmptyWarning();
            var subConditionModels = GroupConditionModel.SubConditions;
            for (int i = 0; i < subConditionModels.Count; ++i)
            {
                if (i >= m_SubConditions.Count || m_SubConditions[i].ConditionModel != subConditionModels[i])
                {
                    var existingContainer = m_SubConditions.Count > i ? m_SubConditions.FindIndex(i + 1, t => t.ConditionModel == subConditionModels[i]) : -1;

                    if (existingContainer >= 0) // existing but not in the right place
                    {
                        var transitionContainer = m_SubConditions[existingContainer];
                        m_SubConditions.RemoveAt(existingContainer);
                        m_SubConditions.Insert(i, transitionContainer);


                        if (i == 0)
                            transitionContainer.SendToBack();
                        else
                            transitionContainer.PlaceInFront(m_SubConditions[i - 1]);
                    }
                    else
                    {
                        var newView = CreateSubConditionView(subConditionModels[i]);

                        m_Container.Insert(i, newView);
                        m_SubConditions.Insert(i, newView);
                        if (!IsClone)
                            ConditionEditor?.RegisterConditionView(newView);

                        if (newView is GroupConditionView)
                        {
                            int rank = 0;
                            GroupConditionView view = GetFirstAncestorOfType<GroupConditionView>();
                            while (view != null)
                            {
                                ++rank;
                                view = view.GetFirstAncestorOfType<GroupConditionView>();
                            }

                            UpdateSubIndentation(rank + 1);
                        }
                    }
                }
                else
                {
                    if (SubConditions[i] is GroupConditionView groupConditionView)
                        groupConditionView.ReorderElements();
                }
            }
        }

        void RefreshEmptyWarning()
        {
            var subConditionModels = GroupConditionModel.SubConditions;
            if (subConditionModels.Count == 0)
            {
                if (m_EmptyWarning.parent == null)
                    m_Container.Add(m_EmptyWarning);
            }
            else
            {
                m_EmptyWarning.RemoveFromHierarchy();
            }
        }

        internal Vector2 WorldToContainer(Vector2 worldPosition)
        {
            return m_Container.WorldToLocal(worldPosition);
        }
    }
}
