// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEditor.AnimatedValues;
using System.Linq;

namespace UnityEditor
{
    [CustomEditor(typeof(Rigidbody))]
    [CanEditMultipleObjects]
    internal class RigidbodyEditor : Editor
    {
        SerializedProperty m_Constraints;
        SerializedProperty m_Mass;
        SerializedProperty m_LinearDamping;
        SerializedProperty m_AngularDamping;

        SerializedProperty m_ImplicitCom;
        SerializedProperty m_CenterOfMass;
        SerializedProperty m_ImplicitTensor;
        SerializedProperty m_InertiaTensor;
        SerializedProperty m_InertiaRotation;

        SerializedProperty m_UseGravity;
        SerializedProperty m_IsKinematic;
        SerializedProperty m_Interpolate;
        SerializedProperty m_CollisionDetection;

        readonly AnimBool m_ShowLayerOverrides = new AnimBool();
        private SavedBool m_ShowLayerOverridesFoldout;
        SerializedProperty m_IncludeLayers;
        SerializedProperty m_ExcludeLayers;


         private class Styles
        {
            public static readonly GUIContent mass = L10n.TextContent("Mass", "Mass of this rigid body.", null, null);
            public static readonly GUIContent useGravity = L10n.TextContent("Use Gravity", "Controls whether gravity affects this rigid body.", null, null);

            public static readonly GUIContent linearDamping = L10n.TextContent("Linear Damping", "Damping factor that affects how this body resists linear motion.", null, null);
            public static readonly GUIContent angularDamping = L10n.TextContent("Angular Damping", "Damping factor that affects how this body resists rotations.", null, null);
            public static readonly GUIContent isKinematic = L10n.TextContent("Is Kinematic", "Controls whether physics affects the rigidbody.", null, null);
            public static readonly GUIContent interpolate = L10n.TextContent("Interpolate", "Smooths out the effect of running physics at a fixed frame rate.", null, null);

            public static readonly GUIContent implicitCom = L10n.TextContent("Automatic Center Of Mass", "Use the calculated center of mass or set it directly.", null, null);
            public static readonly GUIContent implicitTensor = L10n.TextContent("Automatic Tensor", "Use the calculated tensor or set it directly.", null, null);
            public static readonly GUIContent centerOfMass = L10n.TextContent("Center Of Mass", "The local space coordinates of the center of mass.", null, null);
            public static readonly GUIContent inertiaTensor = L10n.TextContent("Inertia Tensor", "The diagonal inertia tensor of mass relative to the center of mass.", null, null);
            public static readonly GUIContent inertiaRotation = L10n.TextContent("Inertia Tensor Rotation", "The rotation of the inertia tensor.", null, null);

            public static readonly GUIContent collisionDetection = L10n.TextContent("Collision Detection", "The method to use to detect collisions for child colliders: discrete (default) or various modes of continuous collision detection that can help solving fast moving object issues.", null, null);

            public static readonly GUIContent freezePositionLabel = L10n.TextContent("Freeze Position", null, null, null);
            public static readonly GUIContent freezeRotationLabel = L10n.TextContent("Freeze Rotation", null, null, null);

            public static readonly GUIContent includeLayers = L10n.TextContent("Include Layers", "Layers to include when producing collisions", null, null);
            public static readonly GUIContent excludeLayers = L10n.TextContent("Exclude Layers", "Layers to exclude when producing collisions", null, null);
        }

         public void OnEnable()
        {
            m_Mass = serializedObject.FindProperty("m_Mass");
            m_LinearDamping = serializedObject.FindProperty("m_LinearDamping");
            m_AngularDamping = serializedObject.FindProperty("m_AngularDamping");

            m_ImplicitCom = serializedObject.FindProperty("m_ImplicitCom");
            m_CenterOfMass = serializedObject.FindProperty("m_CenterOfMass");
            m_ImplicitTensor = serializedObject.FindProperty("m_ImplicitTensor");
            m_InertiaTensor = serializedObject.FindProperty("m_InertiaTensor");
            m_InertiaRotation = serializedObject.FindProperty("m_InertiaRotation");

            m_UseGravity = serializedObject.FindProperty("m_UseGravity");
            m_IsKinematic = serializedObject.FindProperty("m_IsKinematic");
            m_Interpolate = serializedObject.FindProperty("m_Interpolate");
            m_CollisionDetection = serializedObject.FindProperty("m_CollisionDetection");
            m_Constraints = serializedObject.FindProperty("m_Constraints");

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

        void ConstraintToggle(Rect r, string label, RigidbodyConstraints value, int bit)
        {
            bool toggle = ((int)value & (1 << bit)) != 0;
            EditorGUI.showMixedValue = (m_Constraints.hasMultipleDifferentValuesBitwise & (1 << bit)) != 0;
            EditorGUI.BeginChangeCheck();
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            toggle = EditorGUI.ToggleLeft(r, label, toggle);
            EditorGUI.indentLevel = oldIndent;
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(targets, "Edit Constraints");
                m_Constraints.SetBitAtIndexForAllTargetsImmediate(bit, toggle);
            }
            EditorGUI.showMixedValue = false;
        }

