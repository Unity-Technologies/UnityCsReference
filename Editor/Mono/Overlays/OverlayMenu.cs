// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace UnityEditor.Overlays
{
    internal class OverlayMenu : VisualElement
    {
        static bool s_KeepAlive = false;
        static Action s_DelayUntilCanHide;

        const string k_UxmlPath = "UXML/Overlays/overlay-menu.uxml";
        static readonly string ussClassName = "overlay-menu";
        static VisualTreeAsset s_TreeAsset;

        readonly ListView m_ListRoot;
        readonly Toggle m_Toggle;
        TextElement m_DropdownText;
        readonly Button m_Dropdown;
        readonly OverlayCanvas m_Canvas;

        List<Overlay> m_OverlayToShow = new List<Overlay>();

        public OverlayMenu(OverlayCanvas canvas)
        {
            m_Canvas = canvas;
            name = ussClassName;
            if (s_TreeAsset == null)
                s_TreeAsset = EditorGUIUtility.Load(k_UxmlPath) as VisualTreeAsset;

            if (s_TreeAsset != null)
                s_TreeAsset.CloneTree(this);

            AddToClassList(ussClassName);

            m_Toggle = this.Q<Toggle>("overlay-toggle");
            m_Toggle.RegisterCallback<ChangeEvent<bool>>((evt) =>
            {
                canvas.SetOverlaysEnabled(evt.newValue);
            });

            m_ListRoot = this.Q<ListView>("OverlayList");
            m_ListRoot.makeItem = CreateListItem;
            m_ListRoot.bindItem = BindListItem;
            m_ListRoot.itemsSource = m_OverlayToShow;
            m_ListRoot.fixedItemHeight = 20;
            m_ListRoot.selectionType = SelectionType.None;

            m_Dropdown = this.Q<Button>("preset-dropdown");
            m_Dropdown.clicked += DropdownOnClicked;

            m_DropdownText = this.Q<TextElement>(classes: "preset-dropdown-text");
            var lastSavedPreset = canvas.lastAppliedPresetName;
            m_DropdownText.text =  string.IsNullOrEmpty(lastSavedPreset) ? L10n.Tr("Window Preset") : lastSavedPreset;

            canvas.afterOverlaysInitialized += () =>
            {
                m_DropdownText.text = canvas.lastAppliedPresetName;
            };

            RegisterCallback<FocusOutEvent>(evt =>
            {
                if (s_KeepAlive)
                {
                    var focusController = evt.relatedTarget?.focusController;
                    s_DelayUntilCanHide += () => CheckIfShouldHide(focusController?.focusedElement as VisualElement);
                }
                else
                    CheckIfShouldHide(evt.relatedTarget as VisualElement);
            });

            canvas.overlaysEnabledChanged += OnOverlayEnabledChanged;

            focusable = true;
            Hide();
        }

        void OnOverlayEnabledChanged(bool visibility)
        {
            m_Toggle.SetValueWithoutNotify(visibility);
            m_Dropdown.SetEnabled(visibility);
        }

        void CheckIfShouldHide(VisualElement focused)
        {
            if (focused == null || !Contains(focused))
                Hide();
        }

        void DropdownOnClicked()
        {
            var menu = new GenericMenu();
            OverlayPresetManager.GenerateMenu(menu, "", m_Canvas.containerWindow);
            menu.DropDown(m_Dropdown.worldBound);
        }

        VisualElement CreateListItem()
        {
            return new OverlayMenuItem();
        }

        void BindListItem(VisualElement element, int index)
        {
            ((OverlayMenuItem)element).overlay = m_OverlayToShow[index];
        }

        void RebuildMenu(IEnumerable<Overlay> overlays)
        {
            m_OverlayToShow.Clear();

            foreach (var overlay in overlays)
            {
                if (!overlay.userControlledVisibility || !overlay.hasMenuEntry)
                    continue;

                m_OverlayToShow.Add(overlay);
            }

            m_ListRoot.Rebuild();
        }

        public void Show(IEnumerable<Overlay> overlays, Rect bounds, Vector2 mousePosition)
        {
            RebuildMenu(overlays);

            var size = new Vector2(258, 190);
            var menuRect = new Rect(mousePosition, size);

            //Ensure menu is within bounds
            var parentBounds = parent.layout;
            size.x = Mathf.Min(parentBounds.width, size.x);
            size.y = Mathf.Min(parentBounds.height, size.y);

            if (menuRect.xMax > parentBounds.width)
                menuRect.x -= menuRect.xMax - parentBounds.width;
            if (menuRect.xMin < 0)
                menuRect.x = 0;

            if (menuRect.yMax > parentBounds.height)
                menuRect.y -= menuRect.yMax - parentBounds.height;
            if (menuRect.yMin < 0)
                menuRect.y = 0;

            style.top = menuRect.y;
            style.left = menuRect.x;
            style.width = menuRect.width;
            style.height = menuRect.height;

            style.display = DisplayStyle.Flex;

            OnOverlayEnabledChanged(m_Canvas.overlaysEnabled);
            Focus();
        }

        public void Hide()
        {
            style.display = DisplayStyle.None;

            foreach (var overlay in m_OverlayToShow)
                overlay.SetHighlightEnabled(false);
        }

        public static void SetKeepAlive(bool keepAlive)
        {
            s_KeepAlive = keepAlive;

            if (!s_KeepAlive)
            {
                s_DelayUntilCanHide?.Invoke();
                s_DelayUntilCanHide = null;
            }
        }
    }
}
