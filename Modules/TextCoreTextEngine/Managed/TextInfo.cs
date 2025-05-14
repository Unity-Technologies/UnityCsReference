// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;
using UnityEngine.Bindings;

namespace UnityEngine.TextCore.Text
{

    /// <summary>
    /// Structure containing information about the individual words contained in the text object.
    /// </summary>
    struct WordInfo
    {
        public int firstCharacterIndex;
        public int lastCharacterIndex;
        public int characterCount;
    }

    /// <summary>
    /// Class which contains information about every element contained within the text object.
    /// </summary>
    [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
    internal class TextInfo
    {
        static Vector2 s_InfinityVectorPositive = new Vector2(32767, 32767);
        static Vector2 s_InfinityVectorNegative = new Vector2(-32767, -32767);

        public int characterCount;
        public int spriteCount;
        public int spaceCount;
        public int wordCount;
        public int linkCount;
        public int lineCount;

        public int materialCount;

        public TextElementInfo[] textElementInfo;
        public WordInfo[] wordInfo;
        public LinkInfo[] linkInfo;
        public LineInfo[] lineInfo;
        public MeshInfo[] meshInfo;

        public double lastTimeInCache;
        public Action removedFromCache;
        public bool hasMultipleColors = false;


        public void RemoveFromCache()
        {
            removedFromCache?.Invoke();
            removedFromCache = null;
        }

        // Default Constructor
        public TextInfo()
        {
            textElementInfo = new TextElementInfo[4];
            wordInfo = new WordInfo[1];
            lineInfo = new LineInfo[1];
            linkInfo = Array.Empty<LinkInfo>();
            meshInfo = Array.Empty<MeshInfo>();
            materialCount = 0;
        }

        /// <summary>
        /// Function to clear the counters of the text object.
        /// </summary>
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal void Clear()
        {
            characterCount = 0;
            spaceCount = 0;
            wordCount = 0;
            linkCount = 0;
            lineCount = 0;
            spriteCount = 0;
            hasMultipleColors = false;

            for (int i = 0; i < meshInfo.Length; i++)
            {
                meshInfo[i].vertexCount = 0;
            }
        }

        /// <summary>
        /// Function to clear the content of the MeshInfo array while preserving the Triangles, Normals and Tangents.
        /// </summary>
        internal void ClearMeshInfo(bool updateMesh)
        {
            for (int i = 0; i < meshInfo.Length; i++)
                meshInfo[i].Clear(updateMesh);
        }

        /// <summary>
        /// Function to clear and initialize the lineInfo array.
        /// </summary>
        internal void ClearLineInfo()
        {
            if (lineInfo == null)
                lineInfo = new LineInfo[1];

            for (int i = 0; i < lineInfo.Length; i++)
            {
                lineInfo[i].characterCount = 0;
                lineInfo[i].spaceCount = 0;
                lineInfo[i].wordCount = 0;
                lineInfo[i].controlCharacterCount = 0;

                lineInfo[i].ascender = s_InfinityVectorNegative.x;
                lineInfo[i].baseline = 0;
                lineInfo[i].descender = s_InfinityVectorPositive.x;
                lineInfo[i].maxAdvance = 0;

                lineInfo[i].marginLeft = 0;
                lineInfo[i].marginRight = 0;

                lineInfo[i].lineExtents.min = s_InfinityVectorPositive;
                lineInfo[i].lineExtents.max = s_InfinityVectorNegative;
                lineInfo[i].width = 0;
            }
        }


        /// <summary>
        /// Function to resize any of the structure contained in the TextInfo class.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="size"></param>
        internal static void Resize<T>(ref T[] array, int size)
        {
            // Allocated to the next power of two
            int newSize = size > 1024 ? size + 256 : Mathf.NextPowerOfTwo(size);

            Array.Resize(ref array, newSize);
        }

        /// <summary>
        /// Function to resize any of the structure contained in the TextInfo class.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="size"></param>
        /// <param name="isBlockAllocated"></param>
        internal static void Resize<T>(ref T[] array, int size, bool isBlockAllocated)
        {
            if (isBlockAllocated) size = size > 1024 ? size + 256 : Mathf.NextPowerOfTwo(size);

            if (size == array.Length) return;

            Array.Resize(ref array, size);
        }

        public virtual Vector2 GetCursorPositionFromStringIndexUsingCharacterHeight(int index, Rect screenRect, float lineHeight, bool inverseYAxis = true)
        {
            var result = screenRect.position;
            if (characterCount == 0)
                return inverseYAxis ? new Vector2(0, lineHeight) : result;

            var validIndex = index >= characterCount ? characterCount - 1 : index;
            var character = textElementInfo[validIndex];
            var descender = character.descender;
            var vectorX = index >= characterCount ? character.xAdvance : character.origin;

            result += inverseYAxis ?
                new Vector2(vectorX, screenRect.height - descender) :
                new Vector2(vectorX, descender);

            return result;
        }

        public Vector2 GetCursorPositionFromStringIndexUsingLineHeight(int index, Rect screenRect, float lineHeight, bool useXAdvance = false, bool inverseYAxis = true)
        {
            var result = screenRect.position;
            if (characterCount == 0 || index < 0)
                return inverseYAxis ? new Vector2(0, lineHeight) : result;

            if (index >= characterCount)
                index = characterCount - 1;

            var character = textElementInfo[index];
            var line = lineInfo[character.lineNumber];

            if (index >= characterCount - 1 || useXAdvance)
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
        public int GetCursorIndexFromPosition(Vector2 position, Rect screenRect, bool inverseYAxis = true)
        {
            if (inverseYAxis)
                position.y = screenRect.height - position.y;

            var lineNumber = 0;
            if (lineCount > 1)
                lineNumber = FindNearestLine(position);

            var index = FindNearestCharacterOnLine(position, lineNumber, false);
            var cInfo = textElementInfo[index];

            // Get Bottom Left and Top Right position of the current character
            Vector3 bl = cInfo.bottomLeft;
            Vector3 tr = cInfo.topRight;

            float insertPosition = (position.x - bl.x) / (tr.x - bl.x);

            return (insertPosition < 0.5f) || cInfo.character == '\n' ? index : index + 1;
        }

        public int LineDownCharacterPosition(int originalPos)
        {
            if (originalPos >= characterCount)
                return characterCount - 1; // text.Length;

            var originChar = textElementInfo[originalPos];
            int originLine = originChar.lineNumber;

            //// We are on the last line return last character
            if (originLine + 1 >= lineCount)
                return characterCount - 1;

            // Need to determine end line for next line.
            int endCharIdx = lineInfo[originLine + 1].lastCharacterIndex;

            int closest = -1;
            float distance = Mathf.Infinity;
            float range = 0;

            for (int i = lineInfo[originLine + 1].firstCharacterIndex; i < endCharIdx; ++i)
            {
                var currentChar = textElementInfo[i];

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
            if (originalPos >= characterCount)
                originalPos -= 1;

            var originChar = textElementInfo[originalPos];
            int originLine = originChar.lineNumber;

            // We are on the first line return first character
            if (originLine - 1 < 0)
                return 0;

            int endCharIdx = lineInfo[originLine].firstCharacterIndex - 1;

            int closest = -1;
            float distance = Mathf.Infinity;
            float range = 0;

            for (int i = lineInfo[originLine - 1].firstCharacterIndex; i < endCharIdx; ++i)
            {
                var currentChar = textElementInfo[i];

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
            for (int i = 0; i < wordCount; i++)
            {
                var word = wordInfo[i];
                if (word.firstCharacterIndex <= cursorIndex && word.lastCharacterIndex >= cursorIndex)
                    return i;
            }

            return -1;
        }

        public int FindNearestLine(Vector2 position)
        {
            float distance = Mathf.Infinity;
            int closest = -1;

            for (int i = 0; i < lineCount; i++)
            {
                var line = lineInfo[i];

                float ascender = line.ascender;
                float descender = line.descender;

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
            if (line >= lineInfo.Length || line < 0)
                return 0;
            int firstCharacter = lineInfo[line].firstCharacterIndex;
            int lastCharacter = lineInfo[line].lastCharacterIndex;

            float distanceSqr = Mathf.Infinity;
            int closest = lastCharacter;


            for (int i = firstCharacter; i <= lastCharacter; i++)
            {
                // Get current character info.
                var cInfo = textElementInfo[i];
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
        public int FindIntersectingLink(Vector3 position, Rect screenRect, bool inverseYAxis = true)
        {
            if (inverseYAxis)
                position.y = screenRect.height - position.y;

            for (int i = 0; i < linkCount; i++)
            {
                var link = linkInfo[i];

                bool isBeginRegion = false;

                Vector3 bl = Vector3.zero;
                Vector3 tl = Vector3.zero;
                Vector3 br = Vector3.zero;
                Vector3 tr = Vector3.zero;

                // Iterate through each character of the word
                for (int j = 0; j < link.linkTextLength; j++)
                {
                    int characterIndex = link.linkTextfirstCharacterIndex + j;
                    var currentCharInfo = textElementInfo[characterIndex];
                    int currentLine = currentCharInfo.lineNumber;

                    if (!isBeginRegion)
                    {
                        isBeginRegion = true;

                        bl = new Vector3(currentCharInfo.bottomLeft.x, currentCharInfo.descender, 0);
                        tl = new Vector3(currentCharInfo.bottomLeft.x, currentCharInfo.ascender, 0);

                        //Debug.Log("Start Word Region at [" + currentCharInfo.character + "]");

                        // If Word is one character
                        if (link.linkTextLength == 1)
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
                    if (isBeginRegion && j == link.linkTextLength - 1)
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
                    else if (isBeginRegion && currentLine != textElementInfo[characterIndex + 1].lineNumber)
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
        public int GetCorrespondingStringIndex(int index)
        {
            if (index <= 0)
                return 0;
            return textElementInfo[index - 1].index + textElementInfo[index - 1].stringLength;
        }

        public int GetCorrespondingCodePointIndex(int stringIndex)
        {
            if (stringIndex <= 0)
                return 0;

            for (int i = 0; i < characterCount; i++)
            {
                var character = textElementInfo[i];
                if (character.index + character.stringLength >= stringIndex)
                    return i + 1;
            }

            return characterCount;
        }

        public LineInfo GetLineInfoFromCharacterIndex(int index)
        {
            return lineInfo[GetLineNumber(index)];
        }

        private static bool PointIntersectRectangle(Vector3 m, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            // A zero area rectangle is not valid for this method.
            Vector3 normal = Vector3.Cross(b - a, d - a);
            if (normal == Vector3.zero)
                return false;

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
            // If a and b are the same point, just return distance to that point
            if (a == b)
            {
                Vector3 diff = point - a;
                return Vector3.Dot(diff, diff);
            }

            Vector3 n = b - a;
            Vector3 pa = a - point;

            float c = Vector3.Dot(n, pa);

            // Closest point is a
            if (c > 0.0f)
                return Vector3.Dot(pa, pa);

            Vector3 bp = point - b;

            // Closest point is b
            if (Vector3.Dot(n, bp) > 0.0f)
                return Vector3.Dot(bp, bp);

            // Closest point is between a and b
            Vector3 e = pa - n * (c / Vector3.Dot(n, n));

            return Vector3.Dot(e, e);
        }

        public int GetLineNumber(int index)
        {
            if (index <= 0)
                index = 0;

            if (index >= characterCount)
                index = Mathf.Max(0, characterCount - 1);

            return textElementInfo[index].lineNumber;
        }

        public float GetLineHeight(int lineNumber)
        {
            if (lineNumber <= 0)
                lineNumber = 0;

            if (lineNumber >= lineCount)
                lineNumber = Mathf.Max(0, lineCount - 1);

            return lineInfo[lineNumber].lineHeight;
        }

        public float GetLineHeightFromCharacterIndex(int index)
        {
            if (index <= 0)
                index = 0;

            if (index >= characterCount)
                index = Mathf.Max(0, characterCount - 1);
            return GetLineHeight(textElementInfo[index].lineNumber);
        }

        public float GetCharacterHeightFromIndex(int index)
        {
            if (index <= 0)
                index = 0;

            if (index >= characterCount)
                index = Mathf.Max(0, characterCount - 1);

            var characterInfo = textElementInfo[index];
            return characterInfo.ascender - characterInfo.descender;
        }


        /// <summary>
        // Retrieves a substring from this instance.
        /// </summary>
        public string Substring(int startIndex, int length)
        {
            if (startIndex < 0 || startIndex + length > characterCount)
                throw new ArgumentOutOfRangeException();

            var result = new StringBuilder(length);
            for (int i = startIndex; i < startIndex + length; ++i)
            {
                var codePoint = textElementInfo[i].character;
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
            if (startIndex < 0 || startIndex >= characterCount)
                throw new ArgumentOutOfRangeException();

            for (int i = startIndex; i < characterCount; i++)
            {
                if (textElementInfo[i].character == value)
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
            if (startIndex < 0 || startIndex >= characterCount)
                throw new ArgumentOutOfRangeException();

            for (int i = startIndex; i >= 0; i--)
            {
                if (textElementInfo[i].character == value)
                    return i;
            }

            return -1;
        }
    }
}
