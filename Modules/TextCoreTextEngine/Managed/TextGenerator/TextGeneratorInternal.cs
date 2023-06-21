// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.TextCore.LowLevel;

namespace UnityEngine.TextCore.Text
{
    internal partial class TextGenerator
    {
        void SaveWordWrappingState(ref WordWrapState state, int index, int count, TextInfo textInfo)
        {
            // Multi Font & Material support related
            state.currentFontAsset = m_CurrentFontAsset;
            state.currentSpriteAsset = m_CurrentSpriteAsset;
            state.currentMaterial = m_CurrentMaterial;
            state.currentMaterialIndex = m_CurrentMaterialIndex;

            state.previousWordBreak = index;
            state.totalCharacterCount = count;
            state.visibleCharacterCount = m_LineVisibleCharacterCount;
            state.visibleSpaceCount = m_LineVisibleSpaceCount;
            state.visibleLinkCount = textInfo.linkCount;

            state.firstCharacterIndex = m_FirstCharacterOfLine;
            state.firstVisibleCharacterIndex = m_FirstVisibleCharacterOfLine;
            state.lastVisibleCharIndex = m_LastVisibleCharacterOfLine;

            state.fontStyle = m_FontStyleInternal;
            state.italicAngle = m_ItalicAngle;

            state.fontScaleMultiplier = m_FontScaleMultiplier;
            state.currentFontSize = m_CurrentFontSize;

            state.xAdvance = m_XAdvance;
            state.maxCapHeight = m_MaxCapHeight;
            state.maxAscender = m_MaxAscender;
            state.maxDescender = m_MaxDescender;
            state.maxLineAscender = m_MaxLineAscender;
            state.maxLineDescender = m_MaxLineDescender;
            state.startOfLineAscender = m_StartOfLineAscender;
            state.preferredWidth = m_PreferredWidth;
            state.preferredHeight = m_PreferredHeight;
            state.meshExtents = m_MeshExtents;
            state.pageAscender = m_PageAscender;

            state.lineNumber = m_LineNumber;
            state.lineOffset = m_LineOffset;
            state.baselineOffset = m_BaselineOffset;
            state.isDrivenLineSpacing = m_IsDrivenLineSpacing;

            state.vertexColor = m_HtmlColor;
            state.underlineColor = m_UnderlineColor;
            state.strikethroughColor = m_StrikethroughColor;
            state.highlightColor = m_HighlightColor;
            state.highlightState = m_HighlightState;

            state.isNonBreakingSpace = m_IsNonBreakingSpace;
            state.tagNoParsing = m_TagNoParsing;
            state.fxScale = m_FXScale;
            state.fxRotation = m_FXRotation;

            // XML Tag Stack
            state.basicStyleStack = m_FontStyleStack;
            state.italicAngleStack = m_ItalicAngleStack;
            state.colorStack = m_ColorStack;
            state.underlineColorStack = m_UnderlineColorStack;
            state.strikethroughColorStack = m_StrikethroughColorStack;
            state.highlightColorStack = m_HighlightColorStack;
            state.colorGradientStack = m_ColorGradientStack;
            state.highlightStateStack = m_HighlightStateStack;
            state.sizeStack = m_SizeStack;
            state.indentStack = m_IndentStack;
            state.fontWeightStack = m_FontWeightStack;
            state.styleStack = m_StyleStack;
            state.baselineStack = m_BaselineOffsetStack;
            state.actionStack = m_ActionStack;
            state.materialReferenceStack = m_MaterialReferenceStack;
            state.lineJustificationStack = m_LineJustificationStack;

            state.lastBaseGlyphIndex = m_LastBaseGlyphIndex;
            state.spriteAnimationId = m_SpriteAnimationId;

            if (m_LineNumber < textInfo.lineInfo.Length)
                state.lineInfo = textInfo.lineInfo[m_LineNumber];
        }

