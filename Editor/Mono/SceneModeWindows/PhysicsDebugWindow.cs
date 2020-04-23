// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine;

using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor.AnimatedValues;

namespace UnityEditor
{
    public class PhysicsDebugWindow : EditorWindow
    {
        [SerializeField] Vector2 m_MainScrollPos = Vector2.zero;

        private SavedBool m_ShowInfoFoldout;
        private SavedBool m_ShowColliderTypeFoldout;
        private SavedBool m_ColorFoldout;
        private SavedBool m_RenderingFoldout;


        bool m_PickAdded = false;
        bool m_MouseLeaveListenerAdded = false;
        bool m_SceneViewListenerAdded = false;
        OverlayWindow m_OverlayWindow;
        private static class Style
        {
            public static readonly GUIContent physicsDebug          = EditorGUIUtility.TrTextContent("Physics Debug");
            public static readonly GUIContent workflow              = EditorGUIUtility.TrTextContent("Workflow", "The \"Hide\" mode is useful for fast discovery while the \"Show\" mode is useful for finding specific items.");
            public static readonly GUIContent staticColor           = EditorGUIUtility.TrTextContent("Static Colliders");
            public static readonly GUIContent triggerColor          = EditorGUIUtility.TrTextContent("Triggers");
            public static readonly GUIContent rigidbodyColor        = EditorGUIUtility.TrTextContent("Rigidbodies");
            public static readonly GUIContent kinematicColor        = EditorGUIUtility.TrTextContent("Kinematic Bodies");
            public static readonly GUIContent sleepingBodyColor     = EditorGUIUtility.TrTextContent("Sleeping Bodies");
            public static readonly GUIContent colorVariaition       = EditorGUIUtility.TrTextContent("Variation");
            public static readonly GUIContent forceOverdraw         = EditorGUIUtility.TrTextContent("Force Overdraw", "Draws Collider geometry on top of render geometry");
            public static readonly GUIContent transparency          = EditorGUIUtility.TrTextContent("Transparency");
            public static readonly GUIContent viewDistance          = EditorGUIUtility.TrTextContent("View Distance", "Lower bound on distance from camera to physics geometry.");
            public static readonly GUIContent terrainTilesMax       = EditorGUIUtility.TrTextContent("Terrain Tiles Max", "Number of mesh tiles to drawn.");
            public static readonly GUIContent devOptions            = EditorGUIUtility.TrTextContent("devOptions");
            public static readonly GUIContent forceDot              = EditorGUIUtility.TrTextContent("Force Dot");
            public static readonly GUIContent dotAlpha              = EditorGUIUtility.TrTextContent("DotAlpha");
            public static readonly GUIContent toolsHidden           = EditorGUIUtility.TrTextContent("Hide tools");
            public static readonly GUIContent showCollisionGeometry = EditorGUIUtility.TrTextContent("Collision Geometry");
            public static readonly GUIContent enableMouseSelect     = EditorGUIUtility.TrTextContent("Mouse Select");
            public static readonly GUIContent useSceneCam           = EditorGUIUtility.TrTextContent("Use Scene Cam");
            public static readonly GUIContent showLayers            = EditorGUIUtility.TrTextContent("Show Layers", "Show selected layers");
            public static readonly GUIContent showPhysicsScenes     = EditorGUIUtility.TrTextContent("Show Physics Scene", "Show selected physics scenes");
            public static readonly GUIContent showStaticCollider    = EditorGUIUtility.TrTextContent("Show Static Colliders", "Show collision geometry from Colliders that do not have a Rigidbody");
            public static readonly GUIContent showTriggers          = EditorGUIUtility.TrTextContent("Show Triggers", "Show collision geometry from Colliders that have 'isTrigger' enabled");
            public static readonly GUIContent showRigibodies        = EditorGUIUtility.TrTextContent("Show Rigidbodies", "Show collision geometry from Rigidbodies");
            public static readonly GUIContent showKinematicBodies   = EditorGUIUtility.TrTextContent("Show Kinematic Bodies", "Show collision geometry from Kinematic Rigidbodies");
            public static readonly GUIContent showSleepingBodies    = EditorGUIUtility.TrTextContent("Show Sleeping Bodies", "Show collision geometry from Sleeping Rigidbodies");
            public static readonly GUIContent colliderTypes         = EditorGUIUtility.TrTextContent("Collider Types");
            public static readonly GUIContent showBoxCollider       = EditorGUIUtility.TrTextContent("Show BoxColliders", "Show collision geometry that is BoxCollider");
            public static readonly GUIContent showSphereCollider    = EditorGUIUtility.TrTextContent("Show SphereColliders", "Show collision geometry that is SphereCollider");
            public static readonly GUIContent showCapsuleCollider   = EditorGUIUtility.TrTextContent("Show CapsuleColliders", "Show collision geometry that is CapsuleCollider");
            public static readonly GUIContent showConvexMeshCollider = EditorGUIUtility.TrTextContent("Show MeshColliders (convex)", "Show collision geometry that is Convex MeshCollider");
            public static readonly GUIContent showConcaveMeshCollider = EditorGUIUtility.TrTextContent("Show MeshColliders (concave)", "Show collision geometry that is Concave MeshCollider");
            public static readonly GUIContent showTerrainCollider   = EditorGUIUtility.TrTextContent("Show TerrainColliders", "Show collision geometry that is TerrainCollider");
            public static readonly GUIContent selectedObjectInfo    = EditorGUIUtility.TrTextContent("Selected Object Info");
            public static readonly GUIContent showAll               = EditorGUIUtility.TrTextContent("Show All");
            public static readonly GUIContent showNone              = EditorGUIUtility.TrTextContent("Show None");
            public static readonly GUIContent gameObject            = EditorGUIUtility.TrTextContent("GameObject");
            public static readonly GUIContent scene                 = EditorGUIUtility.TrTextContent("Scene");
            public static readonly GUIContent colors                = EditorGUIUtility.TrTextContent("Colors");
            public static readonly GUIContent rendering             = EditorGUIUtility.TrTextContent("Rendering");
        }

