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

        struct DropDescriptor : IComparable<DropDescriptor>
        {
            public int dropDstId;
            public Delegate handler;
            public bool isDefault;

            public bool IsValid()
            {
                return handler != null;
            }

            public int CompareTo(DropDescriptor other)
            {
                var diff = dropDstId - other.dropDstId;
                if (diff != 0)
                {
                    return diff;
                }
                else if (isDefault == other.isDefault)
                {
                    return 0;
                }
                else
                {
                    return isDefault ? 1 : -1;
                }
            }
        }

        public static bool HasHandler(int dropDstId, Delegate handler)
        {
            return m_DropDescriptors.Any(d => d.handler == handler && d.dropDstId == dropDstId);
        }

        public static Action AddDropHandler(ProjectBrowserDropHandler handler, bool isDefault = false)
        {
            return AddDropHandler(kProjectBrowserDropDstId, handler, isDefault);
        }

        public static Action AddDropHandler(SceneDropHandler handler, bool isDefault = false)
        {
            return AddDropHandler(kSceneDropDstId, handler, isDefault);
        }

        public static Action AddDropHandler(HierarchyDropHandler handler, bool isDefault = false)
        {
            return AddDropHandler(kHierarchyDropDstId, handler, isDefault);
        }

        public static Action AddDropHandler(InspectorDropHandler handler, bool isDefault = false)
        {
            return AddDropHandler(kInspectorDropDstId, handler, isDefault);
        }

        public static Action AddDropHandler(int dropDstId, Delegate handler, bool isDefault = false)
        {
            var desc = new DropDescriptor() { dropDstId = dropDstId, handler = handler, isDefault = isDefault};
            if (HasHandler(dropDstId, handler))
            {
                throw new Exception("Delegate already registered for dropDestinationId:" + dropDstId);
            }
            if (isDefault && m_DropDescriptors.Any(d => d.dropDstId == dropDstId && d.isDefault))
            {
                throw new Exception("Default handler already registered for dropDestinationId: " + dropDstId);
            }

            var list = new List<DropDescriptor>(m_DropDescriptors);
            list.Add(desc);
            list.Sort();
            m_DropDescriptors = list.ToArray();
            Action off = () => RemoveDropHandler(handler);
            return off;
        }

        public static void RemoveDropHandler(Delegate handler)
        {
            m_DropDescriptors = m_DropDescriptors.Where(desc => desc.handler != handler).ToArray();
        }

        public static DragAndDropVisualMode Drop(int dropDstId, params object[] args)
        {
            var dropResult = DragAndDropVisualMode.Rejected;
            foreach (var desc in m_DropDescriptors)
            {
                if (desc.dropDstId != dropDstId)
                    continue;

                if (desc.isDefault)
                {
                    if (dropResult == DragAndDropVisualMode.None || dropResult == DragAndDropVisualMode.Rejected)
                    {
                        return (DragAndDropVisualMode)desc.handler.DynamicInvoke(args);
                    }
                }
                else
                {
                    var handlerResult = (DragAndDropVisualMode)desc.handler.DynamicInvoke(args);
                    dropResult = handlerResult != DragAndDropVisualMode.None && handlerResult != DragAndDropVisualMode.Rejected ? handlerResult : dropResult;
                    if (dropResult != DragAndDropVisualMode.None && dropResult != DragAndDropVisualMode.Rejected)
                    {
                        dropResult = handlerResult;
                    }
                }
            }

            return dropResult;
        }

        #region Implementation
        private static DropDescriptor[] m_DropDescriptors;
        static DragAndDropService()
        {
            Init();
        }

        internal static void Init()
        {
            m_DropDescriptors = new DropDescriptor[0];
            AddDropHandler(DefaultProjectBrowserDrop, true);
            AddDropHandler(InternalEditorUtility.SceneViewDrag, true);
            AddDropHandler(InternalEditorUtility.InspectorWindowDrag, true);
            AddDropHandler(InternalEditorUtility.HierarchyWindowDragByID, true);
        }

        internal static bool Validate()
        {
            if (m_DropDescriptors.Length == 0)
                return true;

            var processedIds = new HashSet<int>();
            for (int i = 0; i < m_DropDescriptors.Length; i++)
            {
                var currentDesc = m_DropDescriptors[i];
                var nextDesc = i + 1 < m_DropDescriptors.Length ? m_DropDescriptors[i + 1] : new DropDescriptor();

                // Validate all same dstId are grouped
                if (processedIds.Contains(currentDesc.dropDstId))
                {
                    // Not all ids are properly grouped
                    return false;
                }

                // Validate default is the last element:
                if (currentDesc.isDefault && nextDesc.IsValid() && nextDesc.dropDstId == currentDesc.dropDstId)
                {
                    return false;
                }

                if (!nextDesc.IsValid() || nextDesc.dropDstId != currentDesc.dropDstId)
                {
                    processedIds.Add(currentDesc.dropDstId);
                }
            }

            return true;
        }

        private static DragAndDropVisualMode DefaultProjectBrowserDrop(int dragUponInstanceId, string dummy, bool perform)
        {
            HierarchyProperty search = new HierarchyProperty(HierarchyType.Assets);
            if (search.Find(dragUponInstanceId, null))
                return InternalEditorUtility.ProjectWindowDrag(search, perform);

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

            return InternalEditorUtility.ProjectWindowDrag(null, perform);
        }

        #endregion
    }
}
