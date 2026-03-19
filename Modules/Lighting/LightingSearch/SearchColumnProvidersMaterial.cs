// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEditor.Lighting.LightingSearch
{
    static class SearchColumnProvidersMaterial
    {
        [SearchColumnProvider(LightingSearchPaths.k_MaterialGlobalIlluminationPath)]
        internal static void MaterialGlobalIlluminationSearchColumnProvider(SearchColumn column)
        {
            column.getter = args =>
            {
                var material = args.item.data as Material ?? args.item.ToObject<Material>();
                if (material == null)
                    return null;

                var flags = LightingSearchDataAccessors.GetMaterialGlobalIlluminationFlags(material);
                return MapToDisplayEnum(flags);
            };
            column.setter = args =>
            {
                if (args.value == null || !(args.value is MaterialGlobalIlluminationDisplay))
                    return;

                var material = args.item.data as Material ?? args.item.ToObject<Material>();
                if (material == null)
                    return;

                var displayValue = (MaterialGlobalIlluminationDisplay)args.value;
                var flags = MapFromDisplayEnum(displayValue);
                LightingSearchDataAccessors.SetMaterialGlobalIlluminationFlags(material, flags);
            };
            column.cellCreator = _ =>
            {
                var field = new EnumField { style = { flexGrow = 1 } };
                field.Init(MaterialGlobalIlluminationDisplay.None);
                return field;
            };
            column.binder = LightingSearchColumnHelpers.CreateBinder<EnumField>((f, v) => f.SetValueWithoutNotify((MaterialGlobalIlluminationDisplay)v));
        }

        static MaterialGlobalIlluminationDisplay MapToDisplayEnum(MaterialGlobalIlluminationFlags flags)
        {
            if ((flags & MaterialGlobalIlluminationFlags.BakedEmissive) != 0)
                return MaterialGlobalIlluminationDisplay.Baked;
            if ((flags & MaterialGlobalIlluminationFlags.RealtimeEmissive) != 0)
                return MaterialGlobalIlluminationDisplay.Realtime;
            return MaterialGlobalIlluminationDisplay.None;
        }

        static MaterialGlobalIlluminationFlags MapFromDisplayEnum(MaterialGlobalIlluminationDisplay display)
        {
            return display switch
            {
                MaterialGlobalIlluminationDisplay.Baked => MaterialGlobalIlluminationFlags.BakedEmissive,
                MaterialGlobalIlluminationDisplay.Realtime => MaterialGlobalIlluminationFlags.RealtimeEmissive,
                MaterialGlobalIlluminationDisplay.None => MaterialGlobalIlluminationFlags.None,
                _ => MaterialGlobalIlluminationFlags.None
            };
        }

        [SearchColumnProvider(LightingSearchPaths.k_EmissionColorPath)]
        internal static void EmissionColorSearchColumnProvider(SearchColumn column)
        {
            column.getter = LightingSearchColumnHelpers.CreateMaterialGetter(m => LightingSearchDataAccessors.GetEmissionColor(m));
            column.setter = LightingSearchColumnHelpers.CreateMaterialSetter(
                (m, v) => LightingSearchDataAccessors.SetEmissionColor(m, (Color)v),
                v => v is Color);
            column.cellCreator = _ => new ColorField { style = { flexGrow = 1 } };
            column.binder = LightingSearchColumnHelpers.CreateBinder<ColorField>((f, v) => f.SetValueWithoutNotify((Color)v));
        }
    }
}
