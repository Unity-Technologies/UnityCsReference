// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.Collections;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine.U2D;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEditor.AssetImporters;

namespace Unity.VectorGraphics.Editor
{
    [CustomEditor(typeof(SVGImporter))]
    [CanEditMultipleObjects]
    internal class SVGImporterEditor : ScriptedImporterEditor
    {
        private SerializedProperty m_SVGType;
        private SerializedProperty m_TexturedSpriteMeshType;
        private SerializedProperty m_PixelsPerUnit;
        private SerializedProperty m_GradientResolution;
        private SerializedProperty m_Alignment;
        private SerializedProperty m_CustomPivot;
        private SerializedProperty m_GeneratePhysicsShape;
        private SerializedProperty m_ViewportOptions;
        private SerializedProperty m_AdvancedMode;
        private SerializedProperty m_TessellationMode;
        private SerializedProperty m_StepDistance;
        private SerializedProperty m_SamplingStepDistance;
        private SerializedProperty m_PredefinedResolutionIndex;
        private SerializedProperty m_TargetResolution;
        private SerializedProperty m_ResolutionMultiplier;
        private SerializedProperty m_MaxCordDeviationEnabled;
        private SerializedProperty m_MaxCordDeviation;
        private SerializedProperty m_MaxTangentAngleEnabled;
        private SerializedProperty m_MaxTangentAngle;
        private SerializedProperty m_KeepTextureAspectRatio;
        private SerializedProperty m_TextureSize;
        private SerializedProperty m_TextureWidth;
        private SerializedProperty m_TextureHeight;
        private SerializedProperty m_WrapMode;
        private SerializedProperty m_FilterMode;
        private SerializedProperty m_SampleCount;
        private SerializedProperty m_PreserveSVGImageAspect;
        private SerializedProperty m_UseSVGPixelsPerUnit;

        private readonly GUIContent m_SVGTypeText = new GUIContent("Generated Asset Type", "How the SVG file will be imported.");
        private readonly GUIContent m_TexturedSpriteMeshTypeText = new GUIContent("Mesh Type", "Type of the sprite mesh to generate.");
        private readonly GUIContent m_PixelsPerUnitText = new GUIContent("Pixels Per Unit", "How many pixels in the SVG correspond to one unit in the world.");
        private readonly GUIContent m_GradientResolutionText = new GUIContent("Gradient Resolution", "Size of each rasterized gradient in pixels. Higher values consume memory but result in more accurate gradients.");
        private readonly GUIContent m_AlignmentText = new GUIContent("Pivot", "Sprite pivot point in its local space.");
        private readonly GUIContent m_CustomPivotText = new GUIContent("Custom Pivot");
        private readonly GUIContent m_GeneratePhysicsShapeText = new GUIContent("Generate Physics Shape");
        private readonly GUIContent m_ViewportOptionsText = new GUIContent("Viewport Options", "Viewport options to use while importing the SVG document");
        private readonly GUIContent m_TessellationModeText = new GUIContent("Tessellation Mode");
        private readonly GUIContent m_SettingsText = new GUIContent("Tessellation Settings");
        private readonly GUIContent m_TargetResolutionText = new GUIContent("Target Resolution", "Target resolution below which the sprite will not look tessellated.");
        private readonly GUIContent m_CustomTargetResolutionText = new GUIContent("Custom Target Resolution");
        private readonly GUIContent m_ResolutionMultiplierText = new GUIContent("Zoom Factor", "Target zoom factor for which the SVG asset should not look tessellated.");
        private readonly GUIContent m_StepDistanceText = new GUIContent("Step Distance", "Distance at which vertices will be generated along the paths. Lower values will result in a more dense tessellation.");
        private readonly GUIContent m_SamplingStepDistanceText = new GUIContent("Sampling Steps", "Number of samples evaluated on paths. Higher values give more accurate results (but takes longer).");
        private readonly GUIContent m_MaxCordDeviationEnabledText = new GUIContent("Max Cord Enabled", "Enables the \"max cord deviation\" tessellation test.");
        private readonly GUIContent m_MaxCordDeviationText = new GUIContent("Max Cord Deviation", "Distance on the cord to a straight line between two points after which more tessellation will be generated.");
        private readonly GUIContent m_MaxTangentAngleEnabledText = new GUIContent("Max Tangent Enabled", "Enables the \"max tangent angle\" tessellation test.");
        private readonly GUIContent m_MaxTangentAngleText = new GUIContent("Max Tangent Angle", "Max tangent angle (in degrees) after which more tessellation will be generated.");
        private readonly GUIContent m_KeepTextureAspectRatioText = new GUIContent("Keep Aspect Ratio");
        private readonly GUIContent m_TextureSizeText = new GUIContent("Texture Size", "The size of the generated texture.");
        private readonly GUIContent m_WrapModeText = new GUIContent("Wrap Mode");
        private readonly GUIContent m_FilterModeText = new GUIContent("Filter Mode");
        private readonly GUIContent m_SampleCountText = new GUIContent("Sample Count");
        private readonly GUIContent m_PreserveSVGImageAspectText = new GUIContent("Preserve Aspect");
        private readonly GUIContent m_UseSVGPixelsPerUnitText = new GUIContent("Use SVG Pixels Per Unit", "When set, the \"Pixels Per Unit\" value will be applied relative to the SVG asset size instead of the texture size.");

