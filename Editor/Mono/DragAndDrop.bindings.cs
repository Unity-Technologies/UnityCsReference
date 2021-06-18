// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Collections;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;

namespace UnityEditor
{
    // Visual indication mode for Drag & Drop operation.
    public enum DragAndDropVisualMode
    {
        None = 0,       // No indication (drag should not be performed).
        Copy = 1,       // Copy dragged objects.
        Link = 2,       // Link dragged objects to target.
        Move = 16,      // Move dragged objects.
        Generic = 4,    // Generic drag operation.
        Rejected = 32   // Rejected drag operation.
    }
    // Needs to be kept in sync with DragAndDropForwarding.h
    [Flags]
    public enum HierarchyDropFlags
    {
        None = 0,
        DropUpon = 1 << 0,
        DropBetween = 1 << 1,
        DropAfterParent = 1 << 2,
        SearchActive = 1 << 3,
        DropAbove = 1 << 4
    }

    public struct DragAndDropWindowTarget
    {
        public static readonly int projectBrowser = "ProjectBrowser".GetHashCode();
        public static readonly int sceneView = "SceneView".GetHashCode();
        public static readonly int hierarchy = "Hierarchy".GetHashCode();
        public static readonly int inspector = "Inspector".GetHashCode();
    }

    // Editor drag & drop operations.
    [NativeHeader("Editor/Platform/Interface/DragAndDrop.h"),
     StaticAccessor("GetDragAndDrop()", StaticAccessorType.Dot)]
    public class DragAndDrop
    {
        private static Hashtable s_GenericData;
        private static Dictionary<int, List<Delegate>> m_DropHandlers;

        internal static ProjectBrowserDropHandler DefaultProjectBrowserDropHandler = DefaultProjectBrowserDropHandlerImpl;
        internal static SceneDropHandler DefaultSceneDropHandler = InternalEditorUtility.SceneViewDrag;
        internal static InspectorDropHandler DefaultInspectorDropHandler = InternalEditorUtility.InspectorWindowDrag;
        internal static HierarchyDropHandler DefaultHierarchyDropHandler = InternalEditorUtility.HierarchyWindowDragByID;

        internal static void InitDropHandlers()
        {
            if (m_DropHandlers != null)
                return;
            m_DropHandlers = new Dictionary<int, List<Delegate>>();
            AddDropHandler(DefaultProjectBrowserDropHandler);
            AddDropHandler(DefaultSceneDropHandler);
            AddDropHandler(DefaultInspectorDropHandler);
            AddDropHandler(DefaultHierarchyDropHandler);
        }

        internal static void ClearDropHandlers()
        {
            m_DropHandlers?.Clear();
            m_DropHandlers = null;
        }

        internal static void AddDropHandler(int dropDstId, Delegate handler)
        {
            InitDropHandlers();
            if (HasHandler(dropDstId, handler))
            {
                throw new Exception("Delegate already registered for dropDestinationId:" + dropDstId);
            }

            if (!m_DropHandlers.TryGetValue(dropDstId, out var handlers))
            {
                handlers = new List<Delegate>();
                m_DropHandlers[dropDstId] = handlers;
            }
            handlers.Add(handler);
        }

        internal static void RemoveDropHandler(int dropDstId, Delegate handler)
        {
            InitDropHandlers();
            if (m_DropHandlers.TryGetValue(dropDstId, out var handlers))
            {
                handlers.RemoveAll(dropHandler => dropHandler == handler);
            }
        }

        internal static DragAndDropVisualMode Drop(int dropDstId, params object[] args)
        {
            InitDropHandlers();
            SavedGUIState guiState = SavedGUIState.Create();

            if (!m_DropHandlers.TryGetValue(dropDstId, out var handlers))
            {
                return DragAndDropVisualMode.Rejected;
            }

            var dropResult = DragAndDropVisualMode.None;
            for (var i = handlers.Count - 1; i >= 0; --i)
            {
                var dropHandler = handlers[i];
                dropResult = (DragAndDropVisualMode)dropHandler.DynamicInvoke(args);
                if (dropResult != DragAndDropVisualMode.None)
                {
                    break;
                }
            }

            guiState.ApplyAndForget();
            return dropResult;
        }

        internal static DragAndDropVisualMode DefaultProjectBrowserDropHandlerImpl(int dragUponInstanceId, string dummy, bool perform)
        {
            HierarchyProperty search = new HierarchyProperty(HierarchyType.Assets);
            if (search.Find(dragUponInstanceId, null))
                return InternalEditorUtility.ProjectWindowDrag(search, perform);

            if (dragUponInstanceId != 0)
            {
                var path = AssetDatabase.GetAssetPath(dragUponInstanceId);
                if (string.IsNullOrEmpty(path))
                    return DragAndDropVisualMode.Rejected;

                var packageInfo = PackageManager.PackageInfo.FindForAssetPath(path);
                if (packageInfo != null)
                {
                    search = new HierarchyProperty(packageInfo.assetPath);
                    if (search.Find(dragUponInstanceId, null))
                        return InternalEditorUtility.ProjectWindowDrag(search, perform);
                }
            }
            return InternalEditorUtility.ProjectWindowDrag(null, perform);
        }

