// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.ShortcutManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.EditorWindow;

namespace UnityEditor.Overlays
{
    sealed class OverlayPresetDropdown : BasePopupField<string, string>
    {
        readonly EditorWindow m_TargetWindow;

        public OverlayPresetDropdown(EditorWindow targetWindow)
        {
            m_TargetWindow = targetWindow;
            createMenuCallback = DropdownUtility.CreateDropdown;
            SetValueWithoutNotify(m_TargetWindow.overlayCanvas.lastAppliedPresetName);
            
        }

        internal override void AddMenuItems(IGenericMenu menu)
        {
            OverlayPresetManager.GenerateMenu(menu, "", m_TargetWindow);
        }

        internal override string GetValueToDisplay()
        {
            if (m_TargetWindow == null)
                return string.Empty;

            return m_TargetWindow.overlayCanvas.lastAppliedPresetName;
        }

        //We don't actually use this but we are required to implement it
        internal override string GetListItemToDisplay(string item) => item;
    }

    [Overlay(typeof(EditorWindow), "Overlays/OverlayMenu", "Overlay Menu", "overlay-menu", defaultDockZone = DockZone.LeftColumn, defaultLayout = Layout.HorizontalToolbar, defaultDisplay = true, defaultDockIndex = 0)]
    sealed class OverlayMenu : Overlay, ICreateHorizontalToolbar, ICreateVerticalToolbar
    {
        class Toolbar : OverlayToolbar
        {
            OverlayMenu m_Menu;
            VisualElement m_GlobalToolbar;
            VisualElement m_TransientToolbar;
            VisualElement m_Separator;

            List<(Overlay overlay, int priority)> m_Overlays;

            public Toolbar(OverlayMenu menu, bool horizontal)
            {
                m_Menu = menu;
                Add(m_GlobalToolbar = new VisualElement());
                m_GlobalToolbar.AddToClassList("unity-editor-toolbar-element");
                m_GlobalToolbar.style.flexDirection = horizontal ? FlexDirection.Row : FlexDirection.Column;

                m_Separator = new VisualElement() { name = "Separator" };
                m_Separator.AddToClassList("unity-separator");
                Add(m_Separator);

                Add(m_TransientToolbar = new VisualElement());
                m_TransientToolbar.AddToClassList("unity-editor-toolbar-element");
                m_TransientToolbar.style.flexDirection = horizontal ? FlexDirection.Row : FlexDirection.Column;

                m_Overlays = new List<(Overlay, int)>();
                RebuildContent();
            }

            static EditorToolbarToggle CreateToggle(Overlay overlay)
            {
                var iconContent = overlay.GetCollapsedIconContent();
                var image = iconContent.image as Texture2D;
                var toggle = image != null ? new EditorToolbarToggle(image) : new EditorToolbarToggle(iconContent.text);
                toggle.tooltip = overlay.displayName;
                toggle.SetValueWithoutNotify(overlay.displayed);
                toggle.RegisterValueChangedCallback((evt) => overlay.displayed = evt.newValue);
                Action<bool> displayedChanged = (value) => toggle.SetValueWithoutNotify(value);
                toggle.RegisterCallback<AttachToPanelEvent>((evt) => overlay.displayedChanged += displayedChanged);
                toggle.RegisterCallback<DetachFromPanelEvent>((evt) => overlay.displayedChanged -= displayedChanged);
                return toggle;
            }

            public void RebuildContent()
            {
                m_GlobalToolbar.Clear();
                m_Overlays.Clear();

                foreach (var overlay in m_Menu.canvas.overlays)
                {
                    if (!m_Menu.ShouldShowOverlay(overlay) || overlay is OverlayMenu)
                        continue;

                    var attrib = OverlayUtilities.GetAttribute(m_Menu.containerWindow.GetType(), overlay.GetType());
                    m_Overlays.Add((overlay, attrib.priority));
                }
                m_Overlays.Sort((a, b) => a.priority.CompareTo(b.priority));

                foreach(var tuple in m_Overlays)
                    m_GlobalToolbar.Add(CreateToggle(tuple.overlay));

                EditorToolbarUtility.SetupChildrenAsButtonStrip(m_GlobalToolbar);

                m_Separator.style.display = m_Menu.canvas.HasTransientOverlays() ? DisplayStyle.Flex : DisplayStyle.None;

                m_TransientToolbar.Clear();
                foreach (var overlay in m_Menu.canvas.transientOverlays)
                    m_TransientToolbar.Add(CreateToggle(overlay));

                EditorToolbarUtility.SetupChildrenAsButtonStrip(m_TransientToolbar);
            }
        }

        ScrollView m_ListRoot;
        Toggle m_Toggle;
        OverlayPresetDropdown m_Dropdown;
        Toolbar m_Toolbar;
        const string k_ShowOverlayMenuShortcut = "Overlays/Show Overlay Menu";

        [InitializeOnLoadMethod]
        static void AddOverlayToWindowMenu()
        {
            HostView.populateDefaultMenuItems += PopulateDefaultMenuItems;
        }

