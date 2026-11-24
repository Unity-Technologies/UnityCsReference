// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;

namespace UnityEngine.TextCore.Text
{
    [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
    internal static partial class OSFontFallbackResolver
    {
        // Maps a native OS fallback font (created during itemization on job threads) to the managed FontAsset.
        [AutoStaticsCleanupOnCodeReload]
        static Dictionary<IntPtr, FontAsset> s_PtrToManaged = new Dictionary<IntPtr, FontAsset>();
        [NoAutoStaticsCleanup] // Reusable single-element scratch buffer of value-type NativeTextInfo, cleared at the start of every Resolve; safe to persist across reload
        static List<NativeTextInfo> s_SingleTextInfoBuffer = new(1);

        [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
        internal static bool Resolve(List<NativeTextInfo> textInfos, Dictionary<EntityId, HashSet<uint>> allUniqueMissingGlyphs)
        {
            s_PtrToManaged.Clear();
            ConsumePendingFallbacks();
            return RemapMeshInfos(textInfos, allUniqueMissingGlyphs);
        }

        [VisibleToOtherModules("UnityEngine.IMGUIModule")]
        internal static bool Resolve(NativeTextInfo textInfo, Dictionary<EntityId, HashSet<uint>> allUniqueMissingGlyphs)
        {
            s_SingleTextInfoBuffer.Clear();
            s_SingleTextInfoBuffer.Add(textInfo);
            return Resolve(s_SingleTextInfoBuffer,  allUniqueMissingGlyphs);
        }

        static void ConsumePendingFallbacks()
        {
            int pendingCount = OSFontFallbackBindings.GetPendingFallbackCount();
            if (pendingCount == 0)
                return;

            for (int i = 0; i < pendingCount; i++)
            {
                // The native owner is lost when the font engine is destroyed while the managed copies are kept.
                var fontRef = OSFontFallbackBindings.GetPendingFallbackFontReference(i);
                var managed = TextSettings.FindGlobalOSFallback(fontRef) ?? FontAsset.CreateFontAssetFromFontReference(fontRef);
                if (managed == null)
                    continue;

                var nativePtr = OSFontFallbackBindings.GetPendingFallbackNativePtr(i);
                s_PtrToManaged[nativePtr] = managed;

                TextSettings.RegisterGlobalOSFallback(managed);

                OSFontFallbackBindings.AdoptNativeFallback(nativePtr, managed.GetEntityId());
            }

            OSFontFallbackBindings.ClearPendingFallbacks();
        }

        static bool RemapMeshInfos(List<NativeTextInfo> textInfos, Dictionary<EntityId, HashSet<uint>> allUniqueMissingGlyphs)
        {
            if (s_PtrToManaged.Count == 0)
                return false;

            bool remappedAny = false;
            foreach (var textInfo in textInfos)
            {
                Span<ATGMeshInfo> meshInfos = textInfo.meshInfos;
                for (int i = 0; i < meshInfos.Length; i++)
                {
                    ref var meshInfo = ref meshInfos[i];
                    var rawPtr = (IntPtr)EntityId.ToULong(meshInfo.textAssetId);
                    if (!s_PtrToManaged.TryGetValue(rawPtr, out var managed))
                        continue;

                    // The mesh must point at the managed twin for rendering, whether or not any glyph
                    // is missing - this is what tells the caller a conversion job is still needed.
                    meshInfo.textAssetId = managed.entityId;
                    remappedAny = true;

                    HashSet<uint> missingGlyphs = null;
                    Span<NativeTextElementInfo> elems = meshInfo.textElementInfos;
                    for (int j = 0; j < elems.Length; j++)
                    {
                        uint glyphID = (uint)elems[j].glyphID;
                        if (managed.GetGlyphInCache(glyphID) != null)
                            continue;

                        if (missingGlyphs == null && !allUniqueMissingGlyphs.TryGetValue(managed.entityId, out missingGlyphs))
                        {
                            missingGlyphs = new HashSet<uint>();
                            allUniqueMissingGlyphs[managed.entityId] = missingGlyphs;
                        }
                        missingGlyphs.Add(glyphID);
                    }
                }
            }
            return remappedAny;
        }
    }
}
