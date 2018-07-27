// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor.Experimental.AssetImporters;

namespace UnityEditor
{
    internal class ModelImporterModelEditor : BaseAssetImporterTabUI
    {
        // Scene
        SerializedProperty m_GlobalScale;
        SerializedProperty m_UseFileScale;
        SerializedProperty m_FileScale;
        SerializedProperty m_FileScaleUnit;
        SerializedProperty m_FileScaleFactor;

        SerializedProperty m_ImportBlendShapes;
        SerializedProperty m_ImportVisibility;
        SerializedProperty m_ImportCameras;
        SerializedProperty m_ImportLights;

        // Meshes
        SerializedProperty m_MeshCompression;
        SerializedProperty m_IsReadable;
        SerializedProperty m_OptimizeMeshForGPU;

        // Geometry
        SerializedProperty m_KeepQuads;
        SerializedProperty m_WeldVertices;
        SerializedProperty m_IndexFormat;

        SerializedProperty m_SwapUVChannels;

        SerializedProperty m_GenerateSecondaryUV;
        bool m_SecondaryUVAdvancedOptions = false;
        SerializedProperty m_SecondaryUVAngleDistortion;
        SerializedProperty m_SecondaryUVAreaDistortion;
        SerializedProperty m_SecondaryUVHardAngle;
        SerializedProperty m_SecondaryUVPackMargin;

        SerializedProperty m_NormalImportMode;
        SerializedProperty m_NormalCalculationMode;
        SerializedProperty m_BlendShapeNormalCalculationMode;
        SerializedProperty m_LegacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes;
        SerializedProperty m_NormalSmoothingSource;
        SerializedProperty m_NormalSmoothAngle;
        SerializedProperty m_TangentImportMode;

        // Prefab
        SerializedProperty m_PreserveHierarchy;
        SerializedProperty m_AddColliders;

        public ModelImporterModelEditor(AssetImporterEditor panelContainer)
            : base(panelContainer)
        {
        }

        internal override void OnEnable()
        {
            m_GlobalScale = serializedObject.FindProperty("m_GlobalScale");
            m_UseFileScale = serializedObject.FindProperty("m_UseFileScale");
            m_FileScale = serializedObject.FindProperty("m_FileScale");
            m_FileScaleUnit = serializedObject.FindProperty("m_FileScaleUnit");
            m_FileScaleFactor = serializedObject.FindProperty("m_FileScaleFactor");
            m_MeshCompression = serializedObject.FindProperty("m_MeshCompression");
            m_ImportBlendShapes = serializedObject.FindProperty("m_ImportBlendShapes");
            m_ImportCameras = serializedObject.FindProperty("m_ImportCameras");
            m_ImportLights = serializedObject.FindProperty("m_ImportLights");
            m_AddColliders = serializedObject.FindProperty("m_AddColliders");
            m_SwapUVChannels = serializedObject.FindProperty("swapUVChannels");
            m_GenerateSecondaryUV = serializedObject.FindProperty("generateSecondaryUV");
            m_SecondaryUVAngleDistortion = serializedObject.FindProperty("secondaryUVAngleDistortion");
            m_SecondaryUVAreaDistortion = serializedObject.FindProperty("secondaryUVAreaDistortion");
            m_SecondaryUVHardAngle = serializedObject.FindProperty("secondaryUVHardAngle");
            m_SecondaryUVPackMargin = serializedObject.FindProperty("secondaryUVPackMargin");
            m_NormalSmoothAngle = serializedObject.FindProperty("normalSmoothAngle");
            m_NormalImportMode = serializedObject.FindProperty("normalImportMode");
            m_NormalCalculationMode = serializedObject.FindProperty("normalCalculationMode");
            m_LegacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes = serializedObject.FindProperty("legacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes");
            m_NormalSmoothingSource = serializedObject.FindProperty("normalSmoothingSource");
            m_BlendShapeNormalCalculationMode = serializedObject.FindProperty("blendShapeNormalImportMode");
            m_TangentImportMode = serializedObject.FindProperty("tangentImportMode");
            m_OptimizeMeshForGPU = serializedObject.FindProperty("optimizeMeshForGPU");
            m_IsReadable = serializedObject.FindProperty("m_IsReadable");
            m_KeepQuads = serializedObject.FindProperty("keepQuads");
            m_IndexFormat = serializedObject.FindProperty("indexFormat");
            m_WeldVertices = serializedObject.FindProperty("weldVertices");
            m_ImportVisibility = serializedObject.FindProperty("m_ImportVisibility");
            m_PreserveHierarchy = serializedObject.FindProperty("m_PreserveHierarchy");
        }

