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
            createMenuCallback = () => targetWindow.rootVisualElement.panel.CreateMenu();
            RefreshPresetDisplayValue();
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            m_TargetWindow.overlayCanvas.presetDirtyChanged += RefreshPresetDisplayValue;
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            m_TargetWindow.overlayCanvas.afterOverlaysInitialized += RefreshPresetDisplayValue;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (m_TargetWindow is not null && m_TargetWindow.overlayCanvas is not null)
                m_TargetWindow.overlayCanvas.afterOverlaysInitialized -= RefreshPresetDisplayValue;
        }

        void RefreshPresetDisplayValue()
        {
            SetValueWithoutNotify(GetValueToDisplay());
            if (textElement != null)
                textElement.style.unityFontStyleAndWeight = m_TargetWindow.overlayCanvas.presetDirty ? FontStyle.Bold : FontStyle.Normal;
        }

        internal override void AddMenuItems(AbstractGenericMenu menu)
        {
            OverlayPresetManager.GenerateMenu(menu, "", m_TargetWindow, true, null, new DefaultOverlayPreset());
        }

        internal override string GetValueToDisplay()
        {
            if (m_TargetWindow == null)
                return string.Empty;

            var valueToDisplay = m_TargetWindow.overlayCanvas.lastAppliedPresetName;
            if (m_TargetWindow.overlayCanvas.presetDirty)
                valueToDisplay += "*";

            return valueToDisplay;
        }

        //We don't actually use this but we are required to implement it
        internal override string GetListItemToDisplay(string item) => item;
    }

    [Overlay(typeof(EditorWindow), k_Id, k_DisplayName , k_UssName, defaultDockZone = DockZone.LeftColumn, defaultLayout = Layout.HorizontalToolbar, defaultDisplay = true, defaultDockIndex = 0, group = OverlayAttribute.unityGroup)]
    [Icon("Icons/Overlays/OverlayMenu.png")]
    sealed class OverlayMenu : Overlay, ICreateHorizontalToolbar, ICreateVerticalToolbar
    {
        const string k_DisplayName = "Overlay Menu";
        internal const string k_Id = "Overlays/OverlayMenu"; // Used by tests
        const string k_UssName = "overlay-menu";

        const string k_UnityGroupName = "Unity";

        public class OverlayMenuData : ScriptableSingleton<OverlayMenuData>
        {
            [SerializeField]
            List<string> m_FoldoutStatesData = new() { k_UnityGroupName };

            public bool IsFoldoutExpanded(string groupName)
            {
                return m_FoldoutStatesData.Contains(groupName);
            }

            public void SetFoldoutState(string groupName, bool isExpanded)
            {
                if (isExpanded)
                {
                    if (!m_FoldoutStatesData.Contains(groupName))
                        m_FoldoutStatesData.Add(groupName);
                }
                else
                {
                    m_FoldoutStatesData.Remove(groupName);
                }
            }
        }

        static class OverlayMenuState
        {
            public static bool IsFoldoutExpanded(string groupName)
            {
                return OverlayMenuData.instance.IsFoldoutExpanded(groupName);
            }

            public static void SetFoldoutState(string groupName, bool isExpanded)
            {
                OverlayMenuData.instance.SetFoldoutState(groupName, isExpanded);
            }
        }

        internal sealed class OverlayGroupData : IComparable<OverlayGroupData>
        {
            public readonly string name;
            public readonly List<Overlay> overlays = new List<Overlay>();

            public OverlayGroupData(string name)
            {
                this.name = name;
            }

            public int CompareTo(OverlayGroupData other)
            {
                // Sort the unity group at the top
                if (name == k_UnityGroupName)
                    return -1;

                if (other.name == k_UnityGroupName)
                    return 1;

                return name.CompareTo(other.name);
            }
        }

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
                toggle.RegisterCallback<AttachToPanelEvent>((evt) =>
                {
                    displayedChanged(overlay.displayed);
                    overlay.displayedChanged += displayedChanged;
                });
                toggle.RegisterCallback<DetachFromPanelEvent>((evt) => overlay.displayedChanged -= displayedChanged);
                return toggle;
            }

            public void RebuildContent()
            {
                m_GlobalToolbar.Clear();
                m_Overlays.Clear();

                if (!m_Menu.HasOverlaysToShowInMenu())
                {
                    m_GlobalToolbar.Add(m_Menu.GetReplacementText(m_GlobalToolbar.resolvedStyle.flexDirection == FlexDirection.Column));
                    return;
                }

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
        Toggle m_EnableOverlaysToggle;
        Toggle m_DynamicPanelBehaviorToggle;
        OverlayPresetDropdown m_Dropdown;
        Toolbar m_Toolbar;
        static readonly string k_CustomGroup = L10n.Tr("Custom");
        public const string k_ShowOverlayMenuShortcutPath = "Overlays/Show Overlay Menu";

        [InitializeOnLoadMethod]
        static void AddOverlayToWindowMenu()
        {
            HostView.populateDefaultMenuItems += PopulateDefaultMenuItems;
        }

        internal bool HasOverlaysToShowInMenu()
        {
            if (canvas.HasTransientOverlays())
                return true;

            // If at least one overlay that is not the Overlay Menu itself
            foreach (var overlay in canvas.overlays)
            {
                if (overlay.userControlledVisibility && overlay.hasMenuEntry && !(overlay is OverlayMenu))
                    return true;
            }
            return false;
        }

        static void PopulateDefaultMenuItems(GenericMenu menu, EditorWindow targetWindow)
        {
            if (targetWindow is ISupportsOverlays)
            {
                var binding = ShortcutManager.instance.GetShortcutBinding(OverlayMenu.k_ShowOverlayMenuShortcutPath);
                var overlayMenuItemContent = EditorGUIUtility.TrTextContent($"Overlays/Overlay Menu _{binding}");
                var enableOverlaysContent = EditorGUIUtility.TrTextContent($"Overlays/Enable Overlays");
                var displaceWindowContent = EditorGUIUtility.TrTextContent($"Overlays/Displace Window");
                var overlaySettingsContent = EditorGUIUtility.TrTextContent($"Overlays/Overlay Settings...");

                var displaceWindow = targetWindow.overlayCanvas.dynamicPanelBehavior == DynamicPanelBehavior.DisplaceWindow;
                var overlaysEnabled = targetWindow.overlayCanvas.overlaysEnabled;

                if (targetWindow.overlayCanvas.overlaysSupportEnabled)
                {
                    menu.AddItem(overlayMenuItemContent, false,
                        () => { targetWindow.overlayCanvas.ShowPopup<OverlayMenu>(); });

                    menu.AddSeparator("Overlays/");
                    menu.AddItem(enableOverlaysContent, overlaysEnabled,
                        () => targetWindow.overlayCanvas.overlaysEnabled = !overlaysEnabled);

                    if (OverlayPrefs.IsDynamicPanelBehaviorChangesAllowed(targetWindow.GetType()))
                    {
                        menu.AddItem(displaceWindowContent, displaceWindow,
                            () => targetWindow.overlayCanvas.dynamicPanelBehavior = displaceWindow
                                ? DynamicPanelBehavior.None
                                : DynamicPanelBehavior.DisplaceWindow);
                    }

                    menu.AddItem(overlaySettingsContent, false,
                        () => SettingsService.OpenUserPreferences("Preferences/Overlays") );
                }
                else
                {
                    menu.AddDisabledItem(overlayMenuItemContent, false);
                    menu.AddSeparator("Overlays/");
                    menu.AddDisabledItem(enableOverlaysContent, overlaysEnabled);
                    menu.AddDisabledItem(displaceWindowContent, displaceWindow);
                    menu.AddDisabledItem(overlaySettingsContent, false);
                }
            }
        }

        [Shortcut(k_ShowOverlayMenuShortcutPath, typeof(OverlayShortcutContext), KeyCode.BackQuote)]
        static void ShowOverlayMenu(ShortcutArguments args)
        {
            if (args.context is OverlayShortcutContext context)
            {
                context.editorWindow.overlayCanvas.ShowPopupAtMouse<OverlayMenu>();
            }
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
                canvas.ClearHighlights();

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
            m_EnableOverlaysToggle?.SetValueWithoutNotify(visibility);
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
            return overlay.userControlledVisibility && overlay.hasMenuEntry && !overlay.canvas.IsTransient(overlay) && (overlay != this || isPopup);
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
            content.AddToClassList("overlay-menu");
            m_Toolbar = null;

            if (isPopup)
            {
                content.Add(m_EnableOverlaysToggle = new Toggle(L10n.Tr("Enable Overlays")) { name = "overlay-toggle" });
                m_EnableOverlaysToggle.RegisterCallback<ChangeEvent<bool>>((evt) =>
                {
                    canvas.overlaysEnabled = evt.newValue;
                });
                m_EnableOverlaysToggle.SetValueWithoutNotify(canvas.overlaysEnabled);

                if (OverlayPrefs.IsDynamicPanelBehaviorChangesAllowed(containerWindow.GetType()))
                {
                    content.Add(m_DynamicPanelBehaviorToggle = new Toggle(L10n.Tr("Displace Window")) { name = "overlay-toggle" });
                    m_DynamicPanelBehaviorToggle.tooltip = "This toggle determines whether panels docked as full-height dynamic panels " +
                        "will be drawn on top of the window or displace the window content.";
                    m_DynamicPanelBehaviorToggle.RegisterCallback<ChangeEvent<bool>>((evt) =>
                    {
                        canvas.dynamicPanelBehavior = evt.newValue ? DynamicPanelBehavior.DisplaceWindow : DynamicPanelBehavior.None;
                    });

                    var displaceWindow = canvas.dynamicPanelBehavior == DynamicPanelBehavior.DisplaceWindow;
                    m_DynamicPanelBehaviorToggle.SetValueWithoutNotify(displaceWindow);
                }
            }

            content.Add(m_Dropdown = new OverlayPresetDropdown(canvas.containerWindow));

            content.Add(m_ListRoot = new ScrollView() { name = "OverlayList" });
            m_ListRoot.mode = ScrollViewMode.Vertical;
            m_ListRoot.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            m_ListRoot.verticalScrollerVisibility = ScrollerVisibility.Auto;

            RebuildList();

            return content;
        }

        internal static List<OverlayGroupData> GetGroups(OverlayMenu menu, IEnumerable<Overlay> overlays)
        {
            var groupSet = new Dictionary<string, OverlayGroupData>();
            foreach (var overlay in overlays)
            {
                if (!menu.ShouldShowOverlay(overlay))
                    continue;

                var group = GetGroupName(overlay);
                if (!groupSet.TryGetValue(group, out OverlayGroupData data))
                    groupSet[group] = data = new OverlayGroupData(group);

                data.overlays.Add(overlay);
            }

            var groups = new List<OverlayGroupData>(groupSet.Values);

            // Sort groups and overlays
            groups.Sort();
            foreach (var group in groups)
                group.overlays.Sort((a, b) => a.menuPriority.CompareTo(b.menuPriority));

            return groups;
        }

        internal Label GetReplacementText(bool isVerticalToolbar = false)
        {
            var label = new Label(isVerticalToolbar ? "None" : "No Overlays");
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.tooltip = $"No overlays in the current {canvas.containerWindow.name}";

            return label;
        }

        void RebuildList()
        {
            if (m_ListRoot == null)
                return;

            m_ListRoot.Clear();

            if (!HasOverlaysToShowInMenu())
            {
                m_ListRoot.Add(GetReplacementText());
                return;
            }

            var groups = GetGroups(this, canvas.overlays);
            foreach (var group in groups)
            {
                var groupItem = new OverlayGroupMenuItem(group.name, group.overlays);

                groupItem.value = OverlayMenuState.IsFoldoutExpanded(group.name);
                groupItem.toggle.RegisterValueChangedCallback(evt =>
                {
                    OverlayMenuState.SetFoldoutState(group.name, evt.newValue);
                });

                foreach (var overlay in group.overlays)
                    groupItem.Add(new OverlayMenuItem(overlay));

                m_ListRoot.Add(groupItem);
            }

            // Create transient overlay items
            if (canvas.HasTransientOverlays())
            {
                var separator = new VisualElement() { name = "Separator" };
                separator.AddToClassList("unity-separator");

                m_ListRoot.Add(separator);

                foreach (var overlay in canvas.transientOverlays)
                {
                    m_ListRoot.Add(new OverlayMenuItem(overlay));
                }
            }
        }

        static string GetGroupName(Overlay overlay)
        {
            if (string.IsNullOrEmpty(overlay.menuGroup))
                return k_CustomGroup;

            // Ensure that user can't use our reserved name. Nothing stops them from using our reserved string but it's harder to figure out then just "Unity"
            if (overlay.menuGroup.ToLower().Trim() == "unity")
            {
                Debug.LogWarning(GetReservedNameWarning(overlay));
                return k_CustomGroup;
            }

            if (overlay.menuGroup == OverlayAttribute.unityGroup)
                return k_UnityGroupName;

            return overlay.menuGroup;
        }

        internal static string GetReservedNameWarning(Overlay overlay)
        {
            return $"Ignoring Overlay attribute of {overlay.displayName} using reserved group \"{overlay.menuGroup}\".\n" +
                    $"Overlay attribute on {overlay.GetType().FullName} is using \"{overlay.menuGroup}\" which is reserved for Unity used.";
        }
    }
}
