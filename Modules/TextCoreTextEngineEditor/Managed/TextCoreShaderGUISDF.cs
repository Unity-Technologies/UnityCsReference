// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.TextCore.Text;


namespace UnityEditor.TextCore.Text
{
    public class TextCoreShaderGUISDF : TextCoreShaderGUI
    {
        static ShaderFeature s_OutlineFeature, s_UnderlayFeature, s_BevelFeature, s_GlowFeature, s_MaskFeature;

        static bool s_Face = true, s_Outline = true, s_Outline2 = true, s_Outline3 = true, s_Underlay = true, s_Lighting = true, s_Glow, s_Bevel, s_Light, s_Bump, s_Env;

        static string[]
            s_FaceUVSpeedName = { "_FaceUVSpeed" },
            s_FaceUvSpeedNames = { "_FaceUVSpeedX", "_FaceUVSpeedY" },
            s_OutlineUvSpeedNames = { "_OutlineUVSpeedX", "_OutlineUVSpeedY" },
            s_OutlineUvSpeedName = { "_OutlineUVSpeed" };

        static TextCoreShaderGUISDF()
        {
            s_OutlineFeature = new ShaderFeature()
            {
                undoLabel = "Outline",
                keywords = new[] { "OUTLINE_ON" }
            };

            s_UnderlayFeature = new ShaderFeature()
            {
                undoLabel = "Underlay",
                keywords = new[] { "UNDERLAY_ON", "UNDERLAY_INNER" },
                label = new GUIContent("Underlay Type"),
                keywordLabels = new[]
                {
                    new GUIContent("None"), new GUIContent("Normal"), new GUIContent("Inner")
                }
            };

            s_BevelFeature = new ShaderFeature()
            {
                undoLabel = "Bevel",
                keywords = new[] { "BEVEL_ON" }
            };

            s_GlowFeature = new ShaderFeature()
            {
                undoLabel = "Glow",
                keywords = new[] { "GLOW_ON" }
            };

            s_MaskFeature = new ShaderFeature()
            {
                undoLabel = "Mask",
                keywords = new[] { "MASK_HARD", "MASK_SOFT" },
                label = new GUIContent("Mask"),
                keywordLabels = new[]
                {
                    new GUIContent("Mask Off"), new GUIContent("Mask Hard"), new GUIContent("Mask Soft")
                }
            };
        }

        protected override void DoGUI()
        {
            bool isSRPMaterial = m_Material.HasProperty(TextShaderUtilities.ID_IsoPerimeter);

            s_Face = BeginPanel("Face", s_Face);
            if (s_Face)
            {
                DoFacePanel();
            }

            EndPanel();

            // Outline panels
            if (isSRPMaterial)
            {
                DoOutlinePanels();
            }
            else
            {
                s_Outline = m_Material.HasProperty(TextShaderUtilities.ID_OutlineTex) ? BeginPanel("Outline", s_Outline) : BeginPanel("Outline", s_OutlineFeature, s_Outline);
                if (s_Outline)
                {
                    DoOutlinePanel();
                }

                EndPanel();

                if (m_Material.HasProperty(TextShaderUtilities.ID_Outline2Color))
                {
                    s_Outline2 = BeginPanel("Outline 2", s_OutlineFeature, s_Outline2);
                    if (s_Outline2)
                    {
                        DoOutline2Panel();
                    }

                    EndPanel();
                }
            }

            // Underlay panel
            if (m_Material.HasProperty(TextShaderUtilities.ID_UnderlayColor))
            {
                if (isSRPMaterial)
                {
                    s_Underlay = BeginPanel("Underlay", s_Underlay);
                    if (s_Underlay)
                    {
                        DoUnderlayPanel();
                    }

                    EndPanel();
                }
                else
                {
                    s_Underlay = BeginPanel("Underlay", s_UnderlayFeature, s_Underlay);
                    if (s_Underlay)
                    {
                        DoUnderlayPanel();
                    }

                    EndPanel();
                }
            }

            // Lighting panel
            if (m_Material.HasProperty("_SpecularColor"))
            {
                if (isSRPMaterial)
                    DrawLightingPanelSRP();
                else
                    DrawLightingPanelLegacy();
            }


            else if (m_Material.HasProperty("_SpecColor"))
            {
                s_Bevel = BeginPanel("Bevel", s_Bevel);
                if (s_Bevel)
                {
                    DoBevelPanel();
                }

                EndPanel();

                s_Light = BeginPanel("Surface Lighting", s_Light);
                if (s_Light)
                {
                    DoSurfaceLightingPanel();
                }

                EndPanel();

                s_Bump = BeginPanel("Bump Map", s_Bump);
                if (s_Bump)
                {
                    DoBumpMapPanel();
                }

                EndPanel();

                s_Env = BeginPanel("Environment Map", s_Env);
                if (s_Env)
                {
                    DoEnvMapPanel();
                }

                EndPanel();
            }


            if (m_Material.HasProperty(TextShaderUtilities.ID_GlowColor))
            {
                s_Glow = BeginPanel("Glow", s_GlowFeature, s_Glow);
                if (s_Glow)
                {
                    DoGlowPanel();
                }

                EndPanel();
            }


            s_DebugExtended = BeginPanel("Debug Settings", s_DebugExtended);
            if (s_DebugExtended)
            {
                if (isSRPMaterial)
                    DoDebugPanelSRP();
                else
                    DoDebugPanel();
            }
            EndPanel();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if (isSRPMaterial)
            {
                m_Editor.RenderQueueField();
                m_Editor.EnableInstancingField();
                m_Editor.DoubleSidedGIField();
                m_Editor.EmissionEnabledProperty();
            }
        }

