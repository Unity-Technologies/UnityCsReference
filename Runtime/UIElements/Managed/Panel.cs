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
        // changes to layout
        Layout = 1 << 4,
        // changes to styles, colors and other render properties
        Styles = 1 << 3,
        // transforms are invalid
        Transform = 1 << 2,
        // styles may have changed for children of this node
        StylesPath = 1 << 1,
        // pixels in the target have been changed, just repaint, only makes sense on the Panel
        Repaint = 1 << 0
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
        VisualContainer visualTree { get; }

        IDispatcher dispatcher { get; }
        IScheduler scheduler { get; }
        IDataWatchService dataWatch { get; }
        ContextType contextType { get; }

        VisualElement Pick(Vector2 point);

        VisualElement PickAll(Vector2 point, List<VisualElement> picked);

        BasePanelDebug panelDebug { get; set; }
    }

    abstract class BaseVisualElementPanel : IPanel
    {
        public virtual VisualElement focusedElement { get; set; }

        public abstract EventInterests IMGUIEventInterests { get; set; }
        public abstract int instanceID { get; protected set; }
        public abstract LoadResourceFunction loadResourceFunc { get; protected set; }
        public abstract int IMGUIContainersCount { get; set; }

        public abstract void Repaint(Event e);
        public abstract void ValidateLayout();

        internal virtual IStylePainter stylePainter { get; set; }
        //IPanel
        public abstract VisualContainer visualTree { get; }
        public abstract IDispatcher dispatcher { get; protected set; }
        public abstract IScheduler scheduler { get; }
        public abstract IDataWatchService dataWatch { get; protected set; }
        public abstract ContextType contextType { get; protected set; }
        public abstract VisualElement Pick(Vector2 point);
        public abstract VisualElement PickAll(Vector2 point, List<VisualElement> picked);

        public BasePanelDebug panelDebug { get; set; }
    }

    // Strategy to load assets must be provided in the context of Editor or Runtime
    internal delegate Object LoadResourceFunction(string pathName, System.Type type);

    // Default panel implementation
    internal class Panel : BaseVisualElementPanel
    {
        private StyleSheets.StyleContext m_StyleContext;
        private VisualContainer m_RootContainer;

        public override VisualContainer visualTree
        {
            get { return m_RootContainer; }
        }

        public VisualContainer defaultIMRoot { get; set; }

        public override IDispatcher dispatcher { get; protected set; }

        public override IDataWatchService dataWatch { get; protected set; }

        TimerEventScheduler m_Scheduler;

        public TimerEventScheduler timerEventScheduler
        {
            get { return m_Scheduler ?? (m_Scheduler = new TimerEventScheduler()); }
        }

        public override IScheduler scheduler
        {
            get { return timerEventScheduler; }
        }

        internal StyleContext styleContext
        {
            get { return m_StyleContext; }
        }

        public override int instanceID { get; protected set; }

        public bool allowPixelCaching { get; set; }

        public override ContextType contextType { get; protected set; }

        public override EventInterests IMGUIEventInterests { get; set; }

        public override LoadResourceFunction loadResourceFunc { get; protected set; }

        public override int IMGUIContainersCount { get; set; }
        public Panel(int instanceID, ContextType contextType, LoadResourceFunction loadResourceDelegate = null, IDataWatchService dataWatch = null, IDispatcher dispatcher = null)
        {
            this.instanceID = instanceID;
            this.contextType = contextType;
            this.loadResourceFunc = loadResourceDelegate ?? Resources.Load;
            this.dataWatch = dataWatch;
            this.dispatcher = dispatcher;
            stylePainter = new StylePainter();
            m_RootContainer = new VisualContainer();
            m_RootContainer.name = VisualElementUtils.GetUniqueName("PanelContainer");
            visualTree.ChangePanel(this);
            m_StyleContext = new StyleSheets.StyleContext(m_RootContainer);
            // this really should be an IMGUI container with the EditorWindow OnGUI on it.
            defaultIMRoot = new IMContainer()
            {
                name = "DefaultOnGUI",
                pickingMode = PickingMode.Ignore,
            };
            defaultIMRoot.StretchToParentSize();
            visualTree.InsertChild(0, defaultIMRoot);

            allowPixelCaching = true;
        }

        VisualElement PickAll(VisualElement root, Vector2 point, List<VisualElement> picked = null)
        {
            // do not pick invisible
            if ((root.pseudoStates & PseudoStates.Invisible) == PseudoStates.Invisible)
                return null;

            var container = root as VisualContainer;
            Vector3 localPoint = root.transform.inverse.MultiplyPoint3x4(point);
            bool containsPoint = root.ContainsPoint(localPoint);

            if (container != null)
            {
                // we only skip children in the case we visually clip them
                if (!containsPoint && container.clipChildren)
                {
                    return null;
                }

                if (picked != null)
                {
                    picked.Add(root);
                }

                // reset ref to upper left
                localPoint = localPoint - new Vector3(container.layout.position.x, container.layout.position.y, 0);

                for (int i = container.childrenCount - 1; i >= 0; i--)
                {
                    var child = container.GetChildAt(i);
                    // Depth first in reverse order, do children
                    var result = PickAll(child, localPoint, picked);
                    if (result != null)
                        return result;
                }
            }

            switch (root.pickingMode)
            {
                case PickingMode.Position:
                {
                    if (containsPoint)
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

        void ValidateStyling()
        {
            // if the surface DPI changes we need to invalidate styles
            if (!Mathf.Approximately(m_StyleContext.currentPixelsPerPoint, GUIUtility.pixelsPerPoint))
            {
                m_RootContainer.Dirty(ChangeType.Styles);
                m_StyleContext.currentPixelsPerPoint = GUIUtility.pixelsPerPoint;
            }

            if (m_RootContainer.IsDirty(ChangeType.Styles | ChangeType.StylesPath))
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
                var container = root as VisualContainer;

                if (container != null)
                {
                    foreach (var child in container)
                    {
                        hasNewLayout |= ValidateSubTree(child);
                    }
                }
            }

            root.OnPostLayout(hasNewLayout);

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

        public void PaintSubTree(Event e, VisualElement root, Matrix4x4 offset, Rect currentGlobalClip)
        {
            if ((root.pseudoStates & PseudoStates.Invisible) == PseudoStates.Invisible)
                return;

            var container = root as VisualContainer;
            if (container != null) // container node
            {
                // update clip
                if (container.clipChildren)
                {
                    var worldBound = ComputeAAAlignedBound(root.layout, offset * root.globalTransform);
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
            }
            else
            {
                var offsetBounds = ComputeAAAlignedBound(root.globalBound, offset);
                if (!offsetBounds.Overlaps(currentGlobalClip))
                {
                    return;
                }
            }
            if (
                (root.panel.panelDebug == null || !root.panel.panelDebug.RecordRepaint(root)) &&
                root.usePixelCaching && allowPixelCaching && root.globalBound.size.magnitude > Mathf.Epsilon)
            {
                // now actually paint the texture to previous group
                IStylePainter painter = stylePainter;

                // validate cache texture size first
                var globalBound = root.globalBound;
                int w = (int)globalBound.width;
                int h = (int)globalBound.height;
                int textureWidth = (int)(globalBound.width * GUIUtility.pixelsPerPoint);
                int textureHeight = (int)(globalBound.height * GUIUtility.pixelsPerPoint);

                var cache = root.renderData.pixelCache;
                if (cache != null &&
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
                    offset = Matrix4x4.Translate(new Vector3(-globalBound.x, -globalBound.y, 0));

                    // reset clipping
                    var textureClip = new Rect(0, 0, w, h);
                    GUIClip.SetTransform(offset * root.globalTransform, textureClip);

                    // paint self
                    painter.currentWorldClip = currentGlobalClip;
                    root.DoRepaint(painter);
                    root.ClearDirty(ChangeType.Repaint);

                    if (container != null)
                    {
                        int count = container.childrenCount;
                        for (int i = 0; i < count; i++)
                        {
                            VisualElement child = container.GetChildAt(i);
                            PaintSubTree(e, child, offset, textureClip);

                            if (count != container.childrenCount)
                            {
                                throw new NotImplementedException("Visual tree is read-only during repaint");
                            }
                        }
                    }
                    RenderTexture.active = old;
                }

                // now actually paint the texture to previous group
                painter.currentWorldClip = currentGlobalClip;
                GUIClip.SetTransform(root.globalTransform, currentGlobalClip);

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
                GUIClip.SetTransform(offset * root.globalTransform, currentGlobalClip);

                stylePainter.currentWorldClip = currentGlobalClip;
                stylePainter.mousePosition = root.globalTransform.inverse.MultiplyPoint3x4(e.mousePosition);

                stylePainter.opacity = root.style.opacity.GetSpecifiedValueOrDefault(1.0f);
                root.DoRepaint(stylePainter);
                stylePainter.opacity = 1.0f;
                root.ClearDirty(ChangeType.Repaint);

                if (container != null)
                {
                    int count = container.childrenCount;
                    for (int i = 0; i < count; i++)
                    {
                        VisualElement child = container.GetChildAt(i);
                        PaintSubTree(e, child, offset, currentGlobalClip);

                        if (count != container.childrenCount)
                        {
                            throw new NotImplementedException("Visual tree is read-only during repaint");
                        }
                    }
                }
            }
        }

        public override void Repaint(Event e)
        {
            ValidateLayout();
            stylePainter.repaintEvent = e;

            // paint
            PaintSubTree(e, visualTree, Matrix4x4.identity, visualTree.layout);

            if (panelDebug != null)
            {
                GUIClip.Internal_Push(visualTree.layout, Vector2.zero, Vector2.zero, true);
                if (panelDebug.EndRepaint())
                    this.visualTree.Dirty(ChangeType.Repaint);
                GUIClip.Internal_Pop();
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
