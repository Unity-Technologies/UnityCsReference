// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    // Object used as a context to handle state changes and undo/redo operations.
    internal class Physics2DPreferenceState : ScriptableObject
    {
        public class SettingsContent
        {
            public static readonly GUIContent ColliderAwakeOutlineContent = EditorGUIUtility.TrTextContent("Awake Color (Outline)");
            public static readonly GUIContent ColliderAsleepOutlineContent = EditorGUIUtility.TrTextContent("Asleep Color (Outline)");
            public static readonly GUIContent ColliderAwakeFilledContent = EditorGUIUtility.TrTextContent("Awake Color (Filled)");
            public static readonly GUIContent ColliderAsleepFilledContent = EditorGUIUtility.TrTextContent("Asleep Color (Filled)");
            public static readonly GUIContent ColliderBoundsContent = EditorGUIUtility.TrTextContent("Bounds Color");
            public static readonly GUIContent CompositedColorContent = EditorGUIUtility.TrTextContent("Composited Color");
            public static readonly GUIContent ColliderContactContent = EditorGUIUtility.TrTextContent("Contact Color");
            public static readonly GUIContent ContactArrowScaleContent = EditorGUIUtility.TrTextContent("Contact Arrow Scale");

            public static readonly GUIContent CollidersLabelContent = EditorGUIUtility.TrTextContent("Colliders");
            public static readonly GUIContent ContactsLabelContent = EditorGUIUtility.TrTextContent("Contacts");
        }

        // These must match "GizmoDrawing.cpp" for the Physics2DEditor!
        const string UniqueSettingsKey = "UnityEditor.U2D.Physics/";
        const string ColliderAwakeOutlineColorKey = UniqueSettingsKey + "ColliderAwakeOutlineColor";
        const string ColliderAsleepOutlineColorKey = UniqueSettingsKey + "ColliderAsleepOutlineColor";
        const string ColliderAwakeFilledColorKey = UniqueSettingsKey + "ColliderAwakeFilledColor";
        const string ColliderAsleepFilledColorKey = UniqueSettingsKey + "ColliderAsleepFilledColor";
        const string ColliderBoundsColorKey = UniqueSettingsKey + "ColliderBoundsColor";
        const string ColliderContactColorKey = UniqueSettingsKey + "ColliderContactColor";
        const string CompositedColorKey = UniqueSettingsKey + "CompositedColor";
        const string ContactArrowScaleKey = UniqueSettingsKey + "ContactArrowScale";

        // These must match "GizmoDrawing.cpp" for the Physics2DEditor!
        static readonly Color DefaultColliderAwakeOutlineColor = new Color(0.568f, 0.956f, 0.545f, 1.0f);
        static readonly Color DefaultColliderAsleepOutlineColor = new Color(0.254f, 0.501f, 0.243f, 1.0f);
        static readonly Color DefaultColliderAwakeFilledColor = new Color(0.568f, 0.956f, 0.545f, 0.2f);
        static readonly Color DefaultColliderAsleepFilledColor = new Color(0.254f, 0.501f, 0.243f, 0.2f);
        static readonly Color DefaultColliderBoundsColor = new Color(1.0f, 1.0f, 0.0f, 0.75f);
        static readonly Color DefaultColliderContactColor = new Color(1.0f, 0.0f, 1.0f, 1.0f);
        static readonly Color DefaultCompositedColor = new Color(1.0f, 1.0f, 1.0f, 0.1f);
        static readonly float DefaultContactArrowScale = 0.2f;

        // Preference state.
        public Color colliderAwakeOutlineColor;
        public Color colliderAwakeFilledColor;
        public Color colliderAsleepOutlineColor;
        public Color colliderAsleepFilledColor;
        public Color colliderBoundsColor;
        public Color colliderContactColor;
        public Color compositedColor;
        public float contactArrowScale;

        void OnEnable()
        {
            // We only want it as a undo/redo context associated with nothing else.
            hideFlags = HideFlags.HideAndDontSave;

            ReadPreferenceState();

            // Register to be notified of undo/redo.
            Undo.undoRedoEvent += OnUndoRedo;
        }

        void OnDisable()
        {
            // Remove any undo for the preference state.
            Undo.ClearUndo(this);

            // Unregister undo/redo notifications.
            Undo.undoRedoEvent -= OnUndoRedo;
        }

        private void OnUndoRedo(in UndoRedoInfo info)
        {
            WritePreferenceState();
        }

        private void ReadPreferenceState()
        {
            // Read the preference state.
            colliderAwakeOutlineColor = GetColor(ColliderAwakeOutlineColorKey, DefaultColliderAwakeOutlineColor);
            colliderAwakeFilledColor = GetColor(ColliderAwakeFilledColorKey, DefaultColliderAwakeFilledColor);
            colliderAsleepOutlineColor = GetColor(ColliderAsleepOutlineColorKey, DefaultColliderAsleepOutlineColor);
            colliderAsleepFilledColor = GetColor(ColliderAsleepFilledColorKey, DefaultColliderAsleepFilledColor);
            colliderBoundsColor = GetColor(ColliderBoundsColorKey, DefaultColliderBoundsColor);
            colliderContactColor = GetColor(ColliderContactColorKey, DefaultColliderContactColor);
            compositedColor = GetColor(CompositedColorKey, DefaultCompositedColor);
            contactArrowScale = EditorPrefs.GetFloat(ContactArrowScaleKey, DefaultContactArrowScale);
        }

        private void WritePreferenceState()
        {
            // Wriute the preference state.
            SetColor(ColliderAwakeOutlineColorKey, colliderAwakeOutlineColor);
            SetColor(ColliderAwakeFilledColorKey, colliderAwakeFilledColor);
            SetColor(ColliderAsleepOutlineColorKey, colliderAsleepOutlineColor);
            SetColor(ColliderAsleepFilledColorKey, colliderAsleepFilledColor);
            SetColor(ColliderBoundsColorKey, colliderBoundsColor);
            SetColor(ColliderContactColorKey, colliderContactColor);
            SetColor(CompositedColorKey, compositedColor);
            EditorPrefs.SetFloat(ContactArrowScaleKey, contactArrowScale);
        }

        // Handle the UI.
        public void HandleUI(string searchContext)
        {
            EditorGUILayout.Space();
            EditorGUI.indentLevel++;

            // Collider Preferences.
            EditorGUILayout.LabelField(SettingsContent.CollidersLabelContent, EditorStyles.boldLabel);
            {
                // Collider Awake Outline Color.
                {
                    EditorGUI.BeginChangeCheck();
                    var color = EditorGUILayout.ColorField(SettingsContent.ColliderAwakeOutlineContent, colliderAwakeOutlineColor);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(this, SettingsContent.ColliderAwakeOutlineContent.text);
                        SetColor(ColliderAwakeOutlineColorKey, colliderAwakeOutlineColor = color);
                    }
                }

                // Collider Awake Filled Color.
                {
                    EditorGUI.BeginChangeCheck();
                    var color = EditorGUILayout.ColorField(SettingsContent.ColliderAwakeFilledContent, colliderAwakeFilledColor);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(this, SettingsContent.ColliderAwakeFilledContent.text);
                        SetColor(ColliderAwakeFilledColorKey, colliderAwakeFilledColor = color);
                    }
                }

                // Collider Asleep Outline Color.
                {
                    EditorGUI.BeginChangeCheck();
                    var color = EditorGUILayout.ColorField(SettingsContent.ColliderAsleepOutlineContent, colliderAsleepOutlineColor);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(this, SettingsContent.ColliderAsleepOutlineContent.text);
                        SetColor(ColliderAsleepOutlineColorKey, colliderAsleepOutlineColor = color);
                    }
                }

                // Collider Asleep Filled Color.
                {
                    EditorGUI.BeginChangeCheck();
                    var color = EditorGUILayout.ColorField(SettingsContent.ColliderAsleepFilledContent, colliderAsleepFilledColor);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(this, SettingsContent.ColliderAsleepFilledContent.text);
                        SetColor(ColliderAsleepFilledColorKey, colliderAsleepFilledColor = color);
                    }
                }

                // Collider Bounds Color.
                {
                    EditorGUI.BeginChangeCheck();
                    var color = EditorGUILayout.ColorField(SettingsContent.ColliderBoundsContent, colliderBoundsColor);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(this, SettingsContent.ColliderBoundsContent.text);
                        SetColor(ColliderBoundsColorKey, colliderBoundsColor = color);
                    }
                }

                // Composited Color.
                {
                    EditorGUI.BeginChangeCheck();
                    var color = EditorGUILayout.ColorField(SettingsContent.CompositedColorContent, compositedColor);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(this, SettingsContent.CompositedColorContent.text);
                        SetColor(CompositedColorKey, compositedColor = color);
                    }
                }
            }

            EditorGUILayout.Space();

            // Show Contact Preferences.
            EditorGUILayout.LabelField(SettingsContent.ContactsLabelContent, EditorStyles.boldLabel);
            {
                // Collider Contact Color.
                {
                    EditorGUI.BeginChangeCheck();
                    var color = EditorGUILayout.ColorField(SettingsContent.ColliderContactContent, colliderContactColor);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(this, SettingsContent.ColliderContactContent.text);
                        SetColor(ColliderContactColorKey, colliderContactColor = color);
                    }
                }

                // Contact Arrow Scale.
                {
                    EditorGUI.BeginChangeCheck();
                    var scale = EditorGUILayout.Slider(SettingsContent.ContactArrowScaleContent, contactArrowScale, 0.1f, 1f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(this, SettingsContent.ColliderContactContent.text);
                        EditorPrefs.SetFloat(ContactArrowScaleKey, contactArrowScale = scale);
                    }
                }
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Set Defaults"))
            {
                Undo.RecordObject(this, "Physics2DPreferences.SetDefaults");

                SetColor(ColliderAwakeOutlineColorKey, colliderAwakeOutlineColor = DefaultColliderAwakeOutlineColor);
                SetColor(ColliderAwakeFilledColorKey, colliderAwakeFilledColor = DefaultColliderAwakeFilledColor);
                SetColor(ColliderAsleepOutlineColorKey, colliderAsleepOutlineColor = DefaultColliderAsleepOutlineColor);
                SetColor(ColliderAsleepFilledColorKey, colliderAsleepFilledColor = DefaultColliderAsleepFilledColor);
                SetColor(ColliderBoundsColorKey, colliderBoundsColor = DefaultColliderBoundsColor);
                SetColor(ColliderContactColorKey, colliderContactColor = DefaultColliderContactColor);
                SetColor(CompositedColorKey, compositedColor = DefaultCompositedColor);
                EditorPrefs.SetFloat(ContactArrowScaleKey, contactArrowScale = DefaultContactArrowScale);
            }

            EditorGUI.indentLevel--;
        }

        private Color GetColor(string key, Color defaultColor)
        {
            return new Color(
                EditorPrefs.GetFloat(key + "_r", defaultColor.r),
                EditorPrefs.GetFloat(key + "_g", defaultColor.g),
                EditorPrefs.GetFloat(key + "_b", defaultColor.b),
                EditorPrefs.GetFloat(key + "_a", defaultColor.a));
        }

        private void SetColor(string key, Color color)
        {
            EditorPrefs.SetFloat(key + "_r", color.r);
            EditorPrefs.SetFloat(key + "_g", color.g);
            EditorPrefs.SetFloat(key + "_b", color.b);
            EditorPrefs.SetFloat(key + "_a", color.a);
        }
    }

    internal class Physics2DPreferences : SettingsProvider
    {
        private Physics2DPreferenceState m_PreferenceState;

        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider() { return new Physics2DPreferences(); }

        public Physics2DPreferences()
            : base("Preferences/2D/Physics", SettingsScope.User, GetSearchKeywordsFromGUIContentProperties<Physics2DPreferenceState.SettingsContent>())
        {}

        // Provider activate.
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            // Create the preference states.
            m_PreferenceState = ScriptableObject.CreateInstance<Physics2DPreferenceState>();

            // Hook-up the UI handling.
            guiHandler = (string searchContext) => { m_PreferenceState.HandleUI(searchContext); };

            base.OnActivate(searchContext, rootElement);
        }

        // Provider deactivate.
        public override void OnDeactivate()
        {
            base.OnDeactivate();

            // Remove the preference state.
            Object.DestroyImmediate(m_PreferenceState);
        }
    }
}
