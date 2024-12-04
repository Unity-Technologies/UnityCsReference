// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.ProjectSettings
{
    internal class TabButton : VisualElement
    {
        [Serializable]
        internal new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(text), "text"),
                    new (nameof(target), "target"),
                });
            }

            #pragma warning disable 649
            [SerializeField] string text;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags text_UxmlAttributeFlags;
            [SerializeField] string target;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags target_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new TabButton();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (TabButton)obj;
                if (ShouldWriteAttributeValue(text_UxmlAttributeFlags))
                    e.m_Label.text = text;
                if (ShouldWriteAttributeValue(target_UxmlAttributeFlags))
                    e.TargetId = target;
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
            //we need to calculate the width of the bottom bar based on the tab width and remove border width from left and right
            m_BottomBar.style.width = layout.width - resolvedStyle.borderRightWidth - resolvedStyle.borderLeftWidth;
            //we extract additional padding from the parent to get the correct position + tab height
            m_BottomBar.style.top = (parent.worldBound.y - parent.parent.worldBound.y) + parent.layout.height;
            //we extract additional padding from the parent to get the correct position + tab position x + border width
            m_BottomBar.style.left = (parent.worldBound.x - parent.parent.worldBound.x) + layout.x + resolvedStyle.borderLeftWidth;
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