        private void DrawLightingPanelSRP()
        {
            s_Lighting = BeginPanel("Lighting", s_Lighting);
            if (s_Lighting)
            {
                s_Bevel = BeginPanel("Bevel", s_Bevel);
                if (s_Bevel)
                {
                    DoBevelPanelSRP();
                }
                EndPanel();

                s_Light = BeginPanel("Local Lighting", s_Light);
                if (s_Light)
                {
                    DoLocalLightingPanel();
                }
                EndPanel();
            }

            EndPanel();
        }

        private void DrawLightingPanelLegacy()
        {
            s_Lighting = BeginPanel("Lighting", s_BevelFeature, s_Lighting);
            if (s_Lighting)
            {
                s_Bevel = BeginPanel("Bevel", s_Bevel);
                if (s_Bevel)
                {
                    DoBevelPanel();
                }

                EndPanel();

                s_Light = BeginPanel("Local Lighting", s_Light);
                if (s_Light)
                {
                    DoLocalLightingPanel();
                }

                EndPanel();

                s_Bump = BeginPanel("Bump Map", s_Bump);
                if (s_Bump)
                {
                    DoBumpMapPanel();
                }

                EndPanel();

                s_Env = BeginPanel("Environment Map", s_Env);
                if (s_Env)
                {
                    DoEnvMapPanel();
                }

                EndPanel();
            }

            EndPanel();
        }

        void DoFacePanel()
        {
            EditorGUI.indentLevel += 1;

            DoColor("_FaceColor", "Color");

            if (m_Material.HasProperty(TextShaderUtilities.ID_FaceTex))
            {
                if (m_Material.HasProperty("_FaceUVSpeedX"))
                {
                    DoTexture2D("_FaceTex", "Texture", true, s_FaceUvSpeedNames);
                }
                else if (m_Material.HasProperty("_FaceUVSpeed"))
                {
                    DoTexture2D("_FaceTex", "Texture", true, s_FaceUVSpeedName);
                }
                else
                {
                    DoTexture2D("_FaceTex", "Texture", true);
                }
            }

            if (m_Material.HasProperty("_Softness"))
            {
                DoSlider("_Softness", "X", new Vector2(0, 1), "Softness");
            }

            if (m_Material.HasProperty("_OutlineSoftness"))
            {
                DoSlider("_OutlineSoftness", "Softness");
            }

            if (m_Material.HasProperty(TextShaderUtilities.ID_FaceDilate))
            {
                DoSlider("_FaceDilate", "Dilate");
                if (m_Material.HasProperty(TextShaderUtilities.ID_Shininess))
                {
                    DoSlider("_FaceShininess", "Gloss");
                }
            }

            if (m_Material.HasProperty(TextShaderUtilities.ID_IsoPerimeter))
            {
                DoSlider("_IsoPerimeter", "X", new Vector2(-1, 1), "Dilate");
            }

            EditorGUI.indentLevel -= 1;
            EditorGUILayout.Space();
        }

