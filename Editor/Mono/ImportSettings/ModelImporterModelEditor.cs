// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using UnityEditor.Experimental.AssetImporters;

namespace UnityEditor
{
    internal class ModelImporterModelEditor : BaseAssetImporterTabUI
    {
        bool m_SecondaryUVAdvancedOptions = false;

        // Model
        SerializedProperty m_GlobalScale;
        SerializedProperty m_UseFileScale;
        SerializedProperty m_FileScale;
        SerializedProperty m_MeshCompression;
        SerializedProperty m_ImportBlendShapes;
        SerializedProperty m_AddColliders;
        SerializedProperty m_SwapUVChannels;
        SerializedProperty m_GenerateSecondaryUV;
        SerializedProperty m_SecondaryUVAngleDistortion;
        SerializedProperty m_SecondaryUVAreaDistortion;
        SerializedProperty m_SecondaryUVHardAngle;
        SerializedProperty m_SecondaryUVPackMargin;
        SerializedProperty m_NormalSmoothAngle;
        SerializedProperty m_NormalImportMode;
        SerializedProperty m_NormalCalculationMode;
        SerializedProperty m_TangentImportMode;
        SerializedProperty m_OptimizeMeshForGPU;
        SerializedProperty m_IsReadable;
        SerializedProperty m_KeepQuads;
        SerializedProperty m_IndexFormat;
        SerializedProperty m_WeldVertices;
        SerializedProperty m_ImportCameras;
        SerializedProperty m_ImportLights;
        SerializedProperty m_ImportVisibility;
        SerializedProperty m_PreserveHierarchy;

        public ModelImporterModelEditor(AssetImporterEditor panelContainer)
            : base(panelContainer)
        {
        }

        internal override void OnEnable()
        {
            // Model
            m_GlobalScale = serializedObject.FindProperty("m_GlobalScale");
            m_UseFileScale = serializedObject.FindProperty("m_UseFileScale");
            m_FileScale = serializedObject.FindProperty("m_FileScale");
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
            m_TangentImportMode = serializedObject.FindProperty("tangentImportMode");
            m_OptimizeMeshForGPU = serializedObject.FindProperty("optimizeMeshForGPU");
            m_IsReadable = serializedObject.FindProperty("m_IsReadable");
            m_KeepQuads = serializedObject.FindProperty("keepQuads");
            m_IndexFormat = serializedObject.FindProperty("indexFormat");
            m_WeldVertices = serializedObject.FindProperty("weldVertices");
            m_ImportVisibility = serializedObject.FindProperty("m_ImportVisibility");
            m_PreserveHierarchy = serializedObject.FindProperty("m_PreserveHierarchy");
        }

        internal override void PreApply()
        {
            ScaleAvatar();
        }

        class Styles
        {
            public GUIContent Meshes = EditorGUIUtility.TextContent("Meshes|These options control how geometry is imported.");
            public GUIContent ScaleFactor = EditorGUIUtility.TextContent("Scale Factor|How much to scale the models compared to what is in the source file.");
            public GUIContent UseFileUnits = EditorGUIUtility.TextContent("Use File Units|Detect file units and import as 1FileUnit=1UnityUnit, otherwise it will import as 1cm=1UnityUnit. See ModelImporter.useFileUnits for more details.");
            public GUIContent UseFileScale = EditorGUIUtility.TextContent("Use File Scale|Use File Scale when importing.");
            public GUIContent FileScaleFactor = EditorGUIUtility.TextContent("File Scale|Scale defined by source file, or 1 if Use File Scale is disabled. Click Apply to update.");
            public GUIContent ImportBlendShapes = EditorGUIUtility.TextContent("Import BlendShapes|Should Unity import BlendShapes.");
            public GUIContent GenerateColliders = EditorGUIUtility.TextContent("Generate Colliders|Should Unity generate mesh colliders for all meshes.");
            public GUIContent SwapUVChannels = EditorGUIUtility.TextContent("Swap UVs|Swaps the 2 UV channels in meshes. Use if your diffuse texture uses UVs from the lightmap.");

