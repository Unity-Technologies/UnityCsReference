// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.TextCore;
using UnityEngine.TextCore.Text;

namespace UnityEngine.UIElements
{
    internal partial class UITKTextHandle
    {
        List<WordInfo> m_CachedWordList;

        /// <summary>Per-character metrics used by the overlay and debugger.</summary>
        internal readonly record struct GlyphMetricsForOverlay
        {
            public GlyphMetricsForOverlay(ref TextElementInfo textElementInfo, float pixelPerPoint)
            {
                float inversePPP = 1 / pixelPerPoint;
                this.isVisible = textElementInfo.isVisible;
                this.origin = textElementInfo.origin * inversePPP;
                this.xAdvance = textElementInfo.xAdvance * inversePPP;
                this.ascentline = textElementInfo.ascender * inversePPP;
                this.baseline = textElementInfo.baseLine * inversePPP;
                this.descentline = textElementInfo.descender * inversePPP;
                this.topLeft = textElementInfo.topLeft * inversePPP;
                this.bottomLeft = textElementInfo.bottomLeft * inversePPP;
                this.topRight = textElementInfo.topRight * inversePPP;
                this.bottomRight = textElementInfo.bottomRight * inversePPP;
                this.scale = textElementInfo.scale;
                this.lineNumber = textElementInfo.lineNumber;
                this.fontCapLine = textElementInfo.fontAsset.faceInfo.capLine * inversePPP;
                this.fontMeanLine = textElementInfo.fontAsset.faceInfo.meanLine * inversePPP;
            }

            /// <summary>
            /// Build from ATG NativeTextElementInfo (quad corners in pixel space) with native baseline.
            /// <paramref name="isVisible"/> must be supplied by the caller; use ComputeWordBounds
            /// (which wraps ICU u_isspace) as the authoritative whitespace detector because ATG
            /// whitespace glyphs can produce quads with non-zero area.
            /// <paramref name="fontCapLine"/> and <paramref name="fontMeanLine"/> are Y-down offsets
            /// from the baseline (negative = above baseline) pre-scaled to point space.
            /// </summary>
            public GlyphMetricsForOverlay(NativeTextElementInfo native, float pixelPerPoint, int lineNumber, float baselineY, bool isVisible, float fontCapLine = 0f, float fontMeanLine = 0f)
            {
                float inv = 1f / pixelPerPoint;
                var bl = native.bottomLeft.position * inv;
                var tl = native.topLeft.position * inv;
                var tr = native.topRight.position * inv;
                var br = native.bottomRight.position * inv;
                this.origin = bl.x;
                this.xAdvance = br.x;
                this.ascentline = tl.y;
                this.descentline = bl.y;
                this.baseline = baselineY;
                this.topLeft = tl;
                this.bottomLeft = bl;
                this.topRight = tr;
                this.bottomRight = br;
                this.isVisible = isVisible;
                this.scale = 1f;
                this.lineNumber = lineNumber;
                this.fontCapLine = fontCapLine;
                this.fontMeanLine = fontMeanLine;
            }

            readonly public bool isVisible;
            readonly public float origin;
            readonly public float xAdvance;
            readonly public float ascentline;
            readonly public float baseline;
            readonly public float descentline;
            readonly public Vector3 topLeft;
            readonly public Vector3 bottomLeft;
            readonly public Vector3 topRight;
            readonly public Vector3 bottomRight;
            readonly public float scale;
            readonly public int lineNumber;
            readonly public float fontCapLine;
            readonly public float fontMeanLine;
        }

        /// <summary>
        /// Converts a logical (string) character index to the corresponding visual/glyph index.
        /// For TextCore (LTR) visual order equals logical order; for ATG can differ (e.g. RTL).
        /// </summary>
        internal int LogicalToGlyphIndex(int logicalIndex)
        {
            if (!useAdvancedText)
                return logicalIndex;
            if (textGenerationInfo == IntPtr.Zero)
                return logicalIndex;
            return TextSelectionService.GetGlyphIndex(textGenerationInfo, logicalIndex);
        }