        void ToggleBlock(RigidbodyConstraints constraints, GUIContent label, int x, int y, int z)
        {
            const int toggleOffset = 30;
            GUILayout.BeginHorizontal();
            Rect r = GUILayoutUtility.GetRect(EditorGUIUtility.fieldWidth, EditorGUILayout.kLabelFloatMaxW, EditorGUI.kSingleLineHeight, EditorGUI.kSingleLineHeight, EditorStyles.numberField);
            int id = GUIUtility.GetControlID(7231, FocusType.Keyboard, r);
            r = EditorGUI.PrefixLabel(r, id, label);
            r.width = toggleOffset;
            ConstraintToggle(r, "X", constraints, x);
            r.x += toggleOffset;
            ConstraintToggle(r, "Y", constraints, y);
            r.x += toggleOffset;
            ConstraintToggle(r, "Z", constraints, z);
            GUILayout.EndHorizontal();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Mass, Styles.mass);
            EditorGUILayout.PropertyField(m_LinearDamping, Styles.linearDamping);
            EditorGUILayout.PropertyField(m_AngularDamping, Styles.angularDamping);
            EditorGUILayout.PropertyField(m_ImplicitCom, Styles.implicitCom);
            if (!m_ImplicitCom.boolValue)
                EditorGUILayout.PropertyField(m_CenterOfMass, Styles.centerOfMass);
            EditorGUILayout.PropertyField(m_ImplicitTensor, Styles.implicitTensor);
            if (!m_ImplicitTensor.boolValue)
            {
                EditorGUILayout.PropertyField(m_InertiaTensor, Styles.inertiaTensor);
                EditorGUILayout.PropertyField(m_InertiaRotation, Styles.inertiaRotation);
            }

            EditorGUILayout.PropertyField(m_UseGravity, Styles.useGravity);
            EditorGUILayout.PropertyField(m_IsKinematic, Styles.isKinematic);
            EditorGUILayout.PropertyField(m_Interpolate, Styles.interpolate);
            EditorGUILayout.PropertyField(m_CollisionDetection, Styles.collisionDetection);

            if (System.Array.Exists(targets, x => (x as Rigidbody).interpolation != RigidbodyInterpolation.None))
            {
                if (Physics.simulationMode == SimulationMode.Update)
                    EditorGUILayout.HelpBox("The physics simulation mode is set to run per-frame. Any interpolation mode will be ignored and can be set to 'None'.", MessageType.Info);
                else if (Physics.simulationMode == SimulationMode.Script)
                    EditorGUILayout.HelpBox("The physics simulation mode is set to run manually in the scripts. Some or all selected Rigidbodies are using an interpolation mode other than 'None' which will be executed per-frame. If the manual simulation is being run per-frame then the interpolation mode should be set to 'None'.", MessageType.Info);
            }

            Rect position = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(position, null, m_Constraints);
            m_Constraints.isExpanded = EditorGUI.Foldout(position, m_Constraints.isExpanded, m_Constraints.displayName, true);
            EditorGUI.EndProperty();

            RigidbodyConstraints constraints = (RigidbodyConstraints)m_Constraints.intValue;
            if (m_Constraints.isExpanded)
            {
                EditorGUI.indentLevel++;
                ToggleBlock(constraints, Styles.freezePositionLabel, 1, 2, 3);
                ToggleBlock(constraints, Styles.freezeRotationLabel, 4, 5, 6);
                EditorGUI.indentLevel--;
            }

            ShowLayerOverridesProperties();

            serializedObject.ApplyModifiedProperties();
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
