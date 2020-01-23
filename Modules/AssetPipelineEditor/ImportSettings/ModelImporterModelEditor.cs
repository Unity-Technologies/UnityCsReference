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
#pragma warning disable 0649
        // Scene
        [CacheProperty]
        SerializedProperty m_GlobalScale;
        [CacheProperty]
        SerializedProperty m_UseFileScale;
        [CacheProperty]
        SerializedProperty m_FileScale;
        [CacheProperty]
        SerializedProperty m_FileScaleUnit;
        [CacheProperty]
        SerializedProperty m_FileScaleFactor;

        [CacheProperty]
        SerializedProperty m_ImportBlendShapes;
        [CacheProperty]
        SerializedProperty m_ImportVisibility;
        [CacheProperty]
        protected  SerializedProperty m_ImportCameras;
        [CacheProperty]
        SerializedProperty m_ImportLights;

        // Meshes
        [CacheProperty]
        SerializedProperty m_MeshCompression;
        [CacheProperty]
        SerializedProperty m_IsReadable;
        [CacheProperty("meshOptimizationFlags")]
        SerializedProperty m_MeshOptimizationFlags;

        // Geometry
        [CacheProperty("keepQuads")]
        SerializedProperty m_KeepQuads;
        [CacheProperty("weldVertices")]
        SerializedProperty m_WeldVertices;
        [CacheProperty("indexFormat")]
        protected SerializedProperty m_IndexFormat;

        [CacheProperty("swapUVChannels")]
        SerializedProperty m_SwapUVChannels;

        [CacheProperty("generateSecondaryUV")]
        SerializedProperty m_GenerateSecondaryUV;
        bool m_SecondaryUVAdvancedOptions = false;
        [CacheProperty("secondaryUVAngleDistortion")]
        SerializedProperty m_SecondaryUVAngleDistortion;
        [CacheProperty("secondaryUVAreaDistortion")]
        SerializedProperty m_SecondaryUVAreaDistortion;
        [CacheProperty("secondaryUVHardAngle")]
        SerializedProperty m_SecondaryUVHardAngle;
        [CacheProperty("secondaryUVMarginMethod")]
        SerializedProperty m_SecondaryUVMarginMethod;
        [CacheProperty("secondaryUVPackMargin")]
        SerializedProperty m_SecondaryUVPackMargin;
        [CacheProperty("secondaryUVMinLightmapResolution")]
        SerializedProperty m_SecondaryUVMinLightmapResolution;
        [CacheProperty("secondaryUVMinObjectScale")]
        SerializedProperty m_SecondaryUVMinObjectScale;

        [CacheProperty("normalImportMode")]
        protected SerializedProperty m_NormalImportMode;
        [CacheProperty("normalCalculationMode")]
        protected SerializedProperty m_NormalCalculationMode;
        [CacheProperty("blendShapeNormalImportMode")]
        SerializedProperty m_BlendShapeNormalCalculationMode;
        [CacheProperty("legacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes")]
        SerializedProperty m_LegacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes;
        [CacheProperty("normalSmoothingSource")]
        SerializedProperty m_NormalSmoothingSource;
        [CacheProperty("normalSmoothAngle")]
        protected SerializedProperty m_NormalSmoothAngle;
        [CacheProperty("tangentImportMode")]
        protected SerializedProperty m_TangentImportMode;

        // Prefab
        [CacheProperty]
        SerializedProperty m_PreserveHierarchy;
        [CacheProperty]
        SerializedProperty m_SortHierarchyByName;
        [CacheProperty]
        SerializedProperty m_AddColliders;
        [CacheProperty("bakeAxisConversion")]
        SerializedProperty m_BakeAxisConversion;
