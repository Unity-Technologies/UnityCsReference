// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Search;
using UnityEditor.Search.Providers;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEditor.Lighting.LightingSearch
{
    static class LightingSearchSelectorsMeshRenderer
    {
        [SceneQueryEngineFilter(LightingSearchSelectors.k_RenderingLayersFilter, new[] { "=" })]
        internal static uint RenderingLayersSceneQueryEngineFilter(GameObject go)
        {
            if (!go.TryGetComponent<MeshRenderer>(out var meshRenderer))
                return 0;

            return meshRenderer.renderingLayerMask;
        }

        [SearchSelector(LightingSearchSelectors.k_RenderingLayersPath, provider: LightingSearchSelectors.k_SceneProvider)]
        static object RenderingLayersSearchSelector(SearchSelectorArgs args)
        {
            var go = args.current.ToObject<GameObject>();
            if (go == null)
                return null;

            return LightingSearchDataAccessors.GetRenderingLayers(go);
        }

        [SearchColumnProvider(LightingSearchSelectors.k_RenderingLayersPath)]
        internal static void RenderingLayersSearchColumnProvider(SearchColumn column)
        {
            column.getter = args =>
            {
                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return null;

                return LightingSearchDataAccessors.GetRenderingLayers(go);
            };
            column.setter = args =>
            {
                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return;

                LightingSearchDataAccessors.SetRenderingLayers(go, Convert.ToUInt32(args.value));
            };
            column.cellCreator = _ => new RenderingLayerMaskField { style = { flexGrow = 1 } };
            column.binder = (args, ve) =>
            {
                var field = (RenderingLayerMaskField)ve;
                if (args.value != null)
                {
                    field.visible = true;
                    field.SetValueWithoutNotify(Convert.ToUInt32(args.value));
                }
                else
                {
                    field.visible = false;
                }
            };
        }

        [SceneQueryEngineFilter(LightingSearchSelectors.k_ContributeGIFilter, new[] { "=" })]
        internal static bool ContributeGISceneQueryEngineFilter(GameObject go)
        {
            return LightingSearchDataAccessors.GetContributeGI(go);
        }

        [SearchSelector(LightingSearchSelectors.k_ContributeGIPath, provider: LightingSearchSelectors.k_SceneProvider, priority: 99)]
        static object ContributeGISearchSelector(SearchSelectorArgs args)
        {
            var go = args.current.ToObject<GameObject>();
            if (go == null)
                return null;

            return LightingSearchDataAccessors.GetContributeGI(go);
        }

        [SearchColumnProvider(LightingSearchSelectors.k_ContributeGIPath)]
        public static void ContributeGISearchColumnProvider(SearchColumn column)
        {
            column.getter = args =>
            {
                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return null;

                return LightingSearchDataAccessors.GetContributeGI(go);
            };
            column.setter = args =>
            {
                if (args.value is not bool value)
                    return;

                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return;

                LightingSearchDataAccessors.SetContributeGI(go, value);
            };
            column.cellCreator = _ => new Toggle { style = { alignSelf = Align.Center } };
            column.binder = (args, ve) =>
            {
                var field = (Toggle)ve;
                if (args.value != null)
                {
                    field.visible = true;
                    field.SetValueWithoutNotify(Convert.ToBoolean(args.value));
                }
                else
                {
                    field.visible = false;
                }
            };
        }

        [SceneQueryEngineFilter(LightingSearchSelectors.k_ReceiveGIFilter, new[] { "=" })]
        internal static object ReceiveGISceneQueryEngineFilter(GameObject go)
        {
            if (!go.TryGetComponent<MeshRenderer>(out var meshRenderer))
                return null;

            return meshRenderer.receiveGI;
        }

        [SearchSelector(LightingSearchSelectors.k_ReceiveGIPath, provider: LightingSearchSelectors.k_SceneProvider, priority: 99)]
        static object ReceiveGISearchSelector(SearchSelectorArgs args)
        {
            var go = args.current.ToObject<GameObject>();
            if (go == null)
                return null;

            return LightingSearchDataAccessors.GetReceiveGI(go);
        }

        [SearchColumnProvider(LightingSearchSelectors.k_ReceiveGIPath)]
        public static void ReceiveGISearchColumnProvider(SearchColumn column)
        {
            column.getter = args =>
            {
                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return null;

                if (!go.TryGetComponent<MeshRenderer>(out var meshRenderer))
                    return null;

                return meshRenderer.receiveGI;
            };
            column.setter = args =>
            {
                if (args.value == null || !args.value.GetType().IsEnum)
                    return;

                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return;

                LightingSearchDataAccessors.SetReceiveGI(go, (ReceiveGI)args.value);
            };
            column.cellCreator = _ => new EnumField(null, ReceiveGI.Lightmaps) { style = { flexGrow = 1 } };
            column.binder = (args, ve) =>
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
            };
        }

        [SearchSelector(LightingSearchSelectors.k_ReflectionProbeUsagePath, provider: LightingSearchSelectors.k_SceneProvider)]
        static object ReflectionProbeUsageSearchSelector(SearchSelectorArgs args)
        {
            var go = args.current.ToObject<GameObject>();
            if (go == null)
                return null;

            return LightingSearchDataAccessors.GetReflectionProbeUsage(go);
        }

        [SearchColumnProvider(LightingSearchSelectors.k_ReflectionProbeUsagePath)]
        internal static void ReflectionProbeUsageSearchColumnProvider(SearchColumn column)
        {
            column.getter = args =>
            {
                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return null;

                if (!go.TryGetComponent<MeshRenderer>(out var meshRenderer))
                    return null;

                return meshRenderer.reflectionProbeUsage;
            };
            column.setter = args =>
            {
                if (args.value == null || !args.value.GetType().IsEnum)
                    return;

                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return;

                LightingSearchDataAccessors.SetReflectionProbeUsage(go, (ReflectionProbeUsage)args.value);
            };
            column.cellCreator = _ => new EnumField(null, ReflectionProbeUsage.Off) { style = { flexGrow = 1 } };
            column.binder = (args, ve) =>
            {
                var field = (EnumField)ve;
                if (args.value != null)
                {
                    field.visible = true;
                    field.SetValueWithoutNotify((ReflectionProbeUsage)args.value);
                }
                else
                {
                    field.visible = false;
                }
            };
        }
    }
}