        static class Styles
        {
            public static GUIContent Scene = EditorGUIUtility.TrTextContent("Scene", "FBX Scene import settings");
            public static GUIContent ScaleFactor = EditorGUIUtility.TrTextContent("Scale Factor", "How much to scale the models compared to what is in the source file.");
            public static GUIContent UseFileScale = EditorGUIUtility.TrTextContent("Convert Units", "Convert file units to Unity ones.");

            public static GUIContent ImportBlendShapes = EditorGUIUtility.TrTextContent("Import BlendShapes", "Should Unity import BlendShapes.");
            public static GUIContent ImportVisibility = EditorGUIUtility.TrTextContent("Import Visibility", "Use visibility properties to enable or disable MeshRenderer components.");
            public static GUIContent ImportCameras = EditorGUIUtility.TrTextContent("Import Cameras");
            public static GUIContent ImportLights = EditorGUIUtility.TrTextContent("Import Lights");
            public static GUIContent PreserveHierarchy = EditorGUIUtility.TrTextContent("Preserve Hierarchy", "Always create an explicit prefab root, even if the model only has a single root.");

            public static GUIContent Meshes = EditorGUIUtility.TrTextContent("Meshes", "Global settings for generated meshes");
            public static GUIContent MeshCompressionLabel = EditorGUIUtility.TrTextContent("Mesh Compression" , "Higher compression ratio means lower mesh precision. If enabled, the mesh bounds and a lower bit depth per component are used to compress the mesh data.");
            public static GUIContent IsReadable = EditorGUIUtility.TrTextContent("Read/Write Enabled", "Allow vertices and indices to be accessed from script.");
            public static GUIContent OptimizeMeshForGPU = EditorGUIUtility.TrTextContent("Optimize Mesh", "The vertices and indices will be reordered for better GPU performance.");
            public static GUIContent GenerateColliders = EditorGUIUtility.TrTextContent("Generate Colliders", "Should Unity generate mesh colliders for all meshes.");

            public static GUIContent Geometry = EditorGUIUtility.TrTextContent("Geometry", "Detailed mesh data");
            public static GUIContent KeepQuads = EditorGUIUtility.TrTextContent("Keep Quads", "If model contains quad faces, they are kept for DX11 tessellation.");
            public static GUIContent WeldVertices = EditorGUIUtility.TrTextContent("Weld Vertices", "Combine vertices that share the same position in space.");
            public static GUIContent IndexFormatLabel = EditorGUIUtility.TrTextContent("Index Format", "Format of mesh index buffer. Auto mode picks 16 or 32 bit depending on mesh vertex count.");

            public static GUIContent NormalsLabel = EditorGUIUtility.TrTextContent("Normals", "Source of mesh normals. If Import is selected and a mesh has no normals, they will be calculated instead.");
            public static GUIContent RecalculateNormalsLabel = EditorGUIUtility.TrTextContent("Normals Mode", "How to weight faces when calculating normals.");
            public static GUIContent SmoothingAngle = EditorGUIUtility.TrTextContent("Smoothing Angle", "When calculating normals on a mesh that doesn’t have smoothing groups, edges between faces will be smooth if this value is greater than the angle between the faces.");

            public static GUIContent TangentsLabel = EditorGUIUtility.TrTextContent("Tangents", "Source of mesh tangents. If Import is selected and a mesh has no tangents, they will be calculated instead.");

            public static GUIContent BlendShapeNormalsLabel = EditorGUIUtility.TrTextContent("Blend Shape Normals", "Source of blend shape normals. If Import is selected and a blend shape has no normals, they will be calculated instead.");
            public static GUIContent NormalSmoothingSourceLabel = EditorGUIUtility.TrTextContent("Smoothness Source", "How to determine which edges should be smooth and which should be sharp.");

