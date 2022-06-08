// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
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

        readonly ScrollView m_ListRoot;
        readonly Toggle m_Toggle;
        TextElement m_DropdownText;
        readonly Button m_Dropdown;
        readonly OverlayCanvas m_Canvas;

        StyleFloat m_InitialMinWidth;
        StyleFloat m_InitialMinHeight;
        StyleFloat m_InitialMaxWidth;
        StyleFloat m_InitialMaxHeight;

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

            m_ListRoot = this.Q<ScrollView>("OverlayList");
            m_ListRoot.horizontalScrollerVisibility = ScrollerVisibility.Auto;
            m_ListRoot.verticalScrollerVisibility = ScrollerVisibility.Auto;

            m_Dropdown = this.Q<Button>("preset-dropdown");
            m_Dropdown.clicked += DropdownOnClicked;

            m_DropdownText = this.Q<TextElement>(classes: "preset-dropdown-text");
            var lastSavedPreset = canvas.lastAppliedPresetName;
            m_DropdownText.text =  string.IsNullOrEmpty(lastSavedPreset) ? L10n.Tr("Window Preset") : lastSavedPreset;

            canvas.afterOverlaysInitialized += () =>
            {
                m_DropdownText.text = canvas.lastAppliedPresetName;
                RebuildMenu(canvas.overlays);
            };

            RegisterCallback<FocusOutEvent>(evt =>
            {
                if (s_KeepAlive)
                {
                    var targetFocusController = evt.relatedTarget?.focusController;
                    s_DelayUntilCanHide += () => CheckIfShouldHide(targetFocusController?.focusedElement as VisualElement);
                }
                else
                    CheckIfShouldHide(evt.relatedTarget as VisualElement);
            });

            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);

            canvas.overlaysEnabledChanged += OnOverlayEnabledChanged;

            focusable = true;
            Hide();
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            m_InitialMinWidth = resolvedStyle.minWidth;
            m_InitialMinHeight = resolvedStyle.minHeight;
            m_InitialMaxWidth = resolvedStyle.maxWidth;
            m_InitialMaxHeight = resolvedStyle.maxHeight;

            AdjustToParentSize();

            UnregisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
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

        void RebuildMenu(IEnumerable<Overlay> overlays)
        {
            m_ListRoot.Clear();

            foreach (var overlay in overlays)
            {
                if (!overlay.userControlledVisibility || !overlay.hasMenuEntry)
                    continue;

                var item = new OverlayMenuItem();
                item.overlay = overlay;
                m_ListRoot.Add(item);
            }
        }

        public void Show(IEnumerable<Overlay> overlays, bool atMousePosition)
        {
            var parentBounds = parent.layout;

            var currentLayout = layout;
            var currentSize = new Vector2(currentLayout.width, currentLayout.height);

            // currentSize is at most as large as parentBounds, because of
            // the style adjustments done in AdjustToParentSize().

            if (!atMousePosition)
            {
                //Show has been triggered by a menu entry and not by a shortcut key
                //Place the menu at the center of the parent Bounds
                var p = parentBounds.center - currentSize / 2f;
                style.left = p.x;
                style.top = p.y;
            }
            else
            {
                var mousePosition = PointerDeviceState.GetPointerPosition(PointerId.mousePointerId, ContextType.Editor);
                mousePosition = parent.WorldToLocal(mousePosition);

                var menuRect = new Rect(mousePosition, currentSize);
                if(menuRect.xMax > parentBounds.width)
                    menuRect.x -= menuRect.xMax - parentBounds.width;
                if(menuRect.xMin < 0)
                    menuRect.x = 0;

                if(menuRect.yMax > parentBounds.height)
                    menuRect.y -= menuRect.yMax - parentBounds.height;
                if(menuRect.yMin < 0)
                    menuRect.y = 0;

                style.left = menuRect.x;
                style.top = menuRect.y;
            }

            // Change `visibility` instead of `display` to ensure that menu size is
            // recomputed even when it is not shown.
            style.visibility = Visibility.Visible;

            OnOverlayEnabledChanged(m_Canvas.overlaysEnabled);
            Focus();
        }

        void ResolveSizeLimits(out float minWidth, out float minHeight, out float maxWidth, out float maxHeight)
        {
            if (m_InitialMinWidth.keyword == StyleKeyword.None || m_InitialMinWidth.keyword == StyleKeyword.Auto)
            {
                minWidth = 0;
            }
            else
            {
                minWidth = m_InitialMinWidth.value;
            }

            if (m_InitialMinHeight.keyword == StyleKeyword.None || m_InitialMinHeight.keyword == StyleKeyword.Auto)
            {
                minHeight = 0;
            }
            else
            {
                minHeight = m_InitialMinHeight.value;
            }

            if (m_InitialMaxWidth.keyword == StyleKeyword.None)
            {
                maxWidth = float.MaxValue;
            }
            else
            {
                maxWidth = m_InitialMaxWidth.value;
            }

            if (m_InitialMaxHeight.keyword == StyleKeyword.None)
            {
                maxHeight = float.MaxValue;
            }
            else
            {
                maxHeight = m_InitialMaxHeight.value;
            }

            var parentBounds = parent.layout;

            minWidth = Mathf.Min(parentBounds.width, minWidth);
            minHeight = Mathf.Min(parentBounds.height, minHeight);

            maxWidth = Mathf.Min(parentBounds.width, maxWidth);
            maxHeight = Mathf.Min(parentBounds.height, maxHeight);;
        }

        internal void AdjustToParentSize()
        {
            // If size limits have changed since the last time this method was called,
            // a recomputation of the layout will occur because the code below will update
            // some style properties.

            // Calling this as soon as the parent size is modified ensures that
            // the menu width and height will be appropriate when comes the time to
            // display the menu.

            ResolveSizeLimits(out var minWidth, out var minHeight, out var maxWidth, out var maxHeight);

            style.minWidth = minWidth;
            style.minHeight = minHeight;
            style.maxWidth = maxWidth;
            style.maxHeight = maxHeight;
        }

        public void Hide()
        {
            style.visibility = Visibility.Hidden;

            var overlays = m_ListRoot.Children().OfType<OverlayMenuItem>();
            foreach (var overlay in overlays)
                overlay.overlay.SetHighlightEnabled(false);
        }

        public bool isShown => resolvedStyle.visibility != Visibility.Hidden;

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
