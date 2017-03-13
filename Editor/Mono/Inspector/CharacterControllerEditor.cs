// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

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

        private int m_HandleControlID;

        public void OnEnable()
        {
            m_Height = serializedObject.FindProperty("m_Height");
            m_Radius = serializedObject.FindProperty("m_Radius");
            m_SlopeLimit = serializedObject.FindProperty("m_SlopeLimit");
            m_StepOffset = serializedObject.FindProperty("m_StepOffset");
            m_SkinWidth = serializedObject.FindProperty("m_SkinWidth");
            m_MinMoveDistance = serializedObject.FindProperty("m_MinMoveDistance");
            m_Center = serializedObject.FindProperty("m_Center");

            m_HandleControlID = -1;
        }

        public void OnDisable()
        {
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_SlopeLimit);
            EditorGUILayout.PropertyField(m_StepOffset);
            EditorGUILayout.PropertyField(m_SkinWidth);
            EditorGUILayout.PropertyField(m_MinMoveDistance);

            EditorGUILayout.PropertyField(m_Center);
            EditorGUILayout.PropertyField(m_Radius);
            EditorGUILayout.PropertyField(m_Height);

            serializedObject.ApplyModifiedProperties();
        }

        public void OnSceneGUI()
        {
            bool dragging = GUIUtility.hotControl == m_HandleControlID;

            CharacterController cc = (CharacterController)target;

            // Use our own color for handles
            Color tempColor = Handles.color;
            if (cc.enabled)
                Handles.color = Handles.s_ColliderHandleColor;
            else
                Handles.color = Handles.s_ColliderHandleColorDisabled;

            bool orgGuiEnabled = GUI.enabled;
            if (!Event.current.shift && !dragging)
            {
                GUI.enabled = false;
                Handles.color = new Color(1, 0, 0, .001f);
            }

            float height = cc.height * cc.transform.lossyScale.y;
            float radius = cc.radius * Mathf.Max(cc.transform.lossyScale.x, cc.transform.lossyScale.z);
            height = Mathf.Max(height, radius * 2);

            Matrix4x4 matrix = Matrix4x4.TRS(cc.transform.TransformPoint(cc.center), Quaternion.identity, Vector3.one);

            int prevHotControl = GUIUtility.hotControl;

            // Height  (two handles)
            Vector3 halfHeight = Vector3.up * height * 0.5f;
            float adjusted = SizeHandle(halfHeight, Vector3.up, matrix, true);
            if (!GUI.changed)
                adjusted = SizeHandle(-halfHeight, Vector3.down, matrix, true);
            if (GUI.changed)
            {
                Undo.RecordObject(cc, "Character Controller Resize");
                float heightScale = height / cc.height;
                cc.height += adjusted / heightScale;
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
                Undo.RecordObject(cc, "Character Controller Resize");
                float radiusScale = radius / cc.radius;
                cc.radius += adjusted / radiusScale;
            }

            // Detect if any of our handles got hotcontrol
            if (prevHotControl != GUIUtility.hotControl && GUIUtility.hotControl != 0)
                m_HandleControlID = GUIUtility.hotControl;

            if (GUI.changed)
            {
                const float minValue = 0.00001f;
                cc.radius = Mathf.Max(cc.radius, minValue);
                cc.height = Mathf.Max(cc.height, minValue);
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
    }
}