        void DoOutlinePanel()
        {
            EditorGUI.indentLevel += 1;
            DoColor("_OutlineColor", "Color");
            if (m_Material.HasProperty(TextShaderUtilities.ID_OutlineTex))
            {
                if (m_Material.HasProperty("_OutlineUVSpeedX"))
                {
                    DoTexture2D("_OutlineTex", "Texture", true, s_OutlineUvSpeedNames);
                }
                else if (m_Material.HasProperty("_OutlineUVSpeed"))
                {
                    DoTexture2D("_OutlineTex", "Texture", true, s_OutlineUvSpeedName);
                }
                else
                {
                    DoTexture2D("_OutlineTex", "Texture", true);
                }
            }

            DoSlider("_OutlineWidth", "Thickness");
            if (m_Material.HasProperty("_OutlineShininess"))
            {
                DoSlider("_OutlineShininess", "Gloss");
            }

            EditorGUI.indentLevel -= 1;
            EditorGUILayout.Space();
        }

        void DoOutlinePanel(int outlineID, string propertyField, string label)
        {
            EditorGUI.indentLevel += 1;
            DoColor("_OutlineColor" + outlineID, label);

            if (outlineID != 3)
                DoOffset("_OutlineOffset" + outlineID, "Offset");
            else
            {
                if (m_Material.GetFloat(TextShaderUtilities.ID_OutlineMode) == 0)
                    DoOffset("_OutlineOffset" + outlineID, "Offset");
            }

            DoSlider("_Softness", propertyField, new Vector2(0, 1), "Softness");
            DoSlider("_IsoPerimeter", propertyField, new Vector2(-1, 1), "Dilate");

            if (outlineID == 3)
            {
                DoToggle("_OutlineMode", "Outline Mode");
            }

            if (m_Material.HasProperty("_OutlineShininess"))
            {
                //DoSlider("_OutlineShininess", "Gloss");
            }

            EditorGUI.indentLevel -= 1;
            EditorGUILayout.Space();
        }

        void DoOutlinePanelWithTexture(int outlineID, string propertyField, string label)
        {
            EditorGUI.indentLevel += 1;
            DoColor("_OutlineColor" + outlineID, label);
            if (m_Material.HasProperty(TextShaderUtilities.ID_OutlineTex))
            {
                if (m_Material.HasProperty("_OutlineUVSpeedX"))
                {
                    DoTexture2D("_OutlineTex", "Texture", true, s_OutlineUvSpeedNames);
                }
                else if (m_Material.HasProperty("_OutlineUVSpeed"))
                {
                    DoTexture2D("_OutlineTex", "Texture", true, s_OutlineUvSpeedName);
                }
                else
                {
                    DoTexture2D("_OutlineTex", "Texture", true);
                }
            }

            DoOffset("_OutlineOffset" + outlineID, "Offset");
            DoSlider("_Softness", propertyField, new Vector2(0, 1), "Softness");
            DoSlider("_IsoPerimeter", propertyField, new Vector2(-1, 1), "Dilate");

            if (m_Material.HasProperty("_OutlineShininess"))
            {
                //DoSlider("_OutlineShininess", "Gloss");
            }

            EditorGUI.indentLevel -= 1;
            EditorGUILayout.Space();
        }

        void DoOutline2Panel()
        {
            EditorGUI.indentLevel += 1;
            DoColor("_Outline2Color", "Color");
            //if (m_Material.HasProperty(TextShaderUtilities.ID_OutlineTex))
            //{
            //    if (m_Material.HasProperty("_OutlineUVSpeedX"))
            //    {
            //        DoTexture2D("_OutlineTex", "Texture", true, s_OutlineUvSpeedNames);
            //    }
            //    else
            //    {
            //        DoTexture2D("_OutlineTex", "Texture", true);
            //    }
            //}

            DoSlider("_Outline2Width", "Thickness");
            //if (m_Material.HasProperty("_OutlineShininess"))
            //{
            //    DoSlider("_OutlineShininess", "Gloss");
            //}

            EditorGUI.indentLevel -= 1;
            EditorGUILayout.Space();
        }