        //---------------------------------------------------------------------

        [MenuItem("Window/Analysis/Physics Debugger", false, 11)]
        public static PhysicsDebugWindow ShowWindow()
        {
            var window = GetWindow(typeof(PhysicsDebugWindow)) as PhysicsDebugWindow;
            if (window != null)
            {
                window.titleContent.text = "Physics Debug";
            }
            return window;
        }

        public void OnEnable()
        {
            m_ShowInfoFoldout = new SavedBool("PhysicsDebugWindow.ShowFoldout", false);
            m_ShowColliderTypeFoldout = new SavedBool("PhysicsDebugWindow.ShowColliderType", false);
            m_ColorFoldout = new SavedBool("PhysicsDebugWindow.ShowColorFoldout", false);
            m_RenderingFoldout = new SavedBool("PhysicsDebugWindow.ShowRenderingFoldout", false);
            m_OverlayWindow = new OverlayWindow(Style.physicsDebug, DisplayControls, (int)SceneViewOverlay.Ordering.PhysicsDebug, null,
                SceneViewOverlay.WindowDisplayOption.OneWindowPerTarget);
        }

        void AddPicker()
        {
            if (!m_PickAdded)
                HandleUtility.pickGameObjectCustomPasses += PhysicsVisualizationSettings.PickClosestGameObject;
            m_PickAdded = true;
        }

        void RemovePicker()
        {
            if (m_PickAdded)
                HandleUtility.pickGameObjectCustomPasses -= PhysicsVisualizationSettings.PickClosestGameObject;
            m_PickAdded = false;
        }

        void OnBecameVisible()
        {
            if (!m_SceneViewListenerAdded)
            {
                PhysicsVisualizationSettings.InitDebugDraw();
                SceneView.duringSceneGui += OnSceneViewGUI;
                m_SceneViewListenerAdded = true;
            }

            RepaintSceneAndGameViews();
        }

        void OnBecameInvisible()
        {
            RemovePicker();

            if (m_SceneViewListenerAdded)
            {
                SceneView.duringSceneGui -= OnSceneViewGUI;
                PhysicsVisualizationSettings.DeinitDebugDraw();
                m_SceneViewListenerAdded = false;
            }

            RepaintSceneAndGameViews();
        }

        static void RepaintSceneAndGameViews()
        {
            SceneView.RepaintAll();
        }

