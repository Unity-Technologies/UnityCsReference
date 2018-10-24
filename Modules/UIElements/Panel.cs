// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Profiling;
using GraphicsDeviceType = UnityEngine.Rendering.GraphicsDeviceType;

namespace UnityEngine.Experimental.UIElements
{
    public enum ContextType
    {
        Player = 0,
        Editor = 1
    }

    // Legacy flags used to keep track of dirtied VisualElement.
    // It's replaced with VersionChangeType below and kept for backward compatibility with Dirty functions.
    // TODO : Remove the enum once Dirty functions are removed.
    [Flags]
    public enum ChangeType
    {
        // persistent data ready
        PersistentData = 1 << 6,
        // persistent data ready for children
        PersistentDataPath = 1 << 5,
        // changes to layout
        Layout = 1 << 4,
        // changes to styles, colors and other render properties
        Styles = 1 << 3,
        // transforms are invalid
        Transform = 1 << 2,
        // styles may have changed for children of this node
        StylesPath = 1 << 1,
        // pixels in the target have been changed, just repaint, only makes sense on the Panel
        Repaint = 1 << 0,
        All = Repaint | Transform | Layout | StylesPath |
            Styles | PersistentData | PersistentDataPath
    }

    [Flags]
    internal enum VersionChangeType
    {
        //Some data was bound
        Bindings = 1 << 8,
        // persistent data ready
        PersistentData = 1 << 7,
        // changes to hierarchy
        Hierarchy = 1 << 6,
        // changes to layout
        Layout = 1 << 5,
        // changes to StyleSheet, USS class
        StyleSheet = 1 << 4,
        // changes to styles, colors and other render properties
        Styles = 1 << 3,
        // transforms are invalid
        Transform = 1 << 2,
        // clips are invalid
        Clip = 1 << 1,
        // pixels in the target have been changed, just repaint, only makes sense on the Panel
        Repaint = 1 << 0,
    }

    internal class RepaintData
    {
        public Matrix4x4 currentOffset { get; set; } = Matrix4x4.identity;
        public Vector2 mousePosition { get; set; }
        public Rect currentWorldClip { get; set; }
        public Event repaintEvent { get; set; }
    }

    internal interface IPanelDebugger
    {
        IPanelDebug panelDebug { get; set; }

        bool showOverlay { get; }

        bool InterceptEvents(Event ev);
        void Refresh();
    }

    internal interface IPanelDebug
    {
        bool showOverlay { get; }
        uint highlightedElement { get; }

        void AttachDebugger(IPanelDebugger debugger);
        void DetachDebugger(IPanelDebugger debugger);

        void Refresh();

        void SetHighlightElement(VisualElement ve);
        bool InterceptEvents(Event ev);
    }

    // Passed-in to every element of the visual tree
    public interface IPanel : IDisposable
    {
        VisualElement visualTree { get; }
        EventDispatcher dispatcher { get; }
        ContextType contextType { get; }
        FocusController focusController { get; }
        VisualElement Pick(Vector2 point);
        VisualElement LoadTemplate(string path, Dictionary<string, VisualElement> slots = null);

        VisualElement PickAll(Vector2 point, List<VisualElement> picked);
    }

    abstract class BaseVisualElementPanel : IPanel
    {
        public abstract EventInterests IMGUIEventInterests { get; set; }
        public abstract ScriptableObject ownerObject { get; protected set; }
        public abstract SavePersistentViewData savePersistentViewData { get; set; }
        public abstract GetViewDataDictionary getViewDataDictionary { get; set; }
        public abstract int IMGUIContainersCount { get; set; }
        public abstract FocusController focusController { get; set; }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                UIElementsUtility.RemoveCachedPanel(ownerObject.GetInstanceID());
            else
                DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        public abstract void Repaint(Event e);
        public abstract void ValidateLayout();

        public abstract void UpdateBindings();
        public abstract void ApplyStyles();
        public abstract void DirtyStyleSheets();

        internal float currentPixelsPerPoint { get; set; } = 1.0f;

        internal bool isDirty
        {
            get { return version != repaintVersion; }
        }

        internal abstract uint version { get; }
        internal abstract uint repaintVersion { get; }

        internal abstract void OnVersionChanged(VisualElement ele, VersionChangeType changeTypeFlag);
        internal abstract void SetUpdater(IVisualTreeUpdater updater, VisualTreeUpdatePhase phase);

        internal virtual RepaintData repaintData { get; set; }
        internal virtual ICursorManager cursorManager { get; set; }
        internal virtual ContextualMenuManager contextualMenuManager { get; set; }

        //IPanel
        public abstract VisualElement visualTree { get; }
        public abstract EventDispatcher dispatcher { get; protected set; }

        internal void SendEvent(EventBase e, DispatchMode dispatchMode = DispatchMode.Queued)
        {
            Debug.Assert(dispatcher != null);
            dispatcher?.Dispatch(e, this, dispatchMode);
        }

        internal abstract IScheduler scheduler { get; }
        internal abstract IDataWatchService dataWatch { get; }

