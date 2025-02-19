// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.TextCore.Text;

namespace UnityEngine
{
    internal class IMGUITextHandle : TextHandle
    {
        internal LinkedListNode<TextHandleTuple> tuple;

        const float sFallbackFontSize = 13;
        const float sTimeToFlush = 5.0f;
        const float sTimeBetweenCleanupRuns = 30.0f;
        const int sNewHandlesBetweenCleanupRuns = 500;

        internal static Func<Object> GetEditorTextSettings;
        internal static Func<int, FontAsset, FontAsset> GetBlurryFontAssetMapping;

        private static TextSettings s_EditorTextSettings;

        private static Dictionary<int, IMGUITextHandle> textHandles = new ();
        private static LinkedList<TextHandleTuple> textHandlesTuple = new ();
        private static float lastCleanupTime;
        private static int newHandlesSinceCleanup = 0;

        internal bool isCachedOnNative = false;

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

        // This cleans both the managed and the native cache
        internal static void EmptyCache()
        {
            GUIStyle.Internal_CleanupAllTextGenerator();
            textHandles.Clear();
            textHandlesTuple.Clear();
        }

        // This only cleans up the cache on the managed side. We assume it is already cleaned on the native side to avoid calls.
        internal static void EmptyManagedCache()
        {
            textHandles.Clear();
            textHandlesTuple.Clear();
        }

        internal static IMGUITextHandle GetTextHandle(GUIStyle style, Rect position, string content, Color32 textColor)
        {
            bool isCached = false;
            ConvertGUIStyleToGenerationSettings(settings, style, textColor, content, position);
            return GetTextHandle(settings, false, ref isCached);
        }

        internal static IMGUITextHandle GetTextHandle(GUIStyle style, Rect position, string content, Color32 textColor, ref bool isCached)
        {
            ConvertGUIStyleToGenerationSettings(settings, style, textColor, content, position);
            return GetTextHandle(settings, true, ref isCached);
        }

        private static bool ShouldCleanup(float currentTime, float lastTime, float cleanupThreshold)
        {
            // timeSinceLastCleanup can end up negative if lastCleanupTime is from a previous run.
            // Clean up if this happens.
            float timeSinceLastCleanup = currentTime - lastTime;
            return timeSinceLastCleanup > cleanupThreshold || timeSinceLastCleanup < 0;
        }

        private static void ClearUnusedTextHandles()
        {
            var currentTime = Time.realtimeSinceStartup;
            while (textHandlesTuple.Count > 0)
            {
                var tuple = textHandlesTuple.First();
                if (ShouldCleanup(currentTime, tuple.lastTimeUsed, sTimeToFlush))
                {
                    GUIStyle.Internal_DestroyTextGenerator(tuple.hashCode);
                    if (textHandles.TryGetValue(tuple.hashCode, out IMGUITextHandle textHandleCached))
                    {
                        textHandleCached.RemoveTextInfoFromPermanentCache();
                    }
                    textHandles.Remove(tuple.hashCode);
                    textHandlesTuple.RemoveFirst();
                }
                else
                    break;
            }
        }

        private static IMGUITextHandle GetTextHandle(TextCore.Text.TextGenerationSettings settings, bool isCalledFromNative, ref bool isCached)
        {
            isCached = false;
            var currentTime = Time.realtimeSinceStartup;
            if (ShouldCleanup(currentTime, lastCleanupTime, sTimeBetweenCleanupRuns) ||
                newHandlesSinceCleanup > sNewHandlesBetweenCleanupRuns)
            {
                ClearUnusedTextHandles();
                lastCleanupTime = currentTime;
                newHandlesSinceCleanup = 0;
            }

            int hash = settings.GetHashCode();

            if (textHandles.TryGetValue(hash, out IMGUITextHandle textHandleCached))
            {
                textHandlesTuple.Remove(textHandleCached.tuple);
                textHandlesTuple.AddLast(textHandleCached.tuple);

                isCached = isCalledFromNative ? textHandleCached.isCachedOnNative : true;
                if (!textHandleCached.isCachedOnNative && isCalledFromNative)
                {
                    textHandleCached.UpdateWithHash(hash);
                    textHandleCached.UpdatePreferredSize();
                    textHandleCached.isCachedOnNative = true;
                }
                return textHandleCached;
            }

            var handle = new IMGUITextHandle();
            var tuple = new TextHandleTuple(currentTime, hash);
            var listNode = new LinkedListNode<TextHandleTuple>(tuple);
            handle.tuple = listNode;
            textHandles[hash] = handle;
            handle.UpdateWithHash(hash);
            handle.UpdatePreferredSize();
            textHandlesTuple.AddLast(listNode);
            handle.isCachedOnNative = isCalledFromNative;
            ++newHandlesSinceCleanup;
            return handle;
        }

