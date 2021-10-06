// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEditor.Search.Providers;
using UnityEngine;

namespace UnityEditor.Search
{
    class SceneSelectors
    {
        const string providerId = "scene";

        [SearchSelector("type", provider: providerId, priority: 99)]
        static object GetSceneObjectType(SearchSelectorArgs args)
        {
            var go = args.current.ToObject<GameObject>();
            if (!go)
                return null;
            var components = go.GetComponents<Component>();
            if (components.Length == 0)
                return go.GetType().Name;
            else if (components.Length == 1)
                return components[0]?.GetType().Name;
            return components[1]?.GetType().Name;
        }

        [SearchSelector("tag", provider: providerId, priority: 99)]
        static object GetSceneObjectTag(SearchSelectorArgs args)
        {
            var go = args.current.ToObject<GameObject>();
            if (!go)
                return null;
            return go.tag;
        }

        [SearchSelector("path", provider: providerId, priority: 99)]
        static object GetSceneObjectPath(SearchSelectorArgs args)
        {
            var go = args.current.ToObject<GameObject>();
            if (!go)
                return null;
            return SearchUtils.GetHierarchyPath(go, false);
        }

        [SearchSelector("name", provider: providerId, priority: 99)]
        static object GetObjectName(SearchSelectorArgs args)
        {
            var go = args.current.ToObject<GameObject>();
            if (!go)
                return null;
            return go.name;
        }

        [SearchSelector("enabled", provider: providerId, priority: 99)]
        static object GetEnabled(SearchSelectorArgs args)
        {
            return GetEnabled(args.current);
        }

        private static object GetEnabled(SearchItem item)
        {
            var go = item.ToObject<GameObject>();
            if (!go)
                return null;
            return go.activeSelf;
        }

        private static object DrawEnabled(SearchColumnEventArgs args)
        {
            if (args.value == null)
                return null;
            var w = 14f + EditorStyles.toggle.margin.horizontal;
            if (args.column.options.HasAny(SearchColumnFlags.TextAlignmentRight))
                args.rect.xMin = args.rect.xMax - w;
            else if (args.column.options.HasAny(SearchColumnFlags.TextAlignmentCenter))
                args.rect.xMin += (args.rect.width - w) / 2f;
            else
                args.rect.x += EditorStyles.toggle.margin.left;
            args.value = EditorGUI.Toggle(args.rect, (bool)args.value);
            return args.value;
        }

        static object SetEnabled(SearchColumnEventArgs args)
        {
            var go = args.item.ToObject<GameObject>();
            if (!go)
                return null;
            go.SetActive((bool)args.value);
            return go.activeSelf;
        }

        [SearchColumnProvider("GameObject/Enabled")]
        public static void InitializeObjectReferenceColumn(SearchColumn column)
        {
            column.getter = args => GetEnabled(args.item);
            column.drawer = args => DrawEnabled(args);
            column.setter = args => SetEnabled(args);
        }

        public static IEnumerable<SearchColumn> Enumerate(IEnumerable<SearchItem> items)
        {
            return PropertySelectors.Enumerate(items).Concat(new[]
            {
                new SearchColumn("GameObject/Enabled", "enabled", "GameObject/Enabled", options: SearchColumnFlags.TextAlignmentCenter)
            });
        }

        [SceneQueryEngineFilter("vdist")]
        public static float SceneFilterViewDistance(GameObject go)
        {
            if (!go || !SceneView.lastActiveSceneView)
                return float.NaN;
            var cam = SceneView.lastActiveSceneView.camera;
            var dir = go.transform.position - cam.transform.position;
            return dir.magnitude;
        }

        [SceneQueryEngineFilter("position")]
        public static string SceneFilterPosition(GameObject go)
        {
            var position = go.transform.position;
            return $"{Mathf.RoundToInt(position.x)};{Mathf.RoundToInt(position.y)};{Mathf.RoundToInt(position.z)}";
        }

        [SceneQueryEngineFilter("x")] public static int SceneFilterPositionX(GameObject go) => Mathf.RoundToInt(go.transform.position.z);
        [SceneQueryEngineFilter("y")] public static int SceneFilterPositionY(GameObject go) => Mathf.RoundToInt(go.transform.position.y);
        [SceneQueryEngineFilter("z")] public static int SceneFilterPositionZ(GameObject go) => Mathf.RoundToInt(go.transform.position.z);

        [SearchSelector("position", provider: providerId)]
        public static string SceneSelectPosition(SearchItem item)
        {
            var go = item.ToObject<GameObject>();
            if (!go)
                return null;
            return SceneFilterPosition(go);
        }

        [SearchSelector("x", provider: providerId)]
        public static object SceneSelectPositionX(SearchItem item)
        {
            var go = item.ToObject<GameObject>();
            if (!go)
                return null;
            return Mathf.RoundToInt(go.transform.position.x);
        }

        [SearchSelector("y", provider: providerId)]
        public static object SceneSelectPositionY(SearchItem item)
        {
            var go = item.ToObject<GameObject>();
            if (!go)
                return null;
            return Mathf.RoundToInt(go.transform.position.y);
        }

        [SearchSelector("z", provider: providerId)]
        public static object SceneSelectPositionZ(SearchItem item)
        {
            var go = item.ToObject<GameObject>();
            if (!go)
                return null;
            return Mathf.RoundToInt(go.transform.position.z);
        }

        [SearchSelector("vertices", provider: "scene")]
        static object SelectVertices(SearchSelectorArgs args)
        {
            var go = args.current.ToObject<GameObject>();
            if (!go)
                return null;

            return SceneFilterVertices(go);
        }

        [SceneQueryEngineFilter("vertices")]
        public static int SceneFilterVertices(GameObject go)
        {
            var meshFilter = go.GetComponent<MeshFilter>();
            if (!meshFilter || !meshFilter.sharedMesh)
                return 0;

            return meshFilter.sharedMesh.vertexCount;
        }

        [SearchSelector("faces", provider: "scene")]
        static object SelectFaces(SearchSelectorArgs args)
        {
            var go = args.current.ToObject<GameObject>();
            if (!go)
                return null;

            return SceneFilterFaces(go) ?? null;
        }

        [SceneQueryEngineFilter("faces")]
        public static int? SceneFilterFaces(GameObject go)
        {
            var meshFilter = go.GetComponent<MeshFilter>();
            if (!meshFilter || !meshFilter.sharedMesh)
                return null;

            return meshFilter.sharedMesh.triangles.Length;
        }
    }
}
