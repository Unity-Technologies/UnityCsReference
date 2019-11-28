// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.IMGUI.Controls;
using UnityEngine;

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
        }

        private void QuaternionAsEulerAnglesPropertyField(string tag, SerializedProperty quaternionProperty, Quaternion rotation)
        {
            quaternionProperty.quaternionValue = Quaternion.Euler(EditorGUILayout.Vector3Field(tag, rotation.eulerAngles));
        }

        // prismatic joint allows only one translational degree of freedom
        private void PrismaticJointAxisLockProperty(SerializedProperty linearLock)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(linearLock);
            if (EditorGUI.EndChangeCheck() && linearLock.enumValueIndex != (int)ArticulationDofLock.LockedMotion)
            {
                if (linearLock != m_LinearX)
                    m_LinearX.enumValueIndex = (int)ArticulationDofLock.LockedMotion;

                if (linearLock != m_LinearY)
                    m_LinearY.enumValueIndex = (int)ArticulationDofLock.LockedMotion;

                if (linearLock != m_LinearZ)
                    m_LinearZ.enumValueIndex = (int)ArticulationDofLock.LockedMotion;
            }
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
                    QuaternionAsEulerAnglesPropertyField("Parent Anchor Rotation", m_ParentAnchorRotation, body.parentAnchorRotation);

                    if (EditorGUI.EndChangeCheck())
                    {
                        body.parentAnchorPosition = m_ParentAnchorPosition.vector3Value;
                        body.parentAnchorRotation = m_ParentAnchorRotation.quaternionValue;
                    }
                }

                if (GUILayout.Button("Snap anchor to closest contact"))
                {
                    Vector3 com = parentBody.centerOfMass;
                    Vector3 closestOnSurface = body.GetClosestPoint(com);
                    body.anchorPosition = body.transform.InverseTransformPoint(closestOnSurface);
                    body.anchorRotation = Quaternion.FromToRotation(Vector3.right, body.transform.InverseTransformDirection(com - closestOnSurface).normalized);
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
                        // toggles
                        PrismaticJointAxisLockProperty(m_LinearX);
                        PrismaticJointAxisLockProperty(m_LinearY);
                        PrismaticJointAxisLockProperty(m_LinearZ);

                        EditorGUILayout.Space();
                        if (body.linearLockX != ArticulationDofLock.LockedMotion)
                            StructPropertyGUILayout.GenericStruct(m_XDrive);

                        if (body.linearLockY != ArticulationDofLock.LockedMotion)
                            StructPropertyGUILayout.GenericStruct(m_YDrive);

                        if (body.linearLockZ != ArticulationDofLock.LockedMotion)
                            StructPropertyGUILayout.GenericStruct(m_ZDrive);

                        break;

                    case ArticulationJointType.RevoluteJoint:
                        EditorGUILayout.PropertyField(m_Twist);

                        EditorGUILayout.Space();
                        StructPropertyGUILayout.GenericStruct(m_XDrive);

                        break;

                    case ArticulationJointType.SphericalJoint:
                        EditorGUILayout.PropertyField(m_SwingY);
                        EditorGUILayout.PropertyField(m_SwingZ);
                        EditorGUILayout.PropertyField(m_Twist);

                        EditorGUILayout.Space();

                        if (body.swingYLock != ArticulationDofLock.LockedMotion)
                            StructPropertyGUILayout.GenericStruct(m_YDrive);

                        if (body.swingZLock != ArticulationDofLock.LockedMotion)
                            StructPropertyGUILayout.GenericStruct(m_ZDrive);

                        if (body.twistLock != ArticulationDofLock.LockedMotion)
                            StructPropertyGUILayout.GenericStruct(m_XDrive);

                        break;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        void DrawFrame(Vector3 p, Quaternion q)
        {
            // we don't allow editing anchors visually for now
            Handles.PositionHandle(p, q);
        }

        protected virtual void OnSceneGUI()
        {
            ArticulationBody body = (ArticulationBody)target;
            ArticulationBody parentBody = FindParentBody(body);

            if (body.isRoot)
                return;

            DrawFrame(body.transform.TransformPoint(body.anchorPosition), body.transform.rotation * body.anchorRotation);
            DrawFrame(parentBody.transform.TransformPoint(body.parentAnchorPosition), parentBody.transform.rotation * body.parentAnchorRotation);
        }
    }
}