        /// <summary>
        /// Converts a visual/glyph index to the corresponding logical (string) character index.
        /// For TextCore (LTR) visual order equals logical order; for ATG can differ (e.g. RTL).
        /// </summary>
        internal int GlyphIndexToLogicalIndex(int glyphIndex)
        {
            if (!useAdvancedText)
                return glyphIndex;
            if (textGenerationInfo == IntPtr.Zero)
                return glyphIndex;
            return TextGenerationInfo.GetLogicalIndexFromGlyphIndex(textGenerationInfo, glyphIndex);
        }

        /// <summary>Total number of glyphs in visual order. For TextCore equals character count; for ATG from native.</summary>
        internal int GetGlyphCount()
        {
            if (!useAdvancedText)
                return textInfo?.textElementInfo?.Length ?? 0;
            if (textGenerationInfo == IntPtr.Zero)
                return 0;
            return TextGenerationInfo.GetGlyphCount(textGenerationInfo);
        }

        internal int GetLineCount()
        {
            if (!useAdvancedText)
                return textInfo?.lineCount ?? 0;
            if (textGenerationInfo == IntPtr.Zero)
                return 0;
            return TextGenerationInfo.GetLineCount(textGenerationInfo);
        }

        internal int GetLinkCount()
        {
            if (!useAdvancedText)
                return textInfo?.linkCount ?? 0;
            return m_Links?.Length ?? 0;
        }

        // This method assumes the textInfo is populated (TextCore) or text is generated (ATG).
        internal GlyphMetricsForOverlay GetScaledCharacterMetrics(int i)
        {
            if (!useAdvancedText)
                return new GlyphMetricsForOverlay(ref textInfo.textElementInfo[i], GetPixelsPerPoint());

            if (textGenerationInfo == IntPtr.Zero || m_TextElement == null)
                return default;

            int g = LogicalToGlyphIndex(i);
            var nativeInfo = TextGenerationInfo.GetTextInfo(textGenerationInfo);
            // Mesh textElementInfos are NOT 1:1 with global glyph indices: invisible
            // glyphs are skipped and decoration quads are appended. The (meshIndex,
            // textElementInfoIndex) pair is what AddGlyph actually assigned.
            var info = TextGenerationInfo.GetGlyphRenderInfo(textGenerationInfo, g);
            NativeTextElementInfo nativeElem = default;
            if (info.meshIndex >= 0 && info.meshIndex < nativeInfo.meshInfoCount)
            {
                var mesh = nativeInfo.meshInfos[info.meshIndex];
                var span = mesh.textElementInfos;
                if (info.textElementInfoIndex >= 0 && info.textElementInfoIndex < span.Length)
                    nativeElem = span[info.textElementInfoIndex];
            }

            int lineNum = TextSelectionService.GetLineNumber(textGenerationInfo, i);
            float ppp = GetPixelsPerPoint();
            float baselinePt = TextGenerationInfo.GetLineBaselineY(textGenerationInfo, lineNum) / ppp;

            // ATG whitespace glyphs can have non-zero-area quads, so quad dimensions cannot
            // reliably distinguish them from rendered glyphs.  ComputeWordBounds (ICU u_isspace)
            // is the authoritative oracle: startIndex = -1 means the element is whitespace.
            TextSelectionService.ComputeWordBounds(textGenerationInfo, i, out int wStart, out int _);
            bool isVisible = wStart >= 0;

            // TODO(font-metrics): fontCapLine / fontMeanLine are currently derived from the
            // *primary* font asset assigned to the TextElement.  This is incorrect when the
            // text uses font fallbacks, rich-text style tags (<b>, <i>, <font>), or sprite
            // assets — each glyph can come from a different FontAsset with different faceInfo
            // metrics.  The correct fix requires exposing the per-glyph font-asset reference
            // (or its faceInfo.capLine/meanLine/pointSize/scale) from the native Glyph struct
            // via a new native binding (e.g. TextGenerationInfo.GetGlyphFaceInfo(ptr, glyphIndex)).
            // Until that is available, cap/mean lines are left at 0 so the overlay disables
            // them for ATG rather than drawing them at the wrong position.
            float fontCapLine = 0f;
            float fontMeanLine = 0f;

            return new GlyphMetricsForOverlay(nativeElem, ppp, lineNum, baselinePt, isVisible, fontCapLine, fontMeanLine);
        }