        private readonly GUIContent[] svgTypeOptionsFull =
        {
            new GUIContent("Vector Sprite", "A tessellated sprite with \"infinite\" resolution."),
            new GUIContent("Textured Sprite", "A textured sprite."),
            new GUIContent("UI SVGImage", "A tessellated sprite with \"infinite\" resolution, compatible with the UI canvas masking system."),
            new GUIContent("UI Toolkit Vector Image", "A vector image that can be used by UI Toolkit."),
            new GUIContent("Texture2D", "A normal texture."),
        };

        private readonly int[] svgTypeValuesFull =
        {
            (int)SVGType.VectorSprite,
            (int)SVGType.TexturedSprite,
            (int)SVGType.UISVGImage,
            (int)SVGType.VectorImage,
            (int)SVGType.Texture2D,
        };

        private readonly GUIContent[] svgTypeOptionsModuleOnly =
{
            new GUIContent("UI Toolkit Vector Image", "A vector image that can be used by UI Toolkit."),
            new GUIContent("Texture2D", "A normal texture."),
        };

        private readonly int[] svgTypeValuesModuleOnly =
        {
            (int)SVGType.VectorImage,
            (int)SVGType.Texture2D,
        };

        private readonly GUIContent[] viewportOptions =
        {
            new GUIContent("Don't Preserve Viewport", "Don't preserve the viewport defined in the SVG document."),
            new GUIContent("Preserve Viewport", "Preserves the viewport defined in the SVG document."),
            new GUIContent("Only Apply Root ViewBox", "Applies the root view-box defined in the SVG document (if any).")
        };

        private readonly int[] viewportOptionsValues =
        {
            (int)ViewportOptions.DontPreserve,
            (int)ViewportOptions.PreserveViewport,
            (int)ViewportOptions.OnlyApplyRootViewBox
        };

        private readonly GUIContent[] tessellationModeOptions =
        {
            new GUIContent("Basic Triangulation", "Basic triangulation of the SVG paths."),
            new GUIContent("Antialiased Arc Encoding", "Antialiased arc encoding of the SVG paths, which results in truly infinite curves."),
        };

        private readonly GUIContent[] texturedSpriteMeshTypeOptions =
        {
            new GUIContent("Full Rect"),
            new GUIContent("Tight")
        };

        private readonly int[] texturedSpriteMeshTypeValues =
        {
            (int)SpriteMeshType.FullRect,
            (int)SpriteMeshType.Tight
        };

