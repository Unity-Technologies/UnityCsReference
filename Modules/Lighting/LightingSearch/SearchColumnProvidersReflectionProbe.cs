// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Lighting.LightingSearch
{
    static class SearchColumnProvidersReflectionProbe
    {
        [SearchColumnProvider(LightingSearchPaths.k_ReflectionProbeModePath)]
        internal static void ReflectionProbeModeSearchColumnProvider(SearchColumn column)
        {
            column.getter = LightingSearchColumnHelpers.CreateGameObjectGetter<ReflectionProbe>(go => (object)LightingSearchDataAccessors.GetReflectionProbeMode(go));
            column.setter = LightingSearchColumnHelpers.CreateGameObjectSetter(
                (go, v) => LightingSearchDataAccessors.SetReflectionProbeMode(go, (ReflectionProbeMode)v),
                LightingSearchColumnHelpers.IsValidEnum);
            column.cellCreator = _ => new EnumField(ReflectionProbeMode.Realtime) { style = { flexGrow = 1 } };
            column.binder = LightingSearchColumnHelpers.CreateBinderWithComponentCheck<EnumField, ReflectionProbe>(
                (f, v) => f.SetValueWithoutNotify((ReflectionProbeMode)v));
        }

        [SearchColumnProvider(LightingSearchPaths.k_ReflectionProbeResolutionPath)]
        internal static void ReflectionProbeResolutionSearchColumnProvider(SearchColumn column)
        {
            column.getter = LightingSearchColumnHelpers.CreateGameObjectGetter<ReflectionProbe>(go => LightingSearchDataAccessors.GetReflectionProbeResolution(go));
            column.setter = LightingSearchColumnHelpers.CreateGameObjectSetter(
                (go, v) => LightingSearchDataAccessors.SetReflectionProbeResolution(go, (int)v),
                LightingSearchColumnHelpers.IsValidInt);
            column.cellCreator = _ => new PopupField<int>(
                new List<int> { 16, 32, 64, 128, 256, 512, 1024, 2048 },
                0,
                value => value.ToString(),
                value => value.ToString())
            {
                style = { flexGrow = 1 }
            };
            column.binder = LightingSearchColumnHelpers.CreateBinderWithComponentCheck<PopupField<int>, ReflectionProbe>(
                (f, v) => f.SetValueWithoutNotify(Convert.ToInt32(v)));
        }
    }
}
