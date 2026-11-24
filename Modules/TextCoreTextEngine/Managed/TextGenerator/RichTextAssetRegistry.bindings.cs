// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.TextCore.Text;

namespace UnityEngine.TextCore
{
    [NativeHeader("Modules/TextCoreTextEngine/Native/RichTextAssetPreload.h")]
    [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
    internal static partial class NativeRichTextAssetRegistry
    {
        [AutoStaticsCleanupOnCodeReload]
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static readonly Dictionary<uint, FontAsset> s_FontAssetCache = new();
        [AutoStaticsCleanupOnCodeReload]
        internal static readonly Dictionary<uint, SpriteAsset> s_SpriteAssetCache = new();
        [AutoStaticsCleanupOnCodeReload]
        internal static readonly Dictionary<uint, TextColorGradient> s_GradientAssetCache = new();

        [ThreadStatic]
        [NoAutoStaticsCleanup] // cleared after each use
        static List<SpriteAsset> s_VisitedSpriteAssets;

        internal delegate bool ExternalSpriteResolver(EntityId textSettingsId, uint assetNameHash, uint spriteNameHash, int spriteIndexHint,
            out int spriteIndex, out EntityId spriteAssetId, out GlyphMetrics metrics, out float scale);

        // Hooks for external text systems (TMP) whose assets are not TextCore types.
        [AutoStaticsCleanupOnCodeReload]
        internal static Func<uint, IntPtr> externalFontAssetResolver;
        [AutoStaticsCleanupOnCodeReload]
        internal static Action<EntityId, string> externalFontAssetLoader;
        [AutoStaticsCleanupOnCodeReload]
        internal static Action<EntityId, string> externalSpriteAssetLoader;
        [AutoStaticsCleanupOnCodeReload]
        internal static ExternalSpriteResolver externalSpriteResolver;

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static unsafe void PreloadAssetsFromTags(NativeTextBuffer textBuffer, TextSettings textSettings)
        {
            if (!textBuffer.isCreated || textBuffer.length == 0) return;

            // Prepare the default sprite asset and its fallbacks for worker threads.
            var instanceDefault = textSettings.defaultSpriteAsset;
            var defaultSpriteAsset = instanceDefault != null ? instanceDefault : TextSettings.s_GlobalSpriteAsset;
            if (defaultSpriteAsset != null)
            {
                defaultSpriteAsset.UpdateLookupTables();
                PrepareSpriteAssetForWorkerThreads(defaultSpriteAsset);
            }

            IntPtr nativeTextSettings = textSettings.nativeTextSettings;
            IntPtr textPtr = (IntPtr)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(textBuffer.buffer);

            PreloadAssetsFromText(textPtr, textBuffer.length, nativeTextSettings);
        }

        [RequiredByNativeCode]
        internal static IntPtr GetFontAssetForNative(uint nameHash)
        {
            if (!s_FontAssetCache.TryGetValue(nameHash, out var fontAsset) || ReferenceEquals(fontAsset, null))
                return externalFontAssetResolver?.Invoke(nameHash) ?? IntPtr.Zero;
            return fontAsset.nativeFontAsset;
        }

        [RequiredByNativeCode]
        internal static IntPtr GetGradientAssetForNative(uint nameHash)
        {
            if (!s_GradientAssetCache.TryGetValue(nameHash, out var gradient) || ReferenceEquals(gradient, null))
                return IntPtr.Zero;
            return gradient.nativeInstance;
        }

        const uint k_FnvOffset = 2166136261u;
        const uint k_FnvPrime = 16777619u;

        const uint k_EmptyNameHash = k_FnvOffset;

        [RequiredByNativeCode]
        internal static int ResolveSpriteForNative(EntityId textSettingsId, uint assetNameHash, uint spriteNameHash, int spriteIndexHint,
            out EntityId spriteAssetId, out GlyphMetrics metrics, out float scale)
        {
            spriteAssetId = default;
            metrics = default;
            scale = 0f;

            if (externalSpriteResolver != null &&
                externalSpriteResolver(textSettingsId, assetNameHash, spriteNameHash, spriteIndexHint, out int externalIndex, out spriteAssetId, out metrics, out scale))
                return externalIndex;

            // Fetch the asset from the TextSettings if name is empty
            SpriteAsset asset;
            if (assetNameHash == k_EmptyNameHash)
            {
                var textSettings = UnityEngine.Object.FindObjectFromEntityIdThreadSafe(textSettingsId) as TextSettings;
                if (textSettings == null)
                    return -1;

                var instanceDefault = textSettings.defaultSpriteAsset;
                asset = !ReferenceEquals(instanceDefault, null)
                    ? instanceDefault
                    : TextSettings.s_GlobalSpriteAsset;
            }
            // Otherwise fetch it from the cache
            else
            {
                s_SpriteAssetCache.TryGetValue(assetNameHash, out asset);
            }

            if (ReferenceEquals(asset, null))
                return -1;

            int index = spriteIndexHint;
            if (index < 0 && spriteNameHash != 0)
            {
                var visited = s_VisitedSpriteAssets ??= new List<SpriteAsset>();
                try
                {
                    asset = SearchForSpriteByNameHash(asset, spriteNameHash, visited, out index);
                }
                finally
                {
                    visited.Clear();
                }
                if (ReferenceEquals(asset, null))
                    return -1;
            }

            var table = asset.m_GlyphIndexLookup != null ? asset.spriteCharacterTable : null;
            int count = table == null ? 0 : table.Count;
            if (index < 0 || index >= count)
                return -1;
            var ch = table![index];
            if (ch == null) return -1;

            spriteAssetId = asset.entityId;
            metrics = ch.glyph != null ? ch.glyph.metrics : default;
            float glyphScale = ch.glyph != null ? ch.glyph.scale : 1f;
            scale = ch.scale * glyphScale;
            return index;
        }

        static SpriteAsset SearchForSpriteByNameHash(SpriteAsset asset, uint spriteNameHash, List<SpriteAsset> visited, out int index)
        {
            visited.Add(asset);

            var table = asset.m_GlyphIndexLookup != null ? asset.spriteCharacterTable : null;
            int count = table == null ? 0 : table.Count;
            for (int i = 0; i < count; i++)
            {
                var sc = table![i];
                if (sc != null && HashName(sc.name) == spriteNameHash)
                {
                    index = i;
                    return asset;
                }
            }

            var fallbacks = asset.fallbackSpriteAssets;
            int fallbackCount = fallbacks == null ? 0 : fallbacks.Count;
            for (int i = 0; i < fallbackCount; i++)
            {
                var fallback = fallbacks![i];
                if (ReferenceEquals(fallback, null) || ContainsReference(visited, fallback) || IsDestroyed(fallback))
                    continue;

                var found = SearchForSpriteByNameHash(fallback, spriteNameHash, visited, out index);
                if (!ReferenceEquals(found, null))
                    return found;
            }

            index = -1;
            return null;
        }

        static void PrepareSpriteAssetForWorkerThreads(SpriteAsset asset, List<SpriteAsset> visited)
        {
            visited.Add(asset);
            if (asset.m_GlyphIndexLookup == null || asset.m_IsSpriteAssetLookupTablesDirty)
                asset.UpdateLookupTables();
            _ = asset.entityId;

            var fallbacks = asset.fallbackSpriteAssets;
            if (fallbacks == null)
                return;

            foreach (var fallback in fallbacks)
            {
                if (fallback != null && !ContainsReference(visited, fallback))
                    PrepareSpriteAssetForWorkerThreads(fallback, visited);
            }
        }

        static void PrepareSpriteAssetForWorkerThreads(SpriteAsset asset)
        {
            var visited = s_VisitedSpriteAssets ??= new List<SpriteAsset>();
            try
            {
                PrepareSpriteAssetForWorkerThreads(asset, visited);
            }
            finally
            {
                visited.Clear();
            }
        }

        static bool IsDestroyed(SpriteAsset asset)
        {
            return ReferenceEquals(UnityEngine.Object.FindObjectFromEntityIdThreadSafe(asset.entityId), null);
        }

        // UnityEngine.Object equality calls into native code, which worker threads can't do.
        static bool ContainsReference(List<SpriteAsset> list, SpriteAsset asset)
        {
            foreach (var item in list)
            {
                if (ReferenceEquals(item, asset))
                    return true;
            }
            return false;
        }

        [RequiredByNativeCode]
        internal static string GetStyleOpeningForNative(EntityId textSettingsId, int styleHash)
        {
            var style = ResolveStyle(textSettingsId, styleHash);
            if (ReferenceEquals(style, null))
                return null;
            // Return a single-NUL placeholder for found-but-empty so the native
            // caller can still distinguish from not-found
            var def = style.styleOpeningDefinition;
            return string.IsNullOrEmpty(def) ? "\0" : def;
        }

        [RequiredByNativeCode]
        internal static string GetStyleClosingForNative(EntityId textSettingsId, int styleHash)
        {
            var style = ResolveStyle(textSettingsId, styleHash);
            return ReferenceEquals(style, null) ? null : style.styleClosingDefinition;
        }

        static TextStyle ResolveStyle(EntityId textSettingsId, int styleHash)
        {
            var ts = UnityEngine.Object.FindObjectFromEntityIdThreadSafe(textSettingsId) as TextSettings;
            if (ts == null) return null;
            var sheet = ts.defaultStyleSheet;
            if (ReferenceEquals(sheet, null)) return null;
            return sheet.GetStyle(styleHash);
        }

        public static uint HashName(ReadOnlySpan<char> name)
        {
            uint hash = k_FnvOffset;
            if (name.IsEmpty)
                return hash;

            for (int i = 0; i < name.Length; i++)
            {
                ushort c = name[i];
                // case-insensitive conversion
                if (c >= 'a' && c <= 'z') c = (ushort)(c - 32);
                hash ^= (byte)(c & 0xFF);
                hash *= k_FnvPrime;
                hash ^= (byte)((c >> 8) & 0xFF);
                hash *= k_FnvPrime;
            }
            return hash;
        }

        [FreeFunction("RichTextAssetPreload::PreloadAssetsFromText", IsThreadSafe = true)]
        internal static extern void PreloadAssetsFromText(IntPtr text, int textLength, IntPtr nativeTextSettings);

        [RequiredByNativeCode]
        internal static unsafe void LoadFontAssetForPreload(EntityId textSettingsId, IntPtr namePtr, int nameLength)
        {
            if (namePtr == IntPtr.Zero || nameLength <= 0) return;
            var name = new ReadOnlySpan<char>((char*)namePtr, nameLength);

            uint hash = HashName(name);
            if (s_FontAssetCache.ContainsKey(hash)) return;

            var textSettings = UnityEngine.Object.FindObjectFromEntityIdThreadSafe(textSettingsId) as TextSettings;
            if (textSettings == null) return;

            string nameStr = name.ToString();
            var fontAsset = Resources.Load<FontAsset>(textSettings.defaultFontAssetPath + nameStr);
            if (fontAsset == null)
            {
                externalFontAssetLoader?.Invoke(textSettingsId, nameStr);
                return;
            }

            fontAsset.EnsureNativeFontAssetIsCreated();
            s_FontAssetCache[hash] = fontAsset;
        }

        [RequiredByNativeCode]
        internal static unsafe void LoadSpriteAssetForPreload(EntityId textSettingsId, IntPtr namePtr, int nameLength)
        {
            if (namePtr == IntPtr.Zero || nameLength <= 0) return;
            var name = new ReadOnlySpan<char>((char*)namePtr, nameLength);

            uint hash = HashName(name);
            if (s_SpriteAssetCache.TryGetValue(hash, out var cachedSpriteAsset))
            {
                if (cachedSpriteAsset != null)
                    PrepareSpriteAssetForWorkerThreads(cachedSpriteAsset);
                return;
            }

            var textSettings = UnityEngine.Object.FindObjectFromEntityIdThreadSafe(textSettingsId) as TextSettings;
            if (textSettings == null) return;

            string nameStr = name.ToString();
            var spriteAsset = Resources.Load<SpriteAsset>(textSettings.defaultSpriteAssetPath + nameStr);
            if (spriteAsset == null)
            {
                externalSpriteAssetLoader?.Invoke(textSettingsId, nameStr);
                return;
            }

            PrepareSpriteAssetForWorkerThreads(spriteAsset);
            s_SpriteAssetCache[hash] = spriteAsset;
        }

        [RequiredByNativeCode]
        internal static unsafe void LoadGradientAssetForPreload(EntityId textSettingsId, IntPtr namePtr, int nameLength)
        {
            if (namePtr == IntPtr.Zero || nameLength <= 0) return;
            var name = new ReadOnlySpan<char>((char*)namePtr, nameLength);

            uint hash = HashName(name);
            if (s_GradientAssetCache.ContainsKey(hash)) return;

            var textSettings = UnityEngine.Object.FindObjectFromEntityIdThreadSafe(textSettingsId) as TextSettings;
            if (textSettings == null) return;

            string nameStr = name.ToString();
            var gradientAsset = Resources.Load<TextColorGradient>(textSettings.defaultColorGradientPresetsPath + nameStr);
            if (gradientAsset == null) return;

            _ = gradientAsset.nativeInstance;
            s_GradientAssetCache[hash] = gradientAsset;
        }

    }
}