        int RestoreWordWrappingState(ref WordWrapState state, TextInfo textInfo)
        {
            int index = state.previousWordBreak;

            // Multi Font & Material support related
            m_CurrentFontAsset = state.currentFontAsset;
            m_CurrentSpriteAsset = state.currentSpriteAsset;
            m_CurrentMaterial = state.currentMaterial;
            m_CurrentMaterialIndex = state.currentMaterialIndex;

            m_CharacterCount = state.totalCharacterCount + 1;
            m_LineVisibleCharacterCount = state.visibleCharacterCount;
            m_LineVisibleSpaceCount = state.visibleSpaceCount;

            textInfo.linkCount = state.visibleLinkCount;

            m_FirstCharacterOfLine = state.firstCharacterIndex;
            m_FirstVisibleCharacterOfLine = state.firstVisibleCharacterIndex;
            m_LastVisibleCharacterOfLine = state.lastVisibleCharIndex;

            m_FontStyleInternal = state.fontStyle;
            m_ItalicAngle = state.italicAngle;
            m_FontScaleMultiplier = state.fontScaleMultiplier;

            m_CurrentFontSize = state.currentFontSize;

            m_XAdvance = state.xAdvance;
            m_MaxCapHeight = state.maxCapHeight;
            m_MaxAscender = state.maxAscender;
            m_MaxDescender = state.maxDescender;
            m_MaxLineAscender = state.maxLineAscender;
            m_MaxLineDescender = state.maxLineDescender;
            m_StartOfLineAscender = state.startOfLineAscender;
            m_PreferredWidth = state.preferredWidth;
            m_PreferredHeight = state.preferredHeight;
            m_MeshExtents = state.meshExtents;
            m_PageAscender = state.pageAscender;

            m_LineNumber = state.lineNumber;
            m_LineOffset = state.lineOffset;
            m_BaselineOffset = state.baselineOffset;
            m_IsDrivenLineSpacing = state.isDrivenLineSpacing;

            m_HtmlColor = state.vertexColor;
            m_UnderlineColor = state.underlineColor;
            m_StrikethroughColor = state.strikethroughColor;
            m_HighlightColor = state.highlightColor;
            m_HighlightState = state.highlightState;

            m_IsNonBreakingSpace = state.isNonBreakingSpace;
            m_TagNoParsing = state.tagNoParsing;
            m_FXScale = state.fxScale;
            m_FXRotation = state.fxRotation;

            // XML Tag Stack
            m_FontStyleStack = state.basicStyleStack;
            m_ItalicAngleStack = state.italicAngleStack;
            m_ColorStack = state.colorStack;
            m_UnderlineColorStack = state.underlineColorStack;
            m_StrikethroughColorStack = state.strikethroughColorStack;
            m_HighlightColorStack = state.highlightColorStack;
            m_ColorGradientStack = state.colorGradientStack;
            m_HighlightStateStack = state.highlightStateStack;
            m_SizeStack = state.sizeStack;
            m_IndentStack = state.indentStack;
            m_FontWeightStack = state.fontWeightStack;
            m_StyleStack = state.styleStack;
            m_BaselineOffsetStack = state.baselineStack;
            m_ActionStack = state.actionStack;
            m_MaterialReferenceStack = state.materialReferenceStack;
            m_LineJustificationStack = state.lineJustificationStack;

            m_LastBaseGlyphIndex = state.lastBaseGlyphIndex;
            m_SpriteAnimationId = state.spriteAnimationId;

            if (m_LineNumber < textInfo.lineInfo.Length)
                textInfo.lineInfo[m_LineNumber] = state.lineInfo;

            return index;
        }

