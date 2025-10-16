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
    /// An inspector for a state, that shows all related transitions.
    /// </summary>
    [UnityRestricted]
    internal class StateTransitionsInspector : ModelInspector
    {
        /// <summary>
        /// The USS class name added to this element.
        /// </summary>
        public new static readonly string ussClassName = "ge-state-transitions-inspector";

        /// <summary>
        /// The USS class name added to the filter element.
        /// </summary>
        public static readonly string filterUssClassName = ussClassName.WithUssElement("filter");

        static readonly string k_PropertiesContainerName = "properties-container";

        TransitionSelectionManager<ISelectableTransition> m_SelectionManager;

        List<ModelView> m_TransitionSupportEditors = new();
        ToggleButtonGroup m_FilterButton;

        /// <summary>
        /// The list of <see cref="TransitionSupportEditor"/>s in this inspector.
        /// </summary>
        public IReadOnlyList<ModelView> TransitionSupportEditors => m_TransitionSupportEditors;

        internal TransitionSelectionManager<ISelectableTransition> SelectionManager => m_SelectionManager;

        /// <inheritdoc />
        protected override void BuildUI()
        {
            focusable = true;
            m_SelectionManager = new TransitionSelectionManager<ISelectableTransition>();
            this.AddPackageStylesheet("StateTransitionsInspector.uss");
            AddToClassList(ussClassName);

            RegisterCallback<MouseUpEvent>(OnMouseUpEvent);
            RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
            RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            RegisterCallback<KeyDownEvent>(OnKeyDown);

            m_FilterButton = new ToggleButtonGroup();
            m_FilterButton.isMultipleSelection = true;
            m_FilterButton.RegisterCallback<ChangeEvent<ToggleButtonGroupState>>(OnFilterChanged);
            m_FilterButton.AddToClassList(filterUssClassName);

            m_FilterButton.Add(new Button()
            {
                text = "OnEnter",
                iconImage = Background.FromTexture2D(AssetDatabase.LoadAssetAtPath<Texture2D>(
                    EditorGUIUtility.isProSkin ?
                    $"{GraphElementHelper.k_IconFolder}/StateMachine/Transitions/d_OnEnterTransition.png" :
                    $"{GraphElementHelper.k_IconFolder}StateMachine/Transitions/OnEnterTransition.png"))
            });
            m_FilterButton.Add(new Button()
            {
                text = "Local",
                iconImage = Background.FromTexture2D(AssetDatabase.LoadAssetAtPath<Texture2D>(
                    EditorGUIUtility.isProSkin ?
                    $"{GraphElementHelper.k_IconFolder}StateMachine/Transitions/d_AnyStateTransition.png" :
                    $"{GraphElementHelper.k_IconFolder}StateMachine/Transitions/AnyStateTransition.png"))
            });
            var button = new Button()
            {
                text = "Incoming",
                iconImage = Background.FromTexture2D(AssetDatabase.LoadAssetAtPath<Texture2D>($"{GraphElementHelper.k_IconFolder}Port/ExecutionInput@4x.png"))
            };
            button.AddToClassList(filterUssClassName.WithUssElement("incoming-button"));
            m_FilterButton.Add(button);

            button = new Button()
            {
                text = "Outgoing",
                iconImage = Background.FromTexture2D(AssetDatabase.LoadAssetAtPath<Texture2D>($"{GraphElementHelper.k_IconFolder}Port/ExecutionOutput@4x.png"))
            };
            button.AddToClassList(filterUssClassName.WithUssElement("outgoing-button"));
            m_FilterButton.Add(button);

            Add(m_FilterButton);

            var filterPref = EditorPrefs.GetInt(filterUssClassName, 1 << 0 | 1 << 1 | 1 << 2 | 1 << 3);
            m_FilterButton.value = new ToggleButtonGroupState((ulong)filterPref, 4);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            RebuildTransitionSupports();
        }

        void RebuildTransitionSupports()
        {
            m_SelectionManager.ClearSelection();

            foreach (var container in m_TransitionSupportEditors)
            {
                container.RemoveFromRootView();
                container.RemoveFromHierarchy();
            }

            var transitionSupportModels = GetOrderedSupportModels();

            foreach (var transitionSupportModel in transitionSupportModels)
            {
                var transitionPropertiesContainer = ModelViewFactory.CreateUI<TransitionSupportEditor>(RootView, transitionSupportModel, null, this);
                transitionPropertiesContainer.AddToClassList(ussClassName.WithUssElement(k_PropertiesContainerName));
                Add(transitionPropertiesContainer);
                m_TransitionSupportEditors.Add(transitionPropertiesContainer);
            }

            UpdateFilter();
        }

        void OnValidateCommand(ValidateCommandEvent evt)
        {
            if (panel.GetCapturingElement(PointerId.mousePointerId) != null)
                return;

            if ((evt.commandName == EventCommandNamesBridge.Delete || evt.commandName == EventCommandNamesBridge.SoftDelete) && m_SelectionManager.SelectedElements.Count > 0)
            {
                evt.StopPropagation();
                evt.imguiEvent?.Use();
            }
            else if (evt.commandName == EventCommandNamesBridge.FrameSelected)
            {
                evt.StopPropagation();
                evt.imguiEvent?.Use();
            }
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            var selectedElements = m_SelectionManager.SelectedElements;
            if (selectedElements.Count > 0)
            {
                var selectedTransitions = new List<TransitionModel>();
                var selectedTransitionSupports = new List<TransitionSupportModel>();

                foreach (var selectedElement in selectedElements)
                {
                    if (selectedElement is TransitionSupportEditor container)
                    {
                        selectedTransitionSupports.Add(container.TransitionSupportModel);
                    }
                    else if (selectedElement is TransitionPropertiesEditor editor)
                    {
                        selectedTransitions.Add(editor.TransitionModel);
                    }
                }

                if (evt.keyCode == KeyCode.LeftArrow)
                {
                    RootView.Dispatch(new CollapseTransitionsCommand(selectedTransitionSupports, selectedTransitions, true, true));
                    evt.StopPropagation();
                }
                else if (evt.keyCode == KeyCode.RightArrow)
                {
                    RootView.Dispatch(new CollapseTransitionsCommand(selectedTransitionSupports, selectedTransitions, false, true));
                    evt.StopPropagation();
                }
            }

            if (evt.isPropagationStopped)
                evt.imguiEvent?.Use();
        }

        void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            if (panel.GetCapturingElement(PointerId.mousePointerId) != null)
                return;

            if (evt.commandName == EventCommandNamesBridge.Delete || evt.commandName == EventCommandNamesBridge.SoftDelete)
            {
                if (evt.commandName == EventCommandNamesBridge.Delete || evt.commandName == EventCommandNamesBridge.SoftDelete)
                {
                    DeleteSelection();
                }
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNamesBridge.Rename)
            {
                var selected = m_SelectionManager.SelectedElements[^1];
                selected.BeginEditing();
                evt.StopPropagation();
            }

            if (evt.isPropagationStopped)
                evt.imguiEvent?.Use();
        }

        public void DeleteSelection()
        {
            var transitionSupportModels = new List<TransitionSupportModel>();
            var transitionModels = new Dictionary<TransitionSupportModel, List<TransitionModel>>();
            foreach (var element in m_SelectionManager.SelectedElements)
            {
                if (element is TransitionSupportEditor container)
                {
                    transitionSupportModels.Add(container.TransitionSupportModel);
                }
                else if (element is TransitionPropertiesEditor editor)
                {
                    var transitionSupportContainer = editor.GetFirstAncestorOfType<TransitionSupportEditor>();
                    if (!transitionSupportContainer.IsSelected)
                    {
                        if (!transitionModels.TryGetValue(transitionSupportContainer.TransitionSupportModel, out var transitions))
                        {
                            transitions = new List<TransitionModel>();
                            transitionModels.Add(transitionSupportContainer.TransitionSupportModel, transitions);
                        }

                        transitions.Add(editor.TransitionModel);
                    }
                }
            }

            RootView.Dispatch(new RemoveTransitionElementsCommand(transitionSupportModels, transitionModels));
        }

        void OnFilterChanged(ChangeEvent<ToggleButtonGroupState> evt)
        {
            UpdateFilter();
            var value = m_FilterButton.value;
            EditorPrefs.SetInt(filterUssClassName, (value[0] ? 1 : 0) << 0 | (value[1] ? 1 : 0) << 1 | (value[2] ? 1 : 0) << 2 | (value[3] ? 1 : 0) << 3);
        }

        void UpdateFilter()
        {
            var value = m_FilterButton.value;

            bool[] hasOne = new bool[4];

            foreach (var transitionSupport in m_TransitionSupportEditors)
            {
                if (transitionSupport.Model is not TransitionSupportModel transitionSupportModel)
                    continue;
                switch (transitionSupportModel.TransitionSupportKind)
                {
                    case TransitionSupportKind.OnEnter:
                        transitionSupport.style.display = value[0] ? DisplayStyle.Flex : DisplayStyle.None;
                        hasOne[0] = true;
                        break;
                    case TransitionSupportKind.Local:
                        transitionSupport.style.display = value[1] ? DisplayStyle.Flex : DisplayStyle.None;
                        hasOne[1] = true;
                        break;
                    case TransitionSupportKind.StateToState:
                        if (Models.Contains(transitionSupportModel.ToPort.NodeModel))
                        {
                            transitionSupport.style.display = value[2] ? DisplayStyle.Flex : DisplayStyle.None;
                            hasOne[2] = true;
                        }
                        else
                        {
                            transitionSupport.style.display = value[3] ? DisplayStyle.Flex : DisplayStyle.None;
                            hasOne[3] = true;
                        }

                        break;
                    case TransitionSupportKind.Self:
                        transitionSupport.style.display = value[2] || value[3] ? DisplayStyle.Flex : DisplayStyle.None;
                        hasOne[2] = true;
                        hasOne[3] = true;
                        break;
                }

                if (transitionSupport.style.display == DisplayStyle.None && transitionSupport is ISelectableTransition selectableTransition)
                    m_SelectionManager.Remove(selectableTransition);
            }
            for (int i = 0; i < 4; ++i)
            {
                m_FilterButton[i].SetEnabled(hasOne[i]);
            }

            return;
        }

        public override bool IsEmpty()
        {
            foreach (var model in Models)
            {
                if (model is StateModel baseStateModel)
                {
                    foreach (var wire in baseStateModel.GraphModel.WireModels)
                    {
                        if (wire is TransitionSupportModel && (wire.FromPort.NodeModel == baseStateModel || wire.ToPort.NodeModel == baseStateModel))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        class TransitionSupportModelComparer : IComparer<TransitionSupportModel>
        {
            IReadOnlyList<Model> m_Models;
            public TransitionSupportModelComparer(IReadOnlyList<Model> models)
            {
                m_Models = models;
            }

            public int ListIndexOf<T>(IReadOnlyList<T> list, T item)
            {
                for (int i = 0; i < list.Count; ++i)
                {
                    if (list[i].Equals(item))
                        return i;
                }

                return -1;
            }

            static int[] s_Map = new int[] { 0, 2, 1, 3 };
            public int Compare(TransitionSupportModel x, TransitionSupportModel y)
            {
                if (x == null)
                    return -1;
                if (y == null)
                    return 1;
                int kindCompare = Comparer<int>.Default.Compare(s_Map[(int)x.TransitionSupportKind], s_Map[(int)y.TransitionSupportKind]);
                if (kindCompare != 0)
                    return kindCompare;
                // For transition with the same kind, put incoming before outgoing, then use the order of the inspector Models
                if (x.TransitionSupportKind == TransitionSupportKind.StateToState)
                {
                    int xModelIndex = ListIndexOf(m_Models, x.ToPort.NodeModel);
                    int yModelIndex = ListIndexOf(m_Models, y.ToPort.NodeModel);

                    if (xModelIndex == -1)
                        xModelIndex = int.MaxValue;
                    if (yModelIndex == -1)
                        yModelIndex = int.MaxValue;

                    if (xModelIndex == int.MaxValue && yModelIndex == int.MaxValue)
                    {
                        xModelIndex = ListIndexOf(m_Models, x.FromPort.NodeModel);
                        yModelIndex = ListIndexOf(m_Models, y.FromPort.NodeModel);

                        if (xModelIndex == -1)
                            xModelIndex = int.MaxValue;
                        if (yModelIndex == -1)
                            yModelIndex = int.MaxValue;

                        int fromPortCompare = Comparer<int>.Default.Compare(xModelIndex, yModelIndex);

                        if (fromPortCompare != 0)
                            return fromPortCompare;
                    }
                    else
                    {
                        int toPortCompare = Comparer<int>.Default.Compare(xModelIndex, yModelIndex);

                        if (toPortCompare != 0)
                            return toPortCompare;
                    }
                }

                //Otherwise stabilize the sort by using the Guid
                return Comparer<Hash128>.Default.Compare(x.Guid, y.Guid);
            }
        }

        List<TransitionSupportModel> GetOrderedSupportModels()
        {
            var transitionSupportModels = new List<TransitionSupportModel>();
            foreach (var model in Models)
            {
                if (model is StateModel stateModel)
                {
                    foreach (var wire in stateModel.GetConnectedWires())
                    {
                        if (wire is TransitionSupportModel tsm)
                        {
                            transitionSupportModels.Add(tsm);
                        }
                    }
                }
            }

            transitionSupportModels.Sort(new TransitionSupportModelComparer(Models));

            return transitionSupportModels;
        }

        public void Update(GraphModelStateComponent.Changeset modelChangeSet)
        {
            if (modelChangeSet.DeletedModels.Count > 0)
            {
                for (int i = 0; i < m_TransitionSupportEditors.Count;)
                {
                    if (modelChangeSet.DeletedModels.Contains(m_TransitionSupportEditors[i].Model.Guid))
                    {
                        m_TransitionSupportEditors[i].RemoveFromRootView();
                        m_TransitionSupportEditors[i].RemoveFromHierarchy();
                        if (m_TransitionSupportEditors[i] is ISelectableTransition selectableTransition)
                            m_SelectionManager.Remove(selectableTransition);
                        m_TransitionSupportEditors.RemoveAt(i);
                    }
                    else
                        ++i;
                }
            }

            bool added = false;
            var transitionSupportModels = GetOrderedSupportModels();

            for (int i = 0; i < transitionSupportModels.Count; ++i)
            {
                if (i >= m_TransitionSupportEditors.Count || m_TransitionSupportEditors[i].Model != transitionSupportModels[i])
                {
                    var existingContainer = m_TransitionSupportEditors.Count > i ? m_TransitionSupportEditors.FindIndex(i + 1, t => t.Model == transitionSupportModels[i]) : -1;

                    if (existingContainer >= 0) // existing but not in the right place
                    {
                        var transitionContainer = m_TransitionSupportEditors[existingContainer];
                        m_TransitionSupportEditors.RemoveAt(existingContainer);
                        m_TransitionSupportEditors.Insert(i, transitionContainer);


                        if (i == 0)
                            transitionContainer.SendToBack();
                        else
                            transitionContainer.PlaceInFront(m_TransitionSupportEditors[i - 1]);
                    }
                    else
                    {
                        added = true;
                        var transitionPropertiesContainer = ModelViewFactory.CreateUI<TransitionSupportEditor>(RootView, transitionSupportModels[i], null, this);
                        transitionPropertiesContainer.AddToClassList(ussClassName.WithUssElement(k_PropertiesContainerName));
                        Insert(i + 1 /* because of filter */, transitionPropertiesContainer);
                        m_TransitionSupportEditors.Insert(i, transitionPropertiesContainer);
                    }
                }
            }
            if (added)
                UpdateFilter();
        }
        void OnMouseUpEvent(MouseUpEvent evt)
        {
            if (!(evt.target is VisualElement ve))
                return;
            var container = ve.GetFirstOfType<TransitionSupportEditor>();

            if (evt.button == (int)MouseButton.LeftMouse || evt.button == (int)MouseButton.RightMouse)
            {
                if (container != null)
                {
                    if (evt.modifiers.HasFlag(ClickSelector.PlatformMultiSelectModifier))
                        m_SelectionManager.Toggle(container);
                    else if (evt.modifiers.HasFlag(EventModifiers.Shift) && m_SelectionManager.SelectedElements.Count > 0)
                    {
                        var lastSelectedElement = m_SelectionManager.LastSelectedForContinuousSelection;
                        var selectedElements = new List<ISelectableTransition>();

                        bool decreasing = false;

                        if (lastSelectedElement is TransitionSupportEditor lastSelectedContainer)
                        {
                            int lastIndex = m_TransitionSupportEditors.IndexOf(lastSelectedContainer);
                            int currentIndex = m_TransitionSupportEditors.IndexOf(container);

                            if (currentIndex < lastIndex)
                            {
                                (currentIndex, lastIndex) = (lastIndex, currentIndex);
                            }
                            for (int i = lastIndex; i <= currentIndex; ++i)
                            {
                                if (m_TransitionSupportEditors[i] is ISelectableTransition selectableTransition)
                                    selectedElements.Add(selectableTransition);
                            }
                        }
                        else if (lastSelectedElement is TransitionPropertiesEditor lastSelectedEditor)
                        {
                            var lastContainer = lastSelectedEditor.GetFirstAncestorOfType<TransitionSupportEditor>();
                            {
                                int lastIndex = m_TransitionSupportEditors.IndexOf(lastContainer);
                                int currentIndex = m_TransitionSupportEditors.IndexOf(container);

                                if (currentIndex < lastIndex)
                                {
                                    lastIndex--;
                                    decreasing = true;
                                    (currentIndex, lastIndex) = (lastIndex, currentIndex);
                                }
                                else
                                {
                                    lastIndex++;
                                }
                                for (int i = lastIndex; i <= currentIndex; ++i)
                                {
                                    if (m_TransitionSupportEditors[i] is ISelectableTransition selectableTransition)
                                        selectedElements.Add(selectableTransition);
                                }
                            }
                            {
                                int lastIndex = lastContainer.TransitionPropertiesEditors.IndexOf(lastSelectedEditor);
                                int currentIndex = decreasing ? 0 : lastContainer.TransitionPropertiesEditors.Count - 1;

                                if (currentIndex < lastIndex)
                                {
                                    (currentIndex, lastIndex) = (lastIndex, currentIndex);
                                }

                                for (int i = lastIndex; i <= currentIndex; ++i)
                                {
                                    selectedElements.Add(lastContainer.TransitionPropertiesEditors[i]);
                                }
                                selectedElements.Add(container);
                            }
                        }
                        m_SelectionManager.SetContinuousSelection(selectedElements);
                    }
                    else
                        m_SelectionManager.Select(container);
                }
                evt.StopPropagation();
            }
        }

        public override void RemoveFromRootView()
        {
            foreach (var transitionPropertiesContainer in m_TransitionSupportEditors)
            {
                transitionPropertiesContainer.RemoveFromRootView();
            }
            base.RemoveFromRootView();
        }
    }
}
