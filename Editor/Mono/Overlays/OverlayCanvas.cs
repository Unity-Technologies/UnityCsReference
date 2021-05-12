// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    [Serializable]
    class SaveData
    {
        public const int k_InvalidIndex = -1;
        public DockPosition dockPosition = DockPosition.Bottom;
        public string containerId = string.Empty;
        public bool floating;
        public bool collapsed;
        public bool displayed;
        public Vector2 snapOffset;
        public SnapCorner snapCorner;
        public string id;
        public int index = k_InvalidIndex;
        public Layout layout = Layout.Panel;

        public SaveData()
        {
        }

        public SaveData(SaveData other)
        {
            dockPosition = other.dockPosition;
            containerId = other.containerId;
            floating = other.floating;
            collapsed = other.collapsed;
            displayed = other.displayed;
            snapOffset = other.snapOffset;
            snapCorner = other.snapCorner;
            id = other.id;
            index = other.index;
            layout = other.layout;
        }
    }

    //Dock position within container
    //for a horizontal container, Top is left, Bottom is right
    enum DockPosition
    {
        Top,
        Bottom
    }

    enum SnapCorner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
    }

    [Serializable]
    class OverlayCanvas : ISerializationCallbackReceiver
    {
        public static readonly string ussClassName = "unity-overlay-canvas";
        const string k_UxmlPath = "UXML/Overlays/overlay-canvas.uxml";
        const string k_UxmlPathDropZone = "UXML/Overlays/overlay-toolbar-dropzone.uxml";
        internal const string k_StyleCommon = "StyleSheets/Overlays/OverlayCommon.uss";
        internal const string k_StyleLight = "StyleSheets/Overlays/OverlayLight.uss";
        internal const string k_StyleDark = "StyleSheets/Overlays/OverlayDark.uss";

        const string k_FloatingContainer = "overlay-container--floating";
        const string k_ToolbarZone = "overlay-toolbar-zone";
        const string k_ToolbarArea = "overlay-toolbar-area";
        const string k_DropTargetClassName = "overlay-droptarget";
        const string k_GhostClassName = "overlay-dummy";
        const string k_DefaultContainer = "overlay-container-default";
        static VisualTreeAsset s_TreeAsset;
        static VisualTreeAsset s_DropZoneTreeAsset;

        OverlayMenu m_Menu;

        List<Overlay> m_Overlays = new List<Overlay>();
        Dictionary<string, OverlayContainer> m_ContainerFromId = new Dictionary<string, OverlayContainer>();

        [SerializeField]
        List<SaveData> m_SaveData = new List<SaveData>();

        [SerializeField] bool m_FirstInit = true;

        VisualElement m_RootVisualElement;
        internal EditorWindow containerWindow { get; set; }
        internal VisualElement floatingContainer { get; set; }

        OverlayMenu menu => m_Menu == null
        ? m_Menu = new OverlayMenu(this)
            : m_Menu;

        internal VisualElement rootVisualElement => m_RootVisualElement == null
        ? m_RootVisualElement = CreateRoot()
            : m_RootVisualElement;

        Vector2 localMousePosition { get; set; }
        Overlay hoveredOverlay { get; set; }
        OverlayContainer hoveredOverlayContainer { get; set; }
        OverlayContainer defaultContainer { get; set; }
        OverlayContainer defaultToolbarContainer { get; set; }

        List<OverlayContainer> containers { get; set; }
        List<VisualElement> toolbarZones { get; set; }

        readonly Dictionary<VisualElement, Overlay> m_OverlaysByVE = new Dictionary<VisualElement, Overlay>();
        bool m_Initialized;

        VisualElement m_OriginGhost;
        public OverlayDestinationMarker destinationMarker { get; private set; }

        public IEnumerable<Overlay> overlays => m_Overlays.AsReadOnly();

        VisualElement m_WindowRoot;
        internal VisualElement windowRoot => m_WindowRoot;

        VisualElement CreateRoot()
        {
            var ve = new VisualElement();

            ve.AddToClassList(ussClassName);

            StyleSheet sheet;
            sheet = EditorGUIUtility.Load(k_StyleCommon) as StyleSheet;
            ve.styleSheets.Add(sheet);

            if (EditorGUIUtility.isProSkin)
                sheet = EditorGUIUtility.Load(k_StyleDark) as StyleSheet;
            else
                sheet = EditorGUIUtility.Load(k_StyleLight) as StyleSheet;

            ve.styleSheets.Add(sheet);

            if (s_TreeAsset == null)
                s_TreeAsset = EditorGUIUtility.Load(k_UxmlPath) as VisualTreeAsset;

            if (s_TreeAsset != null)
                s_TreeAsset.CloneTree(ve);

            if (s_DropZoneTreeAsset == null)
                s_DropZoneTreeAsset = EditorGUIUtility.Load(k_UxmlPathDropZone) as VisualTreeAsset;

            var toolbarZonesContainer = ve.Q("overlay-drop-zones__toolbars");

            if (s_DropZoneTreeAsset != null)
            {
                s_DropZoneTreeAsset.CloneTree(toolbarZonesContainer);

                toolbarZones = toolbarZonesContainer.Query(null, k_ToolbarZone).ToList();

                foreach (var visualElement in toolbarZones)
                {
                    visualElement.style.position = Position.Absolute;
                }
            }

            ve.name = ussClassName;
            ve.style.flexGrow = 1;

            containers = ve.Query<OverlayContainer>().ToList();
            foreach (var container in containers)
            {
                container.RegisterCallback<MouseEnterEvent>(OnMouseEnterOverlayContainer);
                if (container.ClassListContains(k_DefaultContainer))
                {
                    if (container.ClassListContains(k_ToolbarArea))
                        defaultToolbarContainer = container;
                    else
                        defaultContainer = container;
                }
            }

            floatingContainer = ve.Q(k_FloatingContainer);
            floatingContainer.style.position = Position.Absolute;
            floatingContainer.style.top = 0;
            floatingContainer.style.left = 0;
            floatingContainer.style.right = 0;
            floatingContainer.style.bottom = 0;

            m_OriginGhost = new VisualElement { name = "origin-ghost"};
            m_OriginGhost.AddToClassList(k_GhostClassName);
            m_OriginGhost.AddToClassList(k_DropTargetClassName);

            destinationMarker = new OverlayDestinationMarker { name = "dest-marker"};
            ve.Q("overlay-drop-zones").Add(destinationMarker);

            EnablePicking(ve, false);

            ve.RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            ve.RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);

            m_WindowRoot = ve.Q("overlay-window-root");
            return ve;
        }

        void EnablePicking(VisualElement element, bool enable, string ignoreClass = "")
        {
            var mode = enable ? PickingMode.Position : PickingMode.Ignore;
            EnablePicking(element, mode, ignoreClass);
        }

        void EnablePicking(VisualElement element, PickingMode mode)
        {
            element.pickingMode = mode;
            foreach (var child in element.Children())
            {
                EnablePicking(child, mode);
            }
        }

        void EnablePicking(VisualElement element, PickingMode mode, string ignoreClass)
        {
            if (string.IsNullOrEmpty(ignoreClass))
                EnablePicking(element, mode);
            else
            {
                element.pickingMode = mode;
                foreach (var child in element.Children())
                {
                    if (!child.ClassListContains(ignoreClass))
                        EnablePicking(child, mode, ignoreClass);
                }
            }
        }

        public bool overlaysEnabled => containers.All(x => x.style.display != DisplayStyle.None);

        public void SetOverlaysEnabled(bool visible)
        {
            foreach (var container in containers)
                container.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            foreach (var toolbar in toolbarZones)
                toolbar.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Show(Overlay overlay)
        {
            if (overlay.userControlledVisibility)
                overlay.displayed = true;
        }

        void OnMouseEnterOverlayContainer(MouseEnterEvent evt)
        {
            var overlayContainer = evt.target as OverlayContainer;
            hoveredOverlayContainer = overlayContainer;
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            //this is necessary to get a mouse position relative to camera rect
            //considering the canvas is non pickable and we need the mouse pos
            //to pop the overlay menu
            rootVisualElement.RegisterCallback<MouseOverEvent>(OnMouseEnter);
            rootVisualElement.RegisterCallback<MouseMoveEvent>(OnMouseMove);


            //this is used to clamp overlays to floating container bounds.
            floatingContainer.RegisterCallback<GeometryChangedEvent>(GeometryChanged);
        }

        void OnDetachedFromPanel(DetachFromPanelEvent evt)
        {
            rootVisualElement.UnregisterCallback<MouseOverEvent>(OnMouseEnter);
            rootVisualElement.UnregisterCallback<MouseMoveEvent>(OnMouseMove);

            floatingContainer.UnregisterCallback<GeometryChangedEvent>(GeometryChanged);
        }

        internal void OnContainerWindowDisabled()
        {
            foreach (var overlay in m_Overlays)
                overlay.OnWillBeDestroyed();
        }

        //clamps a visual element to the rootVisualElement's bounds, if the visual element is absolute.
        void ClampToOverlayWindow(VisualElement ve)
        {
            if (ve.resolvedStyle.position == Position.Absolute)
            {
                var rect = ClampToOverlayWindow(ve.layout);
                ve.style.left = rect.x;
                ve.style.top = rect.y;
            }
        }

        internal Rect ClampToOverlayWindow(Rect rect)
        {
            return ClampRectToBounds(rootVisualElement.localBound, rect);
        }

        internal static Rect ClampRectToBounds(Rect boundary, Rect rectToClamp)
        {
            if (rectToClamp.x + rectToClamp.width > boundary.xMax)
                rectToClamp.x = boundary.xMax - rectToClamp.width;

            if (rectToClamp.x < boundary.xMin)
                rectToClamp.x = boundary.xMin;

            if (rectToClamp.y + rectToClamp.height > boundary.yMax)
                rectToClamp.y = boundary.yMax - rectToClamp.height;

            if (rectToClamp.y < boundary.yMin)
                rectToClamp.y = boundary.yMin;

            return rectToClamp;
        }

        //clamp all overlays to  root visual element's new bounds
        void GeometryChanged(GeometryChangedEvent evt)
        {
            foreach (var overlay in m_Overlays)
            {
                //force an update of the floating position
                overlay.floatingPosition = overlay.floatingPosition;
                overlay.UpdateAbsolutePosition();
            }
        }

        void OnMouseMove(MouseMoveEvent evt)
        {
            localMousePosition = rootVisualElement.WorldToLocal(evt.mousePosition);
        }

        //returns true if toolbar container has any visible elements
        bool ToolbarHasVisibleContent(OverlayContainer container)
        {
            var result = false;
            if (container != null)
            {
                for (int i = 0; i < container.childCount; i++)
                {
                    var ve = container.ElementAt(i);
                    if (ve.ClassListContains(OverlayContainer.spacerClassName)) continue;

                    if (ve.style.display != DisplayStyle.None)
                    {
                        result = true;
                        break;
                    }
                }
            }
            return result;
        }

        void OnMouseEnter(MouseOverEvent evt)
        {
            localMousePosition = rootVisualElement.WorldToLocal(evt.mousePosition);
        }

        //Adds overlay for type, currently only one overlay instance per type
        void AddOverlay(Type overlayType)
        {
            var overlay = m_Overlays.Find(o => o.GetType().IsSubclassOf(overlayType));

            if (overlay == null)
            {
                overlay = OverlayUtilities.CreateOverlay(overlayType, this);
                if (m_Overlays.Find(o => o.id == overlay.id) != null)
                {
                    Debug.LogError($"An overlay with id ({overlay.id}) was already registered to window ({containerWindow.titleContent.text})");
                    return;
                }
                m_Overlays.Add(overlay);
            }

            OnOverlayAdded(overlay);
        }

        void OnOverlayAdded(Overlay overlay)
        {
            //register mouse events for hovering
            overlay.rootVisualElement.RegisterCallback<MouseEnterEvent>(OnMouseEnterOverlay);
            overlay.rootVisualElement.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveOverlay);

            overlay.displayed = overlay.userControlledVisibility;
        }

        void OnOverlayRemoved(Overlay overlay)
        {
            overlay.rootVisualElement.UnregisterCallback<MouseEnterEvent>(OnMouseEnterOverlay);
            overlay.rootVisualElement.UnregisterCallback<MouseLeaveEvent>(OnMouseLeaveOverlay);
        }

        int GetOverlayContainerIndex(Overlay overlay)
        {
            if (overlay.container == null)
                return -1;

            int index = overlay.container.topOverlays.IndexOf(overlay);
            if (index >= 0)
                return index;

            index = overlay.container.bottomOverlays.IndexOf(overlay);
            if (index >= 0)
                return index;

            return -1;
        }

        //reset hovered overlay
        void OnMouseLeaveOverlay(MouseLeaveEvent evt)
        {
            hoveredOverlay = null;
        }

        //set hovered overlay
        void OnMouseEnterOverlay(MouseEnterEvent evt)
        {
            var overlay = evt.target as VisualElement;
            if (overlay != null && overlay.ClassListContains(Overlay.ussClassName))
                hoveredOverlay = m_OverlaysByVE[overlay];
        }

        //hides current hovered overlay
        public void HideHoveredOverlay()
        {
            if (hoveredOverlay != null)
            {
                if (hoveredOverlay.userControlledVisibility)
                    hoveredOverlay.displayed = false;
            }
        }

        //docks current hovered overlay
        public void DockHoveredOverlay()
        {
            if (hoveredOverlay != null) hoveredOverlay.floating = false;
        }

        //shows overlay menu at mouse position if it is within the window
        public void ShowOverlayMenu()
        {
            menu.Show(overlays, floatingContainer.localBound, localMousePosition);
        }

        public bool IsOverlayMenuVisible()
        {
            return menu.style.display != DisplayStyle.None;
        }

        //hides overlay menu
        public void HideOverlayMenu()
        {
            menu.Hide();
        }

        //returns overlay matching the id
        public Overlay GetOverlay(string overlayId)
        {
            foreach (var overlay in overlays)
            {
                if (overlay.id == overlayId)
                    return overlay;
            }

            return null;
        }

        public bool initialized => m_Initialized;

        internal void Initialize(EditorWindow containerWindow)
        {
            this.containerWindow = containerWindow;

            rootVisualElement.Add(menu);

            m_Initialized = true;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            //get all overlays for window type.
            var overlayTypes = OverlayUtilities.GetOverlaysForType(containerWindow.GetType());

            //init all overlays
            foreach (var overlayType in overlayTypes)
                AddOverlay(overlayType);

            for (var index = 0; index < m_Overlays.Count; index++)
            {
                var overlay = m_Overlays[index];
                m_OverlaysByVE[overlay.rootVisualElement] = overlay;
            }

            if (m_FirstInit)
            {
                var preset = OverlayPresetManager.GetDefaultPreset(containerWindow.GetType());
                if (preset != null)
                    preset.ApplyTo(containerWindow);
                else
                    InitContainersFromSaveData();
                m_FirstInit = false;
            }
            else
            {
                InitContainersFromSaveData();
            }
        }

        void OnBeforeAssemblyReload()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            containerWindow = null;
            m_Initialized = false;
        }

        internal Rect GetOriginGhostWorldBound()
        {
            return m_OriginGhost.parent == null ? new Rect(-1000, -1000, 0, 0) : m_OriginGhost.worldBound;
        }

        //hides ghost
        internal void HideOriginGhost()
        {
            m_OriginGhost.RemoveFromHierarchy();
        }

        //adds a ghost to container the same size as the overlay
        internal void ShowOriginGhost(Overlay overlay)
        {
            m_OriginGhost.style.width = overlay.rootVisualElement.layout.width;
            m_OriginGhost.style.height = overlay.rootVisualElement.layout.height;
            overlay.container.Insert(overlay.container.IndexOf(overlay.rootVisualElement), m_OriginGhost);
        }

        public void OnBeforeSerialize()
        {
            m_SaveData.Clear();

            void SaveContainer(List<Overlay> overlays)
            {
                for (var index = 0; index < overlays.Count; index++)
                {
                    var overlay = overlays[index];
                    var data = CreateSaveData(index, overlay);
                    m_SaveData.Add(data);
                }
            }

            if (containers == null) return;

            foreach (var container in containers)
            {
                if (container != null)
                {
                    SaveContainer(container.topOverlays);
                    SaveContainer(container.bottomOverlays);
                }
            }
        }

        SaveData CreateSaveData(int index, Overlay overlay)
        {
            var saveData = new SaveData
            {
                containerId = overlay.container.name,
                index = index,
                dockPosition = overlay.dockPosition,
                floating = overlay.floating,
                collapsed = overlay.collapsed,
                displayed = overlay.displayed,
                layout = overlay.layout,
                id = overlay.id,
                snapCorner = overlay.floatingSnapCorner,
                snapOffset = overlay.floatingSnapOffset
            };
            return saveData;
        }

        public void OnAfterDeserialize()
        {
            EditorApplication.delayCall += InitContainersFromSaveData;
        }

        internal void CopySaveData(out SaveData[] saveData)
        {
            OnBeforeSerialize(); //Force a save of the current data
            saveData = m_SaveData.ToArray();
            // Copy save data
            for (int i = 0; i < saveData.Length; ++i)
            {
                saveData[i] = new SaveData(saveData[i]);
            }
        }

        internal void ApplySaveData(SaveData[] saveData)
        {
            m_SaveData = new List<SaveData>(saveData);
            InitContainersFromSaveData();
        }

        void InitContainersFromSaveData()
        {
            void ApplySaveDataToOverlay(SaveData data, Overlay overlay)
            {
                if (data.containerId != null && m_ContainerFromId.TryGetValue(data.containerId, out var container))
                {
                    // Overlays are sorted by their index in containers so we can directly add them to top or bottom without thinking of order
                    switch (data.dockPosition)
                    {
                        case DockPosition.Top:
                            container.AddToTop(overlay);
                            break;

                        case DockPosition.Bottom:
                            container.AddToBottom(overlay);
                            break;
                    }
                }
                else
                {
                    container = overlay is ToolbarOverlay ? defaultToolbarContainer : defaultContainer;
                    container.AddToBottom(overlay);
                }

                overlay.LoadFromSerializedData(data.displayed, data.collapsed, data.floating, data.snapOffset, data.layout, data.snapCorner, container);

                if (overlay.floating)
                    floatingContainer.Add(overlay.rootVisualElement);
            }

            void ApplyDefaultDataToOverlay(Overlay overlay)
            {
                SaveData saveData = new SaveData
                {
                    floating = false,
                    collapsed = false,
                    containerId = null,
                    displayed = OverlayUtilities.GetIsDefaultDisplayFromAttribute(overlay.GetType()),
                    dockPosition = DockPosition.Bottom,
                    index = int.MaxValue, //ensure that it's added at the bottom
                    layout = Layout.Panel,
                };

                ApplySaveDataToOverlay(saveData, overlay);
            }

            if (containers == null)
                return;

            var saveDataById = new Dictionary<string, SaveData>();
            foreach (var saveData in m_SaveData)
            {
                saveDataById[saveData.id] = saveData;
            }

            m_ContainerFromId.Clear();
            foreach (var container in containers)
            {
                if (!m_ContainerFromId.ContainsKey(container.name))
                    m_ContainerFromId.Add(container.name, container);
            }

            //Sort by index to prepare to add in correct order
            m_SaveData.Sort((x, y) => x.index.CompareTo(y.index));
            foreach (var overlay in overlays)
            {
                overlay.container?.RemoveOverlay(overlay);
            }

            foreach (var overlay in overlays)
            {
                if (saveDataById.TryGetValue(overlay.id, out var saveData))
                {
                    ApplySaveDataToOverlay(saveData, overlay);
                }
                else
                {
                    ApplyDefaultDataToOverlay(overlay);
                }
            }
        }

        public bool IsInToolbar(Overlay overlay)
        {
            return !overlay.floating && IsInToolbar(overlay.container);
        }

        public bool IsInToolbar(OverlayContainer container)
        {
            return container is ToolbarOverlayContainer;
        }
    }
}
