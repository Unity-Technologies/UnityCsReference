// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.TextCore.Text
{
    public partial class FontAsset
    {
        internal IntPtr nativeFontAsset
        {
            [VisibleToOtherModules("UnityEngine.UIElementsModule")]
            get
            {
                if (m_NativeFontAsset == IntPtr.Zero)
                {
                    var fallbacks = GetFallbacks();
                    var weightFallbacks = GetWeightFallbacks();

                    kFontAssetByInstanceId.TryAdd(instanceID, this);
                    Font sourceFont_editorRef = null;
                    sourceFont_editorRef = SourceFont_EditorRef;
                    m_NativeFontAsset = Create(faceInfo, sourceFontFile, sourceFont_editorRef, m_SourceFontFilePath, instanceID, fallbacks, weightFallbacks.Item1, weightFallbacks.Item2);
                }
                return m_NativeFontAsset;
            }
        }
        internal void UpdateFontEditorRef()
        {
            UpdateFontEditorRef(nativeFontAsset, SourceFont_EditorRef);
        }
        internal void UpdateFallbacks()
        {
            UpdateFallbacks(nativeFontAsset, GetFallbacks());
        }

        internal void UpdateWeightFallbacks()
        {
            var weights = GetWeightFallbacks();
            UpdateWeightFallbacks(nativeFontAsset, weights.Item1, weights.Item2);
        }

        internal void UpdateFaceInfo()
        {
            UpdateFaceInfo(nativeFontAsset, faceInfo);
        }

        internal IntPtr[] GetFallbacks()
        {
            List<IntPtr> fallbackList = new List<IntPtr>();
            if (fallbackFontAssetTable == null)
                return fallbackList.ToArray();

            foreach (var fallback in fallbackFontAssetTable)
            {
                if (fallback == null)
                    continue;

                if (fallback.atlasPopulationMode == AtlasPopulationMode.Static && fallback.characterTable.Count > 0)
                {
                    Debug.LogWarning($"Advanced text system cannot use static font asset {fallback.name} as fallback.");
                    continue;
                }

                if (HasRecursion(fallback))
                {
                    Debug.LogWarning($"Circular reference detected. Cannot add {fallback.name} to the fallbacks.");
                    continue;
                }

                fallbackList.Add(fallback.nativeFontAsset);
            }
            return fallbackList.ToArray();
        }


        private static HashSet<int> visitedFontAssets = new HashSet<int>();
        private bool HasRecursion(FontAsset fontAsset)
        {
            visitedFontAssets.Clear();
            return HasRecursionInternal(fontAsset);
        }

        private bool HasRecursionInternal(FontAsset fontAsset)
        {
            // Check if the node has already been visited
            if (visitedFontAssets.Contains(fontAsset.instanceID))
            {
                return true;
            }

            // Mark the node as visited
            visitedFontAssets.Add(fontAsset.instanceID);

            if (fontAsset.fallbackFontAssetTable != null)
            {
                // Recursively check children for recursion
                foreach (var child in fontAsset.fallbackFontAssetTable)
                {
                    if (HasRecursionInternal(child))
                    {
                        return true;
                    }
                }
            }

            // Recursively check children for recursion
            for (int i = 0; i < fontAsset.fontWeightTable.Length; i++)
            {
                var pair = fontAsset.fontWeightTable[i];
                if (pair.regularTypeface != null)
                {
                    if (HasRecursionInternal(pair.regularTypeface))
                    {
                        return true;
                    }
                }

                if (pair.italicTypeface != null)
                {
                    if (HasRecursionInternal(pair.italicTypeface))
                    {
                        return true;
                    }
                }
            }

            // Remove the node from the visited set when backtracking
            visitedFontAssets.Remove(fontAsset.instanceID);

            return false;
        }

        private (IntPtr[], IntPtr[]) GetWeightFallbacks()
        {
            IntPtr[] regularTypefaces = new IntPtr[10];
            IntPtr[] italicTypefaces = new IntPtr[10];

            for (int i = 0; i < fontWeightTable.Length; i++)
            {
                var pair = fontWeightTable[i];
                if (pair.regularTypeface != null)
                {
                    if (pair.regularTypeface.atlasPopulationMode == AtlasPopulationMode.Static && pair.regularTypeface.characterTable.Count > 0)
                    {
                        Debug.LogWarning($"Advanced text system cannot use static font asset {pair.regularTypeface.name} as fallback.");
                        continue;
                    }
                    if (HasRecursion(pair.regularTypeface))
                    {
                        Debug.LogWarning($"Circular reference detected. Cannot add {pair.regularTypeface.name} to the fallbacks.");
                        continue;
                    }
                    regularTypefaces[i] = pair.regularTypeface.nativeFontAsset;
                }

                if (pair.italicTypeface != null)
                {
                    if (pair.italicTypeface.atlasPopulationMode == AtlasPopulationMode.Static && pair.italicTypeface.characterTable.Count > 0)
                    {
                        Debug.LogWarning($"Advanced text system cannot use static font asset {pair.italicTypeface.name} as fallback.");
                        continue;
                    }
                    if (HasRecursion(pair.italicTypeface))
                    {
                        Debug.LogWarning($"Circular reference detected. Cannot add {pair.italicTypeface.name} to the fallbacks.");
                        continue;
                    }
                    italicTypefaces[i] = pair.italicTypeface.nativeFontAsset;
                }
            }

            return (regularTypefaces, italicTypefaces);
        }

        private static extern void UpdateFontEditorRef(IntPtr ptr, Font sourceFont_EditorRef);
        private static extern void UpdateFallbacks(IntPtr ptr, IntPtr[] fallbacks);
        private static extern void UpdateWeightFallbacks(IntPtr ptr, IntPtr[] regularFallbacks, IntPtr[] italicFallbacks);

        private static extern IntPtr Create(FaceInfo faceInfo, Font sourceFontFile, Font sourceFont_EditorRef, string sourceFontFilePath, int fontInstanceID, IntPtr[] fallbacks, IntPtr[] weightFallbacks, IntPtr[] italicFallbacks);
        private static extern void UpdateFaceInfo(IntPtr ptr, FaceInfo faceInfo);

        [FreeFunction("FontAsset::Destroy")]
        private static extern void Destroy(IntPtr ptr);

        ~FontAsset()
        {
            GC.SuppressFinalize(this);
        }

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(FontAsset fontAsset) => fontAsset.m_NativeFontAsset;
        }
    }
}

