// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using ICSharpCode.NRefactory.Ast;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.XR;
//using UnityScript.TypeSystem;

namespace UnityEditor
{
    [CustomEditor(typeof(ArticulationBody))]
    [CanEditMultipleObjects]
    internal class ArticulationBodyEditor : Editor
    {
        SerializedProperty m_Mass;
        SerializedProperty m_ParentAnchorPosition;
        SerializedProperty m_ParentAnchorRotation;
        SerializedProperty m_AnchorPosition;
        SerializedProperty m_AnchorRotation;
        SerializedProperty m_ComputeParentAnchor;

        SerializedProperty m_LinearX;
        SerializedProperty m_LinearY;
        SerializedProperty m_LinearZ;
        SerializedProperty m_SwingY;
        SerializedProperty m_SwingZ;
        SerializedProperty m_Twist;

        SerializedProperty m_ArticulationJointType;

        SerializedProperty m_XDrive;
        SerializedProperty m_YDrive;
        SerializedProperty m_ZDrive;

        SerializedProperty m_LinearDamping;
        SerializedProperty m_AngularDamping;
        SerializedProperty m_JointFriction;

        SerializedProperty m_Immovable;
        bool m_DisplayParentAnchor;

        internal enum NonLockedMotion
        {
            Free = 2,
            Limited = 1
        }

        internal enum LinearDof
        {
            X,
            Y,
            Z
        }

        public void OnEnable()
        {
            m_Mass = serializedObject.FindProperty("m_Mass");
            m_ParentAnchorPosition = serializedObject.FindProperty("m_ParentAnchorPosition");
            m_ParentAnchorRotation = serializedObject.FindProperty("m_ParentAnchorRotation");
            m_AnchorPosition = serializedObject.FindProperty("m_AnchorPosition");
            m_AnchorRotation = serializedObject.FindProperty("m_AnchorRotation");
            m_ComputeParentAnchor = serializedObject.FindProperty("m_ComputeParentAnchor");
            m_ArticulationJointType = serializedObject.FindProperty("m_ArticulationJointType");

            m_LinearX = serializedObject.FindProperty("m_LinearX");
            m_LinearY = serializedObject.FindProperty("m_LinearY");
            m_LinearZ = serializedObject.FindProperty("m_LinearZ");
            m_SwingY = serializedObject.FindProperty("m_SwingY");
            m_SwingZ = serializedObject.FindProperty("m_SwingZ");
            m_Twist = serializedObject.FindProperty("m_Twist");

            m_XDrive = serializedObject.FindProperty("m_XDrive");
            m_YDrive = serializedObject.FindProperty("m_YDrive");
            m_ZDrive = serializedObject.FindProperty("m_ZDrive");

            m_LinearDamping = serializedObject.FindProperty("m_LinearDamping");
            m_AngularDamping = serializedObject.FindProperty("m_AngularDamping");
            m_JointFriction = serializedObject.FindProperty("m_JointFriction");

            m_Immovable = serializedObject.FindProperty("m_Immovable");
            m_DisplayParentAnchor = false;
        }

        private void QuaternionAsEulerAnglesPropertyField(string tag, SerializedProperty quaternionProperty,
            Quaternion rotation)
        {
            quaternionProperty.quaternionValue =
                Quaternion.Euler(EditorGUILayout.Vector3Field(tag, rotation.eulerAngles));
        }

