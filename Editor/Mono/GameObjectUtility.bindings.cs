// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Mono/GameObjectUtility.bindings.h")]
    public sealed partial class GameObjectUtility
    {
        public static extern StaticEditorFlags GetStaticEditorFlags(GameObject go);
        public static extern void SetStaticEditorFlags(GameObject go, StaticEditorFlags flags);
        public static extern bool AreStaticEditorFlagsSet(GameObject go, StaticEditorFlags flags);

        public static extern int GetNavMeshArea(GameObject go);
        public static extern void SetNavMeshArea(GameObject go, int areaIndex);

        public static extern int GetNavMeshAreaFromName(string name);
        public static extern string[] GetNavMeshAreaNames();

        public static extern string GetUniqueNameForSibling(Transform parent, string name);

        internal static bool ContainsStatic(GameObject[] objects)
        {
            if (objects == null || objects.Length == 0)
                return false;
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null && objects[i].isStatic)
                    return true;
            }
            return false;
        }

        internal static bool HasChildren(IEnumerable<GameObject> gameObjects)
        {
            return gameObjects.Any(go => go.transform.childCount > 0);
        }

        internal enum ShouldIncludeChildren
        {
            HasNoChildren = -1,
            IncludeChildren = 0,
            DontIncludeChildren = 1,
            Cancel = 2
        }

        internal static ShouldIncludeChildren DisplayUpdateChildrenDialogIfNeeded(IEnumerable<GameObject> gameObjects,
            string title, string message)
        {
            if (!HasChildren(gameObjects))
                return ShouldIncludeChildren.HasNoChildren;

            return
                (ShouldIncludeChildren)
                EditorUtility.DisplayDialogComplex(title, message, "Yes, change children", "No, this object only", "Cancel");
        }

        public static void SetParentAndAlign(GameObject child, GameObject parent)
        {
            if (parent == null)
                return;

            child.transform.SetParent(parent.transform, false);

            RectTransform rectTransform = child.transform as RectTransform;
            if (rectTransform)
            {
                rectTransform.anchoredPosition = Vector2.zero;
                Vector3 localPosition = rectTransform.localPosition;
                localPosition.z = 0;
                rectTransform.localPosition = localPosition;
            }
            else
            {
                child.transform.localPosition = Vector3.zero;
            }
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;

            SetLayerRecursively(child, parent.layer);
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            Transform t = go.transform;
            for (int i = 0; i < t.childCount; i++)
                SetLayerRecursively(t.GetChild(i).gameObject, layer);
        }

        [System.Obsolete("GetNavMeshArea instead.")]
        public static int GetNavMeshLayer(GameObject go)
        {
            return GetNavMeshArea(go);
        }

        [System.Obsolete("SetNavMeshArea instead.")]
        public static void SetNavMeshLayer(GameObject go, int areaIndex)
        {
            SetNavMeshArea(go, areaIndex);
        }

        [System.Obsolete("GetNavMeshAreaFromName instead.")]
        public static int GetNavMeshLayerFromName(string name)
        {
            return GetNavMeshAreaFromName(name);
        }

        [System.Obsolete("GetNavMeshAreaNames instead.")]
        public static string[] GetNavMeshLayerNames()
        {
            return GetNavMeshAreaNames();
        }

        [System.Obsolete("use AnimatorUtility.OptimizeTransformHierarchy instead.")]
        static void OptimizeTransformHierarchy(GameObject go)
        {
            AnimatorUtility.OptimizeTransformHierarchy(go, null);
        }

        [System.Obsolete("use AnimatorUtility.DeoptimizeTransformHierarchy instead.")]
        static void DeoptimizeTransformHierarchy(GameObject go)
        {
            AnimatorUtility.DeoptimizeTransformHierarchy(go);
        }
    }
}
