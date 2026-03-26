// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Scripting.LifecycleManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

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
        [AutoStaticsCleanupOnCodeReload]
        private static Hashtable s_GenericData;
        [AutoStaticsCleanupOnCodeReload]
        private static Dictionary<int, List<Delegate>> m_DropHandlers;
        [AutoStaticsCleanupOnCodeReload]
        internal static ProjectBrowserDropHandlerV2 DefaultProjectBrowserDropHandler = DefaultProjectBrowserDropHandlerImpl;
        [AutoStaticsCleanupOnCodeReload]
        internal static InspectorDropHandler DefaultInspectorDropHandler = InternalEditorUtility.InspectorWindowDrag;
        [AutoStaticsCleanupOnCodeReload]
        internal static HierarchyDropHandlerV2 DefaultHierarchyDropHandler = InternalEditorUtility.HierarchyWindowDragByID;

        internal static Dictionary<int, List<Delegate>> dropHandlers
        {
            get
            {
                if (m_DropHandlers == null)
                {
                    m_DropHandlers = new Dictionary<int, List<Delegate>>();
                    AddDropHandlerV2(DefaultProjectBrowserDropHandler);
                    AddDropHandlerV2(DefaultInspectorDropHandler);
                    AddDropHandlerV2(DefaultHierarchyDropHandler);
                }
                return m_DropHandlers;
            }
        }

        internal static void ClearDropHandlers()
        {
            m_DropHandlers?.Clear();
            m_DropHandlers = null;
        }

        internal static void AddDropHandler(int dropDstId, Delegate handler)
        {
            if (HasHandler(dropDstId, handler))
            {
                throw new Exception("Delegate already registered for dropDestinationId:" + dropDstId);
            }

            if (!dropHandlers.TryGetValue(dropDstId, out var handlers))
            {
                handlers = new List<Delegate>();
                dropHandlers[dropDstId] = handlers;
            }
            handlers.Add(handler);
        }

        internal static void RemoveDropHandler(int dropDstId, Delegate handler)
        {
            if (dropHandlers.TryGetValue(dropDstId, out var handlers))
            {
                handlers.RemoveAll(dropHandler => dropHandler == handler);
            }
        }

        internal static DragAndDropVisualMode DropOnProjectBrowserWindow(EntityId dragUponInstanceId, string dropUponPath, bool perform)
        {
            return Drop(DragAndDropWindowTarget.projectBrowser, dragUponInstanceId, dropUponPath, perform);
        }

        internal static DragAndDropVisualMode DropOnSceneWindow(UnityEngine.Object dropUpon, Vector3 worldPosition, Vector2 viewportPosition, Transform parentForDraggedObjects, bool perform)
        {
            return Drop(DragAndDropWindowTarget.sceneView, dropUpon, worldPosition, viewportPosition, parentForDraggedObjects, perform);
        }

        internal static DragAndDropVisualMode DropOnInspectorWindow(UnityEngine.Object[] targets, bool perform)
        {
            return Drop(DragAndDropWindowTarget.inspector, targets, perform);
        }

        internal static DragAndDropVisualMode DropOnHierarchyWindow(EntityId dropTargetEntityId, HierarchyDropFlags dropMode, Transform parentForDraggedObjects, bool perform)
        {
            return Drop(DragAndDropWindowTarget.hierarchy, dropTargetEntityId, dropMode, parentForDraggedObjects, perform);
        }

        internal static DragAndDropVisualMode Drop(int dropDstId, params object[] args)
        {
            SavedGUIState guiState = SavedGUIState.Create();

            if (!dropHandlers.TryGetValue(dropDstId, out var handlers))
            {
                return DragAndDropVisualMode.Rejected;
            }

            var dropResult = DragAndDropVisualMode.None;
            for (var i = handlers.Count - 1; i >= 0; --i)
            {
                var dropHandler = handlers[i];
                if (dropHandler.Method.GetParameters()[0].ParameterType == typeof(int) && args[0] is EntityId e)
                {
                    Debug.LogWarning("Using int based drop handlers is deprecated. Please use EntityId based ones instead.");
                    Debug.Assert(sizeof(ulong)==UnsafeUtility.SizeOf<EntityId>(), "EntityId is not the same size as int, update this code to use ulong");
                    args[0] = EntityId.ToULong(e);
                }
                if (dropHandler.Method.GetParameters()[0].ParameterType == typeof(EntityId) && args[0] is int number)
                    args[0] = EntityId.FromULong((ulong)number);
                dropResult = (DragAndDropVisualMode)dropHandler.DynamicInvoke(args);
                if (dropResult != DragAndDropVisualMode.None)
                {
                    break;
                }
            }

            guiState.ApplyAndForget();
            return dropResult;
        }

        internal static DragAndDropVisualMode DefaultProjectBrowserDropHandlerImpl(EntityId dragUponInstanceId, string dummy, bool perform)
        {
            var search = new HierarchyIterator(HierarchyType.Assets);
            if (search.Find(dragUponInstanceId, null))
                return InternalEditorUtility.ProjectWindowDragV2(search, perform);

            if (dragUponInstanceId != EntityId.None)
            {
                var path = AssetDatabase.GetAssetPath(dragUponInstanceId);
                if (string.IsNullOrEmpty(path))
                    return DragAndDropVisualMode.Rejected;

                var packageInfo = PackageManager.PackageInfo.FindForAssetPath(path);
                if (packageInfo != null)
                {
                    search = new HierarchyIterator(packageInfo.assetPath);
                    if (search.Find(dragUponInstanceId, null))
                        return InternalEditorUtility.ProjectWindowDragV2(search, perform);
                }
            }
            return InternalEditorUtility.ProjectWindowDragV2(null, perform);
        }

        // HandleDelayedDrag can be used to start a drag and drop
        internal static bool HandleDelayedDrag(Rect position, int id, EntityId entityIdToDrag)
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
                            entityIds = new[] { entityIdToDrag };
                            StartDrag(ObjectNames.GetDragAndDropTitle(InternalEditorUtility.GetObjectFromEntityId(entityIdToDrag)));
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
            entityIds = Array.Empty<EntityId>();
            visualMode = DragAndDropVisualMode.None;
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

        // Prefer to use entityIds instead to retrieve an array of all entity ids being dragged as it is now possible to drag pure entities in the Editor.
        public static Object[] objectReferences
        {
            get
            {
                var ids = new ReadOnlySpan<EntityId>(entityIds);
                var objects = new List<Object>(entityIds.Length);
                for (int i = 0; i < ids.Length; ++i)
                {
                    var o = InternalEditorUtility.GetObjectFromEntityId(ids[i]);
                    if (o != null)
                        objects.Add(o);
                }
                return objects.ToArray();
            }
            set
            {
                if (value != null)
                {
                    var objects = new ReadOnlySpan<Object>(value);
                    var ids = new EntityId[objects.Length];
                    for (int i = 0; i < objects.Length; ++i)
                        ids[i] = objects[i].GetEntityId();
                    entityIds = ids;
                }
                else
                {
                    entityIds = null;
                }
            }
        }

        public static extern EntityId[] entityIds { [NativeMethod("GetEntityIds")] get; [NativeMethod("SetEntityIds")] set; }
        public static extern string[] paths { get; set; }
        public static extern int activeControlID { get; set; }
        public static extern DragAndDropVisualMode visualMode { get; set; }

        public static extern void AcceptDrag();
        [NativeMethod("PrepareStartDrag")]
        private static extern void PrepareStartDrag_Internal();
        [NativeMethod("Cleanup")]
        internal static extern void Cleanup();
        [NativeMethod("StartDrag")]
        private static extern void StartDrag_Internal(string title);

        public delegate DragAndDropVisualMode ProjectBrowserDropHandlerV2(EntityId dragEntityId, string dropUponPath, bool perform);
        [Obsolete("Use ProjectBrowserDropHandlerV2 instead", true)]
        public delegate DragAndDropVisualMode ProjectBrowserDropHandler(int dragInstanceID, string dropUponPath, bool perform);
        public delegate DragAndDropVisualMode SceneDropHandler(UnityEngine.Object dropUpon, Vector3 worldPosition, Vector2 viewportPosition, Transform parentForDraggedObjects, bool perform);
        public delegate DragAndDropVisualMode InspectorDropHandler(UnityEngine.Object[] targets, bool perform);
        public delegate DragAndDropVisualMode HierarchyDropHandlerV2(EntityId dropTargetEntityId, HierarchyDropFlags dropMode, Transform parentForDraggedObjects, bool perform);
        [Obsolete("Use HierarchyDropHandlerV2 instead", true)]
        public delegate DragAndDropVisualMode HierarchyDropHandler(int dropTargetInstanceID, HierarchyDropFlags dropMode, Transform parentForDraggedObjects, bool perform);

        internal static bool HasHandler(int dropDstId)
        {
            return dropHandlers.ContainsKey(dropDstId);
        }

        public static bool HasHandler(int dropDstId, Delegate handler)
        {
            List<Delegate> handlers = null;
            if (!dropHandlers.TryGetValue(dropDstId, out handlers))
            {
                return false;
            }
            return handlers != null && handlers.Exists(dropHandler => dropHandler == handler);
        }

        [Obsolete("Use AddDropHandlerV2 instead", true)] // not upgradable since it's a different delegate
        public static void AddDropHandler(ProjectBrowserDropHandler handler) => AddDropHandler(DragAndDropWindowTarget.projectBrowser, handler);
        public static void AddDropHandlerV2(ProjectBrowserDropHandlerV2 handler) => AddDropHandler(DragAndDropWindowTarget.projectBrowser, handler);
        [Obsolete("Use AddDropHandlerV2 instead (UnityUpgradable) -> AddDropHandlerV2(*)", true)]
        public static void AddDropHandler(SceneDropHandler handler) => AddDropHandler(DragAndDropWindowTarget.sceneView, handler);
        public static void AddDropHandlerV2(SceneDropHandler handler) => AddDropHandler(DragAndDropWindowTarget.sceneView, handler);
        [Obsolete("Use AddDropHandlerV2 instead", true)] // not upgradable since it's a different delegate
        public static void AddDropHandler(HierarchyDropHandler handler) => AddDropHandler(DragAndDropWindowTarget.hierarchy, handler);
        public static void AddDropHandlerV2(HierarchyDropHandlerV2 handler) => AddDropHandler(DragAndDropWindowTarget.hierarchy, handler);
        [Obsolete("Use AddDropHandlerV2 instead (UnityUpgradable) -> AddDropHandlerV2(*)", true)]
        public static void AddDropHandler(InspectorDropHandler handler) => AddDropHandler(DragAndDropWindowTarget.inspector, handler);
        public static void AddDropHandlerV2(InspectorDropHandler handler) => AddDropHandler(DragAndDropWindowTarget.inspector, handler);


        [Obsolete("Use RemoveDropHandlerV2 instead", true)] // not upgradable since it's a different delegate
        public static void RemoveDropHandler(ProjectBrowserDropHandler handler) => RemoveDropHandler(DragAndDropWindowTarget.projectBrowser, handler);
        public static void RemoveDropHandlerV2(ProjectBrowserDropHandlerV2 handler) => RemoveDropHandler(DragAndDropWindowTarget.projectBrowser, handler);

        [Obsolete("Use RemoveDropHandlerV2 instead (UnityUpgradable) -> RemoveDropHandlerV2(*)", true)]
        public static void RemoveDropHandler(SceneDropHandler handler) => RemoveDropHandler(DragAndDropWindowTarget.sceneView, handler);
        public static void RemoveDropHandlerV2(SceneDropHandler handler) => RemoveDropHandler(DragAndDropWindowTarget.sceneView, handler);


        [Obsolete("Use RemoveDropHandlerV2 instead", true)] // not upgradable since it's a different delegate
        public static void RemoveDropHandler(HierarchyDropHandler handler) => RemoveDropHandler(DragAndDropWindowTarget.hierarchy, handler);
        public static void RemoveDropHandlerV2(HierarchyDropHandlerV2 handler) => RemoveDropHandler(DragAndDropWindowTarget.hierarchy, handler);



        [Obsolete("Use RemoveDropHandlerV2 instead (UnityUpgradable) -> RemoveDropHandlerV2(*)", true)]
        public static void RemoveDropHandler(InspectorDropHandler handler) => RemoveDropHandler(DragAndDropWindowTarget.inspector, handler);
        public static void RemoveDropHandlerV2(InspectorDropHandler handler) => RemoveDropHandler(DragAndDropWindowTarget.inspector, handler);
    }
}
