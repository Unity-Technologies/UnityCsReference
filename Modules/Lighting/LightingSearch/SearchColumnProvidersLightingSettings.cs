// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Lighting.LightingSearch
{
    static class SearchColumnProvidersLightingSettings
    {
        [SearchColumnProvider(LightingSearchPaths.k_MixedLightingModePath)]
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
            column.binder = LightingSearchColumnHelpers.CreateBinder<EnumField>((f, v) => f.SetValueWithoutNotify((MixedLightingMode)v));
        }

        [SearchColumnProvider(LightingSearchPaths.k_LightmapCompressionPath)]
        internal static void LightmapCompressionSearchColumnProvider(SearchColumn column)
        {
            column.getter = LightingSearchColumnHelpers.CreateLightingSettingsGetter(s => (object)LightingSearchDataAccessors.GetLightmapCompression(s));
            column.setter = LightingSearchColumnHelpers.CreateLightingSettingsSetter(
                (s, v) => LightingSearchDataAccessors.SetLightmapCompression(s, (LightmapCompression)v),
                v => v is LightmapCompression);
            column.cellCreator = _ => new EnumField(LightmapCompression.None) { style = { flexGrow = 1 } };
            column.binder = LightingSearchColumnHelpers.CreateBinder<EnumField>((f, v) => f.SetValueWithoutNotify((LightmapCompression)v));
        }
    }
}