        // HandleDelayedDrag can be used to start a drag and drop
        internal static bool HandleDelayedDrag(Rect position, int id, Object objectToDrag)
        {
            Event evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (position.Contains(evt.mousePosition) && evt.clickCount == 1)
                    {
                        if (evt.button == 0 && !(Application.platform == RuntimePlatform.OSXEditor && evt.control))
                        {
                            GUIUtility.hotControl = id;
                            DragAndDropDelay delay = (DragAndDropDelay)GUIUtility.GetStateObject(typeof(DragAndDropDelay), id);
                            delay.mouseDownPosition = evt.mousePosition;
                            return true;
                        }
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        DragAndDropDelay delay = (DragAndDropDelay)GUIUtility.GetStateObject(typeof(DragAndDropDelay), id);
                        if (delay.CanStartDrag())
                        {
                            GUIUtility.hotControl = 0;
                            PrepareStartDrag();
                            Object[] references = { objectToDrag };
                            objectReferences = references;
                            StartDrag(ObjectNames.GetDragAndDropTitle(objectToDrag));
                            return true;
                        }
                    }
                    break;
            }
            return false;
        }

        // Clears drag & drop data.
        public static void PrepareStartDrag()
        {
            if (Event.current != null)
            {
                switch (Event.current.type)
                {
                    case EventType.DragUpdated:
                    case EventType.DragPerform:
                    case EventType.DragExited:
                        Debug.LogError($"Calling \"{nameof(PrepareStartDrag)}\" when dragging is in progress is not supported.");
                        return;
                }
            }
            s_GenericData = null;
            paths = null;
            objectReferences = new UnityEngine.Object[] {};
            PrepareStartDrag_Internal();
        }

        // Start a drag operation.
        public static void StartDrag(string title)
        {
            if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag)
            {
                StartDrag_Internal(title);
            }
            else
            {
                Debug.LogError("Drags can only be started from MouseDown or MouseDrag events");
            }
        }

        [RequiredByNativeCode]
        private static bool HasGenericDragData()
        {
            return s_GenericData != null;
        }

        // Get data associated with current drag and drop operation.
        public static object GetGenericData(string type)
        {
            if (s_GenericData != null && s_GenericData.Contains(type))
                return s_GenericData[type];
            return null;
        }

        // Set data associated with current drag and drop operation.
        public static void SetGenericData(string type, object data)
        {
            if (s_GenericData == null)
                s_GenericData = new Hashtable();
            s_GenericData[type] = data;
        }

        public static extern Object[] objectReferences {[NativeMethod("GetPPtrs")] get; [NativeMethod("SetPPtrs")] set; }
        public static extern string[] paths { get; set; }
        public static extern int activeControlID { get; set; }
        public static extern DragAndDropVisualMode visualMode { get; set; }

        public static extern void AcceptDrag();
        [NativeMethod("PrepareStartDrag")] private static extern void PrepareStartDrag_Internal();
        [NativeMethod("StartDrag")] private static extern void StartDrag_Internal(string title);

        public delegate DragAndDropVisualMode ProjectBrowserDropHandler(int dragInstanceId, string dropUponPath, bool perform);
        public delegate DragAndDropVisualMode SceneDropHandler(UnityEngine.Object dropUpon, Vector3 worldPosition, Vector2 viewportPosition, Transform parentForDraggedObjects, bool perform);
        public delegate DragAndDropVisualMode InspectorDropHandler(UnityEngine.Object[] targets, bool perform);
        public delegate DragAndDropVisualMode HierarchyDropHandler(int dropTargetInstanceID, HierarchyDropFlags dropMode, Transform parentForDraggedObjects, bool perform);

        public static bool HasHandler(int dropDstId, Delegate handler)
        {
            List<Delegate> handlers = null;
            if (m_DropHandlers != null && !m_DropHandlers.TryGetValue(dropDstId, out handlers))
            {
                return false;
            }
            return handlers != null && handlers.Any(dropHandler => dropHandler == handler);
        }

        public static void AddDropHandler(ProjectBrowserDropHandler handler)
        {
            AddDropHandler(DragAndDropWindowTarget.projectBrowser, handler);
        }

        public static void AddDropHandler(SceneDropHandler handler)
        {
            AddDropHandler(DragAndDropWindowTarget.sceneView, handler);
        }

        public static void AddDropHandler(HierarchyDropHandler handler)
        {
            AddDropHandler(DragAndDropWindowTarget.hierarchy, handler);
        }

        public static void AddDropHandler(InspectorDropHandler handler)
        {
            AddDropHandler(DragAndDropWindowTarget.inspector, handler);
        }

        public static void RemoveDropHandler(ProjectBrowserDropHandler handler)
        {
            RemoveDropHandler(DragAndDropWindowTarget.projectBrowser, handler);
        }

        public static void RemoveDropHandler(SceneDropHandler handler)
        {
            RemoveDropHandler(DragAndDropWindowTarget.sceneView, handler);
        }

        public static void RemoveDropHandler(HierarchyDropHandler handler)
        {
            RemoveDropHandler(DragAndDropWindowTarget.hierarchy, handler);
        }

        public static void RemoveDropHandler(InspectorDropHandler handler)
        {
            RemoveDropHandler(DragAndDropWindowTarget.inspector, handler);
        }
    }
}