        public abstract ContextType contextType { get; protected set; }
        public abstract VisualElement Pick(Vector2 point);
        public abstract VisualElement PickAll(Vector2 point, List<VisualElement> picked);
        public abstract VisualElement LoadTemplate(string path, Dictionary<string, VisualElement> slots = null);

        internal bool disposed { get; private set; }
        internal bool allowPixelCaching { get; set; }
        public abstract bool keepPixelCacheOnWorldBoundChange { get; set; }

        internal abstract IVisualTreeUpdater GetUpdater(VisualTreeUpdatePhase phase);

        internal VisualElement topElementUnderMouse { get; set; }

        public IPanelDebug panelDebug { get; set; }
    }

    // Strategy to load assets must be provided in the context of Editor or Runtime
    internal delegate Object LoadResourceFunction(string pathName, System.Type type);

    // Strategy to fetch real time since startup in the context of Editor or Runtime
    internal delegate long TimeMsFunction();

    // Getting the view data dictionary relies on the Editor window.
    internal delegate ISerializableJsonDictionary GetViewDataDictionary();

    // Strategy to save persistent data must be provided in the context of Editor or Runtime
    internal delegate void SavePersistentViewData();

    // Default panel implementation
    internal class Panel : BaseVisualElementPanel
    {
        private VisualElement m_RootContainer;
        private VisualTreeUpdater m_VisualTreeUpdater;
        private string m_PanelName;
        private string m_ProfileUpdateName;
        private string m_ProfileLayoutName;
        private string m_ProfileBindingsName;
        private uint m_Version = 0;
        private uint m_RepaintVersion = 0;

        public override VisualElement visualTree
        {
            get { return m_RootContainer; }
        }

        public override EventDispatcher dispatcher { get; protected set; }

        private IDataWatchService m_DataWatch;
        internal override IDataWatchService dataWatch { get { return m_DataWatch; } }

        TimerEventScheduler m_Scheduler;

        public TimerEventScheduler timerEventScheduler
        {
            get { return m_Scheduler ?? (m_Scheduler = new TimerEventScheduler()); }
        }

        internal override IScheduler scheduler
        {
            get { return timerEventScheduler; }
        }

        public override ScriptableObject ownerObject { get; protected set; }

        public override ContextType contextType { get; protected set; }

        public override SavePersistentViewData savePersistentViewData { get; set; }

        public override GetViewDataDictionary getViewDataDictionary { get; set; }

        public override FocusController focusController { get; set; }

        public override EventInterests IMGUIEventInterests { get; set; }

        internal static LoadResourceFunction loadResourceFunc = null;

        internal string name
        {
            get { return m_PanelName; }
            set
            {
                m_PanelName = value;

                if (!string.IsNullOrEmpty(m_PanelName))
                {
                    m_ProfileUpdateName = $"PanelUpdate.{m_PanelName}";
                    m_ProfileLayoutName = $"PanelLayout.{m_PanelName}";
                    m_ProfileBindingsName = $"PanelBindings.{m_PanelName}";
                }
                else
                {
                    m_ProfileUpdateName = "PanelUpdate";
                    m_ProfileLayoutName = "PanelLayout";
                    m_ProfileBindingsName = "PanelBindings";
                }
            }
        }

        private static TimeMsFunction s_TimeSinceStartup;
        internal static TimeMsFunction TimeSinceStartup
        {
            get { return s_TimeSinceStartup; }
            set
            {
                if (value == null)
                {
                    value = DefaultTimeSinceStartupMs;
                }

                s_TimeSinceStartup = value;
            }
        }

        private bool m_KeepPixelCacheOnWorldBoundChange;
        public override bool keepPixelCacheOnWorldBoundChange
        {
            get { return m_KeepPixelCacheOnWorldBoundChange; }
            set
            {
                if (m_KeepPixelCacheOnWorldBoundChange == value)
                    return;

                m_KeepPixelCacheOnWorldBoundChange = value;

                // We only need to force a repaint if this flag was set from
                // true (do NOT update pixel cache) to false (update pixel cache).
                // When it was true, the pixel cache was just being transformed and
                // now we want to regenerate it at the correct resolution. Going from
                // false to true does not need a repaint because the pixel cache is
                // already valid (was being updated each transform repaint).
                if (!value)
                {
                    m_RootContainer.IncrementVersion(VersionChangeType.Transform | VersionChangeType.Repaint);
                }
            }
        }

        public override int IMGUIContainersCount { get; set; }

        internal override uint version
        {
            get { return m_Version; }
        }

        internal override uint repaintVersion
        {
            get { return m_RepaintVersion; }
        }

        public Panel(ScriptableObject ownerObject, ContextType contextType, IDataWatchService dataWatch = null, EventDispatcher dispatcher = null)
        {
            m_VisualTreeUpdater = new VisualTreeUpdater(this);

            this.ownerObject = ownerObject;
            this.contextType = contextType;
            m_DataWatch = dataWatch;
            this.dispatcher = dispatcher ?? EventDispatcher.instance;
            repaintData = new RepaintData();
            cursorManager = new CursorManager();
            contextualMenuManager = null;
            m_RootContainer = new VisualElement();
            m_RootContainer.name = VisualElementUtils.GetUniqueName("PanelContainer");
            m_RootContainer.persistenceKey = "PanelContainer"; // Required!
            visualTree.SetPanel(this);
            focusController = new FocusController(new VisualElementFocusRing(visualTree));
            m_ProfileUpdateName = "PanelUpdate";
            m_ProfileLayoutName = "PanelLayout";
            m_ProfileBindingsName = "PanelBindings";

            allowPixelCaching = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                m_VisualTreeUpdater.Dispose();

            base.Dispose(disposing);
        }

