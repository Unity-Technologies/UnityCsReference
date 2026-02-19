// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Lighting.LightingSearch
{
    static class LightingSearchSelectorsReflectionProbe
    {
        [SearchSelector(LightingSearchSelectors.k_ReflectionProbeResolutionPath, provider: LightingSearchSelectors.k_SceneProvider)]
        static object ReflectionProbeResolutionSearchSelector(SearchSelectorArgs args)
        {
            var go = args.current.ToObject<GameObject>();
            if (go == null)
                return null;

            return LightingSearchDataAccessors.GetReflectionProbeResolution(go);
        }

        [SearchColumnProvider(LightingSearchSelectors.k_ReflectionProbeResolutionPath)]
        internal static void ReflectionProbeResolutionSearchColumnProvider(SearchColumn column)
        {
            column.getter = args =>
            {
                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return null;

                if (!go.TryGetComponent<ReflectionProbe>(out var reflectionProbe))
                    return null;

                return LightingSearchDataAccessors.GetReflectionProbeResolution(go);
            };
            column.setter = args =>
            {
                if (args.value is not int)
                    return;

                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return;

                LightingSearchDataAccessors.SetReflectionProbeResolution(go, (int)args.value);
            };
            column.cellCreator = _ => new PopupField<int>(
                new List<int> { 16, 32, 64, 128, 256, 512, 1024, 2048 },
                0,
                value => value.ToString(),
                value => value.ToString())
            {
                style = { flexGrow = 1 }
            };
            column.binder = (args, ve) =>
            {
                var field = (PopupField<int>)ve;
                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (!go || !go.TryGetComponent<ReflectionProbe>(out _))
                {
                    field.visible = false;
                    return;
                }

                if (args.value != null)
                {
                    field.visible = true;
                    field.SetValueWithoutNotify(Convert.ToInt32(args.value));
                }
                else
                {
                    field.visible = false;
                }
            };
        }
    }
}
