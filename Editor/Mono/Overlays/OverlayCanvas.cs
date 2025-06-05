// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    [Serializable]
    public class SaveData : IEquatable<SaveData>
    {
        // Note on the obsolete fields in this class:
        // Previously, overlays were not serialized in any form. In 2023.2, overlays are serialized via json to the
        // SaveData.contents field. This removes the need for many state variables previously required to restore an
        // overlay to it's last position, visibility, dock, etc. SaveData as a class is still necessary, however, as
        // the non-obsolete fields are conditions that are not known the Overlay itself.
        // In the interest of backwards compatibility, the obsolete fields are left here for 2023.2. In 2024.1 (or
        // whatever the next version happens to be), consider removing these. Overlay save data is implicitly updated
        // when closing an editor or window for all overlays known to the window.

        const int k_InvalidIndex = -1;

        [SerializeField]
        internal DockPosition dockPosition = DockPosition.Bottom;
        [SerializeField]
        internal string containerId = string.Empty;
        [SerializeField]
        internal bool displayed;
        [SerializeField]
        internal string id;
        [SerializeField]
        internal int index = k_InvalidIndex;
        [SerializeField]
        internal string contents;

        [SerializeField, Obsolete]
        internal bool floating;
        [SerializeField, Obsolete]
        internal bool collapsed;
        [SerializeField, Obsolete]
        internal Vector2 snapOffset;
        [SerializeField, Obsolete]
        internal Vector2 snapOffsetDelta;
        [SerializeField, Obsolete]
        internal SnapCorner snapCorner;
        [SerializeField, Obsolete]
        internal Layout layout = Layout.Panel;
        [SerializeField, Obsolete]
        internal Vector2 size;
        [SerializeField, Obsolete]
        [FormerlySerializedAs("sizeOverriden")]
        internal bool sizeOverridden;

        public SaveData() { }

#pragma warning disable 612

        internal SaveData(SaveData other)
        {
            dockPosition = other.dockPosition;
            containerId = other.containerId;
            id = other.id;
            index = other.index;
            contents = other.contents;

            // obsolete
            floating = other.floating;
            collapsed = other.collapsed;
            displayed = other.displayed;
            snapOffset = other.snapOffset;
            snapOffsetDelta = other.snapOffsetDelta;
            snapCorner = other.snapCorner;
            layout = other.layout;
            size = other.size;
            sizeOverridden = other.sizeOverridden;
        }

        public SaveData(Overlay overlay)
            : this(overlay, k_InvalidIndex) { }

        internal SaveData(Overlay overlay, int indexInContainer)
        {
            if (indexInContainer < 0)
                if (overlay.container == null || !overlay.container.GetOverlayIndex(overlay, out _, out indexInContainer))
                    indexInContainer = k_InvalidIndex;

            string container = overlay.container != null ? overlay.container.name : "";
            DockPosition dock = overlay.container != null
                                && overlay.container.ContainsOverlay(overlay, OverlayContainerSection.BeforeSpacer)
                ? DockPosition.Top
                : DockPosition.Bottom;

            containerId = container;
            index = indexInContainer;
            dockPosition = dock;
            id = overlay.id;
            displayed = overlay.displayed;
            contents = EditorJsonUtility.ToJson(overlay);

            // obsolete
            floating = overlay.floating;
            collapsed = overlay.collapsed;
            layout = overlay.layout;
            snapCorner = overlay.floatingSnapCorner;
            snapOffset = overlay.floatingSnapOffset - overlay.m_SnapOffsetDelta;
            snapOffsetDelta = overlay.m_SnapOffsetDelta;
            size = overlay.sizeToSave;
            sizeOverridden = overlay.sizeOverridden;
        }

        public bool Equals(SaveData other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return dockPosition == other.dockPosition
                && containerId == other.containerId
                && id == other.id
                && index == other.index
                && displayed == other.displayed
                && floating == other.floating
                && collapsed == other.collapsed
                && snapOffset.Equals(other.snapOffset)
                && snapOffsetDelta.Equals(other.snapOffsetDelta)
                && snapCorner == other.snapCorner
                && layout == other.layout
                && size == other.size
                && sizeOverridden == other.sizeOverridden;
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
                hashCode = (hashCode * 397) ^ (id != null ? id.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ collapsed.GetHashCode();
                hashCode = (hashCode * 397) ^ displayed.GetHashCode();
                hashCode = (hashCode * 397) ^ snapOffset.GetHashCode();
                hashCode = (hashCode * 397) ^ snapOffsetDelta.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)snapCorner;
                hashCode = (hashCode * 397) ^ index;
                hashCode = (hashCode * 397) ^ (int)layout;
                hashCode = (hashCode * 397) ^ size.GetHashCode();
                hashCode = (hashCode * 397) ^ sizeOverridden.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"dockPosition: {dockPosition}" +
                   $"\ncontainerId: {containerId}" +
                   $"\nfloating: {floating}" +
                   $"\ncollapsed: {collapsed}" +
                   $"\ndisplayed: {displayed}" +
                   $"\nsnapOffset: {snapOffset}" +
                   $"\nsnapOffsetDelta: {snapOffsetDelta}" +
                   $"\nsnapCorner: {snapCorner}" +
                   $"\nid: {id}" +
                   $"\nindex: {index}" +
                   $"\nlayout: {layout}" +
                   $"\nsize: {size}" +
                   $"\nsizeOverridden: {sizeOverridden}";
        }
#pragma warning restore 612
    }

    [Serializable]
    sealed class ContainerData
    {
        public string containerId;
        public float scrollOffset;
    }

    //Dock position within container
    //for a horizontal container, Top is left, Bottom is right
    public enum DockPosition
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

    // public API to set a default docking zone. default is RightColumnBottom
    public enum DockZone
    {
        LeftToolbar = 0,
        RightToolbar = 1,
        TopToolbar = 2,
        BottomToolbar = 3,
        LeftColumn = 4,
        RightColumn = 5,
        Floating = 6
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
        internal const int k_OverlayMinVisibleArea = 24;

        const string k_FloatingContainer = "overlay-container--floating";
        const string k_ToolbarArea = "overlay-toolbar-area";
        const string k_DropTargetClassName = "overlay-droptarget";
        const string k_DefaultContainer = "overlay-container-default";
        static VisualTreeAsset s_TreeAsset;
        static VisualTreeAsset s_DropZoneTreeAsset;

        static SaveData defaultSaveData => new SaveData()
        {
            containerId = null,
            displayed = false,
            dockPosition = DockPosition.Bottom,
            index = int.MaxValue
        };

        // order must match OverlayDockArea
        static readonly string[] k_DockZoneContainerIDs = new string[7]
        {
            "overlay-toolbar__left",
            "overlay-toolbar__right",
            "overlay-toolbar__top",
            "overlay-toolbar__bottom",
            "overlay-container--left",
            "overlay-container--right",
            "Floating"
        };

        internal static DockZone GetDockZone(OverlayContainer container)
        {
            for(int i = 0, c = k_DockZoneContainerIDs.Length; i < c; i++)
                if (k_DockZoneContainerIDs[i] == container.name)
                    return (DockZone)i;
            return DockZone.Floating;
        }
        
        // used by tests
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal OverlayContainer GetDockZoneContainer(DockZone zone)
        {
            foreach(var container in m_Containers)
                if (container.name == k_DockZoneContainerIDs[(int)zone])
                    return container;
            return null;
        }

        //Used by tests
        internal bool m_MouseInCurrentCanvas = false;

        internal string lastAppliedPresetName => m_LastAppliedPresetName;
        List<Overlay> m_Overlays = new List<Overlay>();
        List<Overlay> m_TransientOverlays = new();

        internal static string k_DefaultPresetName = "Default";
        [SerializeField]
        string m_LastAppliedPresetName = k_DefaultPresetName;

        [SerializeField]
        List<SaveData> m_SaveData = new List<SaveData>();

        [SerializeField]
        List<ContainerData> m_ContainerData = new List<ContainerData>();

        [SerializeField]
        bool m_OverlaysVisible = true;
        
        bool m_OverlaysSupportEnabled = true;

        VisualElement m_RootVisualElement;
        internal EditorWindow containerWindow { get; set; }

        internal FloatingOverlayContainer floatingContainer => m_FloatingOverlayContainer ??= new FloatingOverlayContainer {canvas = this};

        FloatingOverlayContainer m_FloatingOverlayContainer;
        Overlay m_HoveredOverlay;

        internal VisualElement rootVisualElement => m_RootVisualElement ??= CreateRoot();

        internal Overlay hoveredOverlay => m_HoveredOverlay;
        OverlayContainer hoveredOverlayContainer { get; set; }
        OverlayContainer defaultContainer { get; set; }
        OverlayContainer defaultToolbarContainer { get; set; }

        internal OverlayDockArea dockArea { get; private set; }

        List<OverlayContainer> m_Containers;

        internal IEnumerable<OverlayContainer> containers => m_Containers;

        readonly Dictionary<VisualElement, Overlay> m_OverlaysByVE = new Dictionary<VisualElement, Overlay>();

        public IEnumerable<Overlay> overlays => m_Overlays.AsReadOnly();

        internal IEnumerable<Overlay> transientOverlays => m_TransientOverlays;

        OverlayPopup m_PopupOverlay;

        VisualElement m_WindowRoot;
        internal VisualElement windowRoot => m_WindowRoot;

        internal Action afterOverlaysInitialized;
        internal event Action<bool> overlaysEnabledChanged;
        internal event Action<bool> overlaysSupportEnabledChanged;
        internal event Action presetChanged;


        internal event Action overlayListChanged;

        public bool overlaysEnabled
        {
            get => m_Containers != null && m_OverlaysVisible;

            set
            {
                var changed = m_OverlaysVisible != value;
                if (changed)
                    m_OverlaysVisible = value;

                // Ensure the containers' state refreshes in case m_OverlaysVisible value is somehow desynced from container state
                var allContainersMatchEnabledState = true;
                foreach (var container in m_Containers)
                {
                    if (container.style.display != (value ? DisplayStyle.Flex : DisplayStyle.None))
                    {
                        allContainersMatchEnabledState = false;
                        break;
                    }
                }
                if (!allContainersMatchEnabledState)
                {
                    foreach (var container in m_Containers)
                        container.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
                }

                if (changed)
                    overlaysEnabledChanged?.Invoke(m_OverlaysVisible);
            }
        }

        internal bool overlaysSupportEnabled
        {
            get => m_OverlaysSupportEnabled;

            set
            {
                if (value != m_OverlaysSupportEnabled)
                {
                    m_OverlaysSupportEnabled = value;

                    if (!m_OverlaysSupportEnabled)
                    {
                        // Ensure no lingering popup is left open
                        ClosePopupOverlay();
                        // Hide all overlay containers
                        foreach (var container in m_Containers)
                            container.style.display = DisplayStyle.None;
                    }
                    // Unhide overlay containers if they're enabled when reactivating support
                    else if (overlaysEnabled)
                    {
                        foreach (var container in m_Containers)
                            container.style.display = DisplayStyle.Flex;
                    }

                    overlaysSupportEnabledChanged?.Invoke(value);
                }
            }
        }

        internal OverlayCanvas() { }

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

            ve.name = ussClassName;
            ve.style.flexGrow = 1;

            ve.Add(floatingContainer);
            floatingContainer.AddToClassList(k_FloatingContainer);
            floatingContainer.name = "Floating";

            m_Containers = ve.Query<OverlayContainer>().ToList();

            foreach (var container in m_Containers)
            {
                container.RegisterCallback<MouseEnterEvent>(OnMouseEnterOverlayContainer);
                if (container.ClassListContains(k_DefaultContainer))
                {
                    if (container.ClassListContains(k_ToolbarArea))
                        defaultToolbarContainer = container;
                    else
                        defaultContainer = container;
                }

                var data = GetContainerData(container.name);
                if (container is ToolbarOverlayContainer toolbar)
                    toolbar.scrollOffset = data.scrollOffset;
            }

            SetPickingMode(ve, PickingMode.Ignore);

            ve.RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            ve.RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);

            m_WindowRoot = ve.Q("overlay-window-root");

            ve.Add(dockArea = new OverlayDockArea(this));
            m_WindowRoot.RegisterCallback<GeometryChangedEvent>((evt) =>
            {
                var worldPos = m_WindowRoot.LocalToWorld(evt.newRect.position);
                dockArea.transform.position = ve.WorldToLocal(worldPos);
                dockArea.style.width = evt.newRect.width;
                dockArea.style.height = evt.newRect.height;
            });

            overlaysEnabled = m_OverlaysVisible;

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
            //this is used to clamp overlays to floating container bounds.
            floatingContainer.RegisterCallback<GeometryChangedEvent>(GeometryChanged);
            rootVisualElement.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            rootVisualElement.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            EditorWindow.windowFocusChanged += OnFocusedWindowChanged;

        }

        void OnDetachedFromPanel(DetachFromPanelEvent evt)
        {
            floatingContainer.UnregisterCallback<GeometryChangedEvent>(GeometryChanged);
            rootVisualElement.UnregisterCallback<MouseEnterEvent>(OnMouseEnter);
            rootVisualElement.UnregisterCallback<MouseLeaveEvent>(OnMouseLeave);
            EditorWindow.windowFocusChanged -= OnFocusedWindowChanged;
        }

        internal void OnContainerWindowDisabled()
        {
            if (containerWindow != null)
            {
                var overlayCanvasesData = OverlayCanvasesData.instance;
                var containerTypeWindows = Resources.FindObjectsOfTypeAll(containerWindow.GetType());
                // This closing container is last instance of its type
                if (containerTypeWindows.Length <= 1)
                {
                    // Ensures m_SaveData is refreshed.
                    OnBeforeSerialize();
                    overlayCanvasesData.AddAndSaveCanvasData(containerWindow, m_SaveData);
                }
            }
            
            foreach (var overlay in m_Overlays)
                overlay.OnWillBeDestroyed();
        }

        void OnFocusedWindowChanged()
        {
            if (EditorWindow.focusedWindow == containerWindow)
                OverlayCanvasesData.instance.SetLastActiveCanvasForWindowType(this);
        }

        void OnMouseEnter(MouseEnterEvent evt)
        {
            m_MouseInCurrentCanvas = true;
        }

        void OnMouseLeave(MouseLeaveEvent evt)
        {
            m_MouseInCurrentCanvas = false;
        }

        internal Rect ClampToOverlayWindow(Rect rect)
        {
            return ClampRectToBounds(rootVisualElement.localBound, rect);
        }

        // ensure that a minimum area of a rect is within boundary
        internal static Rect ClampRectToBounds(Rect boundary, Rect rectToClamp)
        {
            if (rectToClamp.x > boundary.xMax - k_OverlayMinVisibleArea)
                rectToClamp.x = boundary.xMax - k_OverlayMinVisibleArea;

            if (rectToClamp.xMax < boundary.xMin + k_OverlayMinVisibleArea)
                rectToClamp.x = (boundary.xMin + k_OverlayMinVisibleArea) - rectToClamp.width;

            if (rectToClamp.y > boundary.yMax - k_OverlayMinVisibleArea)
                rectToClamp.y = boundary.yMax - k_OverlayMinVisibleArea;

            if (rectToClamp.y < boundary.yMin)
                rectToClamp.y = boundary.yMin;

            return rectToClamp;
        }

        // clamp all overlays to  root visual element's new bounds
        void GeometryChanged(GeometryChangedEvent evt)
        {
            if (!overlaysEnabled || !overlaysSupportEnabled)
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

        internal bool HasTransientOverlays()
        {
            return m_TransientOverlays.Count > 0;
        }

        internal bool IsTransient(Overlay overlay) => m_TransientOverlays.Contains(overlay);

        internal Func<OverlayUtilities.OverlayEditorWindowAssociation, bool> filterOverlays;

        internal void Initialize(EditorWindow window)
        {
            Profiler.BeginSample("OverlayCanvas.Initialize");
            containerWindow = window;

            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;

            List<Type> overlayTypes = OverlayUtilities.GetOverlaysForType(window.GetType(), filterOverlays);

            // init all overlays
            foreach (var overlayType in overlayTypes)
                AddOverlay(OverlayUtilities.CreateOverlay(overlayType));
            
            // No save data deserialized from layout or this is a new instance
            if (m_SaveData == null || m_SaveData.Count < 1)
            {
                var containerTypeWindows = Resources.FindObjectsOfTypeAll(containerWindow.GetType());
                var overlayCanvasesData = OverlayCanvasesData.instance;
                
                // If our container is not the first instance of its type (i.e. container window is duplicate),
                // we want to "inherit" overlay save data from the last focused overlay canvas.
                if (containerTypeWindows.Length > 1 && overlayCanvasesData.TryGetLastActiveCanvasForWindowType(containerWindow, out var lastActiveCanvas))
                {
                    lastActiveCanvas.CopySaveData(out var lastActiveCanvasData);
                    m_SaveData = new List<SaveData>(lastActiveCanvasData);
                }
                // Otherwise check if SaveData's been serialized in OverlayCanvasesData asset during last window close of container type.
                else if (!overlayCanvasesData.GetCanvasSaveData(containerWindow, out m_SaveData))
                {
                    var preset = OverlayPresetManager.GetDefaultPreset(window.GetType());
                    if (preset != null && preset.saveData != null)
                        m_SaveData = new List<SaveData>(preset.saveData);
                }
            }

            // If save data is still null at this point, initialize it to empty list as code down the line is not expecting it to be null
            if (m_SaveData == null)
                m_SaveData = new List<SaveData>();

            RestoreOverlays();
            Profiler.EndSample();
        }

        void OnBeforeAssemblyReload()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            foreach (var overlay in m_Overlays)
                overlay.rootVisualElement.UnregisterCallback<GeometryChangedEvent>(overlay.OnGeometryChanged);
        }

        void WriteOrReplaceSaveData(Overlay overlay, int containerIndex = -1)
        {
            var saveData = new SaveData(overlay, containerIndex);
            int existing = m_SaveData.FindIndex(x => x.id == overlay.id);

            if (existing < 0)
                m_SaveData.Add(saveData);
            else
                m_SaveData[existing] = saveData;
        }

        public void OnBeforeSerialize()
        {
            if (m_Containers == null)
                return;

            foreach (var container in m_Containers)
            {
                if (container != null)
                {
                    var before = container.GetSection(OverlayContainerSection.BeforeSpacer);
                    var after = container.GetSection(OverlayContainerSection.AfterSpacer);

                    for (int i = 0, c = before.Count; i < c; ++i)
                    {
                        if (!before[i].dontSaveInLayout)
                            WriteOrReplaceSaveData(before[i], i);
                    }

                    for (int i = 0, c = after.Count; i < c; ++i)
                    {
                        if (!after[i].dontSaveInLayout)
                            WriteOrReplaceSaveData(after[i], i);
                    }

                    var data = GetContainerData(container.name);
                    if (container is ToolbarOverlayContainer toolbar)
                        data.scrollOffset = toolbar.scrollOffset;
                }
            }
        }

        public void OnAfterDeserialize() {}

        // used by tests
        internal void CopySaveData(out SaveData[] saveData)
        {
            // Force a save of the current data
            OnBeforeSerialize();
            saveData = m_SaveData.ToArray();
            for (int i = 0; i < saveData.Length; ++i)
                saveData[i] = new SaveData(saveData[i]);
        }

        internal void SetLastAppliedPresetName(string name)
        {
            m_LastAppliedPresetName = name;
        }

        internal void ApplyPreset(IOverlayPreset preset)
        {
            if (!preset.CanApplyToWindow(containerWindow.GetType()))
            {
                Debug.LogError($"Cannot apply preset for type {preset.targetWindowType} to canvas of type " +
                    $"{containerWindow.GetType()}");
                return;
            }

            SetLastAppliedPresetName(preset.name);
            ApplySaveData(preset.saveData);
            preset.ApplyCustomData(this);
            presetChanged?.Invoke();
        }

        internal void ApplySaveData(SaveData[] saveData)
        {
            m_SaveData = new List<SaveData>(saveData);
            RestoreOverlays();
        }

        internal void Move(Overlay overlay, DockZone zone, DockPosition position = DockPosition.Bottom)
        {
            var container = GetDockZoneContainer(zone);
            if (position == DockPosition.Bottom)
                overlay.DockAt(container, OverlayContainerSection.AfterSpacer);
            else
                overlay.DockAt(container, OverlayContainerSection.BeforeSpacer);
        }

        internal void Rebuild()
        {
            OnBeforeSerialize();
            RestoreOverlays();
        }

        // Overlays added to the canvas through this method are considered "temporary" and will be shown in separate
        // category in the menu. Persistent overlays (i.e., overlays registered through OverlayAttribute) are not
        // created using this method.
        public void Add(Overlay overlay)
        {
            if(m_Overlays.Contains(overlay))
                return;
            overlay.canvas?.Remove(overlay);
            AddOverlay(overlay, true);
            RestoreOverlay(overlay);
        }

        public bool Remove(Overlay overlay)
        {
            if (!m_Overlays.Remove(overlay))
                return false;
            m_TransientOverlays.Remove(overlay);
            overlay.OnWillBeDestroyed();
            WriteOrReplaceSaveData(overlay);
            overlay.container?.RemoveOverlay(overlay);
            overlay.canvas = null;
            var root = overlay.rootVisualElement;
            m_OverlaysByVE.Remove(root);
            root.UnregisterCallback<MouseEnterEvent>(OnMouseEnterOverlay);
            root.UnregisterCallback<MouseLeaveEvent>(OnMouseLeaveOverlay);
            root.RemoveFromHierarchy();
            overlayListChanged?.Invoke();
            return true;
        }

        public void ShowPopup<T>() where T : Overlay, new()
        {
            if (ClosePopupOverlay() || !overlaysSupportEnabled)
                return;

            var popup = OverlayPopup.CreateAtCanvasCenter(this, CreateOverlayForPopup<T>());
            SetActiveOverlayPopup(popup);
        }

        public void ShowPopupAtMouse<T>() where T : Overlay, new()
        {
            if (!m_MouseInCurrentCanvas)
            {
                ClosePopupOverlay();
                return;
            }

            if (!overlaysSupportEnabled)
                return;

            ShowPopup<T>(PointerDeviceState.GetPointerPosition(PointerId.mousePointerId, ContextType.Editor));
        }

        public void ShowPopup<T>(Vector2 position) where T : Overlay, new()
        {
            if (ClosePopupOverlay() || !overlaysSupportEnabled)
                return;

            var popup = OverlayPopup.CreateAtPosition(this, CreateOverlayForPopup<T>(), position);
            SetActiveOverlayPopup(popup);
        }

        T CreateOverlayForPopup<T>() where T : Overlay, new()
        {
            var overlay = new T();
            var overlayName = OverlayUtilities.GetDisplayNameFromAttribute(typeof(T));
            if (overlayName == string.Empty)
                overlayName = ObjectNames.NicifyVariableName(typeof(T).Name);

            // OnCreated must be invoked before contents are requested for the first time
            overlay.canvas = this;
            overlay.isPopup = true;
            overlay.OnCreated();
            overlay.displayed = false;
            overlay.displayName = overlayName;

            return overlay;
        }

        void SetActiveOverlayPopup(OverlayPopup popup)
        {
            m_PopupOverlay = popup;
            m_PopupOverlay.RegisterCallback<FocusOutEvent>(evt =>
            {
                if (evt.relatedTarget is VisualElement target && (m_PopupOverlay == target || m_PopupOverlay.Contains(target)))
                    return;

                // When the new focus is an embedded IMGUIContainer or popup window, give focus back to the modal
                // popup so that the next focus out event has the opportunity to close the element.
                if (evt.relatedTarget == null && m_PopupOverlay.containsCursor)
                    EditorApplication.delayCall += m_PopupOverlay.Focus;
                else
                    ClosePopupOverlay();
            });

            rootVisualElement.Add(m_PopupOverlay);
            m_PopupOverlay.Focus();
        }

        internal bool ClosePopupOverlay()
        {
            if (m_PopupOverlay == null)
                return false;

            m_PopupOverlay.overlay.OnWillBeDestroyed();
            m_PopupOverlay.RemoveFromHierarchy();
            m_PopupOverlay = null;
            return true;
        }

        // AddOverlay just registers the Overlay with Canvas. It does not init save data or add to a valid container.
        void AddOverlay(Overlay overlay, bool transient = false)
        {
            // Don't show an error when attempting to add a null overlay. This means that a persistent Overlay type was
            // removed from a project, or moved to a transient overlay. In either case, the user can't do anything
            // meaningful with this information.
            if (overlay == null)
                return;

            if(!OverlayUtilities.EnsureValidId(m_Overlays, overlay))
            {
                Debug.LogError($"An overlay with id \"{overlay.id}\" was already registered to window " +
                    $"({containerWindow.titleContent.text}).");
                return;
            }

            overlay.canvas = this;
            m_Overlays.Add(overlay);
            if (transient)
                m_TransientOverlays.Add(overlay);
            m_OverlaysByVE[overlay.rootVisualElement] = overlay;
            overlay.rootVisualElement.RegisterCallback<MouseEnterEvent>(OnMouseEnterOverlay);
            overlay.rootVisualElement.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveOverlay);


            // OnCreated must be invoked before contents are requested for the first time
            overlay.OnCreated();

            overlayListChanged?.Invoke();
        }

        internal bool TryGetOverlay(string id, out Overlay overlay)
        {
            overlay = m_Overlays.FirstOrDefault(x => x.id == id);
            return overlay != null;
        }

        internal bool TryGetOverlay<T>(string id, out T overlay) where T : Overlay
        {
            overlay = m_Overlays.FirstOrDefault(x => x is T && x.id == id) as T;
            return overlay != null;
        }

        // GetOrCreateOverlay is used to instantiate Overlays. Do not use this method when deserializing and batch
        // constructing Overlays, instead use AddOverlay/RestoreOverlays.
        internal T GetOrCreateOverlay<T>(string id = null) where T : Overlay, new()
        {
            var attrib = OverlayUtilities.GetAttribute(containerWindow.GetType(), typeof(T));

            if (string.IsNullOrEmpty(id))
                id = attrib.id;

            if(TryGetOverlay(id, out T overlay))
                return overlay;

            overlay = new T();
            overlay.Initialize(id, attrib.ussName, attrib.displayName, attrib.defaultSize, attrib.minSize, attrib.maxSize);

            if (overlay is LegacyOverlay legacy)
                legacy.dontSaveInLayout = true;

            AddOverlay(overlay);
            RestoreOverlay(overlay);

            return overlay;
        }

        // used by tests
        internal SaveData FindSaveData(Overlay overlay)
        {
            var data = m_SaveData.FirstOrDefault(x => x.id == overlay.id);

            if (data == null)
            {
                data = defaultSaveData;

                var attrib = overlay.GetType().GetCustomAttribute<OverlayAttribute>();

                if (attrib != null)
                {
                    data.containerId = k_DockZoneContainerIDs[(int)attrib.defaultDockZone];
                    data.index = attrib.defaultDockIndex;
                    data.dockPosition = attrib.defaultDockPosition;
                    data.displayed = attrib.defaultDisplay;
                    overlay.layout = attrib.defaultLayout;

                    // also apply to obsolete SaveData fields for backwards compatibility (ie, there is no
                    // SaveData.contents but we still want layout and size attribute values to be forwarded)
#pragma warning disable 612
                    data.layout = attrib.defaultLayout;
                    data.floating = attrib.defaultDockZone == DockZone.Floating;
#pragma warning restore 612
                }
            }

            return data;
        }

        public void ResetOverlay(Overlay overlay)
        {
            overlay.sizeOverridden = false;

            var attrib = overlay.GetType().GetCustomAttribute<OverlayAttribute>();
            overlay.Reset(attrib);

            var container = GetDockZoneContainer(attrib.defaultDockZone);

            if (attrib.defaultDockPosition == DockPosition.Top)
            {
                var index = Mathf.Min(attrib.defaultDockIndex, container.GetSectionCount(OverlayContainerSection.BeforeSpacer) - (container.ContainsOverlay(overlay, OverlayContainerSection.BeforeSpacer) ? 1 : 0));

                overlay.DockAt(container, OverlayContainerSection.BeforeSpacer, index);
            }
            else if (attrib.defaultDockPosition == DockPosition.Bottom)
            {
                var index = Mathf.Min(attrib.defaultDockIndex, container.GetSectionCount(OverlayContainerSection.AfterSpacer) - (container.ContainsOverlay(overlay, OverlayContainerSection.AfterSpacer) ? 1 : 0));
                overlay.DockAt(container, OverlayContainerSection.AfterSpacer, index);
            }
            else
                throw new Exception("data.dockPosition is not Top or Bottom, did someone add a new one?");

            if(overlay.floating)
                overlay.Undock();

            if (overlay.displayed != attrib.defaultDisplay)
            {
                // start from "not shown" state so that Overlay.displayedChanged is called
                overlay.rootVisualElement.style.display = DisplayStyle.None;
                overlay.displayed = attrib.defaultDisplay;
            }
            else
                overlay.RebuildContent();

            overlay.UpdateAbsolutePosition();
        }

        public void RestoreOverlay(Overlay overlay, SaveData data = null)
        {
            if(data == null)
                data = FindSaveData(overlay);

            try
            {
                EditorJsonUtility.FromJsonOverwrite(data.contents, overlay);
            }
            catch (ArgumentException)
            {
                data.contents = string.Empty;
            }

            #pragma warning disable 618
            if(string.IsNullOrEmpty(data.contents))
                overlay.ApplySaveData(data);
            #pragma warning restore 618

            var container = m_Containers.FirstOrDefault(x => data.containerId == x.name);

            // Overlays were implemented with the idea that they are always associated with an OverlayContainer. While
            // this doesn't really need to be true (floating Overlays don't need a Container), the code isn't capable
            // of handling that case. So if a valid container can't be found from the serialized data, we just add it
            // to a default container.
            if(container == null)
                container = overlay is ToolbarOverlay ? defaultToolbarContainer : defaultContainer;



            // Overlays are sorted by their index in containers so we can directly add them to top or bottom without
            // thinking of order
            if (data.dockPosition == DockPosition.Top || container is FloatingOverlayContainer)
                overlay.DockAt(container, OverlayContainerSection.BeforeSpacer);
            else if (data.dockPosition == DockPosition.Bottom)
                overlay.DockAt(container, OverlayContainerSection.AfterSpacer);
            else
                throw new Exception("data.dockPosition is not Top or Bottom, did someone add a new one?");

            if(overlay.floating)
                overlay.Undock();

            // when restoring an overlay from serialized state, always start from "not shown" state so that
            // Overlay.displayedChanged is called
            overlay.rootVisualElement.style.display = DisplayStyle.None;

            if(overlay.displayed != data.displayed)
                overlay.displayed = data.displayed;
            else
                overlay.RebuildContent();

            overlay.UpdateAbsolutePosition();
        }

        void RestoreOverlays()
        {
            if (m_Containers == null)
                return;

            // Clear OverlayContainer instances and set Overlay.displayed to false. RestoreOverlay expects that Overlay
            // is not present in VisualElement hierarchy.
            foreach (var overlay in this.overlays)
            {
                overlay.displayed = false;
                overlay.container?.RemoveOverlay(overlay);
            }

            // Three steps to reinitialize a canvas:
            // 1. Find and associate all Overlays with SaveData (using default SaveData if necessary)
            // 2. Sort in ascending order by SaveData.index
            // 3. Apply SaveData, insert Overlay in Container
            var overlays = new List<Tuple<SaveData, Overlay>>();

            foreach(var o in this.overlays)
                overlays.Add(new Tuple<SaveData, Overlay>(FindSaveData(o), o));

            var ordered = overlays.OrderBy(x => x.Item1.index);
            foreach (var o in ordered)
                RestoreOverlay(o.Item2, o.Item1);

            afterOverlaysInitialized?.Invoke();
        }

        ContainerData GetContainerData(string containerId)
        {
            foreach (var data in m_ContainerData)
                if (data.containerId == containerId)
                    return data;

            var newData = new ContainerData
            {
                containerId = containerId,
                scrollOffset = 0
            };

            m_ContainerData.Add(newData);
            return newData;
        }
    }
}
