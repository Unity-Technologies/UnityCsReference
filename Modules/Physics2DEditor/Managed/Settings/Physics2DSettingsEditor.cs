// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal;
using UnityEngine.LowLevelPhysics2D;

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
            public static readonly GUIContent kLowLevelLabel = EditorGUIUtility.TrTextContent("Low Level", "Low Level");
        }

        class Styles
        {
            public static readonly GUIStyle kSettingsFramebox = new GUIStyle(EditorStyles.frameBox) { padding = new RectOffset(1, 1, 1, 0) };
        }

        // These are to maintain UI selection.
        const string UniqueSettingsKey = "UnityEditor.U2D.Physics/";
        const string Physics2DSettingsTabKey = UniqueSettingsKey + "Physics2DSettingsTabSelected";
        const string PhysicsLowLevelSettingsKey = "PhysicsLowLevel2D";

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

            // Register to be notified of undo/redo.
            Undo.undoRedoEvent += OnUndoRedo;
        }

        public void OnDisable()
        {
            // Remove any undo for the preference state.
            Undo.ClearUndo(this);

            // Unregister undo/redo notifications.
            Undo.undoRedoEvent -= OnUndoRedo;
        }

        private void OnUndoRedo(in UndoRedoInfo info)
        {
            if (info.undoName == PhysicsLowLevelSettingsKey)
                LowLevelPhysics2D.PhysicsEditor.ReadProjectSettings();
        }

        private enum Physics2DSettingsTab : int
        {
            General = 0,
            CollisionMatrix = 1,
            LowLevel = 2,

            TabCount = 3
        }

        private Physics2DSettingsTab settingsTabSelected
        {
            get { return (Physics2DSettingsTab)EditorPrefs.GetInt(Physics2DSettingsTabKey, (int)Physics2DSettingsTab.General); }
            set { EditorPrefs.SetInt(Physics2DSettingsTabKey, (int)value); }
        }

        static GUIStyle s_TabFirst;
        static GUIStyle s_TabMiddle;
        static GUIStyle s_TabLast;

        static Rect GetTabSelection(Rect rect, Physics2DSettingsTab tabSelected, out GUIStyle tabStyle)
        {
            const int tabCount = (int)Physics2DSettingsTab.TabCount;
            int tabIndex = (int)tabSelected;

            if (s_TabFirst == null)
            {
                s_TabFirst = "Tab first";
                s_TabMiddle = "Tab middle";
                s_TabLast = "Tab last";
            }

            tabStyle = s_TabMiddle;

            if (tabIndex == 0)
            {
                tabStyle = s_TabFirst;
            }
            else if (tabIndex == tabCount-1)
            {
                tabStyle = s_TabLast;
            }
            else
            {
                tabStyle = s_TabMiddle;
            }

            var tabWidth = rect.width / tabCount;
            var left = Mathf.RoundToInt(tabIndex * tabWidth);
            var right = Mathf.RoundToInt((tabIndex + 1) * tabWidth);
            return new Rect(rect.x + left, rect.y, right - left, EditorGUI.kTabButtonHeight);
        }          

        public override void OnInspectorGUI()
        {
            var rect = EditorGUILayout.BeginVertical(Styles.kSettingsFramebox);

            // Select tabs.
            EditorGUI.BeginChangeCheck();

            GUIStyle buttonStyle = null;

            {
                // Draw General Settings Tab.
                var buttonRect = GetTabSelection(rect, Physics2DSettingsTab.General, out buttonStyle);
                if (GUI.Toggle(buttonRect, settingsTabSelected == Physics2DSettingsTab.General, Content.kGeneralLabel, buttonStyle))
                    settingsTabSelected = Physics2DSettingsTab.General;
            }

            {
                // Draw Collision Settings Tab.
                var buttonRect = GetTabSelection(rect, Physics2DSettingsTab.CollisionMatrix, out buttonStyle);
                if (GUI.Toggle(buttonRect, settingsTabSelected == Physics2DSettingsTab.CollisionMatrix, Content.kCollisionLabel, buttonStyle))
                    settingsTabSelected = Physics2DSettingsTab.CollisionMatrix;
            }

            {
                // Low Level Settings Tab.
                var buttonRect = GetTabSelection(rect, Physics2DSettingsTab.LowLevel, out buttonStyle);
                if (GUI.Toggle(buttonRect, settingsTabSelected == Physics2DSettingsTab.LowLevel, Content.kLowLevelLabel, buttonStyle))
                    settingsTabSelected = Physics2DSettingsTab.LowLevel;
            }

            GUILayoutUtility.GetRect(10, EditorGUI.kTabButtonHeight);
            EditorGUI.EndChangeCheck();

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
                            m_PhysicsLowLevelSettings.name);

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

                case Physics2DSettingsTab.LowLevel:
                    {
                        EditorGUI.BeginChangeCheck();

                        // Update object.
                        serializedObject.Update();
                    
                        EditorGUI.indentLevel++;

                        // Low Level Settings.
                        EditorGUILayout.Space(EditorGUI.kDefaultSpacing);
                        EditorGUILayout.ObjectField(m_PhysicsLowLevelSettings, typeof(PhysicsLowLevelSettings2D));

                        // Padding.
                        EditorGUILayout.Space(EditorGUI.kDefaultSpacing * 2f);

                        EditorGUI.indentLevel--;

                        // Apply changes.
                        serializedObject.ApplyModifiedProperties();

                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(this, PhysicsLowLevelSettingsKey);
                            LowLevelPhysics2D.PhysicsEditor.ReadProjectSettings();
                        }
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
