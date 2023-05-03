// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.ProjectSettings
{
    internal class TabButton : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<TabButton, UxmlTraits>
        {
        }

        internal new class UxmlTraits : VisualElement.UxmlTraits
        {
            readonly UxmlStringAttributeDescription m_Text = new() { name = "text" };
            readonly UxmlStringAttributeDescription m_Target = new() { name = "target" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var item = ve as TabButton;

                item.m_Label.text = m_Text.GetValueFromBag(bag, cc);
                item.TargetId = m_Target.GetValueFromBag(bag, cc);
            }
        }

        static readonly string UxmlName = "TabButton";
        static readonly string s_UssClassName = "unity-tab-button";
        static readonly string s_UssActiveClassName = s_UssClassName + "--active";

        public bool IsCloseable { get; set; }
        public string TargetId { get; private set; }
        public VisualElement Target { get; set; }

        public event Action<TabButton> OnSelect;
        public event Action<TabButton> OnClose;

        Label m_Label;
        VisualElement m_BottomBar;

        public TabButton()
        {
            Init();
        }

        public TabButton(string text, VisualElement target)
        {
            Init();
            m_Label.text = text;
            Target = target;
        }

        void PopulateContextMenu(ContextualMenuPopulateEvent populateEvent)
        {
            var dropdownMenu = populateEvent.menu;

            if (IsCloseable)
                dropdownMenu.AppendAction("Close Tab", e => { OnClose?.Invoke(this); });
        }

        void CreateContextMenu(VisualElement visualElement)
        {
            var menuManipulator = new ContextualMenuManipulator(PopulateContextMenu);

            visualElement.focusable = true;
            visualElement.pickingMode = PickingMode.Position;
            visualElement.AddManipulator(menuManipulator);
        }

        void Init()
        {
            AddToClassList(s_UssClassName);

            var visualTree = EditorGUIUtility.Load($"UXML/InspectorWindow/{UxmlName}.uxml") as VisualTreeAsset;
            visualTree.CloneTree(this);

            m_Label = this.Q<Label>("Label");
            m_BottomBar = this.Q<VisualElement>("BottomBar");

            CreateContextMenu(this);
            RegisterCallback<MouseDownEvent>(OnMouseDownEvent);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChangedEvent);
        }

        void OnGeometryChangedEvent(GeometryChangedEvent evt)
        {
            if(m_BottomBar.style.display == DisplayStyle.Flex)
                RecalculateBottomBarPosition();
        }

        public void Select()
        {
            AddToClassList(s_UssActiveClassName);

            if (Target == null)
                return;

            Target.style.display = DisplayStyle.Flex;
            Target.style.flexGrow = 1;
            m_BottomBar.style.display = DisplayStyle.Flex;
            GetFirstAncestorOfType<TabbedView>().hierarchy.Add(m_BottomBar);
            RecalculateBottomBarPosition();
        }

        void RecalculateBottomBarPosition()
        {
            m_BottomBar.style.width = layout.width - resolvedStyle.borderRightWidth - resolvedStyle.borderLeftWidth;
            m_BottomBar.style.top = parent.layout.height;
            m_BottomBar.style.left = m_BottomBar.parent.resolvedStyle.paddingLeft + resolvedStyle.borderLeftWidth + layout.x;
        }

        public void Deselect()
        {
            RemoveFromClassList(s_UssActiveClassName);
            MarkDirtyRepaint();

            if (Target == null)
                return;
            Target.style.display = DisplayStyle.None;
            Target.style.flexGrow = 0;
            m_BottomBar.style.display = DisplayStyle.None;
            parent.Add(m_BottomBar);
        }

        void OnMouseDownEvent(MouseDownEvent e)
        {
            switch (e.button)
            {
                case 0:
                {
                    OnSelect?.Invoke(this);
                    break;
                }

                case 2 when IsCloseable:
                {
                    OnClose?.Invoke(this);
                    break;
                }
            }

            e.StopImmediatePropagation();
        }
    }
}