        static void PopulateDefaultMenuItems(GenericMenu menu, EditorWindow targetWindow)
        {
            if (targetWindow is ISupportsOverlays)
            {
                var binding = ShortcutManager.instance.GetShortcutBinding(k_ShowOverlayMenuShortcut);
                var itemContent = EditorGUIUtility.TrTextContent($"Overlay Menu _{binding}");


                if (targetWindow.overlayCanvas.overlaysSupportEnabled)
                {
                    menu.AddItem(itemContent, false,
                        () => { targetWindow.overlayCanvas.ShowPopup<OverlayMenu>(); });
                }
                else
                    menu.AddDisabledItem(itemContent);
            }
        }

        [Shortcut(k_ShowOverlayMenuShortcut, typeof(OverlayShortcutContext), KeyCode.BackQuote)]
        static void ShowOverlayMenu(ShortcutArguments args)
        {
            if (args.context is OverlayShortcutContext context)
                context.editorWindow.overlayCanvas.ShowPopupAtMouse<OverlayMenu>();
        }

        public override void OnCreated()
        {
            canvas.overlaysEnabledChanged += OnOverlayEnabledChanged;
            canvas.overlayListChanged += OnOverlayListChanged;
            canvas.presetChanged += OnPresetChanged;
            displayedChanged += OnDisplayedChanged;
        }

        public override void OnWillBeDestroyed()
        {
            if (m_ListRoot != null)
                m_ListRoot.Query<OverlayMenuItem>().ForEach((item) => item.overlay?.SetHighlightEnabled(false));

            canvas.overlaysEnabledChanged -= OnOverlayEnabledChanged;
            canvas.overlayListChanged -= OnOverlayListChanged;
            displayedChanged -= OnDisplayedChanged;
            canvas.presetChanged -= OnPresetChanged;
        }

        void OnPresetChanged()
        {
            m_Dropdown?.SetValueWithoutNotify(canvas.lastAppliedPresetName);
        }

        void OnDisplayedChanged(bool displayed)
        {
            if (!displayed)
                SetHighlightEnabled(false);
        }

        void OnOverlayEnabledChanged(bool visibility)
        {
            m_Toggle?.SetValueWithoutNotify(visibility);
            m_Dropdown?.SetEnabled(visibility);
        }

        void OnOverlayListChanged()
        {
            if (m_Toolbar != null)
                m_Toolbar.RebuildContent();
            else
                RebuildList();
        }

        bool ShouldShowOverlay(Overlay overlay)
        {
            return overlay.userControlledVisibility && overlay.hasMenuEntry && !canvas.IsTransient(overlay) && (overlay != this || isPopup);
        }

        public OverlayToolbar CreateHorizontalToolbarContent()
        {
            return m_Toolbar = new Toolbar(this, true);
        }

        public OverlayToolbar CreateVerticalToolbarContent()
        {
            return m_Toolbar = new Toolbar(this, false);
        }

        public override VisualElement CreatePanelContent()
        {
            VisualElement content = new VisualElement();
            content.style.minWidth = 160;
            m_Toolbar = null;

            if (isPopup)
            {
                content.Add(m_Toggle = new Toggle(L10n.Tr("Enable Overlays")) { name = "overlay-toggle" });
                m_Toggle.RegisterCallback<ChangeEvent<bool>>((evt) =>
                {
                    canvas.overlaysEnabled = evt.newValue;
                });
                m_Toggle.SetValueWithoutNotify(canvas.overlaysEnabled);
            }

            content.Add(m_Dropdown = new OverlayPresetDropdown(canvas.containerWindow));

            content.Add(m_ListRoot = new ScrollView() { name = "OverlayList" });
            m_ListRoot.horizontalScrollerVisibility = ScrollerVisibility.Auto;
            m_ListRoot.verticalScrollerVisibility = ScrollerVisibility.Auto;

            RebuildList();

            return content;
        }

        void RebuildList()
        {
            if (m_ListRoot == null)
                return;

            m_ListRoot.Clear();

            var overlays = new List<(Overlay overlay, int priority)>();
            foreach (var overlay in canvas.overlays)
            {
                if (!ShouldShowOverlay(overlay))
                    continue;

                var attrib = OverlayUtilities.GetAttribute(containerWindow.GetType(), overlay.GetType());
                overlays.Add((overlay, attrib.priority));
            }
            overlays.Sort((a, b) => a.priority.CompareTo(b.priority));

            foreach(var sortedOverlay in overlays)
                m_ListRoot.Add(new OverlayMenuItem() { overlay = sortedOverlay.overlay });

            if (canvas.HasTransientOverlays())
            {
                var separator = new VisualElement() { name = "Separator" };
                separator.AddToClassList("unity-separator");
                m_ListRoot.Add(separator);
            }

            foreach (var overlay in canvas.transientOverlays)
            {
                m_ListRoot.Add(new OverlayMenuItem() { overlay = overlay });
            }
        }
    }
}
