// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;

namespace UnityEditor
{
    internal class SketchUpImporterModelEditor : ModelImporterModelEditor
    {
        enum EFileUnit
        {
            Meters,
            Centimeters,
            Millimeters,
            Feet,
            Inches
        }

        const float kInchToMeter = 0.0254f;

        SerializedProperty m_GenerateBackFace;
        SerializedProperty m_MergeCoplanarFaces;
        SerializedProperty m_FileUnit;
        SerializedProperty m_GlobalScale;
        SerializedProperty m_Latitude;
        SerializedProperty m_Longitude;
        SerializedProperty m_NorthCorrection;
        SerializedProperty m_SelectedNodes;
        SketchUpImporter m_Target;

        float lengthToUnit = 0;

        public SketchUpImporterModelEditor(AssetImporterEditor panelContainer)
            : base(panelContainer)
        {}

        internal override void OnEnable()
        {
            m_GenerateBackFace = serializedObject.FindProperty("m_GenerateBackFace");
            m_MergeCoplanarFaces = serializedObject.FindProperty("m_MergeCoplanarFaces");
            m_FileUnit = serializedObject.FindProperty("m_FileUnit");
            m_GlobalScale = serializedObject.FindProperty("m_GlobalScale");
            m_Latitude = serializedObject.FindProperty("m_Latitude");
            m_Longitude = serializedObject.FindProperty("m_Longitude");
            m_NorthCorrection = serializedObject.FindProperty("m_NorthCorrection");
            m_SelectedNodes = serializedObject.FindProperty("m_SelectedNodes");
            m_Target = target as SketchUpImporter;
            base.OnEnable();
        }

        static float CovertUnitToGlobalScale(EFileUnit measurement, float unit)
        {
            switch (measurement)
            {
                case EFileUnit.Meters: // meter
                    return kInchToMeter * unit;

                case EFileUnit.Centimeters: //centimeter
                    return (kInchToMeter * 0.01f) * unit;

                case EFileUnit.Millimeters: //millimeter
                    return unit * (kInchToMeter * 0.001f);

                case EFileUnit.Feet: //feet
                    return unit * (kInchToMeter * 0.3048f);

                case EFileUnit.Inches: //inches
                    return unit;
            }
            Debug.LogError("File Unit value is invalid");
            return 1.0f;
        }

        static float ConvertGlobalScaleToUnit(EFileUnit measurement, float globalScale)
        {
            switch (measurement)
            {
                case EFileUnit.Meters: // meter
                    return globalScale / kInchToMeter;

                case EFileUnit.Centimeters: //centimeter
                    return globalScale / (kInchToMeter * 0.01f);

                case EFileUnit.Millimeters: //millimeter
                    return globalScale / (kInchToMeter * 0.001f);

                case EFileUnit.Feet: //feet
                    return globalScale / (kInchToMeter * 0.3048f);

                case EFileUnit.Inches: //inches
                    return globalScale;
            }
            Debug.LogError("File Unit value is invalid");
            return 1.0f;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField(Styles.sketchUpLabel, EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(m_GenerateBackFace, Styles.generateBackFaceLabel);
            EditorGUILayout.PropertyField(m_MergeCoplanarFaces, Styles.mergeCoplanarFaces);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(Styles.fileUnitLabel);
            var oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            GUILayout.Label("1");
            EditorGUILayout.Popup(m_FileUnit, Styles.measurementOptions, GUIContent.Temp(""), GUILayout.MaxWidth(100));
            lengthToUnit = ConvertGlobalScaleToUnit((EFileUnit)m_FileUnit.intValue, m_GlobalScale.floatValue);
            GUILayout.Label("=");
            lengthToUnit = EditorGUILayout.FloatField(lengthToUnit);
            m_GlobalScale.floatValue = CovertUnitToGlobalScale((EFileUnit)m_FileUnit.intValue, lengthToUnit);
            EditorGUI.indentLevel = oldIndent;
            EditorGUILayout.EndHorizontal();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.FloatField(Styles.longitudeLabel, m_Longitude.floatValue);
                EditorGUILayout.FloatField(Styles.latitudeLabel, m_Latitude.floatValue);
                EditorGUILayout.FloatField(Styles.northCorrectionLabel, m_NorthCorrection.floatValue);
            }

            if (assetTarget == null)
            {
                EditorGUILayout.PropertyField(m_SelectedNodes);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                var size = GUI.skin.button.CalcSize(Styles.selectNodeButton);
                var rect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight, GUI.skin.button, GUILayout.Width(size.x + EditorGUI.indent)));
                if (GUI.Button(rect, Styles.selectNodeButton))
                {
                    SketchUpNodeInfo[] nodes = m_Target.GetNodes();
                    SketchUpImportDlg.Launch(nodes, this);
                    GUIUtility.ExitGUI();
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.PropertyField(m_ImportCameras, ModelImporterModelEditor.Styles.ImportCameras);

            MeshesGUI();

            EditorGUILayout.LabelField(ModelImporterModelEditor.Styles.Geometry, EditorStyles.boldLabel);

            using (var horizontal = new EditorGUILayout.HorizontalScope())
            {
                using (var prop = new EditorGUI.PropertyScope(horizontal.rect, ModelImporterModelEditor.Styles.IndexFormatLabel, m_IndexFormat))
                {
                    EditorGUI.BeginChangeCheck();
                    var newValue = (int)(ModelImporterIndexFormat)EditorGUILayout.EnumPopup(prop.content, (ModelImporterIndexFormat)m_IndexFormat.intValue);
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_IndexFormat.intValue = newValue;
                    }
                }
            }

