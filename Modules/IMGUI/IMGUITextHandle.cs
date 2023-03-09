// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.TextCore.Text;

namespace UnityEngine
{
    internal class IMGUITextHandle : TextHandle
    {
        internal LinkedListNode<TextHandleTuple> tuple;

        const float sFallbackFontSize = 13;
        const float sTimeToFlush = 1.0f;
        const string kDefaultFontName = "LegacyRuntime.ttf";

        internal static Func<Object> GetEditorTextSettings;
        internal static Func<string, float> GetEditorTextSharpness;
        internal static Func<Font> GetEditorFont;

        private static IMGUITextHandle s_TextHandle = new IMGUITextHandle();

        private static Dictionary<int, IMGUITextHandle> textHandles = new Dictionary<int, IMGUITextHandle>();
        private static LinkedList<TextHandleTuple> textHandlesTuple = new LinkedList<TextHandleTuple>();
        private static float lastCleanupTime;

        internal class TextHandleTuple
        {
            public TextHandleTuple(float lastTimeUsed, int hashCode)
            {
                this.hashCode = hashCode;
                this.lastTimeUsed = lastTimeUsed;
            }

            public float lastTimeUsed;
            public int hashCode;
        }

        internal static void EmptyCache()
        {
            GUIStyle.Internal_CleanupAllTextGenerator();
            textHandles.Clear();
            textHandlesTuple.Clear();
        }

        internal static IMGUITextHandle GetTextHandle(GUIStyle style, Rect position, string content, Color32 textColor)
        {
            var settings = new TextCore.Text.TextGenerationSettings();
            ConvertGUIStyleToGenerationSettings(settings, style, textColor, content, position);
            return GetTextHandle(settings);
        }

        private static void ClearUnusedTextHandles()
        {
            var currentTime = Time.realtimeSinceStartup;
            while (textHandlesTuple.Count > 0)
            {
                var tuple = textHandlesTuple.First();
                if (currentTime - tuple.lastTimeUsed > sTimeToFlush)
                {
                    GUIStyle.Internal_DestroyTextGenerator(tuple.hashCode);
                    textHandles.Remove(tuple.hashCode);
                    textHandlesTuple.RemoveFirst();
                }
                else
                    break;
            }
        }

        private static IMGUITextHandle GetTextHandle(TextCore.Text.TextGenerationSettings settings)
        {
            var currentTime = Time.realtimeSinceStartup;
            if (currentTime - lastCleanupTime > sTimeToFlush)
            {
                ClearUnusedTextHandles();
                lastCleanupTime = currentTime;
            }

            int hash = settings.cachedHashCode;

            if (textHandles.TryGetValue(hash, out IMGUITextHandle textHandleCached))
            {
                textHandlesTuple.Remove(textHandleCached.tuple);
                textHandlesTuple.AddLast(textHandleCached.tuple);
                return textHandleCached;
            }

            var handle = new IMGUITextHandle();
            var tuple = new TextHandleTuple(currentTime, hash);
            var listNode = new LinkedListNode<TextHandleTuple>(tuple);
            handle.tuple = listNode;
            textHandles[hash] = handle;
            handle.Update(settings);
            handle.UpdatePreferredSize(settings);
            textHandlesTuple.AddLast(listNode);
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

            var tempAlignment = style.alignment;
            if (style.imagePosition == ImagePosition.ImageAbove)
            {
                switch (style.alignment)
                {
                    case TextAnchor.MiddleRight:
                    case TextAnchor.LowerRight:
                        tempAlignment = TextAnchor.UpperRight;
                        break;
                    case TextAnchor.MiddleCenter:
                    case TextAnchor.LowerCenter:
                        tempAlignment = TextAnchor.UpperCenter;
                        break;
                    case TextAnchor.MiddleLeft:
                    case TextAnchor.LowerLeft:
                        tempAlignment = TextAnchor.UpperLeft;
                        break;
                }
            }

            settings.fontStyle = TextGeneratorUtilities.LegacyStyleToNewStyle(style.fontStyle);
            settings.textAlignment = TextGeneratorUtilities.LegacyAlignmentToNewAlignment(tempAlignment);
            settings.overflowMode = LegacyClippingToNewOverflow(style.clipping);
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

            settings.characterSpacing = 0;
            settings.wordSpacing = 0;
            settings.paragraphSpacing = 0;
            settings.color = textColor;

            settings.inverseYAxis = true;
            settings.isIMGUI = true;
            settings.shouldConvertToLinearSpace = false;
            settings.fontFeatures = m_ActiveFontFeatures;
        }

        static TextOverflowMode LegacyClippingToNewOverflow(TextClipping clipping)
        {
            switch (clipping)
            {
                case TextClipping.Clip:
                    return TextOverflowMode.Masking;
                case TextClipping.Ellipsis:
                    return TextOverflowMode.Ellipsis;
                case TextClipping.Overflow:
                default:
                    return TextOverflowMode.Overflow;
            }
        }
    }
}
