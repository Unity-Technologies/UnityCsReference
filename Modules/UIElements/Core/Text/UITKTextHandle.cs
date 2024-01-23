// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.TextCore;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    internal class UITKTextHandle : TextHandle
    {
        private readonly EventCallback<PointerDownEvent> m_LinkTagOnPointerDown;
        private readonly EventCallback<PointerUpEvent> m_LinkTagOnPointerUp;
        private readonly EventCallback<PointerMoveEvent> m_LinkTagOnPointerMove;
        private readonly EventCallback<PointerOutEvent> m_LinkTagOnPointerOut;
        private readonly EventCallback<PointerUpEvent> m_ATagOnPointerUp;
        private readonly EventCallback<PointerMoveEvent> m_ATagOnPointerMove;
        private readonly EventCallback<PointerOverEvent> m_ATagOnPointerOver;
        private readonly EventCallback<PointerOutEvent> m_ATagOnPointerOut;

        public UITKTextHandle(TextElement te)
        {
            m_TextElement = te;

            // allocate and cache callback delegates
            m_LinkTagOnPointerDown = LinkTagOnPointerDown;
            m_LinkTagOnPointerUp = LinkTagOnPointerUp;
            m_LinkTagOnPointerMove = LinkTagOnPointerMove;
            m_LinkTagOnPointerOut = LinkTagOnPointerOut;
            m_ATagOnPointerUp = ATagOnPointerUp;
            m_ATagOnPointerMove = ATagOnPointerMove;
            m_ATagOnPointerOver = ATagOnPointerOver;
            m_ATagOnPointerOut = ATagOnPointerOut;
        }

        public Vector2 MeasuredSizes { get; set; }
        public Vector2 RoundedSizes { get; set; }

        TextElement m_TextElement;
        static TextLib s_TextLib;

        static TextLib TextLib
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (s_TextLib == null)
                {
                    s_TextLib = new TextLib();
                }
                return s_TextLib;
            }
        }

        public Vector2 ComputeTextSize(in RenderedText textToMeasure, float width, float height)
        {
            ConvertUssToTextGenerationSettings();
            settings.renderedText = textToMeasure;
            settings.screenRect = new Rect(0, 0, width, height);
            UpdatePreferredValues(settings);
            return preferredSize;
        }

        public MeshInfo[] Update()
        {
            ConvertUssToTextGenerationSettings();

            // this should be a dynamic asset
            if (m_PreviousGenerationSettingsHash == settings.GetHashCode())
                AddTextInfoToCache();
            else
                Update(settings);

            HandleATag();
            HandleLinkTag();
            HandleLinkAndATagCallbacks();

            return textInfo.meshInfo;
        }

        public NativeTextInfo UpdateNative(ref bool success)
        {
            if (!ConvertUssToNativeTextGenerationSettings())
            {
                success = false;
                return new NativeTextInfo();
            }

            if (m_PreviousNativeGenerationSettingsHash != nativeSettings.GetHashCode())
                nativeTextInfo = TextLib.GenerateText(nativeSettings);

            success = true;
            return nativeTextInfo;
        }

        public override void AddTextInfoToCache()
        {
            ConvertUssToTextGenerationSettings();
            base.AddTextInfoToCache();
        }

        void ATagOnPointerUp(PointerUpEvent pue)
        {
            var pos = pue.localPosition - new Vector3(m_TextElement.contentRect.min.x, m_TextElement.contentRect.min.y);
            var intersectingLink = FindIntersectingLink(pos);
            if (intersectingLink < 0)
                return;

            var link = textInfo.linkInfo[intersectingLink];
            if (link.hashCode != (int)MarkupTag.HREF)
                return;
            if (link.linkId == null || link.linkIdLength <= 0)
                return;

            var href = link.GetLinkId();
            if (Uri.IsWellFormedUriString(href, UriKind.Absolute))
                Application.OpenURL(href);
        }

        internal bool isOverridingCursor;

        void ATagOnPointerOver(PointerOverEvent _)
        {
            isOverridingCursor = false;
        }

        void ATagOnPointerMove(PointerMoveEvent pme)
        {
            var pos = pme.localPosition - new Vector3(m_TextElement.contentRect.min.x, m_TextElement.contentRect.min.y);
            var intersectingLink = FindIntersectingLink(pos);
            var cursorManager = (m_TextElement.panel as BaseVisualElementPanel)?.cursorManager;
            if (intersectingLink >= 0)
            {
                var link = textInfo.linkInfo[intersectingLink];
                if (link.hashCode == (int)MarkupTag.HREF)
                {
                    if (!isOverridingCursor)
                    {
                        isOverridingCursor = true;
                        // defaultCursorId maps to the UnityEditor.MouseCursor enum where 4 is the link cursor.
                        cursorManager?.SetCursor(new Cursor { defaultCursorId = 4 });
                    }

                    return;
                }
            }
            if (isOverridingCursor)
            {
                cursorManager?.SetCursor(m_TextElement.computedStyle.cursor);
                isOverridingCursor = false;
            }
        }

        void ATagOnPointerOut(PointerOutEvent evt)
        {
            isOverridingCursor = false;
        }

        void LinkTagOnPointerDown(PointerDownEvent pde)
        {
            var pos = pde.localPosition - new Vector3(m_TextElement.contentRect.min.x, m_TextElement.contentRect.min.y);
            var intersectingLink = FindIntersectingLink(pos);
            if (intersectingLink < 0)
                return;

            var link = textInfo.linkInfo[intersectingLink];

            if (link.hashCode == (int)MarkupTag.HREF)
                return;
            if (link.linkId == null || link.linkIdLength <= 0)
                return;

            using (var e = Experimental.PointerDownLinkTagEvent.GetPooled(pde, link.GetLinkId(), link.GetLinkText(textInfo)))
            {
                e.elementTarget = m_TextElement;
                m_TextElement.SendEvent(e);
            }
        }

        void LinkTagOnPointerUp(PointerUpEvent pue)
        {
            var pos = pue.localPosition - new Vector3(m_TextElement.contentRect.min.x, m_TextElement.contentRect.min.y);
            var intersectingLink = FindIntersectingLink(pos);
            if (intersectingLink < 0)
                return;

            var link = textInfo.linkInfo[intersectingLink];

            if (link.hashCode == (int)MarkupTag.HREF)
                return;
            if (link.linkId == null || link.linkIdLength <= 0)
                return;

            using (var e = Experimental.PointerUpLinkTagEvent.GetPooled(pue, link.GetLinkId(), link.GetLinkText(textInfo)))
            {
                e.elementTarget = m_TextElement;
                m_TextElement.SendEvent(e);
            }
        }

        // Used in automated test
        internal int currentLinkIDHash = -1;

        void LinkTagOnPointerMove(PointerMoveEvent pme)
        {
            var pos = pme.localPosition - new Vector3(m_TextElement.contentRect.min.x, m_TextElement.contentRect.min.y);
            var intersectingLink = FindIntersectingLink(pos);
            if (intersectingLink >= 0)
            {
                var link = textInfo.linkInfo[intersectingLink];
                if (link.hashCode != (int)MarkupTag.HREF)
                {
                    // PointerOver
                    if (currentLinkIDHash == -1)
                    {
                        currentLinkIDHash = link.hashCode;
                        using (var e = Experimental.PointerOverLinkTagEvent.GetPooled(pme, link.GetLinkId(), link.GetLinkText(textInfo)))
                        {
                            e.elementTarget = m_TextElement;
                            m_TextElement.SendEvent(e);
                        }

                        return;
                    }
                    // PointerMove
                    if (currentLinkIDHash == link.hashCode)
                    {
                        using (var e = Experimental.PointerMoveLinkTagEvent.GetPooled(pme, link.GetLinkId(), link.GetLinkText(textInfo)))
                        {
                            e.elementTarget = m_TextElement;
                            m_TextElement.SendEvent(e);
                        }

                        return;
                    }
                }
            }

            // PointerOut
            if (currentLinkIDHash != -1)
            {
                currentLinkIDHash = -1;
                using (var e = Experimental.PointerOutLinkTagEvent.GetPooled(pme, string.Empty))
                {
                    e.elementTarget = m_TextElement;
                    m_TextElement.SendEvent(e);
                }
            }
        }

        void LinkTagOnPointerOut(PointerOutEvent poe)
        {
            if (currentLinkIDHash != -1)
            {
                using (var e = Experimental.PointerOutLinkTagEvent.GetPooled(poe, string.Empty))
                {
                    e.elementTarget = m_TextElement;
                    m_TextElement.SendEvent(e);
                }

                currentLinkIDHash = -1;
            }
        }

        internal void HandleLinkAndATagCallbacks()
        {
            if (m_TextElement?.panel == null)
                return;

            if (hasLinkTag)
            {
                m_TextElement.RegisterCallback(m_LinkTagOnPointerDown, TrickleDown.TrickleDown);
                m_TextElement.RegisterCallback(m_LinkTagOnPointerUp, TrickleDown.TrickleDown);
                m_TextElement.RegisterCallback(m_LinkTagOnPointerMove, TrickleDown.TrickleDown);
                m_TextElement.RegisterCallback(m_LinkTagOnPointerOut, TrickleDown.TrickleDown);
            }
            else
            {
                m_TextElement.UnregisterCallback(m_LinkTagOnPointerDown, TrickleDown.TrickleDown);
                m_TextElement.UnregisterCallback(m_LinkTagOnPointerUp, TrickleDown.TrickleDown);
                m_TextElement.UnregisterCallback(m_LinkTagOnPointerMove, TrickleDown.TrickleDown);
                m_TextElement.UnregisterCallback(m_LinkTagOnPointerOut, TrickleDown.TrickleDown);
            }
            if (hasATag)
            {
                m_TextElement.RegisterCallback(m_ATagOnPointerUp, TrickleDown.TrickleDown);
                // Switching the cursor to the Link cursor has been disable at runtime until OS cursor support is available at runtime.
                if (m_TextElement.panel.contextType == ContextType.Editor)
                {
                    m_TextElement.RegisterCallback(m_ATagOnPointerMove, TrickleDown.TrickleDown);
                    m_TextElement.RegisterCallback(m_ATagOnPointerOver, TrickleDown.TrickleDown);
                    m_TextElement.RegisterCallback(m_ATagOnPointerOut, TrickleDown.TrickleDown);
                }
            }
            else
            {
                m_TextElement.UnregisterCallback(m_ATagOnPointerUp, TrickleDown.TrickleDown);
                if (m_TextElement.panel.contextType == ContextType.Editor)
                {
                    m_TextElement.UnregisterCallback(m_ATagOnPointerMove, TrickleDown.TrickleDown);
                    m_TextElement.UnregisterCallback(m_ATagOnPointerOver, TrickleDown.TrickleDown);
                    m_TextElement.UnregisterCallback(m_ATagOnPointerOut, TrickleDown.TrickleDown);
                }
            }
        }

        // Used by our automated tests.
        internal bool hasLinkTag;
        internal void HandleLinkTag()
        {
            for (int i = 0; i < textInfo.linkCount; i++)
            {
                var linkInfo = textInfo.linkInfo[i];
                if (linkInfo.hashCode != (int)MarkupTag.HREF)
                {
                    hasLinkTag = true;
                    AddTextInfoToCache();
                    return;
                }
            }

            if (hasLinkTag)
            {
                hasLinkTag = false;
                RemoveTextInfoFromCache();
            }
        }

        // Used by our automated tests.
        internal bool hasATag;
        internal void HandleATag()
        {
            for (int i = 0; i < textInfo.linkCount; i++)
            {
                var linkInfo = textInfo.linkInfo[i];
                if (linkInfo.hashCode == (int)MarkupTag.HREF)
                {
                    hasATag = true;
                    AddTextInfoToCache();
                    return;
                }
            }

            if (hasATag)
            {
                hasATag = false;
                RemoveTextInfoFromCache();
            }
        }

        TextOverflowMode GetTextOverflowMode()
        {
            var style = m_TextElement.computedStyle;
            if (style.textOverflow == TextOverflow.Clip)
                return TextOverflowMode.Masking;

            if (style.textOverflow != TextOverflow.Ellipsis)
                return TextOverflowMode.Overflow;

            //Disable the ellipsis in text core so that it does not affect the render later.
            if (!TextLibraryCanElide())
                return TextOverflowMode.Masking;

            if (style.overflow == OverflowInternal.Hidden)
                return TextOverflowMode.Ellipsis;

            return TextOverflowMode.Overflow;
        }

        internal bool ConvertUssToNativeTextGenerationSettings()
        {
            var fa = TextUtilities.GetFontAsset(m_TextElement);
            if (fa.atlasPopulationMode == AtlasPopulationMode.Static)
            {
                Debug.LogError($"Advanced text system cannot render using static font asset {fa.name}");
                return false;
            }
            var style = m_TextElement.computedStyle;
            var renderedText = m_TextElement.isElided && !TextLibraryCanElide() ? m_TextElement.elidedText : m_TextElement.renderedText;
            nativeSettings.text = renderedText.CreateString();
            nativeSettings.screenWidth = (int)(m_TextElement.contentRect.width * 64);
            nativeSettings.screenHeight = (int)(m_TextElement.contentRect.height * 64);
            nativeSettings.fontSize = style.fontSize.value > 0
                ? style.fontSize.value
                : fa.faceInfo.pointSize;
            nativeSettings.wrapText = m_TextElement.computedStyle.whiteSpace == WhiteSpace.Normal;
            nativeSettings.horizontalAlignment = TextGeneratorUtilities.GetHorizontalAlignment(style.unityTextAlign);
            nativeSettings.verticalAlignment = TextGeneratorUtilities.GetVerticalAlignment(style.unityTextAlign);

            nativeSettings.color = m_TextElement.computedStyle.color;
            nativeSettings.fontAsset = fa.nativeFontAsset;
            nativeSettings.languageDirection = m_TextElement.localLanguageDirection.toTextCore();

            var textSettings = TextUtilities.GetTextSettingsFrom(m_TextElement);
            List<IntPtr> globalFontAssetFallbacks = new List<IntPtr>();
            if (textSettings != null && textSettings.fallbackFontAssets != null)
            {
                foreach (var fallback in textSettings.fallbackFontAssets)
                {
                    if (fallback == null)
                        continue;
                    if (fallback.atlasPopulationMode == AtlasPopulationMode.Static && fallback.characterTable.Count > 0)
                    {
                        Debug.LogWarning($"Advanced text system cannot use static font asset {fallback.name} as fallback.");
                        continue;
                    }
                    globalFontAssetFallbacks.Add(fallback.nativeFontAsset);
                }
            }

            if (textSettings != null && textSettings.emojiFallbackTextAssets != null)
            {
                foreach (FontAsset fallback in textSettings.emojiFallbackTextAssets)
                {
                    if (fallback == null)
                        continue;
                    if (fallback.atlasPopulationMode == AtlasPopulationMode.Static && fallback.characterTable.Count > 0)
                    {
                        Debug.LogWarning($"Advanced text system cannot use static font asset {fallback.name} as fallback.");
                        continue;
                    }
                    globalFontAssetFallbacks.Add(fallback.nativeFontAsset);
                }
            }
            nativeSettings.globalFontAssetFallbacks = globalFontAssetFallbacks.ToArray();
            nativeSettings.fontStyle = TextGeneratorUtilities.LegacyStyleToNewStyle(style.unityFontStyleAndWeight);
            return true;
        }

        internal virtual void ConvertUssToTextGenerationSettings()
        {
            var style = m_TextElement.computedStyle;
            var tgs = settings;
            tgs.textSettings = TextUtilities.GetTextSettingsFrom(m_TextElement);
            if (tgs.textSettings == null)
                return;

            tgs.fontAsset = TextUtilities.GetFontAsset(m_TextElement);
            if (tgs.fontAsset == null)
                return;

            tgs.material = tgs.fontAsset.material;
            // The screenRect in TextCore is not properly implemented with regards to the offset part, so zero it out for now and we will add it ourselves later
            tgs.screenRect = new Rect(0, 0, m_TextElement.contentRect.width, m_TextElement.contentRect.height);
            tgs.extraPadding = GetTextEffectPadding(tgs.fontAsset);
            tgs.renderedText = m_TextElement.isElided && !TextLibraryCanElide() ? m_TextElement.elidedText : m_TextElement.renderedText;
            tgs.isPlaceholder = m_TextElement.showPlaceholderText;

            tgs.fontSize = style.fontSize.value > 0
                ? style.fontSize.value
                : tgs.fontAsset.faceInfo.pointSize;

            tgs.fontStyle = TextGeneratorUtilities.LegacyStyleToNewStyle(style.unityFontStyleAndWeight);
            tgs.textAlignment = TextGeneratorUtilities.LegacyAlignmentToNewAlignment(style.unityTextAlign);

            tgs.wordWrap = style.whiteSpace == WhiteSpace.Normal;
            tgs.textWrappingMode = TextWrappingMode.Normal;

            tgs.wordWrappingRatio = 0.4f;
            tgs.richText = m_TextElement.enableRichText;
            tgs.overflowMode = GetTextOverflowMode();
            tgs.characterSpacing = style.letterSpacing.value;
            tgs.wordSpacing = style.wordSpacing.value;
            tgs.paragraphSpacing = style.unityParagraphSpacing.value;
            tgs.color = style.color;
            tgs.color *= m_TextElement.playModeTintColor;
            tgs.shouldConvertToLinearSpace = false;
            tgs.parseControlCharacters = m_TextElement.parseEscapeSequences;
            tgs.isRightToLeft = m_TextElement.localLanguageDirection == LanguageDirection.RTL;
            tgs.inverseYAxis = true;
            tgs.fontFeatures = m_ActiveFontFeatures;
            tgs.emojiFallbackSupport = m_TextElement.emojiFallbackSupport;
            tgs.isIMGUI = false;

            // The screenRect in TextCore is not properly implemented with regards to the offset part, so zero it out for now and we will add it ourselves later
            var size = m_TextElement.contentRect.size;

            // If the size is the last rounded size, we use the cached size before the rounding that was calculated
            if (Mathf.Abs(size.x - RoundedSizes.x) < 0.01f && Mathf.Abs(size.y - RoundedSizes.y) < 0.01f)
            {
                size = MeasuredSizes;
            }
            else
            {
                //the size has change, we need to save that information
                RoundedSizes = size;
                MeasuredSizes = size;
            }

            tgs.screenRect = new Rect(Vector2.zero, size);
        }

        internal bool TextLibraryCanElide()
        {
            // TextCore can only elide at the end
            return m_TextElement.computedStyle.unityTextOverflowPosition == TextOverflowPosition.End;
        }

        internal static readonly float k_MinPadding = 6.0f;

        // Function to determine how much extra padding is required as a result of text effect like outline thickness, shadow etc... (see UUM-9524).
        internal float GetTextEffectPadding(FontAsset fontAsset)
        {
            float horizontalPadding;
            float verticalPadding;

            var style = m_TextElement.computedStyle;

            // Grow half inside and half outside
            float outlineThickness = style.unityTextOutlineWidth / 2.0f;

            // Text Shadow is not additive to outline thickness
            float offsetX = Mathf.Abs(style.textShadow.offset.x);
            float offsetY = Mathf.Abs(style.textShadow.offset.y);
            float blurRadius = Mathf.Abs(style.textShadow.blurRadius);

            if (outlineThickness <= 0.0f && offsetX <= 0.0f && offsetY <= 0.0f && blurRadius <= 0.0f)
                return k_MinPadding;

            horizontalPadding = Mathf.Max(offsetX + blurRadius, outlineThickness);
            verticalPadding = Mathf.Max(offsetY + blurRadius, outlineThickness);

            var padding = Mathf.Max(horizontalPadding, verticalPadding) + k_MinPadding;

            var factor = TextUtilities.ConvertPixelUnitsToTextCoreRelativeUnits(m_TextElement, fontAsset);
            var gradientScale = fontAsset.atlasPadding + 1;

            return Mathf.Min(padding * factor * gradientScale, gradientScale);
        }

    }

    internal static class TextUtilities
    {
        public static Func<TextSettings> getEditorTextSettings;
        internal static Func<bool> IsAdvancedTextEnabled;
        private static TextSettings s_TextSettings;
        public static TextSettings textSettings
        {
            get
            {
                if (s_TextSettings == null)
                {
                    s_TextSettings = getEditorTextSettings();
                }
                return s_TextSettings;
            }
        }

        internal static Vector2 MeasureVisualElementTextSize(TextElement te, in RenderedText textToMeasure, float width, VisualElement.MeasureMode widthMode, float height, VisualElement.MeasureMode heightMode)
        {
            float measuredWidth = float.NaN;
            float measuredHeight = float.NaN;

            if (!IsFontAssigned(te))
                return new Vector2(measuredWidth, measuredHeight);

            float pixelsPerPoint = te.scaledPixelsPerPoint;

            if (pixelsPerPoint <= 0)
                return Vector2.zero;

            if (widthMode != VisualElement.MeasureMode.Exactly || heightMode != VisualElement.MeasureMode.Exactly)
            {
                var size = te.uitkTextHandle.ComputeTextSize(textToMeasure, width, height);
                measuredWidth = size.x;
                measuredHeight = size.y;
            }

            if (widthMode == VisualElement.MeasureMode.Exactly)
            {
                measuredWidth = width;
            }
            else
            {
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
                if (heightMode == VisualElement.MeasureMode.AtMost)
                {
                    measuredHeight = Mathf.Min(measuredHeight, height);
                }
            }

            // Case 1215962: round up as yoga could decide to round down and text would start wrapping
            float roundedWidth = AlignmentUtils.CeilToPixelGrid(measuredWidth, pixelsPerPoint, 0.0f);
            float roundedHeight = AlignmentUtils.CeilToPixelGrid(measuredHeight, pixelsPerPoint, 0.0f);
            var roundedValues = new Vector2(roundedWidth, roundedHeight);

            te.uitkTextHandle.MeasuredSizes = new Vector2(measuredWidth, measuredHeight);
            te.uitkTextHandle.RoundedSizes = roundedValues;

            return roundedValues;
        }

        internal static FontAsset GetFontAsset(VisualElement ve)
        {
            if (ve.computedStyle.unityFontDefinition.fontAsset != null)
                return ve.computedStyle.unityFontDefinition.fontAsset as FontAsset;

            var textSettings = GetTextSettingsFrom(ve);
            if (ve.computedStyle.unityFontDefinition.font != null)
                return textSettings.GetCachedFontAsset(ve.computedStyle.unityFontDefinition.font, TextShaderUtilities.ShaderRef_MobileSDF);
            else if (ve.computedStyle.unityFont != null)
                return textSettings.GetCachedFontAsset(ve.computedStyle.unityFont, TextShaderUtilities.ShaderRef_MobileSDF);
            else if (textSettings != null)
                return textSettings.defaultFontAsset;
            return null;
        }

        internal static bool IsFontAssigned(VisualElement ve)
        {
            return ve.computedStyle.unityFont != null || !ve.computedStyle.unityFontDefinition.IsEmpty();
        }

        internal static TextSettings GetTextSettingsFrom(VisualElement ve)
        {
            if (ve.panel is RuntimePanel runtimePanel)
                return runtimePanel.panelSettings.textSettings ?? PanelTextSettings.defaultPanelTextSettings;
            return getEditorTextSettings();
        }

        static bool s_HasAdvancedTextSystemErrorBeenShown = false;

        internal static bool IsAdvancedTextEnabledForElement(TextElement te)
        {
            var isAdvancedTextGeneratorEnabledOnTextElement = te.computedStyle.unityTextGenerator == TextGeneratorType.Advanced;
            var isAdvancedTextGeneratorEnabledOnProject = false;
            isAdvancedTextGeneratorEnabledOnProject = IsAdvancedTextEnabled?.Invoke() ?? false;
            if (!s_HasAdvancedTextSystemErrorBeenShown && !isAdvancedTextGeneratorEnabledOnProject && isAdvancedTextGeneratorEnabledOnTextElement)
            {
                s_HasAdvancedTextSystemErrorBeenShown = true;
                Debug.LogError("Advanced Text Generator is disabled but the API is still called. Please enable it in the UI Toolkit Project Settings if you want to use it, or refrain from calling the API.");
            }

            return isAdvancedTextGeneratorEnabledOnTextElement && isAdvancedTextGeneratorEnabledOnProject;
        }

        internal static float ConvertPixelUnitsToTextCoreRelativeUnits(VisualElement ve, FontAsset fontAsset)
        {
            // Convert the text settings pixel units to TextCore relative units
            float paddingPercent = 1.0f / fontAsset.atlasPadding;
            float pointSizeRatio = ((float)fontAsset.faceInfo.pointSize) / ve.computedStyle.fontSize.value;
            return paddingPercent * pointSizeRatio;
        }

        internal static TextCoreSettings GetTextCoreSettingsForElement(VisualElement ve)
        {
            var fontAsset = GetFontAsset(ve);
            if (fontAsset == null)
                return new TextCoreSettings();

            var resolvedStyle = ve.resolvedStyle;
            var computedStyle = ve.computedStyle;

            float factor = ConvertPixelUnitsToTextCoreRelativeUnits(ve, fontAsset);

            float outlineWidth = Mathf.Clamp(resolvedStyle.unityTextOutlineWidth * factor, 0.0f, 1.0f);
            float underlaySoftness = Mathf.Clamp(computedStyle.textShadow.blurRadius * factor, 0.0f, 1.0f);

            float underlayOffsetX = computedStyle.textShadow.offset.x < 0 ? Mathf.Max(computedStyle.textShadow.offset.x * factor, -1.0f) : Mathf.Min(computedStyle.textShadow.offset.x * factor, 1.0f);
            float underlayOffsetY = computedStyle.textShadow.offset.y < 0 ? Mathf.Max(computedStyle.textShadow.offset.y * factor, -1.0f) : Mathf.Min(computedStyle.textShadow.offset.y * factor, 1.0f);
            Vector2 underlayOffset = new Vector2(underlayOffsetX, underlayOffsetY);

            var faceColor = resolvedStyle.color;
            var outlineColor = resolvedStyle.unityTextOutlineColor;
            if (outlineWidth < UIRUtility.k_Epsilon)
                outlineColor.a = 0.0f;

            return new TextCoreSettings()
            {
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
