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
    static class JointEditorUtil
    {
        internal static void DrawInspector(Editor editor)
        {
            using (new LocalizationGroup(editor.target))
            {
                SerializedObject serializedObject = editor.serializedObject;
                EditorGUI.BeginChangeCheck();

                serializedObject.UpdateIfRequiredOrScript();

                // Loop through properties and create one field (including children) for each top level property.
                SerializedProperty property = serializedObject.GetIterator();
                bool expanded = true;
                while (property.NextVisible(expanded))
                {
                    expanded = false;

                    if(property.propertyPath == "m_ConnectedAnchor")
                    {
                        var isAutoConf = serializedObject.FindProperty("m_AutoConfigureConnectedAnchor").boolValue;
                        using (new EditorGUI.DisabledScope(isAutoConf))
                        {
                            if (isAutoConf)
                            {
                                var joint = editor.target as Joint;
                                Vector3 local = Vector3.zero;
                                Vector3 global = Vector3.zero;

                                EditorGUILayout.Vector3Field(EditorGUIUtility.TempContent(property.localizedDisplayName, property.tooltip), joint.connectedAnchor);
                                editor.Repaint();
                            }
                            else
                            {
                                EditorGUILayout.PropertyField(property, true);
                            }
                        }

                        continue;
                    }

                    EditorGUILayout.PropertyField(property, true);
                }

                serializedObject.ApplyModifiedProperties();

                EditorGUI.EndChangeCheck();
            }
        }
    }

    class JointCommonEditor : Editor
    {
        public static void CheckConnectedBody(Editor editor)
        {
            // Ensure that the connected body doesn't span physics scenes.
            if (editor.targets.Length == 1)
            {
                var joint = editor.target as Joint;

                if (joint.connectedBody != null || joint.connectedArticulationBody != null)
                {
                    var bodyScene = joint.gameObject.scene;
                    var connectedBodyScene = joint.connectedBody != null ? joint.connectedBody.gameObject.scene : joint.connectedArticulationBody.gameObject.scene;

                    // scenes can be invalid when the joint belongs to a prefab
                    if (bodyScene.IsValid() && connectedBodyScene.IsValid())
                    {
                        if (bodyScene.GetPhysicsScene() != connectedBodyScene.GetPhysicsScene())
                        {
                            EditorGUILayout.HelpBox("This joint will not function because it is connected to a body in a different physics scene. This is not supported.", MessageType.Warning);
                        }
                    }
                }
            }
        }

        public static void DrawObjectFieldForBody(Editor editor)
        {
            var joint = editor.target as Joint;
            bool shouldDrawConnectedBody = true;
            bool shouldDrawConnectedArticulationBody = true;

            if (joint.connectedBody != null)
                shouldDrawConnectedArticulationBody = false;

            else if (joint.connectedArticulationBody != null)
                shouldDrawConnectedBody = false;

            if (editor.targets.Length > 1)
            {
                shouldDrawConnectedBody = true;
                shouldDrawConnectedArticulationBody = true;
            }

            if (shouldDrawConnectedBody)
                EditorGUILayout.PropertyField(editor.serializedObject.FindProperty("m_ConnectedBody"), true);

            if (shouldDrawConnectedArticulationBody)
                EditorGUILayout.PropertyField(editor.serializedObject.FindProperty("m_ConnectedArticulationBody"), true);
        }

        public override void OnInspectorGUI()
        {
            CheckConnectedBody(this);
            DrawObjectFieldForBody(this);
            JointEditorUtil.DrawInspector(this);
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
            JointCommonEditor.DrawObjectFieldForBody(this);
            JointEditorUtil.DrawInspector(this);
        }

        protected void DoInspectorEditButtons()
        {
            T joint = (T)target;
            EditorGUI.BeginDisabledGroup(joint.gameObject.activeInHierarchy == false);
            EditorGUILayout.EditorToolbarForTarget(EditorGUIUtility.TrTempContent("Edit Angular Limits"), this);
            EditorGUI.EndDisabledGroup();
        }

        internal override Bounds GetWorldBoundsOfTarget(UnityObject targetObject)
        {
            var bounds = base.GetWorldBoundsOfTarget(targetObject);

            // ensure joint's anchor point is included in bounds
            var jointTool = EditorToolManager.activeTool as JointTool<T>;

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

                if (!joint.gameObject.activeInHierarchy)
                    continue;

                EditorGUI.BeginChangeCheck();

                using (new Handles.DrawingScope(GetAngularLimitHandleMatrix(joint)))
                    DoAngularLimitHandles(joint);

                // wake up rigidbodies in case current orientation is out of bounds of new limits
                if (EditorGUI.EndChangeCheck())
                {
                    var rigidbody = joint.GetComponent<Rigidbody>();

                    if (rigidbody.isKinematic)
                    {
                        if (joint.connectedBody != null)
                            joint.connectedBody.WakeUp();
                        else if (joint.connectedArticulationBody != null)
                            joint.connectedArticulationBody.WakeUp();
                        else
                            rigidbody.WakeUp();
                    }
                    else
                    {
                        rigidbody.WakeUp();
                    }
                }
            }
        }

        protected virtual void GetActors(
            T joint,
            out Transform dynamicPose,
            out Transform connectedPose,
            out int jointFrameActorIndex,
            out bool rightHandedLimit
        )
        {
            jointFrameActorIndex = 1;
            rightHandedLimit = false;

            var thisBody = joint.GetComponent<Rigidbody>();

            dynamicPose = thisBody.transform;

            bool connectedToKinematic = false;

            if (joint.connectedBody)
            {
                connectedPose = joint.connectedBody.transform;
                connectedToKinematic = joint.connectedBody.isKinematic;
            }
            else if (joint.connectedArticulationBody)
            {
                connectedPose = joint.connectedArticulationBody.transform;
                connectedToKinematic = joint.connectedArticulationBody.immovable;
            }
            else
            {
                connectedPose = null;
            }

            if (thisBody.isKinematic && !connectedToKinematic)
            {
                var temp = dynamicPose;
                dynamicPose = connectedPose;
                connectedPose = temp;
            }
        }

        internal Matrix4x4 GetAngularLimitHandleMatrix(T joint)
        {
            Transform dynamicPose, connectedPose;
            int jointFrameActorIndex;
            bool rightHandedLimit;
            GetActors(joint, out dynamicPose, out connectedPose, out jointFrameActorIndex, out rightHandedLimit);

            var connectedBodyRotation =
                connectedPose == null ? Quaternion.identity : connectedPose.transform.rotation;

            // to enhance usability, orient the limit region so the dynamic body is within it, assuming bodies were bound on opposite sides of the anchor
            var jointFrame = joint.GetLocalPoseMatrix(jointFrameActorIndex);
            var jointFrameOrientation = Quaternion.LookRotation(
                jointFrame.MultiplyVector(Vector3.forward),
                jointFrame.MultiplyVector(rightHandedLimit ? Vector3.down : Vector3.up)
            );

            // point of rotation is about the anchor of the joint body, which is not necessarily aligned to the anchor on the connected body
            var jointAnchorPosition = joint.anchor;
            if (dynamicPose != null)
                jointAnchorPosition = dynamicPose.transform.TransformPoint(jointAnchorPosition);

            return Matrix4x4.TRS(jointAnchorPosition, connectedBodyRotation * jointFrameOrientation, Vector3.one);
        }

        protected virtual void DoAngularLimitHandles(T joint)
        {
        }
    }
}
