// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Search.Providers;

namespace UnityEditor.Search
{
    static class LightExplorerSelectors
    {
        // TODO LightExplorer: it would be nice when registering a filter to be able to register a category (ex: Lights/ContributeGI).
        [SceneQueryEngineFilter("ContributeGI", new[] { "=" })]
        internal static bool ContributeGlobalIllumination(GameObject go)
        {
            if (!go.TryGetComponent<MeshRenderer>(out var meshRenderer))
                return false;

            if (!meshRenderer.isVisible)
                return false;

            // TODO LightExplorer: proposition add ContributeGI>1 -> it assumes it is an integer?

            bool contributeGI = GameObjectUtility.AreStaticEditorFlagsSet(go, StaticEditorFlags.ContributeGI);
            return contributeGI;
        }

        static object SetContributeGI(SearchColumnEventArgs args)
        {
            var go = args.item.ToObject<GameObject>();
            if (!go)
                return null;

            var value = (bool)args.value;
            var flag = GameObjectUtility.GetStaticEditorFlags(go);
            if (value)
                flag |= StaticEditorFlags.ContributeGI;
            else
                flag &= ~StaticEditorFlags.ContributeGI;

            Undo.RecordObject(go, "Change ContributeGI");
            GameObjectUtility.SetStaticEditorFlags(go, flag);
            return GameObjectUtility.AreStaticEditorFlagsSet(go, StaticEditorFlags.ContributeGI);
        }

        static object GetContributeGI(SearchItem item)
        {
            var go = item.ToObject<GameObject>();
            if (!go)
                return null;
            return GameObjectUtility.AreStaticEditorFlagsSet(go, StaticEditorFlags.ContributeGI);
        }

        [SearchSelector("GameObject/ContributeGI", provider: "scene", priority: 99)]
        static object GetContributeGI(SearchSelectorArgs args)
        {
            return GetContributeGI(args.current);
        }

        [SearchColumnProvider("GameObject/ContributeGI")]
        public static void InitializeGOContributeGI(SearchColumn column)
        {
            column.getter = args => GetContributeGI(args.item);
            column.setter = args => SetContributeGI(args);
            column.cellCreator = col => new Toggle { style = { alignSelf = Align.Center } };
            column.binder = (SearchColumnEventArgs args, VisualElement ve) =>
            {
                var field = (Toggle)ve;
                if (args.value != null)
                {
                    field.visible = true;
                    field.SetValueWithoutNotify(System.Convert.ToBoolean(args.value));
                }
                else
                {
                    field.visible = false;
                }
            };
        }

        [SceneQueryEngineFilter("ReceiveGI", new[] { "=" })]
        internal static UnityEngine.ReceiveGI GetReceiveGI(GameObject go)
        {
            if (!go.TryGetComponent<MeshRenderer>(out var meshRenderer))
                return UnityEngine.ReceiveGI.Lightmaps;

            if (!meshRenderer.isVisible)
                return UnityEngine.ReceiveGI.Lightmaps;

            return meshRenderer.receiveGI;
        }

        static object SetReceiveGI(SearchColumnEventArgs args)
        {
            if (args.value == null || !args.value.GetType().IsEnum)
                return null;

            var go = args.item.ToObject<GameObject>();
            if (!go)
                return null;
            if (!go.TryGetComponent<MeshRenderer>(out var meshRenderer))
                return null;

            Undo.RecordObject(meshRenderer, "Change ReceiveGI");
            meshRenderer.receiveGI = (UnityEngine.ReceiveGI)args.value;
            return meshRenderer.receiveGI;
        }

        static object GetReceiveGI(SearchItem item)
        {
            var go = item.ToObject<GameObject>();
            if (!go)
                return null;
            if (!go.TryGetComponent<MeshRenderer>(out var meshRenderer))
                return null;

            return meshRenderer.receiveGI;
        }

        [SearchSelector("MeshRenderer/ReceiveGI", provider: "scene", priority: 99)]
        static object GetReceiveGI(SearchSelectorArgs args)
        {
            return GetReceiveGI(args.current);
        }

        [SearchColumnProvider("MeshRenderer/ReceiveGI")]
        public static void InitializeMeshRendererReceiveGI(SearchColumn column)
        {
            column.getter = args => GetReceiveGI(args.item);
            column.setter = args => SetReceiveGI(args);
            column.cellCreator = CreateReceiGI;
            column.binder = BindReceiveGI;
        }

        static VisualElement CreateReceiGI(SearchColumn col)
        {
            return new EnumField(null, ReceiveGI.Lightmaps) { style = { alignSelf = Align.Center } };
        }

        static void BindReceiveGI(SearchColumnEventArgs args, VisualElement ve)
        {
            var field = (EnumField)ve;
            if (args.value != null)
            {
                field.visible = true;
                field.SetValueWithoutNotify((ReceiveGI)args.value);
            }
            else
            {
                field.visible = false;
            }
        }

        static uint SetRenderingLayers(SearchColumnEventArgs args)
        {
            var go = args.item.ToObject<GameObject>();
            if (!go)
                return 0;

            if (!go.TryGetComponent<MeshRenderer>(out var meshRenderer))
                return 0;

            Undo.RecordObject(meshRenderer, "Change Rendering Layers");
            meshRenderer.renderingLayerMask = (uint)args.value;
            return meshRenderer.renderingLayerMask;
        }

        static object GetRenderingLayers(SearchItem item)
        {
            var go = item.ToObject<GameObject>();
            if (!go)
                return 0;
            if (!go.TryGetComponent<MeshRenderer>(out var meshRenderer))
                return 0;
            return meshRenderer.renderingLayerMask;
        }

        [SearchSelector("MeshRenderer/RenderingLayers", provider: "scene", priority: 99)]
        static object GetRenderingLayers(SearchSelectorArgs args)
        {
            return GetRenderingLayers(args.current);
        }

        [SearchColumnProvider("MeshRenderer/RenderingLayers")]
        public static void InitializeMeshRendererRenderingLayers(SearchColumn column)
        {
            column.getter = args => GetRenderingLayers(args.item);
            column.setter = args => SetRenderingLayers(args);
            column.cellCreator = col => new RenderingLayerMaskField { style = { alignSelf = Align.Center } };
            column.binder = (SearchColumnEventArgs args, VisualElement ve) =>
            {
                var field = (RenderingLayerMaskField)ve;
                if (args.value != null)
                {
                    field.visible = true;
                    field.SetValueWithoutNotify(System.Convert.ToUInt32(args.value));
                }
                else
                {
                    field.visible = false;
                }
            };
        }
    }
}
