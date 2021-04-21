// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;


namespace UnityEngine.TextCore.Text
{
    /// <summary>
    ///
    /// </summary>
    internal class TextResourceManager
    {
        // ======================================================
        // TEXT SETTINGS MANAGEMENT
        // ======================================================

        //private static TextSettings s_TextSettings;

        // internal static TextSettings GetTextSettings()
        // {
        //     if (s_TextSettings == null)
        //     {
        //         // Try loading the TMP Settings from a Resources folder in the user project.
        //         s_TextSettings = Resources.Load<TextSettings>("TextSettings"); // ?? ScriptableObject.CreateInstance<TMP_Settings>();
        //
        //         #if UNITY_EDITOR
        //         if (s_TextSettings == null)
        //         {
        //             // Open TMP Resources Importer to enable the user to import the TMP Essential Resources and option TMP Examples & Extras
        //             TMP_PackageResourceImporterWindow.ShowPackageImporterWindow();
        //         }
        //         #endif
        //     }
        //
        //     return s_TextSettings;
        // }

        // ======================================================
        // FONT ASSET MANAGEMENT - Fields, Properties and Functions
        // ======================================================

        private static readonly List<FontAsset> s_FontAssetReferences = new List<FontAsset>();
        private static readonly Dictionary<int, FontAsset> s_FontAssetReferenceLookup = new Dictionary<int, FontAsset>();

        /// <summary>
        ///
        /// </summary>
        /// <param name="fontAsset"></param>
        internal static void AddFontAsset(FontAsset fontAsset)
        {
            int hashcode = fontAsset.hashCode;

            if (s_FontAssetReferenceLookup.ContainsKey(hashcode))
                return;

            s_FontAssetReferences.Add(fontAsset);
            s_FontAssetReferenceLookup.Add(hashcode, fontAsset);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="instanceID"></param>
        // internal static void RemoveFontAssetInstance(int instanceID)
        // {
        //     int length = s_FontAssetReferences.Count;
        //     for (int i = 0; i < length; i++)
        //     {
        //         if (s_FontAssetReferences[i].instanceID == instanceID)
        //         {
        //             s_FontAssetReferences.RemoveAt(i);
        //             break;
        //         }
        //     }
        // }

        /// <summary>
        ///
        /// </summary>
        /// <param name="hashcode"></param>
        /// <param name="fontAsset"></param>
        /// <returns></returns>
        // public static bool TryGetFontAsset(int hashcode, out FontAsset fontAsset)
        // {
        //     fontAsset = null;
        //
        //     return s_FontAssetReferenceLookup.TryGetValue(hashcode, out fontAsset);
        // }

        internal static void RebuildFontAssetCache()
        {
            // Iterate over loaded font assets to update affected font assets
            for (int i = 0; i < s_FontAssetReferences.Count; i++)
            {
                if (s_FontAssetReferences[i] == null)
                {
                    s_FontAssetReferences.RemoveAt(i);
                    continue;
                }

                s_FontAssetReferences[i].InitializeCharacterLookupDictionary();
            }

            TextEventManager.ON_FONT_PROPERTY_CHANGED(true, null);
        }

        // internal static void RebuildFontAssetCache(int instanceID)
        // {
        //     // Iterate over loaded font assets to update affected font assets
        //     for (int i = 0; i < s_FontAssetReferences.Count; i++)
        //     {
        //         TMP_FontAsset fontAsset = s_FontAssetReferences[i];
        //
        //         if (fontAsset.FallbackSearchQueryLookup.Contains(instanceID))
        //             fontAsset.ReadFontAssetDefinition();
        //     }
        // }
    }
}
