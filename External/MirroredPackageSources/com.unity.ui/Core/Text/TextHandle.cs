using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.UIElements.UIR.Implementation;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

namespace UnityEngine.UIElements
{
    internal interface ITextHandle
    {
        Vector2 GetCursorPosition(CursorPositionStylePainterParameters parms, float scaling);
        float ComputeTextWidth(MeshGenerationContextUtils.TextParams parms, float scaling);
        float ComputeTextHeight(MeshGenerationContextUtils.TextParams parms, float scaling);
        float GetLineHeight(int characterIndex, MeshGenerationContextUtils.TextParams textParams, float textScaling,
            float pixelPerPoint);

        void DrawText(UIRStylePainter painter, MeshGenerationContextUtils.TextParams textParams, float pixelsPerPoint);
        TextInfo Update(MeshGenerationContextUtils.TextParams parms, float pixelsPerPoint);
        int VerticesCount(MeshGenerationContextUtils.TextParams parms, float pixelPerPoint);
        ITextHandle New();
    }

    internal struct TextCoreHandle : ITextHandle
    {
        public static ITextHandle New()
        {
            TextCoreHandle h = new TextCoreHandle();
            h.m_CurrentGenerationSettings = new TextCore.TextGenerationSettings();
            h.m_CurrentLayoutSettings = new TextCore.TextGenerationSettings();
            return h;
        }

        // At the moment, FontAssets are not part of Styles. They're created on runtime and cached
        static Dictionary<Font, FontAsset> fontAssetCache = new Dictionary<Font, FontAsset>();
        static FontAsset GetFontAsset(Font font)
        {
            FontAsset fontAsset = null;
            if (fontAssetCache.TryGetValue(font, out fontAsset) && fontAsset != null)
                return fontAsset;
            fontAsset = FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                FontAsset.AtlasPopulationMode.Dynamic);
            return fontAssetCache[font] = fontAsset;
        }

        Vector2 m_PreferredSize;
        int m_PreviousGenerationSettingsHash;
        TextCore.TextGenerationSettings m_CurrentGenerationSettings;
        int m_PreviousLayoutSettingsHash;
        TextCore.TextGenerationSettings m_CurrentLayoutSettings;
        /// <summary>
        /// DO NOT USE m_TextInfo directly, use textInfo to guarantee lazy allocation.
        /// </summary>
        private TextCore.TextInfo m_TextInfo;
        /// <summary>
        /// The TextInfo instance, use from this instead of the m_TextInfo member to guarantee lazy allocation.
        /// </summary>
        internal TextCore.TextInfo textInfo
        {
            get
            {
                if (m_TextInfo == null)
                {
                    m_TextInfo = new TextCore.TextInfo();
                }
                return m_TextInfo;
            }
        }
        /// <summary>
        /// DO NOT USE m_UITKTextInfo directly, use uITKTextInfo to guarantee lazy allocation.
        /// </summary>
        private TextInfo m_UITKTextInfo;
        /// <summary>
        /// The TextInfo instance, use from this instead of the m_UITKTextInfo member to guarantee lazy allocation.
        /// </summary>
        internal TextInfo uITKTextInfo
        {
            get
            {
                if (m_UITKTextInfo == null)
                {
                    m_UITKTextInfo = new TextInfo();
                }
                return m_UITKTextInfo;
            }
        }
        internal bool IsTextInfoAllocated()
        {
            return m_TextInfo != null;
        }

        public Vector2 GetCursorPosition(CursorPositionStylePainterParameters parms, float scaling)
        {
            return TextCore.TextGenerator.GetCursorPosition(textInfo, parms.rect, parms.cursorIndex);
        }

        public float ComputeTextWidth(MeshGenerationContextUtils.TextParams parms, float scaling)
        {
            UpdatePreferredValues(parms);
            return m_PreferredSize.x;
        }

        public float ComputeTextHeight(MeshGenerationContextUtils.TextParams parms, float scaling)
        {
            UpdatePreferredValues(parms);
            return m_PreferredSize.y;
        }

        public void DrawText(UIRStylePainter painter, MeshGenerationContextUtils.TextParams textParams, float pixelsPerPoint)
        {
            painter.DrawTextCore(textParams, this, pixelsPerPoint);
        }