            public GUIContent GenerateSecondaryUV           = EditorGUIUtility.TextContent("Generate Lightmap UVs|Generate lightmap UVs into UV2.");
            public GUIContent GenerateSecondaryUVAdvanced   = EditorGUIUtility.TextContent("Advanced");
            public GUIContent secondaryUVAngleDistortion    = EditorGUIUtility.TextContent("Angle Error|Measured in percents. Angle error measures deviation of UV angles from geometry angles. Area error measures deviation of UV triangles area from geometry triangles if they were uniformly scaled.");
            public GUIContent secondaryUVAreaDistortion     = EditorGUIUtility.TextContent("Area Error");
            public GUIContent secondaryUVHardAngle          = EditorGUIUtility.TextContent("Hard Angle|Angle between neighbor triangles that will generate seam.");
            public GUIContent secondaryUVPackMargin         = EditorGUIUtility.TextContent("Pack Margin|Measured in pixels, assuming mesh will cover an entire 1024x1024 lightmap.");
            public GUIContent secondaryUVDefaults           = EditorGUIUtility.TextContent("Set Defaults");

            public GUIContent TangentSpace = EditorGUIUtility.TextContent("Normals & Tangents");
            public GUIContent TangentSpaceNormalLabel = EditorGUIUtility.TextContent("Normals");
            public GUIContent TangentSpaceTangentLabel = EditorGUIUtility.TextContent("Tangents");

            public GUIContent TangentSpaceOptionImport = EditorGUIUtility.TextContent("Import");
            public GUIContent TangentSpaceOptionCalculateLegacy = EditorGUIUtility.TextContent("Calculate Legacy");
            public GUIContent TangentSpaceOptionCalculateLegacySplit = EditorGUIUtility.TextContent("Calculate Legacy - Split Tangents");
            public GUIContent TangentSpaceOptionCalculate = EditorGUIUtility.TextContent("Calculate Tangent Space");
            public GUIContent TangentSpaceOptionNone = EditorGUIUtility.TextContent("None");
            public GUIContent TangentSpaceOptionNoneNoNormals = EditorGUIUtility.TextContent("None - (Normals required)");

            public GUIContent NormalOptionImport = EditorGUIUtility.TextContent("Import");
            public GUIContent NormalOptionCalculate = EditorGUIUtility.TextContent("Calculate");
            public GUIContent NormalOptionNone = EditorGUIUtility.TextContent("None");

            public GUIContent RecalculateNormalsLabel = EditorGUIUtility.TextContent("Normals Mode");
            public GUIContent[] RecalculateNormalsOpt =
            {
                EditorGUIUtility.TextContent("Unweighted Legacy"),
                EditorGUIUtility.TextContent("Unweighted"),
                EditorGUIUtility.TextContent("Area Weighted"),
                EditorGUIUtility.TextContent("Angle Weighted"),
                EditorGUIUtility.TextContent("Area and Angle Weighted")
            };

            public GUIContent[] TangentSpaceModeOptLabelsAll;
            public GUIContent[] TangentSpaceModeOptLabelsCalculate;
            public GUIContent[] TangentSpaceModeOptLabelsNone;

            public GUIContent[] NormalModeLabelsAll;


            public ModelImporterTangents[] TangentSpaceModeOptEnumsAll;
            public ModelImporterTangents[] TangentSpaceModeOptEnumsCalculate;
            public ModelImporterTangents[] TangentSpaceModeOptEnumsNone;

            public GUIContent SmoothingAngle = EditorGUIUtility.TextContent("Smoothing Angle|Normal Smoothing Angle");

            public GUIContent MeshCompressionLabel = new GUIContent("Mesh Compression" , "Higher compression ratio means lower mesh precision. If enabled, the mesh bounds and a lower bit depth per component are used to compress the mesh data.");
            public GUIContent[] MeshCompressionOpt =
            {
                new GUIContent("Off"),
                new GUIContent("Low"),
                new GUIContent("Medium"),
                new GUIContent("High")
            };