        internal static float GetLineHeight(GUIStyle style)
        {
            ConvertGUIStyleToGenerationSettings(settings, style, Color.white, "", Rect.zero);
            return GetLineHeightDefault(settings);
        }

        internal Vector2 GetPreferredSize()
        {
            return preferredSize;
        }

        internal int GetNumCharactersThatFitWithinWidth(float width)
        {
            AddTextInfoToPermanentCache();
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

        public Rect[] GetHyperlinkRects(Rect content)
        {
            AddTextInfoToPermanentCache();

            List<Rect> rects = new List<Rect>();

            for (int i = 0; i < textInfo.linkCount; i++)
            {
                var minPos = GetCursorPositionFromStringIndexUsingLineHeight(textInfo.linkInfo[i].linkTextfirstCharacterIndex) + new Vector2(content.x, content.y);
                var maxPos = GetCursorPositionFromStringIndexUsingLineHeight(textInfo.linkInfo[i].linkTextLength + textInfo.linkInfo[i].linkTextfirstCharacterIndex) + new Vector2(content.x, content.y);
                var lineHeight = textInfo.lineInfo[0].lineHeight;

                if (minPos.y == maxPos.y)
                {
                    rects.Add(new Rect(minPos.x, minPos.y - lineHeight, maxPos.x - minPos.x, lineHeight));
                }
                else
                {
                    // Rect for the first line - including end part
                    rects.Add(new Rect(minPos.x, minPos.y - lineHeight, textInfo.lineInfo[0].width - minPos.x, lineHeight));
                    // Rect for the middle part
                    rects.Add(new Rect(content.x, minPos.y, textInfo.lineInfo[0].width, maxPos.y - minPos.y - lineHeight));
                    // Rect for the bottom line - up to selection
                    if (maxPos.x != 0f)
                        rects.Add(new Rect(content.x, maxPos.y - lineHeight, maxPos.x, lineHeight));
                }
            }
            return rects.ToArray();
        }

        private static void ConvertGUIStyleToGenerationSettings(UnityEngine.TextCore.Text.TextGenerationSettings settings, GUIStyle style, Color textColor, string text, Rect rect)
        {
            if (s_EditorTextSettings == null)
            {
                s_EditorTextSettings = (TextSettings)GetEditorTextSettings?.Invoke();
            }

            settings.textSettings = s_EditorTextSettings;

            if (settings.textSettings == null)
                return;

            Font font = style.font;

            if (!font)
            {
                font = GUIStyle.GetDefaultFont();
            }

            if (style.fontSize > 0)
                settings.fontSize = style.fontSize;
            else if (font)
                settings.fontSize = font.fontSize;
            else
                settings.fontSize = sFallbackFontSize;

            settings.fontStyle = TextGeneratorUtilities.LegacyStyleToNewStyle(style.fontStyle);

            settings.fontAsset = settings.textSettings.GetCachedFontAsset(font, TextShaderUtilities.ShaderRef_MobileSDF_IMGUI);
            if (settings.fontAsset == null)
                return;
            var shouldRenderBitmap = settings.fontAsset.IsEditorFont && UnityEngine.TextCore.Text.TextGenerationSettings.IsEditorTextRenderingModeBitmap();
            if (shouldRenderBitmap)
            {
                settings.fontAsset = GetBlurryFontAssetMapping((int)Math.Round((settings.fontSize * GUIUtility.pixelsPerPoint), MidpointRounding.AwayFromZero), settings.fontAsset);
                settings.pixelsPerPoint = GUIUtility.pixelsPerPoint;
            }

            settings.isEditorRenderingModeBitmap = shouldRenderBitmap;
            settings.material = settings.fontAsset.material;
            if (!shouldRenderBitmap)
            {
                settings.fontAsset.material.SetFloat("_Sharpness", settings.textSettings.GetEditorTextSharpness());
            }

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

            settings.textAlignment = TextGeneratorUtilities.LegacyAlignmentToNewAlignment(tempAlignment);
            settings.overflowMode = LegacyClippingToNewOverflow(style.clipping);
            settings.wordWrappingRatio = 0.4f;
            if (rect.width > 0 && style.wordWrap)
            {
                settings.textWrappingMode = TextWrappingMode.PreserveWhitespace;
            }
            else
            {
                settings.textWrappingMode = TextWrappingMode.PreserveWhitespaceNoWrap;
            }
            settings.richText = style.richText;
            settings.parseControlCharacters = false;
            settings.isPlaceholder = false;
            settings.isRightToLeft = false;
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
