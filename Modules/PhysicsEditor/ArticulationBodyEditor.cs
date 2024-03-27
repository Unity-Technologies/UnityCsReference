// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.AnimatedValues;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(ArticulationBody))]
    [CanEditMultipleObjects]
    internal class ArticulationBodyEditor : Editor
    {
        SerializedProperty m_Mass;
        SerializedProperty m_Immovable;
        SerializedProperty m_UseGravity;

        SerializedProperty m_ImplicitCom;
        SerializedProperty m_ImplicitTensor;
        SerializedProperty m_CenterOfMass;
        SerializedProperty m_InertiaTensor;
        SerializedProperty m_InertiaRotation;

        SerializedProperty m_LinearDamping;
        SerializedProperty m_AngularDamping;
        SerializedProperty m_JointFriction;

        SerializedProperty m_CollisionDetectionMode;

        SerializedProperty m_ParentAnchorPosition;
        SerializedProperty m_ParentAnchorRotation;
        SerializedProperty m_AnchorPosition;
        SerializedProperty m_AnchorRotation;
        SerializedProperty m_MatchAnchors;

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

        readonly AnimBool m_ShowLayerOverrides = new AnimBool();
        private SavedBool m_ShowLayerOverridesFoldout;
        SerializedProperty m_IncludeLayers;
        SerializedProperty m_ExcludeLayers;

        private class Styles
        {
            public static GUIContent mass = EditorGUIUtility.TrTextContent("Mass", "Mass of this articulation body.");
            public static GUIContent immovable = EditorGUIUtility.TrTextContent("Immovable", "Is this articulation body immovable by forces and torques? Only applies to the root body of the articulation.");
            public static GUIContent useGravity = EditorGUIUtility.TrTextContent("Use Gravity", "Controls whether gravity affects this articulation body.");

            public static GUIContent implicitCom = EditorGUIUtility.TrTextContent("Automatic Center Of Mass", "Use the calculated center of mass or set it directly.");
            public static GUIContent implicitTensor = EditorGUIUtility.TrTextContent("Automatic Tensor", "Use the calculated tensor or set it directly.");

            public static GUIContent centerOfMass = EditorGUIUtility.TrTextContent("Center Of Mass", "The local space coordinates of the center of mass.");
            public static GUIContent inertiaTensor = EditorGUIUtility.TrTextContent("Inertia Tensor", "The diagonal inertia tensor of mass relative to the center of mass.");
            public static GUIContent inertiaRotation = EditorGUIUtility.TrTextContent("Inertia Tensor Rotation", "The rotation of the inertia tensor.");

            public static GUIContent collisionDetectionMode = EditorGUIUtility.TrTextContent("Collision Detection", "The method to use to detect collisions for child colliders: discrete (default) or various modes of continuous collision detection that can help solving fast moving object issues.");

            public static GUIContent linearDamping = EditorGUIUtility.TrTextContent("Linear Damping", "Damping factor that affects how this body resists linear motion.");
            public static GUIContent angularDamping = EditorGUIUtility.TrTextContent("Angular Damping", "Damping factor that affects how this body resists rotations.");
            public static GUIContent jointFriction = EditorGUIUtility.TrTextContent("Joint Friction", "Amount of friction that is applied as a result of connected bodies moving relative to this body.");

            public static GUIContent matchAnchors = EditorGUIUtility.TrTextContent("Match Anchors", "Controls whether to set the anchor relative to the parent to be the same as the anchor relative to this body.");
            public static GUIContent anchorPosition = EditorGUIUtility.TrTextContent("Anchor Position", "Position of the anchor relative to this body.");
            public static GUIContent parentAnchorPosition = EditorGUIUtility.TrTextContent("Parent Anchor Position", "Position of the anchor relative to the parent body.");
            public static GUIContent anchorRotation =  EditorGUIUtility.TrTextContent("Anchor Rotation", "Rotation of the anchor relative to this body.");
            public static GUIContent parentAnchorRotation = EditorGUIUtility.TrTextContent("Parent Anchor Rotation", "Rotation of the anchor relative to the parent body.");

            public static GUIContent prismaticAxis = EditorGUIUtility.TrTextContent("Axis", "The only axis the joint allows linear motion along.");
            public static GUIContent unlockedMotionType = EditorGUIUtility.TrTextContent("Motion", "Controls whether the motion is free or limited.");

            public static GUIContent lowerLimit = EditorGUIUtility.TrTextContent("Lower limit", "Limit the minimum linear or angular coordinate this drive allows.");
            public static GUIContent upperLimit = EditorGUIUtility.TrTextContent("Upper limit", "Limit the maximum linear or angular coordinate this drive allows.");

            public static GUIContent stiffness = EditorGUIUtility.TrTextContent("Stiffness", "The stiffness of the spring connected to this drive.");
            public static GUIContent damping = EditorGUIUtility.TrTextContent("Damping", "The damping of the spring attached to this drive.");
            public static GUIContent forceLimit = EditorGUIUtility.TrTextContent("Force Limit", "The maximum force this drive can apply to a body.");
            public static GUIContent target = EditorGUIUtility.TrTextContent("Target", "The target value for the drive to try reaching.");
            public static GUIContent targetVelocity = EditorGUIUtility.TrTextContent("Target Velocity", "The target velocity for the drive to try reaching.");

            public static GUIContent includeLayers = EditorGUIUtility.TrTextContent("Include Layers", "Layers to include when producing collisions");
            public static GUIContent excludeLayers = EditorGUIUtility.TrTextContent("Exclude Layers", "Layers to exclude when producing collisions");

            public static GUIContent driveType = EditorGUIUtility.TrTextContent("Drive Type", "The drive type");
        }

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
            m_Immovable = serializedObject.FindProperty("m_Immovable");
            m_UseGravity = serializedObject.FindProperty("m_UseGravity");

            m_ImplicitCom = serializedObject.FindProperty("m_ImplicitCom");
            m_ImplicitTensor = serializedObject.FindProperty("m_ImplicitTensor");
            m_CenterOfMass = serializedObject.FindProperty("m_CenterOfMass");
            m_InertiaTensor = serializedObject.FindProperty("m_InertiaTensor");
            m_InertiaRotation = serializedObject.FindProperty("m_InertiaRotation");

            m_CollisionDetectionMode = serializedObject.FindProperty("m_CollisionDetectionMode");

            m_LinearDamping = serializedObject.FindProperty("m_LinearDamping");
            m_AngularDamping = serializedObject.FindProperty("m_AngularDamping");
            m_JointFriction = serializedObject.FindProperty("m_JointFriction");

            m_ParentAnchorPosition = serializedObject.FindProperty("m_ParentAnchorPosition");
            m_ParentAnchorRotation = serializedObject.FindProperty("m_ParentAnchorRotation");
            m_AnchorPosition = serializedObject.FindProperty("m_AnchorPosition");
            m_AnchorRotation = serializedObject.FindProperty("m_AnchorRotation");
            m_MatchAnchors = serializedObject.FindProperty("m_MatchAnchors");
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

            m_IncludeLayers = serializedObject.FindProperty("m_IncludeLayers");
            m_ExcludeLayers = serializedObject.FindProperty("m_ExcludeLayers");

            m_ShowLayerOverrides.valueChanged.AddListener(Repaint);
            m_ShowLayerOverridesFoldout = new SavedBool($"{target.GetType() }.ShowLayerOverridesFoldout", false);
            m_ShowLayerOverrides.value = m_ShowLayerOverridesFoldout.value;

            PhysicsDebugWindow.UpdateSelectionOnComponentAdd();
        }

        public void OnDisable()
        {
            m_ShowLayerOverrides.valueChanged.RemoveListener(Repaint);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            ArticulationBody body = (ArticulationBody)target;
            if (!body.isRoot)
            {
                using (new EditorGUI.DisabledScope(body.gameObject.activeInHierarchy == false))
                {
                    EditorGUILayout.EditorToolbarForTarget(EditorGUIUtility.TrTempContent("Edit Joints"), target);
                }
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(m_Mass, Styles.mass);

            EditorGUILayout.PropertyField(m_UseGravity, Styles.useGravity);
            EditorGUILayout.PropertyField(m_ImplicitCom, Styles.implicitCom);

            if (!m_ImplicitCom.boolValue)
            {
                EditorGUILayout.PropertyField(m_CenterOfMass, Styles.centerOfMass);
            }

            EditorGUILayout.PropertyField(m_ImplicitTensor, Styles.implicitTensor);
            if (!m_ImplicitTensor.boolValue)
            {
                EditorGUILayout.PropertyField(m_InertiaTensor, Styles.inertiaTensor);
                EditorGUILayout.PropertyField(m_InertiaRotation, Styles.inertiaRotation);
            }

            if (body.isRoot)
            {
                EditorGUILayout.PropertyField(m_Immovable, Styles.immovable);
                if (!m_Immovable.boolValue)
                {
                    EditorGUILayout.PropertyField(m_LinearDamping, Styles.linearDamping);
                    EditorGUILayout.PropertyField(m_AngularDamping, Styles.angularDamping);
                }

                EditorGUILayout.PropertyField(m_CollisionDetectionMode, Styles.collisionDetectionMode);

                EditorGUILayout.HelpBox("This is the root body of the articulation.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.PropertyField(m_LinearDamping, Styles.linearDamping);
                EditorGUILayout.PropertyField(m_AngularDamping, Styles.angularDamping);
                EditorGUILayout.PropertyField(m_JointFriction, Styles.jointFriction);

                EditorGUILayout.PropertyField(m_CollisionDetectionMode, Styles.collisionDetectionMode);
                EditorGUILayout.PropertyField(m_MatchAnchors, Styles.matchAnchors);

                // Show anchor edit fields and set to joint if changed
                // The reason we have change checks here is because in AwakeFromLoad we won't overwrite anchors
                // If we were to do that, simulation would drift caused by anchors reset relative to current poses
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_AnchorPosition, Styles.anchorPosition);
                EditorGUILayout.PropertyField(m_AnchorRotation, Styles.anchorRotation);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Changing Articulation body anchor position/rotation");
                    body.anchorPosition = m_AnchorPosition.vector3Value;
                    body.anchorRotation = m_AnchorRotation.quaternionValue;

                    if (m_MatchAnchors.boolValue)
                    {
                        // setting child anchors in this mode will also change the parent ones
                        // lets fetch them back otherwise ApplyModifiedProperties overwrites them
                        m_ParentAnchorPosition.vector3Value = body.parentAnchorPosition;
                        m_ParentAnchorRotation.quaternionValue = body.parentAnchorRotation;
                    }
                }

                // parent anchors
                if (!m_MatchAnchors.boolValue)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(m_ParentAnchorPosition, Styles.parentAnchorPosition);
                    EditorGUILayout.PropertyField(m_ParentAnchorRotation, Styles.parentAnchorRotation);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(target, "Changing Articulation body parent anchor position/rotation");
                        body.parentAnchorPosition = m_ParentAnchorPosition.vector3Value;
                        body.parentAnchorRotation = m_ParentAnchorRotation.quaternionValue;
                    }
                }

                if (GUILayout.Button("Snap anchor to closest contact"))
                {
                    Undo.RecordObject(body, "Changing anchor position/rotation to match closest contact.");
                    body.SnapAnchorToClosestContact();
                }

                EditorGUILayout.PropertyField(m_ArticulationJointType); // the tooltip for this is still in the header

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
                        linearDof = (LinearDof)EditorGUILayout.EnumPopup(Styles.prismaticAxis, linearDof);
                        NonLockedMotion motion = (dofLockSetting == ArticulationDofLock.FreeMotion) ? NonLockedMotion.Free : NonLockedMotion.Limited;
                        motion = (NonLockedMotion)EditorGUILayout.EnumPopup(Styles.unlockedMotionType, motion);
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
                        motion = (NonLockedMotion)EditorGUILayout.EnumPopup(Styles.unlockedMotionType, revoluteMotion);

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
            ClampDriveLimits(m_YDrive, body.yDrive);
            ClampDriveLimits(m_ZDrive, body.zDrive);
            ClampDriveLimits(m_XDrive, body.xDrive);

            ShowLayerOverridesProperties();

            serializedObject.ApplyModifiedProperties();
        }

        private void ClampDriveLimits(SerializedProperty drive, ArticulationDrive bodyDrive)
        {
            var lowerLimit = drive.FindPropertyRelative("lowerLimit");
            var upperLimit = drive.FindPropertyRelative("upperLimit");
            if (bodyDrive.lowerLimit != lowerLimit.floatValue)
                lowerLimit.floatValue = Mathf.Min(lowerLimit.floatValue, upperLimit.floatValue);
            if (bodyDrive.upperLimit != upperLimit.floatValue)
                upperLimit.floatValue = Mathf.Max(lowerLimit.floatValue, upperLimit.floatValue);
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
                EditorGUILayout.PropertyField(drive.FindPropertyRelative("lowerLimit"), Styles.lowerLimit);
                EditorGUILayout.PropertyField(drive.FindPropertyRelative("upperLimit"), Styles.upperLimit);
            }

            var driveTypeProperty = drive.FindPropertyRelative("driveType");
            var driveType = (ArticulationDriveType)driveTypeProperty.intValue;

            // Always display fields

            if (driveType == ArticulationDriveType.Target)
            {
                EditorGUI.BeginDisabled(true);
                EditorGUILayout.FloatField(Styles.stiffness, 1e+25f);
                EditorGUILayout.FloatField(Styles.damping, 0f);
                EditorGUI.EndDisabled();
            }
            else if(driveType == ArticulationDriveType.Velocity)
            {
                EditorGUI.BeginDisabled(true);
                EditorGUILayout.FloatField(Styles.stiffness, 0f);
                EditorGUILayout.FloatField(Styles.damping, 1e+25f);
                EditorGUI.EndDisabled();
            }
            else
            {
                EditorGUILayout.PropertyField(drive.FindPropertyRelative("stiffness"), Styles.stiffness);
                EditorGUILayout.PropertyField(drive.FindPropertyRelative("damping"), Styles.damping);
            }

            EditorGUILayout.PropertyField(drive.FindPropertyRelative("forceLimit"), Styles.forceLimit);

            EditorGUI.BeginDisabled(driveType == ArticulationDriveType.Velocity);
            EditorGUILayout.PropertyField(drive.FindPropertyRelative("target"), Styles.target);
            EditorGUI.EndDisabled();

            EditorGUI.BeginDisabled(driveType == ArticulationDriveType.Target);
            EditorGUILayout.PropertyField(drive.FindPropertyRelative("targetVelocity"), Styles.targetVelocity);
            EditorGUI.EndDisabled();

            //this workaround is required here due to the fact that ArticulationDriveType in native currently resides outside of the ArticulationDrive type
            EditorGUI.BeginChangeCheck();
            driveType = (ArticulationDriveType)EditorGUILayout.EnumPopup(Styles.driveType, driveType);

            if (EditorGUI.EndChangeCheck())
                driveTypeProperty.intValue = (int)driveType;

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        private void ShowLayerOverridesProperties()
        {
            // Show Layer Overrides.
            m_ShowLayerOverridesFoldout.value = m_ShowLayerOverrides.target = EditorGUILayout.Foldout(m_ShowLayerOverrides.target, "Layer Overrides", true);
            if (EditorGUILayout.BeginFadeGroup(m_ShowLayerOverrides.faded))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_IncludeLayers, Styles.includeLayers);
                EditorGUILayout.PropertyField(m_ExcludeLayers, Styles.excludeLayers);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();
        }
    }
}