        private ArticulationBody FindParentBody(ArticulationBody child)
        {
            Transform t = child.transform;
            while (true)
            {
                t = t.parent;

                if (t == null)
                    return null;

                var body = t.GetComponent<ArticulationBody>();

                if (body)
                    return body;
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            ArticulationBody body = (ArticulationBody)target;
            ArticulationBody parentBody = FindParentBody(body);

            EditorGUILayout.PropertyField(m_Mass);

            if (body.isRoot)
            {
                EditorGUILayout.PropertyField(m_Immovable);

                EditorGUILayout.HelpBox("This is the root body of the articulation.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.PropertyField(m_ComputeParentAnchor);

                // Render anchor handle display button only if editor scene view has Move, Rotate or Transform tool mode selected
                if (IsEditToolModeForAnchorDisplay())
                {
                    if (m_DisplayParentAnchor)
                        m_DisplayParentAnchor = !GUILayout.Button("Show anchor handle");
                    else
                        m_DisplayParentAnchor = GUILayout.Button("Show parent anchor handle");
                }

                // Show anchor edit fields and set to joint if changed
                // The reason we have change checks here is because in AwakeFromLoad we won't overwrite anchors
                // If we were to do that, simulation would drift caused by anchors reset relative to current poses
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_AnchorPosition);
                QuaternionAsEulerAnglesPropertyField("Anchor Rotation", m_AnchorRotation, body.anchorRotation);
                if (EditorGUI.EndChangeCheck())
                {
                    body.anchorPosition = m_AnchorPosition.vector3Value;
                    body.anchorRotation = m_AnchorRotation.quaternionValue;

                    if (m_ComputeParentAnchor.boolValue)
                    {
                        // setting child anchors in this mode will also change the parent ones
                        // lets fetch them back otherwise ApplyModifiedProperties overwrites them
                        m_ParentAnchorPosition.vector3Value = body.parentAnchorPosition;
                        m_ParentAnchorRotation.quaternionValue = body.parentAnchorRotation;
                    }
                }

                // parent anchors
                if (!m_ComputeParentAnchor.boolValue)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(m_ParentAnchorPosition);
                    QuaternionAsEulerAnglesPropertyField("Parent Anchor Rotation", m_ParentAnchorRotation,
                        body.parentAnchorRotation);

                    if (EditorGUI.EndChangeCheck())
                    {
                        body.parentAnchorPosition = m_ParentAnchorPosition.vector3Value;
                        body.parentAnchorRotation = m_ParentAnchorRotation.quaternionValue;
                    }
                }

                if (GUILayout.Button("Snap anchor to closest contact"))
                {
                    Undo.RecordObject(body, "Changing anchor position/rotation to match closest contact.");
                    Vector3 com = parentBody.centerOfMass;
                    Vector3 closestOnSurface = body.GetClosestPoint(com);
                    body.anchorPosition = body.transform.InverseTransformPoint(closestOnSurface);
                    body.anchorRotation = Quaternion.FromToRotation(Vector3.right,
                        body.transform.InverseTransformDirection(com - closestOnSurface).normalized);
                }

                EditorGUILayout.PropertyField(m_ArticulationJointType);

                EditorGUILayout.PropertyField(m_LinearDamping);
                EditorGUILayout.PropertyField(m_AngularDamping);
                EditorGUILayout.PropertyField(m_JointFriction);

                switch (body.jointType)
                {
                    case ArticulationJointType.FixedJoint:
                        break;

                    case ArticulationJointType.PrismaticJoint:
                        // work out joint settings
                        LinearDof linearDof = LinearDof.X; // x by default
                        ArticulationDofLock dofLockSetting = ArticulationDofLock.FreeMotion;

                        if (body.linearLockX != ArticulationDofLock.LockedMotion)
                        {
                            linearDof = LinearDof.X;
                            dofLockSetting = body.linearLockX;
                        }
                        else if (body.linearLockY != ArticulationDofLock.LockedMotion)
                        {
                            linearDof = LinearDof.Y;
                            dofLockSetting = body.linearLockY;
                        }
                        else if (body.linearLockZ != ArticulationDofLock.LockedMotion)
                        {
                            linearDof = LinearDof.Z;
                            dofLockSetting = body.linearLockZ;
                        }

                        int dofCount = 0;

                        if (body.linearLockX != ArticulationDofLock.LockedMotion) dofCount++;
                        if (body.linearLockY != ArticulationDofLock.LockedMotion) dofCount++;
                        if (body.linearLockZ != ArticulationDofLock.LockedMotion) dofCount++;

                        bool overrideDof = (dofCount != 1);

                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.Space();
                        linearDof = (LinearDof)EditorGUILayout.EnumPopup("Axis", linearDof);
                        NonLockedMotion motion = (dofLockSetting == ArticulationDofLock.FreeMotion) ? NonLockedMotion.Free : NonLockedMotion.Limited;
                        motion = (NonLockedMotion)EditorGUILayout.EnumPopup("Motion", motion);
                        if (EditorGUI.EndChangeCheck() || overrideDof)
                        {
                            m_LinearX.enumValueIndex = (linearDof == LinearDof.X) ? (int)motion : 0;
                            m_LinearY.enumValueIndex = (linearDof == LinearDof.Y) ? (int)motion : 0;
                            m_LinearZ.enumValueIndex = (linearDof == LinearDof.Z) ? (int)motion : 0;
                        }

                        EditorGUILayout.Space();

                        DoDriveInspector(m_YDrive, (ArticulationDofLock)m_LinearY.enumValueIndex);
                        DoDriveInspector(m_ZDrive, (ArticulationDofLock)m_LinearZ.enumValueIndex);
                        DoDriveInspector(m_XDrive, (ArticulationDofLock)m_LinearX.enumValueIndex);

                        break;

                    case ArticulationJointType.RevoluteJoint:
                        ArticulationDofLock serialisedDofLock = (ArticulationDofLock)m_Twist.enumValueIndex;

                        NonLockedMotion revoluteMotion = serialisedDofLock == ArticulationDofLock.LimitedMotion
                            ? NonLockedMotion.Limited
                            : NonLockedMotion.Free;

                        EditorGUILayout.Space();
                        motion = (NonLockedMotion)EditorGUILayout.EnumPopup("Motion", revoluteMotion);

                        ArticulationDofLock newDofLock = (ArticulationDofLock)motion;

                        if (newDofLock != serialisedDofLock)
                        {
                            m_Twist.enumValueIndex = (int)newDofLock;
                        }

                        EditorGUILayout.Space();
                        DoDriveInspector(m_XDrive, newDofLock);

                        break;

                    case ArticulationJointType.SphericalJoint:
                        EditorGUILayout.Space();
                        // here we just need to make sure we disable getting into an invalid configuration
                        SphericalJointMotionSetting(m_SwingY);
                        SphericalJointMotionSetting(m_SwingZ);
                        SphericalJointMotionSetting(m_Twist);

                        EditorGUILayout.Space();

                        DoDriveInspector(m_YDrive, body.swingYLock);
                        DoDriveInspector(m_ZDrive, body.swingZLock);
                        DoDriveInspector(m_XDrive, body.twistLock);

                        break;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void SphericalJointMotionSetting(SerializedProperty axis)
        {
            EditorGUI.BeginChangeCheck();
            int oldSetting = axis.enumValueIndex; // assume it was correct before changing
            EditorGUILayout.PropertyField(axis);
            if (EditorGUI.EndChangeCheck())
            {
                if (m_SwingY.enumValueIndex == 0 && m_SwingZ.enumValueIndex == 0 && m_Twist.enumValueIndex == 0)
                {
                    axis.enumValueIndex = oldSetting;
                }
            }
        }

        protected virtual void OnSceneGUI()
        {
            ArticulationBody body = (ArticulationBody)target;
            ArticulationBody parentBody = FindParentBody(body);

            if (body.isRoot)
                return;

            if (!m_DisplayParentAnchor)
            {
                Vector3 anchorPosInWorldSpace = body.transform.TransformPoint(body.anchorPosition);
                Quaternion anchorRotInWorldSpace = body.transform.rotation * body.anchorRotation;

                EditorGUI.BeginChangeCheck();

                DisplayProperAnchorHandle(ref anchorPosInWorldSpace, ref anchorRotInWorldSpace);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Changing Articulation body anchor position/rotation");
                    m_AnchorPosition.vector3Value = body.transform.InverseTransformPoint(anchorPosInWorldSpace);
                    m_AnchorRotation.quaternionValue =
                        Quaternion.Inverse(body.transform.rotation) * anchorRotInWorldSpace;

                    body.anchorPosition = m_AnchorPosition.vector3Value;
                    body.anchorRotation = m_AnchorRotation.quaternionValue;
                }

                return;
            }

            Vector3 parentAnchorPosInWorldSpace = body.transform.parent.TransformPoint(body.parentAnchorPosition);
            Quaternion parentAnchorRotInWorldSpace = body.transform.parent.rotation * body.parentAnchorRotation;

            EditorGUI.BeginChangeCheck();

            DisplayProperAnchorHandle(ref parentAnchorPosInWorldSpace, ref parentAnchorRotInWorldSpace);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Changing Articulation body parent anchor position/rotation");
                m_ParentAnchorPosition.vector3Value =
                    body.transform.parent.InverseTransformPoint(parentAnchorPosInWorldSpace);
                m_ParentAnchorRotation.quaternionValue =
                    Quaternion.Inverse(body.transform.parent.rotation) * parentAnchorRotInWorldSpace;

                body.parentAnchorPosition = m_ParentAnchorPosition.vector3Value;
                body.parentAnchorRotation = m_ParentAnchorRotation.quaternionValue;
            }
        }

        private void DisplayProperAnchorHandle(ref Vector3 anchorPos, ref Quaternion anchorRot)
        {
            if (Tools.current == Tool.Move)
                anchorPos = Handles.PositionHandle(anchorPos, anchorRot);
            if (Tools.current == Tool.Rotate)
            {
                anchorRot = Handles.RotationHandle(anchorRot, anchorPos);
            }

            if (Tools.current == Tool.Transform)
            {
                Handles.TransformHandle(ref anchorPos, ref anchorRot);
            }
        }

        private bool IsEditToolModeForAnchorDisplay()
        {
            return (Tools.current == Tool.Move) || (Tools.current == Tool.Rotate) || (Tools.current == Tool.Transform);
        }

        private void DoDriveInspector(SerializedProperty drive, ArticulationDofLock dofLock)
        {
            // If lockedMotion - don't render any drive inspector fields
            if (dofLock == ArticulationDofLock.LockedMotion)
                return;

            EditorGUILayout.LabelField(drive.displayName);

            EditorGUI.indentLevel++;

            // Display limit fields only if drive is LimitedMotion
            if (dofLock == ArticulationDofLock.LimitedMotion)
            {
                EditorGUILayout.PropertyField(drive.FindPropertyRelative("lowerLimit"));
                EditorGUILayout.PropertyField(drive.FindPropertyRelative("upperLimit"));
            }

            // Always display fields
            EditorGUILayout.PropertyField(drive.FindPropertyRelative("stiffness"));
            EditorGUILayout.PropertyField(drive.FindPropertyRelative("damping"));
            EditorGUILayout.PropertyField(drive.FindPropertyRelative("forceLimit"));
            EditorGUILayout.PropertyField(drive.FindPropertyRelative("target"));
            EditorGUILayout.PropertyField(drive.FindPropertyRelative("targetVelocity"));

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }
}
