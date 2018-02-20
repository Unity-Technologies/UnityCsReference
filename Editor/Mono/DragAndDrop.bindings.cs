// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using Object = UnityEngine.Object;
using System.Collections;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

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

    // Editor drag & drop operations.
    [NativeHeader("Editor/Platform/Interface/DragAndDrop.h"),
     StaticAccessor("GetDragAndDrop()", StaticAccessorType.Dot)]
    public class DragAndDrop
    {
        private static Hashtable s_GenericData;

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
            s_GenericData = null;
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
    }
}
