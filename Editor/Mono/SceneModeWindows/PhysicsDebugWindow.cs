// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine;

using System;
using System.Collections.Generic;

namespace UnityEditor
{
    public class PhysicsDebugWindow : EditorWindow
    {
        [SerializeField] bool m_FilterColliderTypesFoldout = false;
        [SerializeField] bool m_ColorFoldout = false;
        [SerializeField] bool m_RenderingFoldout = false;
        [SerializeField] Vector2 m_MainScrollPos = Vector2.zero;

        bool m_PickAdded = false;
        bool m_MouseLeaveListenerAdded = false;

        private static class Contents
        {
            public static readonly GUIContent physicsDebug          = new GUIContent("Physics Debug");
            public static readonly GUIContent workflow              = new GUIContent("Workflow", "The \"Hide\" mode is useful for fast discovery while the \"Show\" mode is useful for finding specific items.");
            public static readonly GUIContent staticColor           = new GUIContent("Static Colliders");
            public static readonly GUIContent triggerColor          = new GUIContent("Triggers");
            public static readonly GUIContent rigidbodyColor        = new GUIContent("Rigidbodies");
            public static readonly GUIContent kinematicColor        = new GUIContent("Kinematic Bodies");
            public static readonly GUIContent sleepingBodyColor     = new GUIContent("Sleeping Bodies");
            public static readonly GUIContent forceOverdraw         = EditorGUIUtility.TextContent("Force Overdraw|Draws Collider geometry on top of render geometry");
            public static readonly GUIContent viewDistance          = EditorGUIUtility.TextContent("View Distance|Lower bound on distance from camera to physics geometry.");
            public static readonly GUIContent terrainTilesMax       = EditorGUIUtility.TextContent("Terrain Tiles Max|Number of mesh tiles to drawn.");
            public static readonly GUIContent devOptions            = EditorGUIUtility.TextContent("devOptions");
            public static readonly GUIContent forceDot              = EditorGUIUtility.TextContent("Force Dot");
            public static readonly GUIContent toolsHidden           = EditorGUIUtility.TextContent("Hide tools");
            public static readonly GUIContent showCollisionGeometry = EditorGUIUtility.TextContent("Collision Geometry");
            public static readonly GUIContent enableMouseSelect     = EditorGUIUtility.TextContent("Mouse Select");
            public static readonly GUIContent useSceneCam           = EditorGUIUtility.TextContent("Use Scene Cam");
            public static readonly ColorPickerHDRConfig pickerConfig = new ColorPickerHDRConfig(0f, 99f, 1 / 99f, 3f);
        }

        //---------------------------------------------------------------------

        [MenuItem("Window/Physics Debugger", false, 2101)]
        public static PhysicsDebugWindow ShowWindow()
        {
            var window = GetWindow(typeof(PhysicsDebugWindow)) as PhysicsDebugWindow;
            if (window != null)
            {
                window.titleContent.text = "Physics Debug";
            }
            return window;
        }

        void AddPicker()
        {
            if (!m_PickAdded || HandleUtility.pickClosestGameObjectDelegate == null)
                HandleUtility.pickClosestGameObjectDelegate += PhysicsVisualizationSettings.PickClosestGameObject;
            m_PickAdded = true;
        }

        void RemovePicker()
        {
            if (m_PickAdded && HandleUtility.pickClosestGameObjectDelegate != null)
                HandleUtility.pickClosestGameObjectDelegate -= PhysicsVisualizationSettings.PickClosestGameObject;
            m_PickAdded = false;
        }

        void OnBecameVisible()
        {
            PhysicsVisualizationSettings.InitDebugDraw();
            SceneView.onSceneGUIDelegate += OnSceneViewGUI;
            RepaintSceneAndGameViews();
        }

        void OnBecameInvisible()
        {
            RemovePicker();
            SceneView.onSceneGUIDelegate -= OnSceneViewGUI;
            PhysicsVisualizationSettings.DeinitDebugDraw();
            RepaintSceneAndGameViews();
        }

        static void RepaintSceneAndGameViews()
        {
            SceneView.RepaintAll();
        }

