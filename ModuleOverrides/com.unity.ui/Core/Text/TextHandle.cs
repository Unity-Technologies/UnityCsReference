// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.UIElements.UIR.Implementation;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    internal interface ITextHandle
    {
        Vector2 GetCursorPosition(CursorPositionStylePainterParameters parms, float scaling);
        float ComputeTextWidth(MeshGenerationContextUtils.TextParams parms, float scaling);
        float ComputeTextHeight(MeshGenerationContextUtils.TextParams parms, float scaling);
        float GetLineHeight(int characterIndex, MeshGenerationContextUtils.TextParams textParams, float textScaling,
            float pixelPerPoint);

        TextInfo Update(MeshGenerationContextUtils.TextParams parms, float pixelsPerPoint);
        int VerticesCount(MeshGenerationContextUtils.TextParams parms, float pixelPerPoint);
        ITextHandle New();
        bool IsLegacy();
        public void SetDirty();
        bool IsElided();

        Vector2 MeasuredSizes { get; set; }
        Vector2 RoundedSizes { get; set; }
    }

    /// <summary>
    /// DO NOT USE TextHandle. This struct is only there for backward compatibility reason and will soon be stripped.
    /// </summary>
    internal struct TextHandle : ITextHandle
    {
        internal ITextHandle textHandle;

        public Vector2 MeasuredSizes { get => textHandle.MeasuredSizes; set => textHandle.MeasuredSizes = value; }

        public Vector2 RoundedSizes { get => textHandle.RoundedSizes; set => textHandle.RoundedSizes = value; }

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

        public bool IsLegacy()
        {
            return textHandle.IsLegacy();
        }

        public void SetDirty()
        {
            textHandle.SetDirty();
        }

        public bool IsElided()
        {
            return textHandle.IsElided();
        }
    }

    internal struct TextCoreHandle : ITextHandle
    {
        public Vector2 MeasuredSizes { get; set; }
        public Vector2 RoundedSizes { get; set; }
        public static ITextHandle New()
        {
            TextCoreHandle h = new TextCoreHandle();
            h.m_CurrentGenerationSettings = new UnityEngine.TextCore.Text.TextGenerationSettings();
            return h;
        }

        Vector2 m_PreferredSize;
        int m_PreviousGenerationSettingsHash;
        UnityEngine.TextCore.Text.TextGenerationSettings m_CurrentGenerationSettings;

        //static instance cached to minimize allocation
        static TextCore.Text.TextGenerationSettings s_LayoutSettings = new TextCore.Text.TextGenerationSettings();

        /// <summary>
        /// DO NOT USE m_TextInfo directly, use textInfo to guarantee lazy allocation.
        /// </summary>
        private TextInfo m_TextInfoMesh;

        /// <summary>
        /// The TextInfo instance, use from this instead of the m_TextInfo member to guarantee lazy allocation.
        /// </summary>
        internal TextInfo textInfoMesh
        {
            get
            {
                if (m_TextInfoMesh == null)
                {
                    m_TextInfoMesh = new TextInfo();
                }

                return m_TextInfoMesh;
            }
        }

        /// <summary>
        /// DO NOT USE m_TextInfo_Layout directly, use textInfo to guarantee lazy allocation.
        /// </summary>
        private static TextInfo s_TextInfoLayout;

        /// <summary>
        /// The TextInfo instance to be used for layout, use from this instead of the m_TextInfo_Layout member to guarantee lazy allocation.
        /// </summary>
        internal static TextInfo textInfoLayout
        {
            get
            {
                if (s_TextInfoLayout == null)
                {
                    s_TextInfoLayout = new TextInfo();
                }

                return s_TextInfoLayout;
            }
        }

        // For testing purposes
        internal bool IsTextInfoAllocated()
        {
            return m_TextInfoMesh != null;
        }

        public bool IsLegacy()
        {
            return false;
        }

        private bool isDirty;

        public void SetDirty()
        {
            isDirty = true;
        }

        public bool IsDirty(MeshGenerationContextUtils.TextParams parms)
        {
            int paramsHash = parms.GetHashCode();
            if (m_PreviousGenerationSettingsHash == paramsHash && !isDirty)
                return false;

            m_PreviousGenerationSettingsHash = paramsHash;
            isDirty = false;
            return true;
        }

        public Vector2 GetCursorPosition(CursorPositionStylePainterParameters parms, float scaling)
        {
            return UnityEngine.TextCore.Text.TextGenerator.GetCursorPosition(textInfoMesh, parms.rect, parms.cursorIndex);
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

        public float GetLineHeight(int characterIndex, MeshGenerationContextUtils.TextParams textParams, float textScaling, float pixelPerPoint)
        {
            var character = m_TextInfoMesh.textElementInfo[m_TextInfoMesh.characterCount - 1];
            var line = m_TextInfoMesh.lineInfo[character.lineNumber];
            return line.lineHeight;
        }

        public int VerticesCount(MeshGenerationContextUtils.TextParams parms, float pixelPerPoint)
        {
            Update(parms, pixelPerPoint);
            var verticesCount = 0;
            foreach (var meshInfo in textInfoMesh.meshInfo)
                verticesCount += meshInfo.vertexCount;
            return verticesCount;
        }

        ITextHandle ITextHandle.New()
        {
            return New();
        }

        public TextInfo Update(MeshGenerationContextUtils.TextParams parms, float pixelsPerPoint)
        {
            // The screenRect in TextCore is not properly implemented with regards to the offset part, so we just keep the size
            var size = parms.rect.size;

            // If the size is the last rounded size, we use the cached size before the rounding that was calculated
            if (Mathf.Abs(parms.rect.size.x - RoundedSizes.x) < 0.01f && Mathf.Abs(parms.rect.size.y - RoundedSizes.y) < 0.01f)
            {
                size = MeasuredSizes;
                parms.wordWrapWidth = size.x;
            }
            else
            {
                //the size has change, we need to save that information
                RoundedSizes = size;
                MeasuredSizes = size;
            }

            parms.rect = new Rect(Vector2.zero, size);
            if (!IsDirty(parms))
            {
                return textInfoMesh;
            }

            UpdateGenerationSettingsCommon(parms, m_CurrentGenerationSettings);

            m_CurrentGenerationSettings.color = parms.fontColor;
            m_CurrentGenerationSettings.inverseYAxis = true;

            textInfoMesh.isDirty = true;
            UnityEngine.TextCore.Text.TextGenerator.GenerateText(m_CurrentGenerationSettings, textInfoMesh);
            return textInfoMesh;
        }

        void UpdatePreferredValues(MeshGenerationContextUtils.TextParams parms)
        {
            // The screenRect in TextCore is not properly implemented with regards to the offset part, so zero it out for now and we will add it ourselves later
            var size = parms.rect.size;
            parms.rect = new Rect(Vector2.zero, size);

            UpdateGenerationSettingsCommon(parms, s_LayoutSettings);
            m_PreferredSize = UnityEngine.TextCore.Text.TextGenerator.GetPreferredValues(s_LayoutSettings, textInfoLayout);
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
            UnityEngine.TextCore.Text.TextGenerationSettings settings)
        {
            if (settings.textSettings == null)
            {
                settings.textSettings = TextUtilities.GetTextSettingsFrom(painterParams);
                if (settings.textSettings == null)
                    return;
            }

            settings.fontAsset = TextUtilities.GetFontAsset(painterParams);
            if (settings.fontAsset == null)
                return;

            settings.material = settings.fontAsset.material;
            settings.screenRect = painterParams.rect;
            //The NoWidthSpace unicode is added at the end of the string to make sure LineFeeds update the layout of the text.
            settings.text = string.IsNullOrEmpty(painterParams.text) ? "\u200B" : painterParams.text + "\u200B";
            settings.fontSize = painterParams.fontSize > 0
                ? painterParams.fontSize
                : settings.fontAsset.faceInfo.pointSize;
            settings.fontStyle = TextGeneratorUtilities.LegacyStyleToNewStyle(painterParams.fontStyle);
            settings.textAlignment = TextGeneratorUtilities.LegacyAlignmentToNewAlignment(painterParams.anchor);
            settings.wordWrap = painterParams.wordWrap;
            settings.wordWrappingRatio = 0.4f;
            settings.richText = painterParams.richText;
            settings.overflowMode = GetTextOverflowMode(painterParams);
            settings.characterSpacing = painterParams.letterSpacing.value;
            settings.wordSpacing = painterParams.wordSpacing.value;
            settings.paragraphSpacing = painterParams.paragraphSpacing.value;
        }

        public bool IsElided()
        {
            if (m_TextInfoMesh == null)
                return false;

            if (m_TextInfoMesh.characterCount == 0) // impossible to differentiate between an empty string and a fully truncated string.
                return true;

            return m_TextInfoMesh.textElementInfo[m_TextInfoMesh.characterCount - 1].character == 0x2026; //the character is hardcoded in textcore
        }
    }

    internal struct TextNativeHandle : ITextHandle
    {
        public Vector2 MeasuredSizes { get; set; }
        public Vector2 RoundedSizes { get; set; }

        // For automated testing purposes
        internal NativeArray<TextVertex> textVertices;
        private int m_PreviousTextParamsHash;

        public static ITextHandle New()
        {
            TextNativeHandle h = new TextNativeHandle();
            h.textVertices = new NativeArray<TextVertex>();
            return h;
        }

        public bool IsLegacy()
        {
            return true;
        }

        public void SetDirty()
        {
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
            var size = parms.rect.size;

            // If the size is the last rounded size, we use the cached size before the rounding that was calculated
            if (Mathf.Abs(parms.rect.size.x - RoundedSizes.x) < 0.01f && Mathf.Abs(parms.rect.size.y - RoundedSizes.y) < 0.01f)
            {
                size = MeasuredSizes;
                parms.wordWrapWidth = size.x;
            }
            else
            {
                //the size has change, we need to save that information
                RoundedSizes = size;
                MeasuredSizes = size;
            }

            parms.rect = new Rect(Vector2.zero, size);


            int paramsHash = parms.GetHashCode();
            if (m_PreviousTextParamsHash == paramsHash)
                return textVertices;

            m_PreviousTextParamsHash = paramsHash;
            TextNativeSettings textSettings = MeshGenerationContextUtils.TextParams.GetTextNativeSettings(parms, scaling);


            Assertions.Assert.IsNotNull(textSettings.font);

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
            float value = TextNative.ComputeTextWidth(
                MeshGenerationContextUtils.TextParams.GetTextNativeSettings(parms, scaling));

            // in NativeTextGenerator::InsertCharacter: line 554 where the wordWrap is a limitation because of floating points values
            // this offset has been introduced as workaround.
            // This is needed only when the scaling factor is different than 1
            // when the width is 0, no character are present and the width is exact=> no need to adjust.
            if (scaling != 1f && value != 0)
                return value + 0.0001f;
            return value;
        }

        public float ComputeTextHeight(MeshGenerationContextUtils.TextParams parms, float scaling)
        {
            return TextNative.ComputeTextHeight(
                MeshGenerationContextUtils.TextParams.GetTextNativeSettings(parms, scaling));
        }

        public bool IsElided()
        {
            return false;
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

            if (textToMeasure == null || !IsFontAssigned(ve))
                return new Vector2(measuredWidth, measuredHeight);

            var elementScaling = ve.ComputeGlobalScale();
            if (elementScaling.x + elementScaling.y <= 0 || ve.scaledPixelsPerPoint <= 0)
                return Vector2.zero;

            float pixelsPerPoint = ve.scaledPixelsPerPoint;
            if (widthMode == VisualElement.MeasureMode.Exactly)
            {
                measuredWidth = width;
            }
            else
            {
                var textParams = MeshGenerationContextUtils.TextParams.MakeStyleBased(ve, textToMeasure);
                textParams.wordWrap = false;
                textParams.rect = new Rect(textParams.rect.x, textParams.rect.y, width, height);


                measuredWidth = textHandle.ComputeTextWidth(textParams, pixelsPerPoint);


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
                textParams.rect = new Rect(textParams.rect.x, textParams.rect.y, width, height);

                measuredHeight = textHandle.ComputeTextHeight(textParams, pixelsPerPoint);


                if (heightMode == VisualElement.MeasureMode.AtMost)
                {
                    measuredHeight = Mathf.Min(measuredHeight, height);
                }
            }


            // Case 1215962: round up as yoga could decide to round down and text would start wrapping
            float roundedWidth = AlignmentUtils.CeilToPixelGrid(measuredWidth, pixelsPerPoint);
            float roundedHeight = AlignmentUtils.CeilToPixelGrid(measuredHeight, pixelsPerPoint);
            var roundedValues = new Vector2(roundedWidth, roundedHeight);

            textHandle.MeasuredSizes = new Vector2(measuredWidth, measuredHeight);
            textHandle.RoundedSizes = roundedValues;


            return roundedValues;
        }

        internal static FontAsset GetFontAsset(MeshGenerationContextUtils.TextParams textParam)
        {
            var textSettings = GetTextSettingsFrom(textParam);
            if (textParam.fontDefinition.fontAsset != null)
                return textParam.fontDefinition.fontAsset;
            if (textParam.fontDefinition.font != null)
                return textSettings.GetCachedFontAsset(textParam.fontDefinition.font);
            return textSettings.GetCachedFontAsset(textParam.font);
        }

        internal static FontAsset GetFontAsset(VisualElement ve)
        {
            if (ve.computedStyle.unityFontDefinition.fontAsset != null)
                return ve.computedStyle.unityFontDefinition.fontAsset as FontAsset;

            var textSettings = GetTextSettingsFrom(ve);
            if (ve.computedStyle.unityFontDefinition.font != null)
                return textSettings.GetCachedFontAsset(ve.computedStyle.unityFontDefinition.font);
            return textSettings.GetCachedFontAsset(ve.computedStyle.unityFont);
        }

        internal static Font GetFont(MeshGenerationContextUtils.TextParams textParam)
        {
            if (textParam.fontDefinition.font != null)
                return textParam.fontDefinition.font;

            if (textParam.font != null)
                return textParam.font;

            return textParam.fontDefinition.fontAsset?.sourceFontFile;
        }

        internal static Font GetFont(VisualElement ve)
        {
            var style = ve.computedStyle;
            if (style.unityFontDefinition.font != null)
                return style.unityFontDefinition.font;
            if (style.unityFont != null)
                return style.unityFont;

            return style.unityFontDefinition.fontAsset?.sourceFontFile;
        }

        internal static bool IsFontAssigned(VisualElement ve)
        {
            return ve.computedStyle.unityFont != null || !ve.computedStyle.unityFontDefinition.IsEmpty();
        }

        internal static bool IsFontAssigned(MeshGenerationContextUtils.TextParams textParams)
        {
            return textParams.font != null || !textParams.fontDefinition.IsEmpty();
        }

        internal static PanelTextSettings GetTextSettingsFrom(VisualElement ve)
        {
            if (ve.panel is RuntimePanel runtimePanel)
                return runtimePanel.panelSettings.textSettings ?? PanelTextSettings.defaultPanelTextSettings;
            return PanelTextSettings.defaultPanelTextSettings;
        }

        internal static PanelTextSettings GetTextSettingsFrom(MeshGenerationContextUtils.TextParams textParam)
        {
            if (textParam.panel is RuntimePanel runtimePanel)
                return runtimePanel.panelSettings.textSettings ?? PanelTextSettings.defaultPanelTextSettings;
            return PanelTextSettings.defaultPanelTextSettings;
        }

        internal static TextCoreSettings GetTextCoreSettingsForElement(VisualElement ve)
        {
            var fontAsset = GetFontAsset(ve);
            if (fontAsset == null)
                return new TextCoreSettings();

            var resolvedStyle = ve.resolvedStyle;
            var computedStyle = ve.computedStyle;

            // Convert the text settings pixel units to TextCore relative units
            float paddingPercent = 1.0f / fontAsset.atlasPadding;
            float pointSizeRatio = ((float)fontAsset.faceInfo.pointSize) / ve.computedStyle.fontSize.value;
            float factor = paddingPercent * pointSizeRatio;

            float outlineWidth = Mathf.Max(0.0f, resolvedStyle.unityTextOutlineWidth * factor);
            float underlaySoftness = Mathf.Max(0.0f, computedStyle.textShadow.blurRadius * factor);
            Vector2 underlayOffset = computedStyle.textShadow.offset * factor;

            var faceColor = resolvedStyle.color;
            var outlineColor = resolvedStyle.unityTextOutlineColor;
            if (outlineWidth < UIRUtility.k_Epsilon)
                outlineColor.a = 0.0f;

            return new TextCoreSettings() {
                faceColor = faceColor,
                outlineColor = outlineColor,
                outlineWidth = outlineWidth,
                underlayColor = computedStyle.textShadow.color,
                underlayOffset = underlayOffset,
                underlaySoftness = underlaySoftness
            };
        }
    }
}