        /// <summary>
        /// Returns link rects in element content space: Y-down, origin top-left (UITK convention).
        /// ATG: native rects scaled to points. TextCore: builds from linkInfo using character metrics, Y-up → Y-down.
        /// </summary>
        internal Rect[] GetLinkRects()
        {
            if (useAdvancedText)
            {
                if (textGenerationInfo == IntPtr.Zero)
                    return Array.Empty<Rect>();
                var rects = TextLib.GetHyperlinkRects(textGenerationInfo);
                if (rects == null || rects.Length == 0)
                    return Array.Empty<Rect>();
                float inv = 1f / GetPixelsPerPoint();
                var result = new Rect[rects.Length];
                for (int i = 0; i < rects.Length; i++)
                {
                    result[i] = new Rect(
                        rects[i].x * inv,
                        rects[i].y * inv,
                        rects[i].width * inv,
                        rects[i].height * inv);
                }
                return result;
            }

            if (textInfo?.linkInfo == null || textInfo.linkCount == 0 || m_TextElement == null)
                return Array.Empty<Rect>();

            float contentHeight = m_TextElement.contentRect.height;
            var linkRects = new Rect[textInfo.linkCount];
            for (int i = 0; i < textInfo.linkCount; i++)
            {
                var link = textInfo.linkInfo[i];
                float maxAscender = float.MinValue;
                float minDescender = float.MaxValue;
                float left = float.MaxValue;
                float right = float.MinValue;
                for (int j = 0; j < link.linkTextLength; j++)
                {
                    var m = GetScaledCharacterMetrics(link.linkTextfirstCharacterIndex + j);
                    maxAscender = Mathf.Max(maxAscender, m.ascentline);
                    minDescender = Mathf.Min(minDescender, m.descentline);
                    left = Mathf.Min(left, m.topLeft.x);
                    right = Mathf.Max(right, m.topRight.x);
                }
                // TextCore returns Y-up (ascender positive upward); convert to content Y-down (top-left origin).
                linkRects[i] = new Rect(left, contentHeight - maxAscender, right - left, maxAscender - minDescender);
            }
            return linkRects;
        }

        internal LineInfo GetLineInfo(int lineIndex)
        {
            if (!useAdvancedText)
            {
                if (textInfo?.lineInfo == null || lineIndex < 0 || lineIndex >= textInfo.lineCount)
                    return default;
                return textInfo.lineInfo[lineIndex];
            }

            if (textGenerationInfo == IntPtr.Zero)
                return default;

            int lineCount = GetLineCount();
            if (lineIndex < 0 || lineIndex >= lineCount)
                return default;

            int n = GetTextElementCount();
            int firstChar = -1;
            int lastChar = -1;
            float lineLeft = float.MaxValue;
            float lineRight = float.MinValue;

            for (int i = 0; i < n; i++)
            {
                if (TextSelectionService.GetLineNumber(textGenerationInfo, i) != lineIndex)
                    continue;

                if (firstChar < 0) firstChar = i;
                lastChar = i;

                var m = GetScaledCharacterMetrics(i);
                lineLeft = Mathf.Min(lineLeft, m.topLeft.x);
                lineRight = Mathf.Max(lineRight, m.topRight.x);
            }

            if (firstChar < 0)
                return default;

            int charCount = lastChar - firstChar + 1;

            // Glyph mesh vertex Y positions are local to each paragraph, so they can't be
            // used for cross-line comparisons. Instead, use the native cursor/line APIs which
            // provide correct global Y coordinates.
            float ppp = GetPixelsPerPoint();
            var cursorPos = TextSelectionService.GetCursorPositionFromLogicalIndex(textGenerationInfo, firstChar);
            float lineHeightPx = TextGenerationInfo.GetLineHeight(textGenerationInfo, lineIndex);
            float lineBaselinePx = TextGenerationInfo.GetLineBaselineY(textGenerationInfo, lineIndex);
            float lineBottomPt = cursorPos.y / ppp;
            float lineTopPt = (cursorPos.y - lineHeightPx) / ppp;
            float baselinePt = lineBaselinePx / ppp;

            return new LineInfo
            {
                firstCharacterIndex = firstChar,
                lastCharacterIndex = lastChar + 1,
                firstVisibleCharacterIndex = firstChar,
                lastVisibleCharacterIndex = lastChar,
                characterCount = charCount,
                visibleCharacterCount = charCount,
                ascender = lineTopPt,
                descender = lineBottomPt,
                baseline = baselinePt,
                lineExtents = new Extents(new Vector2(lineLeft, lineTopPt), new Vector2(lineRight, lineBottomPt)),
                length = lineRight - lineLeft
            };
        }

