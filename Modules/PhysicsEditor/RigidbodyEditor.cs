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
        SerializedProperty m_Drag;
        SerializedProperty m_AngularDrag;

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
            public static GUIContent mass = EditorGUIUtility.TrTextContent("Mass", "Mass of this rigid body.");
            public static GUIContent useGravity = EditorGUIUtility.TrTextContent("Use Gravity", "Controls whether gravity affects this rigid body.");

            public static GUIContent drag = EditorGUIUtility.TrTextContent("Linear Damping", "Damping factor that affects how this body resists linear motion.");
            public static GUIContent angularDrag = EditorGUIUtility.TrTextContent("Angular Damping", "Damping factor that affects how this body resists rotations.");
            public static GUIContent isKinematic = EditorGUIUtility.TrTextContent("Is Kinematic", "Controls whether physics affects the rigidbody.");
            public static GUIContent interpolate = EditorGUIUtility.TrTextContent("Interpolate", "Smooths out the effect of running physics at a fixed frame rate.");

            public static GUIContent implicitCom = EditorGUIUtility.TrTextContent("Automatic Center Of Mass", "Use the calculated center of mass or set it directly.");
            public static GUIContent implicitTensor = EditorGUIUtility.TrTextContent("Automatic Tensor", "Use the calculated tensor or set it directly.");
            public static GUIContent centerOfMass = EditorGUIUtility.TrTextContent("Center Of Mass", "The local space coordinates of the center of mass.");
            public static GUIContent inertiaTensor = EditorGUIUtility.TrTextContent("Inertia Tensor", "The diagonal inertia tensor of mass relative to the center of mass.");
            public static GUIContent inertiaRotation = EditorGUIUtility.TrTextContent("Inertia Tensor Rotation", "The rotation of the inertia tensor.");

            public static GUIContent collisionDetection = EditorGUIUtility.TrTextContent("Collision Detection", "The method to use to detect collisions for child colliders: discrete (default) or various modes of continuous collision detection that can help solving fast moving object issues.");

            public static GUIContent freezePositionLabel = EditorGUIUtility.TrTextContent("Freeze Position");
            public static GUIContent freezeRotationLabel = EditorGUIUtility.TrTextContent("Freeze Rotation");

            public static GUIContent includeLayers = EditorGUIUtility.TrTextContent("Include Layers", "Layers to include when producing collisions");
            public static GUIContent excludeLayers = EditorGUIUtility.TrTextContent("Exclude Layers", "Layers to exclude when producing collisions");
        }

         public void OnEnable()
        {
            m_Mass = serializedObject.FindProperty("m_Mass");
            m_Drag = serializedObject.FindProperty("m_Drag");
            m_AngularDrag = serializedObject.FindProperty("m_AngularDrag");

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
            EditorGUILayout.PropertyField(m_Drag, Styles.drag);
            EditorGUILayout.PropertyField(m_AngularDrag, Styles.angularDrag);
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

            if (targets.Any(x => (x as Rigidbody).interpolation != RigidbodyInterpolation.None))
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