            UvsGUI();
        }

        public void SetSelectedNodes(int[] selectedNodes)
        {
            if (selectedNodes == null)
                return;
            m_SelectedNodes.serializedObject.Update();
            m_SelectedNodes.ClearArray();
            for (int i = 0; i < selectedNodes.Length; ++i)
            {
                m_SelectedNodes.InsertArrayElementAtIndex(i);
                SerializedProperty sp = m_SelectedNodes.GetArrayElementAtIndex(i);
                sp.intValue = selectedNodes[i];
            }
            m_SelectedNodes.serializedObject.ApplyModifiedProperties();
        }

        new static class Styles
        {
            static public readonly GUIContent sketchUpLabel = EditorGUIUtility.TrTextContent("SketchUp", "SketchUp import settings");
            static public readonly GUIContent generateBackFaceLabel = EditorGUIUtility.TrTextContent("Generate Back Face", "Enable/disable generation of back facing polygons");
            static public readonly GUIContent mergeCoplanarFaces = EditorGUIUtility.TrTextContent("Merge Coplanar Faces", "Enable/disable merging of coplanar faces when generating meshes");
            static public readonly GUIContent selectNodeButton = EditorGUIUtility.TrTextContent("Select Nodes...", "Brings up the node selection dialog box");
            static public readonly GUIContent fileUnitLabel = EditorGUIUtility.TrTextContent("Unit conversion", "Length measurement to unit conversion. The value in Scale Factor is calculated based on the value here");
            static public readonly GUIContent longitudeLabel = EditorGUIUtility.TrTextContent("Longitude", "Longitude Geo-location");
            static public readonly GUIContent latitudeLabel = EditorGUIUtility.TrTextContent("Latitude", "Latitude Geo-location");
            static public readonly GUIContent northCorrectionLabel = EditorGUIUtility.TrTextContent("North Correction", "The angle which will rotate the north direction to the z-axis for the model");
            static public readonly GUIContent[] measurementOptions =
            {
                EditorGUIUtility.TrTextContent("Meters"),
                EditorGUIUtility.TrTextContent("Centimeters"),
                EditorGUIUtility.TrTextContent("Millimeters"),
                EditorGUIUtility.TrTextContent("Feet"),
                EditorGUIUtility.TrTextContent("Inches"),
            };
        }
    }
}