            public GUIContent IndexFormatLabel = new GUIContent("Index Format", "Format of mesh index buffer. Auto mode picks 16 or 32 bit depending on mesh vertex count.");
            public GUIContent[] IndexFormatOpt =
            {
                new GUIContent("Auto"),
                new GUIContent("16 bit"),
                new GUIContent("32 bit")
            };

            public GUIContent OptimizeMeshForGPU = EditorGUIUtility.TextContent("Optimize Mesh|The vertices and indices will be reordered for better GPU performance.");
            public GUIContent KeepQuads = EditorGUIUtility.TextContent("Keep Quads|If model contains quad faces, they are kept for DX11 tessellation.");
            public GUIContent WeldVertices = EditorGUIUtility.TextContent("Weld Vertices|Combine vertices that share the same position in space.");
            public GUIContent ImportVisibility = EditorGUIUtility.TextContent("Import Visibility|Use visibility properties to enable or disable MeshRenderer components.");
            public GUIContent ImportCameras = EditorGUIUtility.TextContent("Import Cameras");
            public GUIContent ImportLights = EditorGUIUtility.TextContent("Import Lights");
            public GUIContent PreserveHierarchy = EditorGUIUtility.TextContent("Preserve Hierarchy|Always create an explicit prefab root, even if the model only has a single root.");
            public GUIContent IsReadable = EditorGUIUtility.TextContent("Read/Write Enabled|Allow vertices and indices to be accessed from script.");

            public Styles()
            {
                NormalModeLabelsAll = new GUIContent[] {NormalOptionImport, NormalOptionCalculate, NormalOptionNone};

                TangentSpaceModeOptLabelsAll = new GUIContent[] {TangentSpaceOptionImport, TangentSpaceOptionCalculate, TangentSpaceOptionCalculateLegacy, TangentSpaceOptionCalculateLegacySplit, TangentSpaceOptionNone};
                TangentSpaceModeOptLabelsCalculate = new GUIContent[] {TangentSpaceOptionCalculate, TangentSpaceOptionCalculateLegacy, TangentSpaceOptionCalculateLegacySplit, TangentSpaceOptionNone};
                TangentSpaceModeOptLabelsNone = new GUIContent[] {TangentSpaceOptionNoneNoNormals};

                TangentSpaceModeOptEnumsAll = new ModelImporterTangents[] { ModelImporterTangents.Import, ModelImporterTangents.CalculateMikk, ModelImporterTangents.CalculateLegacy, ModelImporterTangents.CalculateLegacyWithSplitTangents, ModelImporterTangents.None };
                TangentSpaceModeOptEnumsCalculate = new ModelImporterTangents[] { ModelImporterTangents.CalculateMikk, ModelImporterTangents.CalculateLegacy, ModelImporterTangents.CalculateLegacyWithSplitTangents, ModelImporterTangents.None };
                TangentSpaceModeOptEnumsNone = new ModelImporterTangents[] { ModelImporterTangents.None };
            }
        }

        static Styles styles;

        public override void OnInspectorGUI()
        {
            // Material modes
            if (styles == null)
                styles = new Styles();

            MeshesGUI();
            NormalsAndTangentsGUI();
        }

        void MeshesGUI()
        {
            GUILayout.Label(styles.Meshes, EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(targets.Length > 1))
            {
                // Global scale aka User scale
                EditorGUILayout.PropertyField(m_GlobalScale, styles.ScaleFactor);
            }

            // File Scale Factor
            EditorGUILayout.PropertyField(m_UseFileScale, styles.UseFileScale);
            if (m_UseFileScale.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_FileScale, styles.FileScaleFactor);
                EditorGUI.indentLevel--;
            }

            // mesh compression
            EditorGUILayout.Popup(m_MeshCompression, styles.MeshCompressionOpt, styles.MeshCompressionLabel);

