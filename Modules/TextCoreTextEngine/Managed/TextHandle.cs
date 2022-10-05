// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.TextCore.Text
{
    internal class TextHandle
    {
        Vector2 m_PreferredSize;
        /// <summary>
        /// DO NOT USE m_TextInfo directly, use textInfo to guarantee lazy allocation.
        /// </summary>
        private TextInfo m_TextInfo;

        /// <summary>
        /// The TextInfo instance, use from this instead of the m_TextInfo member to guarantee lazy allocation.
        /// </summary>
        internal TextInfo textInfo
        {
            get
            {
                if (m_TextInfo == null)
                {
                    m_TextInfo = new TextInfo();
                }

                return m_TextInfo;
            }
        }

        /// <summary>
        /// DO NOT USE m_LayoutTextInfo directly, use textInfo to guarantee lazy allocation.
        /// </summary>
        static TextInfo m_LayoutTextInfo;

        /// <summary>
        /// The TextInfo instance to be used for layout, use from this instead of the m_LayoutTextInfo member to guarantee lazy allocation.
        /// </summary>
        internal static TextInfo layoutTextInfo
        {
            get
            {
                if (m_LayoutTextInfo == null)
                {
                    m_LayoutTextInfo = new TextInfo();
                }

                return m_LayoutTextInfo;
            }
        }

        internal TextGenerationSettings textGenerationSettings;

        public Vector2 GetCursorPositionFromStringIndexUsingCharacterHeight(int index, bool inverseYAxis = true)
        {
            if (textGenerationSettings == null)
                return Vector2.zero;

            var screenRect = textGenerationSettings.screenRect;
            var result = screenRect.position;
            if (textInfo.characterCount == 0)
                return result;

            var validIndex = index >= textInfo.characterCount ? textInfo.characterCount - 1 : index;
            var character = textInfo.textElementInfo[validIndex];
            var descender = character.descender;
            var vectorX = index >= textInfo.characterCount ? character.xAdvance : character.origin;

            result += inverseYAxis ?
                new Vector2(vectorX, screenRect.height - descender) :
                new Vector2(vectorX, descender);

            return result;
        }

        public Vector2 GetCursorPositionFromStringIndexUsingLineHeight(int index, bool rightCursorPosition = false, bool inverseYAxis = true)
        {
            if (textGenerationSettings == null)
                return Vector2.zero;

            var screenRect = textGenerationSettings.screenRect;
            var result = screenRect.position;
            if (textInfo.characterCount == 0)
                return result;

            if (index >= textInfo.characterCount)
                index = textInfo.characterCount - 1;

            var character = textInfo.textElementInfo[index];
            var line = textInfo.lineInfo[character.lineNumber];

            if (index >= textInfo.characterCount - 1 || rightCursorPosition)
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
            if (textGenerationSettings == null)
                return 0;

            if (inverseYAxis)
                position.y = textGenerationSettings.screenRect.height - position.y;

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
            int firstCharacter = textInfo.lineInfo[line].firstCharacterIndex;
            int lastCharacter = textInfo.lineInfo[line].lastCharacterIndex;

            float distanceSqr = Mathf.Infinity;
            int closest = lastCharacter;


            for (int i = firstCharacter; i < lastCharacter; i++)
            {
                // Get current character info.
                var cInfo = textInfo.textElementInfo[i];
                if (visibleOnly && !cInfo.isVisible) continue;

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
        public float ComputeTextWidth(TextGenerationSettings tgs)
        {
            UpdatePreferredValues(tgs);
            return m_PreferredSize.x;
        }

        public float ComputeTextHeight(TextGenerationSettings tgs)
        {
            UpdatePreferredValues(tgs);
            return m_PreferredSize.y;
        }

        void UpdatePreferredValues(TextGenerationSettings tgs)
        {
            m_PreferredSize = TextGenerator.GetPreferredValues(tgs, layoutTextInfo);
        }

        public int GetLineNumber(int index)
        {
            if (index <= 0)
                index = 0;
            else if (index >= textInfo.characterCount)
                index = Mathf.Max(0, textInfo.characterCount - 1);

            return textInfo.textElementInfo[index].lineNumber;
        }

        public float GetLineHeight(int lineNumber)
        {
            if (lineNumber <= 0)
                lineNumber = 0;
            else if (lineNumber >= textInfo.lineCount)
                lineNumber = Mathf.Max(0, textInfo.lineCount - 1);

            return textInfo.lineInfo[lineNumber].lineHeight;
        }

        public float GetLineHeightFromCharacterIndex(int index)
        {
            if (index <= 0)
                index = 0;
            else if (index >= textInfo.characterCount)
                index = Mathf.Max(0, textInfo.characterCount - 1);
            return GetLineHeight(textInfo.textElementInfo[index].lineNumber);
        }

        public float GetCharacterHeightFromIndex(int index)
        {
            if (index <= 0)
                index = 0;
            else if (index >= textInfo.characterCount)
                index = Mathf.Max(0, textInfo.characterCount - 1);

            var characterInfo = textInfo.textElementInfo[index];
            return characterInfo.ascender - characterInfo.descender;
        }

        public TextInfo Update(TextGenerationSettings tgs)
        {
            textInfo.isDirty = true;
            TextGenerator.GenerateText(tgs, textInfo);
            textGenerationSettings = tgs;
            return textInfo;
        }

        // For testing purposes
        internal bool IsTextInfoAllocated()
        {
            return m_TextInfo != null;
        }

        public bool IsElided()
        {
            if (textInfo == null)
                return false;

            if (textInfo.characterCount == 0) // impossible to differentiate between an empty string and a fully truncated string.
                return true;

            return textInfo.textElementInfo[textInfo.characterCount - 1].character == 0x2026; //the character is hardcoded in textcore
        }
    }
}