        public static long TimeSinceStartupMs()
        {
            return (s_TimeSinceStartup == null) ? DefaultTimeSinceStartupMs() : s_TimeSinceStartup();
        }

        internal static long DefaultTimeSinceStartupMs()
        {
            return (long)(Time.realtimeSinceStartup * 1000.0f);
        }

        internal static VisualElement PickAll(VisualElement root, Vector2 point, List<VisualElement> picked = null)
        {
            Profiler.BeginSample("Panel.PickAll");
            var result = PerformPick(root, point, picked);
            Profiler.EndSample();
            return result;
        }

        private static VisualElement PerformPick(VisualElement root, Vector2 point, List<VisualElement> picked = null)
        {
            // do not pick invisible
            if (root.visible == false)
                return null;

            if (root.pickingMode == PickingMode.Ignore && root.shadow.childCount == 0)
            {
                return null;
            }

            Vector3 localPoint = root.WorldToLocal(point);
            bool containsPoint = root.ContainsPoint(localPoint);

            // we only skip children in the case we visually clip them
            if (!containsPoint && root.ShouldClip())
            {
                return null;
            }

            VisualElement returnedChild = null;
            // Depth first in reverse order, do children
            for (int i = root.shadow.childCount - 1; i >= 0; i--)
            {
                var child = root.shadow[i];
                var result = PerformPick(child, point, picked);
                if (returnedChild == null && result != null)
                    returnedChild = result;
            }

            if (picked != null && root.enabledInHierarchy && root.pickingMode == PickingMode.Position && containsPoint)
            {
                picked.Add(root);
            }

            if (returnedChild != null)
                return returnedChild;

            switch (root.pickingMode)
            {
                case PickingMode.Position:
                {
                    if (containsPoint && root.enabledInHierarchy)
                    {
                        return root;
                    }
                }
                break;
                case PickingMode.Ignore:
                    break;
            }
            return null;
        }

        public override VisualElement LoadTemplate(string path, Dictionary<string, VisualElement> slots = null)
        {
            VisualTreeAsset vta = loadResourceFunc(path, typeof(VisualTreeAsset)) as VisualTreeAsset;
            if (vta == null)
            {
                return null;
            }

            return vta.CloneTree(slots);
        }

        public override VisualElement PickAll(Vector2 point, List<VisualElement> picked)
        {
            ValidateLayout();

            if (picked != null)
                picked.Clear();

            return PickAll(visualTree, point, picked);
        }

        public override VisualElement Pick(Vector2 point)
        {
            return PickAll(visualTree, point);
        }

        public override void ValidateLayout()
        {
            Profiler.BeginSample(m_ProfileLayoutName);
            m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.Styles);
            m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.Layout);
            m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.TransformClip);
            Profiler.EndSample();
        }

        public override void UpdateBindings()
        {
            Profiler.BeginSample(m_ProfileBindingsName);
            m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.Bindings);
            Profiler.EndSample();
        }

        public override void ApplyStyles()
        {
            m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.Styles);
        }

        public override void DirtyStyleSheets()
        {
            m_VisualTreeUpdater.DirtyStyleSheets();
        }

        public override void Repaint(Event e)
        {
            Debug.Assert(GUIClip.Internal_GetCount() == 0, "UIElement is not compatible with IMGUI GUIClips, only GUIClip.ParentClipScope");

            m_RepaintVersion = version;

            // if the surface DPI changes we need to invalidate styles
            if (!Mathf.Approximately(currentPixelsPerPoint, GUIUtility.pixelsPerPoint))
            {
                currentPixelsPerPoint = GUIUtility.pixelsPerPoint;
                visualTree.IncrementVersion(VersionChangeType.StyleSheet);
            }

            repaintData.repaintEvent = e;
            Profiler.BeginSample(m_ProfileUpdateName);

            m_VisualTreeUpdater.UpdateVisualTree();

            Profiler.EndSample();

            panelDebug?.Refresh();
        }

        internal override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            ++m_Version;
            m_VisualTreeUpdater.OnVersionChanged(ve, versionChangeType);
        }

        internal override void SetUpdater(IVisualTreeUpdater updater, VisualTreeUpdatePhase phase)
        {
            m_VisualTreeUpdater.SetUpdater(updater, phase);
        }

        internal override IVisualTreeUpdater GetUpdater(VisualTreeUpdatePhase phase)
        {
            return m_VisualTreeUpdater.GetUpdater(phase);
        }
    }

    // internal data used to cache render state
    internal class RenderData
    {
        public RenderTexture pixelCache;
        public Rect lastLayout;
    }
}
