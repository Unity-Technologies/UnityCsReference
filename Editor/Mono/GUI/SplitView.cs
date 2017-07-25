// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace UnityEditor
{
    /// orders all children along an axis. Resizing, splitting, etc..
    class SplitView : View, ICleanuppable, IDropArea
    {
        const float kRootDropZoneThickness = 70f;
        const float kRootDropZoneOffset = 50f;
        const float kRootDropDestinationThickness = 200f;
        const float kMaxViewDropZoneThickness = 300f;
        const float kMinViewDropDestinationThickness = 100f;

        public bool vertical = false;
        public int controlID = 0;

        [Flags] internal enum ViewEdge
        {
            None = 0,
            Left = 1 << 0,
            Bottom = 1 << 1,
            Top = 1 << 2,
            Right = 1 << 3,
            BottomLeft = Bottom | Left,
            BottomRight = Bottom | Right,
            TopLeft = Top | Left,
            TopRight = Top | Right,
            FitsVertical = Top | Bottom,
            FitsHorizontal = Left | Right,
            Before = Top | Left, // "Before" in SplitView children
            After = Bottom | Right // "After" in SplitView children
        }

        private Rect RectFromEdge(Rect rect, ViewEdge edge, float thickness, float offset)
        {
            switch (edge)
            {
                case ViewEdge.Left:
                    return new Rect(rect.x - offset, rect.y, thickness, rect.height);
                case ViewEdge.Right:
                    return new Rect(rect.xMax - thickness + offset, rect.y, thickness, rect.height);
                case ViewEdge.Top:
                    return new Rect(rect.x, rect.y - offset, rect.width, thickness);
                case ViewEdge.Bottom:
                    return new Rect(rect.x, rect.yMax - thickness + offset, rect.width, thickness);
                default:
                    throw new ArgumentException("Specify exactly one edge");
            }
        }

        // Extra info for dropping.
        internal class ExtraDropInfo
        {
            public bool rootWindow;
            public ViewEdge edge;
            public int index;
            public ExtraDropInfo(bool rootWindow, ViewEdge edge, int index)
            {
                this.rootWindow = rootWindow;
                this.edge = edge;
                this.index = index;
            }
        }

        SplitterState splitState = null;

        void SetupSplitter()
        {
            int[] actualSizes = new int[children.Length];
            int[] minSizes = new int[children.Length];

            for (int j = 0; j < children.Length; j++)
            {
                View c = (View)children[j];
                actualSizes[j] = vertical ? (int)c.position.height : (int)c.position.width;
                minSizes[j] = (int)(vertical ? c.minSize.y : c.minSize.x);
            }

            splitState = new SplitterState(actualSizes, minSizes, null);
            splitState.splitSize = 10;
        }

        void SetupRectsFromSplitter()
        {
            if (children.Length == 0)
                return;

            int cursor = 0;

            int total = 0;
            foreach (int size in splitState.realSizes)
            {
                total += size;
            }
            float scale = 1;
            if (total > (vertical ? position.height : position.width))
                scale = (vertical ? position.height : position.width) / total;

            // OSX webviews might trigger nested Repaint events when being resized
            // so we protected the GUI state at this level
            SavedGUIState state = SavedGUIState.Create();

            for (int i = 0; i < children.Length; i++)
            {
                int size = (int)Mathf.Round(splitState.realSizes[i] * scale);
                if (vertical)
                    children[i].position = new Rect(0, cursor, position.width, size);
                else
                    children[i].position = new Rect(cursor, 0, size, position.height);

                cursor += size;
            }

            state.ApplyAndForget();
        }

        // 2-part process: recalc children sizes bottomup, the reflow top-down
        static void RecalcMinMaxAndReflowAll(SplitView start)
        {
            // search upwards and find the topmost
            SplitView root = start, next = start;
            do
            {
                root = next;
                next = root.parent as SplitView;
            }
            while (next);

            RecalcMinMaxRecurse(root);
            ReflowRecurse(root);
        }

        static void RecalcMinMaxRecurse(SplitView node)
        {
            foreach (View i in node.children)
            {
                SplitView sv = i as SplitView;
                if (sv)
                    RecalcMinMaxRecurse(sv);
            }
            node.ChildrenMinMaxChanged();
        }

        static void ReflowRecurse(SplitView node)
        {
            node.Reflow();
            foreach (View i in node.children)
            {
                SplitView sv = i as SplitView;
                if (sv)
                    RecalcMinMaxRecurse(sv);
            }
        }

        internal override void Reflow()
        {
            SetupSplitter();

            for (int k = 0; k < children.Length - 1; k++)
                splitState.DoSplitter(k, k + 1, 0);
            splitState.RelativeToRealSizes(vertical ? (int)position.height : (int)position.width);
            SetupRectsFromSplitter();
        }

        void PlaceView(int i, float pos, float size)
        {
            float roundPos = Mathf.Round(pos);
            if (vertical)
                children[i].position = new Rect(0, roundPos, position.width, Mathf.Round(pos + size) - roundPos);
            else
                children[i].position = new Rect(roundPos, 0, Mathf.Round(pos + size) - roundPos, position.height);
        }

        public override void AddChild(View child, int idx)
        {
            base.AddChild(child, idx);
            ChildrenMinMaxChanged();
            splitState = null;
        }

        public void RemoveChildNice(View child)
        {
            if (children.Length != 1)
            {
                // Make neighbors to grow to take space
                int idx = IndexOfChild(child);
                float moveToPos = 0;
                if (idx == 0)
                    moveToPos = 0;
                else if (idx == children.Length - 1)
                    moveToPos = 1;
                else
                    moveToPos = .5f;

                moveToPos = vertical ?
                    Mathf.Lerp(child.position.yMin, child.position.yMax, moveToPos) :
                    Mathf.Lerp(child.position.xMin, child.position.xMax, moveToPos);


                if (idx > 0)
                {
                    View c = (View)children[idx - 1];
                    Rect r = c.position;
                    if (vertical)
                        r.yMax = moveToPos;
                    else
                        r.xMax = moveToPos;
                    c.position = r;

                    if (c is SplitView)
                        ((SplitView)c).Reflow();
                }

                if (idx < children.Length - 1)
                {
                    View c = (View)children[idx + 1];
                    Rect r = c.position;
                    if (vertical)
                        c.position = new Rect(r.x, moveToPos, r.width, r.yMax - moveToPos);
                    else
                        c.position = new Rect(moveToPos, r.y, r.xMax - moveToPos, r.height);

                    if (c is SplitView)
                        ((SplitView)c).Reflow();
                }
            }
            RemoveChild(child);
        }

        public override void RemoveChild(View child)
        {
            splitState = null;
            base.RemoveChild(child);
        }

        DropInfo RootViewDropZone(ViewEdge edge, Vector2 mousePos, Rect screenRect)
        {
            var offset = (edge & ViewEdge.FitsVertical) != 0 ? kRootDropZoneThickness : kRootDropZoneOffset;
            if (!RectFromEdge(screenRect, edge, kRootDropZoneThickness, offset).Contains(mousePos))
                return null;

            var dropInfo = new DropInfo(this);
            dropInfo.type = DropInfo.Type.Pane;
            dropInfo.userData = new ExtraDropInfo(true, edge, 0);
            dropInfo.rect = RectFromEdge(screenRect, edge, kRootDropDestinationThickness, 0f);
            return dropInfo;
        }

        public DropInfo DragOverRootView(Vector2 mouseScreenPosition)
        {
            if (children.Length == 1 && DockArea.s_IgnoreDockingForView == children[0])
            {
                return null; // Prevent dragging the view from a single-view window into itself
            }
            return RootViewDropZone(ViewEdge.Bottom, mouseScreenPosition, screenPosition)
                ?? RootViewDropZone(ViewEdge.Top, mouseScreenPosition, screenPosition)
                ?? RootViewDropZone(ViewEdge.Left, mouseScreenPosition, screenPosition)
                ?? RootViewDropZone(ViewEdge.Right, mouseScreenPosition, screenPosition);
        }

        public DropInfo DragOver(EditorWindow w, Vector2 mouseScreenPosition)
        {
            for (var childIndex = 0; childIndex < children.Length; ++childIndex)
            {
                var child = children[childIndex];

                // skip so you can't dock a view to a subview of itself
                if (child == DockArea.s_IgnoreDockingForView)
                    continue;

                // Skip if child is a splitview (it'll handle its rect itself)
                if (child is SplitView)
                    continue;

                // Collect flags of which edge zones the mouse is inside
                var mouseEdges = ViewEdge.None;
                var childRect = child.screenPosition;
                var childRectWithoutDock = RectFromEdge(childRect, ViewEdge.Bottom, childRect.height - DockArea.kDockHeight, 0f);
                var borderWidth = Mathf.Min(Mathf.Round(childRectWithoutDock.width / 3), kMaxViewDropZoneThickness);
                var borderHeight = Mathf.Min(Mathf.Round(childRectWithoutDock.height / 3), kMaxViewDropZoneThickness);
                var leftDropZone = RectFromEdge(childRectWithoutDock, ViewEdge.Left, borderWidth, 0f);
                var rightDropZone = RectFromEdge(childRectWithoutDock, ViewEdge.Right, borderWidth, 0f);
                var bottomDropZone = RectFromEdge(childRectWithoutDock, ViewEdge.Bottom, borderHeight, 0f);
                var topDropZone = RectFromEdge(childRectWithoutDock, ViewEdge.Top, borderHeight, 0f);

                if (leftDropZone.Contains(mouseScreenPosition))
                    mouseEdges |= ViewEdge.Left;

                if (rightDropZone.Contains(mouseScreenPosition))
                    mouseEdges |= ViewEdge.Right;

                if (bottomDropZone.Contains(mouseScreenPosition))
                    mouseEdges |= ViewEdge.Bottom;

                if (topDropZone.Contains(mouseScreenPosition))
                    mouseEdges |= ViewEdge.Top;

                // If mouse is in more than one zone, it is in a corner. Find the corner and divide it diagonally...
                var mouseToCorner = Vector2.zero;
                var oppositeToCorner = Vector2.zero;
                var ccwEdge = mouseEdges;
                var cwEdge = mouseEdges;
                switch (mouseEdges)
                {
                    case ViewEdge.BottomLeft:
                        ccwEdge = ViewEdge.Bottom;
                        cwEdge = ViewEdge.Left;
                        mouseToCorner = new Vector2(childRectWithoutDock.x, childRectWithoutDock.yMax) - mouseScreenPosition;
                        oppositeToCorner = new Vector2(-borderWidth, borderHeight);
                        break;
                    case ViewEdge.BottomRight:
                        ccwEdge = ViewEdge.Right;
                        cwEdge = ViewEdge.Bottom;
                        mouseToCorner = new Vector2(childRectWithoutDock.xMax, childRectWithoutDock.yMax) - mouseScreenPosition;
                        oppositeToCorner = new Vector2(borderWidth, borderHeight);
                        break;
                    case ViewEdge.TopLeft:
                        ccwEdge = ViewEdge.Left;
                        cwEdge = ViewEdge.Top;
                        mouseToCorner = new Vector2(childRectWithoutDock.x, childRectWithoutDock.y) - mouseScreenPosition;
                        oppositeToCorner = new Vector2(-borderWidth, -borderHeight);
                        break;
                    case ViewEdge.TopRight:
                        ccwEdge = ViewEdge.Top;
                        cwEdge = ViewEdge.Right;
                        mouseToCorner = new Vector2(childRectWithoutDock.xMax, childRectWithoutDock.y) - mouseScreenPosition;
                        oppositeToCorner = new Vector2(borderWidth, -borderHeight);
                        break;
                }
                // ...then choose the edge based on the half the mouse is in
                mouseEdges = mouseToCorner.x * oppositeToCorner.y - mouseToCorner.y * oppositeToCorner.x < 0 ? ccwEdge : cwEdge;

                if (mouseEdges != ViewEdge.None) // Valid drop zone
                {
                    var targetThickness = Mathf.Round(((mouseEdges & ViewEdge.FitsHorizontal) != 0 ? childRect.width : childRect.height) / 3);
                    targetThickness = Mathf.Max(targetThickness, kMinViewDropDestinationThickness);

                    var dropInfo = new DropInfo(this);
                    dropInfo.userData = new ExtraDropInfo(false, mouseEdges, childIndex);
                    dropInfo.type = DropInfo.Type.Pane;
                    dropInfo.rect = RectFromEdge(childRect, mouseEdges, targetThickness, 0f);
                    return dropInfo;
                }
            }
            // Claim the drag if we are the root split view, so it doesn't fall through to obscured windows
            if (screenPosition.Contains(mouseScreenPosition) && !(parent is SplitView))
            {
                return new DropInfo(null);
            }

            return null;
        }

        /// Notification so other views can respond to this.
        protected override void ChildrenMinMaxChanged()
        {
            Vector2 min = Vector2.zero, max = Vector2.zero;
            if (vertical)
            {
                foreach (View child in children)
                {
                    min.x = Mathf.Max(child.minSize.x, min.x);
                    max.x = Mathf.Max(child.maxSize.x, max.x);
                    min.y += child.minSize.y;
                    max.y += child.maxSize.y;
                }
            }
            else
            {
                foreach (View child in children)
                {
                    min.x += child.minSize.x;
                    max.x += child.maxSize.x;
                    min.y = Mathf.Max(child.minSize.y, min.y);
                    max.y = Mathf.Max(child.maxSize.y, max.y);
                }
            }
            splitState = null;

            SetMinMaxSizes(min, max);
        }

        public override string ToString()
        {
            return vertical ? "SplitView (vert)" : "SplitView (horiz)";
        }

        public bool PerformDrop(EditorWindow dropWindow, DropInfo dropInfo, Vector2 screenPos)
        {
            var extraInfo = dropInfo.userData as ExtraDropInfo;
            var rootWindow = extraInfo.rootWindow;
            var edge = extraInfo.edge;
            var dropIndex = extraInfo.index;
            var dropRect = dropInfo.rect;
            var beginning = (edge & ViewEdge.Before) != 0;
            var wantsVertical = (edge & ViewEdge.FitsVertical) != 0;
            SplitView parentForDrop = null;
            if (vertical == wantsVertical || children.Length < 2)
            { // Current view can accommodate desired drop
                if (!beginning)
                {
                    if (rootWindow)
                        dropIndex = children.Length;
                    else
                        ++dropIndex;
                }
                parentForDrop = this;
            }
            else if (rootWindow)
            { // Docking to a window: need to insert a parent
                var newParent = ScriptableObject.CreateInstance<SplitView>();
                newParent.position = position;
                if (window.rootView == this)
                    window.rootView = newParent;
                else // Main window has MainView as its root
                    parent.AddChild(newParent, parent.IndexOfChild(this));
                newParent.AddChild(this);
                position = new Rect(Vector2.zero, position.size);

                dropIndex = beginning ? 0 : 1;
                parentForDrop = newParent;
            }
            else
            { // Docking in a view: need to insert a child
                var newChild = ScriptableObject.CreateInstance<SplitView>();

                newChild.AddChild(children[dropIndex]);
                AddChild(newChild, dropIndex);
                newChild.position = newChild.children[0].position;
                newChild.children[0].position = new Rect(Vector2.zero, newChild.position.size);

                dropIndex = beginning ? 0 : 1;
                parentForDrop = newChild;
            }
            dropRect.position = dropRect.position - screenPosition.position;
            var newDockArea = ScriptableObject.CreateInstance<DockArea>();
            parentForDrop.vertical = wantsVertical;
            parentForDrop.MakeRoomForRect(dropRect);
            parentForDrop.AddChild(newDockArea, dropIndex);
            newDockArea.position = dropRect;
            DockArea.s_OriginalDragSource.RemoveTab(dropWindow);
            dropWindow.m_Parent = newDockArea;
            newDockArea.AddTab(dropWindow);
            Reflow();
            RecalcMinMaxAndReflowAll(this);
            newDockArea.MakeVistaDWMHappyDance();
            return true;
        }

        static string PosVals(float[] posVals)
        {
            string s = "[";
            foreach (float p in posVals)
                s += "" + p + ", ";
            s += "]";
            return (s);
        }

        void MakeRoomForRect(Rect r)
        {
            Rect[] sources = new Rect[children.Length];
            for (int i = 0; i < sources.Length; i++)
                sources[i] = children[i].position;

            CalcRoomForRect(sources, r);
            //      string s = "Making room for " + r +" \nchildren {\n";
            for (int i = 0; i < sources.Length; i++)
            {
                //          s += "    " + sources[i] + " \n";
                children[i].position = sources[i];
            }
            //      s+= "}";
            //      Debug.Log (s);
        }

        void CalcRoomForRect(Rect[] sources, Rect r)
        {
            float start = vertical ? r.y : r.x;
            float end = start + (vertical ? r.height : r.width);
            float mid = (start + end) * .5f;

            // Find out where we should split
            int splitPos;
            for (splitPos = 0; splitPos < sources.Length; splitPos++)
            {
                float midPos = vertical ?
                    (sources[splitPos].y + sources[splitPos].height * .5f) :
                    (sources[splitPos].x + sources[splitPos].width * .5f);
                if (midPos > mid)
                    break;
            }

            float p2 = start;
            for (int i = splitPos - 1; i >= 0; i--)
            {
                if (vertical)
                {
                    sources[i].yMax = p2;
                    if (sources[i].height < children[i].minSize.y)
                        p2 = sources[i].yMin = sources[i].yMax - children[i].minSize.y;
                    else
                        break;
                }
                else
                {
                    sources[i].xMax = p2;
                    if (sources[i].width < children[i].minSize.x)
                        p2 = sources[i].xMin = sources[i].xMax - children[i].minSize.x;
                    else
                        break;
                }
            }
            // if we're below zero, move everything forward
            if (p2 < 0)
            {
                float delta = -p2;
                for (int i = 0; i < splitPos - 1; i++)
                {
                    if (vertical)
                        sources[i].y += delta;
                    else
                        sources[i].x += delta;
                }
                end += delta;
            }

            p2 = end;
            for (int i = splitPos; i < sources.Length; i++)
            {
                if (vertical)
                {
                    float tmp = sources[i].yMax;
                    sources[i].yMin = p2;
                    sources[i].yMax = tmp;
                    if (sources[i].height < children[i].minSize.y)
                        p2 = sources[i].yMax = sources[i].yMin + children[i].minSize.y;
                    else
                        break;
                }
                else
                {
                    float tmp = sources[i].xMax;
                    sources[i].xMin = p2;
                    sources[i].xMax = tmp;
                    if (sources[i].width < children[i].minSize.x)
                        p2 = sources[i].xMax = sources[i].xMin + children[i].minSize.x;
                    else
                        break;
                }
            }
            // if we're above max, move everything forward
            float limit = vertical ? position.height : position.width;
            if (p2 > limit)
            {
                float delta = limit - p2;
                for (int i = 0; i < splitPos - 1; i++)
                {
                    if (vertical)
                        sources[i].y += delta;
                    else
                        sources[i].x += delta;
                }
                end += delta;
            }
        }

        /// clean up this view & propagate down
        public void Cleanup()
        {
            // if I'm a one-view splitview, I can propagate my child up and kill myself

            SplitView sp = parent as SplitView;

            if (children.Length == 1 && sp != null)
            {
                View c = children[0];
                c.position = position;
                if (parent != null)
                {
                    parent.AddChild(c, parent.IndexOfChild(this));
                    parent.RemoveChild(this);
                    if (sp)
                        sp.Cleanup();
                    c.position = position;
                    if (!Unsupported.IsDestroyScriptableObject(this))
                        DestroyImmediate(this);
                    return;
                }
                else if (c is SplitView)
                {
                    RemoveChild(c);
                    window.rootView = c;
                    c.position = new Rect(0, 0, c.window.position.width, window.position.height);
                    c.Reflow();
                    if (!Unsupported.IsDestroyScriptableObject(this))
                        DestroyImmediate(this);
                    return;
                }
            }

            if (sp)
            {
                sp.Cleanup();
                // the parent might have moved US up and gotten rid of itself
                sp = parent as SplitView;
                if (sp)
                {
                    // If the parent has the same orientation as us, we can move our views up and kill ourselves
                    if (sp.vertical == vertical)
                    {
                        int idx = new List<View>(parent.children).IndexOf(this);
                        foreach (View child in children)
                        {
                            sp.AddChild(child, idx++);
                            child.position = new Rect(position.x + child.position.x, position.y + child.position.y, child.position.width, child.position.height);
                        }
                    }
                }
            }
            if (children.Length == 0)
            {
                if (parent == null && window != null)
                {
                    // if we're root in the window, we'll remove ourselves
                    window.Close();
                }
                else
                {
                    ICleanuppable ic = parent as ICleanuppable;
                    if (parent is SplitView)
                    {
                        ((SplitView)parent).RemoveChildNice(this);
                        if (!Unsupported.IsDestroyScriptableObject(this))
                            DestroyImmediate(this, true);
                    }
                    else
                    {
                        // This is we're root in the main window.
                        // We want to stay, but tell the parent (MainWindow) to Cleanup, so he can reduce us to zero-size
                        /*                  parent.RemoveChild (this);*/
                    }
                    ic.Cleanup();
                }
                return;
            }
            else
            {
                splitState = null;
                Reflow();
            }
        }

        internal const float kGrabDist = 5;
        public void SplitGUI(Event evt)
        {
            if (splitState == null)
                SetupSplitter();

            SplitView sp = parent as SplitView;
            if (sp)
            {
                Event e = new Event(evt);
                e.mousePosition += new Vector2(position.x, position.y);
                sp.SplitGUI(e);
                if (e.type == EventType.Used)
                    evt.Use();
            }

            float pos = vertical ? evt.mousePosition.y : evt.mousePosition.x;
            int id = GUIUtility.GetControlID(546739, FocusType.Passive);
            controlID = id;

            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (children.Length != 1) // is there a splitter
                    {
                        int cursor = vertical ? (int)children[0].position.y : (int)children[0].position.x;

                        for (int i = 0; i < children.Length - 1; i++)
                        {
                            if (i >= splitState.realSizes.Length)
                            {
                                DockArea dock = GUIView.current as DockArea;
                                string name = "Non-dock area " + GUIView.current.GetType();
                                if (dock && dock.m_Selected < dock.m_Panes.Count && dock.m_Panes[dock.m_Selected])
                                    name = dock.m_Panes[dock.m_Selected].GetType().ToString();

                                if (Unsupported.IsDeveloperBuild())
                                    Debug.LogError("Real sizes out of bounds for: " + name + " index: " + i + " RealSizes: " + splitState.realSizes.Length);

                                SetupSplitter();
                            }
                            Rect splitterRect = vertical ?
                                new Rect(children[0].position.x, cursor + splitState.realSizes[i] - splitState.splitSize / 2, children[0].position.width, splitState.splitSize) :
                                new Rect(cursor + splitState.realSizes[i] - splitState.splitSize / 2, children[0].position.y, splitState.splitSize, children[0].position.height);

                            if (splitterRect.Contains(evt.mousePosition))
                            {
                                splitState.splitterInitialOffset = (int)pos;
                                splitState.currentActiveSplitter = i;
                                GUIUtility.hotControl = id;
                                evt.Use();
                                break;
                            }

                            cursor += splitState.realSizes[i];
                        }
                    }
                    break;
                case EventType.MouseDrag:
                    if (children.Length > 1 && (GUIUtility.hotControl == id) && (splitState.currentActiveSplitter >= 0))
                    {
                        int diff = (int)pos - splitState.splitterInitialOffset;

                        if (diff != 0)
                        {
                            splitState.splitterInitialOffset = (int)pos;
                            splitState.DoSplitter(splitState.currentActiveSplitter, splitState.currentActiveSplitter + 1, diff);
                        }

                        SetupRectsFromSplitter();


                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                        GUIUtility.hotControl = 0;
                    break;
            }
        }

        protected override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            Reflow();
        }
    }
} // namespace
