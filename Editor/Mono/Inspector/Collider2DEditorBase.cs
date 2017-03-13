// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEditor.AnimatedValues;

namespace UnityEditor
{
    [CanEditMultipleObjects]
    internal abstract class Collider2DEditorBase : ColliderEditorBase
    {
        protected class Styles
        {
            public static readonly GUIContent s_ColliderEditDisableHelp = EditorGUIUtility.TextContent("Collider cannot be edited because it is driven by SpriteRenderer's tiling properties.");
            public static readonly GUIContent s_AutoTilingLabel = EditorGUIUtility.TextContent("Auto Tiling | When enabled, the collider's shape will update automaticaly based on the SpriteRenderer's tiling properties");
        }

        private SerializedProperty m_Density;
        private readonly AnimBool m_ShowDensity = new AnimBool();
        private readonly AnimBool m_ShowInfo = new AnimBool();
        private readonly AnimBool m_ShowContacts = new AnimBool();
        Vector2 m_ContactScrollPosition;

        static ContactPoint2D[] m_Contacts = new ContactPoint2D[100];

        private SerializedProperty m_Material;
        private SerializedProperty m_IsTrigger;
        private SerializedProperty m_UsedByEffector;
        private SerializedProperty m_UsedByComposite;
        private SerializedProperty m_Offset;
        protected SerializedProperty m_AutoTiling;

        private readonly AnimBool m_ShowCompositeRedundants = new AnimBool();

        public override void OnEnable()
        {
            base.OnEnable();

            m_Density = serializedObject.FindProperty("m_Density");

            m_ShowDensity.value = ShouldShowDensity();
            m_ShowDensity.valueChanged.AddListener(Repaint);

            m_ShowInfo.valueChanged.AddListener(Repaint);
            m_ShowContacts.valueChanged.AddListener(Repaint);
            m_ContactScrollPosition = Vector2.zero;

            m_Material = serializedObject.FindProperty("m_Material");
            m_IsTrigger = serializedObject.FindProperty("m_IsTrigger");
            m_UsedByEffector = serializedObject.FindProperty("m_UsedByEffector");
            m_UsedByComposite = serializedObject.FindProperty("m_UsedByComposite");
            m_Offset = serializedObject.FindProperty("m_Offset");
            m_AutoTiling  = serializedObject.FindProperty("m_AutoTiling");

            m_ShowCompositeRedundants.value = !m_UsedByComposite.boolValue;
            m_ShowCompositeRedundants.valueChanged.AddListener(Repaint);
        }

        public override void OnDisable()
        {
            m_ShowDensity.valueChanged.RemoveListener(Repaint);
            m_ShowInfo.valueChanged.RemoveListener(Repaint);
            m_ShowContacts.valueChanged.RemoveListener(Repaint);
            m_ShowCompositeRedundants.valueChanged.RemoveListener(Repaint);

            base.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            m_ShowCompositeRedundants.target = !m_UsedByComposite.boolValue;
            if (EditorGUILayout.BeginFadeGroup(m_ShowCompositeRedundants.faded))
            {
                // Density property.
                m_ShowDensity.target = ShouldShowDensity();
                if (EditorGUILayout.BeginFadeGroup(m_ShowDensity.faded))
                    EditorGUILayout.PropertyField(m_Density);
                FixedEndFadeGroup(m_ShowDensity.faded);

                EditorGUILayout.PropertyField(m_Material);
                EditorGUILayout.PropertyField(m_IsTrigger);
                EditorGUILayout.PropertyField(m_UsedByEffector);
            }
            FixedEndFadeGroup(m_ShowCompositeRedundants.faded);

            // Only show 'Used By Composite' if all targets are capable of being composited.
            if (targets.Where(x => (x as Collider2D).compositeCapable == false).Count() == 0)
                EditorGUILayout.PropertyField(m_UsedByComposite);

            if (m_AutoTiling != null)
                EditorGUILayout.PropertyField(m_AutoTiling, Styles.s_AutoTilingLabel);

            EditorGUILayout.PropertyField(m_Offset);
        }

