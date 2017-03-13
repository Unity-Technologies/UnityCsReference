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
    /// An editor for the Platform Effector.
    /// </summary>
    [CustomEditor(typeof(PlatformEffector2D), true)]
    [CanEditMultipleObjects]
    internal class PlatformEffector2DEditor : Effector2DEditor
    {
        SerializedProperty m_RotationalOffset;

        readonly AnimBool m_ShowOneWayRollout = new AnimBool();
        SerializedProperty m_UseOneWay;
        SerializedProperty m_UseOneWayGrouping;
        SerializedProperty m_SurfaceArc;

        static readonly AnimBool m_ShowSidesRollout = new AnimBool();
        SerializedProperty m_UseSideFriction;
        SerializedProperty m_UseSideBounce;
        SerializedProperty m_SideArc;

        public override void OnEnable()
        {
            base.OnEnable();

            m_RotationalOffset = serializedObject.FindProperty("m_RotationalOffset");

            m_ShowOneWayRollout.value = true;
            m_ShowOneWayRollout.valueChanged.AddListener(Repaint);
            m_UseOneWay = serializedObject.FindProperty("m_UseOneWay");
            m_UseOneWayGrouping = serializedObject.FindProperty("m_UseOneWayGrouping");
            m_SurfaceArc = serializedObject.FindProperty("m_SurfaceArc");

            m_ShowSidesRollout.valueChanged.AddListener(Repaint);
            m_UseSideFriction = serializedObject.FindProperty("m_UseSideFriction");
            m_UseSideBounce = serializedObject.FindProperty("m_UseSideBounce");
            m_SideArc = serializedObject.FindProperty("m_SideArc");
        }

        public override void OnDisable()
        {
            base.OnDisable();

            m_ShowOneWayRollout.valueChanged.RemoveListener(Repaint);
            m_ShowSidesRollout.valueChanged.RemoveListener(Repaint);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUILayout.PropertyField(m_RotationalOffset);

            // One-Way.
            m_ShowOneWayRollout.target = EditorGUILayout.Foldout(m_ShowOneWayRollout.target, "One Way", true);
            if (EditorGUILayout.BeginFadeGroup(m_ShowOneWayRollout.faded))
            {
                EditorGUILayout.PropertyField(m_UseOneWay);
                EditorGUILayout.PropertyField(m_UseOneWayGrouping);
                EditorGUILayout.PropertyField(m_SurfaceArc);
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFadeGroup();

            // Sides.
            m_ShowSidesRollout.target = EditorGUILayout.Foldout(m_ShowSidesRollout.target, "Sides", true);
            if (EditorGUILayout.BeginFadeGroup(m_ShowSidesRollout.faded))
            {
                EditorGUILayout.PropertyField(m_UseSideFriction);
                EditorGUILayout.PropertyField(m_UseSideBounce);
                EditorGUILayout.PropertyField(m_SideArc);
            }
            EditorGUILayout.EndFadeGroup();

            serializedObject.ApplyModifiedProperties();
        }

        public void OnSceneGUI()
        {
            var effector = (PlatformEffector2D)target;

            // Ignore disabled effector.
            if (!effector.enabled)
                return;

            if (effector.useOneWay)
                DrawSurfaceArc(effector);

            if (!effector.useSideBounce || !effector.useSideFriction)
                DrawSideArc(effector);
        }

        private static void DrawSurfaceArc(PlatformEffector2D effector)
        {
            // Calculate the surface angle in local-space.
            var rotation = -Mathf.Deg2Rad * effector.rotationalOffset;
            var localUp = effector.transform.TransformVector(new Vector3(Mathf.Sin(rotation), Mathf.Cos(rotation), 0.0f)).normalized;

            // If the transform has created a degenerate local then we cannot draw the gizmo!
            if (localUp.sqrMagnitude < Mathf.Epsilon)
                return;

            // Calculate the surface angle.
            var surfaceAngle = Mathf.Atan2(localUp.x, localUp.y);

            // Fetch the surface arc.
            var surfaceArc = Mathf.Clamp(effector.surfaceArc, 0.5f, 360.0f);
            var halfSurfaceArcRadians = surfaceArc * 0.5f * Mathf.Deg2Rad;

            var fromAngle = new Vector3(Mathf.Sin(surfaceAngle - halfSurfaceArcRadians), Mathf.Cos(surfaceAngle - halfSurfaceArcRadians), 0.0f);
            var toAngle = new Vector3(Mathf.Sin(surfaceAngle + halfSurfaceArcRadians), Mathf.Cos(surfaceAngle + halfSurfaceArcRadians), 0.0f);

            // Fetch all the effector-collider bounds.
            foreach (var collider in effector.gameObject.GetComponents<Collider2D>().Where(collider => collider.enabled && collider.usedByEffector))
            {
                var center = collider.bounds.center;
                var arcRadius = HandleUtility.GetHandleSize(center);

                // arc background
                Handles.color = new Color(0, 1, 1, 0.07f);
                Handles.DrawSolidArc(center, Vector3.back, fromAngle, surfaceArc, arcRadius);

                // arc frame
                Handles.color = new Color(0, 1, 1, 0.7f);
                Handles.DrawWireArc(center, Vector3.back, fromAngle, surfaceArc, arcRadius);
                Handles.DrawDottedLine(center, center + fromAngle * arcRadius, 5.0f);
                Handles.DrawDottedLine(center, center + toAngle * arcRadius, 5.0f);
            }
        }

        private static void DrawSideArc(PlatformEffector2D effector)
        {
            // Calculate the surface angle in local-space.
            var rotation = -Mathf.Deg2Rad * (90.0f + effector.rotationalOffset);
            var localSide = effector.transform.TransformVector(new Vector3(Mathf.Sin(rotation), Mathf.Cos(rotation), 0.0f)).normalized;

            // If the transform has created a degenerate local then we cannot draw the gizmo!
            if (localSide.sqrMagnitude < Mathf.Epsilon)
                return;

            // Calculate the side angles.
            var sideAngleLeft = Mathf.Atan2(localSide.x, localSide.y);
            var sideAngleRight = sideAngleLeft + Mathf.PI;

            // Fetch the side arc.
            var sideArc = Mathf.Clamp(effector.sideArc, 0.5f, 180.0f);
            var halfSideArcRadians = sideArc * 0.5f * Mathf.Deg2Rad;

            var fromAngleLeft = new Vector3(Mathf.Sin(sideAngleLeft - halfSideArcRadians), Mathf.Cos(sideAngleLeft - halfSideArcRadians), 0.0f);
            var toAngleLeft = new Vector3(Mathf.Sin(sideAngleLeft + halfSideArcRadians), Mathf.Cos(sideAngleLeft + halfSideArcRadians), 0.0f);
            var fromAngleRight = new Vector3(Mathf.Sin(sideAngleRight - halfSideArcRadians), Mathf.Cos(sideAngleRight - halfSideArcRadians), 0.0f);
            var toAngleRight = new Vector3(Mathf.Sin(sideAngleRight + halfSideArcRadians), Mathf.Cos(sideAngleRight + halfSideArcRadians), 0.0f);

            // Fetch all the effector-collider bounds.
            foreach (var collider in effector.gameObject.GetComponents<Collider2D>().Where(collider => collider.enabled && collider.usedByEffector))
            {
                var center = collider.bounds.center;
                var arcRadius = HandleUtility.GetHandleSize(center) * 0.8f;

                // arc background
                Handles.color = new Color(0, 1, 0.7f, 0.07f);
                Handles.DrawSolidArc(center, Vector3.back, fromAngleLeft, sideArc, arcRadius);
                Handles.DrawSolidArc(center, Vector3.back, fromAngleRight, sideArc, arcRadius);

                // arc frame
                Handles.color = new Color(0, 1, 0.7f, 0.7f);
                Handles.DrawWireArc(center, Vector3.back, fromAngleLeft, sideArc, arcRadius);
                Handles.DrawWireArc(center, Vector3.back, fromAngleRight, sideArc, arcRadius);
                Handles.DrawDottedLine(center, center + fromAngleLeft * arcRadius, 5.0f);
                Handles.DrawDottedLine(center, center + toAngleLeft * arcRadius, 5.0f);
                Handles.DrawDottedLine(center, center + fromAngleRight * arcRadius, 5.0f);
                Handles.DrawDottedLine(center, center + toAngleRight * arcRadius, 5.0f);
            }
        }
    }
}