        /// <summary>
        /// Store vertex information for each character.
        /// </summary>
        /// <param name="padding"></param>
        /// <param name="stylePadding">stylePadding.</param>
        /// <param name="vertexColor">Vertex color.</param>
        /// <param name="generationSettings"></param>
        /// <param name="textInfo"></param>
        void SaveGlyphVertexInfo(float padding, float stylePadding, Color32 vertexColor, TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            #region Setup Mesh Vertices
            // Save the Vertex Position for the Character
            textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.position = textInfo.textElementInfo[m_CharacterCount].bottomLeft;
            textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.position = textInfo.textElementInfo[m_CharacterCount].topLeft;
            textInfo.textElementInfo[m_CharacterCount].vertexTopRight.position = textInfo.textElementInfo[m_CharacterCount].topRight;
            textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.position = textInfo.textElementInfo[m_CharacterCount].bottomRight;
            #endregion


            #region Setup Vertex Colors
            // Alpha is the lower of the vertex color or tag color alpha used.
            vertexColor.a = m_FontColor32.a < vertexColor.a ? m_FontColor32.a : vertexColor.a;

            bool isColorGlyph = ((GlyphRasterModes)m_CurrentFontAsset.m_AtlasRenderMode & GlyphRasterModes.RASTER_MODE_COLOR) == GlyphRasterModes.RASTER_MODE_COLOR;

            // Handle Vertex Colors & Vertex Color Gradient
            if (generationSettings.fontColorGradient == null || isColorGlyph)
            {
                // Special handling for color glyphs
                vertexColor = isColorGlyph ? new Color32(255, 255, 255, vertexColor.a) : vertexColor;

                textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.color = vertexColor;
                textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.color = vertexColor;
                textInfo.textElementInfo[m_CharacterCount].vertexTopRight.color = vertexColor;
                textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.color = vertexColor;
            }
            else
            {
                if (!generationSettings.overrideRichTextColors && m_ColorStack.index > 1)
                {
                    textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.color = vertexColor;
                    textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.color = vertexColor;
                    textInfo.textElementInfo[m_CharacterCount].vertexTopRight.color = vertexColor;
                    textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.color = vertexColor;
                }
                else // Handle Vertex Color Gradient
                {
                    // Use Vertex Color Gradient Preset (if one is assigned)
                    if (generationSettings.fontColorGradientPreset != null)
                    {
                        textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.color = generationSettings.fontColorGradientPreset.bottomLeft * vertexColor;
                        textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.color = generationSettings.fontColorGradientPreset.topLeft * vertexColor;
                        textInfo.textElementInfo[m_CharacterCount].vertexTopRight.color = generationSettings.fontColorGradientPreset.topRight * vertexColor;
                        textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.color = generationSettings.fontColorGradientPreset.bottomRight * vertexColor;
                    }
                    else
                    {
                        textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.color = generationSettings.fontColorGradient.bottomLeft * vertexColor;
                        textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.color = generationSettings.fontColorGradient.topLeft * vertexColor;
                        textInfo.textElementInfo[m_CharacterCount].vertexTopRight.color = generationSettings.fontColorGradient.topRight * vertexColor;
                        textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.color = generationSettings.fontColorGradient.bottomRight * vertexColor;
                    }
                }
            }

            if (m_ColorGradientPreset != null && !isColorGlyph)
            {
                if (m_ColorGradientPresetIsTinted)
                {
                    textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.color *= m_ColorGradientPreset.bottomLeft;
                    textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.color *= m_ColorGradientPreset.topLeft;
                    textInfo.textElementInfo[m_CharacterCount].vertexTopRight.color *= m_ColorGradientPreset.topRight;
                    textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.color *= m_ColorGradientPreset.bottomRight;
                }
                else
                {
                    textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.color = TextGeneratorUtilities.MinAlpha(m_ColorGradientPreset.bottomLeft, vertexColor);
                    textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.color = TextGeneratorUtilities.MinAlpha(m_ColorGradientPreset.topLeft, vertexColor);
                    textInfo.textElementInfo[m_CharacterCount].vertexTopRight.color = TextGeneratorUtilities.MinAlpha(m_ColorGradientPreset.topRight, vertexColor);
                    textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.color = TextGeneratorUtilities.MinAlpha(m_ColorGradientPreset.bottomRight, vertexColor);
                }
            }
            #endregion

            stylePadding = 0;

            // Setup UVs for the Character
            #region Setup UVs
            Glyph altGlyph = textInfo.textElementInfo[m_CharacterCount].alternativeGlyph;
            GlyphRect glyphRect = altGlyph == null ? m_CachedTextElement.m_Glyph.glyphRect : altGlyph.glyphRect;

            Vector2 uv0;
            uv0.x = (glyphRect.x - padding - stylePadding) / m_CurrentFontAsset.atlasWidth;
            uv0.y = (glyphRect.y - padding - stylePadding) / m_CurrentFontAsset.atlasHeight;

            Vector2 uv1;
            uv1.x = uv0.x;
            uv1.y = (glyphRect.y + padding + stylePadding + glyphRect.height) / m_CurrentFontAsset.atlasHeight;

            Vector2 uv2;
            uv2.x = (glyphRect.x + padding + stylePadding + glyphRect.width) / m_CurrentFontAsset.atlasWidth;
            uv2.y = uv1.y;

            Vector2 uv3;
            uv3.x = uv2.x;
            uv3.y = uv0.y;

            // Store UV Information
            textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.uv = uv0;
            textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.uv = uv1;
            textInfo.textElementInfo[m_CharacterCount].vertexTopRight.uv = uv2;
            textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.uv = uv3;
            #endregion Setup UVs
        }

