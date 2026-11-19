// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolsAuthoringFramework.InternalEditorBridge;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A section of the blackboard. A section contains a group of variables from the graph.
    /// </summary>
    [UnityRestricted]
    internal class BlackboardSection : BlackboardGroup
    {
        /// <summary>
        /// The USS class name added to this element.
        /// </summary>
        public static new readonly string ussClassName = "ge-blackboard-section";

        /// <summary>
        /// The USS class name added to the header.
        /// </summary>
        public static readonly string headerUssClassName = ussClassName.WithUssElement(GraphElementHelper.headerName);

        /// <summary>
        /// The USS class name added to the add button.
        /// </summary>
        public static readonly string addButtonUssClassName = ussClassName.WithUssElement("add");

        /// <summary>
        /// The USS class name added to the title placeholder, which reserves the place for the title since it is in absolute position.
        /// </summary>
        public static readonly string titlePlaceholderUssClassName = ussClassName.WithUssElement("title-placeholder");

        /// <summary>
        /// The add button.
        /// </summary>
        protected Button m_AddButton;

        Blackboard m_Blackboard;

        public override Texture Icon => null;

        public SectionModel SectionModel => Model as SectionModel;

        /// <inheritdoc />
        protected override void BuildUI()
        {
            AddToClassList(ussClassName);

            base.BuildUI();

            m_Title.AddToClassList(headerUssClassName);

            var icon = EditorGUIUtilityBridge.LoadIcon("_Menu");
            m_AddButton = new Button(Background.FromTexture2D(icon));
            m_AddButton.AddToClassList(addButtonUssClassName);
            m_Title.Add(m_AddButton);

            if (SectionModel.Title != GraphModel.DefaultSectionName)
            {
                var titlePlaceHolder = new VisualElement();
                titlePlaceHolder.AddToClassList(titlePlaceholderUssClassName);
                Insert(0, titlePlaceHolder);
            }

            m_AddButton.clickable.clicked += () =>
            {
                var menu = new GenericMenu();

                List<BlackboardContentModel.MenuItem> items = new List<BlackboardContentModel.MenuItem>();
                items.Add(new BlackboardContentModel.MenuItem
                {
                    name = "Select All",
                    action = () =>
                    {
                        RootView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, SectionModel.Items.OfTypeToList<GraphElementModel, IGroupItemModel>()));
                    }
                });
                items.Add(new BlackboardContentModel.MenuItem());

                GroupModel selectedGroupInThisSection = BlackboardView.Blackboard.GetTargetGroupForNewVariable(SectionModel);
                BlackboardView.BlackboardRootViewModel.BlackboardContentState.BlackboardModel.PopulateCreateMenu(SectionModel.Title, items, RootView, selectedGroupInThisSection);

                foreach (var item in items)
                {
                    if (!string.IsNullOrEmpty(item.name))
                        menu.AddItem(new GUIContent(item.name), false, () => item.action());
                    else
                        menu.AddSeparator("");
                }

                var menuPosition = new Vector2(m_AddButton.layout.xMin, m_AddButton.layout.yMax);
                menuPosition = m_AddButton.parent.LocalToWorld(menuPosition);
                menu.DropDown(new Rect(menuPosition, Vector2.zero));
            };

            m_Title.style.display = SectionModel.Title == GraphModel.DefaultSectionName ? DisplayStyle.None : DisplayStyle.Flex;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        protected internal override VisualElement CreateSelectionBorder()
        {
            return null;
        }

        void OnAttachToPanel(AttachToPanelEvent e)
        {
            m_Blackboard = GetFirstAncestorOfType<Blackboard>();

            if (m_Blackboard != null)
            {
                RegisterCallback<GeometryChangedEvent>(OnScrollGeometryChanged);
                m_Blackboard.ScrollView.verticalScroller.valueChanged += OnScrollbarChanged;
            }
        }

        void OnDetachFromPanel(DetachFromPanelEvent e)
        {
            if (m_Blackboard != null)
            {
                UnregisterCallback<GeometryChangedEvent>(OnScrollGeometryChanged);
                m_Blackboard.ScrollView.verticalScroller.valueChanged -= OnScrollbarChanged;
                m_Blackboard = null;
            }
        }

        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            m_TitleLabel.AddToClassList(ussClassName.WithUssElement(k_TitlePartName));

            m_Title.BringToFront();
            m_Title.style.top = 0;

            m_AddButton.BringToFront();
        }

        void OnScrollbarChanged(float value)
        {
            ScrollViewChange();
        }

        void OnScrollGeometryChanged(GeometryChangedEvent evt)
        {
            ScrollViewChange();
        }

        /// <summary>
        /// Makes sure the section header is visible when scrolling.
        /// </summary>
        protected void ScrollViewChange()
        {
            if (m_Blackboard == null)
                return;

            if (BlackboardView != null && BlackboardView.BlackboardRootViewModel.ViewState.GetGroupExpanded((GroupModel)Model))
            {
                var pos = m_Blackboard.ScrollView.ChangeCoordinatesTo(contentContainer, Vector2.zero).y - 1;

                var max = contentContainer.layout.height - m_Title.layout.height;

                m_Title.style.top = Mathf.Min(max, Mathf.Max(0, pos));
            }
            else
            {
                m_Title.style.top = 0;
            }
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            base.UpdateUIFromModel(visitor);

            ScrollViewChange();
        }
    }
}
