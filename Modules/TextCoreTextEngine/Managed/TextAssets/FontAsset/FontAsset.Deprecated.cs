// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;

#pragma warning disable CS0618

namespace UnityEngine.TextCore.Text
{
    /// <summary>
    /// Contains deprecated FontAsset APIs that are being phased out as ATG (Advanced Text Generator)
    /// becomes the primary text backend. These APIs are no longer required for ATG-based text rendering.
    /// </summary>
    public partial class FontAsset
    {
        [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
        internal Dictionary<uint, Character> m_CharacterLookupDictionary;

        /// <summary>
        /// List containing the characters of the given font asset.
        /// </summary>
        [Obsolete(
            "characterTable is deprecated and will be removed in a future release. Advanced Text Generator (ATG) text backend no longer requires character data.",
            false)]
        public List<Character> characterTable
        {
            get { return m_CharacterTable; }
            internal set { m_CharacterTable = value; }
        }

        /// <summary>
        /// Dictionary used to lookup characters contained in the font asset or its fallbacks by their unicode values.
        /// </summary>
        [Obsolete(
            "characterLookupTable is deprecated and will be removed in a future release. Advanced Text Generator (ATG) text backend no longer requires character data.",
            false)]
        public Dictionary<uint, Character> characterLookupTable
        {
            get
            {
                if (m_CharacterLookupDictionary == null)
                    ReadFontAssetDefinition();

                return m_CharacterLookupDictionary;
            }
        }

        /// <summary>
        /// Table containing the various font features of this font asset.
        /// </summary>
        [Obsolete(
            "Font feature tables and OTL feature tags are obsolete. OpenType layout is now handled natively by Advanced Text Generator (ATG).",
            false)]
        public FontFeatureTable fontFeatureTable
        {
            get { return m_FontFeatureTable; }
            internal set { m_FontFeatureTable = value; }
        }

        /// <summary>
        /// List of glyphs contained in the font asset.
        /// </summary>
        [Obsolete(
            "glyphTable is deprecated and will be removed in a future release.",
            false)]
        public List<Glyph> glyphTable
        {
            get { return m_GlyphTable; }
            internal set { m_GlyphTable = value; }
        }

        /// <summary>
        /// Dictionary used to lookup glyphs contained in the font asset by their index.
        /// </summary>
        [Obsolete(
            "glyphLookupTable is deprecated and will be removed in a future release.",
            false)]
        public Dictionary<uint, Glyph> glyphLookupTable
        {
            get
            {
                if (m_GlyphLookupDictionary == null)
                    ReadFontAssetDefinition();

                return m_GlyphLookupDictionary;
            }
        }
    }
}

#pragma warning restore CS0618