        void SaveSpriteVertexInfo(Color32 vertexColor, TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            // Save the Vertex Position for the Character
            #region Setup Mesh Vertices
            textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.position = textInfo.textElementInfo[m_CharacterCount].bottomLeft;
            textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.position = textInfo.textElementInfo[m_CharacterCount].topLeft;
            textInfo.textElementInfo[m_CharacterCount].vertexTopRight.position = textInfo.textElementInfo[m_CharacterCount].topRight;
            textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.position = textInfo.textElementInfo[m_CharacterCount].bottomRight;
            #endregion

            // Vertex Color Alpha
            if (generationSettings.tintSprites)
                m_TintSprite = true;

            Color32 spriteColor = m_TintSprite ? ColorUtilities.MultiplyColors(m_SpriteColor, vertexColor) : m_SpriteColor;
            spriteColor.a = spriteColor.a < m_FontColor32.a ? spriteColor.a < vertexColor.a ? spriteColor.a : vertexColor.a : m_FontColor32.a;

            Color32 c0 = spriteColor;
            Color32 c1 = spriteColor;
            Color32 c2 = spriteColor;
            Color32 c3 = spriteColor;

            if (generationSettings.fontColorGradient != null)
            {
                if (generationSettings.fontColorGradientPreset != null) {
                    c0 = m_TintSprite ? ColorUtilities.MultiplyColors(c0, generationSettings.fontColorGradientPreset.bottomLeft) : c0;
                    c1 = m_TintSprite ? ColorUtilities.MultiplyColors(c1, generationSettings.fontColorGradientPreset.topLeft) : c1;
                    c2 = m_TintSprite ? ColorUtilities.MultiplyColors(c2, generationSettings.fontColorGradientPreset.topRight) : c2;
                    c3 = m_TintSprite ? ColorUtilities.MultiplyColors(c3, generationSettings.fontColorGradientPreset.bottomRight) : c3;
                }
                else
                {
                    c0 = m_TintSprite ? ColorUtilities.MultiplyColors(c0, generationSettings.fontColorGradient.bottomLeft) : c0;
                    c1 = m_TintSprite ? ColorUtilities.MultiplyColors(c1, generationSettings.fontColorGradient.topLeft) : c1;
                    c2 = m_TintSprite ? ColorUtilities.MultiplyColors(c2, generationSettings.fontColorGradient.topRight) : c2;
                    c3 = m_TintSprite ? ColorUtilities.MultiplyColors(c3, generationSettings.fontColorGradient.bottomRight) : c3;
                }
            }

            if (m_ColorGradientPreset != null)
            {
                c0 = m_TintSprite ? ColorUtilities.MultiplyColors(c0, m_ColorGradientPreset.bottomLeft) : c0;
                c1 = m_TintSprite ? ColorUtilities.MultiplyColors(c1, m_ColorGradientPreset.topLeft) : c1;
                c2 = m_TintSprite ? ColorUtilities.MultiplyColors(c2, m_ColorGradientPreset.topRight) : c2;
                c3 = m_TintSprite ? ColorUtilities.MultiplyColors(c3, m_ColorGradientPreset.bottomRight) : c3;
            }

            textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.color = c0;
            textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.color = c1;
            textInfo.textElementInfo[m_CharacterCount].vertexTopRight.color = c2;
            textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.color = c3;

            // Setup UVs for the Character
            #region Setup UVs
            Vector2 uv0 = new Vector2((float)m_CachedTextElement.glyph.glyphRect.x / m_CurrentSpriteAsset.width, (float)m_CachedTextElement.glyph.glyphRect.y / m_CurrentSpriteAsset.height); // bottom left
            Vector2 uv1 = new Vector2(uv0.x, (float)(m_CachedTextElement.glyph.glyphRect.y + m_CachedTextElement.glyph.glyphRect.height) / m_CurrentSpriteAsset.height); // top left
            Vector2 uv2 = new Vector2((float)(m_CachedTextElement.glyph.glyphRect.x + m_CachedTextElement.glyph.glyphRect.width) / m_CurrentSpriteAsset.width, uv1.y); // top right
            Vector2 uv3 = new Vector2(uv2.x, uv0.y); // bottom right

            // Store UV Information
            textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.uv = uv0;
            textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.uv = uv1;
            textInfo.textElementInfo[m_CharacterCount].vertexTopRight.uv = uv2;
            textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.uv = uv3;
            #endregion Setup UVs
        }