        void OnSceneViewGUI(SceneView view)
        {
            SceneViewOverlay.ShowWindow(m_OverlayWindow);
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

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Reset", EditorStyles.toolbarButton))
                PhysicsVisualizationSettings.Reset();

            EditorGUILayout.EndHorizontal();

            m_MainScrollPos = GUILayout.BeginScrollView(m_MainScrollPos);

            {
                EditorGUILayout.Space();
                m_ShowInfoFoldout.value = EditorGUILayout.Foldout(m_ShowInfoFoldout.value, Style.selectedObjectInfo);
                if (m_ShowInfoFoldout.value)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.Space();
                    EditorGUI.BeginDisabledGroup(true);
                    var transforms = Selection.transforms;
                    if (transforms.Length > 0)
                    {
                        foreach (var tr in transforms)
                        {
                            EditorGUILayout.TextField(Style.gameObject, tr.name);
                            EditorGUILayout.TextField(Style.scene, tr.gameObject.scene.name);
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                    Repaint();
                    EditorGUI.indentLevel--;
                }
            }
            GUILayout.Space(4);

            int sceneCount = SceneManager.sceneCount;
            List<string> options = new List<string>();
            for (int i = 0; i < sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                options.Add(string.Format("{0} ", scene.name));
            }

            int newPhysicsSceneMask = EditorGUILayout.MaskField(Style.showPhysicsScenes, PhysicsVisualizationSettings.GetShowPhysicsSceneMask(), options.ToArray());

            PhysicsVisualizationSettings.SetShowPhysicsSceneMask(newPhysicsSceneMask);

            // Layers
            int oldConcatenatedMask = InternalEditorUtility.LayerMaskToConcatenatedLayersMask(
                PhysicsVisualizationSettings.GetShowCollisionLayerMask());

            int newConcatenatedMask = EditorGUILayout.MaskField(
                Style.showLayers, oldConcatenatedMask, InternalEditorUtility.layers);

            PhysicsVisualizationSettings.SetShowCollisionLayerMask(
                (int)InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(newConcatenatedMask));

            // Static Colliders
            PhysicsVisualizationSettings.SetShowStaticColliders(EditorGUILayout.Toggle(
                Style.showStaticCollider, PhysicsVisualizationSettings.GetShowStaticColliders()));

            // Triggers
            PhysicsVisualizationSettings.SetShowTriggers(EditorGUILayout.Toggle(
                Style.showTriggers, PhysicsVisualizationSettings.GetShowTriggers()));

            // Rigidbodies
            PhysicsVisualizationSettings.SetShowRigidbodies(EditorGUILayout.Toggle(
                Style.showRigibodies, PhysicsVisualizationSettings.GetShowRigidbodies()));

            // Kinematic Bodies
            PhysicsVisualizationSettings.SetShowKinematicBodies(EditorGUILayout.Toggle(
                Style.showKinematicBodies, PhysicsVisualizationSettings.GetShowKinematicBodies()));

            // Sleeping Bodies
            PhysicsVisualizationSettings.SetShowSleepingBodies(EditorGUILayout.Toggle(
                Style.showSleepingBodies, PhysicsVisualizationSettings.GetShowSleepingBodies()));

            m_ShowColliderTypeFoldout.value = EditorGUILayout.Foldout(m_ShowColliderTypeFoldout.value, Style.colliderTypes);
            if (m_ShowColliderTypeFoldout.value)
            {
                EditorGUI.indentLevel++;
                float oldWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 200;

                // BoxCollider
                PhysicsVisualizationSettings.SetShowBoxColliders(EditorGUILayout.Toggle(
                    Style.showBoxCollider , PhysicsVisualizationSettings.GetShowBoxColliders()));

                // SphereCollider
                PhysicsVisualizationSettings.SetShowSphereColliders(EditorGUILayout.Toggle(
                    Style.showSphereCollider, PhysicsVisualizationSettings.GetShowSphereColliders()));

                // CapsuleCollider
                PhysicsVisualizationSettings.SetShowCapsuleColliders(EditorGUILayout.Toggle(
                    Style.showCapsuleCollider, PhysicsVisualizationSettings.GetShowCapsuleColliders()));

                // MeshCollider convex
                PhysicsVisualizationSettings.SetShowMeshColliders(PhysicsVisualizationSettings.MeshColliderType.Convex, EditorGUILayout.Toggle(
                    Style.showConvexMeshCollider, PhysicsVisualizationSettings.GetShowMeshColliders(PhysicsVisualizationSettings.MeshColliderType.Convex)));

                // MeshCollider non-convex
                PhysicsVisualizationSettings.SetShowMeshColliders(PhysicsVisualizationSettings.MeshColliderType.NonConvex, EditorGUILayout.Toggle(
                    Style.showConcaveMeshCollider, PhysicsVisualizationSettings.GetShowMeshColliders(PhysicsVisualizationSettings.MeshColliderType.NonConvex)));

                // TerrainCollider
                PhysicsVisualizationSettings.SetShowTerrainColliders(EditorGUILayout.Toggle(
                    Style.showTerrainCollider, PhysicsVisualizationSettings.GetShowTerrainColliders()));

                EditorGUIUtility.labelWidth = oldWidth;
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(4);

            // Selection buttons
            GUILayout.BeginHorizontal();

            bool selectNone = GUILayout.Button(Style.showNone);
            bool selectAll = GUILayout.Button(Style.showAll);
            if (selectNone || selectAll)
                PhysicsVisualizationSettings.SetShowForAllFilters(selectAll);

            GUILayout.EndHorizontal();

            m_ColorFoldout.value = EditorGUILayout.Foldout(m_ColorFoldout.value, Style.colors);
            if (m_ColorFoldout.value)
            {
                EditorGUI.indentLevel++;

                PhysicsVisualizationSettings.staticColor =
                    EditorGUILayout.ColorField(Style.staticColor, PhysicsVisualizationSettings.staticColor);

                PhysicsVisualizationSettings.triggerColor =
                    EditorGUILayout.ColorField(Style.triggerColor, PhysicsVisualizationSettings.triggerColor);

                PhysicsVisualizationSettings.rigidbodyColor =
                    EditorGUILayout.ColorField(Style.rigidbodyColor, PhysicsVisualizationSettings.rigidbodyColor);

                PhysicsVisualizationSettings.kinematicColor =
                    EditorGUILayout.ColorField(Style.kinematicColor, PhysicsVisualizationSettings.kinematicColor);

                PhysicsVisualizationSettings.sleepingBodyColor =
                    EditorGUILayout.ColorField(Style.sleepingBodyColor, PhysicsVisualizationSettings.sleepingBodyColor);

                PhysicsVisualizationSettings.colorVariance =
                    EditorGUILayout.Slider(Style.colorVariaition, PhysicsVisualizationSettings.colorVariance, 0f, 1f);

                EditorGUI.indentLevel--;
            }

            m_RenderingFoldout.value = EditorGUILayout.Foldout(m_RenderingFoldout.value, Style.rendering);
            if (m_RenderingFoldout.value)
            {
                EditorGUI.indentLevel++;

                PhysicsVisualizationSettings.baseAlpha = 1f - EditorGUILayout.Slider(Style.transparency
                    , 1f - PhysicsVisualizationSettings.baseAlpha, 0f, 1f);

                PhysicsVisualizationSettings.forceOverdraw = EditorGUILayout.Toggle(Style.forceOverdraw
                    , PhysicsVisualizationSettings.forceOverdraw);

                PhysicsVisualizationSettings.viewDistance = EditorGUILayout.FloatField(Style.viewDistance
                    , PhysicsVisualizationSettings.viewDistance);

                PhysicsVisualizationSettings.terrainTilesMax = EditorGUILayout.IntField(Style.terrainTilesMax
                    , PhysicsVisualizationSettings.terrainTilesMax);

                EditorGUI.indentLevel--;
            }

            if (Unsupported.IsDeveloperMode() || PhysicsVisualizationSettings.devOptions)
            {
                PhysicsVisualizationSettings.devOptions = EditorGUILayout.Toggle(Style.devOptions
                    , PhysicsVisualizationSettings.devOptions);
            }

            if (PhysicsVisualizationSettings.devOptions)
            {
                PhysicsVisualizationSettings.dotAlpha = EditorGUILayout.Slider(Style.dotAlpha
                    , PhysicsVisualizationSettings.dotAlpha, -1f, 1f);

                PhysicsVisualizationSettings.forceDot = EditorGUILayout.Toggle(Style.forceDot
                    , PhysicsVisualizationSettings.forceDot);

                Tools.hidden = EditorGUILayout.Toggle(Style.toolsHidden
                    , Tools.hidden);
            }

            GUILayout.EndScrollView();

            if (dirtyCount != PhysicsVisualizationSettings.dirtyCount)
                RepaintSceneAndGameViews();
        }

        void DisplayControls(UnityEngine.Object o, SceneView view)
        {
            var dirtyCount = PhysicsVisualizationSettings.dirtyCount;

            PhysicsVisualizationSettings.showCollisionGeometry = EditorGUILayout.Toggle(Style.showCollisionGeometry
                , PhysicsVisualizationSettings.showCollisionGeometry);

            PhysicsVisualizationSettings.enableMouseSelect = EditorGUILayout.Toggle(Style.enableMouseSelect
                , PhysicsVisualizationSettings.enableMouseSelect);

            if (PhysicsVisualizationSettings.devOptions)
            {
                PhysicsVisualizationSettings.useSceneCam = EditorGUILayout.Toggle(Style.useSceneCam
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