        private readonly GUIContent[] m_AlignmentOptions = new GUIContent[]
        {
            new GUIContent("Center"),
            new GUIContent("Top Left"),
            new GUIContent("Top Center"),
            new GUIContent("Top Right"),
            new GUIContent("Left Center"),
            new GUIContent("Right Center"),
            new GUIContent("Bottom Left"),
            new GUIContent("Bottom Center"),
            new GUIContent("Bottom Right"),
            new GUIContent("Custom"),
            new GUIContent("SVG Origin")
        };

        private readonly GUIContent[] m_SettingOptions = new GUIContent[]
        {
            new GUIContent("Basic"),
            new GUIContent("Advanced")
        };

        private readonly GUIContent[] m_TargetResolutionOptions = new GUIContent[]
        {
            new GUIContent("2160p (4K)"),
            new GUIContent("1080p"),
            new GUIContent("720p"),
            new GUIContent("480p"),
            new GUIContent("Custom")
        };

        public readonly GUIContent[] m_WrapModeContents =
        {
            new GUIContent("Repeat"),
            new GUIContent("Clamp"),
            new GUIContent("Mirror"),
            new GUIContent("Mirror Once")
        };

        public readonly int[] m_WrapModeValues =
        {
            (int)TextureWrapMode.Repeat,
            (int)TextureWrapMode.Clamp,
            (int)TextureWrapMode.Mirror,
            (int)TextureWrapMode.MirrorOnce
        };

        public readonly GUIContent[] m_FilterModeContents =
        {
            new GUIContent("Point"),
            new GUIContent("Bilinear"),
            new GUIContent("Trilinear")
        };

        public readonly int[] m_FilterModeValues =
        {
            (int)FilterMode.Point,
            (int)FilterMode.Bilinear,
            (int)FilterMode.Trilinear
        };

        public readonly GUIContent[] m_SampleCountContents =
        {
            new GUIContent("None"),
            new GUIContent("2 samples"),
            new GUIContent("4 samples"),
            new GUIContent("8 samples")
        };

        public readonly int[] m_SampleCountValues =
        {
            1,
            2,
            4,
            8
        };

