// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor
{
    [CustomEditor(typeof(Physics2DSettings))]
    internal class Physics2DSettingsEditor : ProjectSettingsBaseEditor
    {
        internal static class Content
        {
            public static readonly GUIContent kMultithreadingLabel = EditorGUIUtility.TrTextContent("Multithreading", "Allows the configuration of multi-threaded physics using the job system.");
            public static readonly GUIContent kGizmosLabel = EditorGUIUtility.TrTextContent("Gizmos", "Allows the configuration of 2D physics gizmos shown in the Editor.");
            public static readonly GUIContent kSimulationModeLabel = EditorGUIUtility.TrTextContent("Simulation Mode", "Controls when and how the physics simulation will be run.");
            public static readonly GUIContent kLayerCollisionMatrixLabel = EditorGUIUtility.TrTextContent("Layer Collision Matrix", "Allows the configuration of the layer-based collision detection.");
            public static readonly GUIContent kReuseCollisionCallbacksLabel = EditorGUIUtility.TrTextContent("This option boosts performance when ON. With it OFF it can result in poor performance due to GC pressure. For this reason, it defaults to being ON.");
            public static readonly GUIContent kAutoSyncTransformsLabel = EditorGUIUtility.TrTextContent("This option has been deprecated and is for legacy support only. It can result in extremely poor performance when ON. For this reason, it defaults to being OFF.");

            public static readonly GUIContent kGeneralLabel = EditorGUIUtility.TrTextContent("General Settings", "General Settings");
            public static readonly GUIContent kCollisionLabel = EditorGUIUtility.TrTextContent("Layer Collision Matrix", "Collision Settings");
        }

        class Styles
        {
            public static readonly GUIStyle kSettingsFramebox = new GUIStyle(EditorStyles.frameBox) { padding = new RectOffset(0, 0, 0, 0) };
        }

        // These are to maintain UI selection.
        const string UniqueSettingsKey = "UnityEditor.U2D.Physics/";
        const string Physics2DSettingsTabKey = UniqueSettingsKey + "Physics2DSettingsTabSelected";

        // Tab styles.
        static GUIStyle s_TabFirstStyle;
        static GUIStyle s_TabMiddleStyle;

        SerializedProperty m_ReuseCollisionCallbacks;
        SerializedProperty m_AutoSyncTransforms;
        SerializedProperty m_Multithreading;
        SerializedProperty m_GizmoOptions;

        SerializedProperty m_SimulationMode;
        SerializedProperty m_SimulationLayers;
        SerializedProperty m_UseSubStepping;
        SerializedProperty m_UseSubStepContacts;
        SerializedProperty m_MinSubStepFPS;
        SerializedProperty m_MaxSubStepCount;

        SerializedProperty m_PhysicsLowLevelSettings;


        public void OnEnable()
        {
            m_ReuseCollisionCallbacks = serializedObject.FindProperty("m_ReuseCollisionCallbacks");
            m_AutoSyncTransforms = serializedObject.FindProperty("m_AutoSyncTransforms");
            m_Multithreading = serializedObject.FindProperty("m_JobOptions");
            m_SimulationMode = serializedObject.FindProperty("m_SimulationMode");
            m_SimulationLayers = serializedObject.FindProperty("m_SimulationLayers");
            m_UseSubStepping = serializedObject.FindProperty("m_UseSubStepping");
            m_UseSubStepContacts = serializedObject.FindProperty("m_UseSubStepContacts");
            m_MaxSubStepCount = serializedObject.FindProperty("m_MaxSubStepCount");
            m_MinSubStepFPS = serializedObject.FindProperty("m_MinSubStepFPS");
            m_GizmoOptions = serializedObject.FindProperty("m_GizmoOptions");

            m_PhysicsLowLevelSettings = serializedObject.FindProperty("m_PhysicsLowLevelSettings");
        }

        public void OnDisable()
        {
            // Remove any undo for the preference state.
            Undo.ClearUndo(this);
        }

        private enum Physics2DSettingsTab : int
        {
            General = 0,
            CollisionMatrix = 1
        }

        private Physics2DSettingsTab settingsTabSelected
        {
            get { return (Physics2DSettingsTab)EditorPrefs.GetInt(Physics2DSettingsTabKey, (int)Physics2DSettingsTab.General); }
            set { EditorPrefs.SetInt(Physics2DSettingsTabKey, (int)value); }
        }

        private void DrawTabSelector(Physics2DSettingsTab tab, GUIContent content)
        {
            // Select Style.
            GUIStyle style = null;
            if (tab == Physics2DSettingsTab.General)
                style = s_TabFirstStyle;
            else if (tab == Physics2DSettingsTab.CollisionMatrix)
                style = s_TabMiddleStyle;

            // Draw tab selector.
            if (GUILayout.Toggle(settingsTabSelected == tab, content, style))
                settingsTabSelected = tab;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.BeginVertical(Styles.kSettingsFramebox);

            // Initialize tab styles.
            if (s_TabFirstStyle == null)
            {
                s_TabFirstStyle = "Tab first";
                s_TabMiddleStyle = "Tab middle";
            }

            // Tab selectors.
            {
                GUILayout.BeginHorizontal();

                DrawTabSelector(Physics2DSettingsTab.General, Content.kGeneralLabel);
                DrawTabSelector(Physics2DSettingsTab.CollisionMatrix, Content.kCollisionLabel);

                GUILayout.EndHorizontal();
            }

            // Handle the selected tab.
            switch (settingsTabSelected)
            {
                case Physics2DSettingsTab.General:
                    {
                        // Update object.
                        serializedObject.Update();

                        // Padding.
                        EditorGUILayout.Space(EditorGUI.kDefaultSpacing * 2f);
                        EditorGUI.indentLevel++;

                    // Draw standard property settings.
                    DrawPropertiesExcluding(
                        serializedObject,
                        m_ReuseCollisionCallbacks.name,
                        m_AutoSyncTransforms.name,
                        m_SimulationMode.name,
                        m_SimulationLayers.name,
                        m_UseSubStepping.name,
                        m_UseSubStepContacts.name,
                        m_MaxSubStepCount.name,
                        m_MinSubStepFPS.name,
                        m_GizmoOptions.name,
                        m_Multithreading.name,
                        m_PhysicsLowLevelSettings.name
                        );

                    // Reuse Collision Callbacks.
                    EditorGUILayout.PropertyField(m_ReuseCollisionCallbacks);
                        if (!m_ReuseCollisionCallbacks.boolValue)
                        {
                            EditorGUILayout.HelpBox(Content.kReuseCollisionCallbacksLabel.ToString(), MessageType.Warning, false);
                            EditorGUILayout.Space();
                        }

                        // Auto Sync Transforms.
                        EditorGUILayout.PropertyField(m_AutoSyncTransforms);
                        if (m_AutoSyncTransforms.boolValue)
                        {
                            EditorGUILayout.HelpBox(Content.kAutoSyncTransformsLabel.ToString(), MessageType.Warning, false);
                            EditorGUILayout.Space();
                        }

                        // Draw the Simulation Mode options.
                        var simulationMode = (SimulationMode2D)EditorGUILayout.EnumPopup(Content.kSimulationModeLabel, (SimulationMode2D)m_SimulationMode.enumValueIndex);
                        m_SimulationMode.enumValueIndex = (int)simulationMode;

                        // If the simulation mode is "Update" or "Script" then present the sub-stepping options.
                        if (simulationMode == SimulationMode2D.Update || simulationMode == SimulationMode2D.Script)
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.PropertyField(m_UseSubStepping);
                            EditorGUILayout.PropertyField(m_UseSubStepContacts);
                            EditorGUILayout.PropertyField(m_MaxSubStepCount);

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(m_MinSubStepFPS);
                            EditorGUILayout.LabelField(string.Format($"{(1f / m_MinSubStepFPS.floatValue).ToString("0.00000 seconds")} (delta time)"));
                            GUILayout.EndHorizontal();

                            EditorGUI.indentLevel--;
                        }

                        // If the simulation mode is "FixedUpdate" or "Update" then present the simulation layers.
                        if (simulationMode == SimulationMode2D.FixedUpdate || simulationMode == SimulationMode2D.Update)
                        {
                            EditorGUILayout.PropertyField(m_SimulationLayers);
                        }

                        // Draw the Gizmo options.
                        Physics2D.GizmoOptions gizmoOptions = (Physics2D.GizmoOptions)m_GizmoOptions.intValue;
                        gizmoOptions = (Physics2D.GizmoOptions)EditorGUILayout.EnumFlagsField(Content.kGizmosLabel, gizmoOptions);
                        m_GizmoOptions.intValue = (int)gizmoOptions;

                        // Multithreading.
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(0);
                        EditorGUILayout.PropertyField(m_Multithreading, Content.kMultithreadingLabel, true);
                        GUILayout.EndHorizontal();

                        // Padding.
                        EditorGUILayout.Space(EditorGUI.kDefaultSpacing * 2f);
                        EditorGUI.indentLevel--;

                        // Apply changes.
                        serializedObject.ApplyModifiedProperties();
                    }
                    break;

                case Physics2DSettingsTab.CollisionMatrix:
                    {
                        // Padding.
                        EditorGUILayout.Space(EditorGUI.kDefaultSpacing * 2f);

                        // Layer Collision Matrix.
                        LayerCollisionMatrixGUI2D.Draw(
                            Content.kLayerCollisionMatrixLabel,
                            (int layerA, int layerB) => { return !Physics2D.GetIgnoreLayerCollision(layerA, layerB); },
                            (int layerA, int layerB, bool val) => { Physics2D.IgnoreLayerCollision(layerA, layerB, !val); }
                            );
                    }
                    break;

                default:
                    break;
            }

            EditorGUILayout.EndVertical();
        }

        [SettingsProvider]
        internal static SettingsProvider CreateProjectSettingsProvider()
        {
            var provider = AssetSettingsProvider.CreateProviderFromAssetPath(
                "Project/Physics 2D", "ProjectSettings/Physics2DSettings.asset",
                SettingsProvider.GetSearchKeywordsFromPath("ProjectSettings/Physics2DSettings.asset"));
            return provider;
        }

        static bool GetValue(int layerA, int layerB) { return !Physics2D.GetIgnoreLayerCollision(layerA, layerB); }
        static void SetValue(int layerA, int layerB, bool val) { Physics2D.IgnoreLayerCollision(layerA, layerB, !val); }
    }
}