            EditorGUILayout.PropertyField(m_IsReadable, styles.IsReadable);
            EditorGUILayout.PropertyField(m_OptimizeMeshForGPU, styles.OptimizeMeshForGPU);

            // Import BlendShapes
            EditorGUILayout.PropertyField(m_ImportBlendShapes, styles.ImportBlendShapes);

            // Add Collider
            EditorGUILayout.PropertyField(m_AddColliders, styles.GenerateColliders);

            // Keep Quads
            EditorGUILayout.PropertyField(m_KeepQuads, styles.KeepQuads);

            EditorGUILayout.Popup(m_IndexFormat, styles.IndexFormatOpt, styles.IndexFormatLabel);

            // Weld Vertices
            EditorGUILayout.PropertyField(m_WeldVertices, styles.WeldVertices);

            // Import visibility
            EditorGUILayout.PropertyField(m_ImportVisibility, styles.ImportVisibility);

            // Import Cameras
            EditorGUILayout.PropertyField(m_ImportCameras, styles.ImportCameras);

            // Import Lights
            EditorGUILayout.PropertyField(m_ImportLights, styles.ImportLights);

            // Preserve Hierarchy
            EditorGUILayout.PropertyField(m_PreserveHierarchy, styles.PreserveHierarchy);

            // Swap uv channel
            EditorGUILayout.PropertyField(m_SwapUVChannels, styles.SwapUVChannels);

