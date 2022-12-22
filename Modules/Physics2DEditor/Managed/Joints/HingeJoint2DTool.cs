// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.EditorTools;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor
{
    [EditorTool("Edit Hinge Joint 2D", typeof(HingeJoint2D))]
    class HingeJoint2DTool : EditorTool, IDrawSelectedHandles
    {
        protected static class Styles
        {
            public static readonly string editAngularLimitsUndoMessage = EditorGUIUtility.TrTextContent("Change Joint Angular Limits").text;
            public static readonly Color handleColor = new Color(0f, 1f, 0f, 0.7f);
            public static readonly float handleRadius = 0.8f;
        }

        private JointAngularLimitHandle2D m_AngularLimitHandle = new JointAngularLimitHandle2D();

        private static readonly Quaternion s_RightHandedHandleOrientationOffset =
            Quaternion.AngleAxis(180f, Vector3.right) * Quaternion.AngleAxis(90f, Vector3.up);

        private static readonly Quaternion s_LeftHandedHandleOrientationOffset =
            Quaternion.AngleAxis(180f, Vector3.forward) * Quaternion.AngleAxis(90f, Vector3.up);

        static Matrix4x4 GetAnchorSpaceMatrix(Transform transform)
        {
            // Anchor space transformation matrix.
            return Matrix4x4.TRS(transform.position, Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z), transform.lossyScale);
        }

        protected static Vector3 TransformPoint(Transform transform, Vector3 position)
        {
            // Local to World.
            return GetAnchorSpaceMatrix(transform).MultiplyPoint(position);
        }

        protected static Vector3 InverseTransformPoint(Transform transform, Vector3 position)
        {
            // World to Local.
            return GetAnchorSpaceMatrix(transform).inverse.MultiplyPoint(position);
        }

        void OnEnable()
        {
            m_AngularLimitHandle.handleColor = Color.white;

            // +/-PHYSICS_2D_LARGE_RANGE_CLAMP, which is currently used in JointAngleLimits2D.CheckConsistency().
            m_AngularLimitHandle.range = new Vector2(-1e+6f, 1e+6f);
        }

        public override GUIContent toolbarIcon
        {
            get { return EditorGUIUtility.IconContent("JointAngularLimits"); }
        }

        public override void OnToolGUI(EditorWindow window)
        {
            foreach (var obj in targets)
            {
                if (obj is HingeJoint2D)
                {
                    // Ignore disabled joint.
                    var hingeJoint2D = obj as HingeJoint2D;
                    if (hingeJoint2D.enabled)
                    {
                        DrawHandle(true, hingeJoint2D, m_AngularLimitHandle);
                    }
                }
            }
        }

        public void OnDrawHandles()
        {
            foreach (var obj in targets)
            {
                if (obj is HingeJoint2D)
                {
                    // Ignore disabled joint.
                    var hingeJoint2D = obj as HingeJoint2D;
                    if (hingeJoint2D.enabled)
                    {
                        DrawHandle(false, hingeJoint2D, m_AngularLimitHandle);
                    }
                }
            }        }

        public static void DrawHandle(bool editMode, HingeJoint2D hingeJoint2D, JointAngularLimitHandle2D angularLimitHandle)
        {
            // Only display control handles on manipulator if in edit mode.
            if (editMode)
                angularLimitHandle.angleHandleDrawFunction = ArcHandle.DefaultAngleHandleDrawFunction;
            else
                angularLimitHandle.angleHandleDrawFunction = null;

            // Fetch the reference angle which acts as an offset to the absolute angles this gizmo is displaying/editing.
            var referenceAngle = hingeJoint2D.referenceAngle;

            // Fetch the joint limits.
            var limit = hingeJoint2D.limits;

            // Set the limit handle.
            angularLimitHandle.jointMotion = hingeJoint2D.useLimits ? JointAngularLimitHandle2D.JointMotion.Limited : JointAngularLimitHandle2D.JointMotion.Free;
            angularLimitHandle.min = limit.min + referenceAngle;
            angularLimitHandle.max = limit.max + referenceAngle;

            // Fetch if we're using the connected anchor.
            // NOTE: If we're not then we want to draw the gizmo at the body position.
            var usingConnectedAnchor = hingeJoint2D.useConnectedAnchor;

            // To enhance usability, orient the manipulator to best illustrate its affects on the dynamic body in the system.
            var dynamicBody = hingeJoint2D.attachedRigidbody;
            var dynamicBodyLocalReferencePosition = Vector3.right;
            var dynamicAnchor = usingConnectedAnchor ? hingeJoint2D.anchor : Vector2.zero;
            var connectedBody = usingConnectedAnchor ? hingeJoint2D.connectedBody : null;
            var handleOrientationOffset = s_RightHandedHandleOrientationOffset;

            if (dynamicBody.bodyType != RigidbodyType2D.Dynamic
                && hingeJoint2D.connectedBody != null
                && hingeJoint2D.connectedBody.bodyType == RigidbodyType2D.Dynamic)
            {
                dynamicBody = hingeJoint2D.connectedBody;
                dynamicBodyLocalReferencePosition = Vector3.left;
                dynamicAnchor = usingConnectedAnchor ? hingeJoint2D.connectedAnchor : Vector2.zero;
                connectedBody = usingConnectedAnchor ? hingeJoint2D.connectedBody : null;
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

                angularLimitHandle.radius = radius;

                // Reference line within arc to illustrate affected local axis.
                Handles.DrawLine(
                    Vector3.zero,
                    handleMatrix.inverse.MultiplyPoint3x4(dynamicActorReferencePosition).normalized * radius
                );

                angularLimitHandle.DrawHandle();
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(hingeJoint2D, Styles.editAngularLimitsUndoMessage);

                limit = hingeJoint2D.limits;
                limit.min = angularLimitHandle.min - referenceAngle;
                limit.max = angularLimitHandle.max - referenceAngle;
                hingeJoint2D.limits = limit;

                dynamicBody.WakeUp();
            }
        }
    }
}
