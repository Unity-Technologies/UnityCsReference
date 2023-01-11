// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A BlackboardElement to display a <see cref="GroupModel"/>.
    /// </summary>
    class BlackboardGroup : BlackboardElement, IModelViewContainer_Internal
    {
        /// <summary>
        /// The uss class for this element.
        /// </summary>
        public static new readonly string ussClassName = "ge-blackboard-variable-group";

        /// <summary>
        /// The uss class for the title container.
        /// </summary>
        public static readonly string titleContainerUssClassName = ussClassName.WithUssElement("title-container");

        /// <summary>
        /// The uss class name for the collapse toggle button.
        /// </summary>
        public static readonly string titleToggleUssClassName = ussClassName.WithUssElement("foldout");

        /// <summary>
        /// The uss class for the drag indicator.
        /// </summary>
        public static readonly string dragIndicatorUssClassName = ussClassName.WithUssElement("drag-indicator");

        /// <summary>
        /// The uss class for the element with the collapsed modifier.
        /// </summary>
        public static readonly string collapsedUssClassName = ussClassName.WithUssModifier("collapsed");
        /// <summary>
        /// The uss class for the element with the collapsed modifier.
        /// </summary>
        public static readonly string evenUssClassName = ussClassName.WithUssModifier("even");

        /// <summary>
        /// The Label displaying the title.
        /// </summary>
        protected VisualElement m_TitleLabel;

        /// <summary>
        /// The toggle button for collapsing the group.
        /// </summary>
        protected Toggle m_TitleToggle;

        /// <summary>
        /// The element containing the group's items representations.
        /// </summary>
        protected VisualElement m_ItemsContainer;

        /// <summary>
        /// The drag indicator element.
        /// </summary>
        protected VisualElement m_DragIndicator;

        /// <summary>
        /// The title element.
        /// </summary>
        protected VisualElement m_Title;

        SelectionDropper m_SelectionDropper;

        /// <summary>
        /// The name of the title part.
        /// </summary>
        protected static readonly string k_TitlePartName = "title-part";

        /// <summary>
        /// The name of the items part.
        /// </summary>
        protected static readonly string k_ItemsPartName = "items-part";

        GroupModel GroupModel => Model as GroupModel;

        /// <summary>
        /// The selection dropper assigned to this element.
        /// </summary>
        protected SelectionDropper SelectionDropper
        {
            get => m_SelectionDropper;
            set => this.ReplaceManipulator(ref m_SelectionDropper, value);
        }

        public IEnumerable<ModelView> ModelViews
        {
            get
            {
                foreach (var element in m_ItemsContainer.Children().OfType<ModelView>())
                {
                    yield return element;
                }
            }
        }

        /// <summary>
        /// The element containing all item ui representations
        /// </summary>
        public VisualElement ItemsContainer => m_ItemsContainer;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardGroup"/> class.
        /// </summary>
        public BlackboardGroup()
        {
            RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
            RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            RegisterCallback<DragLeaveEvent>(OnDragLeaveEvent);
        }

        /// <inheritdoc />
        protected override void BuildElementUI()
        {
            AddToClassList(ussClassName);

            base.BuildElementUI();

            m_Title = new VisualElement();
            m_Title.AddToClassList(titleContainerUssClassName);

            m_TitleToggle = new Toggle();
            m_TitleToggle.RegisterCallback<ChangeEvent<bool>>(OnTitleToggle);
            m_TitleToggle.AddToClassList(titleToggleUssClassName);

            m_Title.Add(m_TitleToggle);

            Add(m_Title);

            m_DragIndicator = new VisualElement { name = "drag-indicator" };
            m_DragIndicator.AddToClassList(dragIndicatorUssClassName);
            Add(m_DragIndicator);

            var selectionBorder = CreateSelectionBorder();
            if( selectionBorder != null)
                hierarchy.Add(selectionBorder);
        }

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            base.BuildPartList();

            PartList.AppendPart(EditableTitlePart.Create(k_TitlePartName, Model, this, ussClassName));
            PartList.AppendPart(BlackboardGroupItemsPart.Create(k_ItemsPartName, GraphElementModel, this, ussClassName));
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            m_TitleLabel = PartList.GetPart(k_TitlePartName).Root;

            m_Title.Insert(1, m_TitleLabel);

            m_ItemsContainer = PartList.GetPart(k_ItemsPartName).Root;
        }

        void OnTitleToggle(ChangeEvent<bool> e)
        {
            RootView.Dispatch(new ExpandVariableGroupCommand_Internal(GroupModel, e.newValue));
        }

        int GetDepth()
        {
            int cpt = 0;
            var current = GroupModel.ParentGroup;
            while (current != null)
            {
                cpt++;
                current = current.ParentGroup;
            }

            return cpt;
        }

        /// <inheritdoc />
        protected override void UpdateElementFromModel()
        {
            if (GraphElementModel.IsDroppable() && m_SelectionDropper == null)
                SelectionDropper = new SelectionDropper();

            base.UpdateElementFromModel();

            if (m_ItemsContainer != null)
            {
                int depth = GetDepth();

                EnableInClassList(evenUssClassName,depth % 2 == 0);
            }

            var expanded = BlackboardView.BlackboardViewModel.ViewState.GetGroupExpanded(GroupModel);

            m_TitleToggle.SetValueWithoutNotify(expanded);

            EnableInClassList(collapsedUssClassName, !expanded);
        }

        /// <summary>
        /// Computes the index of the element at the given position.
        /// </summary>
        /// <param name="localPos"> The position in this element coordinates.</param>
        /// <returns>The index of the element at the given position.</returns>
        protected int InsertionIndex(Vector2 localPos)
        {
            int index = 0;

            foreach (VisualElement child in m_ItemsContainer.Children())
            {
                Rect r = child.parent.ChangeCoordinatesTo(this,child.layout);

                if (localPos.y > (r.y + r.height / 2))
                {
                    ++index;
                }
                else
                {
                    break;
                }
            }

            return index;
        }

        /// <summary>
        /// Handles <see cref="DragUpdatedEvent"/>.
        /// </summary>
        /// <param name="evt">The event.</param>
        protected virtual void OnDragUpdatedEvent(DragUpdatedEvent evt)
        {
            BlackboardSection section = GetFirstOfType<BlackboardSection>();
            if (section == null)
                return;

            var draggedObjects = SelectionDropper.GetDraggedElements();

            if (!CanAcceptDrop(draggedObjects))
            {
                HideDragIndicator();
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                return;
            }

            int insertIndex = InsertionIndex(evt.localMousePosition);

            float indicatorY;
            if (m_ItemsContainer.childCount == 0)
                indicatorY = m_ItemsContainer.ChangeCoordinatesTo(this, Vector2.zero).y;
            else if (insertIndex >= m_ItemsContainer.childCount)
            {
                VisualElement lastChild = m_ItemsContainer[m_ItemsContainer.childCount - 1];

                indicatorY = lastChild.ChangeCoordinatesTo(this,
                    new Vector2(0, lastChild.layout.height + lastChild.resolvedStyle.marginBottom)).y;
            }
            else if (insertIndex == -1)
            {
                VisualElement childAtInsertIndex = m_ItemsContainer[0];

                indicatorY = childAtInsertIndex.ChangeCoordinatesTo(this,
                    new Vector2(0, -childAtInsertIndex.resolvedStyle.marginTop)).y;
            }
            else
            {
                VisualElement childAtInsertIndex = m_ItemsContainer[insertIndex];

                indicatorY = childAtInsertIndex.ChangeCoordinatesTo(this,
                    new Vector2(0, -childAtInsertIndex.resolvedStyle.marginTop)).y;
            }

            ShowDragIndicator(indicatorY);
            DragAndDrop.visualMode = DragAndDropVisualMode.Move;

            evt.StopPropagation();
        }

        /// <summary>
        /// handles <see cref="DragPerformEvent"/>.
        /// </summary>
        /// <param name="evt">The event.</param>
        protected virtual void OnDragPerformEvent(DragPerformEvent evt)
        {
            var selection = SelectionDropper.GetDraggedElements();

            if (!CanAcceptDrop(selection))
                return;

            int insertIndex = InsertionIndex(evt.localMousePosition);
            OnItemDropped(insertIndex, selection);

            HideDragIndicator();
            evt.StopPropagation();
        }

        static void RecurseGetVariables(IEnumerable<IGroupItemModel> items, List<VariableDeclarationModel> variables)
        {
            foreach (var item in items)
            {
                if( item is VariableDeclarationModel variable)
                    variables.Add(variable);
                else if( item is GroupModel group)
                    RecurseGetVariables(group.Items,variables);
            }
        }

        /// <summary>
        /// Returns whether this element can accept elements as its items.
        /// </summary>
        /// <param name="draggedObjects">The dragged elements.</param>
        /// <returns>Whether this element can accept elements as its items.</returns>
        public virtual bool CanAcceptDrop(IEnumerable<GraphElementModel> draggedObjects)
        {
            var items = draggedObjects.OfType<IGroupItemModel>();

            foreach (var obj in items)
            {
                if (obj is GroupModel vgm && GroupModel.IsIn(vgm))
                    return false;
            }
            var variables = new List<VariableDeclarationModel>();
            RecurseGetVariables(items, variables);

            var section = GroupModel.GetSection();
            string sectionName = section.Title;

            if (!variables.Any()) // We can always drag empty groups.
                return true;

            if (variables.All(t => t.GetSection() != section && !BlackboardView.BlackboardViewModel.GraphModelState.GraphModel.Stencil.CanConvertVariable(t, sectionName)))
                return false;

            return true;
        }

        /// <summary>
        /// Build the contextual menu.
        /// </summary>
        /// <param name="evt">The event.</param>
        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            IGroupItemModel model = ((evt.target as VisualElement)?.GetFirstOfType<ModelView>())?.Model as IGroupItemModel;

            if (model != null)
            {
                evt.menu.AppendAction("Create Group From Selection", _ =>
                {
                    model = BlackboardView.CreateGroupFromSelection_Internal(model);
                });
            }
        }

        void HideDragIndicator()
        {
            GetFirstAncestorOfType<BlackboardGroup>()?.HideDragIndicator();
            m_DragIndicator.style.visibility = Visibility.Hidden;
        }

        void ShowDragIndicator(float yPosition)
        {
            GetFirstAncestorOfType<BlackboardGroup>()?.HideDragIndicator();

            m_DragIndicator.style.visibility = Visibility.Visible;
            m_DragIndicator.style.left = 0;
            m_DragIndicator.style.top = yPosition - m_DragIndicator.resolvedStyle.height / 2;
        }

        /// <summary>
        /// Handles a drop of elements at the given index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="elements">The dropped <see cref="GraphElementModel"/>s.</param>
        protected void OnItemDropped(int index, IEnumerable<GraphElementModel> elements)
        {
            var droppedModels = elements.OfType<IGroupItemModel>().Where(t => !elements.Contains(t.ParentGroup)).ToList();

            droppedModels.Sort(GroupItemOrderComparer_Internal.Default);

            IGroupItemModel insertAfterModel = null;

            if (m_ItemsContainer.childCount != 0)
            {
                for (int i = 0; i < m_ItemsContainer.childCount && i < index; ++i)
                {
                    var itemModel = ((ModelView)m_ItemsContainer[i]).Model as IGroupItemModel;
                    if (!droppedModels.Contains(itemModel))
                    {
                        insertAfterModel = itemModel;
                    }
                }
            }

            RootView.Dispatch(new ReorderGroupItemsCommand((GroupModel)Model, insertAfterModel, droppedModels));
        }

        /// <summary>
        /// Handles the <see cref="DragLeaveEvent"/>.
        /// </summary>
        /// <param name="evt">The event.</param>
        protected virtual void OnDragLeaveEvent(DragLeaveEvent evt)
        {
            HideDragIndicator();
        }

        public override void ActivateRename()
        {
            (PartList.GetPart(k_TitlePartName) as EditableTitlePart)?.BeginEditing();
        }

        public override bool HandlePasteOperation(PasteOperation operation, string operationName, Vector2 delta, CopyPasteData copyPasteData)
        {
            if (copyPasteData.HasVariableContent_Internal())
            {
                BlackboardView.Dispatch(new PasteSerializedDataCommand(operation, operationName, delta, copyPasteData, GroupModel));
                return true;
            }

            return false;
        }
    }
}