        void DoOutlinePanels()
        {
            s_Outline = BeginPanel("Outline 1", s_Outline);
            if (s_Outline)
                DoOutlinePanelWithTexture(1, "Y", "Color");

            EndPanel();

            s_Outline2 = BeginPanel("Outline 2", s_Outline2);
            if (s_Outline2)
                DoOutlinePanel(2, "Z", "Color");

            EndPanel();

            s_Outline3 = BeginPanel("Outline 3", s_Outline3);
            if (s_Outline3)
                DoOutlinePanel(3, "W", "Color");

            EndPanel();
        }

        void DoUnderlayPanel()
        {
            EditorGUI.indentLevel += 1;

            if (m_Material.HasProperty(TextShaderUtilities.ID_IsoPerimeter))
            {
                DoColor("_UnderlayColor", "Color");
                DoSlider("_UnderlayOffset", "X", new Vector2(-1, 1), "Offset X");
                DoSlider("_UnderlayOffset", "Y", new Vector2(-1, 1), "Offset Y");
                DoSlider("_UnderlayDilate", new Vector2(-1, 1), "Dilate");
                DoSlider("_UnderlaySoftness", new Vector2(0, 1), "Softness");
            }
            else
            {
                s_UnderlayFeature.DoPopup(m_Editor, m_Material);
                DoColor("_UnderlayColor", "Color");
                DoSlider("_UnderlayOffsetX", "Offset X");
                DoSlider("_UnderlayOffsetY", "Offset Y");
                DoSlider("_UnderlayDilate", "Dilate");
                DoSlider("_UnderlaySoftness", "Softness");
            }

            EditorGUI.indentLevel -= 1;
            EditorGUILayout.Space();
        }

        static GUIContent[] s_BevelTypeLabels =
        {
            new GUIContent("Outer Bevel"),
            new GUIContent("Inner Bevel")
        };

        void DoBevelPanel()
        {
            EditorGUI.indentLevel += 1;
            DoPopup("_ShaderFlags", "Type", s_BevelTypeLabels);
            DoSlider("_Bevel", "Amount");
            DoSlider("_BevelOffset", "Offset");
            DoSlider("_BevelWidth", "Width");
            DoSlider("_BevelRoundness", "Roundness");
            DoSlider("_BevelClamp", "Clamp");
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.Space();
        }

        void DoBevelPanelSRP()
        {
            EditorGUI.indentLevel += 1;
            DoPopup("_BevelType", "Type", s_BevelTypeLabels);
            DoSlider("_BevelAmount", "Amount");
            DoSlider("_BevelOffset", "Offset");
            DoSlider("_BevelWidth", "Width");
            DoSlider("_BevelRoundness", "Roundness");
            DoSlider("_BevelClamp", "Clamp");
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.Space();
        }

        void DoLocalLightingPanel()
        {
            EditorGUI.indentLevel += 1;
            DoSlider("_LightAngle", "Light Angle");
            DoColor("_SpecularColor", "Specular Color");
            DoSlider("_SpecularPower", "Specular Power");
            DoSlider("_Reflectivity", "Reflectivity Power");
            DoSlider("_Diffuse", "Diffuse Shadow");
            DoSlider("_Ambient", "Ambient Shadow");
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.Space();
        }

        void DoSurfaceLightingPanel()
        {
            EditorGUI.indentLevel += 1;
            DoColor("_SpecColor", "Specular Color");
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.Space();
        }

        void DoBumpMapPanel()
        {
            EditorGUI.indentLevel += 1;
            DoTexture2D("_BumpMap", "Texture");
            DoSlider("_BumpFace", "Face");
            DoSlider("_BumpOutline", "Outline");
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.Space();
        }

        void DoEnvMapPanel()
        {
            EditorGUI.indentLevel += 1;
            DoColor("_ReflectFaceColor", "Face Color");
            DoColor("_ReflectOutlineColor", "Outline Color");
            DoCubeMap("_Cube", "Texture");
            DoVector3("_EnvMatrixRotation", "Rotation");
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.Space();
        }