        /// <summary>
        /// Method to add the underline geometry.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="index"></param>
        /// <param name="startScale"></param>
        /// <param name="endScale"></param>
        /// <param name="maxScale"></param>
        /// <param name="sdfScale"></param>
        /// <param name="underlineColor"></param>
        /// <param name="generationSettings"></param>
        /// <param name="textInfo"></param>
        void DrawUnderlineMesh(Vector3 start, Vector3 end, float startScale, float endScale, float maxScale, float sdfScale, Color32 underlineColor, TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            // Get Underline special character from the primary font asset.
            GetUnderlineSpecialCharacter(generationSettings);

            if (m_Underline.character == null)
            {
                if (generationSettings.textSettings.displayWarnings)
                    Debug.LogWarning("Unable to add underline or strikethrough since the character [0x5F] used by these features is not present in the Font Asset assigned to this text object.");
                return;
            }

            const int k_VertexIncrease = 12;
            int index = textInfo.meshInfo[m_CurrentMaterialIndex].vertexCount;
            int newVerticesCount = index + k_VertexIncrease;

            // Check to make sure our current mesh buffer allocations can hold these new Quads.
            if (newVerticesCount > textInfo.meshInfo[m_CurrentMaterialIndex].vertexBufferSize)
            {
                // Resize Mesh Buffers
                textInfo.meshInfo[m_CurrentMaterialIndex].ResizeMeshInfo(newVerticesCount / 4, generationSettings.isIMGUI);
            }

            // Adjust the position of the underline based on the lowest character. This matters for subscript character.
            start.y = Mathf.Min(start.y, end.y);
            end.y = Mathf.Min(start.y, end.y);

            GlyphMetrics underlineGlyphMetrics = m_Underline.character.glyph.metrics;
            GlyphRect underlineGlyphRect = m_Underline.character.glyph.glyphRect;

            float segmentWidth = underlineGlyphMetrics.width / 2 * maxScale;

            if (end.x - start.x < underlineGlyphMetrics.width * maxScale)
            {
                segmentWidth = (end.x - start.x) / 2f;
            }

            float startPadding = m_Padding * startScale / maxScale;
            float endPadding = m_Padding * endScale / maxScale;

            float underlineThickness = m_Underline.fontAsset.faceInfo.underlineThickness;

            // UNDERLINE VERTICES FOR (3) LINE SEGMENTS
            #region UNDERLINE VERTICES
            var vertexData = textInfo.meshInfo[m_CurrentMaterialIndex].vertexData;

            if (textInfo.vertexDataLayout == VertexDataLayout.VBO)
            {
                // Front Part of the Underline
                vertexData[index + 0].position = start + new Vector3(0, 0 - (underlineThickness + m_Padding) * maxScale, 0); // BL
                vertexData[index + 1].position = start + new Vector3(0, m_Padding * maxScale, 0); // TL
                vertexData[index + 2].position = vertexData[index + 1].position + new Vector3(segmentWidth, 0, 0); // TR
                vertexData[index + 3].position = vertexData[index + 0].position + new Vector3(segmentWidth, 0, 0); // BR

                // Middle Part of the Underline
                vertexData[index + 4].position = vertexData[index + 3].position; // BL
                vertexData[index + 5].position = vertexData[index + 2].position; // TL
                vertexData[index + 6].position = end + new Vector3(-segmentWidth, m_Padding * maxScale, 0); // TR
                vertexData[index + 7].position = end + new Vector3(-segmentWidth, -(underlineThickness + m_Padding) * maxScale, 0); // BR

                // End Part of the Underline
                vertexData[index + 8].position = vertexData[index + 7].position; // BL
                vertexData[index + 9].position = vertexData[index + 6].position; // TL
                vertexData[index + 10].position = end + new Vector3(0, m_Padding * maxScale, 0); // TR
                vertexData[index + 11].position = end + new Vector3(0, -(underlineThickness + m_Padding) * maxScale, 0); // BR
            }
            else
            {
                Vector3[] vertices = textInfo.meshInfo[m_CurrentMaterialIndex].vertices;
                // Front Part of the Underline
                vertices[index + 0] = start + new Vector3(0, 0 - (underlineThickness + m_Padding) * maxScale, 0); // BL
                vertices[index + 1] = start + new Vector3(0, m_Padding * maxScale, 0); // TL
                vertices[index + 2] = vertices[index + 1] + new Vector3(segmentWidth, 0, 0); // TR
                vertices[index + 3] = vertices[index + 0] + new Vector3(segmentWidth, 0, 0); // BR

                // Middle Part of the Underline
                vertices[index + 4] = vertices[index + 3]; // BL
                vertices[index + 5] = vertices[index + 2]; // TL
                vertices[index + 6] = end + new Vector3(-segmentWidth, m_Padding * maxScale, 0); // TR
                vertices[index + 7] = end + new Vector3(-segmentWidth, -(underlineThickness + m_Padding) * maxScale, 0); // BR

                // End Part of the Underline
                vertices[index + 8] = vertices[index + 7]; // BL
                vertices[index + 9] = vertices[index + 6]; // TL
                vertices[index + 10] = end + new Vector3(0, m_Padding * maxScale, 0); // TR
                vertices[index + 11] = end + new Vector3(0, -(underlineThickness + m_Padding) * maxScale, 0); // BR
            }
            #endregion

            // Handle potential axis inversion.
            if (generationSettings.inverseYAxis)
            {
                Vector3 axisOffset;
                axisOffset.x = 0;
                axisOffset.y = generationSettings.screenRect.height;
                axisOffset.z = 0;

                if (textInfo.vertexDataLayout == VertexDataLayout.VBO)
                {
                    for (int i = 0; i < 12; i++)
                    {
                        //vertices[index + i].x += axisOffset.x;
                        textInfo.meshInfo[m_CurrentMaterialIndex].vertexData[index + i].position.y = textInfo.meshInfo[m_CurrentMaterialIndex].vertexData[index + i].position.y * -1 + axisOffset.y;
                    }
                }
                else
                {
                    for (int i = 0; i < 12; i++)
                    {
                        //vertices[index + i].x += axisOffset.x;
                        textInfo.meshInfo[m_CurrentMaterialIndex].vertices[index + i].y = textInfo.meshInfo[m_CurrentMaterialIndex].vertices[index + i].y * -1 + axisOffset.y;
                    }
                }
            }

            // UNDERLINE UV0
            #region HANDLE UV0

            int atlasWidth = m_Underline.fontAsset.atlasWidth;
            int atlasHeight = m_Underline.fontAsset.atlasHeight;

            float xScale = Mathf.Abs(sdfScale);

            // Calculate UV required to setup the 3 Quads for the Underline.
            Vector4 uv0 = new Vector4((underlineGlyphRect.x - startPadding) / atlasWidth, (underlineGlyphRect.y - m_Padding) / atlasHeight, 0, xScale);  // bottom left
            Vector4 uv1 = new Vector4(uv0.x, (underlineGlyphRect.y + underlineGlyphRect.height + m_Padding) / atlasHeight, 0, xScale);  // top left
            Vector4 uv2 = new Vector4((underlineGlyphRect.x - startPadding + (float)underlineGlyphRect.width / 2) / atlasWidth, uv1.y, 0, xScale); // Mid Top Left
            Vector4 uv3 = new Vector4(uv2.x, uv0.y, 0, xScale); // Mid Bottom Left
            Vector4 uv4 = new Vector4((underlineGlyphRect.x + endPadding + (float)underlineGlyphRect.width / 2) / atlasWidth, uv1.y, 0, xScale); // Mid Top Right
            Vector4 uv5 = new Vector4(uv4.x, uv0.y, 0, xScale); // Mid Bottom right
            Vector4 uv6 = new Vector4((underlineGlyphRect.x + endPadding + underlineGlyphRect.width) / atlasWidth, uv1.y, 0, xScale); // End Part - Bottom Right
            Vector4 uv7 = new Vector4(uv6.x, uv0.y, 0, xScale); // End Part - Top Right

            if (textInfo.vertexDataLayout == VertexDataLayout.VBO)
            {
                // Left Part of the Underline
                vertexData[0 + index].uv0 = uv0; // BL
                vertexData[1 + index].uv0 = uv1; // TL
                vertexData[2 + index].uv0 = uv2; // TR
                vertexData[3 + index].uv0 = uv3; // BR

                // Middle Part of the Underline
                vertexData[4 + index].uv0 = new Vector4(uv2.x - uv2.x * 0.001f, uv0.y, 0, xScale);
                vertexData[5 + index].uv0 = new Vector4(uv2.x - uv2.x * 0.001f, uv1.y, 0, xScale);
                vertexData[6 + index].uv0 = new Vector4(uv2.x + uv2.x * 0.001f, uv1.y, 0, xScale);
                vertexData[7 + index].uv0 = new Vector4(uv2.x + uv2.x * 0.001f, uv0.y, 0, xScale);

                // Right Part of the Underline
                vertexData[8 + index].uv0 = uv5;
                vertexData[9 + index].uv0 = uv4;
                vertexData[10 + index].uv0 = uv6;
                vertexData[11 + index].uv0 = uv7;
            }
            else
            {
                Vector4[] uvs0 = textInfo.meshInfo[m_CurrentMaterialIndex].uvs0;
                // Left Part of the Underline
                uvs0[0 + index] = uv0; // BL
                uvs0[1 + index] = uv1; // TL
                uvs0[2 + index] = uv2; // TR
                uvs0[3 + index] = uv3; // BR

                // Middle Part of the Underline
                uvs0[4 + index] = new Vector4(uv2.x - uv2.x * 0.001f, uv0.y, 0, xScale);
                uvs0[5 + index] = new Vector4(uv2.x - uv2.x * 0.001f, uv1.y, 0, xScale);
                uvs0[6 + index] = new Vector4(uv2.x + uv2.x * 0.001f, uv1.y, 0, xScale);
                uvs0[7 + index] = new Vector4(uv2.x + uv2.x * 0.001f, uv0.y, 0, xScale);

                // Right Part of the Underline
                uvs0[8 + index] = uv5;
                uvs0[9 + index] = uv4;
                uvs0[10 + index] = uv6;
                uvs0[11 + index] = uv7;
            }
            #endregion

            // UNDERLINE UV2
            #region HANDLE UV2 - SDF SCALE
            // UV1 contains Face / Border UV layout.
            float min_UvX = 0;

            if (textInfo.vertexDataLayout == VertexDataLayout.VBO)
            {
                float max_UvX = (vertexData[index + 2].position.x - start.x) / (end.x - start.x);

                vertexData[0 + index].uv2 = new Vector2(0, 0);
                vertexData[1 + index].uv2 = new Vector2(0, 1);
                vertexData[2 + index].uv2 = new Vector2(max_UvX, 1);
                vertexData[3 + index].uv2 = new Vector2(max_UvX, 0);

                min_UvX = (vertexData[index + 4].position.x - start.x) / (end.x - start.x);
                max_UvX = (vertexData[index + 6].position.x - start.x) / (end.x - start.x);

                vertexData[4 + index].uv2 = new Vector2(min_UvX, 0);
                vertexData[5 + index].uv2 = new Vector2(min_UvX, 1);
                vertexData[6 + index].uv2 = new Vector2(max_UvX, 1);
                vertexData[7 + index].uv2 = new Vector2(max_UvX, 0);

                min_UvX = (vertexData[index + 8].position.x - start.x) / (end.x - start.x);

                vertexData[8 + index].uv2 = new Vector2(min_UvX, 0);
                vertexData[9 + index].uv2 = new Vector2(min_UvX, 1);
                vertexData[10 + index].uv2 = new Vector2(1, 1);
                vertexData[11 + index].uv2 = new Vector2(1, 0);
            }
            else
            {
                Vector3[] vertices = textInfo.meshInfo[m_CurrentMaterialIndex].vertices;
                float max_UvX = (vertices[index + 2].x - start.x) / (end.x - start.x);

                Vector2[] uvs2 = textInfo.meshInfo[m_CurrentMaterialIndex].uvs2;

                uvs2[0 + index] = new Vector2(0, 0);
                uvs2[1 + index] = new Vector2(0, 1);
                uvs2[2 + index] = new Vector2(max_UvX, 1);
                uvs2[3 + index] = new Vector2(max_UvX, 0);

                min_UvX = (vertices[index + 4].x - start.x) / (end.x - start.x);
                max_UvX = (vertices[index + 6].x - start.x) / (end.x - start.x);

                uvs2[4 + index] = new Vector2(min_UvX, 0);
                uvs2[5 + index] = new Vector2(min_UvX, 1);
                uvs2[6 + index] = new Vector2(max_UvX, 1);
                uvs2[7 + index] = new Vector2(max_UvX, 0);

                min_UvX = (vertices[index + 8].x - start.x) / (end.x - start.x);

                uvs2[8 + index] = new Vector2(min_UvX, 0);
                uvs2[9 + index] = new Vector2(min_UvX, 1);
                uvs2[10 + index] = new Vector2(1, 1);
                uvs2[11 + index] = new Vector2(1, 0);
            }
            #endregion

            // UNDERLINE VERTEX COLORS
            #region UNDERLINE VERTEX COLORS
            // Alpha is the lower of the vertex color or tag color alpha used.
            underlineColor.a = m_FontColor32.a < underlineColor.a ? m_FontColor32.a : underlineColor.a;

            if (textInfo.vertexDataLayout == VertexDataLayout.VBO)
            {
                for (int i = 0; i < 12; i++)
                {
                    vertexData[i + index].color = underlineColor;
                }
            }
            else
            {
                Color32[] colors32 = textInfo.meshInfo[m_CurrentMaterialIndex].colors32;
                for (int i = 0; i < 12; i++)
                {
                    colors32[i + index] = underlineColor;
                }
            }
            #endregion

            textInfo.meshInfo[m_CurrentMaterialIndex].vertexCount += k_VertexIncrease;
        }

