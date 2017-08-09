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
    abstract class JointEditor<T> : Editor where T : Joint
    {
        protected static class Styles
        {
            public static readonly GUIContent editAngularLimitsButton = new GUIContent(EditorGUIUtility.IconContent("JointAngularLimits"));
            public static readonly string editAngularLimitsUndoMessage = EditorGUIUtility.TextContent("Change Joint Angular Limits").text;

            static Styles()
            {
                editAngularLimitsButton.tooltip = EditorGUIUtility.TextContent("Edit joint angular limits.").text;
            }
        }

        protected static float GetAngularLimitHandleSize(Vector3 position)
        {
            return HandleUtility.GetHandleSize(position);
        }

        protected JointAngularLimitHandle angularLimitHandle { get { return m_AngularLimitHandle; } }
        private JointAngularLimitHandle m_AngularLimitHandle = new JointAngularLimitHandle();

        protected bool editingAngularLimits
        {
            get { return EditMode.editMode == EditMode.SceneViewEditMode.JointAngularLimits && EditMode.IsOwner(this); }
        }

        public override void OnInspectorGUI()
        {
            DoInspectorEditButtons();
            base.OnInspectorGUI();
        }

        protected void DoInspectorEditButtons()
        {
            T joint = (T)target;
            EditMode.DoEditModeInspectorModeButton(
                EditMode.SceneViewEditMode.JointAngularLimits,
                "Edit Joint Angular Limits",
                Styles.editAngularLimitsButton,
                this
                );
        }

        internal override Bounds GetWorldBoundsOfTarget(UnityObject targetObject)
        {
            var bounds = base.GetWorldBoundsOfTarget(targetObject);
            // ensure joint's anchor point is included in bounds
            bounds.Encapsulate(GetAngularLimitHandleMatrix((T)targetObject).MultiplyPoint3x4(Vector3.zero));
            return bounds;
        }

        protected virtual void OnSceneGUI()
        {
            if (editingAngularLimits)
            {
                T joint = (T)target;
                EditorGUI.BeginChangeCheck();

                using (new Handles.DrawingScope(GetAngularLimitHandleMatrix(joint)))
                    DoAngularLimitHandles(joint);

                // wake up rigidbodies in case current orientation is out of bounds of new limits
                if (EditorGUI.EndChangeCheck())
                {
                    var rigidbody = joint.GetComponent<Rigidbody>();
                    if (rigidbody.isKinematic && joint.connectedBody != null)
                        joint.connectedBody.WakeUp();
                    else
                        rigidbody.WakeUp();
                }
            }
        }

        protected virtual void GetActors(
            T joint,
            out Rigidbody dynamicActor,
            out Rigidbody connectedActor,
            out int jointFrameActorIndex,
            out bool rightHandedLimit
            )
        {
            jointFrameActorIndex = 1;
            rightHandedLimit = false;

            dynamicActor = joint.GetComponent<Rigidbody>();
            connectedActor = joint.connectedBody;

            if (dynamicActor.isKinematic && connectedActor != null && !connectedActor.isKinematic)
            {
                var temp = connectedActor;
                connectedActor = dynamicActor;
                dynamicActor = temp;
            }
        }

        private Matrix4x4 GetAngularLimitHandleMatrix(T joint)
        {
            Rigidbody dynamicActor, connectedActor;
            int jointFrameActorIndex;
            bool rightHandedLimit;
            GetActors(joint, out dynamicActor, out connectedActor, out jointFrameActorIndex, out rightHandedLimit);

            var connectedBodyRotation =
                connectedActor == null ? Quaternion.identity : connectedActor.transform.rotation;

            // to enhance usability, orient the limit region so the dynamic body is within it, assuming bodies were bound on opposite sides of the anchor
            var jointFrame = joint.GetActorLocalPose(jointFrameActorIndex);
            var jointFrameOrientation = Quaternion.LookRotation(
                    jointFrame.MultiplyVector(Vector3.forward),
                    jointFrame.MultiplyVector(rightHandedLimit ? Vector3.down : Vector3.up)
                    );

            // point of rotation is about the anchor of the joint body, which is not necessarily aligned to the anchor on the connected body
            var jointAnchorPosition = joint.anchor;
            if (dynamicActor != null)
                jointAnchorPosition = dynamicActor.transform.TransformPoint(jointAnchorPosition);

            return Matrix4x4.TRS(jointAnchorPosition, connectedBodyRotation * jointFrameOrientation, Vector3.one);
        }

        protected virtual void DoAngularLimitHandles(T joint)
        {
        }
    }
}
