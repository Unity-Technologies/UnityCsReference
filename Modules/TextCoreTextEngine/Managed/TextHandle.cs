// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System;
using System.Text;
using Unity.Jobs.LowLevel.Unsafe;
using System.Diagnostics;
using UnityEngine.Bindings;

namespace UnityEngine.TextCore.Text
{
    [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
    internal class TextHandle
    {
        public TextHandle()
        {
        }

        [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
        internal static void InitThreadArrays()
        {
            if (s_Settings != null && s_Generators != null && s_TextInfosCommon != null)
                return;
            s_Settings = new TextGenerationSettings[JobsUtility.ThreadIndexCount];
            for (int i = 0; i < JobsUtility.ThreadIndexCount; i++)
            {
                s_Settings[i] = new TextGenerationSettings();
            }
            s_Generators = new TextGenerator[JobsUtility.ThreadIndexCount];
            for (int i = 0; i < JobsUtility.ThreadIndexCount; i++)
            {
                s_Generators[i] = new TextGenerator();
            }
            s_TextInfosCommon = new TextInfo[JobsUtility.ThreadIndexCount];
            for (int i = 0; i < JobsUtility.ThreadIndexCount; i++)
            {
                s_TextInfosCommon[i] = new TextInfo(VertexDataLayout.VBO);
            }
        }

        static TextGenerationSettings[] s_Settings;
        internal static TextGenerationSettings[] settingsArray
        {
            get
            {
                if (s_Settings == null)
                {
                    s_Settings = new TextGenerationSettings[JobsUtility.ThreadIndexCount];
                    for (int i = 0; i < JobsUtility.ThreadIndexCount; i++)
                    {
                        s_Settings[i] = new TextGenerationSettings();
                    }
                }
                return s_Settings;
            }
        }

        static TextGenerator[] s_Generators;
        internal static TextGenerator[] generators
        {
            get
            {
                if (s_Generators == null)
                {
                    s_Generators = new TextGenerator[JobsUtility.ThreadIndexCount];
                    s_Generators[0] = TextGenerator.GetTextGenerator();
                    for (int i = 1; i < JobsUtility.ThreadIndexCount; i++)
                    {
                        s_Generators[i] = new TextGenerator();
                    }
                }
                return s_Generators;
            }
        }

        static TextInfo[] s_TextInfosCommon;
        internal static TextInfo[] textInfosCommon
        {
            get
            {
                if (s_TextInfosCommon == null)
                {
                    s_TextInfosCommon = new TextInfo[JobsUtility.ThreadIndexCount];
                    for (int i = 0; i < JobsUtility.ThreadIndexCount; i++)
                    {
                        s_TextInfosCommon[i] = new TextInfo(VertexDataLayout.VBO);
                    }
                }
                return s_TextInfosCommon;
            }
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal NativeTextInfo nativeTextInfo;
        internal static TextInfo textInfoCommon => textInfosCommon[JobsUtility.ThreadIndex];
        static TextGenerator generator => generators[JobsUtility.ThreadIndex];

        internal static TextGenerationSettings settings
        {
            [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
            get => settingsArray [JobsUtility.ThreadIndex];
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal NativeTextGenerationSettings nativeSettings = NativeTextGenerationSettings.Default;

        ~TextHandle()
        {
            RemoveTextInfoFromCache();
        }

        internal Vector2 preferredSize {
            [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
            get;
            private set;
        }

        private Rect screenRect;
        private float lineHeightDefault;
        private bool isPlaceholder;
        private bool m_IsCached;
        private LinkedListNode<TextInfo> m_TextInfoNode;

        private static LinkedList<TextInfo> s_TextInfoPool = new LinkedList<TextInfo>();
        private static double s_MinTimeInCache = 1;
        [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
        internal static double currentTime;

        /// <summary>
        /// The TextInfo instance, use from this instead of the m_TextInfo member.
        /// References a cached textInfo if dynamic, or a static instance (textInfoCommon) if not cached.
        /// </summary>
        internal TextInfo textInfo
        {
            [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
            get
            {

                if (m_TextInfoNode == null)
                    return textInfoCommon;
                else
                    return m_TextInfoNode.Value;
            }
        }

        static object syncRoot = new object();
        public virtual void AddTextInfoToCache()
        {
            lock (syncRoot)
            {
                bool isMainThread = !JobsUtility.IsExecutingJob;
                if (isMainThread)
                    currentTime = Time.realtimeSinceStartup;

                if (m_IsCached)
                {
                    RefreshCaching();
                    return;
                }

                if (s_TextInfoPool.Count > 0 && (currentTime - s_TextInfoPool.Last.Value.lastTimeInCache > s_MinTimeInCache))
                {
                    RecycleTextInfoFromCache();
                }
                else
                {
                    var textInfo = new TextInfo(VertexDataLayout.VBO);
                    m_TextInfoNode = new LinkedListNode<TextInfo>(textInfo);
                    s_TextInfoPool.AddFirst(m_TextInfoNode);
                    textInfo.lastTimeInCache = currentTime;
                    textInfo.removedFromCache += RemoveTextInfoFromCache;
                }

                m_IsCached = true;
                SetDirty();
                
                if (isMainThread)
                    Update(settings);
                else
                    UpdateFontAssetPrepared();
            }
        }

        public virtual void RemoveTextInfoFromCache()
        {
            lock (syncRoot)
            {
                if (!m_IsCached)
                    return;

                m_IsCached = false;
                textInfo.lastTimeInCache = 0;
                textInfo.removedFromCache = null;
                if (m_TextInfoNode != null)
                {
                    s_TextInfoPool.Remove(m_TextInfoNode);
                    s_TextInfoPool.AddLast(m_TextInfoNode);
                    m_TextInfoNode = null;
                }
            }
        }

        private void RefreshCaching()
        {
            if (!JobsUtility.IsExecutingJob)
                currentTime = Time.realtimeSinceStartup;

            textInfo.lastTimeInCache = currentTime;
            s_TextInfoPool.Remove(m_TextInfoNode);
            s_TextInfoPool.AddFirst(m_TextInfoNode);
        }

        private void RecycleTextInfoFromCache()
        {
            if (!JobsUtility.IsExecutingJob)
                currentTime = Time.realtimeSinceStartup;

            m_TextInfoNode = s_TextInfoPool.Last;
            m_TextInfoNode.Value.RemoveFromCache();
            s_TextInfoPool.RemoveLast();
            s_TextInfoPool.AddFirst(m_TextInfoNode);
            textInfo.removedFromCache += RemoveTextInfoFromCache;
            textInfo.lastTimeInCache = currentTime;
        }

        // For testing purposes
        internal bool IsTextInfoAllocated()
        {
            return textInfo != null;
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal int m_PreviousGenerationSettingsHash;
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal int m_PreviousNativeGenerationSettingsHash;

        protected internal static List<OTL_FeatureTag> m_ActiveFontFeatures = new List<OTL_FeatureTag>() { OTL_FeatureTag.kern };

        private bool isDirty;
        public void SetDirty()
        {
            isDirty = true;
        }

        public bool IsDirty(TextGenerationSettings settings)
        {
            int hash = settings.GetHashCode();
            if (m_PreviousGenerationSettingsHash == hash && !isDirty && m_IsCached)
                return false;

            m_PreviousGenerationSettingsHash = hash;
            isDirty = false;
            return true;
        }

        public Vector2 GetCursorPositionFromStringIndexUsingCharacterHeight(int index, bool inverseYAxis = true)
        {
            AddTextInfoToCache();
            var result = screenRect.position;
            if (textInfo.characterCount == 0)
                return inverseYAxis ? new Vector2(0, lineHeightDefault) : result;

            var validIndex = index >= textInfo.characterCount ? textInfo.characterCount - 1 : index;
            var character = textInfo.textElementInfo[validIndex];
            var descender = character.descender;
            var vectorX = index >= textInfo.characterCount ? character.xAdvance : character.origin;

            result += inverseYAxis ?
                new Vector2(vectorX, screenRect.height - descender) :
                new Vector2(vectorX, descender);

            return result;
        }

        public Vector2 GetCursorPositionFromStringIndexUsingLineHeight(int index, bool useXAdvance = false, bool inverseYAxis = true)
        {
            AddTextInfoToCache();
            var result = screenRect.position;
            if (textInfo.characterCount == 0 || index < 0)
                return inverseYAxis ? new Vector2(0, lineHeightDefault) : result;

            if (index >= textInfo.characterCount)
                index = textInfo.characterCount - 1;

            var character = textInfo.textElementInfo[index];
            var line = textInfo.lineInfo[character.lineNumber];

            if (index >= textInfo.characterCount - 1 || useXAdvance)
            {
                result += inverseYAxis ?
                    new Vector2(character.xAdvance, screenRect.height - line.descender) :
                    new Vector2(character.xAdvance, line.descender);
                return result;
            }

            result += inverseYAxis ?
                new Vector2(character.origin, screenRect.height - line.descender) :
                new Vector2(character.origin, line.descender);

            return result;
        }

        //TODO add special handling for 1 character...
        // Add support for world space.
        public int GetCursorIndexFromPosition(Vector2 position, bool inverseYAxis = true)
        {
            if (inverseYAxis)
                position.y = screenRect.height - position.y;

            var lineNumber = 0;
            if (textInfo.lineCount > 1)
                lineNumber = FindNearestLine(position);

            var index = FindNearestCharacterOnLine(position, lineNumber, false);
            var cInfo = textInfo.textElementInfo[index];

            // Get Bottom Left and Top Right position of the current character
            Vector3 bl = cInfo.bottomLeft;
            Vector3 tr = cInfo.topRight;

            float insertPosition = (position.x - bl.x) / (tr.x - bl.x);

            return (insertPosition < 0.5f) || cInfo.character == '\n' ? index : index + 1;
        }

        public int LineDownCharacterPosition(int originalPos)
        {
            if (originalPos >= textInfo.characterCount)
                return textInfo.characterCount - 1; // text.Length;

            var originChar = textInfo.textElementInfo[originalPos];
            int originLine = originChar.lineNumber;

            //// We are on the last line return last character
            if (originLine + 1 >= textInfo.lineCount)
                return textInfo.characterCount - 1;

            // Need to determine end line for next line.
            int endCharIdx = textInfo.lineInfo[originLine + 1].lastCharacterIndex;

            int closest = -1;
            float distance = Mathf.Infinity;
            float range = 0;

            for (int i = textInfo.lineInfo[originLine + 1].firstCharacterIndex; i < endCharIdx; ++i)
            {
                var currentChar = textInfo.textElementInfo[i];

                float d = originChar.origin - currentChar.origin;
                float r = d / (currentChar.xAdvance - currentChar.origin);

                if (r >= 0 && r <= 1)
                {
                    if (r < 0.5f)
                        return i;
                    else
                        return i + 1;
                }

                d = Mathf.Abs(d);

                if (d < distance)
                {
                    closest = i;
                    distance = d;
                    range = r;
                }
            }

            if (closest == -1) return endCharIdx;

            if (range < 0.5f)
                return closest;
            else
                return closest + 1;
        }

        public int LineUpCharacterPosition(int originalPos)
        {
            if (originalPos >= textInfo.characterCount)
                originalPos -= 1;

            var originChar = textInfo.textElementInfo[originalPos];
            int originLine = originChar.lineNumber;

            // We are on the first line return first character
            if (originLine - 1 < 0)
                return 0;

            int endCharIdx = textInfo.lineInfo[originLine].firstCharacterIndex - 1;

            int closest = -1;
            float distance = Mathf.Infinity;
            float range = 0;

            for (int i = textInfo.lineInfo[originLine - 1].firstCharacterIndex; i < endCharIdx; ++i)
            {
                var currentChar = textInfo.textElementInfo[i];

                float d = originChar.origin - currentChar.origin;
                float r = d / (currentChar.xAdvance - currentChar.origin);

                if (r >= 0 && r <= 1)
                {
                    if (r < 0.5f)
                        return i;
                    else
                        return i + 1;
                }

                d = Mathf.Abs(d);

                if (d < distance)
                {
                    closest = i;
                    distance = d;
                    range = r;
                }
            }

            if (closest == -1) return endCharIdx;

            //Debug.Log("Returning nearest character with Range = " + range);

            if (range < 0.5f)
                return closest;
            else
                return closest + 1;
        }

        // This could be improved if TextElementInfo had a reference to the word index.
        public int FindWordIndex(int cursorIndex)
        {
            for (int i = 0; i < textInfo.wordCount; i++)
            {
                var word = textInfo.wordInfo[i];
                if (word.firstCharacterIndex <= cursorIndex && word.lastCharacterIndex >= cursorIndex)
                    return i;
            }

            return -1;
        }

        public int FindNearestLine(Vector2 position)
        {
            float distance = Mathf.Infinity;
            int closest = -1;

            for (int i = 0; i < textInfo.lineCount; i++)
            {
                var lineInfo = textInfo.lineInfo[i];

                float ascender = lineInfo.ascender;
                float descender = lineInfo.descender;

                if (ascender > position.y && descender < position.y)
                {
                    // Debug.Log("Position is on line " + i);
                    return i;
                }

                float d0 = Mathf.Abs(ascender - position.y);
                float d1 = Mathf.Abs(descender - position.y);

                float d = Mathf.Min(d0, d1);
                if (d < distance)
                {
                    distance = d;
                    closest = i;
                }
            }
            //Debug.Log("Closest line to position is " + closest);
            return closest;
        }

        public int FindNearestCharacterOnLine(Vector2 position, int line, bool visibleOnly)
        {
            if (line >= textInfo.lineInfo.Length || line < 0)
                return 0;
            int firstCharacter = textInfo.lineInfo[line].firstCharacterIndex;
            int lastCharacter = textInfo.lineInfo[line].lastCharacterIndex;

            float distanceSqr = Mathf.Infinity;
            int closest = lastCharacter;


            for (int i = firstCharacter; i <= lastCharacter; i++)
            {
                // Get current character info.
                var cInfo = textInfo.textElementInfo[i];
                if (visibleOnly && !cInfo.isVisible) continue;

                // Ignore Carriage Returns <CR>
                if (cInfo.character == '\r' || cInfo.character == '\n')
                    continue;

                // Get Bottom Left and Top Right position of the current character
                Vector3 bl = cInfo.bottomLeft;
                Vector3 tl = new Vector3(cInfo.bottomLeft.x, cInfo.topRight.y, 0);
                Vector3 tr = cInfo.topRight;
                Vector3 br = new Vector3(cInfo.topRight.x, cInfo.bottomLeft.y, 0);

                if (PointIntersectRectangle(position, bl, tl, tr, br))
                {
                    closest = i;
                    break;
                }

                // Find the closest corner to position.
                float dbl = DistanceToLine(bl, tl, position);
                float dtl = DistanceToLine(tl, tr, position);
                float dtr = DistanceToLine(tr, br, position);
                float dbr = DistanceToLine(br, bl, position);

                float d = dbl < dtl ? dbl : dtl;
                d = d < dtr ? d : dtr;
                d = d < dbr ? d : dbr;

                if (distanceSqr > d)
                {
                    distanceSqr = d;
                    closest = i;
                }
            }
            return closest;
        }

        /// <summary>
        /// Function returning the index of the Link at the given position (if any).
        /// </summary>
        /// <returns></returns>
        public int FindIntersectingLink(Vector3 position, bool inverseYAxis = true)
        {
            if (inverseYAxis)
                position.y = screenRect.height - position.y;

            for (int i = 0; i < textInfo.linkCount; i++)
            {
                var linkInfo = textInfo.linkInfo[i];

                bool isBeginRegion = false;

                Vector3 bl = Vector3.zero;
                Vector3 tl = Vector3.zero;
                Vector3 br = Vector3.zero;
                Vector3 tr = Vector3.zero;

                // Iterate through each character of the word
                for (int j = 0; j < linkInfo.linkTextLength; j++)
                {
                    int characterIndex = linkInfo.linkTextfirstCharacterIndex + j;
                    var currentCharInfo = textInfo.textElementInfo[characterIndex];
                    int currentLine = currentCharInfo.lineNumber;

                    if (!isBeginRegion)
                    {
                        isBeginRegion = true;

                        bl = new Vector3(currentCharInfo.bottomLeft.x, currentCharInfo.descender, 0);
                        tl = new Vector3(currentCharInfo.bottomLeft.x, currentCharInfo.ascender, 0);

                        //Debug.Log("Start Word Region at [" + currentCharInfo.character + "]");

                        // If Word is one character
                        if (linkInfo.linkTextLength == 1)
                        {
                            isBeginRegion = false;

                            br = new Vector3(currentCharInfo.topRight.x, currentCharInfo.descender, 0);
                            tr = new Vector3(currentCharInfo.topRight.x, currentCharInfo.ascender, 0);

                            // Check for Intersection
                            if (PointIntersectRectangle(position, bl, tl, tr, br))
                                return i;

                            //Debug.Log("End Word Region at [" + currentCharInfo.character + "]");
                        }
                    }

                    // Last Character of Word
                    if (isBeginRegion && j == linkInfo.linkTextLength - 1)
                    {
                        isBeginRegion = false;

                        br = new Vector3(currentCharInfo.topRight.x, currentCharInfo.descender, 0);
                        tr = new Vector3(currentCharInfo.topRight.x, currentCharInfo.ascender, 0);

                        // Check for Intersection
                        if (PointIntersectRectangle(position, bl, tl, tr, br))
                            return i;

                        //Debug.Log("End Word Region at [" + currentCharInfo.character + "]");
                    }
                    // If Word is split on more than one line.
                    else if (isBeginRegion && currentLine != textInfo.textElementInfo[characterIndex + 1].lineNumber)
                    {
                        isBeginRegion = false;

                        br = new Vector3(currentCharInfo.topRight.x, currentCharInfo.descender, 0);
                        tr = new Vector3(currentCharInfo.topRight.x, currentCharInfo.ascender, 0);

                        // Check for Intersection
                        if (PointIntersectRectangle(position, bl, tl, tr, br))
                            return i;

                        //Debug.Log("End Word Region at [" + currentCharInfo.character + "]");
                    }
                }

                //Debug.Log("Word at Index: " + i + " is located at (" + bl + ", " + tl + ", " + tr + ", " + br + ").");

            }

            return -1;
        }

        public float ComputeTextWidth(TextGenerationSettings tgs)
        {
            UpdatePreferredValues(tgs);
            return preferredSize.x;
        }

        public float ComputeTextHeight(TextGenerationSettings tgs)
        {
            UpdatePreferredValues(tgs);
            return preferredSize.y;
        }

        public int GetCorrespondingStringIndex(int index)
        {
            if (index <= 0)
                return 0;

            return textInfo.textElementInfo[index - 1].index + textInfo.textElementInfo[index - 1].stringLength;
        }

        public int GetCorrespondingCodePointIndex(int stringIndex)
        {
            if (stringIndex <= 0)
                return 0;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                var character = textInfo.textElementInfo[i];
                if (character.index + character.stringLength >= stringIndex)
                    return i + 1;
            }

            return textInfo.characterCount;
        }

        public LineInfo GetLineInfoFromCharacterIndex(int index)
        {
            return textInfo.lineInfo[GetLineNumber(index)];
        }

        private static bool PointIntersectRectangle(Vector3 m, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Vector3 ab = b - a;
            Vector3 am = m - a;
            Vector3 bc = c - b;
            Vector3 bm = m - b;

            float abamDot = Vector3.Dot(ab, am);
            float bcbmDot = Vector3.Dot(bc, bm);

            return 0 <= abamDot && abamDot <= Vector3.Dot(ab, ab) && 0 <= bcbmDot && bcbmDot <= Vector3.Dot(bc, bc);
        }

        /// <summary>
        /// Function returning the Square Distance from a Point to a Line.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        private static float DistanceToLine(Vector3 a, Vector3 b, Vector3 point)
        {
            Vector3 n = b - a;
            Vector3 pa = a - point;

            float c = Vector3.Dot( n, pa );

            // Closest point is a
            if ( c > 0.0f )
                return Vector3.Dot( pa, pa );

            Vector3 bp = point - b;

            // Closest point is b
            if (Vector3.Dot( n, bp ) > 0.0f )
                return Vector3.Dot( bp, bp );

            // Closest point is between a and b
            Vector3 e = pa - n * (c / Vector3.Dot( n, n ));

            return Vector3.Dot( e, e );
        }

        public int GetLineNumber(int index)
        {
            if (index <= 0)
                index = 0;

            if (index >= textInfo.characterCount)
                index = Mathf.Max(0, textInfo.characterCount - 1);

            return textInfo.textElementInfo[index].lineNumber;
        }

        public float GetLineHeight(int lineNumber)
        {
            if (lineNumber <= 0)
                lineNumber = 0;

            if (lineNumber >= textInfo.lineCount)
                lineNumber = Mathf.Max(0, textInfo.lineCount - 1);

            return textInfo.lineInfo[lineNumber].lineHeight;
        }

        public float GetLineHeightFromCharacterIndex(int index)
        {
            if (index <= 0)
                index = 0;

            if (index >= textInfo.characterCount)
                index = Mathf.Max(0, textInfo.characterCount - 1);
            return GetLineHeight(textInfo.textElementInfo[index].lineNumber);
        }

        public float GetCharacterHeightFromIndex(int index)
        {
            if (index <= 0)
                index = 0;

            if (index >= textInfo.characterCount)
                index = Mathf.Max(0, textInfo.characterCount - 1);

            var characterInfo = textInfo.textElementInfo[index];
            return characterInfo.ascender - characterInfo.descender;
        }

        public bool IsPlaceholder
        {
            get => isPlaceholder;
        }

        public bool IsElided()
        {
            if (textInfo == null)
                return false;

            if (textInfo.characterCount == 0) // impossible to differentiate between an empty string and a fully truncated string.
                return true;

            return TextGenerator.isTextTruncated;
        }

        /// <summary>
        // Retrieves a substring from this instance.
        /// </summary>
        public string Substring(int startIndex, int length)
        {
            if (startIndex < 0 || startIndex + length > textInfo.characterCount)
                throw new ArgumentOutOfRangeException();

            var result = new StringBuilder(length);
            for (int i = startIndex; i < startIndex + length; ++i)
            {
                var codePoint = textInfo.textElementInfo[i].character;
                if (codePoint >= CodePoint.UNICODE_PLANE01_START && codePoint <= CodePoint.UNICODE_PLANE16_END)
                {
                    uint highSurrogate = CodePoint.HIGH_SURROGATE_START + ((codePoint - CodePoint.UNICODE_PLANE01_START) >> 10);
                    uint lowSurrogate = CodePoint.LOW_SURROGATE_START + ((codePoint - CodePoint.UNICODE_PLANE01_START) & CodePoint.LOWEST_10BITS_MASK);

                    result.Append((char)highSurrogate);
                    result.Append((char)lowSurrogate);
                }
                else
                {
                    result.Append((char)codePoint);
                }
            }

            return result.ToString();
        }

        /// <summary>
        // Reports the zero-based index of the first occurrence of the specified Unicode character in this string.
        // The search starts at a specified character position.
        /// </summary>
        /// <remarks>
        /// The search is case sensitive.
        /// </remarks>
        public int IndexOf(char value, int startIndex)
        {
            if (startIndex < 0 || startIndex >= textInfo.characterCount)
                throw new ArgumentOutOfRangeException();

            for (int i = startIndex; i < textInfo.characterCount; i++)
            {
                if (textInfo.textElementInfo[i].character == value)
                    return i;
            }

            return -1;
        }

        /// <summary>
        // Reports the zero-based index position of the last occurrence of a specified Unicode character within this
        // instance. The search starts at a specified character position and proceeds backward toward the beginning of the string.
        /// </summary>
        /// <remarks>
        /// The search is case sensitive.
        /// </remarks>
        public int LastIndexOf(char value, int startIndex)
        {
            if (startIndex < 0 || startIndex >= textInfo.characterCount)
                throw new ArgumentOutOfRangeException();

            for (int i = startIndex; i >= 0; i--)
            {
                if (textInfo.textElementInfo[i].character == value)
                    return i;
            }

            return -1;
        }

        protected void UpdatePreferredValues(TextGenerationSettings tgs)
        {
            preferredSize = TextGenerator.GetPreferredValues(tgs, textInfoCommon);
        }

        [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
        protected TextInfo Update(TextGenerationSettings tgs)
        {
            screenRect = tgs.screenRect;
            lineHeightDefault = GetLineHeightDefault(tgs);
            isPlaceholder = tgs.isPlaceholder;
            if (!IsDirty(tgs))
                return textInfo;

            TextGenerator.GenerateText(settings, textInfo);

            return textInfo;
        }

        [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
        internal TextInfo UpdateFontAssetPrepared()
        {
            screenRect = settings.screenRect;
            lineHeightDefault = GetLineHeightDefault(settings);
            isPlaceholder = settings.isPlaceholder;
            if (!IsDirty(settings))
                return textInfo;

            generator.Prepare(settings, textInfo);
            generator.GenerateTextMesh(settings, textInfo);
            return textInfo;
        }

        [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
        internal bool PrepareFontAsset()
        {
            if (settings.fontAsset == null)
                return false;
            int hash = settings.GetHashCode();
            if (m_PreviousGenerationSettingsHash == hash && !isDirty && m_IsCached)
                return true;

            bool success = generator.PrepareFontAsset(settings);
            return success;
        }

		[VisibleToOtherModules("UnityEngine.IMGUIModule")]
        internal void UpdatePreferredSize(TextGenerationSettings generationSettings)
        {
            if (textInfo.characterCount <= 0)
                return;

            var maxAscender = float.MinValue;
            var maxDescender = textInfo.textElementInfo[textInfo.characterCount - 1].descender;
            var renderedWidth = 0f;
            var renderedHeight = 0f;

            for (var i = 0; i < textInfo.lineCount; i++)
            {
                var lineInfo = textInfo.lineInfo[i];
                maxAscender = Mathf.Max(maxAscender, textInfo.textElementInfo[lineInfo.firstVisibleCharacterIndex].ascender);
                maxDescender = Mathf.Min(maxDescender, textInfo.textElementInfo[lineInfo.firstVisibleCharacterIndex].descender);

                // UUM-46147: For IMGUI rendered width includes xAdvance for backward compatibility
                renderedWidth = generationSettings.isIMGUI ? Mathf.Max(renderedWidth, lineInfo.length) : Mathf.Max(renderedWidth, lineInfo.lineExtents.max.x - lineInfo.lineExtents.min.x);
            }
            renderedHeight = maxAscender - maxDescender;

            // Adjust Preferred Width and Height to account for Margins.
            renderedWidth += generationSettings.margins.x > 0 ? generationSettings.margins.x : 0;
            renderedWidth += generationSettings.margins.z > 0 ? generationSettings.margins.z : 0;
            renderedHeight += generationSettings.margins.y > 0 ? generationSettings.margins.y : 0;
            renderedHeight += generationSettings.margins.w > 0 ? generationSettings.margins.w : 0;

            // Round Preferred Values to nearest 5/100.
            renderedWidth = (int)(renderedWidth * 100 + 1f) / 100f;
            renderedHeight = (int)(renderedHeight * 100 + 1f) / 100f;

            preferredSize = new Vector2(renderedWidth, renderedHeight);
        }

        [VisibleToOtherModules("UnityEngine.IMGUIModule")]
        internal static float GetLineHeightDefault(TextGenerationSettings settings)
        {
            if (settings != null && settings.fontAsset != null)
            {
                return settings.fontAsset.faceInfo.lineHeight / settings.fontAsset.faceInfo.pointSize * settings.fontSize;
            }
            return 0.0f;
        }
    }
}
