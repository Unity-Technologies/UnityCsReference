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
        new protected static class Styles
        {
            public static readonly GUIContent editAngularLimitsButton = new GUIContent(EditorGUIUtility.IconContent("JointAngularLimits"));
            public static readonly string editAngularLimitsUndoMessage = EditorGUIUtility.TextContent("Change Joint Angular Limits").text;
            public static readonly Color handleColor = new Color(0f, 1f, 0f, 0.7f);
            public static readonly float handleRadius = 0.8f;

            static Styles()
            {
                editAngularLimitsButton.tooltip = EditorGUIUtility.TextContent("Edit joint angular limits.").text;
            }
        }

        private JointAngularLimitHandle m_AngularLimitHandle = new JointAngularLimitHandle();

        new public void OnEnable()
        {
            base.OnEnable();

            m_AngularLimitHandle.xHandleColor = Color.white;
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

        private void NonEditableHandleDrawFunction(
            int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType
            )
        {
        }

        private static readonly Quaternion s_RightHandedHandleOrientationOffset =
            Quaternion.AngleAxis(180f, Vector3.right) * Quaternion.AngleAxis(90f, Vector3.up);
        private static readonly Quaternion s_LeftHandedHandleOrientationOffset =
            Quaternion.AngleAxis(180f, Vector3.forward) * Quaternion.AngleAxis(90f, Vector3.up);

        new public void OnSceneGUI()
        {
            var hingeJoint2D = (HingeJoint2D)target;

            // Ignore disabled joint.
            if (!hingeJoint2D.enabled)
                return;

            m_AngularLimitHandle.xMotion = hingeJoint2D.useLimits ? ConfigurableJointMotion.Limited : ConfigurableJointMotion.Free;

            JointAngleLimits2D limit;

            limit = hingeJoint2D.limits;
            m_AngularLimitHandle.xMin = limit.min;
            m_AngularLimitHandle.xMax = limit.max;

            // only display control handles on manipulator if in edit mode
            var editMode = EditMode.editMode == EditMode.SceneViewEditMode.JointAngularLimits && EditMode.IsOwner(this);
            if (editMode)
                m_AngularLimitHandle.angleHandleDrawFunction = null;
            else
                m_AngularLimitHandle.angleHandleDrawFunction = NonEditableHandleDrawFunction;

            // to enhance usability, orient the manipulator to best illustrate its affects on the dynamic body in the system
            var dynamicBody = hingeJoint2D.attachedRigidbody;
            var dynamicBodyLocalReferencePosition = Vector3.right;
            var dynamicAnchor = hingeJoint2D.anchor;
            var connectedBody = hingeJoint2D.connectedBody;
            var handleOrientationOffset = s_RightHandedHandleOrientationOffset;
            if (
                dynamicBody.bodyType != RigidbodyType2D.Dynamic
                && hingeJoint2D.connectedBody != null
                && hingeJoint2D.connectedBody.bodyType == RigidbodyType2D.Dynamic
                )
            {
                dynamicBody = hingeJoint2D.connectedBody;
                dynamicBodyLocalReferencePosition = Vector3.left;
                dynamicAnchor = hingeJoint2D.connectedAnchor;
                connectedBody = hingeJoint2D.attachedRigidbody;
                handleOrientationOffset = s_LeftHandedHandleOrientationOffset;
            }

            var handlePosition = TransformPoint(dynamicBody.transform, dynamicAnchor);
            var handleOrientation = (
                    connectedBody == null ?
                    Quaternion.identity :
                    Quaternion.LookRotation(Vector3.forward, connectedBody.transform.rotation * Vector3.up)
                    ) * handleOrientationOffset;
            var dynamicActorReferencePosition =
                handlePosition
                + Quaternion.LookRotation(Vector3.forward, dynamicBody.transform.rotation * Vector3.up)
                * dynamicBodyLocalReferencePosition;

            var handleMatrix = Matrix4x4.TRS(handlePosition, handleOrientation, Vector3.one);

            EditorGUI.BeginChangeCheck();

            using (new Handles.DrawingScope(Styles.handleColor, handleMatrix))
            {
                var radius = HandleUtility.GetHandleSize(Vector3.zero) * Styles.handleRadius;
                m_AngularLimitHandle.radius = radius;

                // reference line within arc to illustrate affected local axis
                Handles.DrawLine(
                    Vector3.zero,
                    handleMatrix.inverse.MultiplyPoint3x4(dynamicActorReferencePosition).normalized * radius
                    );

                m_AngularLimitHandle.DrawHandle();
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(hingeJoint2D, Styles.editAngularLimitsUndoMessage);

                limit = hingeJoint2D.limits;
                limit.min = m_AngularLimitHandle.xMin;
                limit.max = m_AngularLimitHandle.xMax;
                hingeJoint2D.limits = limit;

                dynamicBody.WakeUp();
            }

            base.OnSceneGUI();
        }
    }
}