        public int VerticesCount(MeshGenerationContextUtils.TextParams parms, float pixelPerPoint)
        {
            Update(parms, pixelPerPoint);
            var verticesCount = 0;
            foreach (var meshInfo in textInfo.meshInfo)
                verticesCount += meshInfo.vertexCount;
            return verticesCount;
        }

        ITextHandle ITextHandle.New()
        {
            return New();
        }

        public TextInfo Update(MeshGenerationContextUtils.TextParams parms, float pixelsPerPoint)
        {
            // The screenRect in TextCore is not properly implemented with regards to the offset part, so zero it out for now and we will add it ourselves later
            parms.rect = new Rect(Vector2.zero, parms.rect.size);
            int paramsHash = parms.GetHashCode();
            if (m_PreviousGenerationSettingsHash == paramsHash)
                return uITKTextInfo;
            UpdateGenerationSettingsCommon(parms, m_CurrentGenerationSettings);
            m_CurrentGenerationSettings.color = parms.fontColor;
            m_CurrentGenerationSettings.inverseYAxis = true;
            m_CurrentGenerationSettings.scale = pixelsPerPoint;
            m_CurrentGenerationSettings.overflowMode = GetTextOverflowMode(parms);
            textInfo.isDirty = true;
            TextCore.TextGenerator.GenerateText(m_CurrentGenerationSettings, textInfo);
            m_PreviousGenerationSettingsHash = paramsHash;
            return ConvertTo(textInfo);
        }

        public float GetLineHeight(int characterIndex, MeshGenerationContextUtils.TextParams textParams, float textScaling, float pixelPerPoint)
        {
            Update(textParams, pixelPerPoint);
            var character = m_TextInfo.textElementInfo[m_TextInfo.characterCount - 1];
            var line = m_TextInfo.lineInfo[character.lineNumber];
            return line.lineHeight;
        }

        private TextMeshInfo ConvertTo(UnityEngine.TextCore.MeshInfo meshInfo)
        {
            TextMeshInfo result;
            result.vertexCount = meshInfo.vertexCount;
            result.vertices = meshInfo.vertices;
            result.uvs0 = meshInfo.uvs0;
            result.uvs2 = meshInfo.uvs2;
            result.colors32 = meshInfo.colors32;
            result.triangles = meshInfo.triangles;
            result.material = meshInfo.material;
            return result;
        }

        private void ConvertTo(MeshInfo[] meshInfos, List<TextMeshInfo> result)
        {
            result.Clear();
            for (int i = 0; i < meshInfos.Length; i++)
                result.Add(ConvertTo(meshInfos[i]));
        }

        private TextInfo ConvertTo(UnityEngine.TextCore.TextInfo textInfo)
        {
            uITKTextInfo.materialCount = textInfo.materialCount;
            ConvertTo(textInfo.meshInfo, uITKTextInfo.meshInfos);
            return uITKTextInfo;
        }

        void UpdatePreferredValues(MeshGenerationContextUtils.TextParams parms)
        {
            // The screenRect in TextCore is not properly implemented with regards to the offset part, so zero it out for now and we will add it ourselves later
            parms.rect = new Rect(Vector2.zero, parms.rect.size);
            int paramsHash = parms.GetHashCode();
            if (m_PreviousLayoutSettingsHash == paramsHash)
                return;
            UpdateGenerationSettingsCommon(parms, m_CurrentLayoutSettings);
            m_PreferredSize = TextCore.TextGenerator.GetPreferredValues(m_CurrentLayoutSettings, textInfo);
            m_PreviousLayoutSettingsHash = paramsHash;
        }

        private static TextOverflowMode GetTextOverflowMode(MeshGenerationContextUtils.TextParams textParams)
        {
            if (textParams.textOverflow == TextOverflow.Clip)
                return TextOverflowMode.Masking;
            if (textParams.textOverflow != TextOverflow.Ellipsis)
                return TextOverflowMode.Overflow;
            if (!textParams.wordWrap && textParams.overflow == OverflowInternal.Hidden)
                return TextOverflowMode.Ellipsis;
            return TextOverflowMode.Overflow;
        }