        void DrawTextHighlight(Vector3 start, Vector3 end, Color32 highlightColor, TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            GetUnderlineSpecialCharacter(generationSettings);

            if (m_Underline.character == null)
            {
                if (generationSettings.textSettings.displayWarnings)
                    Debug.LogWarning("Unable to add highlight since the primary Font Asset doesn't contain the underline character.");

                return;
            }

            const int k_VertexIncrease = 4;
            int index = textInfo.meshInfo[m_CurrentMaterialIndex].vertexCount;
            int newVerticesCount = index + k_VertexIncrease;

            // Check to make sure our current mesh buffer allocations can hold these new Quads.
            if (newVerticesCount > textInfo.meshInfo[m_CurrentMaterialIndex].vertexBufferSize)
            {
                // Resize Mesh Buffers
                textInfo.meshInfo[m_CurrentMaterialIndex].ResizeMeshInfo(newVerticesCount / 4, generationSettings.isIMGUI);
            }

            var vertexData = textInfo.meshInfo[m_CurrentMaterialIndex].vertexData;

            // UNDERLINE VERTICES FOR (3) LINE SEGMENTS
            #region HIGHLIGHT VERTICES

            if (textInfo.vertexDataLayout == VertexDataLayout.VBO)
            {
                vertexData[index + 0].position = start; // BL
                vertexData[index + 1].position = new Vector3(start.x, end.y, 0); // TL
                vertexData[index + 2].position = end; // TR
                vertexData[index + 3].position = new Vector3(end.x, start.y, 0); // BR
            }
            else
            {
                Vector3[] vertices = textInfo.meshInfo[m_CurrentMaterialIndex].vertices;
                // Front Part of the Underline
                vertices[index + 0] = start; // BL
                vertices[index + 1] = new Vector3(start.x, end.y, 0); // TL
                vertices[index + 2] = end; // TR
                vertices[index + 3] = new Vector3(end.x, start.y, 0); // BR
            }

            #endregion

            if (generationSettings.inverseYAxis)
            {
                Vector3 axisOffset;
                axisOffset.x = 0;
                axisOffset.y = generationSettings.screenRect.height;
                axisOffset.z = 0;

                if (textInfo.vertexDataLayout == VertexDataLayout.VBO)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        //vertices[index + 0].x += axisOffset.x;
                        vertexData[index + i].position.y = vertexData[index + i].position.y * -1 + axisOffset.y;
                    }
                }
                else
                {
                    for (int i = 0; i < 4; i++)
                    {
                        //vertices[index + 0].x += axisOffset.x;
                        textInfo.meshInfo[m_CurrentMaterialIndex].vertices[index + i].y = textInfo.meshInfo[m_CurrentMaterialIndex].vertices[index + i].y * -1 + axisOffset.y;
                    }
                }
            }