            // Secondary UV generation
            EditorGUILayout.PropertyField(m_GenerateSecondaryUV, styles.GenerateSecondaryUV);
            if (m_GenerateSecondaryUV.boolValue)
            {
                EditorGUI.indentLevel++;
                m_SecondaryUVAdvancedOptions = EditorGUILayout.Foldout(m_SecondaryUVAdvancedOptions, styles.GenerateSecondaryUVAdvanced, true, EditorStyles.foldout);
                if (m_SecondaryUVAdvancedOptions)
                {
                    // TODO: all slider min/max values should be revisited
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.Slider(m_SecondaryUVHardAngle, 0, 180, styles.secondaryUVHardAngle);
                    EditorGUILayout.Slider(m_SecondaryUVPackMargin, 1, 64, styles.secondaryUVPackMargin);
                    EditorGUILayout.Slider(m_SecondaryUVAngleDistortion, 1, 75, styles.secondaryUVAngleDistortion);
                    EditorGUILayout.Slider(m_SecondaryUVAreaDistortion, 1, 75, styles.secondaryUVAreaDistortion);
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_SecondaryUVHardAngle.floatValue = Mathf.Round(m_SecondaryUVHardAngle.floatValue);
                        m_SecondaryUVPackMargin.floatValue = Mathf.Round(m_SecondaryUVPackMargin.floatValue);
                        m_SecondaryUVAngleDistortion.floatValue = Mathf.Round(m_SecondaryUVAngleDistortion.floatValue);
                        m_SecondaryUVAreaDistortion.floatValue = Mathf.Round(m_SecondaryUVAreaDistortion.floatValue);
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        void NormalsAndTangentsGUI()
        {
            // Tangent space
            GUILayout.Label(styles.TangentSpace, EditorStyles.boldLabel);

            bool isTangentImportSupported = true;
            foreach (ModelImporter importer in targets)
                if (!importer.isTangentImportSupported)
                    isTangentImportSupported = false;

            // TODO : check if normal import is supported!
            //normalImportMode = styles.TangentSpaceModeOptEnumsAll[EditorGUILayout.Popup(styles.TangentSpaceNormalLabel, (int)normalImportMode, styles.TangentSpaceModeOptLabelsAll)];
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Popup(m_NormalImportMode, styles.NormalModeLabelsAll, styles.TangentSpaceNormalLabel);
            if (EditorGUI.EndChangeCheck())
            {
                // Let the tangent mode follow the normal mode - that's a sane default and it's needed
                // because the tangent mode value can't be lower than the normal mode.
                // We make the tangent mode follow in BOTH directions for consistency
                // - so that if you change the normal mode one way and then back, the tangent mode will also go back again.

                if (m_NormalImportMode.intValue == (int)ModelImporterNormals.None)
                    m_TangentImportMode.intValue = (int)ModelImporterTangents.None;
                else if (m_NormalImportMode.intValue == (int)ModelImporterNormals.Import && isTangentImportSupported)
                    m_TangentImportMode.intValue = (int)ModelImporterTangents.Import;
                else
                    m_TangentImportMode.intValue = (int)ModelImporterTangents.CalculateMikk;
            }

            // Normal split angle
            using (new EditorGUI.DisabledScope(m_NormalImportMode.intValue != (int)ModelImporterNormals.Calculate))
            {
                // Normal calculation mode
                EditorGUILayout.Popup(m_NormalCalculationMode, styles.RecalculateNormalsOpt, styles.RecalculateNormalsLabel);

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.Slider(m_NormalSmoothAngle, 0, 180, styles.SmoothingAngle);
                // Property is serialized as float but we want to show it as an int so we round the value when changed
                if (EditorGUI.EndChangeCheck())
                    m_NormalSmoothAngle.floatValue = Mathf.Round(m_NormalSmoothAngle.floatValue);
            }

            // Choose the option values and labels based on what the NormalImportMode is
            GUIContent[] tangentImportModeOptLabels = styles.TangentSpaceModeOptLabelsAll;
            ModelImporterTangents[] tangentImportModeOptEnums = styles.TangentSpaceModeOptEnumsAll;
            if (m_NormalImportMode.intValue == (int)ModelImporterNormals.Calculate || !isTangentImportSupported)
            {
                tangentImportModeOptLabels = styles.TangentSpaceModeOptLabelsCalculate;
                tangentImportModeOptEnums = styles.TangentSpaceModeOptEnumsCalculate;
            }
            else if (m_NormalImportMode.intValue == (int)ModelImporterNormals.None)
            {
                tangentImportModeOptLabels = styles.TangentSpaceModeOptLabelsNone;
                tangentImportModeOptEnums = styles.TangentSpaceModeOptEnumsNone;
            }

            using (new EditorGUI.DisabledScope(m_NormalImportMode.intValue == (int)ModelImporterNormals.None))
            {
                int tangentOption = Array.IndexOf(tangentImportModeOptEnums, (ModelImporterTangents)m_TangentImportMode.intValue);
                EditorGUI.BeginChangeCheck();
                tangentOption = EditorGUILayout.Popup(styles.TangentSpaceTangentLabel, tangentOption, tangentImportModeOptLabels);
                if (EditorGUI.EndChangeCheck())
                    m_TangentImportMode.intValue = (int)tangentImportModeOptEnums[tangentOption];
            }
        }

        private void ScaleAvatar()
        {
            //You can't scale multiple
            if (m_GlobalScale.hasMultipleDifferentValues)
                return;

            if (targets.Length != 1)
                return;

            foreach (object o in targets)
            {
                float prevScale = (o as ModelImporter).globalScale;
                float currentScale = m_GlobalScale.floatValue;

                if (prevScale != currentScale && currentScale != 0 && prevScale != 0)
                {
                    float scaleFactor = currentScale / prevScale;
                    SerializedProperty skeletonBoneArray = serializedObject.FindProperty(AvatarSetupTool.sSkeleton);
                    for (int i = 0; i < skeletonBoneArray.arraySize; ++i)
                    {
                        SerializedProperty bone = skeletonBoneArray.GetArrayElementAtIndex(i);
                        bone.FindPropertyRelative(AvatarSetupTool.sPosition).vector3Value =
                            bone.FindPropertyRelative(AvatarSetupTool.sPosition).vector3Value * scaleFactor;
                    }
                }
            }
        }
    }
}