        public override void OnEnable()
        {
            base.OnEnable();

            m_SVGType = serializedObject.FindProperty("m_SvgType");
            m_TexturedSpriteMeshType = serializedObject.FindProperty("m_TexturedSpriteMeshType");
            m_PixelsPerUnit = serializedObject.FindProperty("m_SvgPixelsPerUnit");
            m_GradientResolution = serializedObject.FindProperty("m_GradientResolution");
            m_Alignment = serializedObject.FindProperty("m_Alignment");
            m_CustomPivot = serializedObject.FindProperty("m_CustomPivot");
            m_GeneratePhysicsShape = serializedObject.FindProperty("m_GeneratePhysicsShape");
            m_ViewportOptions = serializedObject.FindProperty("m_ViewportOptions");
            m_AdvancedMode = serializedObject.FindProperty("m_AdvancedMode");
            m_TessellationMode = serializedObject.FindProperty("m_TessellationMode");
            m_PredefinedResolutionIndex = serializedObject.FindProperty("m_PredefinedResolutionIndex");
            m_TargetResolution = serializedObject.FindProperty("m_TargetResolution");
            m_ResolutionMultiplier = serializedObject.FindProperty("m_ResolutionMultiplier");
            m_StepDistance = serializedObject.FindProperty("m_StepDistance");
            m_SamplingStepDistance = serializedObject.FindProperty("m_SamplingStepDistance");
            m_MaxCordDeviationEnabled = serializedObject.FindProperty("m_MaxCordDeviationEnabled");
            m_MaxCordDeviation = serializedObject.FindProperty("m_MaxCordDeviation");
            m_MaxTangentAngleEnabled = serializedObject.FindProperty("m_MaxTangentAngleEnabled");
            m_MaxTangentAngle = serializedObject.FindProperty("m_MaxTangentAngle");
            m_KeepTextureAspectRatio = serializedObject.FindProperty("m_KeepTextureAspectRatio");
            m_TextureSize = serializedObject.FindProperty("m_TextureSize");
            m_TextureWidth = serializedObject.FindProperty("m_TextureWidth");
            m_TextureHeight = serializedObject.FindProperty("m_TextureHeight");
            m_WrapMode = serializedObject.FindProperty("m_WrapMode");
            m_FilterMode = serializedObject.FindProperty("m_FilterMode");
            m_SampleCount = serializedObject.FindProperty("m_SampleCount");
            m_PreserveSVGImageAspect = serializedObject.FindProperty("m_PreserveSVGImageAspect");
            m_UseSVGPixelsPerUnit = serializedObject.FindProperty("m_UseSVGPixelsPerUnit");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeMigration", "UA1002")]
        public override void OnInspectorGUI()
        {
            if (!SVGImporter.isVectorGraphicsPackageInstalled &&
                m_SVGType.intValue != (int)SVGType.Texture2D &&
                m_SVGType.intValue != (int)SVGType.VectorImage)
            {
                EditorGUILayout.HelpBox("The Vector Graphics package (com.unity.vectorgraphics) is required for this asset type.", MessageType.Warning);
                ApplyRevertGUI();
                return;
            }

            serializedObject.UpdateIfRequiredOrScript();

            if (SVGImporter.isVectorGraphicsPackageInstalled)
            {
                IntPopup(m_SVGType, m_SVGTypeText, svgTypeOptionsFull, svgTypeValuesFull);
            }
            else
            {
                IntPopup(m_SVGType, m_SVGTypeText, svgTypeOptionsModuleOnly, svgTypeValuesModuleOnly);
            }

            if (m_SVGType.intValue != (int)SVGType.VectorImage)
            {
                PropertyField(m_PixelsPerUnit, m_PixelsPerUnitText);
                IntPopup(m_Alignment, m_AlignmentText, m_AlignmentOptions);

                if (!m_Alignment.hasMultipleDifferentValues && m_Alignment.intValue == (int)VectorUtils.Alignment.Custom)
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(m_CustomPivot, m_CustomPivotText);
                    GUILayout.EndHorizontal();
                }
            }

            PropertyField(m_GradientResolution, m_GradientResolutionText);

            if (m_SVGType.intValue != (int)SVGType.VectorImage)
            {
                using (new EditorGUI.DisabledScope(m_SVGType.hasMultipleDifferentValues || m_SVGType.intValue == (int)SVGType.Texture2D))
                    BoolToggle(m_GeneratePhysicsShape, m_GeneratePhysicsShapeText);
            }

            IntPopup(m_ViewportOptions, m_ViewportOptionsText, viewportOptions);
            
            EditorGUILayout.Space();

            if (m_SVGType.intValue == (int)SVGType.VectorImage)
            {
                IntPopup(m_TessellationMode, m_TessellationModeText, tessellationModeOptions);
            }

            if (m_TessellationMode.intValue == (int)TessellationMode.Triangulation || m_SVGType.intValue != (int)SVGType.VectorImage)
            {
                IntPopup(m_AdvancedMode, m_SettingsText, m_SettingOptions);

                ++EditorGUI.indentLevel;

                if (!m_AdvancedMode.hasMultipleDifferentValues)
                {
                    if (m_AdvancedMode.boolValue)
                    {
                        PropertyField(m_StepDistance, m_StepDistanceText);
                        PropertyField(m_SamplingStepDistance, m_SamplingStepDistanceText);

                        BoolToggle(m_MaxCordDeviationEnabled, m_MaxCordDeviationEnabledText);
                        if (!m_MaxCordDeviationEnabled.hasMultipleDifferentValues)
                        {
                            using (new EditorGUI.DisabledScope(!m_MaxCordDeviationEnabled.boolValue))
                                PropertyField(m_MaxCordDeviation, m_MaxCordDeviationText);
                        }

                        BoolToggle(m_MaxTangentAngleEnabled, m_MaxTangentAngleEnabledText);
                        if (!m_MaxTangentAngleEnabled.hasMultipleDifferentValues)
                        {
                            using (new EditorGUI.DisabledScope(!m_MaxTangentAngleEnabled.boolValue))
                                PropertyField(m_MaxTangentAngle, m_MaxTangentAngleText);
                        }
                    }
                    else
                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUI.showMixedValue = m_PredefinedResolutionIndex.hasMultipleDifferentValues;
                        int resolutionIndex = EditorGUILayout.Popup(m_TargetResolutionText, m_PredefinedResolutionIndex.intValue, m_TargetResolutionOptions);
                        EditorGUI.showMixedValue = false;
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_PredefinedResolutionIndex.intValue = resolutionIndex;
                            if (m_PredefinedResolutionIndex.intValue != (int)SVGImporter.PredefinedResolution.Custom)
                                m_TargetResolution.intValue = TargetResolutionFromPredefinedValue((SVGImporter.PredefinedResolution)m_PredefinedResolutionIndex.intValue);
                        }

                        if (!m_PredefinedResolutionIndex.hasMultipleDifferentValues && m_PredefinedResolutionIndex.intValue == (int)SVGImporter.PredefinedResolution.Custom)
                            PropertyField(m_TargetResolution, m_CustomTargetResolutionText);

                        PropertyField(m_ResolutionMultiplier, m_ResolutionMultiplierText);
                    }
                }
            }