        static void UpdateGenerationSettingsCommon(MeshGenerationContextUtils.TextParams painterParams,
            TextCore.TextGenerationSettings settings)
        {
            settings.fontAsset = GetFontAsset(painterParams.font);
            settings.material = settings.fontAsset.material;
            // in case rect is not properly set (ex: style has not been resolved), make sure its width at least matches wordWrapWidth
            var screenRect = painterParams.rect;
            if (float.IsNaN(screenRect.width))
                screenRect.width = painterParams.wordWrapWidth;
            settings.screenRect = screenRect;
            settings.text = string.IsNullOrEmpty(painterParams.text) ? " " : painterParams.text;
            settings.fontSize = painterParams.fontSize > 0 ? painterParams.fontSize : painterParams.font.fontSize;
            settings.fontStyle = TextGeneratorUtilities.LegacyStyleToNewStyle(painterParams.fontStyle);
            settings.textAlignment = TextGeneratorUtilities.LegacyAlignmentToNewAlignment(painterParams.anchor);
            settings.wordWrap = painterParams.wordWrap;
            settings.richText = false;
            settings.overflowMode = TextOverflowMode.Overflow;
        }
    }

    /// <summary>
    /// DO NOT USE TextHandle. This struct is only there for backward compatibility reason and will soon be stripped.
    /// </summary>
    internal struct TextHandle : ITextHandle
    {
        internal ITextHandle textHandle;
        public Vector2 GetCursorPosition(CursorPositionStylePainterParameters parms, float scaling)
        {
            return textHandle.GetCursorPosition(parms, scaling);
        }

        public float GetLineHeight(int characterIndex, MeshGenerationContextUtils.TextParams textParams, float textScaling, float pixelPerPoint)
        {
            return textHandle.GetLineHeight(characterIndex, textParams, textScaling, pixelPerPoint);
        }

        public float ComputeTextWidth(MeshGenerationContextUtils.TextParams parms, float scaling)
        {
            return textHandle.ComputeTextWidth(parms, scaling);
        }

        public float ComputeTextHeight(MeshGenerationContextUtils.TextParams parms, float scaling)
        {
            return textHandle.ComputeTextHeight(parms, scaling);
        }

        public void DrawText(UIRStylePainter painter, MeshGenerationContextUtils.TextParams textParams, float pixelsPerPoint)
        {
            textHandle.DrawText(painter, textParams, pixelsPerPoint);
        }

        public TextInfo Update(MeshGenerationContextUtils.TextParams parms, float pixelsPerPoint)
        {
            return textHandle.Update(parms, pixelsPerPoint);
        }

        public int VerticesCount(MeshGenerationContextUtils.TextParams parms, float pixelPerPoint)
        {
            return textHandle.VerticesCount(parms, pixelPerPoint);
        }

        public ITextHandle New()
        {
            return textHandle.New();
        }
    }


    internal struct TextNativeHandle : ITextHandle
    {
        // For automated testing purposes
        internal NativeArray<TextVertex> textVertices;
        private int m_PreviousTextParamsHash;

        public static ITextHandle New()
        {
            TextNativeHandle h = new TextNativeHandle();
            h.textVertices = new NativeArray<TextVertex>();
            return h;
        }

        ITextHandle ITextHandle.New()
        {
            return New();
        }

        public float GetLineHeight(int characterIndex, MeshGenerationContextUtils.TextParams textParams, float textScaling, float pixelPerPoint)
        {
            textParams.wordWrapWidth = 0.0f;
            textParams.wordWrap = false;

            return ComputeTextHeight(textParams, textScaling);
        }

        public void DrawText(UIRStylePainter painter, MeshGenerationContextUtils.TextParams parms, float pixelsPerPoint)
        {
            painter.DrawTextNative(parms, this, pixelsPerPoint);
        }

        public TextInfo Update(MeshGenerationContextUtils.TextParams parms, float pixelsPerPoint)
        {
            Debug.Log(("TextNative Update should not be called"));
            return null;
        }

        public int VerticesCount(MeshGenerationContextUtils.TextParams parms, float pixelPerPoint)
        {
            return GetVertices(parms, pixelPerPoint).Length;
        }

        public NativeArray<TextVertex> GetVertices(MeshGenerationContextUtils.TextParams parms, float scaling)
        {
            int paramsHash = parms.GetHashCode();
            if (m_PreviousTextParamsHash == paramsHash)
                return textVertices;

            m_PreviousTextParamsHash = paramsHash;
            TextNativeSettings textSettings = MeshGenerationContextUtils.TextParams.GetTextNativeSettings(parms, scaling);
            textVertices = TextNative.GetVertices(textSettings);
            return textVertices;
        }

        public Vector2 GetCursorPosition(CursorPositionStylePainterParameters parms, float scaling)
        {
            return TextNative.GetCursorPosition(parms.GetTextNativeSettings(scaling), parms.rect,
                parms.cursorIndex);
        }

        public float ComputeTextWidth(MeshGenerationContextUtils.TextParams parms, float scaling)
        {
            return TextNative.ComputeTextWidth(
                MeshGenerationContextUtils.TextParams.GetTextNativeSettings(parms, scaling));
        }

        public float ComputeTextHeight(MeshGenerationContextUtils.TextParams parms, float scaling)
        {
            return TextNative.ComputeTextHeight(
                MeshGenerationContextUtils.TextParams.GetTextNativeSettings(parms, scaling));
        }
    }

    internal static class TextUtilities
    {
        public static float ComputeTextScaling(Matrix4x4 worldMatrix, float pixelsPerPoint)
        {
            var axisX = new Vector3(worldMatrix.m00, worldMatrix.m10, worldMatrix.m20);
            var axisY = new Vector3(worldMatrix.m01, worldMatrix.m11, worldMatrix.m21);
            float worldScale = (axisX.magnitude + axisY.magnitude) / 2;
            return worldScale * pixelsPerPoint;
        }

        internal static Vector2 MeasureVisualElementTextSize(VisualElement ve, string textToMeasure, float width, VisualElement.MeasureMode widthMode, float height, VisualElement.MeasureMode heightMode, ITextHandle textHandle)
        {
            float measuredWidth = float.NaN;
            float measuredHeight = float.NaN;

            Font usedFont = ve.computedStyle.unityFont;
            if (textToMeasure == null || usedFont == null)
                return new Vector2(measuredWidth, measuredHeight);

            var elementScaling = ve.ComputeGlobalScale();
            if (elementScaling.x + elementScaling.y <= 0 || ve.scaledPixelsPerPoint <= 0)
                return Vector2.zero;

            float pixelsPerPoint = ve.scaledPixelsPerPoint;
            float pixelOffset = 0.02f;
            float pointOffset = pixelOffset / pixelsPerPoint;

            if (widthMode == VisualElement.MeasureMode.Exactly)
            {
                measuredWidth = width;
            }
            else
            {
                var textParams = MeshGenerationContextUtils.TextParams.MakeStyleBased(ve, textToMeasure);
                textParams.wordWrap = false;

                // Case 1215962: round up as yoga could decide to round down and text would start wrapping
                measuredWidth = textHandle.ComputeTextWidth(textParams, pixelsPerPoint);
                measuredWidth = measuredWidth < pointOffset ? 0 : AlignmentUtils.CeilToPixelGrid(measuredWidth, pixelsPerPoint, pixelOffset);

                if (widthMode == VisualElement.MeasureMode.AtMost)
                {
                    measuredWidth = Mathf.Min(measuredWidth, width);
                }
            }

            if (heightMode == VisualElement.MeasureMode.Exactly)
            {
                measuredHeight = height;
            }
            else
            {
                var textParams = MeshGenerationContextUtils.TextParams.MakeStyleBased(ve, textToMeasure);
                textParams.wordWrapWidth = measuredWidth;

                measuredHeight = textHandle.ComputeTextHeight(textParams, pixelsPerPoint);
                measuredHeight = measuredHeight < pointOffset ? 0 : AlignmentUtils.CeilToPixelGrid(measuredHeight, pixelsPerPoint, pixelOffset);

                if (heightMode == VisualElement.MeasureMode.AtMost)
                {
                    measuredHeight = Mathf.Min(measuredHeight, height);
                }
            }

            return new Vector2(measuredWidth, measuredHeight);
        }
    }
}
