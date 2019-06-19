// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    internal static class DragAndDropService
    {
        public static int kProjectBrowserDropDstId = "ProjectBrowser".GetHashCode();
        public static int kSceneDropDstId = "SceneView".GetHashCode();
        public static int kHierarchyDropDstId = "Hierarchy".GetHashCode();
        public static int kInspectorDropDstId = "Inspector".GetHashCode();

        public delegate DragAndDropVisualMode ProjectBrowserDropHandler(int dragInstanceId, string dropUponPath, bool perform);
        public delegate DragAndDropVisualMode SceneDropHandler(UnityEngine.Object dropUpon, Vector3 worldPosition, Vector2 viewportPosition, Transform parentForDraggedObjects, bool perform);
        public delegate DragAndDropVisualMode HierarchyDropHandler(int dropTargetInstanceID, InternalEditorUtility.HierarchyDropMode dropMode, Transform parentForDraggedObjects, bool perform);
        public delegate DragAndDropVisualMode InspectorDropHandler(UnityEngine.Object[] targets, bool perform);

        public static bool HasHandler(int dropDstId, Delegate handler)
        {
            List<Delegate> handlers;
            if (!m_DropDescriptors.TryGetValue(dropDstId, out handlers))
            {
                return false;
            }
            return handlers.Any(h => h == handler);
        }

        public static Action AddDropHandler(ProjectBrowserDropHandler handler)
        {
            return AddDropHandler(kProjectBrowserDropDstId, handler);
        }

        public static Action AddDropHandler(SceneDropHandler handler)
        {
            return AddDropHandler(kSceneDropDstId, handler);
        }

        public static Action AddDropHandler(HierarchyDropHandler handler)
        {
            return AddDropHandler(kHierarchyDropDstId, handler);
        }

        public static Action AddDropHandler(InspectorDropHandler handler)
        {
            return AddDropHandler(kInspectorDropDstId, handler);
        }

        public static Action AddDropHandler(int dropDstId, Delegate handler)
        {
            if (HasHandler(dropDstId, handler))
            {
                throw new Exception("Delegate already registered for dropDestinationId:" + dropDstId);
            }

            List<Delegate> handlers;
            if (!m_DropDescriptors.TryGetValue(dropDstId, out handlers))
            {
                handlers = new List<Delegate>();
                m_DropDescriptors[dropDstId] = handlers;
            }

            handlers.Add(handler);
            Action off = () =>
            {
                RemoveDropHandler(dropDstId, handler);
            };
            return off;
        }

        public static void RemoveDropHandler(int dropDstId, Delegate handler)
        {
            List<Delegate> handlers;
            if (m_DropDescriptors.TryGetValue(dropDstId, out handlers))
            {
                handlers.Remove(handler);
            }
        }

        public static DragAndDropVisualMode Drop(int dropDstId, params object[] args)
        {
            List<Delegate> handlers;
            if (!m_DropDescriptors.TryGetValue(dropDstId, out handlers))
            {
                return DragAndDropVisualMode.Rejected;
            }

            var dropResult = DragAndDropVisualMode.Rejected;
            for (var i = handlers.Count - 1; i >= 0; --i)
            {
                var handler = handlers[i];
                dropResult = (DragAndDropVisualMode)handler.DynamicInvoke(args);
                if (dropResult != DragAndDropVisualMode.None)
                {
                    break;
                }
            }

            return dropResult;
        }

        #region Implementation
        private static Dictionary<int, List<Delegate>> m_DropDescriptors;
        static DragAndDropService()
        {
            Init();
        }

        internal static void Init()
        {
            m_DropDescriptors = new Dictionary<int, List<Delegate>>();
            AddDropHandler(DefaultProjectBrowserDropHandler);
            AddDropHandler(DefaultSceneDropHandler);
            AddDropHandler(DefaultInspectorDropHandler);
            AddDropHandler(DefaultHierarchyDropHandler);
        }

        internal static DragAndDropVisualMode DefaultProjectBrowserDropHandler(int dragUponInstanceId, string dummy, bool perform)
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

        internal static SceneDropHandler DefaultSceneDropHandler = InternalEditorUtility.SceneViewDrag;
        internal static InspectorDropHandler DefaultInspectorDropHandler = InternalEditorUtility.InspectorWindowDrag;
        internal static HierarchyDropHandler DefaultHierarchyDropHandler = InternalEditorUtility.HierarchyWindowDragByID;

        #endregion
    }
}
