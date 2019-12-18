// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.EditorTools;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace UnityEditor
{
    class JointCommonEditor : Editor
    {
        public static void CheckConnectedBody(Editor editor)
        {
            // Ensure that the connected body doesn't span physics scenes.
            if (editor.targets.Length == 1)
            {
                var joint = editor.target as Joint;

                if (joint.connectedBody != null)
                {
                    var bodyScene = joint.gameObject.scene;
                    var connectedBodyScene = joint.connectedBody.gameObject.scene;

                    // scenes can be invalid when the joint belongs to a prefab
                    if (bodyScene.IsValid() && connectedBodyScene.IsValid())
                    {
                        if (bodyScene.GetPhysicsScene() != connectedBodyScene.GetPhysicsScene())
                        {
                            EditorGUILayout.HelpBox("This joint will not function because it is connected to a Rigidbody in a different physics scene. This is not supported.", MessageType.Warning);
                        }
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            CheckConnectedBody(this);
            base.OnInspectorGUI();
        }
    }

    [CustomEditor(typeof(FixedJoint))]
    [CanEditMultipleObjects]
    class FixedJointEditor : JointCommonEditor
    {
    }

    [CustomEditor(typeof(SpringJoint))]
    [CanEditMultipleObjects]
    class SpringJointEditor : JointCommonEditor
    {
    }

    abstract class JointEditor<T> : Editor where T : Joint
    {
        public override void OnInspectorGUI()
        {
            JointCommonEditor.CheckConnectedBody(this);
            DoInspectorEditButtons();
            base.OnInspectorGUI();
        }

        protected void DoInspectorEditButtons()
        {
            T joint = (T)target;
            EditorGUI.BeginDisabledGroup(joint.gameObject.activeInHierarchy == false);
            EditorGUILayout.EditorToolbarForTarget(EditorGUIUtility.TrTempContent("Edit Angular Limits"), target);
            EditorGUI.EndDisabledGroup();
        }

        internal override Bounds GetWorldBoundsOfTarget(UnityObject targetObject)
        {
            var bounds = base.GetWorldBoundsOfTarget(targetObject);

            // ensure joint's anchor point is included in bounds
            var jointTool = EditorToolContext.activeTool as JointTool<T>;

            if (jointTool != null)
                bounds.Encapsulate(jointTool.GetAngularLimitHandleMatrix((T)targetObject).MultiplyPoint3x4(Vector3.zero));

            return bounds;
        }
    }

    abstract class JointTool<T> : EditorTool where T : Joint
    {
        protected static class Styles
        {
            public static readonly string editAngularLimitsUndoMessage = L10n.Tr("Change Joint Angular Limits");
        }

        public override GUIContent toolbarIcon
        {
            get { return EditorGUIUtility.IconContent("JointAngularLimits"); }
        }

        protected static float GetAngularLimitHandleSize(Vector3 position)
        {
            return HandleUtility.GetHandleSize(position);
        }

        protected JointAngularLimitHandle angularLimitHandle { get { return m_AngularLimitHandle; } }
        JointAngularLimitHandle m_AngularLimitHandle = new JointAngularLimitHandle();

        public override void OnToolGUI(EditorWindow window)
        {
            foreach (var obj in targets)
            {
                T joint = obj as T;

                if (joint == null)
                    continue;

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

        internal Matrix4x4 GetAngularLimitHandleMatrix(T joint)
        {
            Rigidbody dynamicActor, connectedActor;
            int jointFrameActorIndex;
            bool rightHandedLimit;
            GetActors(joint, out dynamicActor, out connectedActor, out jointFrameActorIndex, out rightHandedLimit);

            var connectedBodyRotation =
                connectedActor == null ? Quaternion.identity : connectedActor.transform.rotation;

            // to enhance usability, orient the limit region so the dynamic body is within it, assuming bodies were bound on opposite sides of the anchor
            var jointFrame = joint.GetLocalPoseMatrix(jointFrameActorIndex);
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