            --EditorGUI.indentLevel;

            EditorGUILayout.Space();

            if (!m_SVGType.hasMultipleDifferentValues && (m_SVGType.intValue == (int)SVGType.TexturedSprite || m_SVGType.intValue == (int)SVGType.Texture2D))
            {
                ++EditorGUI.indentLevel;

                if (m_SVGType.intValue == (int)SVGType.TexturedSprite)
                {
                    IntPopup(m_TexturedSpriteMeshType, m_TexturedSpriteMeshTypeText, texturedSpriteMeshTypeOptions, texturedSpriteMeshTypeValues);
                    BoolToggle(m_UseSVGPixelsPerUnit, m_UseSVGPixelsPerUnitText);
                }

                PropertyField(m_KeepTextureAspectRatio, m_KeepTextureAspectRatioText);
                if (!m_KeepTextureAspectRatio.hasMultipleDifferentValues && m_KeepTextureAspectRatio.boolValue)
                {
                    PropertyField(m_TextureSize, m_TextureSizeText);
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel(m_TextureSizeText);
                    IntField(m_TextureWidth, GUIContent.none, GUILayout.MinWidth(40));
                    GUILayout.Label("x");
                    IntField(m_TextureHeight, GUIContent.none, GUILayout.MinWidth(40));
                    GUILayout.EndHorizontal();
                }

                IntPopup(m_WrapMode, m_WrapModeText, m_WrapModeContents, m_WrapModeValues);
                IntPopup(m_FilterMode, m_FilterModeText, m_FilterModeContents, m_FilterModeValues);
                IntPopup(m_SampleCount, m_SampleCountText, m_SampleCountContents, m_SampleCountValues);

                --EditorGUI.indentLevel;

                EditorGUILayout.Space();
            }

            if (!m_SVGType.hasMultipleDifferentValues && m_SVGType.intValue == (int)SVGType.UISVGImage)
            {
                BoolToggle(m_PreserveSVGImageAspect, m_PreserveSVGImageAspectText);
            }

