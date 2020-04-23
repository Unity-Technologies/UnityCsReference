using System.Collections.Generic;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using static UnityEngine.TextCore.FontAsset;

namespace UnityEngine.UIElements
{
    internal struct TextHandle
    {
        public static TextHandle New()
        {
            TextHandle h = new TextHandle();
            h.m_TextInfo = new TextInfo();
            h.useLegacy = false;
            h.m_CurrentGenerationSettings = new TextCore.TextGenerationSettings();
            h.m_CurrentLayoutSettings = new TextCore.TextGenerationSettings();
            return h;
        }

        // whether we use TextCore (New) or TextNative (Legacy)
        public bool useLegacy;

        // At the moment, FontAssets are not part of Styles. They're created on runtime and cached
        static Dictionary<Font, FontAsset> fontAssetCache = new Dictionary<Font, FontAsset>();

        static FontAsset GetFontAsset(Font font)
        {
            FontAsset fontAsset = null;
            if (fontAssetCache.TryGetValue(font, out fontAsset) && fontAsset != null)
                return fontAsset;
            fontAsset = FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic);
            return fontAssetCache[font] = fontAsset;
        }

        Vector2 m_PreferredSize;
        TextInfo m_TextInfo;
        int m_PreviousGenerationSettingsHash;
        TextCore.TextGenerationSettings m_CurrentGenerationSettings;
        int m_PreviousLayoutSettingsHash;
        TextCore.TextGenerationSettings m_CurrentLayoutSettings;

        public Vector2 GetCursorPosition(CursorPositionStylePainterParameters parms, float scaling)
        {
            if (useLegacy)
                return TextNative.GetCursorPosition(parms.GetTextNativeSettings(scaling), parms.rect,
                    parms.cursorIndex);
            return TextCore.TextGenerator.GetCursorPosition(m_TextInfo, parms.rect, parms.cursorIndex);
        }

        public float ComputeTextWidth(MeshGenerationContextUtils.TextParams parms, float scaling)
        {
            if (useLegacy)
                return TextNative.ComputeTextWidth(
                    MeshGenerationContextUtils.TextParams.GetTextNativeSettings(parms, scaling));
            UpdatePreferredValues(parms);
            return m_PreferredSize.x;
        }

        public float ComputeTextHeight(MeshGenerationContextUtils.TextParams parms, float scaling)
        {
            if (useLegacy)
                return TextNative.ComputeTextHeight(
                    MeshGenerationContextUtils.TextParams.GetTextNativeSettings(parms, scaling));
            UpdatePreferredValues(parms);
            return m_PreferredSize.y;
        }

        internal TextInfo Update(MeshGenerationContextUtils.TextParams parms, float pixelsPerPoint)
        {
            // The screenRect in TextCore is not properly implemented with regards to the offset part, so zero it out for now and we will add it ourselves later
            parms.rect = new Rect(Vector2.zero, parms.rect.size);
            int paramsHash = parms.GetHashCode();
            if (m_PreviousGenerationSettingsHash == paramsHash)
                return m_TextInfo;

            UpdateGenerationSettingsCommon(parms, m_CurrentGenerationSettings);

            m_CurrentGenerationSettings.color = parms.fontColor;
            m_CurrentGenerationSettings.inverseYAxis = true;
            m_CurrentGenerationSettings.scale = pixelsPerPoint;
            m_CurrentGenerationSettings.overflowMode = parms.textOverflowMode;

            m_TextInfo.isDirty = true;
            TextCore.TextGenerator.GenerateText(m_CurrentGenerationSettings, m_TextInfo);
            m_PreviousGenerationSettingsHash = paramsHash;
            return m_TextInfo;
        }

        void UpdatePreferredValues(MeshGenerationContextUtils.TextParams parms)
        {
            // The screenRect in TextCore is not properly implemented with regards to the offset part, so zero it out for now and we will add it ourselves later
            parms.rect = new Rect(Vector2.zero, parms.rect.size);
            int paramsHash = parms.GetHashCode();
            if (m_PreviousLayoutSettingsHash == paramsHash)
                return;

            UpdateGenerationSettingsCommon(parms, m_CurrentLayoutSettings);
            m_PreferredSize = TextCore.TextGenerator.GetPreferredValues(m_CurrentLayoutSettings, m_TextInfo);
            m_PreviousLayoutSettingsHash = paramsHash;
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

        public static float ComputeTextScaling(Matrix4x4 worldMatrix, float pixelsPerPoint)
        {
            var axisX = new Vector3(worldMatrix.m00, worldMatrix.m10, worldMatrix.m20);
            var axisY = new Vector3(worldMatrix.m01, worldMatrix.m11, worldMatrix.m21);
            float worldScale = (axisX.magnitude + axisY.magnitude) / 2;
            return worldScale * pixelsPerPoint;
        }
    }
}