            // UNDERLINE UV0
            #region HANDLE UV0

            int atlasWidth = m_Underline.fontAsset.atlasWidth;
            int atlasHeight = m_Underline.fontAsset.atlasHeight;
            GlyphRect glyphRect = m_Underline.character.glyph.glyphRect;

            // Calculate UV
            Vector2 uvGlyphCenter = new Vector2((glyphRect.x + (float)glyphRect.width / 2) / atlasWidth, (glyphRect.y + (float)glyphRect.height / 2) / atlasHeight);
            Vector2 uvTexelSize = new Vector2(1.0f / atlasWidth, 1.0f / atlasHeight);

            if (textInfo.vertexDataLayout == VertexDataLayout.VBO)
            {
                // UVs for the Quad
                vertexData[0 + index].uv0 = uvGlyphCenter - uvTexelSize; // BL
                vertexData[1 + index].uv0 = uvGlyphCenter + new Vector2(-uvTexelSize.x, uvTexelSize.y); // TL
                vertexData[2 + index].uv0 = uvGlyphCenter + uvTexelSize; // TR
                vertexData[3 + index].uv0 = uvGlyphCenter + new Vector2(uvTexelSize.x, -uvTexelSize.y); // BR
            }
            else
            {
                Vector4[] uvs0 = textInfo.meshInfo[m_CurrentMaterialIndex].uvs0;
                // UVs for the Quad
                uvs0[0 + index] = uvGlyphCenter - uvTexelSize; // BL
                uvs0[1 + index] = uvGlyphCenter + new Vector2(-uvTexelSize.x, uvTexelSize.y); // TL
                uvs0[2 + index] = uvGlyphCenter + uvTexelSize; // TR
                uvs0[3 + index] = uvGlyphCenter + new Vector2(uvTexelSize.x, -uvTexelSize.y); // BR
            }
            #endregion

