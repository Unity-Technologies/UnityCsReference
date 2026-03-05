// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable CS0618

namespace UnityEngine.TextCore.Text
{
    /// <summary>
    /// Contains deprecated TextSettings APIs that are being phased out.
    /// </summary>
    public partial class TextSettings
    {
        string m_StyleSheetsResourcePath = "Text Style Sheets/";

        /// <summary>
        /// The Fallback Sprite Assets list is now obsolete. Use the emojiFallbackTextAssets instead.
        /// </summary>
        [Obsolete("The Fallback Sprite Assets list is now obsolete. Use the emojiFallbackTextAssets instead.", true)]
        public List<SpriteAsset> fallbackSpriteAssets
        {
            get => m_FallbackSpriteAssets;
            set => m_FallbackSpriteAssets = value;
        }

        /// <summary>
        /// This property is obsolete and no longer used. It will be removed in a future version.
        /// </summary>
        [Obsolete("styleSheetsResourcePath is no longer used and will be removed in a future version.", false)]
        public string styleSheetsResourcePath
        {
            get => m_StyleSheetsResourcePath;
            set => m_StyleSheetsResourcePath = value;
        }

        /// <summary>
        /// The Font Asset automatically assigned to newly created text objects.
        /// </summary>
        [Obsolete("defaultFontAsset is obsolete and will be removed in a future version.", false)]
        public FontAsset defaultFontAsset
        {
            get => m_DefaultFontAsset;
            set => m_DefaultFontAsset = value;
        }

        /// <summary>
        /// Determines if the text system will use an instance material derived from the primary material preset or use the default material of the fallback font asset.
        /// </summary>
        [Obsolete("matchMaterialPreset is obsolete and will be removed in a future version.", false)]
        public bool matchMaterialPreset
        {
            get => m_MatchMaterialPreset;
            set => m_MatchMaterialPreset = value;
        }

        /// <summary>
        /// The unicode value of the character that will be used when the requested character is missing from the font asset and potential fallbacks.
        /// </summary>
        [Obsolete("missingCharacterUnicode is obsolete and will be removed in a future version.", false)]
        public int missingCharacterUnicode
        {
            get => m_MissingCharacterUnicode;
            set => m_MissingCharacterUnicode = value;
        }

        /// <summary>
        /// Determines if Emoji support is enabled in the Input Field TouchScreenKeyboard.
        /// </summary>
        [Obsolete("enableEmojiSupport is obsolete and will be removed in a future version. It is now support by default with the Advanced Text Generator (ATG).", false)]
        public bool enableEmojiSupport
        {
            get { return m_EnableEmojiSupport; }
            set { m_EnableEmojiSupport = value; }
        }

        /// <summary>
        /// The unicode value of the sprite character that will be used when the requested character sprite is missing from the sprite asset and potential fallbacks.
        /// </summary>
        [Obsolete("missingSpriteCharacterUnicode is obsolete and will be removed in a future version.", false)]
        public uint missingSpriteCharacterUnicode
        {
            get => m_MissingSpriteCharacterUnicode;
            set => m_MissingSpriteCharacterUnicode = value;
        }

        /// <summary>
        /// Text file that contains the line breaking rules for all unicode characters.
        /// </summary>
        [Obsolete("lineBreakingRules is obsolete and will be removed in a future version. It is now support by default with the Advanced Text Generator (ATG).", false)]
        public UnicodeLineBreakingRules lineBreakingRules
        {
            get
            {
                if (m_UnicodeLineBreakingRules == null)
                {
                    m_UnicodeLineBreakingRules = new UnicodeLineBreakingRules();
                    m_UnicodeLineBreakingRules.LoadLineBreakingRules();
                }

                return m_UnicodeLineBreakingRules;
            }
            set => m_UnicodeLineBreakingRules = value;
        }
    }
}

#pragma warning restore CS0618
