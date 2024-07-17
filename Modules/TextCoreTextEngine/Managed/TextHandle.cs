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
    internal partial class TextHandle
    {
        public TextHandle()
        {
        }

        ~TextHandle()
        {
            RemoveTextInfoFromTemporaryCache();
            RemoveTextInfoFromPermanentCache();
        }

        internal static TextHandleTemporaryCache s_TemporaryCache = new TextHandleTemporaryCache();
        internal static TextHandlePermanentCache s_PermanentCache = new TextHandlePermanentCache();

        [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
        internal static void InitThreadArrays()
        {
            if (s_Settings != null && s_Generators != null && s_TextInfosCommon != null)
                return;

            InitArray(ref s_Settings, () => new TextGenerationSettings());
            InitArray(ref s_Generators, () => new TextGenerator());
            InitArray(ref s_TextInfosCommon, () => new TextInfo(VertexDataLayout.VBO));
        }

        static TextGenerationSettings[] s_Settings;
        internal static TextGenerationSettings[] settingsArray
        {
            get
            {
                if (s_Settings == null)
                {
                    InitArray(ref s_Settings, () => new TextGenerationSettings());
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
                    InitArray(ref s_Generators, () => new TextGenerator());
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
                    InitArray(ref s_TextInfosCommon, () => new TextInfo(VertexDataLayout.VBO));
                }
                return s_TextInfosCommon;
            }
        }

        private static void InitArray<T>(ref T[] array, Func<T> createInstance)
        {
            if (array != null)
                return;
            array = new T[JobsUtility.ThreadIndexCount];
            for (int i = 0; i < JobsUtility.ThreadIndexCount; i++)
            {
                array[i] = createInstance();
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

        [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
        internal Vector2 preferredSize { get; set; }

        private Rect m_ScreenRect;
        private float m_LineHeightDefault;
        private bool m_IsPlaceholder;
        private bool m_IsEllided;

        internal LinkedListNode<TextInfo> TextInfoNode { get; set; }
        internal bool IsCachedPermanent { get; set; }
        internal bool IsCachedTemporary { get; set; }

        public virtual void AddTextInfoToPermanentCache()
        {
            s_PermanentCache.AddTextInfoToCache(this);
        }

        public void AddTextInfoToTemporaryCache(int hashCode)
        {
            s_TemporaryCache.AddTextInfoToCache(this, hashCode);
        }

        public void RemoveTextInfoFromTemporaryCache()
        {
            s_TemporaryCache.RemoveTextInfoFromCache(this);
        }

        public void RemoveTextInfoFromPermanentCache()
        {
            s_PermanentCache.RemoveTextInfoFromCache(this);
        }

        public static void UpdateCurrentFrame()
        {
            s_TemporaryCache.UpdateCurrentFrame();
        }

        /// <summary>
        /// The TextInfo instance, use from this instead of the m_TextInfo member.
        /// References a cached textInfo if dynamic, or a static instance (textInfoCommon) if not cached.
        /// </summary>
        internal TextInfo textInfo
        {
            [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
            get
            {

                if (TextInfoNode == null)
                    return textInfoCommon;
                else
                    return TextInfoNode.Value;
            }
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

        public bool IsDirty(int hashCode)
        {
            if (m_PreviousGenerationSettingsHash == hashCode && !isDirty && (IsCachedTemporary || IsCachedPermanent))
                return false;

            return true;
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

        public bool IsPlaceholder
        {
            get => m_IsPlaceholder;
        }

        public bool IsElided()
        {
            if (textInfo == null)
                return false;

            if (textInfo.characterCount == 0) // impossible to differentiate between an empty string and a fully truncated string.
                return true;

            return m_IsEllided;
        }

        protected void UpdatePreferredValues(TextGenerationSettings tgs)
        {
            preferredSize = generator.GetPreferredValues(tgs, textInfoCommon);
        }

        [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
        internal TextInfo Update()
        {
            return UpdateWithHash(settings.GetHashCode());
        }

        [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
        internal TextInfo UpdateWithHash(int hashCode)
        {
            m_ScreenRect = settings.screenRect;
            m_LineHeightDefault = GetLineHeightDefault(settings);
            m_IsPlaceholder = settings.isPlaceholder;
            if (!IsDirty(hashCode))
                return textInfo;

            if (settings.fontAsset == null || settings.fontAsset.characterLookupTable == null)
            {
                Debug.LogWarning("Can't Generate Mesh, No Font Asset has been assigned.");
                return textInfo;
            }

            generator.GenerateText(settings, textInfo);
            m_PreviousGenerationSettingsHash = hashCode;
            isDirty = false;
            m_IsEllided = generator.isTextTruncated;

            return textInfo;
        }

        [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
        internal bool PrepareFontAsset()
        {
            if (settings.fontAsset == null)
                return false;

            if (!IsDirty(settings.GetHashCode()))
                return true;

            bool success = generator.PrepareFontAsset(settings);
            return success;
        }

		[VisibleToOtherModules("UnityEngine.IMGUIModule")]
        internal void UpdatePreferredSize()
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
                renderedWidth = settings.isIMGUI ? Mathf.Max(renderedWidth, lineInfo.length) : Mathf.Max(renderedWidth, lineInfo.lineExtents.max.x - lineInfo.lineExtents.min.x);
            }
            renderedHeight = maxAscender - maxDescender;

            // Adjust Preferred Width and Height to account for Margins.
            renderedWidth += settings.margins.x > 0 ? settings.margins.x : 0;
            renderedWidth += settings.margins.z > 0 ? settings.margins.z : 0;
            renderedHeight += settings.margins.y > 0 ? settings.margins.y : 0;
            renderedHeight += settings.margins.w > 0 ? settings.margins.w : 0;

            // Round Preferred Values to nearest 5/100.
            renderedWidth = (int)(renderedWidth * 100 + 1f) / 100f;
            renderedHeight = (int)(renderedHeight * 100 + 1f) / 100f;

            preferredSize = new Vector2(renderedWidth, renderedHeight);
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static float ConvertPixelUnitsToTextCoreRelativeUnits(float fontSize, FontAsset fontAsset)
        {
            // Convert the text settings pixel units to TextCore relative units
            float paddingPercent = 1.0f / fontAsset.atlasPadding;
            float pointSizeRatio = ((float)fontAsset.faceInfo.pointSize) / fontSize;
            return paddingPercent * pointSizeRatio;
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

        public virtual Vector2 GetCursorPositionFromStringIndexUsingCharacterHeight(int index, bool inverseYAxis = true)
        {
            AddTextInfoToPermanentCache();
            return textInfo.GetCursorPositionFromStringIndexUsingCharacterHeight(index, m_ScreenRect, m_LineHeightDefault, inverseYAxis);
        }

        public Vector2 GetCursorPositionFromStringIndexUsingLineHeight(int index, bool useXAdvance = false, bool inverseYAxis = true)
        {
            AddTextInfoToPermanentCache();
            return textInfo.GetCursorPositionFromStringIndexUsingLineHeight(index, m_ScreenRect, m_LineHeightDefault, useXAdvance, inverseYAxis);
        }

        //TODO add special handling for 1 character...
        // Add support for world space.
        public int GetCursorIndexFromPosition(Vector2 position, bool inverseYAxis = true)
        {
            return textInfo.GetCursorIndexFromPosition(position, m_ScreenRect, inverseYAxis);
        }

        public int LineDownCharacterPosition(int originalPos)
        {
            return textInfo.LineDownCharacterPosition(originalPos);
        }

        public int LineUpCharacterPosition(int originalPos)
        {
            return textInfo.LineUpCharacterPosition(originalPos);
        }

        // This could be improved if TextElementInfo had a reference to the word index.
        public int FindWordIndex(int cursorIndex)
        {
            return textInfo.FindWordIndex(cursorIndex);
        }

        public int FindNearestLine(Vector2 position)
        {
            return textInfo.FindNearestLine(position);
        }

        public int FindNearestCharacterOnLine(Vector2 position, int line, bool visibleOnly)
        {
            return textInfo.FindNearestCharacterOnLine(position, line, visibleOnly);
        }

        /// <summary>
        /// Function returning the index of the Link at the given position (if any).
        /// </summary>
        /// <returns></returns>
        public int FindIntersectingLink(Vector3 position, bool inverseYAxis = true)
        {
            return textInfo.FindIntersectingLink(position, m_ScreenRect, inverseYAxis);
        }

        public int GetCorrespondingStringIndex(int index)
        {
            return textInfo.GetCorrespondingStringIndex(index);
        }

        public int GetCorrespondingCodePointIndex(int stringIndex)
        {
            return textInfo.GetCorrespondingCodePointIndex(stringIndex);
        }

        public LineInfo GetLineInfoFromCharacterIndex(int index)
        {
            return textInfo.GetLineInfoFromCharacterIndex(index);
        }

        public int GetLineNumber(int index)
        {
            return textInfo.GetLineNumber(index);
        }

        public float GetLineHeight(int lineNumber)
        {
            return textInfo.GetLineHeight(lineNumber);
        }

        public float GetLineHeightFromCharacterIndex(int index)
        {
            return textInfo.GetLineHeightFromCharacterIndex(index);
        }

        public float GetCharacterHeightFromIndex(int index)
        {
            return textInfo.GetCharacterHeightFromIndex(index);
        }


        /// <summary>
        // Retrieves a substring from this instance.
        /// </summary>
        public string Substring(int startIndex, int length)
        {
            return textInfo.Substring(startIndex, length);
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
            return textInfo.IndexOf(value, startIndex);
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
            return textInfo.LastIndexOf(value, startIndex);
        }
    }
}
