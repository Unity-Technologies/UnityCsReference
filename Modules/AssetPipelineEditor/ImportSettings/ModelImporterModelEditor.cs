// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor.AssetImporters;
using UnityEditorInternal;

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
        SerializedProperty m_ImportBlendShapeDeformPercent;
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

        [CacheProperty("generateMeshLods")]
        SerializedProperty m_GenerateMeshLods;

        [CacheProperty("meshLodGenerationFlags")]
        SerializedProperty m_MeshLodGenerationFlags;

        [CacheProperty("maximumMeshLod")]
        SerializedProperty m_MaximumMeshLod;


        // Geometry
        [CacheProperty("keepQuads")]
        SerializedProperty m_KeepQuads;
        [CacheProperty("weldVertices")]
        SerializedProperty m_WeldVertices;
        [CacheProperty("indexFormat")]
        protected SerializedProperty m_IndexFormat;

        [CacheProperty("importUVs")]
        protected SerializedProperty m_ImportUVs;
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
        [CacheProperty]
        SerializedProperty m_PreBakeConvexCollisionMesh;
        [CacheProperty]
        SerializedProperty m_PreBakeTriangleCollisionMesh;
        [CacheProperty("bakeAxisConversion")]
        SerializedProperty m_BakeAxisConversion;

        [CacheProperty]
        SerializedProperty m_StrictVertexDataChecks;

        [CacheProperty("importVertexColors")]
        protected SerializedProperty m_ImportVertexColors;

