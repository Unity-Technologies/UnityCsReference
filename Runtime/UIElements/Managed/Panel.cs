// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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
        // changes to styles, colors and other render properties
        Styles = 1 << 3,
        // transforms are invalid
        Transform = 1 << 2,
        // styles may have changed for children of this node
        StylesPath = 1 << 1,
        // pixels in the target have been changed, just repaint, only makes sense on the Panel
        Repaint = 1 << 0
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
    }

    interface IVisualElementPanel : IPanel
    {
        IStylePainter stylePainter { get; }
        int instanceID { get; }
        LoadResourceFunction loadResourceFunc { get; }
        int IMGUIContainersCount { get; set; }

        void Dirty(ChangeType type);
        bool IsDirty(ChangeType type);
        void ClearDirty(ChangeType type);

        void Repaint(Event e);
        void ValidateLayout();
    }

    // Strategy to load assets must be provided in the context of Editor or Runtime
    internal delegate Object LoadResourceFunction(string pathName, System.Type type);

    // Default panel implementation
    internal class Panel : VisualContainer, IVisualElementPanel
    {
        private StyleSheets.StyleContext m_StyleContext;

        public VisualContainer visualTree
        {
            get { return this; }
        }

        public VisualContainer defaultIMRoot { get; set; }

        public IDispatcher dispatcher { get; set; }

        public IDataWatchService dataWatch { get; set; }

        IStylePainter m_StylePainter;
        public IStylePainter stylePainter
        {
            get
            {
                if (m_StylePainter == null)
                {
                    m_StylePainter = new StylePainter();
                }
                return m_StylePainter;
            }
            set
            {
                m_StylePainter = value;
            }
        }

        TimerEventScheduler m_Scheduler;

        public TimerEventScheduler timerEventScheduler
        {
            get { return m_Scheduler ?? (m_Scheduler = new TimerEventScheduler()); }
        }

        public IScheduler scheduler
        {
            get { return timerEventScheduler; }
        }

        internal StyleContext styleContext
        {
            get { return m_StyleContext; }
        }

        public int instanceID { get; set; }

        public bool allowPixelCaching { get; set; }

        public ContextType contextType { get; private set; }

        public LoadResourceFunction loadResourceFunc { get; private set; }

        public int IMGUIContainersCount { get; set; }

        public Panel(int instanceID, ContextType contextType, GUISkin legacyGUISkin, LoadResourceFunction loadResourceDelegate = null)
        {
            this.instanceID = instanceID;
            this.contextType = contextType;
            this.loadResourceFunc = loadResourceDelegate ?? Resources.Load;

            name = VisualElementUtils.GetUniqueName("PanelContainer");
            visualTree.ChangePanel(this);
            m_StyleContext = new StyleSheets.StyleContext(this, legacyGUISkin);
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

        VisualElement Pick(VisualElement root, Vector2 point)
        {
            // do not pick invisible
            if ((root.paintFlags & PaintFlags.Invisible) == PaintFlags.Invisible)
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

                // reset ref to upper left
                localPoint = localPoint - new Vector3(container.position.position.x, container.position.position.y, 0);

                for (int i = container.childrenCount - 1; i >= 0; i--)
                {
                    var child = container.GetChildAt(i);
                    // Depth first in reverse order, do children
                    var result = Pick(child, localPoint);
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

        public VisualElement Pick(Vector2 point)
        {
            ValidateLayout();

            return Pick(visualTree, point);
        }

        void ValidateStyling()
        {
            if (IsDirty(ChangeType.Styles | ChangeType.StylesPath))
            {
                m_StyleContext.ApplyStyles();
            }
        }

        public void ValidateLayout()
        {
            ValidateStyling();

            // update flex once
            if (visualTree.cssNode.IsDirty)
            {
                visualTree.cssNode.CalculateLayout();
                ValidateSubTree(visualTree);
            }
        }

        void ValidateSubTree(VisualElement root)
        {
            // if the last layout is different than this one we must dirty transform on children
            if (root.renderData.lastLayout != new Rect(root.cssNode.LayoutX, root.cssNode.LayoutY, root.cssNode.LayoutWidth, root.cssNode.LayoutHeight))
            {
                root.Dirty(ChangeType.Transform);
                root.renderData.lastLayout = new Rect(root.cssNode.LayoutX, root.cssNode.LayoutY, root.cssNode.LayoutWidth, root.cssNode.LayoutHeight);
                // and if we do, no need to go and visit individually again, the change will bubble all the way down
                return;
            }

            // ignore clean sub trees
            if (root.cssNode.HasNewLayout)
            {
                var container = root as VisualContainer;

                if (container != null)
                {
                    foreach (var child in container)
                    {
                        ValidateSubTree(child);
                    }
                }
            }
            // reset both flags at the end
            root.cssNode.MarkLayoutSeen();
        }

        // get the AA aligned bound
        public Rect ComputeAAAlignedBound(Rect position, Matrix4x4 transform)
        {
            var min = transform.MultiplyPoint3x4(position.min);
            var max = transform.MultiplyPoint3x4(position.max);
            return Rect.MinMaxRect(Math.Min(min.x, max.x), Math.Min(min.y, max.y), Math.Max(min.x, max.x), Math.Max(min.y, max.y));
        }

        public void PaintSubTree(Event e, VisualElement root, Matrix4x4 offset, Rect currentClip, Matrix4x4 clipTransform, Rect currentGlobalClip)
        {
            if ((root.paintFlags & PaintFlags.Invisible) == PaintFlags.Invisible)
                return;

            var container = root as VisualContainer;
            if (container != null) // container node
            {
                // update clip
                if (container.clipChildren)
                {
                    var worldBound = ComputeAAAlignedBound(root.position, root.globalTransform);
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

                    clipTransform = root.globalTransform;
                    // back to local
                    var inv = clipTransform.inverse;
                    var min = inv.MultiplyPoint3x4(currentGlobalClip.min);
                    var max = inv.MultiplyPoint3x4(currentGlobalClip.max);
                    currentClip = Rect.MinMaxRect(Math.Min(min.x, max.x), Math.Min(min.y, max.y), Math.Max(min.x, max.x),
                            Math.Max(min.y, max.y));
                }
            }
            else
            {
                if (!root.globalBound.Overlaps(currentGlobalClip))
                {
                    return;
                }
            }

            if (root.usePixelCaching && allowPixelCaching)
            {
                // now actually paint the texture to previous group
                IStylePainter painter = stylePainter;
                painter.currentWorldClip = currentGlobalClip;

                // validate cache texture size first
                var globalBound = root.globalBound;
                int w = (int)globalBound.width;
                int h = (int)globalBound.height;
                int textureWidth = (int)(globalBound.width * GUIUtility.pixelsPerPoint);
                int textureHeight = (int)(globalBound.height * GUIUtility.pixelsPerPoint);

                var cache = root.renderData.pixelCache;
                if (cache != null &&
                    (cache.width != textureWidth && cache.height != textureHeight))
                {
                    Object.DestroyImmediate(cache);
                    root.renderData.pixelCache = null;
                }

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
                    var textureClip = new Rect(globalBound.x, globalBound.y, w, h);
                    GUIClip.SetTransform(offset * Matrix4x4.identity, offset * root.globalTransform, textureClip);
                    // paint self

                    painter.currentWorldClip = textureClip;
                    root.DoRepaint(painter);
                    root.ClearDirty(ChangeType.Repaint);

                    if (container != null)
                    {
                        foreach (var child in container)
                        {
                            // and children
                            PaintSubTree(e, child, offset, textureClip, offset, textureClip);
                        }
                    }
                    RenderTexture.active = old;
                }

                // now actually paint the texture to previous group
                painter.currentWorldClip = currentGlobalClip;

                GUIClip.SetTransform(clipTransform, root.globalTransform, currentClip);
                painter.DrawTexture(root.position, root.renderData.pixelCache, Color.white);
            }
            else
            {
                GUIClip.SetTransform(offset * clipTransform, offset * root.globalTransform, currentClip);

                IStylePainter painter = stylePainter;
                painter.currentWorldClip = currentGlobalClip;
                painter.mousePosition = root.globalTransform.inverse.MultiplyPoint3x4(e.mousePosition);

                root.DoRepaint(painter);
                root.ClearDirty(ChangeType.Repaint);

                if (container == null)
                    return;

                foreach (var child in container)
                {
                    PaintSubTree(e, child, offset, currentClip, clipTransform, currentGlobalClip);
                }
            }
        }

        public void Repaint(Event e)
        {
            ValidateLayout();

            GUIClip.Internal_Push(visualTree.position, Vector2.zero, Vector2.zero, true);
            // paint
            PaintSubTree(e, visualTree, Matrix4x4.identity, visualTree.position, Matrix4x4.identity, visualTree.position);

            GUIClip.Internal_Pop();
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