        void OnSceneViewGUI(SceneView view)
        {
            SceneViewOverlay.Window(Contents.physicsDebug, DisplayControls, (int)SceneViewOverlay.Ordering.PhysicsDebug,
                SceneViewOverlay.WindowDisplayOption.OneWindowPerTarget);
        }

        void AddMouseLeaveListener()
        {
            if (!m_MouseLeaveListenerAdded)
            {
                EditorApplication.update += OnMouseLeaveCheck;
                m_MouseLeaveListenerAdded = true;
            }
        }

        void OnMouseLeaveCheck()
        {
            if (m_MouseLeaveListenerAdded && EditorWindow.mouseOverWindow as SceneView == null)
            {
                EditorApplication.update -= OnMouseLeaveCheck;
                m_MouseLeaveListenerAdded = false;

                if (PhysicsVisualizationSettings.HasMouseHighlight())
                {
                    PhysicsVisualizationSettings.ClearMouseHighlight();
                }
            }
        }

        void OnGUI()
        {
            var dirtyCount = PhysicsVisualizationSettings.dirtyCount;

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Workflow
            PhysicsVisualizationSettings.filterWorkflow = (PhysicsVisualizationSettings.FilterWorkflow)EditorGUILayout.EnumPopup(
                    PhysicsVisualizationSettings.filterWorkflow, EditorStyles.toolbarPopup, GUILayout.Width(130));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Reset", EditorStyles.toolbarButton))
                PhysicsVisualizationSettings.Reset();

            EditorGUILayout.EndHorizontal();

            m_MainScrollPos = GUILayout.BeginScrollView(m_MainScrollPos);

            PhysicsVisualizationSettings.FilterWorkflow filterMode = PhysicsVisualizationSettings.filterWorkflow;
            string action = (filterMode == PhysicsVisualizationSettings.FilterWorkflow.ShowSelectedItems) ? "Show " : "Hide ";

            // Layers
            int oldConcatenatedMask = InternalEditorUtility.LayerMaskToConcatenatedLayersMask(
                    PhysicsVisualizationSettings.GetShowCollisionLayerMask(filterMode));

            int newConcatenatedMask = EditorGUILayout.MaskField(
                    GUIContent.Temp(action + "Layers", action + "selected layers"), oldConcatenatedMask, InternalEditorUtility.layers);

            PhysicsVisualizationSettings.SetShowCollisionLayerMask(
                filterMode, (int)InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(newConcatenatedMask));

            // Static Colliders
            PhysicsVisualizationSettings.SetShowStaticColliders(filterMode, EditorGUILayout.Toggle(
                    GUIContent.Temp(action + "Static Colliders", action + "collision geometry from Colliders that do not have a Rigidbody")
                    , PhysicsVisualizationSettings.GetShowStaticColliders(filterMode)));

            // Triggers
            PhysicsVisualizationSettings.SetShowTriggers(filterMode, EditorGUILayout.Toggle(
                    GUIContent.Temp(action + "Triggers", action + "collision geometry from Colliders that have 'isTrigger' enabled")
                    , PhysicsVisualizationSettings.GetShowTriggers(filterMode)));

            // Rigidbodies
            PhysicsVisualizationSettings.SetShowRigidbodies(filterMode, EditorGUILayout.Toggle(
                    GUIContent.Temp(action + "Rigidbodies", action + "collision geometry from Rigidbodies")
                    , PhysicsVisualizationSettings.GetShowRigidbodies(filterMode)));

            // Kinematic Bodies
            PhysicsVisualizationSettings.SetShowKinematicBodies(filterMode, EditorGUILayout.Toggle(
                    GUIContent.Temp(action + "Kinematic Bodies", action + "collision geometry from Kinematic Rigidbodies")
                    , PhysicsVisualizationSettings.GetShowKinematicBodies(filterMode)));

            // Sleeping Bodies
            PhysicsVisualizationSettings.SetShowSleepingBodies(filterMode, EditorGUILayout.Toggle(
                    GUIContent.Temp(action + "Sleeping Bodies", action + "collision geometry from Sleeping Rigidbodies")
                    , PhysicsVisualizationSettings.GetShowSleepingBodies(filterMode)));

            m_FilterColliderTypesFoldout = EditorGUILayout.Foldout(m_FilterColliderTypesFoldout, "Collider Types");
            if (m_FilterColliderTypesFoldout)
            {
                EditorGUI.indentLevel++;
                float oldWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 200;

                // BoxCollider
                PhysicsVisualizationSettings.SetShowBoxColliders(filterMode, EditorGUILayout.Toggle(
                        GUIContent.Temp(action + "BoxColliders", action + "collision geometry from BoxColliders")
                        , PhysicsVisualizationSettings.GetShowBoxColliders(filterMode)));

                // SphereCollider
                PhysicsVisualizationSettings.SetShowSphereColliders(filterMode, EditorGUILayout.Toggle(
                        GUIContent.Temp(action + "SphereColliders", action + "collision geometry from SphereColliders")
                        , PhysicsVisualizationSettings.GetShowSphereColliders(filterMode)));

                // CapsuleCollider
                PhysicsVisualizationSettings.SetShowCapsuleColliders(filterMode, EditorGUILayout.Toggle(
                        GUIContent.Temp(action + "CapsuleColliders", action + "collision geometry from CapsuleColliders")
                        , PhysicsVisualizationSettings.GetShowCapsuleColliders(filterMode)));

                // MeshCollider convex
                PhysicsVisualizationSettings.SetShowMeshColliders(filterMode, PhysicsVisualizationSettings.MeshColliderType.Convex, EditorGUILayout.Toggle(
                        GUIContent.Temp(action + "MeshColliders (convex)", action + "collision geometry from convex MeshColliders")
                        , PhysicsVisualizationSettings.GetShowMeshColliders(filterMode, PhysicsVisualizationSettings.MeshColliderType.Convex)));

                // MeshCollider non-convex
                PhysicsVisualizationSettings.SetShowMeshColliders(filterMode, PhysicsVisualizationSettings.MeshColliderType.NonConvex, EditorGUILayout.Toggle(
                        GUIContent.Temp(action + "MeshColliders (concave)", action + "collision geometry from non-convex MeshColliders")
                        , PhysicsVisualizationSettings.GetShowMeshColliders(filterMode, PhysicsVisualizationSettings.MeshColliderType.NonConvex)));

                // TerrainCollider
                PhysicsVisualizationSettings.SetShowTerrainColliders(filterMode, EditorGUILayout.Toggle(
                        GUIContent.Temp(action + "TerrainColliders", action + "collision geometry from TerrainColliders")
                        , PhysicsVisualizationSettings.GetShowTerrainColliders(filterMode)));

                EditorGUIUtility.labelWidth = oldWidth;
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(4);

            // Selection buttons
            GUILayout.BeginHorizontal();

            bool selectNone = GUILayout.Button(action + "None", "MiniButton");
            bool selectAll = GUILayout.Button(action + "All", "MiniButton");
            if (selectNone || selectAll)
                PhysicsVisualizationSettings.SetShowForAllFilters(filterMode, selectAll);

            GUILayout.EndHorizontal();

            m_ColorFoldout = EditorGUILayout.Foldout(m_ColorFoldout, "Colors");
            if (m_ColorFoldout)
            {
                EditorGUI.indentLevel++;

                PhysicsVisualizationSettings.staticColor = EditorGUILayout.ColorField(Contents.staticColor
                        , PhysicsVisualizationSettings.staticColor, false, true, false, Contents.pickerConfig);

                PhysicsVisualizationSettings.triggerColor = EditorGUILayout.ColorField(Contents.triggerColor
                        , PhysicsVisualizationSettings.triggerColor, false, true, false, Contents.pickerConfig);

                PhysicsVisualizationSettings.rigidbodyColor = EditorGUILayout.ColorField(Contents.rigidbodyColor
                        , PhysicsVisualizationSettings.rigidbodyColor, false, true, false, Contents.pickerConfig);

                PhysicsVisualizationSettings.kinematicColor = EditorGUILayout.ColorField(Contents.kinematicColor
                        , PhysicsVisualizationSettings.kinematicColor, false, true, false, Contents.pickerConfig);

                PhysicsVisualizationSettings.sleepingBodyColor = EditorGUILayout.ColorField(Contents.sleepingBodyColor
                        , PhysicsVisualizationSettings.sleepingBodyColor, false, true, false, Contents.pickerConfig);

                PhysicsVisualizationSettings.colorVariance = EditorGUILayout.Slider("Variation"
                        , PhysicsVisualizationSettings.colorVariance, 0f, 1f);

                EditorGUI.indentLevel--;
            }

            m_RenderingFoldout = EditorGUILayout.Foldout(m_RenderingFoldout, "Rendering");
            if (m_RenderingFoldout)
            {
                EditorGUI.indentLevel++;

                PhysicsVisualizationSettings.baseAlpha = 1f - EditorGUILayout.Slider("Transparency"
                        , 1f - PhysicsVisualizationSettings.baseAlpha, 0f, 1f);

                PhysicsVisualizationSettings.forceOverdraw = EditorGUILayout.Toggle(Contents.forceOverdraw
                        , PhysicsVisualizationSettings.forceOverdraw);

                PhysicsVisualizationSettings.viewDistance = EditorGUILayout.FloatField(Contents.viewDistance
                        , PhysicsVisualizationSettings.viewDistance);

                PhysicsVisualizationSettings.terrainTilesMax = EditorGUILayout.IntField(Contents.terrainTilesMax
                        , PhysicsVisualizationSettings.terrainTilesMax);

                EditorGUI.indentLevel--;
            }

            if (Unsupported.IsDeveloperBuild() || PhysicsVisualizationSettings.devOptions)
            {
                PhysicsVisualizationSettings.devOptions = EditorGUILayout.Toggle(Contents.devOptions
                        , PhysicsVisualizationSettings.devOptions);
            }

            if (PhysicsVisualizationSettings.devOptions)
            {
                PhysicsVisualizationSettings.dotAlpha = EditorGUILayout.Slider("dotAlpha"
                        , PhysicsVisualizationSettings.dotAlpha, -1f, 1f);

                PhysicsVisualizationSettings.forceDot = EditorGUILayout.Toggle(Contents.forceDot
                        , PhysicsVisualizationSettings.forceDot);

                Tools.hidden = EditorGUILayout.Toggle(Contents.toolsHidden
                        , Tools.hidden);
            }

            GUILayout.EndScrollView();

            if (dirtyCount != PhysicsVisualizationSettings.dirtyCount)
                RepaintSceneAndGameViews();
        }

        void DisplayControls(UnityEngine.Object o, SceneView view)
        {
            var dirtyCount = PhysicsVisualizationSettings.dirtyCount;

            PhysicsVisualizationSettings.showCollisionGeometry = EditorGUILayout.Toggle(Contents.showCollisionGeometry
                    , PhysicsVisualizationSettings.showCollisionGeometry);

            PhysicsVisualizationSettings.enableMouseSelect = EditorGUILayout.Toggle(Contents.enableMouseSelect
                    , PhysicsVisualizationSettings.enableMouseSelect);

            if (PhysicsVisualizationSettings.devOptions)
            {
                PhysicsVisualizationSettings.useSceneCam = EditorGUILayout.Toggle(Contents.useSceneCam
                        , PhysicsVisualizationSettings.useSceneCam);
            }

            Vector2 mousePos = Event.current.mousePosition;
            Rect sceneViewOnly = new Rect(0, EditorGUI.kWindowToolbarHeight, view.position.width, view.position.height - EditorGUI.kWindowToolbarHeight);
            bool mouseInSceneView = sceneViewOnly.Contains(Event.current.mousePosition);

            bool allowInteraction = PhysicsVisualizationSettings.showCollisionGeometry && PhysicsVisualizationSettings.enableMouseSelect && mouseInSceneView;

            if (allowInteraction)
            {
                AddPicker();
                AddMouseLeaveListener();

                // mouse-over highlight
                if (Event.current.type == EventType.MouseMove)
                    PhysicsVisualizationSettings.UpdateMouseHighlight(HandleUtility.GUIPointToScreenPixelCoordinate(mousePos));

                if (Event.current.type == EventType.MouseDrag)
                    PhysicsVisualizationSettings.ClearMouseHighlight();
            }
            else
            {
                RemovePicker();
                PhysicsVisualizationSettings.ClearMouseHighlight();
            }

            if (dirtyCount != PhysicsVisualizationSettings.dirtyCount)
                RepaintSceneAndGameViews();
        }
    }
}
