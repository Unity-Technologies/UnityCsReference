// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace UnityEditor.Overlays
{
    class OverlayMenu : VisualElement
    {
        static bool s_KeepAlive = false;
        static Action s_DelayUntilCanHide;

        const string k_UxmlPath = "UXML/Overlays/overlay-menu.uxml";
        static readonly string ussClassName = "overlay-menu";
        static VisualTreeAsset s_TreeAsset;

        readonly ScrollView m_ListRoot;
        int m_ListContentsHash;
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

        bool RebuildMenu()
        {
            m_ListRoot.Clear();
            var previousHash = m_ListContentsHash;
            m_ListContentsHash = 13;

            foreach (var overlay in m_Canvas.overlays)
            {
                if (!overlay.userControlledVisibility || !overlay.hasMenuEntry || m_Canvas.IsTransient(overlay))
                    continue;
                m_ListRoot.Add(new OverlayMenuItem() { overlay = overlay });
                m_ListContentsHash = Tuple.CombineHashCodes(overlay.GetHashCode(), m_ListContentsHash);
            }

            if (m_Canvas.transientOverlays.Any())
            {
                var separator = new VisualElement() { name = "Separator" };
                separator.AddToClassList("unity-separator");
                m_ListRoot.Add(separator);
            }

            foreach (var overlay in m_Canvas.transientOverlays)
            {
                m_ListRoot.Add(new OverlayMenuItem() { overlay = overlay });
                m_ListContentsHash = Tuple.CombineHashCodes(overlay.GetHashCode(), m_ListContentsHash);
            }

            return m_ListContentsHash != previousHash;
        }

        public void Show(bool atMousePosition = true)
        {
            // If the contents of the scroll view changed, we need to wait until the layout is recalculated before
            // placing the popup overlay. If the contents have _not_ changed, this event will not be invoked and thus
            // we need to immediately show the popup.
            if(RebuildMenu())
                m_ListRoot.RegisterCallback<GeometryChangedEvent>(atMousePosition ? PresentAtMouse : PresentAtCenter);
            else
                Present(atMousePosition);
        }

        void PresentAtCenter(GeometryChangedEvent _)
        {
            m_ListRoot.UnregisterCallback<GeometryChangedEvent>(PresentAtCenter);
            Present(false);
        }

        void PresentAtMouse(GeometryChangedEvent _)
        {
            m_ListRoot.UnregisterCallback<GeometryChangedEvent>(PresentAtMouse);
            Present(true);
        }

        void Present(bool atMousePosition)
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

        internal void AdjustToParentSize()
        {
            // If size limits have changed since the last time this method was called,
            // a re-computation of the layout will occur because the code below will update
            // some style properties.

            // Calling this as soon as the parent size is modified ensures that
            // the menu width and height will be appropriate when comes the time to
            // display the menu.

            float minWidth = m_InitialMinWidth.keyword == StyleKeyword.None ||
                             m_InitialMinWidth.keyword == StyleKeyword.Auto
                ? 0
                : m_InitialMinWidth.value;

            float minHeight = m_InitialMinHeight.keyword == StyleKeyword.None ||
                              m_InitialMinHeight.keyword == StyleKeyword.Auto
                ? 0
                : m_InitialMinHeight.value;

            float maxWidth = m_InitialMaxWidth.keyword == StyleKeyword.None
                ? float.MaxValue
                : m_InitialMaxWidth.value;

            float maxHeight = m_InitialMaxHeight.keyword == StyleKeyword.None
                ? float.MaxValue
                : m_InitialMaxHeight.value;

            var parentBounds = parent.layout;

            minWidth = Mathf.Min(parentBounds.width, minWidth);
            minHeight = Mathf.Min(parentBounds.height, minHeight);

            maxWidth = Mathf.Min(parentBounds.width, maxWidth);
            maxHeight = Mathf.Min(parentBounds.height, maxHeight);

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
