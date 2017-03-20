// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(Rigidbody2D))]
    [CanEditMultipleObjects]
    internal class Rigidbody2DEditor : Editor
    {
        SerializedProperty m_Simulated;
        SerializedProperty m_BodyType;
        SerializedProperty m_Material;
        SerializedProperty m_UseFullKinematicContacts;
        SerializedProperty m_UseAutoMass;
        SerializedProperty m_Mass;
        SerializedProperty m_LinearDrag;
        SerializedProperty m_AngularDrag;
        SerializedProperty m_GravityScale;
        SerializedProperty m_Interpolate;
        SerializedProperty m_SleepingMode;
        SerializedProperty m_CollisionDetection;
        SerializedProperty m_Constraints;

        readonly AnimBool m_ShowIsStatic = new AnimBool();
        readonly AnimBool m_ShowIsKinematic = new AnimBool();

        readonly AnimBool m_ShowInfo = new AnimBool();
        readonly AnimBool m_ShowContacts = new AnimBool();
        Vector2 m_ContactScrollPosition;

        static readonly GUIContent m_FreezePositionLabel = new GUIContent("Freeze Position");
        static readonly GUIContent m_FreezeRotationLabel = new GUIContent("Freeze Rotation");

        static ContactPoint2D[] m_Contacts = new ContactPoint2D[100];

        const int k_ToggleOffset = 30;

        public void OnEnable()
        {
            var body = target as Rigidbody2D;

            m_Simulated = serializedObject.FindProperty("m_Simulated");
            m_BodyType = serializedObject.FindProperty("m_BodyType");
            m_Material = serializedObject.FindProperty("m_Material");
            m_UseFullKinematicContacts = serializedObject.FindProperty("m_UseFullKinematicContacts");
            m_UseAutoMass = serializedObject.FindProperty("m_UseAutoMass");
            m_Mass = serializedObject.FindProperty("m_Mass");
            m_LinearDrag = serializedObject.FindProperty("m_LinearDrag");
            m_AngularDrag = serializedObject.FindProperty("m_AngularDrag");
            m_GravityScale = serializedObject.FindProperty("m_GravityScale");
            m_Interpolate = serializedObject.FindProperty("m_Interpolate");
            m_SleepingMode = serializedObject.FindProperty("m_SleepingMode");
            m_CollisionDetection = serializedObject.FindProperty("m_CollisionDetection");
            m_Constraints = serializedObject.FindProperty("m_Constraints");

            m_ShowIsStatic.value = body.bodyType != RigidbodyType2D.Static;
            m_ShowIsStatic.valueChanged.AddListener(Repaint);

            m_ShowIsKinematic.value = body.bodyType != RigidbodyType2D.Kinematic;
            m_ShowIsKinematic.valueChanged.AddListener(Repaint);

            m_ShowInfo.valueChanged.AddListener(Repaint);
            m_ShowContacts.valueChanged.AddListener(Repaint);
            m_ContactScrollPosition = Vector2.zero;
        }

        public void OnDisable()
        {
            m_ShowIsStatic.valueChanged.RemoveListener(Repaint);
            m_ShowIsKinematic.valueChanged.RemoveListener(Repaint);
            m_ShowInfo.valueChanged.RemoveListener(Repaint);
            m_ShowContacts.valueChanged.RemoveListener(Repaint);
        }

        public override void OnInspectorGUI()
        {
            var body = target as Rigidbody2D;

            serializedObject.Update();

            EditorGUILayout.PropertyField(m_BodyType);
            EditorGUILayout.PropertyField(m_Material);

            // Provide the user some information when simulation is turned off.
            EditorGUILayout.PropertyField(m_Simulated);
            if (!m_Simulated.boolValue && !m_Simulated.hasMultipleDifferentValues)
                EditorGUILayout.HelpBox("The body has now been taken out of the simulation along with any attached colliders, joints or effectors.", MessageType.Info);


            // Can only multi-edit if we have the same body-type.
            if (m_BodyType.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("Cannot edit properties that are body type specific when the selection contains different body types.", MessageType.Info);
            }
            else
            {
                // Non-static options.
                m_ShowIsStatic.target = body.bodyType != RigidbodyType2D.Static;
                if (EditorGUILayout.BeginFadeGroup(m_ShowIsStatic.faded))
                {
                    // Kinematic options.
                    m_ShowIsKinematic.target = body.bodyType != RigidbodyType2D.Kinematic;
                    if (EditorGUILayout.BeginFadeGroup(m_ShowIsKinematic.faded))
                    {
                        // Collider Mass.
                        EditorGUILayout.PropertyField(m_UseAutoMass);

                        // Only show mass property if selected objects have the same useAutoMass value.
                        if (!m_UseAutoMass.hasMultipleDifferentValues)
                        {
                            // If we're using auto-mass but either the object is part of a prefab parent or is not active then we cannot show the calculated mass value.
                            if (m_UseAutoMass.boolValue && targets.Any(x => PrefabUtility.GetPrefabType(x) == PrefabType.Prefab || !(x as Rigidbody2D).gameObject.activeInHierarchy))
                            {
                                EditorGUILayout.HelpBox("The auto mass value cannot be displayed for a prefab or if the object is not active.  The value will be calculated for a prefab instance and when the object is active.", MessageType.Info);
                            }
                            else
                            {
                                EditorGUI.BeginDisabledGroup(body.useAutoMass);
                                EditorGUILayout.PropertyField(m_Mass);
                                EditorGUI.EndDisabledGroup();
                            }
                        }

                        EditorGUILayout.PropertyField(m_LinearDrag);
                        EditorGUILayout.PropertyField(m_AngularDrag);
                        EditorGUILayout.PropertyField(m_GravityScale);
                    }
                    FixedEndFadeGroup(m_ShowIsKinematic.faded);

                    if (!m_ShowIsKinematic.target)
                        EditorGUILayout.PropertyField(m_UseFullKinematicContacts);

                    EditorGUILayout.PropertyField(m_CollisionDetection);
                    EditorGUILayout.PropertyField(m_SleepingMode);
                    EditorGUILayout.PropertyField(m_Interpolate);

                    GUILayout.BeginHorizontal();
                    m_Constraints.isExpanded = EditorGUILayout.Foldout(m_Constraints.isExpanded, "Constraints", true);
                    GUILayout.EndHorizontal();

                    var constraints = (RigidbodyConstraints2D)m_Constraints.intValue;
                    if (m_Constraints.isExpanded)
                    {
                        EditorGUI.indentLevel++;
                        ToggleFreezePosition(constraints, m_FreezePositionLabel, 0, 1);
                        ToggleFreezeRotation(constraints, m_FreezeRotationLabel, 2);
                        EditorGUI.indentLevel--;
                    }

                    // Provide end-user warning about the equivalence of all constraints on versus no Rigidbody2D component.
                    if (constraints == RigidbodyConstraints2D.FreezeAll)
                        EditorGUILayout.HelpBox("Rather than turning on all constraints, you may want to consider removing the Rigidbody2D component which makes any colliders static.  This gives far better performance overall.", MessageType.Info);
                }
                FixedEndFadeGroup(m_ShowIsStatic.faded);
            }

            serializedObject.ApplyModifiedProperties();

            ShowBodyInfoProperties();
        }

        private void ShowBodyInfoProperties()
        {
            m_ShowInfo.target = EditorGUILayout.Foldout(m_ShowInfo.target, "Info", true);
            if (EditorGUILayout.BeginFadeGroup(m_ShowInfo.faded))
            {
                if (targets.Length == 1)
                {
                    var body = targets[0] as Rigidbody2D;
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.Vector2Field("Position", body.position);
                    EditorGUILayout.FloatField("Rotation", body.rotation);
                    EditorGUILayout.Vector2Field("Velocity", body.velocity);
                    EditorGUILayout.FloatField("Angular Velocity", body.angularVelocity);
                    EditorGUILayout.FloatField("Inertia", body.inertia);
                    EditorGUILayout.Vector2Field("Local Center of Mass", body.centerOfMass);
                    EditorGUILayout.Vector2Field("World Center of Mass", body.worldCenterOfMass);
                    EditorGUILayout.LabelField("Sleep State", body.IsSleeping() ? "Asleep" : "Awake");
                    EditorGUI.EndDisabledGroup();

                    ShowContacts(body);

                    // We need to repaint as some of the above properties can change without causing a repaint.
                    Repaint();
                }
                else
                {
                    EditorGUILayout.HelpBox("Cannot show Info properties when multiple bodies are selected.", MessageType.Info);
                }
            }
            FixedEndFadeGroup(m_ShowInfo.faded);
        }

        void ShowContacts(Rigidbody2D body)
        {
            EditorGUI.indentLevel++;
            m_ShowContacts.target = EditorGUILayout.Foldout(m_ShowContacts.target, "Contacts");
            if (EditorGUILayout.BeginFadeGroup(m_ShowContacts.faded))
            {
                var contactCount = body.GetContacts(m_Contacts);
                if (contactCount > 0)
                {
                    m_ContactScrollPosition = EditorGUILayout.BeginScrollView(m_ContactScrollPosition, GUILayout.Height(180));
                    EditorGUI.BeginDisabledGroup(true);
                    for (var i = 0; i < contactCount; ++i)
                    {
                        var contact = m_Contacts[i];
                        EditorGUILayout.HelpBox(string.Format("Contact#{0}", i), MessageType.None);
                        EditorGUI.indentLevel++;
                        EditorGUILayout.Vector2Field("Point", contact.point);
                        EditorGUILayout.Vector2Field("Normal", contact.normal);
                        EditorGUILayout.Vector2Field("Relative Velocity", contact.relativeVelocity);
                        EditorGUILayout.FloatField("Normal Impulse", contact.normalImpulse);
                        EditorGUILayout.FloatField("Tangent Impulse", contact.tangentImpulse);
                        EditorGUILayout.ObjectField("Collider", contact.collider, typeof(Collider2D), true);
                        EditorGUILayout.ObjectField("Rigidbody", contact.rigidbody, typeof(Rigidbody2D), false);
                        EditorGUILayout.ObjectField("OtherCollider", contact.otherCollider, typeof(Collider2D), false);
                        EditorGUI.indentLevel--;
                        EditorGUILayout.Space();
                    }
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndScrollView();
                }
                else
                {
                    EditorGUILayout.HelpBox("No Contacts", MessageType.Info);
                }
            }
            FixedEndFadeGroup(m_ShowContacts.faded);
            EditorGUI.indentLevel--;
        }

        // Fix for nested fade-groups as found here:
        // http://answers.unity3d.com/questions/1096244/custom-editor-fade-group-inside-fade-group.html
        static void FixedEndFadeGroup(float value)
        {
            if (value == 0.0f || value == 1.0f)
                return;

            EditorGUILayout.EndFadeGroup();
        }

        void ConstraintToggle(Rect r, string label, RigidbodyConstraints2D value, int bit)
        {
            var toggle = ((int)value & (1 << bit)) != 0;
            EditorGUI.showMixedValue = (m_Constraints.hasMultipleDifferentValuesBitwise & (1 << bit)) != 0;
            EditorGUI.BeginChangeCheck();
            var oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            toggle = EditorGUI.ToggleLeft(r, label, toggle);
            EditorGUI.indentLevel = oldIndent;
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(targets, "Edit Constraints2D");
                m_Constraints.SetBitAtIndexForAllTargetsImmediate(bit, toggle);
            }
            EditorGUI.showMixedValue = false;
        }

        void ToggleFreezePosition(RigidbodyConstraints2D constraints, GUIContent label, int x, int y)
        {
            GUILayout.BeginHorizontal();
            var r = GUILayoutUtility.GetRect(EditorGUIUtility.fieldWidth, EditorGUILayout.kLabelFloatMaxW, EditorGUI.kSingleLineHeight, EditorGUI.kSingleLineHeight, EditorStyles.numberField);
            var id = GUIUtility.GetControlID(7231, FocusType.Keyboard, r);
            r = EditorGUI.PrefixLabel(r, id, label);
            r.width = k_ToggleOffset;
            ConstraintToggle(r, "X", constraints, x);
            r.x += k_ToggleOffset;
            ConstraintToggle(r, "Y", constraints, y);
            GUILayout.EndHorizontal();
        }

        void ToggleFreezeRotation(RigidbodyConstraints2D constraints, GUIContent label, int z)
        {
            GUILayout.BeginHorizontal();
            var r = GUILayoutUtility.GetRect(EditorGUIUtility.fieldWidth, EditorGUILayout.kLabelFloatMaxW, EditorGUI.kSingleLineHeight, EditorGUI.kSingleLineHeight, EditorStyles.numberField);
            var id = GUIUtility.GetControlID(7231, FocusType.Keyboard, r);
            r = EditorGUI.PrefixLabel(r, id, label);
            r.width = k_ToggleOffset;
            ConstraintToggle(r, "Z", constraints, z);
            GUILayout.EndHorizontal();
        }
    }
}