        void DoGlowPanel()
        {
            EditorGUI.indentLevel += 1;
            DoColor("_GlowColor", "Color");
            DoSlider("_GlowOffset", "Offset");
            DoSlider("_GlowInner", "Inner");
            DoSlider("_GlowOuter", "Outer");
            DoSlider("_GlowPower", "Power");
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.Space();
        }

        void DoDebugPanel()
        {
            EditorGUI.indentLevel += 1;
            DoTexture2D("_MainTex", "Font Atlas");
            DoFloat("_GradientScale", "Gradient Scale");
            DoFloat("_TextureWidth", "Texture Width");
            DoFloat("_TextureHeight", "Texture Height");
            EditorGUILayout.Space();
            DoFloat("_ScaleX", "Scale X");
            DoFloat("_ScaleY", "Scale Y");

            if (m_Material.HasProperty(TextShaderUtilities.ID_Sharpness))
                DoSlider("_Sharpness", "Sharpness");

            DoSlider("_PerspectiveFilter", "Perspective Filter");
            EditorGUILayout.Space();
            DoFloat("_VertexOffsetX", "Offset X");
            DoFloat("_VertexOffsetY", "Offset Y");

            if (m_Material.HasProperty(TextShaderUtilities.ID_MaskCoord))
            {
                EditorGUILayout.Space();
                s_MaskFeature.ReadState(m_Material);
                s_MaskFeature.DoPopup(m_Editor, m_Material);
                if (s_MaskFeature.Active)
                {
                    DoMaskSubgroup();
                }

                EditorGUILayout.Space();
                DoVector("_ClipRect", "Clip Rect", s_LbrtVectorLabels);
            }
            else if (m_Material.HasProperty("_MaskTex"))
            {
                DoMaskTexSubgroup();
            }
            else if (m_Material.HasProperty(TextShaderUtilities.ID_MaskSoftnessX))
            {
                EditorGUILayout.Space();
                DoFloat("_MaskSoftnessX", "Softness X");
                DoFloat("_MaskSoftnessY", "Softness Y");
                DoVector("_ClipRect", "Clip Rect", s_LbrtVectorLabels);
            }

            if (m_Material.HasProperty(TextShaderUtilities.ID_StencilID))
            {
                EditorGUILayout.Space();
                DoFloat("_Stencil", "Stencil ID");
                DoFloat("_StencilComp", "Stencil Comp");
            }

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            bool useRatios = EditorGUILayout.Toggle("Use Ratios", !m_Material.IsKeywordEnabled("RATIOS_OFF"));
            if (EditorGUI.EndChangeCheck())
            {
                m_Editor.RegisterPropertyChangeUndo("Use Ratios");
                if (useRatios)
                {
                    m_Material.DisableKeyword("RATIOS_OFF");
                }
                else
                {
                    m_Material.EnableKeyword("RATIOS_OFF");
                }
            }

            if (m_Material.HasProperty(TextShaderUtilities.ShaderTag_CullMode))
            {
                EditorGUILayout.Space();
                DoPopup("_CullMode", "Cull Mode", s_CullingTypeLabels);
            }

            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(true);
            DoFloat("_ScaleRatioA", "Scale Ratio A");
            DoFloat("_ScaleRatioB", "Scale Ratio B");
            DoFloat("_ScaleRatioC", "Scale Ratio C");
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.Space();
        }

