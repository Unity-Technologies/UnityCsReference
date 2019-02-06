// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;


namespace UnityEngine.TextCore
{
    //[CreateAssetMenu(fileName = "TextSettings.asset", menuName = "TextCore/Text Settings", order = 150)]
    [System.Serializable]
    class TextSettings : ScriptableObject
    {
        const string k_DefaultLeadingCharacters = "([｛〔〈《「『【〘〖〝‘“｟«$—…‥〳〴〵\\［（{£¥\"々〇〉》」＄｠￥￦ #";
        const string k_DefaultFollowingCharacters = ")]｝〕〉》」』】〙〗〟’”｠»ヽヾーァィゥェォッャュョヮヵヶぁぃぅぇぉっゃゅょゎゕゖㇰㇱㇲㇳㇴㇵㇶㇷㇸㇹㇺㇻㇼㇽㇾㇿ々〻‐゠–〜?!‼⁇⁈⁉・、%,.:;。！？］）：；＝}¢°\"†‡℃〆％，．";

        static TextSettings s_Instance;

        /// <summary>
        /// The character the will be used as a replacement for missing glyphs in a font asset.
        /// </summary>
        public static int missingGlyphCharacter
        {
            get { return instance.m_missingGlyphCharacter; }
            set { instance.m_missingGlyphCharacter = value; }
        }
        [SerializeField]
        int m_missingGlyphCharacter;

        /// <summary>
        /// Controls the display of warning message in the console.
        /// </summary>
        public static bool warningsDisabled
        {
            get { return instance.m_warningsDisabled; }
            set { instance.m_warningsDisabled = value; }
        }
        [SerializeField]
        bool m_warningsDisabled = true;

        /// <summary>
        /// Returns the Default Font Asset to be used by newly created text objects.
        /// </summary>
        public static FontAsset defaultFontAsset
        {
            get { return instance.m_defaultFontAsset; }
            set { instance.m_defaultFontAsset = value; }
        }
        [SerializeField]
        FontAsset m_defaultFontAsset;

        /// <summary>
        /// The relative path to a Resources folder in the project.
        /// </summary>
        public static string defaultFontAssetPath
        {
            get { return instance.m_defaultFontAssetPath; }
            set { instance.m_defaultFontAssetPath = value; }
        }
        [SerializeField]
        string m_defaultFontAssetPath;

        /// <summary>
        /// Returns the list of Fallback Fonts defined in the Text Settings file.
        /// </summary>
        public static List<FontAsset> fallbackFontAssets
        {
            get { return instance.m_fallbackFontAssets; }
            set { instance.m_fallbackFontAssets = value; }
        }
        [SerializeField]
        List<FontAsset> m_fallbackFontAssets;

        /// <summary>
        /// Controls whether or not TextCore will create a matching material preset or use the default material of the fallback font asset.
        /// </summary>
        public static bool matchMaterialPreset
        {
            get { return instance.m_matchMaterialPreset; }
            set { instance.m_matchMaterialPreset = value; }
        }
        [SerializeField]
        bool m_matchMaterialPreset;

        /// <summary>
        /// The Default Sprite Asset to be used by default.
        /// </summary>
        public static TextSpriteAsset defaultSpriteAsset
        {
            get { return instance.m_defaultSpriteAsset; }
            set { instance.m_defaultSpriteAsset = value; }
        }
        [SerializeField]
        TextSpriteAsset m_defaultSpriteAsset;

        /// <summary>
        /// The relative path to a Resources folder in the project.
        /// </summary>
        public static string defaultSpriteAssetPath
        {
            get { return instance.m_defaultSpriteAssetPath; }
            set { instance.m_defaultSpriteAssetPath = value; }
        }
        [SerializeField]
        string m_defaultSpriteAssetPath;

        /// <summary>
        /// The relative path to a Resources folder in the project that contains Color Gradient Presets.
        /// </summary>
        public static string defaultColorGradientPresetsPath
        {
            get { return instance.m_defaultColorGradientPresetsPath; }
            set { instance.m_defaultColorGradientPresetsPath = value; }
        }
        [SerializeField]
        string m_defaultColorGradientPresetsPath;

        /// <summary>
        /// The Default Style Sheet used by the text objects.
        /// </summary>
        public static TextStyleSheet defaultStyleSheet
        {
            get { return instance.m_defaultStyleSheet; }
            set
            {
                instance.m_defaultStyleSheet = value;
                TextStyleSheet.LoadDefaultStyleSheet();
            }
        }
        [SerializeField]
        TextStyleSheet m_defaultStyleSheet;

        /// <summary>
        /// Text file that contains the leading characters used for line breaking for Asian languages.
        /// </summary>
        [SerializeField]
        UnityEngine.TextAsset m_leadingCharacters = null;

        /// <summary>
        /// Text file that contains the following characters used for line breaking for Asian languages.
        /// </summary>
        [SerializeField]
        UnityEngine.TextAsset m_followingCharacters = null;

        /// <summary>
        ///
        /// </summary>
        public static LineBreakingTable linebreakingRules
        {
            get
            {
                if (instance.m_linebreakingRules == null)
                    LoadLinebreakingRules();

                return instance.m_linebreakingRules;
            }
        }
        [SerializeField]
        LineBreakingTable m_linebreakingRules;

        /// <summary>
        /// Get a singleton instance of the settings class.
        /// </summary>
        public static TextSettings instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = Resources.Load<TextSettings>("TextSettings") ?? CreateInstance<TextSettings>();
                }

                return s_Instance;
            }
        }

        public static void LoadLinebreakingRules()
        {
            if (instance == null) return;

            if (s_Instance.m_linebreakingRules == null)
                s_Instance.m_linebreakingRules = new LineBreakingTable();

            s_Instance.m_linebreakingRules.leadingCharacters = s_Instance.m_leadingCharacters != null ? GetCharacters(s_Instance.m_leadingCharacters.text) : GetCharacters(k_DefaultLeadingCharacters);
            s_Instance.m_linebreakingRules.followingCharacters = s_Instance.m_followingCharacters != null ? GetCharacters(s_Instance.m_followingCharacters.text) : GetCharacters(k_DefaultFollowingCharacters);
        }

        /// <summary>
        ///  Get the characters from the line breaking files
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        static Dictionary<int, char> GetCharacters(string text)
        {
            Dictionary<int, char> dict = new Dictionary<int, char>();

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                // Check to make sure we don't include duplicates
                if (dict.ContainsKey((int)c) == false)
                {
                    dict.Add((int)c, c);
                }
            }

            return dict;
        }

        public class LineBreakingTable
        {
            public Dictionary<int, char> leadingCharacters;
            public Dictionary<int, char> followingCharacters;
        }
    }
}