            public static GUIContent SwapUVChannels = EditorGUIUtility.TrTextContent("Swap UVs", "Swaps the 2 UV channels in meshes. Use if your diffuse texture uses UVs from the lightmap.");
            public static GUIContent GenerateSecondaryUV           = EditorGUIUtility.TrTextContent("Generate Lightmap UVs", "Generate lightmap UVs into UV2.");
            public static GUIContent GenerateSecondaryUVAdvanced   = EditorGUIUtility.TrTextContent("Lightmap UVs settings", "Advanced settings for Lightmap UVs generation");
            public static GUIContent secondaryUVAngleDistortion    = EditorGUIUtility.TrTextContent("Angle Error", "Measured in percents. Angle error measures deviation of UV angles from geometry angles. Area error measures deviation of UV triangles area from geometry triangles if they were uniformly scaled.");
            public static GUIContent secondaryUVAreaDistortion     = EditorGUIUtility.TrTextContent("Area Error");
            public static GUIContent secondaryUVHardAngle          = EditorGUIUtility.TrTextContent("Hard Angle", "Angle between neighbor triangles that will generate seam.");
            public static GUIContent secondaryUVPackMargin         = EditorGUIUtility.TrTextContent("Pack Margin", "Measured in pixels, assuming mesh will cover an entire 1024x1024 lightmap.");

            public static GUIContent LegacyBlendShapeNormalsWarning = EditorGUIUtility.TrTextContent("This Model was originally imported in an earlier version of Unity. If your Mesh has Blend Shapes, you may notice some artifacts or missing normals, and options for Blend Shape Normals and Smoothness Source will not be shown.");
            public static GUIContent UpgradeButtonLabel = EditorGUIUtility.TrTextContent("Fix Now");
        }

        public override void OnInspectorGUI()
        {
            SceneGUI();
            MeshesGUI();
            GeometryGUI();
        }

        bool HelpBoxWithButton(GUIContent messageContent, GUIContent buttonContent, MessageType type)
        {
            const float kButtonWidth = 60f;
            const float kSpacing = 5f;
            const float kButtonHeight = 20f;

            // Reserve size of wrapped text
            Rect contentRect = GUILayoutUtility.GetRect(messageContent, EditorStyles.helpBox);
            // Reserve size of button
            GUILayoutUtility.GetRect(1, kButtonHeight + kSpacing);

            // Render background box with text at full height
            contentRect.height += kButtonHeight + kSpacing;
            GUI.Label(contentRect, EditorGUIUtility.TempContent(messageContent.text, EditorGUIUtility.GetHelpIcon(type)), EditorStyles.helpBox);

            // Button (align lower right)
            Rect buttonRect = new Rect(contentRect.xMax - kButtonWidth - 4f, contentRect.yMax - kButtonHeight - 4f, kButtonWidth, kButtonHeight);
            return GUI.Button(buttonRect, buttonContent);
        }

        void MeshesGUI()
        {
            GUILayout.Label(Styles.Meshes, EditorStyles.boldLabel);
            using (var horizontal = new EditorGUILayout.HorizontalScope())
            {
                using (var prop = new EditorGUI.PropertyScope(horizontal.rect, Styles.MeshCompressionLabel, m_MeshCompression))
                {
                    EditorGUI.BeginChangeCheck();
                    var newValue = (int)(ModelImporterMeshCompression)EditorGUILayout.EnumPopup(prop.content, (ModelImporterMeshCompression)m_MeshCompression.intValue);
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_MeshCompression.intValue = newValue;
                    }
                }
            }

            EditorGUILayout.PropertyField(m_IsReadable, Styles.IsReadable);
            EditorGUILayout.PropertyField(m_OptimizeMeshForGPU, Styles.OptimizeMeshForGPU);
            EditorGUILayout.PropertyField(m_AddColliders, Styles.GenerateColliders);
        }

