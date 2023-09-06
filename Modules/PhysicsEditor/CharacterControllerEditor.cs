// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.AnimatedValues;

namespace UnityEditor
{
    [CustomEditor(typeof(CharacterController))]
    [CanEditMultipleObjects]
    internal class CharacterControllerEditor : Editor
    {
        SerializedProperty m_Height;
        SerializedProperty m_Radius;
        SerializedProperty m_SlopeLimit;
        SerializedProperty m_StepOffset;
        SerializedProperty m_SkinWidth;
        SerializedProperty m_MinMoveDistance;
        SerializedProperty m_Center;

        private SerializedProperty m_LayerOverridePriority;
        private SerializedProperty m_IncludeLayers;
        private SerializedProperty m_ExcludeLayers;

        private readonly AnimBool m_ShowLayerOverrides = new AnimBool();
        private SavedBool m_ShowLayerOverridesFoldout;

        private int m_HandleControlID;

        private CharacterController m_CharacterController;

        public void OnEnable()
        {
            m_CharacterController = (CharacterController)target;

            m_Height = serializedObject.FindProperty("m_Height");
            m_Radius = serializedObject.FindProperty("m_Radius");
            m_SlopeLimit = serializedObject.FindProperty("m_SlopeLimit");
            m_StepOffset = serializedObject.FindProperty("m_StepOffset");
            m_SkinWidth = serializedObject.FindProperty("m_SkinWidth");
            m_MinMoveDistance = serializedObject.FindProperty("m_MinMoveDistance");
            m_Center = serializedObject.FindProperty("m_Center");

            m_ShowLayerOverrides.valueChanged.AddListener(Repaint);
            m_ShowLayerOverridesFoldout = new SavedBool($"{target.GetType() }.ShowLayerOverridesFoldout", false);
            m_ShowLayerOverrides.value = m_ShowLayerOverridesFoldout.value;

            m_LayerOverridePriority = serializedObject.FindProperty("m_LayerOverridePriority");
            m_IncludeLayers = serializedObject.FindProperty("m_IncludeLayers");
            m_ExcludeLayers = serializedObject.FindProperty("m_ExcludeLayers");

            m_HandleControlID = -1;
        }

        protected static class Styles
        {
            public static readonly GUIContent layerOverridePriority = EditorGUIUtility.TrTextContent("Layer Override Priority", "When 2 colliders have conflicting overrides, the settings of the collider with the higher priority are taken.");
            public static readonly GUIContent includeLayers = EditorGUIUtility.TrTextContent("Include Layers", "Layers to include when producing collisions");
            public static readonly GUIContent excludeLayers = EditorGUIUtility.TrTextContent("Exclude Layers", "Layers to exclude when producing collisions");
        }

        public override void OnInspectorGUI()
        {
            if(!m_CharacterController.isSupported)
                EditorGUILayout.HelpBox("Character Controller not supported with the current physics engine", MessageType.Error);

            using (new EditorGUI.DisabledScope(!m_CharacterController.isSupported))
            {
                serializedObject.Update();

                EditorGUILayout.PropertyField(m_SlopeLimit);
                EditorGUILayout.PropertyField(m_StepOffset);
                EditorGUILayout.PropertyField(m_SkinWidth);
                EditorGUILayout.PropertyField(m_MinMoveDistance);

                EditorGUILayout.PropertyField(m_Center);
                EditorGUILayout.PropertyField(m_Radius);
                EditorGUILayout.PropertyField(m_Height);

                ShowLayerOverridesProperties();

                serializedObject.ApplyModifiedProperties();
            }
        }

