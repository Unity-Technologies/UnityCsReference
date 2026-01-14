// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile.Elements
{
    /// <summary>
    /// UI Toolkit version of DropdownButton that looks like a single button
    /// It is required to make this button focusable after constructor
    /// "focusable = true" can't be in constructor since it's virtual member call in constructor
    /// </summary>
    internal class DropdownButton : VisualElement
    {
        const int k_DropdownWidth = 16;

        readonly GenericMenu m_Menu;
        readonly Action m_DefaultClicked;
        readonly Label m_Label;
        readonly VisualElement m_ArrowContainer;

        public DropdownButton(string text, Action defaultClicked, GenericMenu menu)
        {
            m_Menu = menu;
            m_DefaultClicked = defaultClicked;

            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;

            m_Label = new Label(text);
            m_Label.style.flexGrow = 1;
            m_Label.pickingMode = PickingMode.Ignore;

            Add(m_Label);

            if (m_Menu != null)
            {
                var separator = new VisualElement();
                separator.style.width = 1;
                separator.style.height = 12;
                separator.style.backgroundColor = new Color(0, 0, 0, 0.3f);
                separator.style.alignSelf = Align.Center;
                separator.pickingMode = PickingMode.Ignore;
                Add(separator);

                m_ArrowContainer = new VisualElement();
                m_ArrowContainer.style.width = k_DropdownWidth;
                m_ArrowContainer.style.alignSelf = Align.Stretch;
                m_ArrowContainer.style.alignItems = Align.Center;
                m_ArrowContainer.style.justifyContent = Justify.Center;
                m_ArrowContainer.style.marginRight = -6;

                var arrow = new Label("▼");
                arrow.style.fontSize = 6;
                arrow.pickingMode = PickingMode.Ignore;

                m_ArrowContainer.Add(arrow);
                Add(m_ArrowContainer);

                m_ArrowContainer.RegisterCallback<PointerDownEvent>(OnDropdownClicked);
            }

            RegisterCallback<ClickEvent>(OnMainClicked);
            RegisterCallback<KeyDownEvent>(OnKeyDown);

            AddToClassList("unity-button");
            AddToClassList("form-button");
            AddToClassList("ml-medium");
        }

        public void SetText(string text)
        {
            m_Label.text = text;
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.Space:
                    ExecuteMainAction();
                    evt.StopPropagation();
                    break;

                case KeyCode.DownArrow:
                    if (m_Menu != null)
                    {
                        OpenDropdown();
                        evt.StopPropagation();
                    }
                    break;
            }
        }

        void OnMainClicked(ClickEvent evt)
        {
            if (m_Menu != null && evt.target == m_ArrowContainer)
            {
                return;
            }

            ExecuteMainAction();
            evt.StopPropagation();
        }

        void OnDropdownClicked(PointerDownEvent evt)
        {
            OpenDropdown();
        }

        void OpenDropdown()
        {
            if (m_Menu != null)
            {
                var worldBound = this.worldBound;
                var rect = new Rect(worldBound.x, worldBound.y + worldBound.height, worldBound.width, 0);
                m_Menu.DropDown(rect);
            }
        }

        void ExecuteMainAction() => m_DefaultClicked?.Invoke();
    }
}