        /// <summary>
        /// Build word list from native ICU word boundaries using ComputeWordBounds.
        /// ComputeWordBounds returns startIndex=-1 when on whitespace; we advance and skip.
        /// One entry per logical word (no split by line) so each word's rect includes all glyphs including the first on wrapped lines; wordId is the logical word index.
        /// </summary>
        void BuildWordListFromNative()
        {
            m_CachedWordList = null;
            if (textGenerationInfo == IntPtr.Zero)
                return;

            int n = GetTextElementCount();
            if (n <= 0)
                return;

            var list = new List<WordInfo>();
            int i = 0;
            int wordIdCounter = 0;
            while (i < n)
            {
                TextSelectionService.ComputeWordBounds(textGenerationInfo, i, out int wordStart, out int wordEnd);
                if (wordStart < 0)
                {
                    i++;
                    continue;
                }
                if (wordStart >= n)
                    break;
                if (wordEnd <= wordStart || wordEnd > n)
                {
                    i++;
                    continue;
                }

                if (wordStart < i)
                {
                    i = Math.Max(i, wordEnd);
                    if (i < n)
                        i++;
                    continue;
                }

                int len = wordEnd - wordStart;
                if (len <= 0)
                {
                    i++;
                    continue;
                }
                list.Add(new WordInfo { firstCharacterIndex = wordStart, lastCharacterIndex = wordEnd - 1, characterCount = len, wordId = wordIdCounter });
                wordIdCounter++;
                i = wordEnd;
            }
            m_CachedWordList = list;
        }

        internal int GetWordCount()
        {
            if (!useAdvancedText)
                return textInfo?.wordCount ?? 0;
            if (textGenerationInfo == IntPtr.Zero)
                return 0;
            BuildWordListFromNative();
            return m_CachedWordList?.Count ?? 0;
        }

        internal WordInfo GetWordInfo(int wordIndex)
        {
            if (!useAdvancedText)
            {
                if (textInfo?.wordInfo == null || wordIndex < 0 || wordIndex >= textInfo.wordCount)
                    return default;
                return textInfo.wordInfo[wordIndex];
            }
            if (m_CachedWordList == null)
                BuildWordListFromNative();
            if (m_CachedWordList == null || wordIndex < 0 || wordIndex >= m_CachedWordList.Count)
                return default;
            return m_CachedWordList[wordIndex];
        }

        /// <summary>
        /// Returns both the inclusive start and exclusive end of the word at logicalIndex (read-only).
        /// startIndex is -1 when logicalIndex is on whitespace. For testing and tooling.
        /// </summary>
        internal void ComputeWordBounds(int logicalIndex, out int startIndex, out int endIndex)
        {
            if (textGenerationInfo == IntPtr.Zero)
            {
                startIndex = -1;
                endIndex = -1;
                return;
            }
            TextSelectionService.ComputeWordBounds(textGenerationInfo, logicalIndex, out startIndex, out endIndex);
        }
    }
}
