// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.TextCore.Text;

namespace UnityEngine
{
    internal class IMGUITextHandle : TextHandle
    {
        const float sFallbackFontSize = 13;
        const float sTimeToFlush = 1.0f;
        const string kDefaultFontName = "LegacyRuntime.ttf";

        internal static Func<Object> GetEditorTextSettings;
        internal static Func<string, float> GetEditorTextSharpness;
        internal static Func<Font> GetEditorFont;

        private static IMGUITextHandle s_TextHandle = new IMGUITextHandle();

        private static List<IMGUITextHandle> textHandles = new List<IMGUITextHandle>();

        internal static void EmptyCache()
        {
            GUIStyle.Internal_CleanupAllTextGenerator();
            textHandles.Clear();
        }

        internal static IMGUITextHandle GetTextHandle(GUIStyle style, Rect position, string content, Color32 textColor, bool isOnlyForGeometry = false)
        {
            var settings = new TextCore.Text.TextGenerationSettings();
            ConvertGUIStyleToGenerationSettings(settings, style, textColor, content, position);
            return GetTextHandle(settings, isOnlyForGeometry);
        }

        private static IMGUITextHandle GetTextHandle(TextCore.Text.TextGenerationSettings settings, bool isOnlyForGeometry)
        {
            var currentTime = Time.realtimeSinceStartup;
            bool isCached = false;
            IMGUITextHandle textHandleCached = null;
            int hash = isOnlyForGeometry ? settings.cachedGeomertyHashCode : settings.cachedHashCode;

            for (int i = textHandles.Count - 1; i >= 0; i--)
            {
                var textHandle = textHandles[i];
                var hash2 = isOnlyForGeometry ? textHandle.textGenerationSettings.cachedGeomertyHashCode : textHandle.textGenerationSettings.cachedHashCode;
                if (hash == hash2)
                {
                    textHandleCached = textHandle;
                    textHandle.lastTimeUsed = currentTime;
                    isCached = true;
                }

                if (currentTime - textHandle.lastTimeUsed > sTimeToFlush)
                {
                    GUIStyle.Internal_DestroyTextGenerator(textHandle.textGenerationSettings.cachedHashCode);
                    textHandles.RemoveAt(i);
                }
            }

            if (isCached)
                return textHandleCached;

            var handle = new IMGUITextHandle();
            handle.lastTimeUsed = currentTime;
            textHandles.Add(handle);
            handle.Update(settings);
            handle.UpdatePreferredSize(settings);
            return handle;
        }

        internal static float GetLineHeight(GUIStyle style)
        {
            var settings = new TextCore.Text.TextGenerationSettings();
            ConvertGUIStyleToGenerationSettings(settings, style, Color.white, "", Rect.zero);
            return GetLineHeightDefault(settings);
        }


        internal TextInfo GetTextInfo(ref int id)
        {
            id = textGenerationSettings.cachedHashCode;
            return textInfo;
        }

        internal Vector2 GetPreferredSize()
        {
            return preferredSize;
        }

        internal int GetNumCharactersThatFitWithinWidth(float width)
        {
            int characterCount = textInfo.lineInfo[0].characterCount;
            int charCount;
            float currentSize = 0;

            for (charCount = 0; charCount < characterCount; charCount++)
            {
                currentSize += textInfo.textElementInfo[charCount].xAdvance - textInfo.textElementInfo[charCount].origin;
                if (currentSize > width)
                {
                    break;
                }
            }

            return charCount;
        }

        private static void ConvertGUIStyleToGenerationSettings(UnityEngine.TextCore.Text.TextGenerationSettings settings, GUIStyle style, Color textColor, string text, Rect rect)
        {
            if (settings.textSettings == null)
            {
                settings.textSettings = (TextSettings)GetEditorTextSettings();

                if (settings.textSettings == null)
                    return;
            }

            Font font = style.font;

            if (!font)
            {
                font = GetEditorFont();
            }

            settings.fontAsset = settings.textSettings.GetCachedFontAsset(font, TextShaderUtilities.ShaderRef_MobileSDF_IMGUI);

            if (settings.fontAsset == null)
                return;

            settings.material = settings.fontAsset.material;

            // We only want to update the sharpness of the text in the editor with those preferences
            settings.fontAsset.material.SetFloat("_Sharpness", GetEditorTextSharpness(style.font ? style.font.name : GetEditorFont().name));

            settings.screenRect = new Rect(0, 0, rect.width, rect.height);
            settings.text = text;

            settings.fontStyle = TextGeneratorUtilities.LegacyStyleToNewStyle(style.fontStyle);
            settings.textAlignment = TextGeneratorUtilities.LegacyAlignmentToNewAlignment(style.alignment);
            settings.wordWrap = rect.width > 0 ? style.wordWrap : false;
            settings.wordWrappingRatio = 0.4f;
            settings.richText = style.richText;
            settings.parseControlCharacters = false;

            if (style.fontSize > 0)
                settings.fontSize = style.fontSize;
            else if (style.font)
                settings.fontSize = style.font.fontSize;
            else
                settings.fontSize = sFallbackFontSize;

            settings.overflowMode = TextOverflowMode.Overflow;
            settings.characterSpacing = 0;
            settings.wordSpacing = 0;
            settings.paragraphSpacing = 0;
            settings.color = textColor;

            settings.inverseYAxis = true;
            settings.shouldConvertToLinearSpace = false;
        }
    }
}
