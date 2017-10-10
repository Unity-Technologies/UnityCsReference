// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEngine.Experimental.UIElements
{
    public enum ContextType
    {
        Player = 0,
        Editor = 1
    }

    // The process is as follows:
    // A VisualElement is Dirtied. An update pass on the axis being dirtied is done, this might cause other dirties to occur.
    // For example:
    // Changing styles MAY dirty layout which MAY dirty transform and end up causing a repaint.
    // dirtying the Style flag will cause Style to be validated and updated if needed.
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

    public abstract class BasePanelDebug
    {
        internal bool enabled { get; set; }

        internal virtual bool RecordRepaint(VisualElement visualElement)
        {
            return false;
        }

        internal virtual bool EndRepaint()
        {
            return false;
        }

        internal Func<Event, bool> interceptEvents { get; set; }
    }

    // Passed-in to every element of the visual tree
    public interface IPanel
    {
        VisualElement visualTree { get; }

        IEventDispatcher dispatcher { get; }
        ContextType contextType { get; }
        FocusController focusController { get; }
        VisualElement Pick(Vector2 point);
        VisualElement LoadTemplate(string path, Dictionary<string, VisualElement> slots = null);

        VisualElement PickAll(Vector2 point, List<VisualElement> picked);

        BasePanelDebug panelDebug { get; set; }
    }

    abstract class BaseVisualElementPanel : IPanel
    {
        public abstract EventInterests IMGUIEventInterests { get; set; }
        public abstract ScriptableObject ownerObject { get; protected set; }
        public abstract SavePersistentViewData savePersistentViewData { get; set; }
        public abstract GetViewDataDictionary getViewDataDictionary { get; set; }
        public abstract int IMGUIContainersCount { get; set; }
        public abstract FocusController focusController { get; set; }

        public abstract void Repaint(Event e);
        public abstract void ValidateLayout();

        internal virtual IStylePainter stylePainter { get; set; }
        //IPanel
        public abstract VisualElement visualTree { get; }
        public abstract IEventDispatcher dispatcher { get; protected set; }
        internal abstract IScheduler scheduler { get; }
        internal abstract IDataWatchService dataWatch { get; }

        public abstract ContextType contextType { get; protected set; }
        public abstract VisualElement Pick(Vector2 point);
        public abstract VisualElement PickAll(Vector2 point, List<VisualElement> picked);
        public abstract VisualElement LoadTemplate(string path, Dictionary<string, VisualElement> slots = null);

        public abstract bool keepPixelCacheOnWorldBoundChange { get; set; }

        public BasePanelDebug panelDebug { get; set; }
    }

    // Strategy to load assets must be provided in the context of Editor or Runtime
    internal delegate Object LoadResourceFunction(string pathName, System.Type type);

    // Getting the view data dictionary relies on the Editor window.
    internal delegate ISerializableJsonDictionary GetViewDataDictionary();

    // Strategy to save persistent data must be provided in the context of Editor or Runtime
    internal delegate void SavePersistentViewData();

    // Default panel implementation
    internal class Panel : BaseVisualElementPanel
    {
        private StyleSheets.StyleContext m_StyleContext;
        private VisualElement m_RootContainer;

        public override VisualElement visualTree
        {
            get { return m_RootContainer; }
        }

        public override IEventDispatcher dispatcher { get; protected set; }

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

        internal StyleContext styleContext
        {
            get { return m_StyleContext; }
        }

        public override ScriptableObject ownerObject { get; protected set; }

        public bool allowPixelCaching { get; set; }

        public override ContextType contextType { get; protected set; }

        public override SavePersistentViewData savePersistentViewData { get; set; }

        public override GetViewDataDictionary getViewDataDictionary { get; set; }

        public override FocusController focusController { get; set; }

        public override EventInterests IMGUIEventInterests { get; set; }

        internal static LoadResourceFunction loadResourceFunc = null;

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
                    m_RootContainer.Dirty(ChangeType.Repaint | ChangeType.Transform);
            }
        }

        public override int IMGUIContainersCount { get; set; }
        public Panel(ScriptableObject ownerObject, ContextType contextType, IDataWatchService dataWatch = null, IEventDispatcher dispatcher = null)
        {
            this.ownerObject = ownerObject;
            this.contextType = contextType;
            m_DataWatch = dataWatch;
            this.dispatcher = dispatcher;
            stylePainter = new StylePainter();
            m_RootContainer = new VisualElement();
            m_RootContainer.name = VisualElementUtils.GetUniqueName("PanelContainer");
            m_RootContainer.persistenceKey = "PanelContainer"; // Required!
            visualTree.ChangePanel(this);
            focusController = new FocusController(new VisualElementFocusRing(visualTree));
            m_StyleContext = new StyleSheets.StyleContext(m_RootContainer);

            allowPixelCaching = true;
        }

        VisualElement PickAll(VisualElement root, Vector2 point, List<VisualElement> picked = null)
        {
            // do not pick invisible
            if ((root.pseudoStates & PseudoStates.Invisible) == PseudoStates.Invisible)
                return null;

            Vector3 localPoint = root.transform.matrix.inverse.MultiplyPoint3x4(point);
            bool containsPoint = root.ContainsPoint(localPoint);

            // we only skip children in the case we visually clip them
            if (!containsPoint && root.clippingOptions != VisualElement.ClippingOptions.NoClipping)
            {
                return null;
            }

            if (picked != null && root.enabledInHierarchy && root.pickingMode == PickingMode.Position)
            {
                picked.Add(root);
            }

            // reset ref to upper left
            localPoint = localPoint - new Vector3(root.layout.position.x, root.layout.position.y, 0);

            VisualElement returnedChild = null;
            // Depth first in reverse order, do children
            for (int i = root.shadow.childCount - 1; i >= 0; i--)
            {
                var child = root.shadow[i];
                var result = PickAll(child, localPoint, picked);
                if (returnedChild == null && result != null)
                    returnedChild = result;
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
            ValidateLayout();

            return PickAll(visualTree, point);
        }

        const int kMaxValidatePersistentDataCount = 5;

        void ValidatePersistentData()
        {
            int validatePersistentDataCount = 0;
            while (visualTree.AnyDirty(ChangeType.PersistentData | ChangeType.PersistentDataPath))
            {
                ValidatePersistentDataOnSubTree(visualTree, true);
                validatePersistentDataCount++;

                if (validatePersistentDataCount > kMaxValidatePersistentDataCount)
                {
                    Debug.LogError("UIElements: Too many children recursively added that rely on persistent data: " + visualTree);
                    break;
                }
            }
        }

        void ValidatePersistentDataOnSubTree(VisualElement root, bool enablePersistence)
        {
            // We don't want to persist when there is a high chance that there will
            // be persistenceKey conflicts and data sharing. Generally, if an element
            // has no persistenceKey, we do not persist it and any of its children.
            // There are some exceptions, hence the use of IsPersitenceSupportedOnChildren().
            if (!root.IsPersitenceSupportedOnChildren())
                enablePersistence = false;

            if (root.IsDirty(ChangeType.PersistentData))
            {
                root.OnPersistentDataReady(enablePersistence);
                root.ClearDirty(ChangeType.PersistentData);
            }

            if (root.IsDirty(ChangeType.PersistentDataPath))
            {
                for (int i = 0; i < root.shadow.childCount; ++i)
                {
                    ValidatePersistentDataOnSubTree(root.shadow[i], enablePersistence);
                }

                root.ClearDirty(ChangeType.PersistentDataPath);
            }
        }

        void ValidateStyling()
        {
            // if the surface DPI changes we need to invalidate styles
            if (!Mathf.Approximately(m_StyleContext.currentPixelsPerPoint, GUIUtility.pixelsPerPoint))
            {
                m_RootContainer.Dirty(ChangeType.Styles);
                m_StyleContext.currentPixelsPerPoint = GUIUtility.pixelsPerPoint;
            }

            if (m_RootContainer.AnyDirty(ChangeType.Styles | ChangeType.StylesPath))
            {
                m_StyleContext.ApplyStyles();
            }
        }

        const int kMaxValidateLayoutCount = 5;

        public override void ValidateLayout()
        {
            ValidateStyling();

            // update flex once
            int validateLayoutCount = 0;
            while (visualTree.cssNode.IsDirty)
            {
                visualTree.cssNode.CalculateLayout();
                ValidateSubTree(visualTree);

                if (validateLayoutCount++ >= kMaxValidateLayoutCount)
                {
                    Debug.LogError("ValidateLayout is struggling to process current layout (consider simplifying to avoid recursive layout): " + visualTree);
                    break;
                }
            }
        }

        bool ValidateSubTree(VisualElement root)
        {
            // if the last layout is different than this one we must dirty transform on children
            if (root.renderData.lastLayout != new Rect(root.cssNode.LayoutX, root.cssNode.LayoutY, root.cssNode.LayoutWidth, root.cssNode.LayoutHeight))
            {
                root.Dirty(ChangeType.Transform);
                root.renderData.lastLayout = new Rect(root.cssNode.LayoutX, root.cssNode.LayoutY, root.cssNode.LayoutWidth, root.cssNode.LayoutHeight);
            }

            // ignore clean sub trees
            bool hasNewLayout = root.cssNode.HasNewLayout;
            if (hasNewLayout)
            {
                for (int i = 0; i < root.shadow.childCount; ++i)
                {
                    ValidateSubTree(root.shadow[i]);
                }
            }

            var evt = PostLayoutEvent.GetPooled(hasNewLayout);
            evt.target = root;

            UIElementsUtility.eventDispatcher.DispatchEvent(evt, this);
            PostLayoutEvent.ReleasePooled(evt);

            // reset both flags at the end
            root.ClearDirty(ChangeType.Layout);
            root.cssNode.MarkLayoutSeen();

            return hasNewLayout;
        }

        // get the AA aligned bound
        private Rect ComputeAAAlignedBound(Rect position, Matrix4x4 mat)
        {
            Rect p = position;
            Vector3 v0 = mat.MultiplyPoint3x4(new Vector3(p.x, p.y, 0.0f));
            Vector3 v1 = mat.MultiplyPoint3x4(new Vector3(p.x + p.width, p.y, 0.0f));
            Vector3 v2 = mat.MultiplyPoint3x4(new Vector3(p.x, p.y + p.height, 0.0f));
            Vector3 v3 = mat.MultiplyPoint3x4(new Vector3(p.x + p.width, p.y + p.height, 0.0f));
            return Rect.MinMaxRect(
                Mathf.Min(v0.x, Mathf.Min(v1.x, Mathf.Min(v2.x, v3.x))),
                Mathf.Min(v0.y, Mathf.Min(v1.y, Mathf.Min(v2.y, v3.y))),
                Mathf.Max(v0.x, Mathf.Max(v1.x, Mathf.Max(v2.x, v3.x))),
                Mathf.Max(v0.y, Mathf.Max(v1.y, Mathf.Max(v2.y, v3.y))));
        }

        private bool ShouldUsePixelCache(VisualElement root)
        {
            return allowPixelCaching && root.clippingOptions == VisualElement.ClippingOptions.ClipAndCacheContents &&
                (root.panel.panelDebug == null || !root.panel.panelDebug.RecordRepaint(root)) &&
                root.worldBound.size.magnitude > Mathf.Epsilon;
        }

        public void PaintSubTree(Event e, VisualElement root, Matrix4x4 offset, Rect currentGlobalClip)
        {
            if ((root.pseudoStates & PseudoStates.Invisible) == PseudoStates.Invisible)
                return;

            // update clip
            if (root.clippingOptions != VisualElement.ClippingOptions.NoClipping)
            {
                var worldBound = ComputeAAAlignedBound(root.layout, offset * root.worldTransform);
                // are we and our children clipped?
                if (!worldBound.Overlaps(currentGlobalClip))
                {
                    return;
                }

                float x1 = Mathf.Max(worldBound.x, currentGlobalClip.x);
                float x2 = Mathf.Min(worldBound.x + worldBound.width, currentGlobalClip.x + currentGlobalClip.width);
                float y1 = Mathf.Max(worldBound.y, currentGlobalClip.y);
                float y2 = Mathf.Min(worldBound.y + worldBound.height, currentGlobalClip.y + currentGlobalClip.height);

                // new global clip
                currentGlobalClip = new Rect(x1, y1, x2 - x1, y2 - y1);
            }
            else
            {
                //since our children are not clipped, there is no early out.
            }

            if (ShouldUsePixelCache(root))
            {
                // now actually paint the texture to previous group
                IStylePainter painter = stylePainter;

                // validate cache texture size first
                var worldBound = root.worldBound;
                int w = (int)worldBound.width;
                int h = (int)worldBound.height;
                int textureWidth = (int)(worldBound.width * GUIUtility.pixelsPerPoint);
                int textureHeight = (int)(worldBound.height * GUIUtility.pixelsPerPoint);

                var cache = root.renderData.pixelCache;
                if (cache != null && !keepPixelCacheOnWorldBoundChange &&
                    (cache.width != textureWidth || cache.height != textureHeight))
                {
                    Object.DestroyImmediate(cache);
                    cache = root.renderData.pixelCache = null;
                }

                float oldOpacity = stylePainter.opacity;

                // if the child node world transforms are not up to date due to changes below the pixel cache this is fine.
                if (root.IsDirty(ChangeType.Repaint)
                    || root.renderData.pixelCache == null)
                {
                    // recreate as needed
                    if (cache == null)
                    {
                        root.renderData.pixelCache = cache = new RenderTexture(
                                    textureWidth,
                                    textureHeight,
                                    32, // depth
                                    RenderTextureFormat.ARGB32,
                                    RenderTextureReadWrite.Linear);
                    }

                    // render sub tree to texture
                    var old = RenderTexture.active;
                    RenderTexture.active = cache;

                    GL.Clear(true, true, new Color(0, 0, 0, 0));

                    // fix up transform for subtree to match texture upper left
                    offset = Matrix4x4.Translate(new Vector3(-worldBound.x, -worldBound.y, 0));

                    // reset clipping
                    var textureClip = new Rect(0, 0, w, h);
                    painter.currentTransform = offset * root.worldTransform;
                    GUIClip.SetTransform(painter.currentTransform, textureClip);

                    // paint self
                    painter.currentWorldClip = textureClip;
                    root.DoRepaint(painter);
                    root.ClearDirty(ChangeType.Repaint);

                    PaintSubTreeChildren(e, root, offset, textureClip);

                    RenderTexture.active = old;
                }

                // now actually paint the texture to previous group
                painter.currentWorldClip = currentGlobalClip;
                painter.currentTransform = root.worldTransform;
                GUIClip.SetTransform(painter.currentTransform, currentGlobalClip);

                var painterParams = new TextureStylePainterParameters
                {
                    layout = root.layout,
                    texture = root.renderData.pixelCache,
                    color = Color.white,
                    scaleMode = ScaleMode.ScaleAndCrop
                };
                painter.DrawTexture(painterParams);
            }
            else
            {
                stylePainter.currentTransform = offset * root.worldTransform;
                GUIClip.SetTransform(stylePainter.currentTransform, currentGlobalClip);

                stylePainter.currentWorldClip = currentGlobalClip;
                stylePainter.mousePosition = root.worldTransform.inverse.MultiplyPoint3x4(e.mousePosition);

                stylePainter.opacity = root.style.opacity.GetSpecifiedValueOrDefault(1.0f);
                root.DoRepaint(stylePainter);
                stylePainter.opacity = 1.0f;
                root.ClearDirty(ChangeType.Repaint);

                PaintSubTreeChildren(e, root, offset, currentGlobalClip);
            }
        }

        private void PaintSubTreeChildren(Event e, VisualElement root, Matrix4x4 offset, Rect textureClip)
        {
            int count = root.shadow.childCount;
            for (int i = 0; i < count; i++)
            {
                VisualElement child = root.shadow[i];

                PaintSubTree(e, child, offset, textureClip);

                if (count != root.shadow.childCount)
                {
                    throw new NotImplementedException("Visual tree is read-only during repaint");
                }
            }
        }

        public override void Repaint(Event e)
        {
            ValidatePersistentData();
            ValidateLayout();
            stylePainter.repaintEvent = e;

            // paint
            Rect clipRect = visualTree.clippingOptions == VisualElement.ClippingOptions.NoClipping ? GUIClip.topmostRect : visualTree.layout;
            PaintSubTree(e, visualTree, Matrix4x4.identity, clipRect);

            if (panelDebug != null)
            {
                GUIClip.SetTransform(Matrix4x4.identity, new Rect(0, 0, visualTree.style.width, visualTree.style.height));
                if (panelDebug.EndRepaint())
                    this.visualTree.Dirty(ChangeType.Repaint);
            }
        }
    }

    // internal data used to cache render state
    internal class RenderData
    {
        public RenderTexture pixelCache;

        public Matrix4x4 worldTransForm = Matrix4x4.identity;
        public Rect lastLayout;
    }
}