#pragma warning restore 0649

        public ModelImporterModelEditor(AssetImporterEditor panelContainer)
            : base(panelContainer)
        {
        }

        internal override void OnEnable()
        {
            Editor.AssignCachedProperties(this, serializedObject.GetIterator());
        }

        internal override void PostSerializedObjectCreation()
        {
            base.PostSerializedObjectCreation();
            Editor.AssignCachedProperties(this, serializedObject.GetIterator());
        }


        protected static class Styles
        {
            public static readonly GUIContent Scene = L10n.TextContent("Scene", "FBX Scene import settings", null, null);
            public static readonly GUIContent ScaleFactor = L10n.TextContent("Scale Factor", "How much to scale the models compared to what is in the source file.", null, null);
            public static readonly GUIContent UseFileScale = L10n.TextContent("Convert Units", "Convert file units to Unity ones.", null, null);

            public static readonly GUIContent ImportBlendShapes = L10n.TextContent("Import BlendShapes", "Should Unity import BlendShapes.", null, null);
            public static readonly GUIContent ImportBlendShapesDeformPercent = L10n.TextContent("Import Deform Percent", "Import BlendShapes deform percent. If disabled, all values will be set to 0.", null, null);
            public static readonly GUIContent ImportVisibility = L10n.TextContent("Import Visibility", "Use visibility properties to enable or disable MeshRenderer components.", null, null);
            public static readonly GUIContent ImportCameras = L10n.TextContent("Import Cameras", null, null, null);
            public static readonly GUIContent ImportLights = L10n.TextContent("Import Lights", null, null, null);
            public static readonly GUIContent PreserveHierarchy = L10n.TextContent("Preserve Hierarchy", "Always create an explicit prefab root, even if the model only has a single root.", null, null);
            public static readonly GUIContent SortHierarchyByName = L10n.TextContent("Sort Hierarchy By Name", "Sort game objects children by name.", null, null);
            public static readonly GUIContent StrictVertexDataChecks = L10n.TextContent("Strict Vertex Data Checks", "Enables strict checks on Vertex data. If enabled, checks discard invalid data, this may result in missing vertex data but ensures that import results are consistent and can prevent crashes.", null, null);

            public static readonly GUIContent Meshes = L10n.TextContent("Meshes", "Global settings for generated meshes", null, null);
            public static readonly GUIContent MeshCompressionLabel = L10n.TextContent("Mesh Compression" , "Higher compression ratio means lower mesh precision. If enabled, the mesh bounds and a lower bit depth per component are used to compress the mesh data.", null, null);
            public static readonly GUIContent IsReadable = L10n.TextContent("Read/Write", "Allow vertices and indices to be accessed from script.", null, null);
            public static readonly GUIContent OptimizationFlags = L10n.TextContent("Optimize Mesh", "Reorder vertices and/or polygons for better GPU performance.", null, null);
            public static readonly GUIContent MeshLods = L10n.TextContent("Mesh LODs", null, null, null);
            public static readonly GUIContent MeshLodsInfoBox = EditorGUIUtility.TrTextContent("The quality of simplified meshes depends on the complexity and structure of the original mesh. To achieve the best outcomes, read the documentation to understand the feature's capabilities, limitations, and optimal workflows.", EditorGUIUtility.GetHelpIcon(MessageType.Info));
            public static readonly GUIContent GenerateMeshLods = L10n.TextContent("Generate Mesh LODs", "Generate Mesh LODs during the import process.", null, null);
            public static readonly GUIContent MeshLodLimit = L10n.TextContent("Limit LODs", "Enable to limit the number of LODs that Unity generates.", null, null);
            public static readonly GUIContent MaximumMeshLod = L10n.TextContent("Maximum Level", "Enter the maximum index number of generated LODs.", null, null);
            public static readonly GUIContent Collision = L10n.TextContent("Collision", null, null, null);
            public static readonly GUIContent GenerateColliders = L10n.TextContent("Generate Colliders", "Should Unity generate mesh colliders for all meshes.", null, null);
            public static readonly GUIContent PreBakeConvexCollisionMesh = L10n.TextContent("Pre-bake Convex Collision Mesh", "Enable this property if the Mesh is used by a convex Mesh Collider.", null, null);
            public static readonly GUIContent PreBakeTriangleCollisionMesh = L10n.TextContent("Pre-bake Triangle Collision Mesh", "Enable this property if the Mesh is used by a non convex Mesh Collider.", null, null);
            public static readonly GUIContent MeshLodDiscardOddLevels = L10n.TextContent("Discard Odd Levels", "Limits the number of generated LODs by discarding all odd LOD indices.", null, null);

            public static readonly GUIContent Geometry = L10n.TextContent("Geometry", "Detailed mesh data", null, null);
            public static readonly GUIContent KeepQuads = L10n.TextContent("Keep Quads", "If model contains quad faces, they are kept for DX11 tessellation.", null, null);
            public static readonly GUIContent WeldVertices = L10n.TextContent("Weld Vertices", "Combine vertices that share the same position in space.", null, null);
            public static readonly GUIContent IndexFormatLabel = L10n.TextContent("Index Format", "Format of mesh index buffer. Auto mode picks 16 or 32 bit depending on mesh vertex count.", null, null);

            public static readonly GUIContent NormalsLabel = L10n.TextContent("Normals", "Source of mesh normals. If Import is selected and a mesh has no normals, they will be calculated instead.", null, null);
            public static readonly GUIContent RecalculateNormalsLabel = L10n.TextContent("Normals Mode", "How to weight faces when calculating normals.", null, null);
            public static readonly GUIContent SmoothingAngle = L10n.TextContent("Smoothing Angle", "When calculating normals on a mesh that doesn't have smoothing groups, edges between faces will be smooth if this value is greater than the angle between the faces.", null, null);

            public static readonly GUIContent TangentsLabel = L10n.TextContent("Tangents", "Source of mesh tangents. If Import is selected and a mesh has no tangents, they will be calculated instead.", null, null);

            public static readonly GUIContent BlendShapeNormalsLabel = L10n.TextContent("Blend Shape Normals", "Source of blend shape normals. If Import is selected and a blend shape has no normals, they will be calculated instead.", null, null);
            public static readonly GUIContent NormalSmoothingSourceLabel = L10n.TextContent("Smoothness Source", "How to determine which edges should be smooth and which should be sharp.", null, null);

            public static readonly GUIContent ImportUVs = L10n.TextContent("Import UVs", "Determine which UV channels are imported from the source model.", null, null);
            public static readonly GUIContent SwapUVChannels = L10n.TextContent("Swap UVs", "Swaps the 2 UV channels in meshes. Use if your diffuse texture uses UVs from the lightmap.", null, null);
            public static readonly GUIContent GenerateSecondaryUV = L10n.TextContent("Generate Lightmap UVs", "Generate lightmap UVs into UV1.", null, null);
            public static readonly GUIContent GenerateSecondaryUVAdvanced = L10n.TextContent("Lightmap UVs settings", "Advanced settings for Lightmap UVs generation", null, null);

            public static readonly GUIContent secondaryUVAngleDistortion       = L10n.TextContent("Angle Error", "Measured in percents. Angle error measures deviation of UV angles from geometry angles. Area error measures deviation of UV triangles area from geometry triangles if they were uniformly scaled.", null, null);
            public static readonly GUIContent secondaryUVAreaDistortion        = L10n.TextContent("Area Error", null, null, null);
            public static readonly GUIContent secondaryUVHardAngle             = L10n.TextContent("Hard Angle", "Angle between neighbor triangles that will generate seam.", null, null);
            public static readonly GUIContent secondaryUVMarginMethod          = L10n.TextContent("Margin Method", "Method to handle margins between UV charts.", null, null);
            public static readonly GUIContent secondaryUVPackMargin            = L10n.TextContent("Pack Margin", "Measured in pixels, assuming mesh will cover an entire 1024x1024 lightmap.", null, null);
            public static readonly GUIContent secondaryUVMinLightmapResolution = L10n.TextContent("Min Lightmap Resolution", "The minimum lightmap resolution at which this object will be used. Used to determine a packing which ensures no texel bleeding.", null, null);
            public static readonly GUIContent secondaryUVMinObjectScale        = L10n.TextContent("Min Object Scale", "The smallest scale at which this mesh will be used. Used to determine a packing which ensures no texel bleeding.", null, null);

            public static readonly GUIContent secondaryUVMinLightmapResolutionNotice = EditorGUIUtility.TrTextContent("The active scene's Lightmap Resolution is less than the specified Min Lightmap Resolution.", EditorGUIUtility.GetHelpIcon(MessageType.Info));

            public static readonly GUIContent ImportVertexColors = L10n.TextContent("Import Vertex Colors", "Import the vertex colors channel from the source model.", null, null);

            public static readonly GUIContent LegacyComputeNormalsFromSmoothingGroupsWhenMeshHasBlendShapes = L10n.TextContent("Legacy Blend Shape Normals", "Compute normals from smoothing groups when the mesh has BlendShapes.", null, null);
            public static readonly GUIContent BakeAxisConversion = L10n.TextContent("Bake Axis Conversion", "Perform axis conversion on all content for models defined in an axis system that differs from Unity's (left handed, Z forward, Y-up).", null, null);
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

            EditorGUILayout.LabelField(Styles.Collision, EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(m_AddColliders, Styles.GenerateColliders);
            EditorGUILayout.PropertyField(m_PreBakeConvexCollisionMesh, Styles.PreBakeConvexCollisionMesh);
            EditorGUILayout.PropertyField(m_PreBakeTriangleCollisionMesh, Styles.PreBakeTriangleCollisionMesh);

            if (m_AddColliders.boolValue && !m_PreBakeTriangleCollisionMesh.boolValue && !m_IsReadable.boolValue)
            {
                if (InternalEditorUtility.DrawWarningHelpBoxWithButton(
                    L10n.TextContent("Generating Mesh Colliders is enabled but Read/Write and pre-bake triangle collision are both disabled. In future versions of Unity, the build process will require any of these 2 options to be enabled to generate collision data.", null, null, null),
                    L10n.TextContent("Enable Pre-bake Collision", null, null, null), 200.0f))
                {
                    m_PreBakeTriangleCollisionMesh.boolValue = true;
                }
            }

            EditorGUILayout.LabelField(Styles.MeshLods, EditorStyles.boldLabel);

            using (new EditorGUI.DisabledGroupScope(m_KeepQuads.boolValue))
            {
                EditorGUILayout.PropertyField(m_GenerateMeshLods, Styles.GenerateMeshLods);

                using (new EditorGUI.IndentLevelScope())
                {
                    if (m_GenerateMeshLods.boolValue)
                    {
                        EditorGUILayout.HelpBox(Styles.MeshLodsInfoBox);

                        EditorGUI.BeginChangeCheck();

                        bool discardOddLevelsFlag = (m_MeshLodGenerationFlags.intValue &
                                                     (int) MeshLodUtility.LodGenerationFlags.DiscardOddLevels) != 0;
                        discardOddLevelsFlag =
                            EditorGUILayout.Toggle(Styles.MeshLodDiscardOddLevels, discardOddLevelsFlag);

                        if (EditorGUI.EndChangeCheck())
                        {
                            m_MeshLodGenerationFlags.intValue = discardOddLevelsFlag
                                ? m_MeshLodGenerationFlags.intValue | (int)MeshLodUtility.LodGenerationFlags.DiscardOddLevels
                                : m_MeshLodGenerationFlags.intValue & ~(int)MeshLodUtility.LodGenerationFlags.DiscardOddLevels;
                        }

                        EditorGUI.BeginChangeCheck();

                        var shouldShowField = EditorGUILayout.Toggle(Styles.MeshLodLimit, m_MaximumMeshLod.intValue != -1);

                        if (EditorGUI.EndChangeCheck())
                        {
                            if (shouldShowField)
                                m_MaximumMeshLod.intValue = 32;
                            else
                                m_MaximumMeshLod.intValue = -1;
                        }

                        if (shouldShowField)
                        {
                            using (new EditorGUI.IndentLevelScope())
                            {
                                var newValue = EditorGUILayout.IntField(Styles.MaximumMeshLod, m_MaximumMeshLod.intValue);
                                m_MaximumMeshLod.intValue = Mathf.Max(newValue, 0);
                            }
                        }
                    }
                }
            }
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
                                : GUIContent.Temp(string.Format(L10n.Tr("1{0} (File) to {1}m (Unity)", null), m_FileScaleUnit.stringValue, m_FileScaleFactor.floatValue));
                            EditorGUILayout.LabelField(content, GUILayout.ExpandWidth(true));
                        }
                        else
                        {
                            GUIContent content = m_FileScaleUnit.hasMultipleDifferentValues
                                ? EditorGUI.mixedValueContent
                                : GUIContent.Temp(string.Format(L10n.Tr("1 unit (File) to {0}m (Unity)", null), m_FileScale.floatValue));
                            EditorGUILayout.LabelField(content);
                        }
                    }
                }
            }

            EditorGUILayout.PropertyField(m_BakeAxisConversion, Styles.BakeAxisConversion);
            EditorGUILayout.PropertyField(m_ImportBlendShapes, Styles.ImportBlendShapes);

            if(m_ImportBlendShapes.boolValue)
                EditorGUILayout.PropertyField(m_ImportBlendShapeDeformPercent, Styles.ImportBlendShapesDeformPercent);

            EditorGUILayout.PropertyField(m_ImportVisibility, Styles.ImportVisibility);
            EditorGUILayout.PropertyField(m_ImportCameras, Styles.ImportCameras);
            EditorGUILayout.PropertyField(m_ImportLights, Styles.ImportLights);
            EditorGUILayout.PropertyField(m_PreserveHierarchy, Styles.PreserveHierarchy);
            EditorGUILayout.PropertyField(m_SortHierarchyByName, Styles.SortHierarchyByName);
        }

        protected void GeometryGUI()
        {
            GUILayout.Label(Styles.Geometry, EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(m_GenerateMeshLods.boolValue))
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

            EditorGUILayout.PropertyField(m_ImportVertexColors, Styles.ImportVertexColors);

            EditorGUILayout.PropertyField(m_StrictVertexDataChecks, Styles.StrictVertexDataChecks);
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

            EditorGUI.BeginChangeCheck();
            var importUVs = (ModelImporterUVs)EditorGUILayout.EnumFlagsField(Styles.ImportUVs, (ModelImporterUVs)m_ImportUVs.intValue);
            if (EditorGUI.EndChangeCheck())
            {
                m_ImportUVs.intValue = (int)importUVs;
            }

            var swappedUVs = ModelImporterUVs.UV0 | ModelImporterUVs.UV1;
            if ((importUVs & swappedUVs) == swappedUVs)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_SwapUVChannels, Styles.SwapUVChannels);
                EditorGUI.indentLevel--;
            }
        }
    }
}
