// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.CSO;
using Unity.GraphToolkit.InternalBridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A <see cref="IViewContext"/> for the condition editor.
    /// </summary>
    [UnityRestricted]
    internal class ConditionEditorContext : IViewContext
    {
        static ConditionEditorContext s_Default = new ConditionEditorContext();
        public static ConditionEditorContext Default = s_Default;
        public bool Equals(IViewContext other)
        {
            return ReferenceEquals(this, other);
        }
    }
    /// <summary>
    /// A <see cref="ModelView"/> to edit a transition's conditions.
    /// </summary>
    [UnityRestricted]
    internal class ConditionEditor : ModelView
    {
        static readonly string k_USSClassName = "ge-condition-editor";

        Dictionary<ConditionModel, ConditionView> m_ConditionViewModels = new();
        RootGroupConditionView m_RootConditionView;

        const float k_DragThresholdSquare = 6 * 6;
        static readonly List<VisualElement> k_PickedElements = new();
        GroupConditionView m_HoveredGroupCondition;
        bool m_RootSelected;

        VisualElement m_DragRoot;
        enum ToolState
        {
            None,
            ConditionInteraction,
            MovingConditions
        }

        ToolState m_ToolState;
        ConditionView m_InteractedCondition;
        Vector3 m_StartDrag;

        TransitionSelectionManager<ConditionView> m_SelectionManager = new();

        /// <summary>
        /// The transition model for this editor.
        /// </summary>
        public TransitionModel TransitionModel => (TransitionModel)Model;

        /// <summary>
        /// The transition support model for this editor.
        /// </summary>
        public TransitionSupportModel TransitionSupportModel { get; set; }

        /// <summary>
        /// All the condition views in this editor.
        /// </summary>
        public IReadOnlyCollection<ConditionView> ConditionViews => m_ConditionViewModels.Values;

        /// <summary>
        /// The selection manager for this editor.
        /// </summary>
        internal TransitionSelectionManager<ConditionView> SelectionManager => m_SelectionManager;

        /// <summary>
        /// Create an instance of <see cref="ConditionEditor"/>.
        /// </summary>
        public ConditionEditor(TransitionPropertiesEditor transitionPropertiesEditor)
        {
            TransitionSupportModel = transitionPropertiesEditor.TransitionSupportEditor.TransitionSupportModel;
            RegisterCallback<PointerDownEvent>(OnPointerDownEvent);
            RegisterCallback<PointerUpEvent>(OnPointerUpEvent);
            RegisterCallback<MouseCaptureOutEvent>(OnCaptureOut);
            RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
            RegisterCallback<KeyDownEvent>(OnKeyDownEvent);
        }

        void OnCaptureOut(MouseCaptureOutEvent evt)
        {
            ReleaseDragging();
        }

        /// <inheritdoc />
        public override void BuildUITree()
        {
            base.BuildUITree();
            this.AddPackageStylesheet("ConditionEditor.uss");
            AddToClassList(k_USSClassName);

            BuildConditionView();
            focusable = true;
        }

        void BuildConditionView()
        {
            if (TransitionModel == null)
                return;
            m_RootConditionView = ModelViewFactory.CreateUI<RootGroupConditionView>(RootView, TransitionModel.ConditionModel, RootGroupConditionViewContext.Default, this);
            m_RootConditionView.style.marginTop = 0;
            RegisterConditionView(m_RootConditionView);
            Add(m_RootConditionView);
        }

        /// <summary>
        /// Clears the selection in this editor.
        /// </summary>
        public void ClearSelection()
        {
            m_SelectionManager.ClearSelection();
        }

        bool CanDeleteSelection
        {
            get
            {
                foreach (var condition in m_ConditionViewModels)
                {
                    if (condition.Value.IsSelected && condition.Key.Parent != null)
                        return true;
                }

                return false;
            }
        }

        bool CanDuplicateSelection
        {
            get
            {
                foreach (var condition in m_ConditionViewModels)
                {
                    if (condition.Value.IsSelected && condition.Key.Parent != null)
                        return true;
                }

                return false;
            }
        }

        void DeleteSelected()
        {
            var listSelectedItems = m_SelectionManager.SelectedElements;

            if (listSelectedItems.Count > 0)
            {
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                RootView.Dispatch(new DeleteConditionsCommand(listSelectedItems.Select(c => c.ConditionModel).ToList()));
#pragma warning restore UA2001
            }
        }

        void DuplicateSelected()
        {
            var listSelectedItems = m_SelectionManager.SelectedElements;

            if (listSelectedItems.Count > 0)
            {
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                RootView.Dispatch(new DuplicateConditionsCommand(listSelectedItems.Select(c => c.ConditionModel).ToList()));
#pragma warning restore UA2001
            }
        }

        void OnPointerDownEvent(PointerDownEvent evt)
        {
            if (!(evt.target is VisualElement ve))
                return;

            var conditionView = ve.GetFirstOfType<ConditionView>();
            if (conditionView != null)
            {
                m_ToolState = ToolState.ConditionInteraction;
                m_InteractedCondition = conditionView;
                m_StartDrag = evt.position;
                m_RootSelected = m_InteractedCondition == m_RootConditionView;

                RegisterCallback<PointerMoveEvent>(OnPointerMoveEvent);
                this.CaptureMouse();
                evt.StopPropagation();
            }
        }

        void OnKeyDownEvent(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.UpArrow)
            {
                if (evt.modifiers.HasFlag(EventModifiers.Shift) || evt.modifiers.HasFlag(ClickSelector.PlatformMultiSelectModifier))
                {
                    var currentSelectionHead = m_SelectionManager.CurrentSelectionHead;
                    if (currentSelectionHead == null)
                        return;
                    if (!GetAdjacent(currentSelectionHead, out var newSelected, true))
                        return;
                    ManageContinuousSelection(newSelected);
                }
                else
                {
                    var lastSelected = m_SelectionManager.LastSelectedForContinuousSelection;
                    if (lastSelected != null)
                    {
                        if (!GetAdjacent(lastSelected, out var newSelected, true))
                            return;
                        m_SelectionManager.Select(newSelected, true);
                    }
                }

                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.DownArrow)
            {
                if (evt.modifiers.HasFlag(EventModifiers.Shift) || evt.modifiers.HasFlag(ClickSelector.PlatformMultiSelectModifier))
                {
                    var currentSelectionHead = m_SelectionManager.CurrentSelectionHead;
                    if (currentSelectionHead == null)
                        return;
                    if (!GetAdjacent(currentSelectionHead, out var newSelected, false))
                        return;
                    ManageContinuousSelection(newSelected);
                }
                else
                {
                    var lastSelected = m_SelectionManager.LastSelectedForContinuousSelection;

                    if (lastSelected != null)
                    {
                        if (!GetAdjacent(lastSelected, out var newSelected, false))
                            return;
                        m_SelectionManager.Select(newSelected, true);
                    }
                }

                evt.StopPropagation();
            }
        }

        static bool GetAdjacent(ConditionView lastSelected, out ConditionView newSelected, bool prev)
        {
            if (!prev && lastSelected is GroupConditionView group && group.SubConditions.Count > 0)
            {
                newSelected = group.SubConditions[0];
                return true;
            }
            newSelected = null;
            var container = lastSelected.GetFirstAncestorOfType<GroupConditionView>();
            if (container == null)
                return false;
            var originalContainer = container;
            int lastIndex = container.SubConditions.IndexOf(lastSelected);
            bool hadGoneUp = false;
            while ((prev && lastIndex == 0) || (!prev && lastIndex == container.SubConditions.Count - 1))
            {
                lastSelected = container;
                container = lastSelected.GetFirstAncestorOfType<GroupConditionView>();
                if (container == null)
                    return false;
                lastIndex = container.SubConditions.IndexOf(lastSelected);
                hadGoneUp = true;
            }
            newSelected = (hadGoneUp && prev) ? lastSelected : container.SubConditions[prev ? lastIndex - 1 : lastIndex + 1];
            if (prev && originalContainer != newSelected)
            {
                while (newSelected is GroupConditionView groupCondition)
                {
                    if (groupCondition.SubConditions.Count == 0)
                        break;
                    newSelected = groupCondition.SubConditions[^1];
                }
            }
            return true;
        }

        void OnPointerUpEvent(PointerUpEvent evt)
        {
            if (m_ToolState == ToolState.MovingConditions)
            {
                GroupConditionView groupCondition = GetGroupAtPosition(evt.position);
                if (groupCondition != null)
                {
                    var posInGroup = groupCondition.WorldToContainer(evt.position);

                    groupCondition.BlockDropped(posInGroup, m_SelectionManager.SelectedElements);
                }
                if (groupCondition == m_HoveredGroupCondition)
                    m_HoveredGroupCondition = null;
                m_SelectionManager.ClearSelection();

                ReleaseDragging();
            }
            else if (m_ToolState == ToolState.ConditionInteraction)
            {
                if (m_InteractedCondition != null && m_InteractedCondition != m_RootConditionView)
                {
                    if (evt.modifiers.HasFlag(EventModifiers.Shift))
                    {
                        ManageContinuousSelection(m_InteractedCondition);
                    }
                    else if (evt.modifiers.HasFlag(ClickSelector.PlatformMultiSelectModifier))
                        m_SelectionManager.Toggle(m_InteractedCondition);
                    else
                        m_SelectionManager.Select(m_InteractedCondition);

                    var editor = GetFirstAncestorOfType<TransitionPropertiesEditor>();
                    if (editor != null)
                    {
                        editor.SelectionManager.Select(editor);
                    }
                    evt.StopPropagation();
                }
            }
            this.ReleaseMouse();
            m_ToolState = ToolState.None;
            m_InteractedCondition = null;
        }

        void ManageContinuousSelection(ConditionView interactedCondition)
        {
            var selected = new List<ConditionView>();

            bool increasing = false;
            if (interactedCondition != m_SelectionManager.LastSelectedForContinuousSelection)
            {
                bool select = false;

                RecurseConditionTree(m_RootConditionView, interactedCondition, m_SelectionManager.LastSelectedForContinuousSelection, selected, ref select, ref increasing);

                if (!increasing)
                    selected.Reverse();
            }
            else
            {
                selected.Add(interactedCondition);
            }

            m_SelectionManager.SetContinuousSelection(selected);
        }

        bool RecurseConditionTree(GroupConditionView rootElement, ConditionView interactedCondition, ConditionView last, List<ConditionView> selected, ref bool select, ref bool increasing)
        {
            foreach (var subCondition in rootElement.SubConditions)
            {
                if (subCondition == interactedCondition || subCondition == last)
                {
                    if (select)
                    {
                        selected.Add(subCondition);
                        increasing = subCondition == interactedCondition;
                        return true;
                    }
                    select = true;
                }
                if (select)
                    selected.Add(subCondition);

                if (subCondition is GroupConditionView groupCondition)
                {
                    if (RecurseConditionTree(groupCondition, interactedCondition, last, selected, ref select, ref increasing))
                        return true;
                }
            }

            return false;
        }

        bool IsGroupInSelection(GroupConditionView view)
        {
            while (view != m_RootConditionView && view != null)
            {
                if (SelectionManager.SelectedElements.Contains(view))
                    return true;
                view = view.GetFirstAncestorOfType<GroupConditionView>();
            }

            return false;
        }

        GroupConditionView GetGroupAtPosition(Vector2 position)
        {
            k_PickedElements.Clear();
            m_InteractedCondition.panel.PickAll(position, k_PickedElements);

            for (int i = 0; i < k_PickedElements.Count; i++)
            {
                if (k_PickedElements[i] is GroupConditionView groupCondition)
                {
                    if (!m_DragRoot.Contains(groupCondition) && !IsGroupInSelection(groupCondition))
                        return groupCondition;
                }
            }

            return m_RootConditionView;
        }

        void OnPointerMoveEvent(PointerMoveEvent evt)
        {
            //If the LeftButton is no longer pressed.
            if ((evt.pressedButtons & 1 << ((int)MouseButton.LeftMouse)) != (1 << ((int)MouseButton.LeftMouse)))
            {
                ReleaseDragging();
            }

            if (m_ToolState == ToolState.ConditionInteraction)
            {
                if (!m_RootSelected && (evt.position - m_StartDrag).sqrMagnitude > k_DragThresholdSquare)
                {
                    m_ToolState = ToolState.MovingConditions;

                    m_HoveredGroupCondition = m_InteractedCondition.GetFirstAncestorOfType<GroupConditionView>();

                    if (!m_InteractedCondition.IsSelected)
                    {
                        m_SelectionManager.Select(m_InteractedCondition);
                    }

                    if (m_DragRoot == null)
                    {
                        m_DragRoot = new VisualElement();
                        m_DragRoot.AddToClassList(k_USSClassName.WithUssElement("drag-root"));
                        m_DragRoot.AddPackageStylesheet("ConditionEditor.uss");
                    }
                    m_DragRoot.Clear();

                    foreach (var block in m_SelectionManager.SelectedElements)
                    {
                        var doubleChildView = ModelViewFactory.CreateUI<ConditionView>(RootView, block.ConditionModel, DragCloneCreationContext.Default, this);
                        doubleChildView.style.width = block.layout.width;
                        m_DragRoot.Add(doubleChildView);
                    }

                    RootView.Add(m_DragRoot);

                    m_HoveredGroupCondition.StartBlockHoveringOver();
                    var posInGroup = m_HoveredGroupCondition.WorldToContainer(evt.position);
                    m_HoveredGroupCondition.BlockHoveringOver(posInGroup, m_SelectionManager.SelectedElements);
                }
            }

            if (m_ToolState == ToolState.MovingConditions)
            {
                var dragRootPosition = m_DragRoot.parent.WorldToLocal(evt.position);
                m_DragRoot.style.left = dragRootPosition.x;
                m_DragRoot.style.top = dragRootPosition.y + 2;


                GroupConditionView groupCondition = GetGroupAtPosition(evt.position);
                if (m_HoveredGroupCondition != groupCondition)
                {
                    m_HoveredGroupCondition?.StopBlockHoveringOver();
                    m_HoveredGroupCondition = groupCondition;
                    m_HoveredGroupCondition?.StartBlockHoveringOver();
                }
                var posInGroup = groupCondition.WorldToContainer(evt.position);
                m_HoveredGroupCondition?.BlockHoveringOver(posInGroup, m_SelectionManager.SelectedElements);
            }
        }

        void ReleaseDragging()
        {
            if (m_DragRoot != null)
            {
                m_DragRoot.RemoveFromHierarchy();
                foreach (var child in m_DragRoot.Children())
                {
                    ((ConditionView)child).RemoveFromRootView();
                }
                m_DragRoot.Clear();
            }

            UnregisterCallback<PointerMoveEvent>(OnPointerMoveEvent);
            this.ReleaseMouse();

            if (m_HoveredGroupCondition != null)
            {
                m_HoveredGroupCondition.StopBlockHoveringOver();
                m_HoveredGroupCondition = null;
            }
            m_ToolState = ToolState.None;
        }

        void OnValidateCommand(ValidateCommandEvent evt)
        {
            if (panel.GetCapturingElement(PointerId.mousePointerId) != null)
                return;

            if ((evt.commandName == EventCommandNamesBridge.Delete || evt.commandName == EventCommandNamesBridge.SoftDelete) && CanDeleteSelection)
            {
                evt.StopPropagation();
                evt.imguiEvent?.Use();
            }
            if (evt.commandName == EventCommandNamesBridge.Duplicate && CanDuplicateSelection)
            {
                evt.StopPropagation();
                evt.imguiEvent?.Use();
            }
        }

        void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            if (panel.GetCapturingElement(PointerId.mousePointerId) != null)
                return;

            if (evt.commandName == EventCommandNamesBridge.Delete || evt.commandName == EventCommandNamesBridge.SoftDelete)
            {
                DeleteSelected();
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNamesBridge.Duplicate)
            {
                DuplicateSelected();
                evt.StopPropagation();
            }

            if (evt.isPropagationStopped)
                evt.imguiEvent?.Use();
        }

        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction(CommandMenuItemNames.Delete, _ =>
            {
                DeleteSelected();
            }, CanDeleteSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction(CommandMenuItemNames.Duplicate, _ =>
            {
                DuplicateSelected();
            }, CanDuplicateSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);


            evt.StopPropagation();
        }

        /// <summary>
        /// Registers a condition view.
        /// </summary>
        /// <param name="conditionView">The condition view.</param>
        public virtual void RegisterConditionView(ConditionView conditionView)
        {
            m_ConditionViewModels.Add(conditionView.ConditionModel, conditionView);
            conditionView.TransitionModel = TransitionModel;
            conditionView.TransitionSupportModel = TransitionSupportModel;
        }

        /// <summary>
        /// Unregisters a condition view.
        /// </summary>
        /// <param name="conditionView">The condition view.</param>
        public virtual void UnregisterConditionView(ConditionView conditionView)
        {
            if (conditionView is GroupConditionView childGroup)
            {
                foreach (var subCondition in childGroup.SubConditions)
                {
                    UnregisterConditionView(subCondition);
                }
            }
            SelectionManager.Remove(conditionView);
            m_ConditionViewModels.Remove(conditionView.ConditionModel);
            conditionView.RemoveFromRootView();
        }

        /// <inheritdoc />
        public override void RemoveFromRootView()
        {
            UnregisterConditionView(m_RootConditionView);
            base.RemoveFromRootView();
            foreach (var conditionView in ConditionViews)
            {
                conditionView.RemoveFromRootView();
            }
        }
    }
}