            // HIGHLIGHT UV2
            #region HANDLE UV2 - SDF SCALE

            Vector2 customUV = new Vector2(0, 1);
            if (textInfo.vertexDataLayout == VertexDataLayout.VBO)
            {
                // HIGHLIGHT UV2
                vertexData[0 + index].uv2 = customUV;
                vertexData[1 + index].uv2 = customUV;
                vertexData[2 + index].uv2 = customUV;
                vertexData[3 + index].uv2 = customUV;
            }
            else
            {
                Vector2[] uvs2 = textInfo.meshInfo[m_CurrentMaterialIndex].uvs2;
                // HIGHLIGHT UV2
                uvs2[0 + index] = customUV;
                uvs2[1 + index] = customUV;
                uvs2[2 + index] = customUV;
                uvs2[3 + index] = customUV;
            }
            #endregion

            // HIGHLIGHT VERTEX COLORS
            #region HIGHLIGHT VERTEX COLORS
            // Alpha is the lower of the vertex color or tag color alpha used.
            highlightColor.a = m_FontColor32.a < highlightColor.a ? m_FontColor32.a : highlightColor.a;

            if (textInfo.vertexDataLayout == VertexDataLayout.VBO)
            {
                vertexData[0 + index].color = highlightColor;
                vertexData[1 + index].color = highlightColor;
                vertexData[2 + index].color = highlightColor;
                vertexData[3 + index].color = highlightColor;
            }
            else
            {
                Color32[] colors = textInfo.meshInfo[m_CurrentMaterialIndex].colors32;
                colors[0 + index] = highlightColor;
                colors[1 + index] = highlightColor;
                colors[2 + index] = highlightColor;
                colors[3 + index] = highlightColor;
            }
            #endregion

            textInfo.meshInfo[m_CurrentMaterialIndex].vertexCount += k_VertexIncrease;
        }

        /// <summary>
        /// Function to clear the geometry of the Primary and Sub Text objects.
        /// </summary>
        static void ClearMesh(bool updateMesh, TextInfo textInfo)
        {
            textInfo.ClearMeshInfo(updateMesh);
        }
    }
}
