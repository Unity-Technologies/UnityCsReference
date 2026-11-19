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
            => LightingSearchDataAccessors.GetRenderingLayers(go);

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
            => LightingSearchDataAccessors.GetContributeGI(go);

        [SearchColumnProvider(LightingSearchPaths.k_ContributeGIPath)]
        internal static void ContributeGISearchColumnProvider(SearchColumn column)
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
            return LightingSearchDataAccessors.GetReceiveGI(go) ?? default;
        }

        [SearchColumnProvider(LightingSearchPaths.k_ReceiveGIPath)]
        internal static void ReceiveGISearchColumnProvider(SearchColumn column)
        {
            column.getter = LightingSearchColumnHelpers.CreateGameObjectGetter<MeshRenderer>(go => LightingSearchDataAccessors.GetReceiveGI(go).Value);
            column.setter = LightingSearchColumnHelpers.CreateGameObjectSetter(
                (go, v) => LightingSearchDataAccessors.SetReceiveGI(go, (ReceiveGI)v),
                LightingSearchColumnHelpers.IsValidEnum);
            column.cellCreator = _ => new EnumField(null, ReceiveGI.Lightmaps) { style = { flexGrow = 1 } };
            column.binder = LightingSearchColumnHelpers.CreateBinderWithGameObject<EnumField>((f, go, v) =>
            {
                bool contributesGI = LightingSearchDataAccessors.GetContributeGI(go);
                f.SetEnabled(contributesGI);
                f.SetValueWithoutNotify(contributesGI ? (ReceiveGI)v : ReceiveGI.LightProbes);
            });
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
