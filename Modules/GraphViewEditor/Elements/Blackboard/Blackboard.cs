// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    public class Blackboard : GraphElement
    {
        private const int k_DefaultWidth = 200;
        private const float k_DefaultHeight = 400;
        private VisualElement m_MainContainer;
        private VisualElement m_Root;
        private Label m_TitleLabel;
        private Label m_SubTitleLabel;
        private ScrollView m_ScrollView;
        private VisualElement m_ContentContainer;
        private VisualElement m_HeaderItem;
        private Button m_AddButton;
        private bool m_Scrollable = true;
        internal static readonly string StyleSheetPath = "StyleSheets/GraphView/Blackboard.uss";

        public Action<Blackboard> addItemRequested { get; set; }
        public Action<Blackboard, int, VisualElement> moveItemRequested { get; set; }
        public Action<Blackboard, VisualElement, string> editTextRequested { get; set; }

        public override string title
        {
            get { return m_TitleLabel.text; }
            set { m_TitleLabel.text = value; }
        }

        public string subTitle
        {
            get { return m_SubTitleLabel.text; }
            set { m_SubTitleLabel.text = value; }
        }

        public override VisualElement contentContainer { get { return m_ContentContainer; } }

        public bool scrollable
        {
            get
            {
                return m_Scrollable;
            }
            set
            {
                if (m_Scrollable == value)
                    return;

                m_Scrollable = value;

                if (m_Scrollable)
                {
                    if (m_ScrollView == null)
                    {
                        m_ScrollView = new ScrollView();
                        m_ScrollView.stretchContentWidth = true;
                    }

                    // Remove the sections container from the content item and add it to the scrollview
                    m_ContentContainer.RemoveFromHierarchy();
                    m_Root.Add(m_ScrollView);
                    m_ScrollView.Add(m_ContentContainer);
                    style.positionType = PositionType.Manual; // As both the width and height can be changed by the user using a resizer

                    // If the current the current geometry is invalid then set a default size

                    if (layout.width == 0 || layout.height == 0)
                    {
                        layout = new Rect(layout.x, layout.y, layout.width == 0 ? k_DefaultWidth : layout.width, layout.height == 0 ? k_DefaultHeight : layout.height);
                    }

                    AddToClassList("scrollable");
                }
                else
                {
                    if (m_ScrollView != null)
                    {
                        // Remove the sections container from the scrollview and add it to the content item
                        style.positionType = PositionType.Absolute; // As the height is automatically computed from the content but the width can be changed by the user using a resizer
                        m_ScrollView.RemoveFromHierarchy();
                        m_ContentContainer.RemoveFromHierarchy();
                        m_Root.Add(m_ContentContainer);
                    }
                    RemoveFromClassList("scrollable");
                }
            }
        }

        public Blackboard()
        {
            var tpl = EditorGUIUtility.Load("UXML/GraphView/Blackboard.uxml") as VisualTreeAsset;
            AddStyleSheetPath(StyleSheetPath);

            m_MainContainer = tpl.CloneTree(null);
            m_MainContainer.AddToClassList("mainContainer");

            m_Root = m_MainContainer.Q("content");

            m_HeaderItem = m_MainContainer.Q("header");
            m_HeaderItem.AddToClassList("blackboardHeader");

            m_AddButton = m_MainContainer.Q(name: "addButton") as Button;
            m_AddButton.clickable.clicked += () => {
                if (addItemRequested != null)
                {
                    addItemRequested(this);
                }
            };

            m_TitleLabel = m_MainContainer.Q<Label>(name: "titleLabel");
            m_SubTitleLabel = m_MainContainer.Q<Label>(name: "subTitleLabel");
            m_ContentContainer = m_MainContainer.Q<VisualElement>(name: "contentContainer");

            shadow.Add(m_MainContainer);

            capabilities |= Capabilities.Movable | Capabilities.Resizable;
            clippingOptions = ClippingOptions.ClipAndCacheContents;

            ClearClassList();
            AddToClassList("blackboard");

            this.AddManipulator(new Dragger { clampToParentEdges = true });

            scrollable = false;

            shadow.Add(new Resizer());

            RegisterCallback<DragUpdatedEvent>(e =>
            {
                e.StopPropagation();
            });
        }
    }
}
