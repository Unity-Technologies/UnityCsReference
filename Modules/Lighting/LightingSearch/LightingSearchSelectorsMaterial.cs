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
    static class LightingSearchSelectorsMaterial
    {
        [SearchSelector(LightingSearchSelectors.k_MaterialGlobalIlluminationPath, provider: LightingSearchSelectors.k_AssetProvider)]
        static object MaterialGlobalIlluminationSearchSelector(SearchSelectorArgs args)
        {
            var material = args.current.ToObject<Material>();
            if (material == null)
                return null;

            return LightingSearchDataAccessors.GetMaterialGlobalIlluminationFlags(material);
        }

        [SearchColumnProvider(LightingSearchSelectors.k_MaterialGlobalIlluminationPath)]
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
            column.binder = (args, ve) =>
            {
                var field = (EnumField)ve;
                if (args.value is not MaterialGlobalIlluminationDisplay displayValue)
                {
                    field.visible = false;
                    return;
                }

                field.visible = true;
                field.SetValueWithoutNotify(displayValue);
            };
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

        [SearchSelector(LightingSearchSelectors.k_EmissionColorPath, provider: LightingSearchSelectors.k_AssetProvider, cacheable = false)]
        static object EmissionColorSearchSelector(SearchSelectorArgs args)
        {
            var material = args.current.ToObject<Material>();
            if (material == null)
                return null;

            return LightingSearchDataAccessors.GetEmissionColor(material);
        }

        [SearchColumnProvider(LightingSearchSelectors.k_EmissionColorPath)]
        internal static void EmissionColorSearchColumnProvider(SearchColumn column)
        {
            column.getter = args =>
            {
                var material = args.item.data as Material ?? args.item.ToObject<Material>();
                if (material == null)
                    return null;

                return LightingSearchDataAccessors.GetEmissionColor(material);
            };
            column.setter = args =>
            {
                if (args.value is not Color color)
                    return;

                var material = args.item.data as Material ?? args.item.ToObject<Material>();
                if (material == null)
                    return;

                LightingSearchDataAccessors.SetEmissionColor(material, color);
            };
            column.cellCreator = _ => new ColorField { style = { flexGrow = 1 } };
            column.binder = (args, ve) =>
            {
                var field = (ColorField)ve;
                if (args.value is Color color)
                {
                    field.visible = true;
                    field.SetValueWithoutNotify(color);
                }
                else
                {
                    field.visible = false;
                }
            };
        }
    }
}
