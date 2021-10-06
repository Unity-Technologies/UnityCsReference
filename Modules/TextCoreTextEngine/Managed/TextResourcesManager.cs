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

        struct FontAssetRef
        {
            public int nameHashCode;
            public int familyNameHashCode;
            public int styleNameHashCode;
            public long familyNameAndStyleHashCode;
            public readonly FontAsset fontAsset;

            public FontAssetRef(int nameHashCode, int familyNameHashCode, int styleNameHashCode, FontAsset fontAsset)
            {
                this.nameHashCode = nameHashCode;
                this.familyNameHashCode = familyNameHashCode;
                this.styleNameHashCode = styleNameHashCode;
                this.familyNameAndStyleHashCode = (long)styleNameHashCode << 32 | (uint)familyNameHashCode;
                this.fontAsset = fontAsset;
            }
        }

        static readonly Dictionary<int, FontAssetRef> s_FontAssetReferences = new Dictionary<int, FontAssetRef>();
        static readonly Dictionary<int, FontAsset> s_FontAssetNameReferenceLookup = new Dictionary<int, FontAsset>();
        static readonly Dictionary<long, FontAsset> s_FontAssetFamilyNameAndStyleReferenceLookup = new Dictionary<long, FontAsset>();
        static readonly List<int> s_FontAssetRemovalList = new List<int>(16);

        static readonly int k_RegularStyleHashCode = TextUtilities.GetHashCodeCaseInSensitive("Regular");

        /// <summary>
        /// Add font asset to resource manager.
        /// </summary>
        /// <param name="fontAsset">The font asset to be added.</param>
        internal static void AddFontAsset(FontAsset fontAsset)
        {
            int instanceID = fontAsset.instanceID;

            if (!s_FontAssetReferences.ContainsKey(instanceID))
            {
                FontAssetRef fontAssetRef = new FontAssetRef(fontAsset.hashCode, fontAsset.familyNameHashCode, fontAsset.styleNameHashCode, fontAsset);
                s_FontAssetReferences.Add(instanceID, fontAssetRef);

                // Add font asset to name reference lookup
                if (!s_FontAssetNameReferenceLookup.ContainsKey(fontAssetRef.nameHashCode))
                    s_FontAssetNameReferenceLookup.Add(fontAssetRef.nameHashCode, fontAsset);

                // Add font asset to family name and style lookup
                if (!s_FontAssetFamilyNameAndStyleReferenceLookup.ContainsKey(fontAssetRef.familyNameAndStyleHashCode))
                    s_FontAssetFamilyNameAndStyleReferenceLookup.Add(fontAssetRef.familyNameAndStyleHashCode, fontAsset);
            }
            else
            {
                FontAssetRef fontAssetRef = s_FontAssetReferences[instanceID];

                // Return if font asset name, family and style name have not changed.
                if (fontAssetRef.nameHashCode == fontAsset.hashCode && fontAssetRef.familyNameHashCode == fontAsset.familyNameHashCode && fontAssetRef.styleNameHashCode == fontAsset.styleNameHashCode)
                    return;

                // Check if font asset name has changed
                if (fontAssetRef.nameHashCode != fontAsset.hashCode)
                {
                    s_FontAssetNameReferenceLookup.Remove(fontAssetRef.nameHashCode);

                    fontAssetRef.nameHashCode = fontAsset.hashCode;

                    if (!s_FontAssetNameReferenceLookup.ContainsKey(fontAssetRef.nameHashCode))
                        s_FontAssetNameReferenceLookup.Add(fontAssetRef.nameHashCode, fontAsset);
                }

                // Check if family or style name has changed
                if (fontAssetRef.familyNameHashCode != fontAsset.familyNameHashCode || fontAssetRef.styleNameHashCode != fontAsset.styleNameHashCode)
                {
                    s_FontAssetFamilyNameAndStyleReferenceLookup.Remove(fontAssetRef.familyNameAndStyleHashCode);

                    fontAssetRef.familyNameHashCode = fontAsset.familyNameHashCode;
                    fontAssetRef.styleNameHashCode = fontAsset.styleNameHashCode;
                    fontAssetRef.familyNameAndStyleHashCode = (long)fontAsset.styleNameHashCode << 32 | (uint)fontAsset.familyNameHashCode;

                    if (!s_FontAssetFamilyNameAndStyleReferenceLookup.ContainsKey(fontAssetRef.familyNameAndStyleHashCode))
                        s_FontAssetFamilyNameAndStyleReferenceLookup.Add(fontAssetRef.familyNameAndStyleHashCode, fontAsset);
                }

                s_FontAssetReferences[instanceID] = fontAssetRef;
            }
        }

        /// <summary>
        /// Remove font asset from resource manager.
        /// </summary>
        /// <param name="fontAsset">Font asset to be removed from the resource manager.</param>
        internal static void RemoveFontAsset(FontAsset fontAsset)
        {
            //int hashCode = fontAsset.hashCode;

            //if (s_FontAssetReferenceLookup.ContainsKey(hashCode))
            //{
            //    s_FontAssetReferenceLookup.Remove(hashCode);
            //    s_FontAssetReferences.Remove(fontAsset);
            //}
        }

        /// <summary>
        /// Try getting a reference to the font asset using the hash code calculated from its file name.
        /// </summary>
        /// <param name="nameHashcode"></param>
        /// <param name="fontAsset"></param>
        /// <returns></returns>
        internal static bool TryGetFontAssetByName(int nameHashcode, out FontAsset fontAsset)
        {
            fontAsset = null;

            return s_FontAssetNameReferenceLookup.TryGetValue(nameHashcode, out fontAsset);
        }

        /// <summary>
        /// Try getting a reference to the font asset using the hash code calculated from font's family and style name.
        /// </summary>
        /// <param name="familyNameHashCode"></param>
        /// <param name="styleNameHashCode"></param>
        /// <param name="fontAsset"></param>
        /// <returns></returns>
        internal static bool TryGetFontAssetByFamilyName(int familyNameHashCode, int styleNameHashCode, out FontAsset fontAsset)
        {
            fontAsset = null;

            if (styleNameHashCode == 0)
                styleNameHashCode = k_RegularStyleHashCode;

            long familyAndStyleNameHashCode = (long)styleNameHashCode << 32 | (uint)familyNameHashCode;

            return s_FontAssetFamilyNameAndStyleReferenceLookup.TryGetValue(familyAndStyleNameHashCode, out fontAsset);
        }

        /// <summary>
        ///
        /// </summary>
        internal static void RebuildFontAssetCache()
        {
            // Iterate over loaded font assets to update affected font assets
            foreach (var pair in s_FontAssetReferences)
            {
                FontAssetRef fontAssetRef = pair.Value;

                FontAsset fontAsset = fontAssetRef.fontAsset;

                if (fontAsset == null)
                {
                    // Remove font asset from our lookup dictionaries
                    s_FontAssetNameReferenceLookup.Remove(fontAssetRef.nameHashCode);
                    s_FontAssetFamilyNameAndStyleReferenceLookup.Remove(fontAssetRef.familyNameAndStyleHashCode);

                    // Add font asset to our removal list
                    s_FontAssetRemovalList.Add(pair.Key);
                    continue;
                }

                fontAsset.InitializeCharacterLookupDictionary();
                fontAsset.AddSynthesizedCharactersAndFaceMetrics();
            }

            // Remove font assets in our removal list from our font asset references
            for (int i = 0; i < s_FontAssetRemovalList.Count; i++)
            {
                s_FontAssetReferences.Remove(s_FontAssetRemovalList[i]);
            }
            s_FontAssetRemovalList.Clear();

            TextEventManager.ON_FONT_PROPERTY_CHANGED(true, null);
        }
    }
}
