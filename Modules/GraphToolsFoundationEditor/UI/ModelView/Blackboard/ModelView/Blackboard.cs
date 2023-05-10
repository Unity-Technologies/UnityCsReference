// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A GraphElement to display a <see cref="BlackboardGraphModel"/>.
    /// </summary>
    class Blackboard : BlackboardElement
    {
        /// <summary>
        /// The uss class name for this element.
        /// </summary>
        public static new readonly string ussClassName = "ge-blackboard";
        public static readonly string contentContainerElementName = "content-container";

        /// <summary>
        /// The name of the header part.
        /// </summary>
        public static readonly string blackboardHeaderPartName = "header";

        /// <summary>
        /// The name of the content part.
        /// </summary>
        public static readonly string blackboardContentPartName = "content";

        /// <summary>
        /// The container for the content of the <see cref="BlackboardElement"/>, if any.
        /// </summary>
        protected VisualElement m_ContentContainer;

        /// <summary>
        /// The ScrollView used for the whole blackboard.
        /// </summary>
        public ScrollView ScrollView { get; protected set; }

        /// <summary>
        /// The container for the content of the <see cref="BlackboardElement"/>.
        /// </summary>
        public override VisualElement contentContainer => m_ContentContainer;

        /// <summary>
        /// Initializes a new instance of the <see cref="Blackboard"/> class.
        /// </summary>
        public Blackboard()
        {
            m_ContentContainer = new VisualElement { name = contentContainerElementName, pickingMode = PickingMode.Ignore };
            m_ContentContainer.AddToClassList(ussClassName.WithUssElement(contentContainerElementName));
            hierarchy.Add(m_ContentContainer);

            RegisterCallback<DragUpdatedEvent>(e =>
            {
                e.StopPropagation();
            });

            RegisterCallback<MouseDownEvent>(e =>
            {
                if (e.button == (int)MouseButton.LeftMouse)
                {
                    BlackboardView.Dispatch(new ClearSelectionCommand());
                }
                e.StopPropagation();
            });

            RegisterCallback<PromptItemLibraryEvent>(OnPromptItemLibrary);
            RegisterCallback<ShortcutShowItemLibraryEvent>(OnShortcutShowItemLibraryEvent);
        }

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            base.BuildPartList();

            PartList.AppendPart(BlackboardHeaderPart.Create(blackboardHeaderPartName, Model, this, ussClassName));
            PartList.AppendPart(BlackboardSectionListPart.Create(blackboardContentPartName, Model, this, ussClassName));
        }

        /// <inheritdoc />
        protected override void BuildElementUI()
        {
            base.BuildElementUI();

            ScrollView = new ScrollView(ScrollViewMode.VerticalAndHorizontal);

            hierarchy.Add(ScrollView);
            ScrollView.Add(contentContainer);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(ussClassName);
            this.AddStylesheet_Internal("Blackboard.uss");

            var headerPart = PartList.GetPart(blackboardHeaderPartName).Root;
            if (headerPart != null)
                hierarchy.Insert(0, headerPart);
        }

        /// <inheritdoc />
        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            BlackboardView.ViewSelection.BuildContextualMenu(evt);

            evt.menu.AppendAction("Select Unused", _ =>
            {
                BlackboardView.DispatchSelectUnusedVariables();
            }, _ => DropdownMenuAction.Status.Normal);
        }

        /// <summary>
        /// Callback for <see cref="ShortcutShowItemLibraryEvent"/>.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnShortcutShowItemLibraryEvent(ShortcutShowItemLibraryEvent e)
        {
            using (var itemLibraryEvent = PromptItemLibraryEvent.GetPooled(e.MousePosition))
            {
                itemLibraryEvent.target = e.target;
                SendEvent(itemLibraryEvent);
            }
            e.StopPropagation();
        }

        void OnPromptItemLibrary(PromptItemLibraryEvent e)
        {
            var graphModel = (Model as BlackboardGraphModel)?.GraphModel;

            if (graphModel == null)
            {
                return;
            }

            ItemLibraryService.ShowVariableTypes(
                RootView,
                (Stencil)graphModel.Stencil,
                RootView.GraphTool.Preferences,
                e.MenuPosition,
                (t, _) =>
                {
                    BlackboardView.Dispatch(new CreateGraphVariableDeclarationCommand
                    {
                        VariableName = "newVariable",
                        TypeHandle = t,
                        ModifierFlags = ModifierFlags.None,
                        IsExposed = true
                    });
                });

            e.StopPropagation();
        }
    }
}