#pragma warning restore 0649

        public ModelImporterModelEditor(AssetImporterEditor panelContainer)
            : base(panelContainer)
        {
        }

        internal override void OnEnable()
        {
            Editor.AssignCachedProperties(this, serializedObject.GetIterator());
        }

        protected static class Styles
        {
            public static GUIContent Scene = EditorGUIUtility.TrTextContent("Scene", "FBX Scene import settings");
            public static GUIContent ScaleFactor = EditorGUIUtility.TrTextContent("Scale Factor", "How much to scale the models compared to what is in the source file.");
            public static GUIContent UseFileScale = EditorGUIUtility.TrTextContent("Convert Units", "Convert file units to Unity ones.");

            public static GUIContent ImportBlendShapes = EditorGUIUtility.TrTextContent("Import BlendShapes", "Should Unity import BlendShapes.");
            public static GUIContent ImportVisibility = EditorGUIUtility.TrTextContent("Import Visibility", "Use visibility properties to enable or disable MeshRenderer components.");
            public static GUIContent ImportCameras = EditorGUIUtility.TrTextContent("Import Cameras");
            public static GUIContent ImportLights = EditorGUIUtility.TrTextContent("Import Lights");
            public static GUIContent PreserveHierarchy = EditorGUIUtility.TrTextContent("Preserve Hierarchy", "Always create an explicit prefab root, even if the model only has a single root.");
            public static GUIContent SortHierarchyByName = EditorGUIUtility.TrTextContent("Sort Hierarchy By Name", "Sort game objects children by name.");

            public static GUIContent Meshes = EditorGUIUtility.TrTextContent("Meshes", "Global settings for generated meshes");
            public static GUIContent MeshCompressionLabel = EditorGUIUtility.TrTextContent("Mesh Compression" , "Higher compression ratio means lower mesh precision. If enabled, the mesh bounds and a lower bit depth per component are used to compress the mesh data.");
            public static GUIContent IsReadable = EditorGUIUtility.TrTextContent("Read/Write Enabled", "Allow vertices and indices to be accessed from script.");
            public static GUIContent OptimizationFlags = EditorGUIUtility.TrTextContent("Optimize Mesh", "Reorder vertices and/or polygons for better GPU performance.");

            public static GUIContent GenerateColliders = EditorGUIUtility.TrTextContent("Generate Colliders", "Should Unity generate mesh colliders for all meshes.");

            public static GUIContent Geometry = EditorGUIUtility.TrTextContent("Geometry", "Detailed mesh data");
            public static GUIContent KeepQuads = EditorGUIUtility.TrTextContent("Keep Quads", "If model contains quad faces, they are kept for DX11 tessellation.");
            public static GUIContent WeldVertices = EditorGUIUtility.TrTextContent("Weld Vertices", "Combine vertices that share the same position in space.");
            public static GUIContent IndexFormatLabel = EditorGUIUtility.TrTextContent("Index Format", "Format of mesh index buffer. Auto mode picks 16 or 32 bit depending on mesh vertex count.");

            public static GUIContent NormalsLabel = EditorGUIUtility.TrTextContent("Normals", "Source of mesh normals. If Import is selected and a mesh has no normals, they will be calculated instead.");
            public static GUIContent RecalculateNormalsLabel = EditorGUIUtility.TrTextContent("Normals Mode", "How to weight faces when calculating normals.");
            public static GUIContent SmoothingAngle = EditorGUIUtility.TrTextContent("Smoothing Angle", "When calculating normals on a mesh that doesn't have smoothing groups, edges between faces will be smooth if this value is greater than the angle between the faces.");

            public static GUIContent TangentsLabel = EditorGUIUtility.TrTextContent("Tangents", "Source of mesh tangents. If Import is selected and a mesh has no tangents, they will be calculated instead.");

            public static GUIContent BlendShapeNormalsLabel = EditorGUIUtility.TrTextContent("Blend Shape Normals", "Source of blend shape normals. If Import is selected and a blend shape has no normals, they will be calculated instead.");
            public static GUIContent NormalSmoothingSourceLabel = EditorGUIUtility.TrTextContent("Smoothness Source", "How to determine which edges should be smooth and which should be sharp.");

            public static GUIContent SwapUVChannels = EditorGUIUtility.TrTextContent("Swap UVs", "Swaps the 2 UV channels in meshes. Use if your diffuse texture uses UVs from the lightmap.");
            public static GUIContent GenerateSecondaryUV = EditorGUIUtility.TrTextContent("Generate Lightmap UVs", "Generate lightmap UVs into UV2.");
            public static GUIContent GenerateSecondaryUVAdvanced = EditorGUIUtility.TrTextContent("Lightmap UVs settings", "Advanced settings for Lightmap UVs generation");

            public static GUIContent secondaryUVAngleDistortion       = EditorGUIUtility.TrTextContent("Angle Error", "Measured in percents. Angle error measures deviation of UV angles from geometry angles. Area error measures deviation of UV triangles area from geometry triangles if they were uniformly scaled.");
            public static GUIContent secondaryUVAreaDistortion        = EditorGUIUtility.TrTextContent("Area Error");
            public static GUIContent secondaryUVHardAngle             = EditorGUIUtility.TrTextContent("Hard Angle", "Angle between neighbor triangles that will generate seam.");
            public static GUIContent secondaryUVMarginMethod          = EditorGUIUtility.TrTextContent("Margin Method", "Method to handle margins between UV charts.");
            public static GUIContent secondaryUVPackMargin            = EditorGUIUtility.TrTextContent("Pack Margin", "Measured in pixels, assuming mesh will cover an entire 1024x1024 lightmap.");
            public static GUIContent secondaryUVMinLightmapResolution = EditorGUIUtility.TrTextContent("Min Lightmap Resolution", "The minimum lightmap resolution at which this object will be used. Used to determine a packing which ensures no texel bleeding.");
            public static GUIContent secondaryUVMinObjectScale        = EditorGUIUtility.TrTextContent("Min Object Scale", "The smallest scale at which this mesh will be used. Used to determine a packing which ensures no texel bleeding.");

            public static GUIContent secondaryUVMinLightmapResolutionNotice = EditorGUIUtility.TrTextContent("The active scene's Lightmap Resolution is less than the specified Min Lightmap Resolution.", EditorGUIUtility.GetHelpIcon(MessageType.Info));

            public static GUIContent LegacyComputeNormalsFromSmoothingGroupsWhenMeshHasBlendShapes = EditorGUIUtility.TrTextContent("Legacy Blend Shape Normals", "Compute normals from smoothing groups when the mesh has BlendShapes.");
            public static GUIContent BakeAxisConversion = EditorGUIUtility.TrTextContent("Bake Axis Conversion", "Perform axis conversion on all content for models defined in an axis system that differs from Unity's (left handed, Z forward, Y-up).");
        }

        public override void OnInspectorGUI()
        {
            SceneGUI();
            MeshesGUI();
            GeometryGUI();
        }

        protected void MeshesGUI()
        {
            EditorGUILayout.LabelField(Styles.Meshes, EditorStyles.boldLabel);
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

            m_MeshOptimizationFlags.intValue = (int)(MeshOptimizationFlags)EditorGUILayout.EnumFlagsField(Styles.OptimizationFlags, (MeshOptimizationFlags)m_MeshOptimizationFlags.intValue);

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
                    EditorGUI.showMixedValue = m_UseFileScale.hasMultipleDifferentValues;
                    using (var changed = new EditorGUI.ChangeCheckScope())
                    {
                        var result = EditorGUILayout.Toggle(propertyField.content, m_UseFileScale.boolValue);
                        if (changed.changed)
                            m_UseFileScale.boolValue = result;
                    }
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
                                : GUIContent.Temp(UnityString.Format(L10n.Tr("1{0} (File) to {1}m (Unity)"), m_FileScaleUnit.stringValue, m_FileScaleFactor.floatValue));
                            EditorGUILayout.LabelField(content, GUILayout.ExpandWidth(true));
                        }
                        else
                        {
                            GUIContent content = m_FileScaleUnit.hasMultipleDifferentValues
                                ? EditorGUI.mixedValueContent
                                : GUIContent.Temp(UnityString.Format(L10n.Tr("1 unit (File) to {0}m (Unity)"), m_FileScale.floatValue));
                            EditorGUILayout.LabelField(content);
                        }
                    }
                }
            }

            EditorGUILayout.PropertyField(m_BakeAxisConversion, Styles.BakeAxisConversion);
            EditorGUILayout.PropertyField(m_ImportBlendShapes, Styles.ImportBlendShapes);
            EditorGUILayout.PropertyField(m_ImportVisibility, Styles.ImportVisibility);
            EditorGUILayout.PropertyField(m_ImportCameras, Styles.ImportCameras);
            EditorGUILayout.PropertyField(m_ImportLights, Styles.ImportLights);
            EditorGUILayout.PropertyField(m_PreserveHierarchy, Styles.PreserveHierarchy);
            EditorGUILayout.PropertyField(m_SortHierarchyByName, Styles.SortHierarchyByName);
        }

        protected void GeometryGUI()
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
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = m_LegacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes.hasMultipleDifferentValues;
            var legacyComputeFromSmoothingGroups = EditorGUILayout.Toggle(Styles.LegacyComputeNormalsFromSmoothingGroupsWhenMeshHasBlendShapes, m_LegacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes.boolValue);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                m_LegacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes.boolValue = legacyComputeFromSmoothingGroups;
            }

            using (var horizontal = new EditorGUILayout.HorizontalScope())
            {
                using (var property = new EditorGUI.PropertyScope(horizontal.rect, Styles.NormalsLabel, m_NormalImportMode))
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.showMixedValue = m_NormalImportMode.hasMultipleDifferentValues;
                    var newValue = (int)(ModelImporterNormals)EditorGUILayout.EnumPopup(property.content, (ModelImporterNormals)m_NormalImportMode.intValue);
                    EditorGUI.showMixedValue = false;
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_NormalImportMode.intValue = newValue;
                        // This check is made in CheckConsistency, but because AssetImporterEditor does not serialize the object each update,
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
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.showMixedValue = m_BlendShapeNormalCalculationMode.hasMultipleDifferentValues;
                    var blendShapeNormalCalculationMode  = (int)(ModelImporterNormals)EditorGUILayout.EnumPopup(Styles.BlendShapeNormalsLabel, (ModelImporterNormals)m_BlendShapeNormalCalculationMode.intValue);
                    EditorGUI.showMixedValue = false;
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_BlendShapeNormalCalculationMode.intValue = blendShapeNormalCalculationMode;
                    }
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
                        EditorGUI.showMixedValue = m_NormalCalculationMode.hasMultipleDifferentValues;
                        var normalCalculationMode = (int)(ModelImporterNormalCalculationMode)EditorGUILayout.EnumPopup(property.content, (ModelImporterNormalCalculationMode)m_NormalCalculationMode.intValue);
                        EditorGUI.showMixedValue = false;
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_NormalCalculationMode.intValue = normalCalculationMode;
                        }
                    }
                }

                // Normal smoothness
                if (!m_LegacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes.boolValue)
                {
                    using (var horizontal = new EditorGUILayout.HorizontalScope())
                    using (var property = new EditorGUI.PropertyScope(horizontal.rect, Styles.NormalSmoothingSourceLabel, m_NormalSmoothingSource))
                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUI.showMixedValue = m_NormalSmoothingSource.hasMultipleDifferentValues;
                        var normalSmoothingSource = (int)(ModelImporterNormalSmoothingSource)EditorGUILayout.EnumPopup(property.content, (ModelImporterNormalSmoothingSource)m_NormalSmoothingSource.intValue);
                        EditorGUI.showMixedValue = false;
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_NormalSmoothingSource.intValue = normalSmoothingSource;
                        }
                    }
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

        protected bool TangentModeAvailabilityCheck(Enum value)
        {
            return (int)(ModelImporterTangents)value >= m_NormalImportMode.intValue;
        }

        protected void UvsGUI()
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
                        EditorGUI.BeginChangeCheck();

                        EditorGUILayout.Slider(m_SecondaryUVHardAngle, 0, 180, Styles.secondaryUVHardAngle);
                        EditorGUILayout.Slider(m_SecondaryUVAngleDistortion, 1, 75, Styles.secondaryUVAngleDistortion);
                        EditorGUILayout.Slider(m_SecondaryUVAreaDistortion, 1, 75, Styles.secondaryUVAreaDistortion);

                        using (var horizontal = new EditorGUILayout.HorizontalScope())
                        {
                            using (var prop = new EditorGUI.PropertyScope(horizontal.rect, Styles.secondaryUVMarginMethod, m_SecondaryUVMarginMethod))
                            {
                                EditorGUI.BeginChangeCheck();
                                var newValue = (int)(ModelImporterSecondaryUVMarginMethod)EditorGUILayout.EnumPopup(prop.content, (ModelImporterSecondaryUVMarginMethod)m_SecondaryUVMarginMethod.intValue);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    m_SecondaryUVMarginMethod.intValue = newValue;
                                }
                            }
                        }
                        if (m_SecondaryUVMarginMethod.intValue == (int)ModelImporterSecondaryUVMarginMethod.Calculate)
                        {
                            EditorGUILayout.PropertyField(m_SecondaryUVMinLightmapResolution, Styles.secondaryUVMinLightmapResolution);
                            if (Lightmapping.GetLightingSettingsOrDefaultsFallback().lightmapResolution < m_SecondaryUVMinLightmapResolution.floatValue)
                            {
                                EditorGUILayout.HelpBox(Styles.secondaryUVMinLightmapResolutionNotice);
                            }

                            EditorGUILayout.PropertyField(m_SecondaryUVMinObjectScale, Styles.secondaryUVMinObjectScale);
                        }
                        else
                        {
                            EditorGUILayout.Slider(m_SecondaryUVPackMargin, 1, 64, Styles.secondaryUVPackMargin);
                        }

                        if (EditorGUI.EndChangeCheck())
                        {
                            m_SecondaryUVHardAngle.floatValue = Mathf.Round(m_SecondaryUVHardAngle.floatValue);
                            m_SecondaryUVPackMargin.floatValue = Mathf.Round(m_SecondaryUVPackMargin.floatValue);
                            m_SecondaryUVMinLightmapResolution.floatValue = Mathf.Round(m_SecondaryUVMinLightmapResolution.floatValue);
                            m_SecondaryUVMinObjectScale.floatValue = m_SecondaryUVMinObjectScale.floatValue;
                            m_SecondaryUVAngleDistortion.floatValue = Mathf.Round(m_SecondaryUVAngleDistortion.floatValue);
                            m_SecondaryUVAreaDistortion.floatValue = Mathf.Round(m_SecondaryUVAreaDistortion.floatValue);
                        }
                    }
                }
            }
        }
    }
}