        void SceneGUI()
        {
            GUILayout.Label(Styles.Scene, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_GlobalScale, Styles.ScaleFactor);

            using (var horizontalScope = new EditorGUILayout.HorizontalScope())
            {
                using (var propertyField = new EditorGUI.PropertyScope(horizontalScope.rect, Styles.UseFileScale, m_UseFileScale))
                {
                    m_UseFileScale.boolValue = EditorGUILayout.Toggle(propertyField.content, m_UseFileScale.boolValue);
                    // Put the unit convertion description on a second line if the Inspector is too small.
                    if (!EditorGUIUtility.wideMode)
                    {
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                    }
                    using (new EditorGUI.DisabledScope(!m_UseFileScale.boolValue))
                    {
                        if (!string.IsNullOrEmpty(m_FileScaleUnit.stringValue))
                        {
                            GUIContent content = m_FileScaleUnit.hasMultipleDifferentValues
                                ? EditorGUI.mixedValueContent
                                : GUIContent.Temp(string.Format(L10n.Tr("1{0} (File) to {1}m (Unity)"), m_FileScaleUnit.stringValue, m_FileScaleFactor.floatValue));
                            EditorGUILayout.LabelField(content, GUILayout.ExpandWidth(true));
                        }
                        else
                        {
                            GUIContent content = m_FileScaleUnit.hasMultipleDifferentValues
                                ? EditorGUI.mixedValueContent
                                : GUIContent.Temp(string.Format(L10n.Tr("1 unit (File) to {0}m (Unity)"), m_FileScale.floatValue));
                            EditorGUILayout.LabelField(content);
                        }
                    }
                }
            }

            EditorGUILayout.PropertyField(m_ImportBlendShapes, Styles.ImportBlendShapes);
            EditorGUILayout.PropertyField(m_ImportVisibility, Styles.ImportVisibility);
            EditorGUILayout.PropertyField(m_ImportCameras, Styles.ImportCameras);
            EditorGUILayout.PropertyField(m_ImportLights, Styles.ImportLights);
            EditorGUILayout.PropertyField(m_PreserveHierarchy, Styles.PreserveHierarchy);
        }

        void GeometryGUI()
        {
            GUILayout.Label(Styles.Geometry, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_KeepQuads, Styles.KeepQuads);
            EditorGUILayout.PropertyField(m_WeldVertices, Styles.WeldVertices);
            using (var horizontal = new EditorGUILayout.HorizontalScope())
            {
                using (var prop = new EditorGUI.PropertyScope(horizontal.rect, Styles.IndexFormatLabel, m_IndexFormat))
                {
                    EditorGUI.BeginChangeCheck();
                    var newValue = (int)(ModelImporterIndexFormat)EditorGUILayout.EnumPopup(prop.content, (ModelImporterIndexFormat)m_IndexFormat.intValue);
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_IndexFormat.intValue = newValue;
                    }
                }
            }

            NormalsTangentsGUI();

