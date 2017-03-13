// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.AnimatedValues;

namespace UnityEditor
{
    /// <summary>
    /// Prompts the end-user to add 2D colliders if non exist for 2D effector to work with.
    /// </summary>
    [CustomEditor(typeof(BuoyancyEffector2D), true)]
    [CanEditMultipleObjects]
    internal class BuoyancyEffector2DEditor : Effector2DEditor
    {
        SerializedProperty m_Density;
        SerializedProperty m_SurfaceLevel;

        static readonly AnimBool m_ShowDampingRollout = new AnimBool();
        SerializedProperty m_LinearDrag;
        SerializedProperty m_AngularDrag;

        static readonly AnimBool m_ShowFlowRollout = new AnimBool();
        SerializedProperty m_FlowAngle;
        SerializedProperty m_FlowMagnitude;
        SerializedProperty m_FlowVariation;

        public override void OnEnable()
        {
            base.OnEnable();

            m_Density = serializedObject.FindProperty("m_Density");
            m_SurfaceLevel = serializedObject.FindProperty("m_SurfaceLevel");

            m_ShowDampingRollout.valueChanged.AddListener(Repaint);
            m_LinearDrag = serializedObject.FindProperty("m_LinearDrag");
            m_AngularDrag = serializedObject.FindProperty("m_AngularDrag");

            m_ShowFlowRollout.valueChanged.AddListener(Repaint);
            m_FlowAngle = serializedObject.FindProperty("m_FlowAngle");
            m_FlowMagnitude = serializedObject.FindProperty("m_FlowMagnitude");
            m_FlowVariation = serializedObject.FindProperty("m_FlowVariation");
        }

        public override void OnDisable()
        {
            base.OnDisable();

            m_ShowDampingRollout.valueChanged.RemoveListener(Repaint);
            m_ShowFlowRollout.valueChanged.RemoveListener(Repaint);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Density);
            EditorGUILayout.PropertyField(m_SurfaceLevel);

            // Drag.
            m_ShowDampingRollout.target = EditorGUILayout.Foldout(m_ShowDampingRollout.target, "Damping", true);
            if (EditorGUILayout.BeginFadeGroup(m_ShowDampingRollout.faded))
            {
                EditorGUILayout.PropertyField(m_LinearDrag);
                EditorGUILayout.PropertyField(m_AngularDrag);
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFadeGroup();

            // Flow.
            m_ShowFlowRollout.target = EditorGUILayout.Foldout(m_ShowFlowRollout.target, "Flow", true);
            if (EditorGUILayout.BeginFadeGroup(m_ShowFlowRollout.faded))
            {
                EditorGUILayout.PropertyField(m_FlowAngle);
                EditorGUILayout.PropertyField(m_FlowMagnitude);
                EditorGUILayout.PropertyField(m_FlowVariation);
            }
            EditorGUILayout.EndFadeGroup();

            serializedObject.ApplyModifiedProperties();
        }

        public void OnSceneGUI()
        {
            var effector = (BuoyancyEffector2D)target;

            // Ignore disabled effector.
            if (!effector.enabled)
                return;


            var effectorPosition = effector.transform.position;
            var surfaceY = effectorPosition.y + (effector.transform.lossyScale.y * effector.surfaceLevel);

            var intersections = new List<Vector3>();
            var farLeft = float.NegativeInfinity;
            var farRight = farLeft;

            // Fetch all the effector-collider bounds.
            foreach (var c in effector.gameObject.GetComponents<Collider2D>().Where(c => c.enabled && c.usedByEffector))
            {
                var b = c.bounds;
                var left = b.min.x;
                var right = b.max.x;
                if (float.IsNegativeInfinity(farLeft))
                {
                    farLeft = left;
                    farRight = right;
                }
                else
                {
                    if (left < farLeft)
                        farLeft = left;

                    if (right > farRight)
                        farRight = right;
                }

                var start = new Vector3(left, surfaceY, 0.0f);
                var end = new Vector3(right, surfaceY, 0.0f);

                intersections.Add(start);
                intersections.Add(end);
            }

            // Draw the overall surface.
            Handles.color = Color.red;
            Handles.DrawAAPolyLine(new Vector3[] { new Vector3(farLeft, surfaceY, 0.0f), new Vector3(farRight, surfaceY, 0.0f) });

            // Draw the collider intersections.
            Handles.color = Color.cyan;
            for (var i = 0; i < intersections.Count - 1; i = i + 2)
            {
                Handles.DrawAAPolyLine(intersections[i], intersections[i + 1]);
            }
        }
    }
}
