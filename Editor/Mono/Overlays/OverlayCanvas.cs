// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    [Serializable]
    class SaveData : IEquatable<SaveData>
    {
        public const int k_InvalidIndex = -1;
        public DockPosition dockPosition = DockPosition.Bottom;
        public string containerId = string.Empty;
        public bool floating;
        public bool collapsed;
        public bool displayed;
        public Vector2 snapOffset;
        public Vector2 snapOffsetDelta;
        public SnapCorner snapCorner;
        public string id;
        public int index = k_InvalidIndex;
        public Layout layout = Layout.Panel;

        public SaveData() { }

        public SaveData(SaveData other)
        {
            dockPosition = other.dockPosition;
            containerId = other.containerId;
            floating = other.floating;
            collapsed = other.collapsed;
            displayed = other.displayed;
            snapOffset = other.snapOffset;
            snapOffsetDelta = other.snapOffsetDelta;
            snapCorner = other.snapCorner;
            id = other.id;
            index = other.index;
            layout = other.layout;
        }

        public SaveData(Overlay overlay, int indexInContainer = k_InvalidIndex)
        {
            var container = overlay.container != null ? overlay.container.name : "";
            var dock = overlay.container != null && overlay.container.topOverlays.Contains(overlay)
                ? DockPosition.Top
                : DockPosition.Bottom;

            containerId = container;
            index = indexInContainer;
            dockPosition = dock;
            floating = overlay.floating;
            collapsed = overlay.collapsed;
            displayed = overlay.displayed;
            layout = overlay.layout;
            id = overlay.id;
            snapCorner = overlay.floatingSnapCorner;
            snapOffset = overlay.floatingSnapOffset - overlay.m_SnapOffsetDelta;
            snapOffsetDelta = overlay.m_SnapOffsetDelta;
        }

        public bool Equals(SaveData other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return dockPosition == other.dockPosition
                && containerId == other.containerId
                && floating == other.floating
                && collapsed == other.collapsed
                && displayed == other.displayed
                && snapOffset.Equals(other.snapOffset)
                && snapOffsetDelta.Equals(other.snapOffsetDelta)
                && snapCorner == other.snapCorner
                && id == other.id
                && index == other.index
                && layout == other.layout;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SaveData)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)dockPosition;
                hashCode = (hashCode * 397) ^ (containerId != null ? containerId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ floating.GetHashCode();
                hashCode = (hashCode * 397) ^ collapsed.GetHashCode();
                hashCode = (hashCode * 397) ^ displayed.GetHashCode();
                hashCode = (hashCode * 397) ^ snapOffset.GetHashCode();
                hashCode = (hashCode * 397) ^ snapOffsetDelta.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)snapCorner;
                hashCode = (hashCode * 397) ^ (id != null ? id.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ index;
                hashCode = (hashCode * 397) ^ (int)layout;
                return hashCode;
            }
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
    public sealed class OverlayCanvas : ISerializationCallbackReceiver
    {
        internal static readonly string ussClassName = "unity-overlay-canvas";
        const string k_UxmlPath = "UXML/Overlays/overlay-canvas.uxml";
        const string k_UxmlPathDropZone = "UXML/Overlays/overlay-toolbar-dropzone.uxml";
        internal const string k_StyleCommon = "StyleSheets/Overlays/OverlayCommon.uss";
        internal const string k_StyleLight = "StyleSheets/Overlays/OverlayLight.uss";
        internal const string k_StyleDark = "StyleSheets/Overlays/OverlayDark.uss";

        const string k_FloatingContainer = "overlay-container--floating";
        const string k_ToolbarZone = "overlay-toolbar-zone";
        const string k_ToolbarArea = "overlay-toolbar-area";
        const string k_DropTargetClassName = "overlay-droptarget";
        const string k_GhostClassName = "overlay-ghost";
        const string k_GhostAreaHovered = "unity-overlay-in-ghost-area";
        const string k_DefaultContainer = "overlay-container-default";
        static VisualTreeAsset s_TreeAsset;
        static VisualTreeAsset s_DropZoneTreeAsset;

        static readonly SaveData k_DefaultSaveData = new SaveData()
        {
            floating = false,
            collapsed = false,
            containerId = null,
            displayed = false,
            dockPosition = DockPosition.Bottom,
            index = int.MaxValue,
            layout = Layout.Panel
        };

        OverlayMenu m_Menu;
        internal string lastAppliedPresetName => m_LastAppliedPresetName;
        List<Overlay> m_Overlays = new List<Overlay>();

        [SerializeField]
        string m_LastAppliedPresetName = "Default";

        [SerializeField]
        List<SaveData> m_SaveData = new List<SaveData>();

        VisualElement m_RootVisualElement;
        internal EditorWindow containerWindow { get; set; }
        internal VisualElement floatingContainer { get; set; }
        Overlay m_HoveredOverlay;

        OverlayMenu menu => m_Menu ??= new OverlayMenu(this);
        internal VisualElement rootVisualElement => m_RootVisualElement ??= CreateRoot();

        Vector2 localMousePosition { get; set; }
        internal Overlay hoveredOverlay => m_HoveredOverlay;
        OverlayContainer hoveredOverlayContainer { get; set; }
        OverlayContainer defaultContainer { get; set; }
        OverlayContainer defaultToolbarContainer { get; set; }

        List<OverlayContainer> containers { get; set; }
        List<VisualElement> toolbarZones { get; set; }

        readonly Dictionary<VisualElement, Overlay> m_OverlaysByVE = new Dictionary<VisualElement, Overlay>();

        VisualElement m_OriginGhost;
        internal OverlayDestinationMarker destinationMarker { get; private set; }

        internal IEnumerable<Overlay> overlays => m_Overlays.AsReadOnly();

        VisualElement m_WindowRoot;
        internal VisualElement windowRoot => m_WindowRoot;

        internal Action afterOverlaysInitialized;
        internal event Action<bool> overlaysEnabledChanged;

        internal bool overlaysEnabled => containers.All(x => x.style.display != DisplayStyle.None);

        internal OverlayCanvas() { }

        internal void SetOverlaysEnabled(bool visible)
        {
            if (visible == overlaysEnabled)
                return;

            foreach (var container in containers)
                container.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            foreach (var toolbar in toolbarZones)
                toolbar.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            floatingContainer.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

            overlaysEnabledChanged?.Invoke(visible);
        }

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

            SetPickingMode(ve, PickingMode.Ignore);

            ve.RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            ve.RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);

            m_WindowRoot = ve.Q("overlay-window-root");
            return ve;
        }

        void SetPickingMode(VisualElement element, PickingMode mode)
        {
            element.pickingMode = mode;
            foreach (var child in element.Children())
                SetPickingMode(child, mode);
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

        // clamp all overlays to  root visual element's new bounds
        void GeometryChanged(GeometryChangedEvent evt)
        {
            if (!overlaysEnabled)
                return;

            foreach (var overlay in m_Overlays)
            {
                if (overlay == null)
                    continue;

                using (new Overlay.LockedAnchor(overlay))
                    overlay.floatingPosition = overlay.floatingPosition; //force an update of the floating position

                overlay.UpdateAbsolutePosition();

                //Register the geometrychanged callback to the overlay if it was not registered before,
                //this is not doing anything if it has already been registered
                overlay.rootVisualElement.RegisterCallback<GeometryChangedEvent>(overlay.OnGeometryChanged);
            }
        }

        void OnMouseMove(MouseMoveEvent evt)
        {
            localMousePosition = rootVisualElement.WorldToLocal(evt.mousePosition);
        }

        void OnMouseEnter(MouseOverEvent evt)
        {
            localMousePosition = rootVisualElement.WorldToLocal(evt.mousePosition);
        }

        void OnMouseLeaveOverlay(MouseLeaveEvent evt)
        {
            m_HoveredOverlay = null;
        }

        void OnMouseEnterOverlay(MouseEnterEvent evt)
        {
            var overlay = evt.target as VisualElement;
            if (overlay != null && overlay.ClassListContains(Overlay.ussClassName))
                m_HoveredOverlay = m_OverlaysByVE[overlay];
        }

        internal void HideHoveredOverlay()
        {
            if (hoveredOverlay != null && hoveredOverlay.userControlledVisibility)
                hoveredOverlay.displayed = false;
        }

        internal bool menuVisible
        {
            get => menu.style.display != DisplayStyle.None;
            set
            {
                if(value && !menuVisible)
                    menu.Show(overlays, floatingContainer.localBound, localMousePosition);
                else if(!value)
                    menu.Hide();
            }
        }

        internal void Initialize(EditorWindow window)
        {
            Profiler.BeginSample("OverlayCanvas.Initialize");
            containerWindow = window;
            rootVisualElement.Add(menu);

            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            var overlayTypes = OverlayUtilities.GetOverlaysForType(window.GetType());

            // init all overlays
            foreach (var overlayType in overlayTypes)
                AddOverlay(OverlayUtilities.CreateOverlay(overlayType));

            if (m_SaveData == null || m_SaveData.Count < 1)
            {
                var preset = OverlayPresetManager.GetDefaultPreset(window.GetType());
                if(preset != null && preset.saveData != null)
                    m_SaveData = new List<SaveData>(preset.saveData);
            }

            RestoreOverlays();
            Profiler.EndSample();
        }

        void OnBeforeAssemblyReload()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            foreach (var overlay in m_Overlays)
                overlay.rootVisualElement.UnregisterCallback<GeometryChangedEvent>(overlay.OnGeometryChanged);
        }

        internal Rect GetOriginGhostWorldBound()
        {
            return m_OriginGhost.parent == null ? new Rect(-1000, -1000, 0, 0) : m_OriginGhost.worldBound;
        }

        internal void HideOriginGhost()
        {
            m_OriginGhost.RemoveFromHierarchy();
        }

        internal void ShowOriginGhost(Overlay overlay)
        {
            m_OriginGhost.style.width = overlay.rootVisualElement.layout.width;
            m_OriginGhost.style.height = overlay.rootVisualElement.layout.height;
            overlay.container?.Insert(overlay.container.IndexOf(overlay.rootVisualElement), m_OriginGhost);
        }

        internal void UpdateGhostHover(bool hovered)
        {
            m_OriginGhost.EnableInClassList(k_GhostAreaHovered, hovered);
        }

        void WriteOrReplaceSaveData(Overlay overlay, int containerIndex = -1)
        {
            if (containerIndex < 0)
                containerIndex = overlay.container?.FindIndex(overlay) ?? SaveData.k_InvalidIndex;
            var saveData = new SaveData(overlay, containerIndex);
            int existing = m_SaveData.FindIndex(x => x.id == overlay.id);

            if (existing < 0)
                m_SaveData.Add(saveData);
            else
                m_SaveData[existing] = saveData;
        }

        public void OnBeforeSerialize()
        {
            if (containers == null)
                return;

            foreach (var container in containers)
            {
                if (container != null)
                {
                    var top = container.topOverlays;
                    var bot = container.bottomOverlays;

                    for (int i = 0, c = top.Count; i < c; ++i)
                    {
                        if (!top[i].dontSaveInLayout)
                            WriteOrReplaceSaveData(top[i], i);
                    }

                    for (int i = 0, c = bot.Count; i < c; ++i)
                    {
                        if (!bot[i].dontSaveInLayout)
                            WriteOrReplaceSaveData(bot[i], i);
                    }
                }
            }
        }

        public void OnAfterDeserialize() {}

        internal void CopySaveData(out SaveData[] saveData)
        {
            // Force a save of the current data
            OnBeforeSerialize();
            saveData = m_SaveData.ToArray();
            for (int i = 0; i < saveData.Length; ++i)
                saveData[i] = new SaveData(saveData[i]);
        }

        internal void ApplyPreset(OverlayPreset preset)
        {
            if (!preset.CanApplyToWindow(containerWindow.GetType()))
            {
                Debug.LogError($"Cannot apply preset for type {preset.targetWindowType} to canvas of type " +
                    $"{containerWindow.GetType()}");
                return;
            }

            m_LastAppliedPresetName = preset.name;
            ApplySaveData(preset.saveData);
        }

        internal void ApplySaveData(SaveData[] saveData)
        {
            m_SaveData = new List<SaveData>(saveData);
            RestoreOverlays();
        }

        public void Add(Overlay overlay, bool show = true)
        {
            if(m_Overlays.Contains(overlay))
                return;
            overlay.canvas?.Remove(overlay);
            AddOverlay(overlay);
            RestoreOverlay(overlay);
            overlay.displayed = show;
        }

        public bool Remove(Overlay overlay)
        {
            if (!m_Overlays.Remove(overlay))
                return false;
            WriteOrReplaceSaveData(overlay);
            overlay.container?.RemoveOverlay(overlay);
            overlay.canvas = null;
            var root = overlay.rootVisualElement;
            m_OverlaysByVE.Remove(root);
            root.UnregisterCallback<MouseEnterEvent>(OnMouseEnterOverlay);
            root.UnregisterCallback<MouseLeaveEvent>(OnMouseLeaveOverlay);
            root.RemoveFromHierarchy();
            return true;
        }

        // AddOverlay just registers the Overlay with Canvas. It does not init save data or add to a valid container.
        void AddOverlay(Overlay overlay)
        {
            if(!OverlayUtilities.EnsureValidId(m_Overlays, overlay))
            {
                Debug.LogError($"An overlay with id \"{overlay.id}\" was already registered to window " +
                    $"({containerWindow.titleContent.text}).");
                return;
            }

            overlay.canvas = this;
            m_Overlays.Add(overlay);
            m_OverlaysByVE[overlay.rootVisualElement] = overlay;
            overlay.rootVisualElement.RegisterCallback<MouseEnterEvent>(OnMouseEnterOverlay);
            overlay.rootVisualElement.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveOverlay);

            // OnCreated must be invoked before contents are requested for the first time
            overlay.OnCreated();
        }

        // GetOrCreateOverlay is used to instantiate Overlays. Do not use this method when deserializing and batch
        // constructing Overlays, instead use AddOverlay/RestoreOverlays.
        internal T GetOrCreateOverlay<T>(string id = null) where T : Overlay, new()
        {
            var attrib = OverlayUtilities.GetAttribute(containerWindow.GetType(), typeof(T));

            if (string.IsNullOrEmpty(id))
                id = attrib.id;

            if(m_Overlays.FirstOrDefault(x => x is T && x.id == id) is T overlay)
                return overlay;

            overlay = new T();
            overlay.Initialize(id, attrib.ussName, attrib.displayName);

            if (overlay is LegacyOverlay legacy)
                legacy.dontSaveInLayout = true;

            AddOverlay(overlay);
            RestoreOverlay(overlay);

            return overlay;
        }

        SaveData FindSaveData(Overlay overlay)
        {
            return m_SaveData.FirstOrDefault(x => x.id == overlay.id) ?? k_DefaultSaveData;
        }

        void RestoreOverlay(Overlay overlay, SaveData data = null)
        {
            if(data == null)
                data = FindSaveData(overlay);

            overlay.ApplySaveData(data);

            var container = containers.FirstOrDefault(x => data.containerId == x.name);

            // Overlays were implemented with the idea that they are always associated with an OverlayContainer. While
            // this doesn't really need to be true (floating Overlays don't need a Container), the code isn't capable
            // of handling that case. So if a valid container can't be found from the serialized data, we just add it
            // to a default container.
            if(container == null)
                container = overlay is ToolbarOverlay ? defaultToolbarContainer : defaultContainer;

            // Overlays are sorted by their index in containers so we can directly add them to top or bottom without
            // thinking of order
            if(data.dockPosition == DockPosition.Top)
                container.AddToTop(overlay);
            else if (data.dockPosition == DockPosition.Bottom)
                container.AddToBottom(overlay);
            else
                throw new Exception("data.dockPosition is not Top or Bottom, did someone add a new one?");

            if(overlay.floating)
                floatingContainer.Add(overlay.rootVisualElement);

            overlay.SetDisplayedNoCallback(data.displayed);
            overlay.RebuildContent();
            overlay.UpdateAbsolutePosition();
        }

        void RestoreOverlays()
        {
            if (containers == null)
                return;

            // Clear existing Overlays
            foreach (var overlay in overlays)
                overlay.container?.RemoveOverlay(overlay);

            // Three steps to reinitialize a canvas:
            // 1. Find and associate all Overlays with SaveData (using default SaveData if necessary)
            // 2. Sort in ascending order by SaveData.index
            // 3. Apply SaveData, insert Overlay in Container
            var ordered = new List<Tuple<SaveData, Overlay>>();

            foreach(var o in overlays)
                ordered.Add(new Tuple<SaveData, Overlay>(FindSaveData(o), o));

            foreach (var o in ordered.OrderBy(x => x.Item1.index))
                RestoreOverlay(o.Item2, o.Item1);

            afterOverlaysInitialized?.Invoke();
        }
    }
}