        void DoDebugPanelSRP()
        {
            EditorGUI.indentLevel += 1;
            DoTexture2D("_MainTex", "Font Atlas");
            DoFloat("_GradientScale", "Gradient Scale");
            //DoFloat("_TextureWidth", "Texture Width");
            //DoFloat("_TextureHeight", "Texture Height");
            EditorGUILayout.Space();

            /*
            DoFloat("_ScaleX", "Scale X");
            DoFloat("_ScaleY", "Scale Y");

            if (m_Material.HasProperty(TextShaderUtilities.ID_Sharpness))
                DoSlider("_Sharpness", "Sharpness");

            DoSlider("_PerspectiveFilter", "Perspective Filter");
            EditorGUILayout.Space();
            DoFloat("_VertexOffsetX", "Offset X");
            DoFloat("_VertexOffsetY", "Offset Y");

            if (m_Material.HasProperty(TextShaderUtilities.ID_MaskCoord))
            {
                EditorGUILayout.Space();
                s_MaskFeature.ReadState(m_Material);
                s_MaskFeature.DoPopup(m_Editor, m_Material);
                if (s_MaskFeature.Active)
                {
                    DoMaskSubgroup();
                }

                EditorGUILayout.Space();
                DoVector("_ClipRect", "Clip Rect", s_LbrtVectorLabels);
            }
            else if (m_Material.HasProperty("_MaskTex"))
            {
                DoMaskTexSubgroup();
            }
            else if (m_Material.HasProperty(TextShaderUtilities.ID_MaskSoftnessX))
            {
                EditorGUILayout.Space();
                DoFloat("_MaskSoftnessX", "Softness X");
                DoFloat("_MaskSoftnessY", "Softness Y");
                DoVector("_ClipRect", "Clip Rect", s_LbrtVectorLabels);
            }

            if (m_Material.HasProperty(TextShaderUtilities.ID_StencilID))
            {
                EditorGUILayout.Space();
                DoFloat("_Stencil", "Stencil ID");
                DoFloat("_StencilComp", "Stencil Comp");
            }

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            bool useRatios = EditorGUILayout.Toggle("Use Ratios", !m_Material.IsKeywordEnabled("RATIOS_OFF"));
            if (EditorGUI.EndChangeCheck())
            {
                m_Editor.RegisterPropertyChangeUndo("Use Ratios");
                if (useRatios)
                {
                    m_Material.DisableKeyword("RATIOS_OFF");
                }
                else
                {
                    m_Material.EnableKeyword("RATIOS_OFF");
                }
            }
            */
            if (m_Material.HasProperty(TextShaderUtilities.ShaderTag_CullMode))
            {
                EditorGUILayout.Space();
                DoPopup("_CullMode", "Cull Mode", s_CullingTypeLabels);
            }

            EditorGUILayout.Space();
            /*
            EditorGUI.BeginDisabledGroup(true);
            DoFloat("_ScaleRatioA", "Scale Ratio A");
            DoFloat("_ScaleRatioB", "Scale Ratio B");
            DoFloat("_ScaleRatioC", "Scale Ratio C");
            EditorGUI.EndDisabledGroup();
            */

            EditorGUI.indentLevel -= 1;
            EditorGUILayout.Space();
        }

        void DoMaskSubgroup()
        {
            DoVector("_MaskCoord", "Mask Bounds", s_XywhVectorLabels);
            if (Selection.activeGameObject != null)
            {
                Renderer renderer = Selection.activeGameObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Rect rect = EditorGUILayout.GetControlRect();
                    rect.x += EditorGUIUtility.labelWidth;
                    rect.width -= EditorGUIUtility.labelWidth;
                    if (GUI.Button(rect, "Match Renderer Bounds"))
                    {
                        FindProperty("_MaskCoord", m_Properties).vectorValue = new Vector4(
                            0,
                            0,
                            Mathf.Round(renderer.bounds.extents.x * 1000) / 1000,
                            Mathf.Round(renderer.bounds.extents.y * 1000) / 1000
                        );
                    }
                }
            }

            if (s_MaskFeature.State == 1)
            {
                DoFloat("_MaskSoftnessX", "Softness X");
                DoFloat("_MaskSoftnessY", "Softness Y");
            }
        }

        void DoMaskTexSubgroup()
        {
            EditorGUILayout.Space();
            DoTexture2D("_MaskTex", "Mask Texture");
            DoToggle("_MaskInverse", "Inverse Mask");
            DoColor("_MaskEdgeColor", "Edge Color");
            DoSlider("_MaskEdgeSoftness", "Edge Softness");
            DoSlider("_MaskWipeControl", "Wipe Position");
            DoFloat("_MaskSoftnessX", "Softness X");
            DoFloat("_MaskSoftnessY", "Softness Y");
            DoVector("_ClipRect", "Clip Rect", s_LbrtVectorLabels);
        }
    }
}
