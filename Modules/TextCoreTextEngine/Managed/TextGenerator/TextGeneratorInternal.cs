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

            // Handle Vertex Colors
            {
                // Special handling for color glyphs
                vertexColor = isColorGlyph ? new Color32(255, 255, 255, vertexColor.a) : vertexColor;

                textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.color = vertexColor;
                textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.color = vertexColor;
                textInfo.textElementInfo[m_CharacterCount].vertexTopRight.color = vertexColor;
                textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.color = vertexColor;
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

            Vector2 uVBottomLeft;
            uVBottomLeft.x = (glyphRect.x - padding - stylePadding) / m_CurrentFontAsset.atlasWidth;
            uVBottomLeft.y = (glyphRect.y - padding - stylePadding) / m_CurrentFontAsset.atlasHeight;

            Vector2 uVTopLeft;
            uVTopLeft.x = uVBottomLeft.x;
            uVTopLeft.y = (glyphRect.y + padding + stylePadding + glyphRect.height) / m_CurrentFontAsset.atlasHeight;

            Vector2 uVTopRight;
            uVTopRight.x = (glyphRect.x + padding + stylePadding + glyphRect.width) / m_CurrentFontAsset.atlasWidth;
            uVTopRight.y = uVTopLeft.y;

            Vector2 uVBottomRight;
            uVBottomRight.x = uVTopRight.x;
            uVBottomRight.y = uVBottomLeft.y;

            // Store UV Information
            textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.uv = uVBottomLeft;
            textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.uv = uVTopLeft;
            textInfo.textElementInfo[m_CharacterCount].vertexTopRight.uv = uVTopRight;
            textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.uv = uVBottomRight;
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

            Color32 spriteColor = m_TintSprite ? ColorUtilities.MultiplyColors(m_SpriteColor, vertexColor) : m_SpriteColor;
            spriteColor.a = spriteColor.a < m_FontColor32.a ? spriteColor.a < vertexColor.a ? spriteColor.a : vertexColor.a : m_FontColor32.a;

            Color32 c0 = spriteColor;
            Color32 c1 = spriteColor;
            Color32 c2 = spriteColor;
            Color32 c3 = spriteColor;


            if (m_ColorGradientPreset != null)
            {
                c0 = m_TintSprite ? ColorUtilities.MultiplyColors(c0, m_ColorGradientPreset.bottomLeft) : c0;
                c1 = m_TintSprite ? ColorUtilities.MultiplyColors(c1, m_ColorGradientPreset.topLeft) : c1;
                c2 = m_TintSprite ? ColorUtilities.MultiplyColors(c2, m_ColorGradientPreset.topRight) : c2;
                c3 = m_TintSprite ? ColorUtilities.MultiplyColors(c3, m_ColorGradientPreset.bottomRight) : c3;
            }

            m_TintSprite = false;

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
        /// <param name="start">Start position in panel points. Includes padding that has been scaled with startScale.</param>
        /// <param name="end">Start position in panel points. Includes padding that has been scaled with endScale.</param>
        /// <param name="startScale">Scaling of the first character.</param>
        /// <param name="endScale">Scaling of the last character.</param>
        /// <param name="maxScale">Maximum scaling of any of the characters being underlined. Defines the thickness of the underline.</param>
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

            float underlineThickness = m_Underline.fontAsset.faceInfo.underlineThickness;

            // The underline is built from three parts. The left part goes from the left of the character minus padding
            // to the center of the character. The right part does something similar, going from the center of the
            // character to the right of the character plus padding. The middle part fills the gap between the two, but
            // samples only the center of the underline character.

            // Modify start.x and end.x to make it look like it was computed for maxScale for both start and end.
            start.x += (startScale - maxScale) * m_Padding;
            end.x += (maxScale - endScale) * m_Padding;

            float segmentWidth = (underlineGlyphMetrics.width * 0.5f + m_Padding) * maxScale;
            float segmentRatio = 1; // How much of the normal segment is really being used

            // When we underline something very that is narrower than the segment width, we keep the padding, but sample
            // less of the underline character. In this situation, we create a discontinuity in the underline. The
            // middle part quad is collapsed but the left and right quads sample the left and right tips of the
            // underline character
            float segmentWidthSum = 2 * segmentWidth;
            float meshWidth = end.x - start.x;
            if (meshWidth < segmentWidthSum)
            {
                segmentRatio = meshWidth / segmentWidthSum;
                segmentWidth *= segmentRatio;
            }

            // UNDERLINE VERTICES FOR (3) LINE SEGMENTS
            #region UNDERLINE VERTICES
            var vertexData = textInfo.meshInfo[m_CurrentMaterialIndex].vertexData;

            float posLeft = start.x;
            float posMidLeft = start.x + segmentWidth;
            float posMidRight = end.x - segmentWidth;
            float posRight = end.x;

            float posBottom = start.y - (underlineThickness + m_Padding) * maxScale;
            float posTop = start.y + m_Padding * maxScale;

            {
                // Left part of the underline
                vertexData[index + 0].position = new Vector3(posLeft, posBottom);    // BL
                vertexData[index + 1].position = new Vector3(posLeft, posTop);       // TL
                vertexData[index + 2].position = new Vector3(posMidLeft, posTop);    // TR
                vertexData[index + 3].position = new Vector3(posMidLeft, posBottom); // BR

                // Middle part of the underline
                vertexData[index + 4].position = new Vector3(posMidLeft, posBottom);  // BL
                vertexData[index + 5].position = new Vector3(posMidLeft, posTop);     // TL
                vertexData[index + 6].position = new Vector3(posMidRight, posTop);    // TR
                vertexData[index + 7].position = new Vector3(posMidRight, posBottom); // BR

                // Right part of the underline
                vertexData[index + 8].position = new Vector3(posMidRight, posBottom); // BL
                vertexData[index + 9].position = new Vector3(posMidRight, posTop);    // TL
                vertexData[index + 10].position = new Vector3(posRight, posTop);      // TR
                vertexData[index + 11].position = new Vector3(posRight, posBottom);   // BR
            }
            #endregion

            // Handle axis inversion.
            {
                Vector3 axisOffset;
                axisOffset.x = 0;
                axisOffset.y = generationSettings.screenRect.height;
                axisOffset.z = 0;

                {
                    for (int i = 0; i < 12; i++)
                    {
                        //vertices[index + i].x += axisOffset.x;
                        textInfo.meshInfo[m_CurrentMaterialIndex].vertexData[index + i].position.y = textInfo.meshInfo[m_CurrentMaterialIndex].vertexData[index + i].position.y * -1 + axisOffset.y;
                    }
                }
            }

            // UNDERLINE UV0
            #region HANDLE UV0

            float invAtlasWidth = 1f / m_Underline.fontAsset.atlasWidth;
            float invAtlasHeight = 1f / m_Underline.fontAsset.atlasHeight;

            // Calculate UV required to setup the 3 Quads for the Underline.
            float uvSegmentWidth = (underlineGlyphRect.width * 0.5f + m_Padding) * segmentRatio * invAtlasWidth;

            float uvLeft0 = (underlineGlyphRect.x - m_Padding) * invAtlasWidth;
            float uvLeft1 = uvLeft0 + uvSegmentWidth;
            float uvMid = (underlineGlyphRect.x + underlineGlyphRect.width * 0.5f) * invAtlasWidth;
            float uvRight1 = (underlineGlyphRect.x + underlineGlyphRect.width + m_Padding) * invAtlasWidth;
            float uvRight0 = uvRight1 - uvSegmentWidth;

            float uvBottom = (underlineGlyphRect.y - m_Padding) * invAtlasHeight; // Why not use maxScale?
            float uvTop = (underlineGlyphRect.y + underlineGlyphRect.height + m_Padding) * invAtlasHeight;

            {
                // Left part of the underline
                vertexData[0 + index].uv0 = new Vector4(uvLeft0, uvBottom); // BL
                vertexData[1 + index].uv0 = new Vector4(uvLeft0, uvTop);    // TL
                vertexData[2 + index].uv0 = new Vector4(uvLeft1, uvTop);    // TR
                vertexData[3 + index].uv0 = new Vector4(uvLeft1, uvBottom); // BR

                // Middle part of the underline
                vertexData[4 + index].uv0 = new Vector4(uvMid, uvBottom); // BL
                vertexData[5 + index].uv0 = new Vector4(uvMid, uvTop);    // TL
                vertexData[6 + index].uv0 = new Vector4(uvMid, uvTop);    // TR
                vertexData[7 + index].uv0 = new Vector4(uvMid, uvBottom); // BR

                // Right part of the underline
                vertexData[8 + index].uv0 = new Vector4(uvRight0, uvBottom);  // BL
                vertexData[9 + index].uv0 = new Vector4(uvRight0, uvTop);     // TL
                vertexData[10 + index].uv0 = new Vector4(uvRight1, uvTop);    // TR
                vertexData[11 + index].uv0 = new Vector4(uvRight1, uvBottom); // BR
            }

            #endregion

            // UNDERLINE UV2
            #region HANDLE UV2 - SDF SCALE
            // UV1 contains Face / Border UV layout.
            float min_UvX = 0;

            float invMeshWidth = 1f / meshWidth;

            {
                float max_UvX = (vertexData[index + 2].position.x - start.x) * invMeshWidth;

                vertexData[0 + index].uv2 = new Vector2(0, 0);
                vertexData[1 + index].uv2 = new Vector2(0, 1);
                vertexData[2 + index].uv2 = new Vector2(max_UvX, 1);
                vertexData[3 + index].uv2 = new Vector2(max_UvX, 0);

                min_UvX = (vertexData[index + 4].position.x - start.x) * invMeshWidth;
                max_UvX = (vertexData[index + 6].position.x - start.x) * invMeshWidth;

                vertexData[4 + index].uv2 = new Vector2(min_UvX, 0);
                vertexData[5 + index].uv2 = new Vector2(min_UvX, 1);
                vertexData[6 + index].uv2 = new Vector2(max_UvX, 1);
                vertexData[7 + index].uv2 = new Vector2(max_UvX, 0);

                min_UvX = (vertexData[index + 8].position.x - start.x) * invMeshWidth;

                vertexData[8 + index].uv2 = new Vector2(min_UvX, 0);
                vertexData[9 + index].uv2 = new Vector2(min_UvX, 1);
                vertexData[10 + index].uv2 = new Vector2(1, 1);
                vertexData[11 + index].uv2 = new Vector2(1, 0);
            }
          
            #endregion

            // UNDERLINE VERTEX COLORS
            #region UNDERLINE VERTEX COLORS
            // Alpha is the lower of the vertex color or tag color alpha used.
            underlineColor.a = m_FontColor32.a < underlineColor.a ? m_FontColor32.a : underlineColor.a;

            {
                for (int i = 0; i < 12; i++)
                {
                    vertexData[i + index].color = underlineColor;
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

            {
                vertexData[index + 0].position = start; // BL
                vertexData[index + 1].position = new Vector3(start.x, end.y, 0); // TL
                vertexData[index + 2].position = end; // TR
                vertexData[index + 3].position = new Vector3(end.x, start.y, 0); // BR
            }

            #endregion

            {
                Vector3 axisOffset;
                axisOffset.x = 0;
                axisOffset.y = generationSettings.screenRect.height;
                axisOffset.z = 0;

                {
                    for (int i = 0; i < 4; i++)
                    {
                        //vertices[index + 0].x += axisOffset.x;
                        vertexData[index + i].position.y = vertexData[index + i].position.y * -1 + axisOffset.y;
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

            {
                // UVs for the Quad
                vertexData[0 + index].uv0 = uvGlyphCenter - uvTexelSize; // BL
                vertexData[1 + index].uv0 = uvGlyphCenter + new Vector2(-uvTexelSize.x, uvTexelSize.y); // TL
                vertexData[2 + index].uv0 = uvGlyphCenter + uvTexelSize; // TR
                vertexData[3 + index].uv0 = uvGlyphCenter + new Vector2(uvTexelSize.x, -uvTexelSize.y); // BR
            }

            #endregion

            // HIGHLIGHT UV2
            #region HANDLE UV2 - SDF SCALE

            Vector2 customUV = new Vector2(0, 1);
            {
                // HIGHLIGHT UV2
                vertexData[0 + index].uv2 = customUV;
                vertexData[1 + index].uv2 = customUV;
                vertexData[2 + index].uv2 = customUV;
                vertexData[3 + index].uv2 = customUV;
            }

            #endregion

            // HIGHLIGHT VERTEX COLORS
            #region HIGHLIGHT VERTEX COLORS
            // Alpha is the lower of the vertex color or tag color alpha used.
            highlightColor.a = m_FontColor32.a < highlightColor.a ? m_FontColor32.a : highlightColor.a;

            {
                vertexData[0 + index].color = highlightColor;
                vertexData[1 + index].color = highlightColor;
                vertexData[2 + index].color = highlightColor;
                vertexData[3 + index].color = highlightColor;
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
