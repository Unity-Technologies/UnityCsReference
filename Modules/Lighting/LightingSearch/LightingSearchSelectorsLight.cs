// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Lighting.LightingSearch
{
    static class LightingSearchSelectorsLight
    {
        [SearchColumnProvider(LightingSearchSelectors.k_LightTypePath)]
        internal static void LightTypeSearchColumnProvider(SearchColumn column)
        {
            column.getter = args =>
            {
                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return null;

                if (!go.TryGetComponent<Light>(out var light))
                    return null;

                return GetSimplifiedLightType(light.type);
            };
            column.setter = args =>
            {
                if (args.value == null || !args.value.GetType().IsEnum)
                    return;

                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return;

                if (!go.TryGetComponent<Light>(out var light))
                    return;

                var simplifiedType = (SimplifiedLightType)args.value;
                light.type = (LightType)simplifiedType;
                
                // Mark the light component as dirty so other columns (Shape, Intensity, Unit) refresh
                // when the light type changes.
                EditorUtility.SetDirty(light);
            };
            column.cellCreator = _ => new EnumField(SimplifiedLightType.Spot) { style = { flexGrow = 1 } };
            column.binder = (args, ve) =>
            {
                var field = (EnumField)ve;
                if (args.value is SimplifiedLightType lightType)
                {
                    field.visible = true;
                    field.SetValueWithoutNotify(lightType);
                }
                else
                {
                    field.visible = false;
                }
            };
        }

        static SimplifiedLightType GetSimplifiedLightType(LightType lightType)
        {
            return lightType switch
            {
                LightType.Spot or LightType.Pyramid or LightType.Box => SimplifiedLightType.Spot,
                LightType.Directional => SimplifiedLightType.Directional,
                LightType.Point => SimplifiedLightType.Point,
                LightType.Rectangle or LightType.Disc or LightType.Tube => SimplifiedLightType.Area,
                _ => SimplifiedLightType.Spot
            };
        }
    }
}
