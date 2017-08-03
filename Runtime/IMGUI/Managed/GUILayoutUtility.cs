// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using UnityEngineInternal;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // Utility functions for implementing and extending the GUILayout class.
    public partial class GUILayoutUtility
    {
        internal sealed class LayoutCache
        {
            //*undocumented*
            internal GUILayoutGroup topLevel = new GUILayoutGroup();
            //*undocumented*
            internal GenericStack layoutGroups = new GenericStack();
            //*undocumented*
            internal GUILayoutGroup windows = new GUILayoutGroup();

            //*undocumented*
            internal LayoutCache()
            {
                layoutGroups.Push(topLevel);
            }

            internal LayoutCache(LayoutCache other)
            {
                topLevel = other.topLevel;
                layoutGroups = other.layoutGroups;
                windows = other.windows;
            }
        }

        // TODO: Clean these up after a while
        static Dictionary<int, LayoutCache> s_StoredLayouts = new Dictionary<int, LayoutCache>();
        static Dictionary<int, LayoutCache> s_StoredWindows = new Dictionary<int, LayoutCache>();

        internal static LayoutCache current = new LayoutCache();

        internal static readonly Rect kDummyRect = new Rect(0, 0, 1, 1);

        static internal void CleanupRoots()
        {
            // See GUI.CleanupRoots
            s_SpaceStyle = null;
            s_StoredLayouts.Clear();
            s_StoredWindows.Clear();
            current = new LayoutCache();
        }

        static internal LayoutCache SelectIDList(int instanceID, bool isWindow)
        {
            Dictionary<int, LayoutCache> store = isWindow ? s_StoredWindows : s_StoredLayouts;
            LayoutCache cache;
            if (store.TryGetValue(instanceID, out cache) == false)
            {
                //          Debug.Log ("Creating ID " +instanceID + " " + Event.current.type);
                cache = new LayoutCache();
                store[instanceID] = cache;
            }
            else
            {
                //          Debug.Log ("reusing ID " +instanceID + " " + Event.current.type);
            }
            current.topLevel = cache.topLevel;
            current.layoutGroups = cache.layoutGroups;
            current.windows = cache.windows;
            return cache;
        }

        // Set up the internal GUILayouting
        // Called by the main GUI class automatically (from GUI.Begin)
        static internal void Begin(int instanceID)
        {
            LayoutCache cache = SelectIDList(instanceID, false);
            // Make a vertical group to encompass the whole thing
            if (Event.current.type == EventType.Layout)
            {
                current.topLevel = cache.topLevel = new GUILayoutGroup();
                current.layoutGroups.Clear();
                current.layoutGroups.Push(current.topLevel);
                current.windows = cache.windows = new GUILayoutGroup();
            }
            else
            {
                current.topLevel = cache.topLevel;
                current.layoutGroups = cache.layoutGroups;
                current.windows = cache.windows;
            }
        }

        static internal void BeginContainer(LayoutCache cache)
        {
            // Make a vertical group to encompass the whole thing
            if (Event.current.type == EventType.Layout)
            {
                current.topLevel = cache.topLevel = new GUILayoutGroup();
                current.layoutGroups.Clear();
                current.layoutGroups.Push(current.topLevel);
                current.windows = cache.windows = new GUILayoutGroup();
            }
            else
            {
                current.topLevel = cache.topLevel;
                current.layoutGroups = cache.layoutGroups;
                current.windows = cache.windows;
            }
        }

        static internal void BeginWindow(int windowID, GUIStyle style, GUILayoutOption[] options)
        {
            LayoutCache cache = SelectIDList(windowID, true);
            // Make a vertical group to encompass the whole thing
            if (Event.current.type == EventType.Layout)
            {
                current.topLevel = cache.topLevel = new GUILayoutGroup();
                current.topLevel.style = style;
                current.topLevel.windowID = windowID;
                if (options != null)
                    current.topLevel.ApplyOptions(options);
                current.layoutGroups.Clear();
                current.layoutGroups.Push(current.topLevel);
                current.windows = cache.windows = new GUILayoutGroup();
            }
            else
            {
                current.topLevel = cache.topLevel;
                current.layoutGroups = cache.layoutGroups;
                current.windows = cache.windows;
            }
        }

        // TODO: actually make these check...
        // *undocumented*
        [Obsolete("BeginGroup has no effect and will be removed", false)]
        static public void BeginGroup(string GroupName) {}
        // *undocumented*
        [Obsolete("EndGroup has no effect and will be removed", false)]
        static public void EndGroup(string groupName) {}


        static internal void Layout()
        {
            if (current.topLevel.windowID == -1)
            {
                // Normal GUILayout.whatever -outside beginArea calls.
                // Here we go over all entries and calculate their sizes
                current.topLevel.CalcWidth();
                current.topLevel.SetHorizontal(0, Mathf.Min(Screen.width / GUIUtility.pixelsPerPoint, current.topLevel.maxWidth));
                current.topLevel.CalcHeight();
                current.topLevel.SetVertical(0, Mathf.Min(Screen.height / GUIUtility.pixelsPerPoint, current.topLevel.maxHeight));

                LayoutFreeGroup(current.windows);
            }
            else
            {
                LayoutSingleGroup(current.topLevel);
                LayoutFreeGroup(current.windows);
            }
        }

        // Global fayout function. Called from EditorWindows (differs from game view in that they use the full window size and try to stretch GUI
        // *undocumented*
        static internal void LayoutFromEditorWindow()
        {
            current.topLevel.CalcWidth();
            current.topLevel.SetHorizontal(0, Screen.width / GUIUtility.pixelsPerPoint);
            current.topLevel.CalcHeight();
            current.topLevel.SetVertical(0, Screen.height / GUIUtility.pixelsPerPoint);

            // UNCOMMENT ME TO DEBUG THE EditorWindow ROOT LAYOUT RESULTS
            //          Debug.Log (current.topLevel);
            // Layout all beginarea parts...
            LayoutFreeGroup(current.windows);
        }

        static internal void LayoutFromContainer(float w, float h)
        {
            current.topLevel.CalcWidth();
            current.topLevel.SetHorizontal(0, w);
            current.topLevel.CalcHeight();
            current.topLevel.SetVertical(0, h);

            // UNCOMMENT ME TO DEBUG THE EditorWindow ROOT LAYOUT RESULTS
            //          Debug.Log (current.topLevel);
            // Layout all beginarea parts...
            LayoutFreeGroup(current.windows);
        }

        // Global layout function. Calculates all sizes of all windows etc & assigns.
        // After this call everything has a properly calculated size
        // Called by Unity automatically.
        // Is public so we can access it from editor inspectors, but not supported by public stuff
        // *undocumented*
        static internal float LayoutFromInspector(float width)
        {
            if (current.topLevel != null && current.topLevel.windowID == -1)
            {
                // Here we go over all entries and calculate their sizes
                current.topLevel.CalcWidth();
                current.topLevel.SetHorizontal(0, width);
                current.topLevel.CalcHeight();
                current.topLevel.SetVertical(0, Mathf.Min(Screen.height / GUIUtility.pixelsPerPoint, current.topLevel.maxHeight));
                // UNCOMMENT ME TO DEBUG THE INSPECTOR
                //          Debug.Log (current.topLevel);
                float height = current.topLevel.minHeight;
                // Layout all beginarea parts...
                // TODO: NOT SURE HOW THIS WORKS IN AN INSPECTOR
                LayoutFreeGroup(current.windows);
                return height;
            }
            else
            {
                if (current.topLevel != null)
                    LayoutSingleGroup(current.topLevel);
                return 0;
            }
        }

        static internal void LayoutFreeGroup(GUILayoutGroup toplevel)
        {
            foreach (GUILayoutGroup i in toplevel.entries)
            {
                LayoutSingleGroup(i);
            }
            toplevel.ResetCursor();
        }

        static void LayoutSingleGroup(GUILayoutGroup i)
        {
            if (!i.isWindow)
            {
                // CalcWidth knocks out minWidth with the calculated sizes from its children. Normally, this is fine, but since we're in a fixed-size area,
                // we want to maintain that
                float origMinWidth = i.minWidth;
                float origMaxWidth = i.maxWidth;

                // Figure out the group's min & maxWidth.
                i.CalcWidth();

                // Make it as wide as possible, but the Rect supplied takes precedence...
                i.SetHorizontal(i.rect.x, Mathf.Clamp(i.maxWidth, origMinWidth, origMaxWidth));

                // Do the same preservation for CalcHeight...
                float origMinHeight = i.minHeight;
                float origMaxHeight = i.maxHeight;

                i.CalcHeight();
                // Make it as high as possible, but the Rect supplied takes precedence...
                i.SetVertical(i.rect.y, Mathf.Clamp(i.maxHeight, origMinHeight, origMaxHeight));
            }
            else
            {
                // Figure out the group's min & maxWidth.
                i.CalcWidth();

                Rect winRect = Internal_GetWindowRect(i.windowID);

                // Make it as wide as possible, but the Rect supplied takes precedence...
                i.SetHorizontal(winRect.x, Mathf.Clamp(winRect.width, i.minWidth, i.maxWidth));

                i.CalcHeight();

                // Make it as high as possible, but the Rect supplied takes precedence...
                i.SetVertical(winRect.y, Mathf.Clamp(winRect.height, i.minHeight, i.maxHeight));

                // If GUILayout did any resizing, make sure the window reflects this.

                Internal_MoveWindow(i.windowID, i.rect);
            }
        }

        [SecuritySafeCritical]
        static GUILayoutGroup CreateGUILayoutGroupInstanceOfType(Type LayoutType)
        {
            if (!typeof(GUILayoutGroup).IsAssignableFrom(LayoutType))
                throw new ArgumentException("LayoutType needs to be of type GUILayoutGroup");
            return (GUILayoutGroup)Activator.CreateInstance(LayoutType);
        }

        // Generic helper - use this when creating a layoutgroup. It will make sure everything is wired up correctly.
        internal static GUILayoutGroup BeginLayoutGroup(GUIStyle style, GUILayoutOption[] options, Type layoutType)
        {
            GUILayoutGroup g;
            switch (Event.current.type)
            {
                case EventType.Used:
                case EventType.Layout:
                    g = CreateGUILayoutGroupInstanceOfType(layoutType);
                    g.style = style;
                    if (options != null)
                        g.ApplyOptions(options);
                    current.topLevel.Add(g);
                    break;
                default:
                    g = current.topLevel.GetNext() as GUILayoutGroup;
                    if (g == null)
                        throw new ArgumentException("GUILayout: Mismatched LayoutGroup." + Event.current.type);
                    g.ResetCursor();
                    GUIDebugger.LogLayoutGroupEntry(g.rect, g.margin, g.style, g.isVertical);
                    break;
            }
            current.layoutGroups.Push(g);
            current.topLevel = g;
            return g;
        }

        // The matching end for BeginLayoutGroup
        internal static void EndLayoutGroup()
        {
            if (Event.current.type != EventType.Layout && Event.current.type != EventType.Used)
                GUIDebugger.LogLayoutEndGroup();
            switch (Event.current.type)
            {
                default:
                    current.layoutGroups.Pop();
                    current.topLevel = (0 < current.layoutGroups.Count) ?
                        (GUILayoutGroup)current.layoutGroups.Peek() :
                        null;
                    return;
            }
        }

        // Generic helper - use this when creating a layoutgroup. It will make sure everything is wired up correctly.
        internal static GUILayoutGroup BeginLayoutArea(GUIStyle style, Type layoutType)
        {
            GUILayoutGroup g;
            switch (Event.current.type)
            {
                case EventType.Used:
                case EventType.Layout:
                    g = CreateGUILayoutGroupInstanceOfType(layoutType);
                    g.style = style;
                    current.windows.Add(g);
                    break;
                default:
                    g = current.windows.GetNext() as GUILayoutGroup;
                    if (g == null)
                        throw new ArgumentException("GUILayout: Mismatched LayoutGroup." + Event.current.type);
                    g.ResetCursor();
                    GUIDebugger.LogLayoutGroupEntry(g.rect, g.margin, g.style, g.isVertical);
                    break;
            }
            current.layoutGroups.Push(g);
            current.topLevel = g;
            return g;
        }

        // Trampoline for Editor stuff
        //*undocumented*
        internal static GUILayoutGroup DoBeginLayoutArea(GUIStyle style, Type layoutType)
        {
            return BeginLayoutArea(style, layoutType);
        }

        internal static GUILayoutGroup topLevel
        {
            get { return current.topLevel; }
        }

        /// *listonly*
        public static Rect GetRect(GUIContent content, GUIStyle style)                                 { return DoGetRect(content, style, null); }
        // Reserve layout space for a rectangle for displaying some contents with a specific style.
        public static Rect GetRect(GUIContent content, GUIStyle style, params GUILayoutOption[] options)       { return DoGetRect(content, style, options); }

        static Rect DoGetRect(GUIContent content, GUIStyle style, GUILayoutOption[] options)
        {
            GUIUtility.CheckOnGUI();

            switch (Event.current.type)
            {
                case EventType.Layout:
                    if (style.isHeightDependantOnWidth)
                    {
                        current.topLevel.Add(new GUIWordWrapSizer(style, content, options));
                    }
                    else
                    {
                        Vector2 sizeConstraints = new Vector2(0, 0);
                        if (options != null)
                        {
                            foreach (var option in options)
                            {
                                switch (option.type)
                                {
                                    case GUILayoutOption.Type.maxHeight:
                                        sizeConstraints.y = (float)option.value;
                                        break;
                                    case GUILayoutOption.Type.maxWidth:
                                        sizeConstraints.x = (float)option.value;
                                        break;
                                }
                            }
                        }

                        Vector2 size = style.CalcSizeWithConstraints(content, sizeConstraints);
                        current.topLevel.Add(new GUILayoutEntry(size.x, size.x, size.y, size.y, style, options));
                    }
                    return kDummyRect;

                case EventType.Used:
                    return kDummyRect;
                default:
                    var entry = current.topLevel.GetNext();
                    GUIDebugger.LogLayoutEntry(entry.rect, entry.margin, entry.style);
                    return entry.rect;
            }
        }

        /// *listonly*
        public static Rect GetRect(float width, float height)                                      { return DoGetRect(width, width, height, height, GUIStyle.none, null); }
        /// *listonly*
        public static Rect GetRect(float width, float height, GUIStyle style)                      {return DoGetRect(width, width, height, height, style, null); }
        /// *listonly*
        public static Rect GetRect(float width, float height, params GUILayoutOption[] options)    {return DoGetRect(width, width, height, height, GUIStyle.none, options); }
        // Reserve layout space for a rectangle with a fixed content area.
        public static Rect GetRect(float width, float height, GUIStyle style, params GUILayoutOption[] options)
        {return DoGetRect(width, width, height, height, style, options); }


        /// *listonly*
        public static Rect GetRect(float minWidth, float maxWidth, float minHeight, float maxHeight)
        { return DoGetRect(minWidth, maxWidth, minHeight, maxHeight, GUIStyle.none, null); }
        /// *listonly*
        public static Rect GetRect(float minWidth, float maxWidth, float minHeight, float maxHeight, GUIStyle style)
        { return DoGetRect(minWidth, maxWidth, minHeight, maxHeight, style, null); }
        /// *listonly*
        public static Rect GetRect(float minWidth, float maxWidth, float minHeight, float maxHeight, params GUILayoutOption[] options)
        { return DoGetRect(minWidth, maxWidth, minHeight, maxHeight, GUIStyle.none, options); }
        // Reserve layout space for a flexible rect.
        public static Rect GetRect(float minWidth, float maxWidth, float minHeight, float maxHeight, GUIStyle style, params GUILayoutOption[] options)
        { return DoGetRect(minWidth, maxWidth, minHeight, maxHeight, style, options); }
        static Rect DoGetRect(float minWidth, float maxWidth, float minHeight, float maxHeight, GUIStyle style, GUILayoutOption[] options)
        {
            switch (Event.current.type)
            {
                case EventType.Layout:
                    current.topLevel.Add(new GUILayoutEntry(minWidth, maxWidth, minHeight, maxHeight, style, options));
                    return kDummyRect;
                case EventType.Used:
                    return kDummyRect;
                default:
                    return current.topLevel.GetNext().rect;
            }
        }

        // Get the rectangle last used by GUILayout for a control.
        public static Rect GetLastRect()
        {
            switch (Event.current.type)
            {
                case EventType.Layout:
                case EventType.Used:
                    return kDummyRect;
                default:
                    return current.topLevel.GetLast();
            }
        }

        /// *listonly*
        public static Rect GetAspectRect(float aspect)                                         { return DoGetAspectRect(aspect, GUIStyle.none, null); }
        /// *listonly*
        public static Rect GetAspectRect(float aspect, GUIStyle style)                             { return DoGetAspectRect(aspect, style, null); }
        /// *listonly*
        public static Rect GetAspectRect(float aspect, params GUILayoutOption[] options)               {  return DoGetAspectRect(aspect, GUIStyle.none, options); }
        // Reserve layout space for a rectangle with a specific aspect ratio.
        public static Rect GetAspectRect(float aspect, GUIStyle style, params GUILayoutOption[] options)   {  return DoGetAspectRect(aspect, GUIStyle.none, options); }
        static Rect DoGetAspectRect(float aspect, GUIStyle style, GUILayoutOption[] options)
        {
            switch (Event.current.type)
            {
                case EventType.Layout:
                    current.topLevel.Add(new GUIAspectSizer(aspect, options));
                    return kDummyRect;
                case EventType.Used:
                    return kDummyRect;
                default:
                    return current.topLevel.GetNext().rect;
            }
        }

        // Style used by space elements so we can do special handling of spaces.
        internal static GUIStyle spaceStyle
        {
            get
            {
                if (s_SpaceStyle == null) s_SpaceStyle = new GUIStyle();
                s_SpaceStyle.stretchWidth = false;
                return s_SpaceStyle;
            }
        }
        static GUIStyle s_SpaceStyle;
    }
}
