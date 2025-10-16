// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using Unity.GraphToolkit.InternalBridge;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The editor for a transition in the inspector.
    /// </summary>
    [UnityRestricted]
    internal class TransitionPropertiesEditor : ModelView, ISelectableTransition, ICollapsibleContainer
    {
        /// <summary>
        /// The USS class name added to this element.
        /// </summary>
        public static readonly string ussClassName = "ge-transition-properties-editor";

        static readonly string k_DragHandleName = "drag-handle";
        static readonly string k_CollapseButtonName = "collapse-button";
        static readonly string k_EnableToggleName = "enable-toggle";
        static readonly string k_TitleLabelName = "title-label";
        static readonly string k_OptionButtonName = "option-button";

        static readonly string k_ConditionEditorName = "condition-editor";


        /// <summary>
        /// The USS class name added to this element when collapsed.
        /// </summary>
        public static readonly string collapsedUssClassName = ussClassName.WithUssModifier(GraphElementHelper.collapsedUssModifier);

        /// <summary>
        /// The USS class name added to this element when selected.
        /// </summary>
        public static readonly string selectedUssClassName = ussClassName.WithUssModifier(GraphElementHelper.selectedUssModifier);

        const string k_DefaultTransitionTitle = "Transition";
        internal static readonly string k_ExpandMenuName = "Expand _RIGHT";
        internal static readonly string k_CollapseMenuName = "Collapse _LEFT";

        static readonly string k_MoveUpMenuName = "Move up";
        static readonly string k_MoveDownMenuName = "Move down";
        static readonly string k_MoveTopOption = "Move to top";
        static readonly string k_MoveBottomOption = "Move to bottom";

        static readonly FieldInfo k_TransitionEnabledField = typeof(TransitionModel).GetField("m_Enabled", BindingFlags.NonPublic | BindingFlags.Instance);

        TransitionSupportEditor m_TransitionSupportEditor;
        VisualElement m_Header;
        Toggle m_CollapseButton;
        Toggle m_EnableToggle;
        Image m_DebugIcon;
        EditableLabel m_TitleLabel;
        Button m_OptionButton;

        VisualElement m_Container;
        ConditionEditor m_ConditionEditor;

        protected FieldsInspector m_FieldsInspector;

        bool m_IsSelected;
        bool ISelectableElement.IsSelected
        {
            get => m_IsSelected;
            set
            {
                if (m_IsSelected == value)
                    return;
                m_IsSelected = value;
                if (m_IsSelected)
                    AddToClassList(selectedUssClassName);
                else
                {
                    RemoveFromClassList(selectedUssClassName);
                    m_ConditionEditor.ClearSelection();
                }
            }
        }

        public bool IsSelected => m_IsSelected;
        internal TransitionSelectionManager<ISelectableTransition> SelectionManager => m_TransitionSupportEditor.SelectionManager;

        /// <summary>
        /// The <see cref="TransitionModel"/> displayed by this editor.
        /// </summary>
        public TransitionModel TransitionModel => (TransitionModel)Model;

        /// <summary>
        /// The <see cref="GraphToolkit.Editor.TransitionSupportEditor"/> that contains this editor.
        /// </summary>
        public TransitionSupportEditor TransitionSupportEditor => m_TransitionSupportEditor;

        /// <summary>
        /// The condition editor contained in this editor.
        /// </summary>
        public ConditionEditor ConditionEditor => m_ConditionEditor;

        /// <summary>
        /// Creates a new instance of <see cref="TransitionPropertiesEditor"/>.
        /// </summary>
        /// <param name="transitionSupportEditor">The <see cref="TransitionSupportEditor"/>.</param>
        public TransitionPropertiesEditor(TransitionSupportEditor transitionSupportEditor)
        {
            m_TransitionSupportEditor = transitionSupportEditor;
        }

        /// <inheritdoc />
        protected override void BuildUI()
        {
            base.BuildUI();
            this.AddPackageStylesheet("TransitionPropertiesEditor.uss");
            BuildHeader();

            m_Container = new VisualElement { name = GraphElementHelper.containerName };
            m_Container.AddToClassList(ussClassName.WithUssElement(GraphElementHelper.containerName));
            hierarchy.Add(m_Container);
            AddToClassList(ussClassName);

            focusable = true;
        }

        /// <inheritdoc />
        public override VisualElement contentContainer => m_Container ?? this;

        void BuildHeader()
        {
            m_Header = new VisualElement { name = GraphElementHelper.headerName };
            m_Header.AddToClassList(ussClassName.WithUssElement(GraphElementHelper.headerName));


            var dragHandle = new VisualElement();
            dragHandle.AddToClassList(ussClassName.WithUssElement(k_DragHandleName));
            m_Header.Add(dragHandle);

            m_CollapseButton = new Toggle { name = k_CollapseButtonName };
            m_CollapseButton.AddToClassList(Foldout.toggleUssClassName);
            m_CollapseButton.AddToClassList(ussClassName.WithUssElement(k_CollapseButtonName));
            m_Header.Add(m_CollapseButton);
            m_CollapseButton.focusable = false; //currently a focusable foldout does not have the right styles.

            m_EnableToggle = new Toggle { name = k_EnableToggleName, value = TransitionModel.Enabled };
            m_EnableToggle.AddToClassList(ussClassName.WithUssElement(k_EnableToggleName));
            m_EnableToggle.RegisterValueChangedCallback(evt =>
            {
                RootView.Dispatch(new SetInspectedModelFieldCommand(evt.newValue, new[] { TransitionModel }, k_TransitionEnabledField));
            });
            m_Header.Add(m_EnableToggle);


            m_DebugIcon = new Image();
            m_DebugIcon.AddToClassList(ussClassName.WithUssElement("debug-icon"));
            m_Header.Add(m_DebugIcon);

            m_TitleLabel = new EditableLabel { name = k_TitleLabelName, Multiline = false, EditActionName = CommandMenuItemNames.Rename };
            m_TitleLabel.RegisterCallback<ChangeEvent<string>>(OnRename);
            m_TitleLabel.AddToClassList(ussClassName.WithUssElement(k_TitleLabelName));
            m_TitleLabel.MakeElementCapturePointer();
            m_Header.Add(m_TitleLabel);

            InitializeOptionsButton();
            m_Header.Add(m_OptionButton);

            Add(m_Header);
            m_CollapseButton.RegisterCallback<ChangeEvent<bool>>(OnCollapseChange);
        }

        void InitializeOptionsButton()
        {
            var contextMenuManipulator = new ContextualMenuManipulator(OnPopupMenuContextualPopulate);
            contextMenuManipulator.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, clickCount = 1 });
            m_OptionButton = new EditorToolbarButton(EditorGUIUtility.IconContent("_Menu").image as Texture2D, null) { name = k_OptionButtonName };
            m_OptionButton.clickable = null; // if the clickable is left on the button then the context menu manipulator may fail to trigger
            m_OptionButton.AddManipulator(contextMenuManipulator);
            m_OptionButton.AddToClassList(ussClassName.WithUssElement(k_OptionButtonName));
            m_OptionButton.RemoveFromClassList("unity-toolbar-button");

            m_OptionButton.MakeElementCapturePointer();
        }

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            m_FieldsInspector = SerializedFieldsInspector.Create("", new[] { TransitionModel }, this, ussClassName);

            PartList.AppendPart(m_FieldsInspector);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            var conditionTitle = new Label("Conditions");
            conditionTitle.AddToClassList(ussClassName.WithUssElement("condition-title"));
            m_Container.Add(conditionTitle);

            m_ConditionEditor = ModelViewFactory.CreateUI<ConditionEditor>(RootView, TransitionModel, ConditionEditorContext.Default, this);
            m_ConditionEditor.AddToClassList(ussClassName.WithUssElement(k_ConditionEditorName));
            m_Container.Add(m_ConditionEditor);
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            var value = String.IsNullOrEmpty(TransitionModel.Title) ? k_DefaultTransitionTitle : TransitionModel.Title;
            m_TitleLabel.SetValueWithoutNotify(value);

            m_EnableToggle.SetValueWithoutNotify(TransitionModel.Enabled);
        }

        /// <inheritdoc />
        public void UpdateCollapsible(UpdateCollapsibleVisitor visitor)
        {
            var isCollapsed = ((ModelInspectorViewModel)RootView.Model).TransitionInspectorState.GetTransitionModelCollapsed(TransitionModel, m_TransitionSupportEditor.InStateInspector);
            EnableInClassList(collapsedUssClassName, isCollapsed);
            m_CollapseButton.SetValueWithoutNotify(!isCollapsed);
        }

        void OnCollapseChange(ChangeEvent<bool> e)
        {
            RootView.Dispatch(new CollapseTransitionsCommand(TransitionModel, !e.newValue, m_TransitionSupportEditor.InStateInspector));
        }

        void OnRename(ChangeEvent<string> e)
        {
            RootView.Dispatch(new RenameElementsCommand(TransitionModel, e.newValue));
        }

        void OnPopupMenuContextualPopulate(ContextualMenuPopulateEvent evt)
        {
            MakeMenu(evt.menu, false);
            evt.StopPropagation();
        }

        /// <inheritdoc />
        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt) => MakeMenu(evt.menu, true);

        /// <summary>
        /// Create the option menu for this editor.
        /// </summary>
        /// <param name="menu">The menu on which to add elements.</param>
        /// <param name="contextual">Whether it is the contextual menu or the option menu in the header.</param>
        protected virtual void MakeMenu(DropdownMenu menu, bool contextual)
        {
            if (contextual && IsSelected)
            {
                menu.AppendAction(EventCommandNamesBridge.Delete, _ =>
                {
                    m_TransitionSupportEditor.DeleteTransitions(m_TransitionSupportEditor.GetSelectedTransitionModels());
                });
            }
            else
            {
                menu.AppendAction(EventCommandNamesBridge.Delete, _ =>
                {
                    m_TransitionSupportEditor.DeleteTransitions(new[] { TransitionModel });
                });
            }

            menu.AppendSeparator();

            if (!m_TitleLabel.IsInEditMode)
            {
                menu.AppendAction(EventCommandNamesBridge.Rename, _ => m_TitleLabel.BeginEditing());
            }

            menu.AppendSeparator();


            var isCollapsed = ((ModelInspectorViewModel)RootView.Model).TransitionInspectorState.GetTransitionModelCollapsed(TransitionModel, m_TransitionSupportEditor.InStateInspector);

            menu.AppendAction(k_ExpandMenuName, _ =>
            {
                RootView.Dispatch(new CollapseTransitionsCommand(TransitionModel, false, m_TransitionSupportEditor.InStateInspector));
            }, isCollapsed ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            menu.AppendAction(k_CollapseMenuName, _ =>
            {
                RootView.Dispatch(new CollapseTransitionsCommand(TransitionModel, true, m_TransitionSupportEditor.InStateInspector));
            }, isCollapsed ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);

            menu.AppendSeparator();

            var selectedTransitions = m_TransitionSupportEditor.GetSelectedTransitions();

            menu.AppendAction(k_MoveUpMenuName, _ =>
            {
                m_TransitionSupportEditor.ShiftTransitions(selectedTransitions, true);
            }, m_TransitionSupportEditor.CanShiftTransitions(selectedTransitions, true) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            menu.AppendAction(k_MoveDownMenuName, _ =>
            {
                m_TransitionSupportEditor.ShiftTransitions(selectedTransitions, false);
            }, m_TransitionSupportEditor.CanShiftTransitions(selectedTransitions, false) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            menu.AppendAction(k_MoveTopOption, _ =>
            {
                m_TransitionSupportEditor.MoveTransitions(selectedTransitions, true);
            }, m_TransitionSupportEditor.CanMoveTransitions(selectedTransitions, true) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            menu.AppendAction(k_MoveBottomOption, _ =>
            {
                m_TransitionSupportEditor.MoveTransitions(selectedTransitions, false);
            }, m_TransitionSupportEditor.CanMoveTransitions(selectedTransitions, false) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        /// <inheritdoc />
        public void BeginEditing()
        {
            m_TitleLabel.BeginEditing();
        }

        /// <inheritdoc />
        public override void RemoveFromRootView()
        {
            base.RemoveFromRootView();
            ConditionEditor.RemoveFromRootView();
        }
    }
}
