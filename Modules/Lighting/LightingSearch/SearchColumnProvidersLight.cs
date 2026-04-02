// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Lighting.LightingSearch
{
    static class SearchColumnProvidersLight
    {
        [SearchColumnProvider(LightingSearchPaths.k_LightTypePath)]
        internal static void LightTypeSearchColumnProvider(SearchColumn column)
        {
            column.getter = LightingSearchColumnHelpers.CreateGameObjectGetter(go => LightingSearchDataAccessors.GetLightType(go));
            column.setter = LightingSearchColumnHelpers.CreateGameObjectSetter(
                (go, v) => LightingSearchDataAccessors.SetLightType(go, (SimplifiedLightType)v),
                v => v is SimplifiedLightType);
            column.cellCreator = _ => new EnumField(SimplifiedLightType.Spot) { style = { flexGrow = 1 } };
            column.binder = (args, ve) =>
            {
                var field = (EnumField)ve;
                var simplifiedType = LightingSearchDataAccessors.ToSimplifiedLightTypeFromValue(args.value);
                if (!simplifiedType.HasValue)
                {
                    var go = LightingSearchColumnHelpers.GetGameObject(args);
                    if (go != null)
                        simplifiedType = LightingSearchDataAccessors.GetLightType(go);
                }
                if (simplifiedType.HasValue)
                {
                    field.visible = true;
                    field.SetValueWithoutNotify(simplifiedType.Value);
                }
                else
                {
                    field.visible = false;
                }
            };
        }

        [SearchColumnProvider(LightingSearchPaths.k_LightModePath)]
        internal static void LightModeSearchColumnProvider(SearchColumn column)
        {
            column.getter = LightingSearchColumnHelpers.CreateGameObjectGetter(go => LightingSearchDataAccessors.GetLightMode(go));
            column.setter = LightingSearchColumnHelpers.CreateGameObjectSetter(
                (go, v) =>
                {
                    if (go.TryGetComponent<Light>(out var light) && LightingSearchDataAccessors.IsAreaLight(light.type))
                        return;
                    LightingSearchDataAccessors.SetLightMode(go, (LightmapBakeType)v);
                },
                LightingSearchColumnHelpers.IsValidEnum);
            column.cellCreator = _ => new EnumField(LightmapBakeType.Realtime) { style = { flexGrow = 1 } };
            column.binder = (args, ve) =>
            {
                var field = (EnumField)ve;
                var go = LightingSearchColumnHelpers.GetGameObject(args);
                if (args.value is LightmapBakeType bakeType)
                {
                    field.visible = true;
                    field.SetValueWithoutNotify(bakeType);
                    bool isAreaLight = go != null && go.TryGetComponent<Light>(out var light) && LightingSearchDataAccessors.IsAreaLight(light.type);
                    field.SetEnabled(!isAreaLight);
                }
                else
                {
                    field.visible = false;
                }
            };
        }

        [SearchColumnProvider(LightingSearchPaths.k_LightColorTemperaturePath)]
        internal static void LightColorTemperatureSearchColumnProvider(SearchColumn column)
        {
            column.getter = LightingSearchColumnHelpers.CreateGameObjectGetter<Light>(go => LightingSearchDataAccessors.GetColorTemperature(go));
            column.setter = LightingSearchColumnHelpers.CreateGameObjectSetter(
                (go, v) => LightingSearchDataAccessors.SetColorTemperature(go, Convert.ToSingle(v)),
                v => v != null);
            column.cellCreator = _ => new FloatField { style = { flexGrow = 1 } };
            column.binder = (args, ve) =>
            {
                var field = (FloatField)ve;
                var go = LightingSearchColumnHelpers.GetGameObject(args);
                if (go == null || !go.TryGetComponent<Light>(out var light))
                {
                    field.visible = false;
                    return;
                }

                if (args.value is float colorTemp)
                {
                    field.visible = true;
                    field.SetEnabled(light.useColorTemperature);
                    field.SetValueWithoutNotify(colorTemp);
                }
                else
                {
                    field.visible = false;
                }
            };
        }
    }
}
