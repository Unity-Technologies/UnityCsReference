// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.TextCore.Text;

namespace UnityEngine.TextCore
{
    [NativeHeader("Modules/TextCoreTextEngine/Native/RichTextAssetPreload.h")]
    [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
    internal static class NativeRichTextAssetRegistry
    {
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static readonly Dictionary<uint, IntPtr> s_FontAssetCache = new();
        internal static readonly Dictionary<uint, SpriteAsset> s_SpriteAssetCache = new();
        internal static readonly Dictionary<uint, TextColorGradient> s_GradientAssetCache = new();

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static unsafe void PreloadAssetsFromTags(NativeTextBuffer textBuffer, TextSettings textSettings)
        {
            if (!textBuffer.isCreated || textBuffer.length == 0) return;

            // Pre-warm the default sprite asset's EntityId on the main thread.
            var instanceDefault = textSettings.defaultSpriteAsset;
            var defaultSpriteAsset = instanceDefault != null ? instanceDefault : TextSettings.s_GlobalSpriteAsset;
            if (defaultSpriteAsset != null)
            {
                defaultSpriteAsset.UpdateLookupTables();
                _ = defaultSpriteAsset.entityId;
            }

            IntPtr nativeTextSettings = textSettings.nativeTextSettings;
            IntPtr textPtr = (IntPtr)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(textBuffer.buffer);

            PreloadAssetsFromText(textPtr, textBuffer.length, nativeTextSettings);
        }

        [RequiredByNativeCode]
        internal static IntPtr GetFontAssetForNative(uint nameHash)
        {
            return s_FontAssetCache.TryGetValue(nameHash, out var ptr) ? ptr : IntPtr.Zero;
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

            // Fetch the asset from the TextSettings if name is empty
            SpriteAsset asset;
            if (assetNameHash == k_EmptyNameHash)
            {
                var textSettings = UnityEngine.Object.FindObjectFromInstanceIDThreadSafe(textSettingsId) as TextSettings;
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

            // Fetch the correct sprite glyph
            var table = asset.spriteCharacterTable;
            int count = table == null ? 0 : table.Count;
            int index = spriteIndexHint;
            if (index < 0 && spriteNameHash != 0)
            {
                for (int i = 0; i < count; i++)
                {
                    var sc = table![i];
                    if (sc != null && HashName(sc.name) == spriteNameHash)
                    {
                        index = i;
                        break;
                    }
                }
            }
            if (index < 0 || index >= count)
                return -1;
            var ch = table![index];
            if (ch == null) return -1;

            spriteAssetId = asset.entityId;
            metrics = ch.glyph != null ? ch.glyph.metrics : default;
            scale = ch.scale;
            return index;
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
            var ts = UnityEngine.Object.FindObjectFromInstanceIDThreadSafe(textSettingsId) as TextSettings;
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

            var textSettings = UnityEngine.Object.FindObjectFromInstanceIDThreadSafe(textSettingsId) as TextSettings;
            if (textSettings == null) return;

            string nameStr = name.ToString();
            var fontAsset = Resources.Load<FontAsset>(textSettings.defaultFontAssetPath + nameStr);
            if (fontAsset == null) return;

            fontAsset.EnsureNativeFontAssetIsCreated();
            s_FontAssetCache[hash] = fontAsset.nativeFontAsset;
        }

        [RequiredByNativeCode]
        internal static unsafe void LoadSpriteAssetForPreload(EntityId textSettingsId, IntPtr namePtr, int nameLength)
        {
            if (namePtr == IntPtr.Zero || nameLength <= 0) return;
            var name = new ReadOnlySpan<char>((char*)namePtr, nameLength);

            uint hash = HashName(name);
            if (s_SpriteAssetCache.ContainsKey(hash)) return;

            var textSettings = UnityEngine.Object.FindObjectFromInstanceIDThreadSafe(textSettingsId) as TextSettings;
            if (textSettings == null) return;

            string nameStr = name.ToString();
            var spriteAsset = Resources.Load<SpriteAsset>(textSettings.defaultSpriteAssetPath + nameStr);
            if (spriteAsset == null) return;

            spriteAsset.UpdateLookupTables();
            _ = spriteAsset.entityId;
            s_SpriteAssetCache[hash] = spriteAsset;
        }

        [RequiredByNativeCode]
        internal static unsafe void LoadGradientAssetForPreload(EntityId textSettingsId, IntPtr namePtr, int nameLength)
        {
            if (namePtr == IntPtr.Zero || nameLength <= 0) return;
            var name = new ReadOnlySpan<char>((char*)namePtr, nameLength);

            uint hash = HashName(name);
            if (s_GradientAssetCache.ContainsKey(hash)) return;

            var textSettings = UnityEngine.Object.FindObjectFromInstanceIDThreadSafe(textSettingsId) as TextSettings;
            if (textSettings == null) return;

            string nameStr = name.ToString();
            var gradientAsset = Resources.Load<TextColorGradient>(textSettings.defaultColorGradientPresetsPath + nameStr);
            if (gradientAsset == null) return;

            _ = gradientAsset.nativeInstance;
            s_GradientAssetCache[hash] = gradientAsset;
        }

    }
}
