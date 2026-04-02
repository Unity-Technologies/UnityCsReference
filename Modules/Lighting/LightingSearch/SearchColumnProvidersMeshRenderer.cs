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
    static class SearchColumnProvidersMeshRenderer
    {
        [SceneQueryEngineFilter(LightingSearchPaths.k_RenderingLayersFilter, new[] { "=" })]
        internal static uint RenderingLayersSceneQueryEngineFilter(GameObject go)
        {
            if (!go.TryGetComponent<MeshRenderer>(out var meshRenderer))
                return 0;

            return meshRenderer.renderingLayerMask;
        }

        [SearchColumnProvider(LightingSearchPaths.k_RenderingLayersPath)]
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
                if (args.value is not uint value)
                    return;

                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return;

                LightingSearchDataAccessors.SetRenderingLayers(go, value);
            };
            column.cellCreator = _ => new RenderingLayerMaskField { style = { flexGrow = 1 } };
            column.binder = LightingSearchColumnHelpers.CreateBinder<RenderingLayerMaskField>((f, v) => f.SetValueWithoutNotify((uint)v));
        }

        [SceneQueryEngineFilter(LightingSearchPaths.k_ContributeGIFilter, new[] { "=" })]
        internal static bool ContributeGISceneQueryEngineFilter(GameObject go)
        {
            return LightingSearchDataAccessors.GetContributeGI(go);
        }

        [SearchColumnProvider(LightingSearchPaths.k_ContributeGIPath)]
        public static void ContributeGISearchColumnProvider(SearchColumn column)
        {
            column.getter = LightingSearchColumnHelpers.CreateGameObjectGetter(go => LightingSearchDataAccessors.GetContributeGI(go));
            column.setter = LightingSearchColumnHelpers.CreateGameObjectSetter(
                (go, v) => LightingSearchDataAccessors.SetContributeGI(go, (bool)v),
                LightingSearchColumnHelpers.IsValidBool);
            column.cellCreator = _ => new Toggle { style = { alignSelf = Align.Center } };
            column.binder = LightingSearchColumnHelpers.CreateBinder<Toggle>((f, v) => f.SetValueWithoutNotify(Convert.ToBoolean(v)));
        }

        [SceneQueryEngineFilter(LightingSearchPaths.k_ReceiveGIFilter, new[] { "=" })]
        internal static ReceiveGI ReceiveGISceneQueryEngineFilter(GameObject go)
        {
            if (!go.TryGetComponent<MeshRenderer>(out var meshRenderer))
                return default;

            return meshRenderer.receiveGI;
        }

        [SearchColumnProvider(LightingSearchPaths.k_ReceiveGIPath)]
        public static void ReceiveGISearchColumnProvider(SearchColumn column)
        {
            column.getter = LightingSearchColumnHelpers.CreateGameObjectGetter<MeshRenderer>(go => LightingSearchDataAccessors.GetReceiveGI(go));
            column.setter = LightingSearchColumnHelpers.CreateGameObjectSetter(
                (go, v) => LightingSearchDataAccessors.SetReceiveGI(go, (ReceiveGI)v),
                LightingSearchColumnHelpers.IsValidEnum);
            column.cellCreator = _ => new EnumField(null, ReceiveGI.Lightmaps) { style = { flexGrow = 1 } };
            column.binder = LightingSearchColumnHelpers.CreateBinder<EnumField>((f, v) => f.SetValueWithoutNotify((ReceiveGI)v));
        }

        [SearchColumnProvider(LightingSearchPaths.k_ReflectionProbeUsagePath)]
        internal static void ReflectionProbeUsageSearchColumnProvider(SearchColumn column)
        {
            column.getter = LightingSearchColumnHelpers.CreateGameObjectGetter<MeshRenderer>(go => LightingSearchDataAccessors.GetReflectionProbeUsage(go));
            column.setter = LightingSearchColumnHelpers.CreateGameObjectSetter(
                (go, v) => LightingSearchDataAccessors.SetReflectionProbeUsage(go, (ReflectionProbeUsage)v),
                LightingSearchColumnHelpers.IsValidEnum);
            column.cellCreator = _ => new EnumField(null, ReflectionProbeUsage.Off) { style = { flexGrow = 1 } };
            column.binder = LightingSearchColumnHelpers.CreateBinder<EnumField>((f, v) => f.SetValueWithoutNotify((ReflectionProbeUsage)v));
        }
    }
}