        public void FinalizeInspectorGUI()
        {
            ShowColliderInfoProperties();

            // Check for collider error state.
            CheckColliderErrorState();

            // If used-by-composite is enabled but there is not composite then show a warning.
            if (targets.Length == 1)
            {
                var collider = target as Collider2D;
                if (collider.isActiveAndEnabled && collider.composite == null && m_UsedByComposite.boolValue)
                    EditorGUILayout.HelpBox("This collider will not function with a composite until there is a CompositeCollider2D on the GameObject that the attached Rigidbody2D is on.", MessageType.Warning);
            }

            // Check for effector warnings.
            Effector2DEditor.CheckEffectorWarnings(target as Collider2D);
        }

        private void ShowColliderInfoProperties()
        {
            m_ShowInfo.target = EditorGUILayout.Foldout(m_ShowInfo.target, "Info", true);
            if (EditorGUILayout.BeginFadeGroup(m_ShowInfo.faded))
            {
                if (targets.Length == 1)
                {
                    var collider = targets[0] as Collider2D;
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField("Attached Body", collider.attachedRigidbody, typeof(Rigidbody2D), false);
                    EditorGUILayout.FloatField("Friction", collider.friction);
                    EditorGUILayout.FloatField("Bounciness", collider.bounciness);
                    EditorGUILayout.FloatField("Shape Count", collider.shapeCount);
                    if (collider.isActiveAndEnabled)
                        EditorGUILayout.BoundsField("Bounds", collider.bounds);
                    EditorGUI.EndDisabledGroup();

                    ShowContacts(collider);

                    // We need to repaint as some of the above properties can change without causing a repaint.
                    Repaint();
                }
                else
                {
                    EditorGUILayout.HelpBox("Cannot show Info properties when multiple colliders are selected.", MessageType.Info);
                }
            }
            EditorGUILayout.EndFadeGroup();
        }

        bool ShouldShowDensity()
        {
            if (targets.Select(x => (x as Collider2D).attachedRigidbody).Distinct().Count() > 1)
                return false;

            var rigidbody = (target as Collider2D).attachedRigidbody;
            return rigidbody && rigidbody.useAutoMass && rigidbody.bodyType == RigidbodyType2D.Dynamic;
        }

        void ShowContacts(Collider2D collider)
        {
            EditorGUI.indentLevel++;
            m_ShowContacts.target = EditorGUILayout.Foldout(m_ShowContacts.target, "Contacts");
            if (EditorGUILayout.BeginFadeGroup(m_ShowContacts.faded))
            {
                var contactCount = collider.GetContacts(m_Contacts);
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
                        EditorGUILayout.ObjectField("Collider", contact.collider, typeof(Collider2D), false);
                        EditorGUILayout.ObjectField("Rigidbody", contact.rigidbody, typeof(Rigidbody2D), false);
                        EditorGUILayout.ObjectField("OtherRigidbody", contact.otherRigidbody, typeof(Rigidbody2D), false);
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

        internal override void OnForceReloadInspector()
        {
            base.OnForceReloadInspector();

            // Whenever inspector get reloaded (reset, move up/down), quit the edit mode if was in editing mode.
            // Not sure why this pattern is used here but not for any other editors that implement edit mode button
            if (editingCollider)
                EditMode.QuitEditMode();
        }

        protected void CheckColliderErrorState()
        {
            switch ((target as Collider2D).errorState)
            {
                case ColliderErrorState2D.NoShapes:
                    // Show warning.
                    EditorGUILayout.HelpBox("The collider did not create any collision shapes as they all failed verification.  This could be because they were deemed too small or the vertices were too close.  Vertices can also become close under certain rotations or very small scaling.", MessageType.Warning);
                    break;

                case ColliderErrorState2D.RemovedShapes:
                    // Show warning.
                    EditorGUILayout.HelpBox("The collider created collision shape(s) but some were removed as they failed verification.  This could be because they were deemed too small or the vertices were too close.  Vertices can also become close under certain rotations or very small scaling.", MessageType.Warning);
                    break;
            }
        }

        protected void BeginColliderInspector()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(targets.Length > 1))
            {
                InspectorEditButtonGUI();
            }
        }

        protected void EndColliderInspector()
        {
            serializedObject.ApplyModifiedProperties();
        }

        protected bool CanEditCollider()
        {
            var e = targets.FirstOrDefault((x) =>
                {
                    var sr = (x as Component).GetComponent<SpriteRenderer>();
                    return (sr != null && sr.drawMode != SpriteDrawMode.Simple && m_AutoTiling.boolValue == true);
                }
                    );
            return e == false;
        }
    }
}