            if (!m_SVGType.hasMultipleDifferentValues &&
                    (m_SVGType.intValue == (int)SVGType.VectorSprite ||
                     m_SVGType.intValue == (int)SVGType.TexturedSprite ||
                     m_SVGType.intValue == (int)SVGType.UISVGImage))
            {
                if (SVGImporter.isVectorGraphicsPackageInstalled)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Sprite Editor"))
                    {
                        SpriteUtilityWindow.ShowSpriteEditorWindow();
                    }
                    GUILayout.EndHorizontal();
                }
            }

            serializedObject.ApplyModifiedProperties();

            ApplyRevertGUI();
        }

        protected override void Apply()
        {
            base.Apply();

            // Adjust every values to make sure they're in range
            foreach (var target in targets)
            {
                var svgImporter = target as SVGImporter;
                svgImporter.SvgPixelsPerUnit = Mathf.Max(0.001f, svgImporter.SvgPixelsPerUnit);
                svgImporter.GradientResolution = Math.Min((ushort)4096, Math.Max((ushort)2, svgImporter.GradientResolution));
                svgImporter.StepDistance = Mathf.Max(0.0f, svgImporter.StepDistance);
                svgImporter.SamplingStepDistance = Mathf.Clamp(svgImporter.SamplingStepDistance, 3.0f, 1000.0f);
                svgImporter.MaxCordDeviation = Mathf.Max(0.0f, svgImporter.MaxCordDeviation);
                svgImporter.MaxTangentAngle = Mathf.Clamp(svgImporter.MaxTangentAngle, 0.0f, 90.0f);
                svgImporter.TargetResolution = (int)Mathf.Max(1, svgImporter.TargetResolution);
                svgImporter.ResolutionMultiplier = Mathf.Clamp(svgImporter.ResolutionMultiplier, 1.0f, 100.0f);
                svgImporter.TextureSize = Math.Max(1, svgImporter.TextureSize);
                svgImporter.TextureWidth = Math.Max(1, svgImporter.TextureWidth);
                svgImporter.TextureHeight = Math.Max(1, svgImporter.TextureHeight);
            }
        }

        private void PropertyField(SerializedProperty prop, GUIContent label)
        {
            EditorGUI.showMixedValue = prop.hasMultipleDifferentValues;
            EditorGUILayout.PropertyField(prop, label);
            EditorGUI.showMixedValue = false;
        }

        private void IntField(SerializedProperty prop, GUIContent label, params GUILayoutOption[] options)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMultipleDifferentValues;
            int value = EditorGUILayout.IntField(label, prop.intValue, options);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                prop.intValue = value;
        }

        private void IntPopup(SerializedProperty prop, GUIContent label, GUIContent[] displayedOptions)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMultipleDifferentValues;
            int value = EditorGUILayout.Popup(label, prop.intValue, displayedOptions);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                prop.intValue = value;
        }

        private void IntPopup(SerializedProperty prop, GUIContent label, GUIContent[] displayedOptions, int[] options)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMultipleDifferentValues;
            int value = EditorGUILayout.IntPopup(label, prop.intValue, displayedOptions, options);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                prop.intValue = value;
        }

        private void BoolToggle(SerializedProperty prop, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMultipleDifferentValues;
            bool value = EditorGUILayout.Toggle(label, prop.boolValue);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                prop.boolValue = value;
        }

        private int TargetResolutionFromPredefinedValue(SVGImporter.PredefinedResolution resolution)
        {
            switch (resolution)
            {
            case SVGImporter.PredefinedResolution.Res_2160p: return 2160;
            case SVGImporter.PredefinedResolution.Res_1080p: return 1080;
            case SVGImporter.PredefinedResolution.Res_720p:  return 720;
            case SVGImporter.PredefinedResolution.Res_480p:  return 480;
            default: return 1080;
            }
        }

        public override bool HasPreviewGUI()
        {
            return true;
        }

        protected override bool useAssetDrawPreview { get { return false; } }

        public override Texture2D RenderStaticPreview(string assetPath, UnityEngine.Object[] subAssets, int width, int height)
        {
            var obj = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (obj != null)
                return BuildPreviewTexture(obj, width, height);
            return null;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            background.Draw(r, false, false, false, false);

            Texture2D previewTex = null;
            var sourceSize = Vector2.zero;

            var sprite = SVGImporter.GetImportedSprite(assetTarget);
            if (sprite == null)
            {
                if (assetTarget is Texture2D)
                {
                    EditorGUI.DrawTextureTransparent(r, (Texture2D)assetTarget, ScaleMode.ScaleToFit, 0.0f, 0);
                    return;
                }

                if (assetTarget is VectorImage)
                {
                    var vi = assetTarget as VectorImage;
                    sourceSize = new Vector2(vi.width, vi.height);
                }
            }
            else
            {
                sourceSize = sprite.rect.size;
            }

            float zoomLevel = Mathf.Min(r.width / sourceSize.x, r.height / sourceSize.y);
            Rect wantedRect = new Rect(r.x, r.y, sourceSize.x * zoomLevel, sourceSize.y * zoomLevel);
            wantedRect.center = r.center;

            previewTex = BuildPreviewTexture(assetTarget, (int)wantedRect.width, (int)wantedRect.height);
            if (previewTex != null)
            {
                EditorGUI.DrawTextureTransparent(r, previewTex, ScaleMode.ScaleToFit);
                Texture2D.DestroyImmediate(previewTex);
            }
        }

        internal static Texture2D BuildPreviewTexture(UnityEngine.Object obj, int width, int height)
        {
            Texture2D previewTex = null;

            var sprite = SVGImporter.GetImportedSprite(obj);
            var vi = obj as VectorImage;
            if (sprite != null)
            {
                var mat = SVGImporter.GetSVGMaterial(sprite.texture != null, false);
                previewTex = VectorUtils.RenderSpriteToTexture2D(sprite, width, height, mat, 4);
            }
            else if (vi != null)
            {
                previewTex = VectorImageUtils.RenderToTexture2D(vi, width, height, 4);
            }
            return previewTex;
        }

        internal static string GetTextureInfoString(UnityEngine.Texture2D tex)
        {
            var format = TextureUtil.GetTextureFormat(tex);
            return "" + tex.width + "x" + tex.height + " " +
                GraphicsFormatUtility.GetFormatString(format) + " " +
                EditorUtility.FormatBytes(TextureUtil.GetStorageMemorySizeLong(tex));
        }

        public override string GetInfoString()
        {
            var sprite = SVGImporter.GetImportedSprite(assetTarget);
            if (sprite == null)
            {
                var tex = assetTarget as Texture2D;
                if (tex != null)
                    return GetTextureInfoString(tex);

                var vi = assetTarget as VectorImage;
                if (vi != null)
                    return $"VectorImage (size {vi.width:F1}x{vi.height:F1}) ({vi.vertices.Length} verts)";

                return "";
            }

            int vertexCount = sprite.vertices.Length;
            int indexCount = sprite.triangles.Length;

            var stats = "" + vertexCount + " Vertices (Pos";

            int vertexSize = sizeof(float) * 2;
            if (sprite.HasVertexAttribute(VertexAttribute.Color))
            {
                stats += ", Col";
                vertexSize += 4;
            }
            if (sprite.HasVertexAttribute(VertexAttribute.TexCoord0))
            {
                stats += ", TexCoord0";
                vertexSize += sizeof(float) * 2;
            }
            if (sprite.HasVertexAttribute(VertexAttribute.TexCoord1))
            {
                stats += ", TexCoord1";
                vertexSize += sizeof(float) * 2;
            }
            if (sprite.HasVertexAttribute(VertexAttribute.TexCoord2))
            {
                stats += ", TexCoord2";
                vertexSize += sizeof(float) * 2;
            }

            stats += ") " + HumanReadableSize(vertexSize * vertexCount + indexCount * 2);

            return stats;
        }

        private static string HumanReadableSize(int bytes)
        {
            var units = new string[] { "B", "KB", "MB", "GB", "TB" };

            int order = 0;
            while (bytes >= 2014 && order < units.Length-1) {
                ++order;
                bytes /= 1024;
            }

            if (order >=  units.Length)
                return "" + bytes;

            return String.Format("{0:0.#} {1}", bytes, units[order]);
        }
    }
}
