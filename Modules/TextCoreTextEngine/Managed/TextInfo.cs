// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.TextCore.Text
{
    struct PageInfo
    {
        public int firstCharacterIndex;
        public int lastCharacterIndex;
        public float ascender;
        public float baseLine;
        public float descender;
    }

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
    class TextInfo
    {
        static Vector2 s_InfinityVectorPositive = new Vector2(32767, 32767);
        static Vector2 s_InfinityVectorNegative = new Vector2(-32767, -32767);

        public int characterCount;
        public int spriteCount;
        public int spaceCount;
        public int wordCount;
        public int linkCount;
        public int lineCount;
        public int pageCount;

        public int materialCount;

        public TextElementInfo[] textElementInfo;
        public WordInfo[] wordInfo;
        public LinkInfo[] linkInfo;
        public LineInfo[] lineInfo;
        public PageInfo[] pageInfo;
        public MeshInfo[] meshInfo;

        public double lastTimeInCache;
        public Action removedFromCache;
        public VertexDataLayout vertexDataLayout { get; private set; }
        public bool hasMultipleColors = false;


        public void RemoveFromCache()
        {
            removedFromCache?.Invoke();
            removedFromCache = null;
        }

        // Default Constructor
        public TextInfo(VertexDataLayout vertexDataLayout)
        {
            this.vertexDataLayout = vertexDataLayout;
            textElementInfo = new TextElementInfo[4];
            wordInfo = new WordInfo[1];
            lineInfo = new LineInfo[1];
            pageInfo = new PageInfo[1];
            linkInfo = Array.Empty<LinkInfo>();
            meshInfo = Array.Empty<MeshInfo>();
            materialCount = 0;
        }

        /// <summary>
        /// Function to clear the counters of the text object.
        /// </summary>
        internal void Clear()
        {
            characterCount = 0;
            spaceCount = 0;
            wordCount = 0;
            linkCount = 0;
            lineCount = 0;
            pageCount = 0;
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
        ///
        /// </summary>
        internal void ClearPageInfo()
        {
            if (pageInfo == null)
                pageInfo = new PageInfo[2];

            int length = pageInfo.Length;

            for (int i = 0; i < length; i++)
            {
                pageInfo[i].firstCharacterIndex = 0;
                pageInfo[i].lastCharacterIndex = 0;
                pageInfo[i].ascender = -32767;
                pageInfo[i].baseLine = 0;
                pageInfo[i].descender = 32767;
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
    }
}
