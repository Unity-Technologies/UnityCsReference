// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace UnityEditor
{
    [CustomEditor(typeof(HingeJoint2D))]
    [CanEditMultipleObjects]
    internal class HingeJoint2DEditor : AnchoredJoint2DEditor
    {
        private const float k_ArcRadiusScale = 0.8f;

        new protected static class Styles
        {
            public static readonly GUIContent editAngularLimitsButton = new GUIContent(EditorGUIUtility.IconContent("JointAngularLimits"));
            public static readonly string editAngularLimitsUndoMessage = EditorGUIUtility.TextContent("Change Joint Angular Limits").text;

            static Styles()
            {
                editAngularLimitsButton.tooltip = EditorGUIUtility.TextContent("Edit joint angular limits.").text;
            }
        }

        private JointAngularLimitHandle m_AngularLimitHandle = new JointAngularLimitHandle();

        new public void OnEnable()
        {
            base.OnEnable();

            m_AngularLimitHandle.xHandleColor = new Color(0f, 1f, 0f, 0.7f);
            m_AngularLimitHandle.fillAlpha = 0.04285714286f; // matches color of static representation when not in edit mode

            m_AngularLimitHandle.yHandleColor = Color.clear;
            m_AngularLimitHandle.zHandleColor = Color.clear;

            m_AngularLimitHandle.yMotion = ConfigurableJointMotion.Locked;
            m_AngularLimitHandle.zMotion = ConfigurableJointMotion.Locked;

            // +/-PHYSICS_2D_LARGE_RANGE_CLAMP, which is currently used in JointAngleLimits2D.CheckConsistency()
            m_AngularLimitHandle.xRange = new Vector2(-1e+6f, 1e+6f);
        }

        public override void OnInspectorGUI()
        {
            HingeJoint2D joint = (HingeJoint2D)target;
            EditMode.DoEditModeInspectorModeButton(
                EditMode.SceneViewEditMode.JointAngularLimits,
                "Edit Joint Angular Limits",
                Styles.editAngularLimitsButton,
                this
                );

            base.OnInspectorGUI();
        }

        internal override Bounds GetWorldBoundsOfTarget(UnityObject targetObject)
        {
            var bounds = base.GetWorldBoundsOfTarget(targetObject);
            // ensure joint's anchor point is included in bounds
            var joint = (HingeJoint2D)targetObject;
            bounds.Encapsulate(TransformPoint(joint.transform, joint.anchor));
            return bounds;
        }

        new public void OnSceneGUI()
        {
            var hingeJoint2D = (HingeJoint2D)target;

            // Ignore disabled joint.
            if (!hingeJoint2D.enabled)
                return;

            if (EditMode.editMode == EditMode.SceneViewEditMode.JointAngularLimits && EditMode.IsOwner(this))
            {
                m_AngularLimitHandle.xMotion = hingeJoint2D.useLimits ? ConfigurableJointMotion.Limited : ConfigurableJointMotion.Free;

                JointAngleLimits2D limit;

                limit = hingeJoint2D.limits;
                m_AngularLimitHandle.xMin = limit.min;
                m_AngularLimitHandle.xMax = limit.max;

                EditorGUI.BeginChangeCheck();

                Matrix4x4 handleMatrix = Matrix4x4.TRS(
                        TransformPoint(hingeJoint2D.transform, hingeJoint2D.anchor),
                        Quaternion.AngleAxis(90f, Vector3.up),
                        Vector3.one
                        );
                using (new Handles.DrawingScope(handleMatrix))
                {
                    m_AngularLimitHandle.radius = HandleUtility.GetHandleSize(Vector3.zero) * k_ArcRadiusScale;
                    m_AngularLimitHandle.DrawHandle();
                }

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(hingeJoint2D, Styles.editAngularLimitsUndoMessage);

                    limit = hingeJoint2D.limits;
                    limit.min = m_AngularLimitHandle.xMin;
                    limit.max = m_AngularLimitHandle.xMax;
                    hingeJoint2D.limits = limit;
                }
            }
            else if (hingeJoint2D.useLimits)
            {
                var center = TransformPoint(hingeJoint2D.transform, hingeJoint2D.anchor);

                var limitMin = Mathf.Min(hingeJoint2D.limits.min, hingeJoint2D.limits.max);
                var limitMax = Mathf.Max(hingeJoint2D.limits.min, hingeJoint2D.limits.max);

                var arcAngle = limitMax - limitMin;
                var arcRadius = HandleUtility.GetHandleSize(center) * k_ArcRadiusScale;

                var hingeBodyAngle = hingeJoint2D.GetComponent<Rigidbody2D>().rotation;
                Vector3 fromDirection = RotateVector2(Vector3.right, -limitMax - hingeBodyAngle);
                var referencePosition = center + (Vector3)(RotateVector2(Vector3.right, -hingeJoint2D.jointAngle - hingeBodyAngle) * arcRadius);

                // "reference" line
                Handles.color = new Color(0, 1, 0, 0.7f);
                DrawAALine(center, referencePosition);

                // arc background
                Handles.color = new Color(0, 1, 0, 0.03f);
                Handles.DrawSolidArc(center, Vector3.back, fromDirection, arcAngle, arcRadius);

                // arc frame
                Handles.color = new Color(0, 1, 0, 0.7f);
                Handles.DrawWireArc(center, Vector3.back, fromDirection, arcAngle, arcRadius);

                DrawTick(center, arcRadius, 0, fromDirection, 1);
                DrawTick(center, arcRadius, arcAngle, fromDirection, 1);
            }

            base.OnSceneGUI();
        }

        void DrawTick(Vector3 center, float radius, float angle, Vector3 up, float length)
        {
            Vector3 direction = RotateVector2(up, angle).normalized;
            Vector3 start = center + direction * radius;
            Vector3 end = start + (center - start).normalized * radius * length;
            Handles.DrawAAPolyLine(new[] { new Color(0, 1, 0, 0.7f), new Color(0, 1, 0, 0) }, new[] { start, end });
        }
    }
}
