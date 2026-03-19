// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.TextCore.LowLevel;

#pragma warning disable CS0618

namespace UnityEngine.TextCore.Text
{
    public partial class FontAsset
    {
        internal IntPtr nativeFontAsset
        {
            [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
            get
            {
                EnsureNativeFontAssetIsCreated();
                return m_NativeFontAsset;
            }
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal void EnsureNativeFontAssetIsCreated()
        {
            if (m_NativeFontAsset != IntPtr.Zero)
                return;

            // We always try to create the asset on the main thread. If we are here on a job, it means the asset is invalid.
            if (JobsUtility.IsExecutingJob)
                return;

            if (atlasPopulationMode == AtlasPopulationMode.Static && characterTable.Count > 0)
            {
                Debug.LogWarning($"Advanced text system cannot use static font asset {name}.");
                return;
            }

            if (atlasPopulationMode == AtlasPopulationMode.Dynamic && sourceFontFile == null)
            {
                Debug.LogWarning($"{name} FontAsset is invalid. Please assign a Source Font File.");
                return;
            }

            var fallbacks = GetFallbacks();
            var weightFallbacks = GetWeightFallbacks();

            Font sourceFont_editorRef = null;
            sourceFont_editorRef = SourceFont_EditorRef;

            m_NativeFontAsset = Create(faceInfo, sourceFontFile, sourceFont_editorRef, m_SourceFontFilePath, entityId, fallbacks, weightFallbacks.Item1, weightFallbacks.Item2, m_AtlasRenderMode, italicStyleSlant, boldStyleWeight, (int)(boldStyleSpacing * 64.0f), MarshalledUnityObject.MarshalNotNull(this));
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

        internal void UpdateRenderMode()
        {
            UpdateRenderMode(nativeFontAsset, m_AtlasRenderMode);
        }

        internal void UpdateItalicAngle()
        {
            UpdateItalicAngle(nativeFontAsset, italicStyleSlant);
        }

        internal void UpdateBoldWeight()
        {
            UpdateBoldWeight(nativeFontAsset, boldStyleWeight);
        }

        internal void UpdateBoldSpacing()
        {
            UpdateBoldSpacing(nativeFontAsset, (int)(boldStyleSpacing * 64.0f));
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
                    continue;
                }

                fallbackList.Add(fallback.nativeFontAsset);
            }
            return fallbackList.ToArray();
        }

        private static HashSet<EntityId> visitedFontAssets = new HashSet<EntityId>();
        private bool HasRecursion(FontAsset fontAsset)
        {
            visitedFontAssets.Clear();
            return HasRecursionInternal(fontAsset);
        }

        private bool HasRecursionInternal(FontAsset fontAsset)
        {
            // Check if the node has already been visited
            if (visitedFontAssets.Contains(fontAsset.entityId))
            {
                return true;
            }

            // Mark the node as visited
            visitedFontAssets.Add(fontAsset.entityId);

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
            visitedFontAssets.Remove(fontAsset.entityId);

            return false;
        }

        private (IntPtr[], IntPtr[]) GetWeightFallbacks()
        {
            // Font weight fallback arrays must be exactly size 10 to match native mapping
            // Array indices correspond to TextFontWeight enum values as follows:
            // [0] = unused (reserved)
            // [1] = Thin (100)
            // [2] = ExtraLight (200)
            // [3] = Light (300)
            // [4] = Regular (400)
            // [5] = Medium (500)
            // [6] = SemiBold (600)
            // [7] = Bold (700)
            // [8] = Heavy (800)
            // [9] = Black (900)
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

        static extern void UpdateFontEditorRef(IntPtr ptr, Font sourceFont_EditorRef);

        static extern void UpdateFallbacks(IntPtr ptr, IntPtr[] fallbacks);
        static extern void UpdateWeightFallbacks(IntPtr ptr, IntPtr[] regularFallbacks, IntPtr[] italicFallbacks);

        static extern IntPtr Create(FaceInfo faceInfo, Font sourceFontFile, Font sourceFont_EditorRef, string sourceFontFilePath, EntityId fontEntityId, IntPtr[] fallbacks, IntPtr[] weightFallbacks, IntPtr[] italicFallbacks, GlyphRenderMode renderMode, byte italicSlant, float boldWeight, int boldSpacing, IntPtr managedObject);
        static extern void UpdateFaceInfo(IntPtr ptr, FaceInfo faceInfo);
        static extern void UpdateRenderMode(IntPtr ptr, GlyphRenderMode renderMode);
        static extern void UpdateItalicAngle(IntPtr ptr, byte italicAngle);
        static extern void UpdateBoldWeight(IntPtr ptr, float boldWeight);
        static extern void UpdateBoldSpacing(IntPtr ptr, int boldSpacing);

        [FreeFunction("FontAsset::Destroy")]
        static extern void Destroy(IntPtr ptr, IntPtr managedObject);

        // Resetting the Unity FontObject destroys the FontEngine. Is then possible that the hb_face is no longer valid.
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        [FreeFunction("FontAsset::CreateHbFaceIfNeeded")]
        internal static extern void CreateHbFaceIfNeeded();

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(FontAsset fontAsset) => fontAsset.m_NativeFontAsset;
        }
    }
}

#pragma warning restore CS0618

