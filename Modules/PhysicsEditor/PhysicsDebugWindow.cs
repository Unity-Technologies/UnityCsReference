// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine;

using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;

namespace UnityEditor
{
    public partial class PhysicsDebugWindow : EditorWindow
    {
        private enum VisualisationState
        {
            None = 0,
            CenterOfMass = 1,
            InertiaTensor = 2
        }

        [SerializeField] Vector2 m_MainScrollPos = Vector2.zero;
        [SerializeField] Vector2 m_InfoTabScrollPos = Vector2.zero;

        private SavedBool m_ShowColliderTypeFoldout;
        private SavedInt m_CurrentTab;
        private SavedInt m_Collumns;

        bool m_MouseLeaveListenerAdded = false;
        bool m_SceneViewListenerAdded = false;

        static PhysicsDebugWindow s_Window;

        private float m_LastValidWidth = 0f;

        private Dictionary<Transform, VisualisationState> m_LockedObjects     = new Dictionary<Transform, VisualisationState>();
        [SerializeField] private List<RenderedTransform> m_TransformsToRender = new List<RenderedTransform>();
        private List<RenderedTransform> m_ObjectsToAdd                        = new List<RenderedTransform>();
        private List<Transform> m_ObjectsToRemove                             = new List<Transform>();

        // For dictionary saving
        [SerializeField] private List<Transform> m_DictionaryKeys             = new List<Transform>();
        [SerializeField] private List<VisualisationState> m_DictionaryValues  = new List<VisualisationState>();

        // To avoid reallocations when hashing the selection
        [SerializeField] private HashSet<Transform> m_TemporarySelection = new HashSet<Transform>();

        private static class Style
        {
            public static readonly GUIContent staticColor           = EditorGUIUtility.TrTextContent("Static Colliders");
            public static readonly GUIContent triggerColor          = EditorGUIUtility.TrTextContent("Triggers");
            public static readonly GUIContent rigidbodyColor        = EditorGUIUtility.TrTextContent("Rigidbodies");
            public static readonly GUIContent kinematicColor        = EditorGUIUtility.TrTextContent("Kinematic Bodies");
            public static readonly GUIContent articulationBodyColor = EditorGUIUtility.TrTextContent("Articulation Bodies");
            public static readonly GUIContent sleepingBodyColor     = EditorGUIUtility.TrTextContent("Sleeping Bodies");
            public static readonly GUIContent colorVariaition       = EditorGUIUtility.TrTextContent("Variation", "Random color variation that is added on top of the base color");
            public static readonly GUIContent centerOfMassUseScreenSize = EditorGUIUtility.TrTextContent("Constant screen size", "Use constant screen size for the center of mass gizmos");
            public static readonly GUIContent inertiaTensorScale    = EditorGUIUtility.TrTextContent("Inertia Tensor scale", "Scale by which the original inertia tensor is multiplied before drawing");
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
            public static readonly GUIContent showArticulationBodies = EditorGUIUtility.TrTextContent("Show Articulation Bodies", "Show collision geometry from Articulation Bodies");
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
            m_ShowColliderTypeFoldout   = new SavedBool("PhysicsDebugWindow.ShowColliderType", false);
            m_CurrentTab                = new SavedInt("PhysicsDebugWindow.CurrentTab", 0);
            m_Collumns                  = new SavedInt("PhysicsDebugWindow.Collumns", 1);

            SceneView.duringSceneGui += OnSceneGUI;
            Selection.selectionChanged += UpdateSelection;
            EditorSceneManager.sceneClosed += OnSceneClose;
            SetPickingEnabled(PhysicsVisualizationSettings.showCollisionGeometry
                && PhysicsVisualizationSettings.enableMouseSelect);

            LoadDictionary();
            ClearInvalidLockedObjects();
            UpdateSelection();
        }

        public void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            Selection.selectionChanged -= UpdateSelection;
            EditorSceneManager.sceneClosed -= OnSceneClose;
            SetPickingEnabled(false);

            SaveDictionary();
            ClearInvalidLockedObjects();
        }

        static void SetPickingEnabled(bool enabled)
        {
            HandleUtility.pickClosestGameObjectDelegate = enabled
                ? PhysicsVisualizationSettings.PickClosestGameObject
                : (HandleUtility.PickClosestGameObjectFunc)null;
        }

        void OnBecameVisible()
        {
            if (!m_SceneViewListenerAdded)
            {
                PhysicsVisualizationSettings.InitDebugDraw();
                m_SceneViewListenerAdded = true;
            }

            RepaintSceneAndGameViews();
            s_Window = this;
        }

        void OnBecameInvisible()
        {
            if (m_SceneViewListenerAdded)
            {
                PhysicsVisualizationSettings.DeinitDebugDraw();
                m_SceneViewListenerAdded = false;
            }

            RepaintSceneAndGameViews();
            s_Window = null;
        }

