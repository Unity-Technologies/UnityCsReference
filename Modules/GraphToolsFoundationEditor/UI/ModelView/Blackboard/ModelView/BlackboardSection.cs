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
    /// A section of the blackboard. A section contains a group of variables from the graph.
    /// </summary>
    class BlackboardSection : BlackboardGroup
    {
        /// <summary>
        /// The uss class name for this element.
        /// </summary>
        public static new readonly string ussClassName = "ge-blackboard-section";

        /// <summary>
        /// The uss class name for the header.
        /// </summary>
        public static readonly string headerUssClassName = ussClassName.WithUssElement("header");

        /// <summary>
        /// The uss class name for the add button.
        /// </summary>
        public static readonly string addButtonUssClassName = ussClassName.WithUssElement("add");

        /// <summary>
        /// The uss class name for the title placeholder, which reserves the place for the title since it is in absolute position.
        /// </summary>
        public static readonly string titlePlaceholderUssClassName = ussClassName.WithUssElement("title-placeholder");

        /// <summary>
        /// The add button.
        /// </summary>
        protected Button m_AddButton;

        public SectionModel SectionModel => Model as SectionModel;

        /// <inheritdoc />
        protected override void BuildElementUI()
        {
            AddToClassList(ussClassName);

            base.BuildElementUI();

            m_Title.AddToClassList(headerUssClassName);

            m_AddButton = new Button {text = "+"};
            m_AddButton.AddToClassList(addButtonUssClassName);
            m_Title.Add(m_AddButton);

            var titlePlaceHolder = new VisualElement();
            titlePlaceHolder.AddToClassList(titlePlaceholderUssClassName);
            Insert(0, titlePlaceHolder);

            m_AddButton.clicked += () =>
            {
                var menu = new GenericMenu();

                List<Stencil.MenuItem> items = new List<Stencil.MenuItem>();
                items.Add(new Stencil.MenuItem{name ="Create Empty Group",action = () =>
                {
                    RootView.Dispatch(new BlackboardGroupCreateCommand(SectionModel, SectionModel.Items.LastOrDefault()));
                }});
                items.Add(new Stencil.MenuItem());

                ((Stencil) GraphElementModel.GraphModel.Stencil)?.PopulateBlackboardCreateMenu(name, items, RootView, SectionModel);

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

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        Blackboard m_Blackboard;

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

            if (BlackboardView != null && BlackboardView.BlackboardViewModel.ViewState.GetGroupExpanded((GroupModel) Model))
            {
                var pos = m_Blackboard.ScrollView.ChangeCoordinatesTo(contentContainer, Vector2.zero);

                var max = contentContainer.layout.height - m_Title.layout.height;

                m_Title.style.top = Mathf.Min(max, Mathf.Max(0, pos.y));
            }
            else
            {
                m_Title.style.top = 0;
            }
        }

        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            ScrollViewChange();
        }
    }
}
