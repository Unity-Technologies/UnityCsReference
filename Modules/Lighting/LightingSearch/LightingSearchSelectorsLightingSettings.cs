// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Lighting.LightingSearch
{
    static class LightingSearchSelectorsLightingSettings
    {
        [SearchColumnProvider(LightingSearchSelectors.k_MixedLightingModePath)]
        public static void MixedLightingModeSearchColumnProvider(SearchColumn column)
        {
            column.getter = args =>
            {
                var lightingSettings = args.item.data as LightingSettings ?? args.item.ToObject() as LightingSettings;
                if (lightingSettings == null)
                    return null;

                return LightingSearchDataAccessors.GetMixedLightingMode(lightingSettings);
            };
            column.setter = args =>
            {
                if (args.value == null || !args.value.GetType().IsEnum)
                    return;

                var lightingSettings = args.item.data as LightingSettings ?? args.item.ToObject() as LightingSettings;
                if (lightingSettings == null)
                    return;

                LightingSearchDataAccessors.SetMixedLightingMode(lightingSettings, (MixedLightingMode)args.value);
            };
            column.cellCreator = _ =>
            {
                var enumField = new EnumField { style = { flexGrow = 1 } };
                enumField.Init(MixedLightingMode.IndirectOnly);
                return enumField;
            };
            column.binder = (args, ve) =>
            {
                var field = (EnumField)ve;

                if (args.value != null)
                {
                    field.visible = true;
                    field.SetValueWithoutNotify((MixedLightingMode)args.value);
                }
                else
                {
                    field.visible = false;
                }
            };
        }

        [SearchColumnProvider(LightingSearchSelectors.k_LightmapCompressionPath)]
        internal static void LightmapCompressionSearchColumnProvider(SearchColumn column)
        {
            column.getter = args =>
            {
                var lightingSettings = args.item.data as LightingSettings ?? args.item.ToObject() as LightingSettings;
                if (lightingSettings == null)
                    return null;

                return LightingSearchDataAccessors.GetLightmapCompression(lightingSettings);
            };
            column.setter = args =>
            {
                if (args.value == null || !(args.value is LightmapCompression compression))
                    return;

                var lightingSettings = args.item.data as LightingSettings ?? args.item.ToObject() as LightingSettings;
                if (lightingSettings == null)
                    return;

                LightingSearchDataAccessors.SetLightmapCompression(lightingSettings, compression);
            };
            column.cellCreator = _ => new EnumField(LightmapCompression.None) { style = { flexGrow = 1 } };
            column.binder = (args, ve) =>
            {
                var field = (EnumField)ve;
                if (args.value != null)
                {
                    field.visible = true;
                    field.SetValueWithoutNotify((LightmapCompression)args.value);
                }
                else
                {
                    field.visible = false;
                }
            };
        }
    }
}