        static void RepaintSceneAndGameViews()
        {
            SceneView.RepaintAll();
            GameView.RepaintAll();
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

        void OnSceneGUI(SceneView view)
        {
            var dirtyCount = PhysicsVisualizationSettings.dirtyCount;
            Vector2 mousePos = Event.current.mousePosition;
            Rect sceneViewOnly = new Rect(0, EditorGUI.kWindowToolbarHeight, view.position.width, view.position.height - EditorGUI.kWindowToolbarHeight);
            bool mouseInSceneView = sceneViewOnly.Contains(Event.current.mousePosition);

            bool allowInteraction = PhysicsVisualizationSettings.showCollisionGeometry && PhysicsVisualizationSettings.enableMouseSelect && mouseInSceneView;

            if (allowInteraction)
            {
                AddMouseLeaveListener();

                // mouse-over highlight
                if (Event.current.type == EventType.MouseMove)
                    PhysicsVisualizationSettings.UpdateMouseHighlight(HandleUtility.GUIPointToScreenPixelCoordinate(mousePos));

                if (Event.current.type == EventType.MouseDrag)
                    PhysicsVisualizationSettings.ClearMouseHighlight();
            }
            else
            {
                PhysicsVisualizationSettings.ClearMouseHighlight();
            }

            if (dirtyCount != PhysicsVisualizationSettings.dirtyCount)
                RepaintSceneAndGameViews();
        }

        void OnGUI()
        {
            var dirtyCount = PhysicsVisualizationSettings.dirtyCount;

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            var currentStyle = GUI.skin.button;
            currentStyle.margin = new RectOffset(0, 0, 1, 0);

            m_CurrentTab.value = GUILayout.Toolbar(m_CurrentTab.value
                , (Unsupported.IsDeveloperMode() || PhysicsVisualizationSettings.devOptions)
                ? new string[] { "Filtering", "Rendering", "Info", "Other" }
                : new string[] { "Filtering", "Rendering", "Info" }
                , currentStyle);

            if (GUILayout.Button("Reset", EditorStyles.toolbarButton, GUILayout.MaxWidth(75f)))
            {
                PhysicsVisualizationSettings.Reset();
                ClearAllLockedObjects();
            }

            EditorGUILayout.EndHorizontal();

            m_MainScrollPos = GUILayout.BeginScrollView(m_MainScrollPos);

            switch (m_CurrentTab)
            {
                case 0:
                    DrawFilteringTab();
                    break;
                case 1:
                    DrawRenderingTab();
                    break;
                case 2:
                    DrawInfoTab();
                    break;
                case 3:
                    DrawMiscTab();
                    break;
            }

            GUILayout.EndScrollView();

            if (dirtyCount != PhysicsVisualizationSettings.dirtyCount)
                RepaintSceneAndGameViews();
        }

        private void DrawFilteringTab()
        {
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

            // Articulation Bodies
            PhysicsVisualizationSettings.SetShowArticulationBodies(EditorGUILayout.Toggle(
                Style.showArticulationBodies, PhysicsVisualizationSettings.GetShowArticulationBodies()));

            // Sleeping Bodies
            PhysicsVisualizationSettings.SetShowSleepingBodies(EditorGUILayout.Toggle(
                Style.showSleepingBodies, PhysicsVisualizationSettings.GetShowSleepingBodies()));

            m_ShowColliderTypeFoldout.value = EditorGUILayout.Foldout(m_ShowColliderTypeFoldout.value, Style.colliderTypes, true);
            if (m_ShowColliderTypeFoldout.value)
            {
                EditorGUI.indentLevel++;
                float oldWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 200;

                // BoxCollider
                PhysicsVisualizationSettings.SetShowBoxColliders(EditorGUILayout.Toggle(
                    Style.showBoxCollider, PhysicsVisualizationSettings.GetShowBoxColliders()));

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
        }

        private void DrawRenderingTab()
        {
            PhysicsVisualizationSettings.staticColor =
                EditorGUILayout.ColorField(Style.staticColor, PhysicsVisualizationSettings.staticColor);

            PhysicsVisualizationSettings.triggerColor =
                EditorGUILayout.ColorField(Style.triggerColor, PhysicsVisualizationSettings.triggerColor);

            PhysicsVisualizationSettings.rigidbodyColor =
                EditorGUILayout.ColorField(Style.rigidbodyColor, PhysicsVisualizationSettings.rigidbodyColor);

            PhysicsVisualizationSettings.kinematicColor =
                EditorGUILayout.ColorField(Style.kinematicColor, PhysicsVisualizationSettings.kinematicColor);

            PhysicsVisualizationSettings.articulationBodyColor =
                EditorGUILayout.ColorField(Style.articulationBodyColor, PhysicsVisualizationSettings.articulationBodyColor);

            PhysicsVisualizationSettings.sleepingBodyColor =
                EditorGUILayout.ColorField(Style.sleepingBodyColor, PhysicsVisualizationSettings.sleepingBodyColor);

            PhysicsVisualizationSettings.colorVariance =
                EditorGUILayout.Slider(Style.colorVariaition, PhysicsVisualizationSettings.colorVariance, 0f, 1f);

            PhysicsVisualizationSettings.baseAlpha = 1f - EditorGUILayout.Slider(Style.transparency
                , 1f - PhysicsVisualizationSettings.baseAlpha, 0f, 1f);

            PhysicsVisualizationSettings.forceOverdraw = EditorGUILayout.Toggle(Style.forceOverdraw
                , PhysicsVisualizationSettings.forceOverdraw);

            PhysicsVisualizationSettings.viewDistance = EditorGUILayout.FloatField(Style.viewDistance
                , PhysicsVisualizationSettings.viewDistance);

            PhysicsVisualizationSettings.terrainTilesMax = EditorGUILayout.IntField(Style.terrainTilesMax
                , PhysicsVisualizationSettings.terrainTilesMax);

            EditorGUILayout.LabelField("Gizmos settings:");
            EditorGUI.indentLevel++;

            PhysicsVisualizationSettings.centerOfMassUseScreenSize = EditorGUILayout.Toggle(Style.centerOfMassUseScreenSize
                , PhysicsVisualizationSettings.centerOfMassUseScreenSize);

            PhysicsVisualizationSettings.inertiaTensorScale = EditorGUILayout.Slider(Style.inertiaTensorScale
                , PhysicsVisualizationSettings.inertiaTensorScale, 0f, 1f);

            EditorGUI.indentLevel--;
        }

        private void DrawInfoTab()
        {
            var anyItems = DrawInfoTabHeader();

            var totalItems = m_TransformsToRender.Count;
            var index = 0;
            var rows = Mathf.CeilToInt((float)totalItems / (float)m_Collumns);

            int[] numberOfItemPerRow = new int[rows];
            int itemsLeft = totalItems;

            for (int i = 0; i < rows; i++)
            {
                if (itemsLeft > m_Collumns)
                {
                    numberOfItemPerRow[i] = m_Collumns;
                    itemsLeft -= m_Collumns;
                }
                else
                {
                    numberOfItemPerRow[i] = itemsLeft;
                    itemsLeft = 0;
                }
            }

            EditorGUILayout.Space(10f);

            m_InfoTabScrollPos = EditorGUILayout.BeginScrollView(m_InfoTabScrollPos);

            for (int row = 0; row < rows; row++)
            {
                bool isRowFull = numberOfItemPerRow[row] == m_Collumns;

                if (!isRowFull && row > 0)
                {
                    float maxWidth = m_LastValidWidth == 0f ? 0f : numberOfItemPerRow[row] * m_LastValidWidth + (numberOfItemPerRow[row] - 1) * 10f;
                    EditorGUILayout.BeginHorizontal(GUILayout.Width(maxWidth));
                }
                else
                    EditorGUILayout.BeginHorizontal();

                for (int column = 0; column < numberOfItemPerRow[row]; column++)
                {
                    bool isLastItem = column == numberOfItemPerRow[row] - 1;

                    if (row == 0 && column == 0)
                    {
                        var width = EditorGUILayout.BeginVertical().width;
                        if (width != 0f)
                            m_LastValidWidth = width;
                    }
                    else if (!isRowFull && row > 0)
                        EditorGUILayout.BeginVertical(GUILayout.MaxWidth(m_LastValidWidth));
                    else
                        EditorGUILayout.BeginVertical();

                    DrawSingleInfoItem(GetNextTransform(index));
                    index++;
                    EditorGUILayout.Space(10f);
                    EditorGUILayout.EndVertical();

                    if (!isLastItem)
                        EditorGUILayout.Space(10f);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            AddLockedObjects();
            RemoveLockedObjects();

            // FIXME This can still be better
            if (anyItems)
                Repaint();
        }

        private void DrawMiscTab()
        {
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
        }

        [Overlay(typeof(SceneView), k_OverlayId, k_DisplayName)]
        class SceneViewPhysicsDebuggerOverlay : TransientSceneViewOverlay
        {
            const string k_OverlayId = "Scene View/Physics Debugger";
            const string k_DisplayName = "Physics Debug";
            public override bool visible => s_Window != null;

            public override void OnGUI()
            {
                var dirtyCount = PhysicsVisualizationSettings.dirtyCount;

                EditorGUI.BeginChangeCheck();
                PhysicsVisualizationSettings.showCollisionGeometry = EditorGUILayout.Toggle(Style.showCollisionGeometry
                    , PhysicsVisualizationSettings.showCollisionGeometry);
                using (new EditorGUI.DisabledScope(!PhysicsVisualizationSettings.showCollisionGeometry))
                {
                    PhysicsVisualizationSettings.enableMouseSelect = EditorGUILayout.Toggle(Style.enableMouseSelect, PhysicsVisualizationSettings.enableMouseSelect);
                }
                if (EditorGUI.EndChangeCheck())
                    SetPickingEnabled(PhysicsVisualizationSettings.showCollisionGeometry && PhysicsVisualizationSettings.enableMouseSelect);

                if (PhysicsVisualizationSettings.devOptions)
                {
                    PhysicsVisualizationSettings.useSceneCam = EditorGUILayout.Toggle(Style.useSceneCam
                        , PhysicsVisualizationSettings.useSceneCam);
                }

                if (dirtyCount != PhysicsVisualizationSettings.dirtyCount)
                    RepaintSceneAndGameViews();
            }
        }
    }
}
