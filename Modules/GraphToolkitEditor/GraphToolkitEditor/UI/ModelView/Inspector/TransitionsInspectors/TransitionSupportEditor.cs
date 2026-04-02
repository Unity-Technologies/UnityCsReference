// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.CSO;
using Unity.GraphToolkit.InternalBridge;
using Unity.GraphToolsAuthoringFramework.InternalEditorBridge;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Editor for a <see cref="TransitionSupportModel"/> in the inspector
    /// </summary>
    [UnityRestricted]
    internal class TransitionSupportEditor : ModelView, ISelectableTransition, ICollapsibleContainer
    {
        /// <summary>
        /// The USS class name added to this element.
        /// </summary>
        public static readonly string ussClassName = "ge-transition-support-editor";

        static readonly string k_CollapseButtonName = "collapse-button";
        static readonly string k_TitleLabelName = "title-label";

        static readonly string k_PropertiesEditorName = "properties-editor";
        static readonly string k_DragBlockName = "drag-block";

        static readonly string k_AddButtonName = "add-button";
        static readonly string k_OptionButtonName = "option-button";

        /// <summary>
        /// The USS class name added to this element when collapsed.
        /// </summary>
        public static readonly string collapsedUssClassName = ussClassName.WithUssModifier(GraphElementHelper.collapsedUssModifier);

        /// <summary>
        /// The USS class name added to this element when selected.
        /// </summary>
        public static readonly string selectedUssClassName = ussClassName.WithUssModifier(GraphElementHelper.selectedUssModifier);

        /// <summary>
        /// The USS class name added to the icon.
        /// </summary>
        static readonly string k_IconUssName = ussClassName.WithUssElement(GraphElementHelper.iconName);

        VisualElement m_Header;
        Toggle m_CollapseButton;
        VisualElement m_Icon;
        bool m_IsSelected;
        EditableLabel m_TitleLabel;
        VisualElement m_Container;
        List<TransitionPropertiesEditor> m_TransitionPropertiesEditors = new();
        Button m_AddButton;
        EditorToolbarButton m_OptionButton;
        string m_LastIconUssClassName;

        VisualElement m_DragBlock;

        StateTransitionsInspector m_StateTransitionsInspector;

        TransitionSelectionManager<ISelectableTransition> m_TransitionSelectionManager;

        /// <summary>
        /// The <see cref="TransitionSupportModel"/> displayed by this editor.
        /// </summary>
        public TransitionSupportModel TransitionSupportModel => (TransitionSupportModel)Model;

        /// <summary>
        /// The <see cref="TransitionPropertiesEditor"/> contained in this editor.
        /// </summary>
        public IReadOnlyList<TransitionPropertiesEditor> TransitionPropertiesEditors => m_TransitionPropertiesEditors;

        /// <summary>
        /// Whether this editor is in a state inspector or a regular TransitionSupportInspector.
        /// </summary>
        public bool InStateInspector => m_StateTransitionsInspector != null;

        internal TransitionSelectionManager<ISelectableTransition> SelectionManager => m_TransitionSelectionManager;

        /// <summary>
        /// Creates a new instance of <see cref="TransitionSupportEditor"/>.
        /// </summary>
        /// <param name="stateTransitionsInspector">The containing <see cref="StateTransitionsInspector"/>, if contained in a <see cref="StateTransitionsInspector"/>. </param>
        public TransitionSupportEditor(StateTransitionsInspector stateTransitionsInspector)
        {
            m_StateTransitionsInspector = stateTransitionsInspector;
            m_TransitionSelectionManager = m_StateTransitionsInspector != null ? m_StateTransitionsInspector.SelectionManager : new TransitionSelectionManager<ISelectableTransition>();
        }

        /// <inheritdoc />
        protected override void BuildUI()
        {
            base.BuildUI();

            RegisterCallback<PointerDownEvent>(OnMouseDownEvent);
            RegisterCallback<PointerUpEvent>(OnMouseUpEvent);
            RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
            RegisterCallback<KeyDownEvent>(OnKeyDown);

            this.AddPackageStylesheet("TransitionSupportEditor.uss");

            BuildHeader();
            BuildContainer();
            AddToClassList(ussClassName);
        }

        void BuildHeader()
        {
            m_Header = new VisualElement { name = GraphElementHelper.headerName };
            m_Header.AddToClassList(ussClassName.WithUssElement(GraphElementHelper.headerName));

            m_CollapseButton = new Toggle { name = k_CollapseButtonName };
            m_CollapseButton.AddToClassList(Foldout.toggleUssClassName);
            m_CollapseButton.AddToClassList(ussClassName.WithUssElement(k_CollapseButtonName));
            m_CollapseButton.focusable = false; //currently a focusable foldout does not have the right styles.
            m_Header.Add(m_CollapseButton);

            m_Icon = new VisualElement();
            m_Icon.AddToClassList(k_IconUssName);
            m_Header.Add(m_Icon);


            m_TitleLabel = new EditableLabel { name = k_TitleLabelName };
            m_TitleLabel.ClickToEditDisabled = true;
            m_TitleLabel.EditActionName = "Rename";
            m_TitleLabel.AddToClassList(ussClassName.WithUssElement(k_TitleLabelName));
            m_TitleLabel.RegisterCallback<ChangeEvent<string>>(OnTitleChanged);
            m_Header.Add(m_TitleLabel);

            m_AddButton = new Button() { name = k_AddButtonName };
            m_AddButton.AddToClassList(ussClassName.WithUssElement(k_AddButtonName));
            m_AddButton.iconImage = Background.FromTexture2D(EditorResources.Load<Texture2D>(EditorGUIUtility.isProSkin ? "Icons/d_CreateAddNew.png" : "Icons/CreateAddNew.png"));
            m_Header.Add(m_AddButton);
            m_AddButton.clicked += AddTransition;

            InitializeOptionsButton();
            m_Header.Add(m_OptionButton);

            Add(m_Header);
            m_CollapseButton.RegisterCallback<ChangeEvent<bool>>(OnCollapseChange);
        }

        void InitializeOptionsButton()
        {
            var contextMenuManipulator = new ContextualMenuManipulator(OnPopupMenuContextualPopulate);

            contextMenuManipulator.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, clickCount = 1 });
            m_OptionButton = new EditorToolbarButton(EditorGUIUtility.IconContent("_Menu").image as Texture2D, null);
            m_OptionButton.clickable = null; // if the clickable is left on the button then the context menu manipulator may fail to trigger
            m_OptionButton.AddManipulator(contextMenuManipulator);


            m_OptionButton.AddToClassList(ussClassName.WithUssElement(k_OptionButtonName));
        }

        void OnTitleChanged(ChangeEvent<string> e)
        {
            if (TransitionSupportModel is IRenamable renamable)
                RootView.Dispatch(new RenameElementsCommand(new[] { renamable }, e.newValue));
        }

        void OnPopupMenuContextualPopulate(ContextualMenuPopulateEvent evt)
        {
            MakeMenu(evt.menu, false);
            evt.StopPropagation();
        }

        /// <inheritdoc />
        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            var menu = evt.menu;

            MakeMenu(menu, true);
        }

        /// <inheritdoc />
        public void BeginEditing()
        {
            m_TitleLabel.BeginEditing();
        }

        /// <summary>
        /// Create the option menu for this editor.
        /// </summary>
        /// <param name="menu">The menu on which to add elements.</param>
        /// <param name="contextual">Whether it is the contextual menu or the option menu in the header.</param>
        protected virtual void MakeMenu(DropdownMenu menu, bool contextual)
        {
            if (contextual && IsSelected)
            {
                menu.AppendAction(CommandMenuItemNames.Delete, _ =>
                {
                    DeleteSelection();
                });
            }
            else
            {
                menu.AppendAction(CommandMenuItemNames.Delete, _ =>
                {
                    RemoveWire();
                });
            }

            menu.AppendSeparator();

            if (!m_TitleLabel.IsInEditMode)
            {
                menu.AppendAction(CommandMenuItemNames.Rename, _ => m_TitleLabel.BeginEditing());
            }

            menu.AppendAction(CommandMenuItemNames.FrameSelected, _ =>
            {
                FrameInGraphView();
            });

            menu.AppendSeparator();

            var isCollapsed = ((ModelInspectorViewModel)RootView.Model).TransitionInspectorState.GetTransitionSupportModelCollapsed(TransitionSupportModel, InStateInspector);

            menu.AppendAction(TransitionPropertiesEditor.k_ExpandMenuName, _ =>
            {
                RootView.Dispatch(new CollapseTransitionsCommand(TransitionSupportModel, false, InStateInspector));
            }, isCollapsed ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            menu.AppendAction(TransitionPropertiesEditor.k_CollapseMenuName, _ =>
            {
                RootView.Dispatch(new CollapseTransitionsCommand(TransitionSupportModel, true, InStateInspector));
            }, isCollapsed ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);


            bool allTransitionsCollapsed = true;
            bool allTransitionsExpanded = true;

            foreach (var transition in TransitionSupportModel.Transitions)
            {
                var isTransitionCollapsed = ((ModelInspectorViewModel)RootView.Model).TransitionInspectorState.GetTransitionModelCollapsed(transition, InStateInspector);
                allTransitionsCollapsed &= isTransitionCollapsed;
                allTransitionsExpanded &= !isTransitionCollapsed;
            }

            menu.AppendAction("Expand All In Group", _ =>
            {
                RootView.Dispatch(new CollapseTransitionsCommand(TransitionSupportModel.Transitions, false, InStateInspector));
            }, allTransitionsExpanded ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);

            menu.AppendAction("Collapse All In Group", _ =>
            {
                RootView.Dispatch(new CollapseTransitionsCommand(TransitionSupportModel.Transitions, true, InStateInspector));
            }, allTransitionsCollapsed ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);

            menu.AppendSeparator();
        }

        void FrameInGraphView()
        {
            var graphView = (RootView.Window as GraphViewEditorWindow)?.GraphView;
            if (graphView != null)
            {
                var wireInGraphView = TransitionSupportModel.GetView<Transition>(graphView);
                if (wireInGraphView != null)
                {
                    (RootView.Window as GraphViewEditorWindow)?.GraphView.DispatchFrameAndSelectElementsCommand(false, wireInGraphView);
                }
            }
        }

        void BuildContainer()
        {
            m_Container = new VisualElement { name = GraphElementHelper.containerName };
            m_Container.AddToClassList(ussClassName.WithUssElement(GraphElementHelper.containerName));

            m_DragBlock = new VisualElement() { name = k_DragBlockName };
            m_DragBlock.AddToClassList(ussClassName.WithUssElement(k_DragBlockName));

            Add(m_Container);
        }

        void UpdateTitleFromModel()
        {
            if (!string.IsNullOrEmpty(TransitionSupportModel.Title))
            {
                m_TitleLabel.SetValueWithoutNotify(TransitionSupportModel.Title);
            }
            else if (TransitionSupportModel.TransitionSupportKind is TransitionSupportKind.Local or TransitionSupportKind.OnEnter)
            {
                m_TitleLabel.SetValueWithoutNotify((TransitionSupportModel.TransitionSupportKind == TransitionSupportKind.Local ? "LOCAL \u2192 " : "ON ENTER \u2192 ") + TransitionSupportModel.ToPort.NodeModel.Title);
            }
            else
            {
                TransitionSupportModel.GraphModel.TryGetModelFromGuid(TransitionSupportModel.FromNodeGuid, out var fromState);
                TransitionSupportModel.GraphModel.TryGetModelFromGuid(TransitionSupportModel.ToNodeGuid, out var toState);
                m_TitleLabel.SetValueWithoutNotify($"{(fromState as IHasTitle)?.Title ?? "?"} \u2192 {(toState as IHasTitle)?.Title ?? "?"}");
            }
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            UpdateTitleFromModel();
            string iconUssClassName = k_IconUssName.WithUssModifier(TransitionSupportModel.TransitionSupportKind switch
            {
                TransitionSupportKind.Local => "local",
                TransitionSupportKind.Self => "self",
                TransitionSupportKind.OnEnter => "on-enter",
                TransitionSupportKind.StateToState => "to-state",
                _ => string.Empty
            });

            if (TransitionSupportModel.TransitionSupportKind == TransitionSupportKind.StateToState)
            {
                if (m_StateTransitionsInspector != null)
                {
                    if (m_StateTransitionsInspector.Models.Contains(TransitionSupportModel.FromPort.NodeModel))
                        iconUssClassName = k_IconUssName.WithUssModifier("from-state");
                }
            }

            if (m_LastIconUssClassName != iconUssClassName)
            {
                if (m_LastIconUssClassName != null)
                    m_Icon.RemoveFromClassList(m_LastIconUssClassName);

                m_Icon.AddToClassList(iconUssClassName);
                m_LastIconUssClassName = iconUssClassName;
            }

            ReorderEditors();

            foreach (var propertiesEditor in m_TransitionPropertiesEditors)
            {
                propertiesEditor.UpdateUIFromModel(visitor);
            }
        }

        void OnCollapseChange(ChangeEvent<bool> e)
        {
            RootView.Dispatch(new CollapseTransitionsCommand(TransitionSupportModel, !e.newValue, InStateInspector));
        }

        void AddTransition()
        {
            RootView.Dispatch(new AddTransitionCommand(TransitionSupportModel));
        }

        void RemoveWire()
        {
            RootView.Dispatch(new DeleteElementsCommand(TransitionSupportModel));
        }

        public bool IsSelected
        {
            get => m_IsSelected;
            set
            {
                if (m_IsSelected == value)
                    return;
                m_IsSelected = value;
                EnableInClassList(selectedUssClassName, m_IsSelected);
            }
        }

        void SetSelection(TransitionPropertiesEditor transition)
        {
            m_TransitionSelectionManager.Select(transition);
        }

        void ToggleSelection(TransitionPropertiesEditor transition)
        {
            m_TransitionSelectionManager.Toggle(transition);
        }

        public List<TransitionPropertiesEditor> GetSelectedTransitions()
        {
            var listSelectedItems = new List<TransitionPropertiesEditor>();
            foreach (var transitionProperties in m_TransitionPropertiesEditors)
            {
                if (transitionProperties.IsSelected)
                    listSelectedItems.Add(transitionProperties);
            }

            return listSelectedItems;
        }

        public List<TransitionModel> GetSelectedTransitionModels()
        {
            var listSelectedItems = new List<TransitionModel>();
            foreach (var transitionProperties in m_TransitionPropertiesEditors)
            {
                if (transitionProperties.IsSelected)
                    listSelectedItems.Add(transitionProperties.TransitionModel);
            }

            return listSelectedItems;
        }

        public void DeleteTransitions(IReadOnlyList<TransitionModel> transitions)
        {
            if (transitions.Count > 0)
            {
                if (transitions.Count < TransitionSupportModel.Transitions.Count)
                    RootView.Dispatch(new RemoveTransitionCommand(TransitionSupportModel, transitions));
                else
                {
                    RootView.Dispatch(new DeleteElementsCommand(TransitionSupportModel));
                }
            }
        }

        void DeleteSelection()
        {
            if (m_StateTransitionsInspector != null)
                m_StateTransitionsInspector.DeleteSelection();
            else
            {
                DeleteTransitions(GetSelectedTransitionModels());
            }
        }

        public bool CanShiftTransitions(IReadOnlyList<TransitionPropertiesEditor> transitions, bool shiftUp)
        {
            if (transitions.Count == 0)
                return false;

            var index = m_TransitionPropertiesEditors.IndexOf(transitions[0]);

            if (shiftUp)
                return --index >= 0;
            else
                return ++index < m_TransitionPropertiesEditors.Count;
        }

        public void ShiftTransitions(IReadOnlyList<TransitionPropertiesEditor> transitions, bool shiftUp)
        {
            if (transitions.Count > 0)
            {
                var index = m_TransitionPropertiesEditors.IndexOf(transitions[0]);
                RootView.Dispatch(new MoveTransitionCommand(
                    TransitionSupportModel,
                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    transitions.Select(c => c.TransitionModel).ToList(),
#pragma warning restore UA2001
                    shiftUp ? --index : ++index));
            }
        }

        public void MoveTransitions(IReadOnlyList<TransitionPropertiesEditor> transitions, bool toTop)
        {
            if (transitions.Count > 0)
            {
                RootView.Dispatch(new MoveTransitionCommand(
                    TransitionSupportModel,
                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    transitions.Select(c => c.TransitionModel).ToList(),
#pragma warning restore UA2001
                    toTop ? 0 : TransitionSupportModel.Transitions.Count - 1));
            }
        }

        public bool CanMoveTransitions(IReadOnlyList<TransitionPropertiesEditor> transitions, bool toTop)
        {
            if (transitions.Count == 0)
                return false;

            var index = m_TransitionPropertiesEditors.IndexOf(transitions[0]);

            if (toTop)
                return index > 0;
            else
                return index < m_TransitionPropertiesEditors.Count - 1;
        }

        void OnValidateCommand(ValidateCommandEvent evt)
        {
            if (panel.GetCapturingElement(PointerId.mousePointerId) != null)
                return;

            if ((evt.commandName == EventCommandNamesBridge.Delete || evt.commandName == EventCommandNamesBridge.SoftDelete) && !InStateInspector)
            {
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNamesBridge.FrameSelected)
            {
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNamesBridge.Rename && !InStateInspector)
            {
                evt.StopPropagation();
            }
            if (evt.isPropagationStopped)
                evt.imguiEvent?.Use();
        }

        void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            if (panel.GetCapturingElement(PointerId.mousePointerId) != null)
                return;
            var selectedModels = GetSelectedTransitionModels();

            if (evt.commandName == EventCommandNamesBridge.Delete || evt.commandName == EventCommandNamesBridge.SoftDelete && !InStateInspector)
            {
                DeleteTransitions(selectedModels);
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNamesBridge.FrameSelected)
            {
                FrameInGraphView();
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNamesBridge.Rename && !InStateInspector)
            {
                var selected = GetSelectedTransitions();
                if (selected.Count == 1)
                    selected[0].BeginEditing();
                else
                    m_TitleLabel.BeginEditing();
                evt.StopPropagation();
            }

            if (evt.isPropagationStopped)
                evt.imguiEvent?.Use();
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (InStateInspector)
                return;

            var selectedModels = GetSelectedTransitionModels();
            if (selectedModels.Count > 0)
            {
                if (evt.keyCode == KeyCode.LeftArrow)
                {
                    RootView.Dispatch(new CollapseTransitionsCommand(selectedModels, true, InStateInspector));
                    evt.StopPropagation();
                }
                else if (evt.keyCode == KeyCode.RightArrow)
                {
                    RootView.Dispatch(new CollapseTransitionsCommand(selectedModels, false, InStateInspector));
                    evt.StopPropagation();
                }
            }

            if (evt.isPropagationStopped)
                evt.imguiEvent?.Use();
        }

        enum ToolState
        {
            None,
            TransitionInteraction,
            MovingTransitions
        }

        ToolState m_ToolState;
        TransitionPropertiesEditor m_InteractedTransition;
        Vector3 m_StartDrag;
        float m_DraggedHeight;
        List<TransitionPropertiesEditor> m_SelectedTransitionEditors;
        static readonly List<VisualElement> k_PickedElements = new List<VisualElement>();
        bool m_IsHoveringOver;
        Vector3 m_DraggedTransitionOffset;
        int m_DraggedTransitionIndex;

        const float k_DragThresholdSquare = 6 * 6;
        const float k_FragmentSpacing = 4;

        void OnMouseDownEvent(PointerDownEvent evt)
        {
            if (!(evt.target is VisualElement ve))
                return;

            var transition = ve.GetFirstAncestorOfType<TransitionPropertiesEditor>();

            if (transition != null && evt.button == (int)MouseButton.LeftMouse)
            {
                m_ToolState = ToolState.TransitionInteraction;
                m_InteractedTransition = transition;
                m_StartDrag = evt.position;
                RegisterCallback<MouseCaptureOutEvent>(OnCaptureLost);
                RegisterCallback<PointerMoveEvent>(OnMouseMoveEvent);
                this.CaptureMouse();
            }
        }

        void OnCaptureLost(MouseCaptureOutEvent e)
        {
            ReleaseDragging();
        }

        void OnMouseUpEvent(PointerUpEvent evt)
        {
            if (!(evt.target is VisualElement ve))
                return;
            if (m_ToolState == ToolState.MovingTransitions)
            {
                k_PickedElements.Clear();
                m_InteractedTransition.panel.PickAll(evt.position, k_PickedElements);

                foreach (var element in k_PickedElements)
                {
                    if (element == this)
                    {
                        var posInGroup = m_Container.WorldToLocal(evt.position);

                        foreach (var block in m_SelectedTransitionEditors)
                        {
                            block.RemoveFromHierarchy();
                        }

                        BlockDropped(posInGroup);
                        m_IsHoveringOver = false;
                        m_SelectedTransitionEditors.Clear(); //Clean so that ReleaseDragging do not put them back in the original context
                        break;
                    }
                }

                ReleaseDragging();
                evt.StopPropagation();
            }
            else if (evt.button == (int)MouseButton.RightMouse || evt.button == (int)MouseButton.LeftMouse)
            {
                var transition = m_InteractedTransition ?? ve.GetFirstOfType<TransitionPropertiesEditor>();
                if (transition != null)
                {
                    if (evt.modifiers.HasFlag(ClickSelector.PlatformMultiSelectModifier))
                        ToggleSelection(transition);
                    else if (evt.modifiers.HasFlag(EventModifiers.Shift) && m_TransitionSelectionManager.SelectedElements.Count > 0)
                    {
                        var lastSelectedElement = m_TransitionSelectionManager.LastSelectedForContinuousSelection;
                        var selectedElements = new List<ISelectableTransition>();
                        bool decreasing = false;

                        if (lastSelectedElement is TransitionPropertiesEditor lastSelectedEditor)
                        {
                            if (lastSelectedEditor.TransitionSupportEditor == this)
                            {
                                int lastIndex = m_TransitionPropertiesEditors.IndexOf(lastSelectedEditor);
                                int currentIndex = m_TransitionPropertiesEditors.IndexOf(transition);

                                if (currentIndex < lastIndex)
                                {
                                    (currentIndex, lastIndex) = (lastIndex, currentIndex);
                                }
                                for (int i = lastIndex; i <= currentIndex; ++i)
                                {
                                    selectedElements.Add(m_TransitionPropertiesEditors[i]);
                                }
                            }
                            else
                            {
                                {
                                    int lastIndex = m_StateTransitionsInspector.TransitionSupportEditors.IndexOf(lastSelectedEditor.TransitionSupportEditor);
                                    int currentIndex = m_StateTransitionsInspector.TransitionSupportEditors.IndexOf(this);

                                    if (currentIndex < lastIndex)
                                    {
                                        currentIndex++;
                                        lastIndex--;
                                        decreasing = true;
                                        (currentIndex, lastIndex) = (lastIndex, currentIndex);
                                    }
                                    else
                                    {
                                        currentIndex--;
                                        lastIndex++;
                                    }
                                    for (int i = lastIndex; i <= currentIndex; ++i)
                                    {
                                        if (m_StateTransitionsInspector.TransitionSupportEditors[i] is TransitionSupportEditor transitionSupportEditor)
                                        {
                                            foreach (var transitionPropertiesEditor in transitionSupportEditor.TransitionPropertiesEditors)
                                            {
                                                selectedElements.Add(transitionPropertiesEditor);
                                            }
                                        }
                                    }
                                }
                                {
                                    int lastIndex = decreasing ? TransitionPropertiesEditors.Count - 1 : 0;
                                    int currentIndex = TransitionPropertiesEditors.IndexOf(transition);

                                    if (currentIndex < lastIndex)
                                    {
                                        (currentIndex, lastIndex) = (lastIndex, currentIndex);
                                    }

                                    for (int i = lastIndex; i <= currentIndex; ++i)
                                    {
                                        selectedElements.Add(TransitionPropertiesEditors[i]);
                                    }
                                }
                                {
                                    int lastIndex = lastSelectedEditor.TransitionSupportEditor.TransitionPropertiesEditors.IndexOf(lastSelectedEditor);
                                    int currentIndex = decreasing ? 0 : lastSelectedEditor.TransitionSupportEditor.TransitionPropertiesEditors.Count - 1;

                                    if (currentIndex < lastIndex)
                                    {
                                        (currentIndex, lastIndex) = (lastIndex, currentIndex);
                                    }

                                    for (int i = lastIndex; i <= currentIndex; ++i)
                                    {
                                        selectedElements.Add(lastSelectedEditor.TransitionSupportEditor.TransitionPropertiesEditors[i]);
                                    }
                                }
                            }
                        }
                        else if (m_StateTransitionsInspector != null && lastSelectedElement is TransitionSupportEditor lastSelectedContainer)
                        {
                            {
                                int lastIndex = m_StateTransitionsInspector.TransitionSupportEditors.IndexOf(lastSelectedContainer);
                                int currentIndex = m_StateTransitionsInspector.TransitionSupportEditors.IndexOf(this);

                                if (currentIndex < lastIndex)
                                {
                                    currentIndex++;
                                    decreasing = true;
                                    (currentIndex, lastIndex) = (lastIndex, currentIndex);
                                }
                                else
                                {
                                    currentIndex--;
                                }
                                for (int i = lastIndex; i <= currentIndex; ++i)
                                {
                                    if (m_StateTransitionsInspector.TransitionSupportEditors[i] is ISelectableTransition selectableTransition)
                                        selectedElements.Add(selectableTransition);
                                }
                            }
                            {
                                int lastIndex = decreasing ? TransitionPropertiesEditors.Count - 1 : 0;
                                int currentIndex = TransitionPropertiesEditors.IndexOf(transition);

                                if (currentIndex < lastIndex)
                                {
                                    (currentIndex, lastIndex) = (lastIndex, currentIndex);
                                }

                                for (int i = lastIndex; i <= currentIndex; ++i)
                                {
                                    selectedElements.Add(TransitionPropertiesEditors[i]);
                                }
                            }
                        }

                        m_TransitionSelectionManager.SetContinuousSelection(selectedElements);
                    }
                    else
                        SetSelection(transition);
                    evt.StopPropagation();
                }

            }

            m_ToolState = ToolState.None;
            m_InteractedTransition = null;
        }

        void OnMouseMoveEvent(PointerMoveEvent evt)
        {
            //If the LeftButton is no longer pressed or another is pressed. Release the dragging
            if ((evt.pressedButtons != 1 << ((int)MouseButton.LeftMouse)))
            {
                ReleaseDragging();
            }

            if (m_ToolState == ToolState.TransitionInteraction)
            {
                if ((evt.position - m_StartDrag).sqrMagnitude > k_DragThresholdSquare)
                {
                    m_ToolState = ToolState.MovingTransitions;

                    var selectedBlockModels = GetSelectedTransitions();

                    m_SelectedTransitionEditors = selectedBlockModels.Contains(m_InteractedTransition) ? selectedBlockModels : new List<TransitionPropertiesEditor> { m_InteractedTransition };

                    m_DraggedTransitionOffset = m_InteractedTransition.WorldToLocal(evt.position);
                    m_DraggedTransitionIndex = m_SelectedTransitionEditors.FindIndex(t => t == m_InteractedTransition);

                    m_DraggedHeight = -k_FragmentSpacing;

                    foreach (var block in m_SelectedTransitionEditors)
                    {
                        m_DraggedHeight += block.layout.height + k_FragmentSpacing;

                        this.Add(block);
                        block.style.width = block.layout.width;
                        block.style.position = Position.Absolute;
                    }

                    StartBlockHoveringOver(m_DraggedHeight);
                    var posInGroup = m_Container.WorldToLocal(evt.position);
                    BlockHoveringOver(posInGroup);
                }
            }

            if (m_ToolState == ToolState.MovingTransitions)
            {
                var mousePosition = evt.localPosition;
                var myPos = new Vector2(mousePosition.x - m_DraggedTransitionOffset.x, mousePosition.y - m_DraggedTransitionOffset.y);
                m_InteractedTransition.style.left = myPos.x;
                m_InteractedTransition.style.top = myPos.y;

                //adjust other dragged transitions positions based on index
                var positionY = myPos.y;
                for (int i = m_DraggedTransitionIndex - 1; i >= 0; --i)
                {
                    m_SelectedTransitionEditors[i].style.left = myPos.x;
                    var height = m_SelectedTransitionEditors[i].layout.height + k_FragmentSpacing;
                    positionY -= height;
                    m_SelectedTransitionEditors[i].style.top = positionY;
                }

                positionY = myPos.y + m_InteractedTransition.layout.height + k_FragmentSpacing;
                for (int i = m_DraggedTransitionIndex + 1; i < m_SelectedTransitionEditors.Count; ++i)
                {
                    m_SelectedTransitionEditors[i].style.left = myPos.x;
                    m_SelectedTransitionEditors[i].style.top = positionY;
                    var height = m_SelectedTransitionEditors[i].layout.height + k_FragmentSpacing;
                    positionY += height;
                }

                k_PickedElements.Clear();
                m_InteractedTransition.panel.PickAll(evt.position, k_PickedElements);

                var found = false;
                foreach (var element in k_PickedElements)
                {
                    if (element == this)
                    {
                        found = true;
                        if (!m_IsHoveringOver)
                        {
                            m_IsHoveringOver = true;
                            StartBlockHoveringOver(m_DraggedHeight);
                        }

                        var posInGroup = m_Container.WorldToLocal(evt.position);
                        BlockHoveringOver(posInGroup);

                        break;
                    }
                }

                if (!found && m_IsHoveringOver)
                {
                    StopBlockHoveringOver();
                    m_IsHoveringOver = false;
                }
            }
        }

        void ReleaseDragging()
        {
            this.ReleaseMouse();
            UnregisterCallback<MouseCaptureOutEvent>(OnCaptureLost);
            UnregisterCallback<PointerMoveEvent>(OnMouseMoveEvent);

            if (m_IsHoveringOver)
            {
                StopBlockHoveringOver();
                m_IsHoveringOver = false;
            }

            if (m_ToolState == ToolState.MovingTransitions)
            {
                m_ToolState = ToolState.None;

                m_Container.Clear();
                for (int i = 0; i < m_TransitionPropertiesEditors.Count; ++i)
                {
                    m_Container.Add(m_TransitionPropertiesEditors[i]);

                    m_TransitionPropertiesEditors[i].style.width = StyleKeyword.Null;
                    m_TransitionPropertiesEditors[i].style.position = StyleKeyword.Null;
                    m_TransitionPropertiesEditors[i].style.top = StyleKeyword.Null;
                    m_TransitionPropertiesEditors[i].style.left = StyleKeyword.Null;
                }
            }
        }

        void StartBlockHoveringOver(float blocksHeight)
        {
            m_DragBlock.style.height = blocksHeight;
        }

        int GetBlockIndex(Vector2 posInContext)
        {
            if (m_TransitionPropertiesEditors.Count > 0)
            {
                int i = 0;
                float y = 0;
                for (; i < m_TransitionPropertiesEditors.Count - 1; i++)
                {
                    float blockY = m_TransitionPropertiesEditors[i].layout.height;
                    if (y + blockY > posInContext.y)
                        break;

                    y += blockY + m_TransitionPropertiesEditors[i].resolvedStyle.marginTop + m_Container[i].resolvedStyle.marginBottom;
                }

                return i;
            }

            return 0;
        }

        void BlockHoveringOver(Vector2 posInContext)
        {
            var index = GetBlockIndex(posInContext);

            if (index >= m_Container.childCount)
                m_Container.Add(m_DragBlock);
            else
                m_Container.Insert((index < 0 ? 0 : index), m_DragBlock);
        }

        void BlockDropped(Vector2 posInContext)
        {
            var index = GetBlockIndex(posInContext);

            RootView.Dispatch(new MoveTransitionCommand(
                TransitionSupportModel,
                m_SelectedTransitionEditors.ConvertAll(c => c.TransitionModel),
                index));

            StopBlockHoveringOver();
        }

        void StopBlockHoveringOver()
        {
            m_DragBlock.RemoveFromHierarchy();
        }

        /// <inheritdoc />
        public void UpdateCollapsible(UpdateCollapsibleVisitor visitor)
        {
            var isCollapsed = ((ModelInspectorViewModel)RootView.Model).TransitionInspectorState.GetTransitionSupportModelCollapsed(TransitionSupportModel, InStateInspector);
            EnableInClassList(collapsedUssClassName, isCollapsed);
            m_CollapseButton.SetValueWithoutNotify(!isCollapsed);
        }

        void ReorderEditors()
        {
            var transitionModels = TransitionSupportModel.Transitions;
            for (int i = 0; i < m_TransitionPropertiesEditors.Count;)
            {
                if (!transitionModels.Contains(m_TransitionPropertiesEditors[i].TransitionModel))
                {
                    m_TransitionPropertiesEditors[i].RemoveFromHierarchy();
                    m_TransitionSelectionManager.Remove(m_TransitionPropertiesEditors[i]);
                    m_TransitionPropertiesEditors[i].RemoveFromRootView();
                    m_TransitionPropertiesEditors.RemoveAt(i);

                }
                else
                    ++i;
            }
            for (int i = 0; i < transitionModels.Count; ++i)
            {
                if (i >= m_TransitionPropertiesEditors.Count || m_TransitionPropertiesEditors[i].TransitionModel != transitionModels[i])
                {
                    var existingContainer = m_TransitionPropertiesEditors.Count > i ? m_TransitionPropertiesEditors.FindIndex(i + 1, t => t.TransitionModel == transitionModels[i]) : -1;

                    if (existingContainer >= 0) // existing but not in the right place
                    {
                        var transitionContainer = m_TransitionPropertiesEditors[existingContainer];
                        m_TransitionPropertiesEditors.RemoveAt(existingContainer);
                        m_TransitionPropertiesEditors.Insert(i, transitionContainer);


                        if (i == 0)
                            transitionContainer.SendToBack();
                        else
                            transitionContainer.PlaceInFront(m_TransitionPropertiesEditors[i - 1]);
                    }
                    else
                    {
                        var propertiesEditor = ModelViewFactory.CreateUI<TransitionPropertiesEditor>(RootView, transitionModels[i], null, this);
                        propertiesEditor.AddToClassList(ussClassName.WithUssElement(k_PropertiesEditorName));

                        m_Container.Insert(i, propertiesEditor);
                        m_TransitionPropertiesEditors.Insert(i, propertiesEditor);
                    }
                }
            }
        }

        /// <inheritdoc />
        public override void RemoveFromRootView()
        {
            foreach (var editor in m_TransitionPropertiesEditors)
            {
                editor.RemoveFromRootView();
            }
            base.RemoveFromRootView();
        }
    }
}