        public void OnSceneGUI()
        {
            if (!target)
                return;

           if (!m_CharacterController.isSupported)
               return;

            bool dragging = GUIUtility.hotControl == m_HandleControlID;

            // Use our own color for handles
            Color tempColor = Handles.color;
            if (m_CharacterController.enabled)
                Handles.color = Handles.s_ColliderHandleColor;
            else
                Handles.color = Handles.s_ColliderHandleColorDisabled;

            bool orgGuiEnabled = GUI.enabled;
            if (!Event.current.shift && !dragging)
            {
                GUI.enabled = false;
                Handles.color = new Color(1, 0, 0, .001f);
            }

            float height = m_CharacterController.height * m_CharacterController.transform.lossyScale.y;
            float radius = m_CharacterController.radius * Mathf.Max(m_CharacterController.transform.lossyScale.x, m_CharacterController.transform.lossyScale.z);
            height = Mathf.Max(height, radius * 2);

            Matrix4x4 matrix = Matrix4x4.TRS(m_CharacterController.transform.TransformPoint(m_CharacterController.center), Quaternion.identity, Vector3.one);

            int prevHotControl = GUIUtility.hotControl;

            // Height  (two handles)
            Vector3 halfHeight = Vector3.up * height * 0.5f;
            float adjusted = SizeHandle(halfHeight, Vector3.up, matrix, true);
            if (!GUI.changed)
                adjusted = SizeHandle(-halfHeight, Vector3.down, matrix, true);
            if (GUI.changed)
            {
                Undo.RecordObject(m_CharacterController, "Character Controller Resize");
                float heightScale = height / m_CharacterController.height;
                m_CharacterController.height += adjusted / heightScale;
            }

            // Radius  (four handles)
            adjusted = SizeHandle(Vector3.left * radius, Vector3.left, matrix, true);
            if (!GUI.changed)
                adjusted = SizeHandle(-Vector3.left * radius, -Vector3.left, matrix, true);
            if (!GUI.changed)
                adjusted = SizeHandle(Vector3.forward * radius, Vector3.forward, matrix, true);
            if (!GUI.changed)
                adjusted = SizeHandle(-Vector3.forward * radius, -Vector3.forward, matrix, true);
            if (GUI.changed)
            {
                Undo.RecordObject(m_CharacterController, "Character Controller Resize");
                float radiusScale = radius / m_CharacterController.radius;
                m_CharacterController.radius += adjusted / radiusScale;
            }

            // Detect if any of our handles got hotcontrol
            if (prevHotControl != GUIUtility.hotControl && GUIUtility.hotControl != 0)
                m_HandleControlID = GUIUtility.hotControl;

            if (GUI.changed)
            {
                const float minValue = 0.00001f;
                m_CharacterController.radius = Mathf.Max(m_CharacterController.radius, minValue);
                m_CharacterController.height = Mathf.Max(m_CharacterController.height, minValue);
            }

            // Reset original color
            Handles.color = tempColor;
            GUI.enabled = orgGuiEnabled;
        }

        private static float SizeHandle(Vector3 localPos, Vector3 localPullDir, Matrix4x4 matrix, bool isEdgeHandle)
        {
            Vector3 worldDir = matrix.MultiplyVector(localPullDir);
            Vector3 worldPos = matrix.MultiplyPoint(localPos);

            float handleSize = HandleUtility.GetHandleSize(worldPos);
            bool orgGUIchanged = GUI.changed;
            GUI.changed = false;
            Color tempColor = Handles.color;

            // Adjust color of handle if in background
            float displayThreshold = 0.0f;
            if (isEdgeHandle)
                displayThreshold = Mathf.Cos(Mathf.PI * 0.25f);
            float cosV;
            if (Camera.current.orthographic)
                cosV = Vector3.Dot(-Camera.current.transform.forward, worldDir);
            else
                cosV = Vector3.Dot((Camera.current.transform.position - worldPos).normalized, worldDir);
            if (cosV < -displayThreshold)
                Handles.color = new Color(Handles.color.r, Handles.color.g, Handles.color.b, Handles.color.a * Handles.backfaceAlphaMultiplier);

            // Now do handle
            Vector3 newWorldPos = Handles.Slider(worldPos, worldDir, handleSize * 0.03f, Handles.DotHandleCap, 0f);
            float adjust = 0.0f;
            if (GUI.changed)
            {
                // Project newWorldPos to worldDir  (the sign of the return value indicates if we growing or shrinking)
                adjust = HandleUtility.PointOnLineParameter(newWorldPos, worldPos, worldDir);
            }

            // Reset states
            GUI.changed |= orgGUIchanged;
            Handles.color = tempColor;

            return adjust;
        }

        protected void ShowLayerOverridesProperties()
        {
            // Show Layer Overrides.
            m_ShowLayerOverridesFoldout.value = m_ShowLayerOverrides.target = EditorGUILayout.Foldout(m_ShowLayerOverrides.target, "Layer Overrides", true);
            if (EditorGUILayout.BeginFadeGroup(m_ShowLayerOverrides.faded))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_LayerOverridePriority, Styles.layerOverridePriority);
                EditorGUILayout.PropertyField(m_IncludeLayers, Styles.includeLayers);
                EditorGUILayout.PropertyField(m_ExcludeLayers, Styles.excludeLayers);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();
        }
    }
}