            UvsGUI();
        }

        void NormalsTangentsGUI()
        {
            using (var horizontal = new EditorGUILayout.HorizontalScope())
            {
                using (var property = new EditorGUI.PropertyScope(horizontal.rect, Styles.NormalsLabel, m_NormalImportMode))
                {
                    EditorGUI.BeginChangeCheck();
                    var newValue = (int)(ModelImporterNormals)EditorGUILayout.EnumPopup(property.content, (ModelImporterNormals)m_NormalImportMode.intValue);
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_NormalImportMode.intValue = newValue;
                        // This check is made in CheckConcistency, but because AssetImporterEditor does not serialize the object each update,
                        // We need to double check here for UI consistency.
                        if (m_NormalImportMode.intValue == (int)ModelImporterNormals.None)
                            m_TangentImportMode.intValue = (int)ModelImporterTangents.None;
                        else if (m_NormalImportMode.intValue == (int)ModelImporterNormals.Calculate && m_TangentImportMode.intValue == (int)ModelImporterTangents.Import)
                            m_TangentImportMode.intValue = (int)ModelImporterTangents.CalculateMikk;


                        // Also make the blendshape normal mode follow normal mode, with the exception that we never
                        // select Import automatically (since we can't trust imported normals to be correct, and we
                        // also can't detect when they're not).
                        if (m_NormalImportMode.intValue == (int)ModelImporterNormals.None)
                            m_BlendShapeNormalCalculationMode.intValue = (int)ModelImporterNormals.None;
                        else
                            m_BlendShapeNormalCalculationMode.intValue = (int)ModelImporterNormals.Calculate;
                    }
                }
            }

            if (!m_LegacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes.boolValue && m_ImportBlendShapes.boolValue && !m_LegacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes.hasMultipleDifferentValues)
            {
                using (new EditorGUI.DisabledScope(m_NormalImportMode.intValue == (int)ModelImporterNormals.None))
                {
                    m_BlendShapeNormalCalculationMode.intValue = (int)(ModelImporterNormals)EditorGUILayout.EnumPopup(Styles.BlendShapeNormalsLabel, (ModelImporterNormals)m_BlendShapeNormalCalculationMode.intValue);
                }
            }

            if (m_NormalImportMode.intValue != (int)ModelImporterNormals.None || m_BlendShapeNormalCalculationMode.intValue != (int)ModelImporterNormals.None)
            {
                // Normal calculation mode
                using (var horizontal = new EditorGUILayout.HorizontalScope())
                {
                    using (var property = new EditorGUI.PropertyScope(horizontal.rect, Styles.RecalculateNormalsLabel, m_NormalCalculationMode))
                    {
                        EditorGUI.BeginChangeCheck();
                        var newValue = (int)(ModelImporterNormalCalculationMode)EditorGUILayout.EnumPopup(property.content, (ModelImporterNormalCalculationMode)m_NormalCalculationMode.intValue);
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_NormalCalculationMode.intValue = newValue;
                        }
                    }
                }

                // Normal smoothness
                if (!m_LegacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes.boolValue)
                {
                    m_NormalSmoothingSource.intValue = (int)(ModelImporterNormalSmoothingSource)EditorGUILayout.EnumPopup(Styles.NormalSmoothingSourceLabel, (ModelImporterNormalSmoothingSource)m_NormalSmoothingSource.intValue);
                }

                // Normal split angle
                if (m_LegacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes.boolValue || m_NormalSmoothingSource.intValue == (int)ModelImporterNormalSmoothingSource.PreferSmoothingGroups || m_NormalSmoothingSource.intValue == (int)ModelImporterNormalSmoothingSource.FromAngle)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.Slider(m_NormalSmoothAngle, 0, 180, Styles.SmoothingAngle);

                    // Property is serialized as float but we want to show it as an int so we round the value when changed
                    if (EditorGUI.EndChangeCheck())
                        m_NormalSmoothAngle.floatValue = Mathf.Round(m_NormalSmoothAngle.floatValue);
                }

                if (m_LegacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes.boolValue)
                {
                    if (HelpBoxWithButton(Styles.LegacyBlendShapeNormalsWarning, Styles.UpgradeButtonLabel, MessageType.Warning))
                    {
                        m_LegacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes.boolValue = false;
                    }
                }
            }

            // Choose the option values and labels based on what the NormalImportMode is
            if (m_NormalImportMode.intValue != (int)ModelImporterNormals.None)
            {
                using (var horizontal = new EditorGUILayout.HorizontalScope())
                {
                    using (var property = new EditorGUI.PropertyScope(horizontal.rect, Styles.TangentsLabel, m_TangentImportMode))
                    {
                        EditorGUI.BeginChangeCheck();
                        var newValue = (int)(ModelImporterTangents)EditorGUILayout.EnumPopup(property.content, (ModelImporterTangents)m_TangentImportMode.intValue, TangentModeAvailabilityCheck, false);
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_TangentImportMode.intValue = newValue;
                        }
                    }
                }
            }
        }

        bool TangentModeAvailabilityCheck(Enum value)
        {
            return (int)(ModelImporterTangents)value >= m_NormalImportMode.intValue;
        }

        void UvsGUI()
        {
            EditorGUILayout.PropertyField(m_SwapUVChannels, Styles.SwapUVChannels);
            EditorGUILayout.PropertyField(m_GenerateSecondaryUV, Styles.GenerateSecondaryUV);
            if (m_GenerateSecondaryUV.boolValue)
            {
                m_SecondaryUVAdvancedOptions = EditorGUILayout.Foldout(m_SecondaryUVAdvancedOptions, Styles.GenerateSecondaryUVAdvanced, true, EditorStyles.foldout);
                if (m_SecondaryUVAdvancedOptions)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        // TODO: all slider min/max values should be revisited
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.Slider(m_SecondaryUVHardAngle, 0, 180, Styles.secondaryUVHardAngle);
                        EditorGUILayout.Slider(m_SecondaryUVPackMargin, 1, 64, Styles.secondaryUVPackMargin);
                        EditorGUILayout.Slider(m_SecondaryUVAngleDistortion, 1, 75, Styles.secondaryUVAngleDistortion);
                        EditorGUILayout.Slider(m_SecondaryUVAreaDistortion, 1, 75, Styles.secondaryUVAreaDistortion);
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_SecondaryUVHardAngle.floatValue = Mathf.Round(m_SecondaryUVHardAngle.floatValue);
                            m_SecondaryUVPackMargin.floatValue = Mathf.Round(m_SecondaryUVPackMargin.floatValue);
                            m_SecondaryUVAngleDistortion.floatValue = Mathf.Round(m_SecondaryUVAngleDistortion.floatValue);
                            m_SecondaryUVAreaDistortion.floatValue = Mathf.Round(m_SecondaryUVAreaDistortion.floatValue);
                        }
                    }
                }
            }
        }
    }
}
