// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    internal class UITKTextHandle : TextHandle
    {
        public UITKTextHandle(TextElement te)
        {
            m_TextElement = te;
        }

        public Vector2 MeasuredSizes { get; set; }
        public Vector2 RoundedSizes { get; set; }

        TextElement m_TextElement;

        public Vector2 ComputeTextSize(string textToMeasure, float width, float height)
        {
            ConvertUssToTextGenerationSettings();
            settings.text = textToMeasure;
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
            if (link.hashCode == (int)MarkupTag.HREF)
            {
                if (link.linkId != null && link.linkIdLength > 0)
                {
                    var href = link.GetLinkId();
                    if (Uri.IsWellFormedUriString(href, UriKind.Absolute))
                        Application.OpenURL(href);
                }
            }
        }

        internal bool isOverridingCursor = false;
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

        void ATagOnPointerOut(PointerOutEvent _)
        {
            isOverridingCursor = false;
        }

        internal void LinkTagOnPointerDown(PointerDownEvent pde)
        {
            var pos = pde.localPosition - new Vector3(m_TextElement.contentRect.min.x, m_TextElement.contentRect.min.y);
            var intersectingLink = FindIntersectingLink(pos);
            if (intersectingLink < 0)
                return;

            var link = textInfo.linkInfo[intersectingLink];
            if (link.hashCode != (int)MarkupTag.HREF)
            {
                if (link.linkId != null && link.linkIdLength > 0)
                {
                    using (Experimental.PointerDownLinkTagEvent e = Experimental.PointerDownLinkTagEvent.GetPooled(pde, link.GetLinkId(), link.GetLinkText(textInfo)))
                    {
                        e.elementTarget = m_TextElement;
                        m_TextElement.SendEvent(e);
                    }
                }
            }
        }

        internal void LinkTagOnPointerUp(PointerUpEvent pue)
        {
            var pos = pue.localPosition - new Vector3(m_TextElement.contentRect.min.x, m_TextElement.contentRect.min.y);
            var intersectingLink = FindIntersectingLink(pos);
            if (intersectingLink < 0)
                return;

            var link = textInfo.linkInfo[intersectingLink];
            if (link.hashCode != (int)MarkupTag.HREF)
            {
                if (link.linkId != null && link.linkIdLength > 0)
                {
                    using (Experimental.PointerUpLinkTagEvent e = Experimental.PointerUpLinkTagEvent.GetPooled(pue, link.GetLinkId(), link.GetLinkText(textInfo)))
                    {
                        e.elementTarget = m_TextElement;
                        m_TextElement.SendEvent(e);
                    }
                }
            }
        }

        // Used in automated test
        internal int currentLinkIDHash = -1;
        internal void LinkTagOnPointerMove(PointerMoveEvent pme)
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
                        using (Experimental.PointerOverLinkTagEvent e = Experimental.PointerOverLinkTagEvent.GetPooled(pme, link.GetLinkId(), link.GetLinkText(textInfo)))
                        {
                            e.elementTarget = m_TextElement;
                            m_TextElement.SendEvent(e);
                        }

                        return;
                    }
                    // PointerMove
                    if (currentLinkIDHash == link.hashCode)
                    {
                        using (Experimental.PointerMoveLinkTagEvent e = Experimental.PointerMoveLinkTagEvent.GetPooled(pme, link.GetLinkId(), link.GetLinkText(textInfo)))
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
                using (Experimental.PointerOutLinkTagEvent e = Experimental.PointerOutLinkTagEvent.GetPooled(pme, String.Empty))
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
                using (Experimental.PointerOutLinkTagEvent e = Experimental.PointerOutLinkTagEvent.GetPooled(poe, String.Empty))
                {
                    e.elementTarget = m_TextElement;
                    m_TextElement.SendEvent(e);
                }

                currentLinkIDHash = -1;
            }
        }

        internal void HandleLinkAndATagCallbacks()
        {
            if (m_TextElement == null || m_TextElement.panel == null)
                return;

            if (hasLinkTag)
            {
                m_TextElement.RegisterCallback<PointerDownEvent>(LinkTagOnPointerDown, TrickleDown.TrickleDown);
                m_TextElement.RegisterCallback<PointerUpEvent>(LinkTagOnPointerUp, TrickleDown.TrickleDown);
                m_TextElement.RegisterCallback<PointerMoveEvent>(LinkTagOnPointerMove, TrickleDown.TrickleDown);
                m_TextElement.RegisterCallback<PointerOutEvent>(LinkTagOnPointerOut, TrickleDown.TrickleDown);
            }
            else
            {
                m_TextElement.UnregisterCallback<PointerDownEvent>(LinkTagOnPointerDown, TrickleDown.TrickleDown);
                m_TextElement.UnregisterCallback<PointerUpEvent>(LinkTagOnPointerUp, TrickleDown.TrickleDown);
                m_TextElement.UnregisterCallback<PointerMoveEvent>(LinkTagOnPointerMove, TrickleDown.TrickleDown);
                m_TextElement.UnregisterCallback<PointerOutEvent>(LinkTagOnPointerOut, TrickleDown.TrickleDown);
            }
            if (hasATag)
            {
                m_TextElement.RegisterCallback<PointerUpEvent>(ATagOnPointerUp, TrickleDown.TrickleDown);
                // Switching the cursor to the Link cursor has been disable at runtime until OS cursor support is available at runtime.
                if (m_TextElement.panel.contextType == ContextType.Editor)
                {
                    m_TextElement.RegisterCallback<PointerMoveEvent>(ATagOnPointerMove, TrickleDown.TrickleDown);
                    m_TextElement.RegisterCallback<PointerOverEvent>(ATagOnPointerOver, TrickleDown.TrickleDown);
                    m_TextElement.RegisterCallback<PointerOutEvent>(ATagOnPointerOut, TrickleDown.TrickleDown);
                }
            }
            else
            {
                m_TextElement.UnregisterCallback<PointerUpEvent>(ATagOnPointerUp, TrickleDown.TrickleDown);
                if (m_TextElement.panel.contextType == ContextType.Editor)
                {
                    m_TextElement.UnregisterCallback<PointerMoveEvent>(ATagOnPointerMove, TrickleDown.TrickleDown);
                    m_TextElement.UnregisterCallback<PointerOverEvent>(ATagOnPointerOver, TrickleDown.TrickleDown);
                    m_TextElement.UnregisterCallback<PointerOutEvent>(ATagOnPointerOut, TrickleDown.TrickleDown);
                }
            }
        }

        // Used by our automated tests.
        internal bool hasLinkTag = false;
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
        internal bool hasATag = false;
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
            tgs.text = m_TextElement.isElided && !TextLibraryCanElide() ? m_TextElement.elidedText : m_TextElement.renderedText;
            tgs.isPlaceholder = m_TextElement.showPlaceholderText;

            tgs.fontSize = style.fontSize.value > 0
                ? style.fontSize.value
                : tgs.fontAsset.faceInfo.pointSize;

            tgs.fontStyle = TextGeneratorUtilities.LegacyStyleToNewStyle(style.unityFontStyleAndWeight);
            tgs.textAlignment = TextGeneratorUtilities.LegacyAlignmentToNewAlignment(style.unityTextAlign);

            tgs.wordWrap = style.whiteSpace == WhiteSpace.Normal;

            tgs.wordWrappingRatio = 0.4f;
            tgs.richText = m_TextElement.enableRichText;
            tgs.overflowMode = GetTextOverflowMode();
            tgs.characterSpacing = style.letterSpacing.value;
            tgs.wordSpacing = style.wordSpacing.value;
            tgs.paragraphSpacing = style.unityParagraphSpacing.value;

            tgs.color = style.color;
            tgs.shouldConvertToLinearSpace = false;
            tgs.parseControlCharacters = m_TextElement.parseEscapeSequences;
            tgs.isRightToLeft = m_TextElement.localLanguageDirection == LanguageDirection.RTL;

            if (m_TextElement.panel?.contextType == ContextType.Editor)
                tgs.color *= UIElementsUtility.editorPlayModeTintColor;

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

        internal static Vector2 MeasureVisualElementTextSize(TextElement te, string textToMeasure, float width, VisualElement.MeasureMode widthMode, float height, VisualElement.MeasureMode heightMode)
        {
            float measuredWidth = float.NaN;
            float measuredHeight = float.NaN;

            if (textToMeasure == null || !IsFontAssigned(te))
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

        internal static TextSettings GetTextSettingsFrom(VisualElement ve)
        {
            if (ve.panel is RuntimePanel runtimePanel)
                return runtimePanel.panelSettings.textSettings ?? PanelTextSettings.defaultPanelTextSettings;
            return getEditorTextSettings();
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
