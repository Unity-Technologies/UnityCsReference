// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Pool;
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

        public static bool TryRemoveLastRenderingLayerName()
            => TagManager.Internal_TryRemoveLastRenderingLayerName();

        /// <summary>
        /// Retrieves the maximum number of rendering layers supported by the currently active render pipeline.
        /// </summary>
        /// <returns>The maximum number of supported rendering layers, or the default maximum if no specific setting is found.</returns>
        internal static int GetActiveMaxRenderingLayers()
        {
            if (EditorGraphicsSettings.TryGetRenderPipelineSettingsFromInterface<RenderingLayersLimitSettings>(out var settings)
                && settings.Length != 0)
                return settings[0].maxSupportedRenderingLayers;
            return RenderingLayerMask.maxRenderingLayerSize;
        }

        /// <summary>
        /// Gathers the maximum number of rendering layers supported by all configured render pipelines in the graphics settings.
        /// </summary>
        /// <returns>A list of tuples, each containing the maximum number of rendering layers and the name of the corresponding render pipeline type.</returns>
        internal static List<(int, string)> GetMaxRenderingLayersFromSettings()
        {
            using var hashset = HashSetPool<Type>.Get(out var types);

            var result = new List<(int, string)>();
            var renderPipelineAssets = GraphicsSettings.allConfiguredRenderPipelines;
            foreach (var renderPipelineAsset in renderPipelineAssets)
            {
                var pipelineType = GetPipelineTypeFromPipelineAssetType(renderPipelineAsset.GetType());
                if (pipelineType == null || types.Contains(pipelineType))
                    continue;

                if (!EditorGraphicsSettings.TryGetRenderPipelineSettingsFromInterfaceForPipeline<RenderingLayersLimitSettings>(pipelineType, out var settings))
                    continue;

                if (settings.Length == 0)
                    continue;

                types.Add(pipelineType);
                result.Add((settings[0].maxSupportedRenderingLayers, renderPipelineAsset.pipelineType.Name));
            }

            return result;
        }

        /// <summary>
        /// Get names and values for the Rendering Layer Mask dropdown.
        /// </summary>
        /// <param name="currentMask">Current mask value.</param>
        /// <returns>Names and values lists corresponding to the requirements from active Render Pipeline and selected Mask</returns>
        internal static (string[], int[]) GetRenderingLayerNamesAndValuesForMask(uint currentMask)
        {
            var names = RenderingLayerMask.GetDefinedRenderingLayerNames();
            var values = RenderingLayerMask.GetDefinedRenderingLayerValues();

            var currentMax = GetActiveMaxRenderingLayers();
            var isMaskEverything = currentMask == uint.MaxValue;
            var remainingMaskForUnnamedLayers = CalculateMaskOutsideOfDefinedLayers(currentMask, isMaskEverything, values);

            if (currentMax < RenderingLayerMask.maxRenderingLayerSize || remainingMaskForUnnamedLayers != 0)
                (names, values) = ApplyRPLimitAndUnnamedLayers(currentMask, remainingMaskForUnnamedLayers, names, values, currentMax, isMaskEverything);

            return (names, values);
        }

        /// <summary>
        /// Apply Rendering Pipeline limit and add used unnamed layers to the dropdown options.
        /// </summary>
        /// <param name="currentMask">Current selected mask.</param>
        /// <param name="remainingMaskForUnnamedLayers">Mask value for unnamed layers extracted from the current selected mask.</param>
        /// <param name="names">List of defined names in Tags&Layers.</param>
        /// <param name="values">List of values that correspond to the names in Tags&Layers.</param>
        /// <param name="currentMax">Limit of Layers used by active Render Pipeline</param>
        /// <param name="isMaskEverything">True if current mask is everything.</param>
        /// <returns>Modified names and values that includes selected unnamed layers cropped by limit of the active Render Pipeline.</returns>
        static (string[], int[]) ApplyRPLimitAndUnnamedLayers(uint currentMask, uint remainingMaskForUnnamedLayers,
            string[] names, int[] values, int currentMax, bool isMaskEverything)
        {
            {
                using var namesPooledObject = ListPool<string>.Get(out var namesList);
                using var valuesPooledObject = ListPool<int>.Get(out var valuesList);

                // Add defined Layers to the dropdown options if they are selected or if there's no RP limit
                var maxIndex = BitOperationUtils.BitCountToIndex(currentMax);
                for (int i = 0; i < values.Length; i++)
                {
                    var valueUint = unchecked((uint)values[i]);
                    if (BitOperationUtils.IsValueSmallerOrEqualThanIndex(valueUint, maxIndex) ||
                        IsLayerSelected(valueUint))
                    {
                        namesList.Add(names[i]);
                        valuesList.Add(values[i]);
                    }
                }

                // Add used unnamed Layers to the dropdown options
                if (remainingMaskForUnnamedLayers == 0)
                    return (namesList.ToArray(), valuesList.ToArray());

                using var listOfUnnamedBitsPooledObject = ListPool<int>.Get(out var listOfUnnamedBits);

                ConvertUsedUnnamedLayersValuesToLayerIndexes(remainingMaskForUnnamedLayers, listOfUnnamedBits);
                for (int i = 0; i < listOfUnnamedBits.Count; i++)
                {
                    //find an index of the next value that is greater than the current unnamed bit
                    var index = listOfUnnamedBits[i];
                    var indexOfTheNextValue = NextIndexInTheArray(index, valuesList);

                    //insert
                    namesList.Insert(indexOfTheNextValue, $"Unnamed Layer {index}");
                    valuesList.Insert(indexOfTheNextValue, unchecked((int)BitOperationUtils.IndexToValue(index)));
                }

                return (namesList.ToArray(), valuesList.ToArray());
            }

            bool IsLayerSelected(uint valueUint)
            {
                return !isMaskEverything && BitOperationUtils.AnyBitMatch(valueUint, currentMask);
            }
        }

        /// <summary>
        /// Find an index of the next value that is greater than the bit.
        /// </summary>
        /// <param name="layerIndex">Index of the current bit.</param>
        /// <param name="valuesList">Current list of values.</param>
        /// <returns></returns>
        static int NextIndexInTheArray(int layerIndex, List<int> valuesList)
        {
            var indexOfTheNextValue = valuesList.FindIndex(currentValue =>
                BitOperationUtils.IsValueBiggerThanIndex(unchecked((uint)currentValue), layerIndex));
            if (indexOfTheNextValue == -1)
                indexOfTheNextValue = valuesList.Count;
            return indexOfTheNextValue;
        }

        /// <summary>
        /// Convert used unnamed layers from mask value to layer indexes.
        /// </summary>
        /// <param name="remainingMaskForUnnamedLayers">Mask value for unnamed layers.</param>
        /// <param name="listOfUnnamedBits">List of indexes of used unnamed layers.</param>
        static void ConvertUsedUnnamedLayersValuesToLayerIndexes(uint remainingMaskForUnnamedLayers, List<int> listOfUnnamedBits)
        {
            for (int i = 0; i < RenderingLayerMask.maxRenderingLayerSize; i++)
            {
                if (BitOperationUtils.AnyBitMatch(remainingMaskForUnnamedLayers, BitOperationUtils.IndexToValue(i)))
                    listOfUnnamedBits.Add(i);
            }
        }

        /// <summary>
        /// Calculate mask value for unnamed layers if the mask is not full.
        /// </summary>
        /// <param name="currentMask">Specify current mask.</param>
        /// <param name="isMaskEverything">Specify if current mask is Everything.</param>
        /// <param name="values">All named values for Layers in the Tags&Layers.</param>
        /// <returns>Returns 0u if Mask is full or remaining mask after excluding all named values from current Mask.</returns>
        static uint CalculateMaskOutsideOfDefinedLayers(uint currentMask, bool isMaskEverything, int[] values)
        {
            var remainingMask = 0u;
            if (isMaskEverything)
                return remainingMask;

            remainingMask = currentMask;
            for (int i = 0; i < values.Length; i++)
            {
                uint valueUint = unchecked((uint)values[i]);
                if (BitOperationUtils.AnyBitMatch(currentMask, valueUint))
                    remainingMask &= ~valueUint;
            }

            return remainingMask;
        }

        /// <summary>
        /// Checks if a rendering layer mask contains layer bits set outside the maximum allowed bit count.
        /// </summary>
        /// <param name="mask">The rendering layer mask to check.</param>
        /// <param name="bitCount">The maximum bit count allowed for rendering layers.</param>
        /// <returns>A boolean indicating whether the mask includes layer bits beyond the specified maximum.</returns>
        internal static bool DoesMaskContainRenderingLayersOutsideOfMaxBitCount(uint mask, int bitCount) =>
            bitCount != RenderingLayerMask.maxRenderingLayerSize && mask != uint.MaxValue && BitOperationUtils.IsValueBiggerOrEqualThanIndex(mask, bitCount);

        /// <summary>
        /// Constructs a warning message indicating that a rendering layer mask contains layers exceeding the supported range of the active Render Pipeline.
        /// </summary>
        /// <param name="bitCount">The maximum number of layer bits supported by the active Render Pipeline.</param>
        /// <returns>A string with the warning message about layers beyond the supported bit count being ignored.</returns>
        internal static string GetOutsideOfMaxBitCountWarningMessage(int bitCount) =>
            $"Current mask contains layers outside of a supported range by active Render Pipeline. The active Render Pipeline only supports up to {bitCount} layers. Rendering Layers above {bitCount} are ignored.";

        internal static bool SupportPreview(Camera camera, out string reason)
        {
            if (!RenderPipelineManager.isCurrentPipelineValid)
            {
                //Thus we are in Built-in Render Pipeline. Preview are supported here.
                if (camera == null || camera.Equals(null))
                {
                    reason = "Camera is null";
                    return false;
                }
                reason = null;
                return true;
            }

            return RenderPipelineManager.currentPipeline.IsPreviewSupported(camera, out reason);
        }
    }
}
