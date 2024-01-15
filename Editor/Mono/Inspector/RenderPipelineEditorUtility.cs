// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering
{
    public static class RenderPipelineEditorUtility
    {
        public static Type[] GetDerivedTypesSupportedOnCurrentPipeline<T>()
        {
            return TypeCache.GetTypesDerivedFrom<T>()
                .Where(t => t.GetCustomAttribute<SupportedOnRenderPipelineAttribute>() is { isSupportedOnCurrentPipeline: true })
                .ToArray();
        }

        [Obsolete($"{nameof(FetchFirstCompatibleTypeUsingScriptableRenderPipelineExtension)} is deprecated. Use {nameof(GetDerivedTypesSupportedOnCurrentPipeline)} instead. #from(2023.1)", false)]
        public static Type FetchFirstCompatibleTypeUsingScriptableRenderPipelineExtension<TBaseClass>()
        {
            var extensionTypes = TypeCache.GetTypesDerivedFrom<TBaseClass>();

            foreach (Type extensionType in extensionTypes)
            {
#pragma warning disable CS0618
                if (Attribute.GetCustomAttribute(extensionType, typeof(ScriptableRenderPipelineExtensionAttribute)) is ScriptableRenderPipelineExtensionAttribute { inUse: true })
                    return extensionType;
#pragma warning restore CS0618
            }

            return null;
        }

        private static Dictionary<Type, Type> s_RenderPipelineAssetToRenderPipelineType = new();

        public static Type GetPipelineTypeFromPipelineAssetType(Type pipelineAssetType)
        {
            if (!typeof(RenderPipelineAsset).IsAssignableFrom(pipelineAssetType))
                return null;

            if (s_RenderPipelineAssetToRenderPipelineType.TryGetValue(pipelineAssetType, out var pipelineType))
                return pipelineType;

            Type baseGenericType = pipelineAssetType;
            while (baseGenericType != null)
            {
                if (!baseGenericType.IsGenericType || baseGenericType.GetGenericTypeDefinition() != typeof(RenderPipelineAsset<>))
                {
                    baseGenericType = baseGenericType.BaseType;
                    continue;
                }

                pipelineType = baseGenericType.GetGenericArguments()[0];
                s_RenderPipelineAssetToRenderPipelineType[pipelineAssetType] = pipelineType;
                return pipelineType;
            }

            var pipelineAsset = ScriptableObject.CreateInstance(pipelineAssetType) as RenderPipelineAsset;
            pipelineType = pipelineAsset.pipelineType;
            Object.DestroyImmediate(pipelineAsset);
            s_RenderPipelineAssetToRenderPipelineType[pipelineAssetType] = pipelineType;
            return pipelineType;
        }

        public static bool TrySetRenderingLayerName(int index, string name)
            => TagManager.Internal_TrySetRenderingLayerName(index, name);

        public static bool TryAddRenderingLayerName(string name)
            => TagManager.Internal_TryAddRenderingLayerName(name);

        internal static int GetActiveMaxRenderingLayers()
        {
            if (EditorGraphicsSettings.TryGetRenderPipelineSettingsFromInterface<RenderingLayersLimitSettings>(out var settings)
                && settings.Length != 0)
                return settings[0].maxSupportedRenderingLayers;
            return 32;
        }

        internal static List<(int, string)> GetMaxRenderingLayersFromSettings()
        {
            var result = new List<(int, string)>();
            var renderPipelineAssets = GraphicsSettings.allConfiguredRenderPipelines;
            foreach (var renderPipelineAsset in renderPipelineAssets)
            {
                var pipelineType = GetPipelineTypeFromPipelineAssetType(renderPipelineAsset.GetType());
                if (pipelineType == null)
                    continue;

                if (!EditorGraphicsSettings.TryGetRenderPipelineSettingsFromInterfaceForPipeline<RenderingLayersLimitSettings>(pipelineType, out var settings))
                    continue;

                if (settings.Length == 0)
                    continue;

                result.Add((settings[0].maxSupportedRenderingLayers, renderPipelineAsset.pipelineType.Name));
            }

            return result;
        }

        internal static (string[], int[]) GetRenderingLayerNamesAndValuesForMask(uint currentMask)
        {
            var names = RenderingLayerMask.GetDefinedRenderingLayerNames();
            var values = RenderingLayerMask.GetDefinedRenderingLayerValues();

            if (currentMask != uint.MaxValue)
            {
                //calculate remaining mask value
                uint remainingMask = currentMask;
                for (int i = 0; i < values.Length; i++)
                {
                    uint valueUint = unchecked((uint)values[i]);
                    if ((currentMask & valueUint) != 0)
                        remainingMask &= ~valueUint;
                }

                //add remaining mask value to the end of the list
                if (remainingMask != 0)
                {
                    var listOfUnnamedBits = new List<(int, uint)>();
                    for (int i = 0; i < 32; i++)
                    {
                        if ((remainingMask & (1u << i)) != 0)
                            listOfUnnamedBits.Add((i, 1u << i));
                    }

                    var allNames = new List<string>(names.Length + listOfUnnamedBits.Count);
                    var allValues = new List<int>(names.Length + listOfUnnamedBits.Count);
                    allNames.AddRange(names);
                    allValues.AddRange(values);

                    for (int i = 0; i < listOfUnnamedBits.Count; i++)
                    {
                        var bit = listOfUnnamedBits[i];
                        var indexOfTheNextValue = allValues.FindIndex((currentValue) => unchecked((uint)currentValue) > bit.Item2);
                        if (indexOfTheNextValue == -1)
                            indexOfTheNextValue = allValues.Count;

                        //find an index of specific bit value
                        allNames.Insert(indexOfTheNextValue, $"Undefined Layer {bit.Item1}");
                        allValues.Insert(indexOfTheNextValue, unchecked((int)bit.Item2));
                    }
                    names = allNames.ToArray();
                    values = allValues.ToArray();
                }
            }

            return (names, values);
        }
    }
}
