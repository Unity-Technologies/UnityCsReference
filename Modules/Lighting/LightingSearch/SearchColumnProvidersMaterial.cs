// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEditor.Lighting.LightingSearch
{
    static class SearchColumnProvidersMaterial
    {
        internal const string k_EmissiveFilter = "emissive";
        internal const string k_EmissiveRealtimeDirectValue = "realtimedirect";
        internal const string k_EmissiveRealtimeIndirectValue = "realtimeindirect";
        internal const string k_EmissiveBakedValue = "baked";
        internal const string k_EmissiveAnyValue = "any";

        [CustomObjectIndexer(typeof(Material), version = 2)]
        internal static void IndexMaterialEmission(CustomObjectIndexerTarget context, ObjectIndexer indexer)
        {
            if (context.target is not Material material)
                return;

            var flags = material.globalIlluminationFlags;

            var flagValues = new (MaterialGlobalIlluminationFlags flag, string value)[]
            {
                (MaterialGlobalIlluminationFlags.RealtimeDirectEmission, k_EmissiveRealtimeDirectValue),
                (MaterialGlobalIlluminationFlags.RealtimeIndirectEmission, k_EmissiveRealtimeIndirectValue),
                (MaterialGlobalIlluminationFlags.BakedEmission, k_EmissiveBakedValue),
            };

            bool any = false;
            foreach (var (flag, value) in flagValues)
            {
                if ((flags & flag) == flag)
                {
                    indexer.IndexProperty(context.documentIndex, k_EmissiveFilter, value, saveKeyword: true);
                    any = true;
                }
            }

            if (any)
                indexer.IndexProperty(context.documentIndex, k_EmissiveFilter, k_EmissiveAnyValue, saveKeyword: true);
        }

        // URP has its own column provider that also exposes RealtimeDirectEmission.
        static MaterialGlobalIlluminationFlags[] EmissionFlags()
        {
            if (SupportedRenderingFeatures.IsLightmapBakeTypeSupported(LightmapBakeType.Realtime))
            {
                return new[]
                {
                    MaterialGlobalIlluminationFlags.RealtimeIndirectEmission,
                    MaterialGlobalIlluminationFlags.BakedEmission
                };
            }

            return new[] { MaterialGlobalIlluminationFlags.BakedEmission };
        }

        static List<string> EmissionOptions()
        {
            var options = new List<string>();
            foreach (var flag in EmissionFlags())
                options.Add(flag == MaterialGlobalIlluminationFlags.RealtimeIndirectEmission ? "Realtime" : "Baked");
            return options;
        }

        static int FlagsToMask(MaterialGlobalIlluminationFlags flags)
        {
            var emissionFlags = EmissionFlags();
            int mask = 0;
            for (int i = 0; i < emissionFlags.Length; i++)
            {
                if ((flags & emissionFlags[i]) != 0)
                    mask |= 1 << i;
            }
            return mask;
        }

        static MaterialGlobalIlluminationFlags MaskToFlags(MaterialGlobalIlluminationFlags flags, int mask)
        {
            var emissionFlags = EmissionFlags();
            for (int i = 0; i < emissionFlags.Length; i++)
            {
                if ((mask & (1 << i)) != 0)
                    flags |= emissionFlags[i];
                else
                    flags &= ~emissionFlags[i];
            }
            return flags;
        }

        [SearchColumnProvider(LightingSearchPaths.k_MaterialGlobalIlluminationPath)]
        internal static void MaterialGlobalIlluminationSearchColumnProvider(SearchColumn column)
        {
            column.getter = args =>
            {
                var material = args.item.data as Material ?? args.item.ToObject<Material>();
                if (material == null)
                    return null;

                var flags = LightingSearchDataAccessors.GetMaterialGlobalIlluminationFlags(material);
                return FlagsToMask(flags);
            };
            column.setter = args =>
            {
                if (args.value is not int mask)
                    return;

                var material = args.item.data as Material ?? args.item.ToObject<Material>();
                if (material == null)
                    return;

                var flags = LightingSearchDataAccessors.GetMaterialGlobalIlluminationFlags(material);
                flags = MaskToFlags(flags, mask);
                LightingSearchDataAccessors.SetMaterialGlobalIlluminationFlags(material, flags);
            };
            column.cellCreator = _ => new MaskField(EmissionOptions(), 0) { style = { flexGrow = 1 } };
            column.binder = LightingSearchColumnHelpers.CreateBinder<MaskField>((f, v) => f.SetValueWithoutNotify((int)v));
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
