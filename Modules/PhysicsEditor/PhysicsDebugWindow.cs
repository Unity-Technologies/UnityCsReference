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

        private enum Tabs
        {
            Info = 0,
            Filtering = 1,
            Rendering = 2,
            Contacts = 3,
            Queries = 4,
        }

        [SerializeField] Vector2 m_MainScrollPos = Vector2.zero;
        [SerializeField] Vector2 m_InfoTabScrollPos = Vector2.zero;

        private SavedBool m_ShowColliderTypeFoldout;
        private SavedInt m_CurrentTab;
        private SavedInt m_Collumns;

        bool m_MouseLeaveListenerAdded = false;
        bool m_SceneViewListenerAdded = false;

        static PhysicsDebugWindow s_Window;

        private int m_CollumnsPrev = -1;
        private int m_TotalItems = -1;
        private float m_LastValidWidth = 0f;
        private bool m_ShowAllContactsWhenEnteredPlayMode = false; // For displaying the help message
        private bool m_ShowContactsWhenEnteredPlayMode = false;
        private bool m_AnyItems = false; // Are there any tracked items in the Info tab?
        private bool m_FixedUpdateOccured = false;

        private Dictionary<Transform, VisualisationState> m_LockedObjects     = new Dictionary<Transform, VisualisationState>();
        [SerializeField] private List<RenderedTransform> m_TransformsToRender = new List<RenderedTransform>();

        // For dictionary saving
        [SerializeField] private List<Transform> m_DictionaryKeys             = new List<Transform>();
        [SerializeField] private List<VisualisationState> m_DictionaryValues  = new List<VisualisationState>();

        // To avoid reallocations when hashing the selection
        #region Pre-allocations
        [SerializeField]
        private HashSet<Transform> m_TemporarySelection                       = new HashSet<Transform>();
        private LinkedList<RenderedTransform> m_ObjectsToAdd                  = new LinkedList<RenderedTransform>();
        private LinkedList<Transform> m_ObjectsToRemove                       = new LinkedList<Transform>();
        private List<string> m_SceneList                                      = new List<string>();
        private List<int> m_NumberOfItemPerRow                                = new List<int>();
        #endregion

        private static class Style
        {
            #region Info
            public static readonly GUIContent numOfItems = EditorGUIUtility.TrTextContent("Number of items per row:");
            public static readonly GUIContent clearLocked = EditorGUIUtility.TrTextContent("Clear locked  objects");
            public static readonly GUIContent drawGizmosFor = EditorGUIUtility.TrTextContent("Draw Gizmos for:");
            public static readonly GUIContent gameObjectField = EditorGUIUtility.TrTextContent("GameObject:");
            public static readonly GUILayoutOption notExpandWidth = GUILayout.ExpandWidth(false);
            public static readonly GUILayoutOption maxWidth50 = GUILayout.MaxWidth(50f);
            public static readonly GUILayoutOption maxWidth75 = GUILayout.MaxWidth(75f);
            public static readonly GUILayoutOption maxWidth150 = GUILayout.MaxWidth(150f);
            public static readonly GUIContent lockToggle = EditorGUIUtility.TrTextContent("Lock");
            #endregion

            #region Filtering
            public static readonly GUIContent showLayers = EditorGUIUtility.TrTextContent("Show Layers", "Show selected layers");
            public static readonly GUIContent showPhysicsScenes = EditorGUIUtility.TrTextContent("Show Physics Scene", "Show selected physics scenes");
            public static readonly GUIContent showUnityScenes = EditorGUIUtility.TrTextContent("Show Unity Scene", "Show selected Unity scenes");
            public static readonly GUIContent showStaticCollider = EditorGUIUtility.TrTextContent("Show Static Colliders", "Show collision geometry from Colliders that do not have a Rigidbody");
            public static readonly GUIContent showTriggers = EditorGUIUtility.TrTextContent("Show Triggers", "Show collision geometry from Colliders that have 'isTrigger' enabled");
            public static readonly GUIContent showRigibodies = EditorGUIUtility.TrTextContent("Show Rigidbodies", "Show collision geometry from Rigidbodies");
            public static readonly GUIContent showKinematicBodies = EditorGUIUtility.TrTextContent("Show Kinematic Bodies", "Show collision geometry from Kinematic Rigidbodies");
            public static readonly GUIContent showArticulationBodies = EditorGUIUtility.TrTextContent("Show Articulation Bodies", "Show collision geometry from Articulation Bodies");
            public static readonly GUIContent showSleepingBodies = EditorGUIUtility.TrTextContent("Show Sleeping Bodies", "Show collision geometry from Sleeping Rigidbodies");
            public static readonly GUIContent colliderTypes = EditorGUIUtility.TrTextContent("Collider Types");
            public static readonly GUIContent showBoxCollider = EditorGUIUtility.TrTextContent("Show BoxColliders", "Show collision geometry that is BoxCollider");
            public static readonly GUIContent showSphereCollider = EditorGUIUtility.TrTextContent("Show SphereColliders", "Show collision geometry that is SphereCollider");
            public static readonly GUIContent showCapsuleCollider = EditorGUIUtility.TrTextContent("Show CapsuleColliders", "Show collision geometry that is CapsuleCollider");
            public static readonly GUIContent showConvexMeshCollider = EditorGUIUtility.TrTextContent("Show MeshColliders (convex)", "Show collision geometry that is Convex MeshCollider");
            public static readonly GUIContent showConcaveMeshCollider = EditorGUIUtility.TrTextContent("Show MeshColliders (concave)", "Show collision geometry that is Concave MeshCollider");
            public static readonly GUIContent showTerrainCollider = EditorGUIUtility.TrTextContent("Show TerrainColliders", "Show collision geometry that is TerrainCollider");
            #endregion

            #region Rendering
            public static readonly GUIContent staticColor = EditorGUIUtility.TrTextContent("Static Colliders");
            public static readonly GUIContent triggerColor = EditorGUIUtility.TrTextContent("Triggers");
            public static readonly GUIContent rigidbodyColor = EditorGUIUtility.TrTextContent("Rigidbodies");
            public static readonly GUIContent kinematicColor = EditorGUIUtility.TrTextContent("Kinematic Bodies");
            public static readonly GUIContent articulationBodyColor = EditorGUIUtility.TrTextContent("Articulation Bodies");
            public static readonly GUIContent sleepingBodyColor = EditorGUIUtility.TrTextContent("Sleeping Bodies");
            public static readonly GUIContent colorVariaition = EditorGUIUtility.TrTextContent("Variation", "Random color variation that is added on top of the base color");
            public static readonly GUIContent centerOfMassUseScreenSize = EditorGUIUtility.TrTextContent("Constant screen size", "Use constant screen size for the center of mass gizmos");
            public static readonly GUIContent inertiaTensorScale = EditorGUIUtility.TrTextContent("Inertia Tensor scale", "Scale by which the original inertia tensor is multiplied before drawing");
            public static readonly GUIContent forceOverdraw = EditorGUIUtility.TrTextContent("Force Overdraw", "Draws Collider geometry on top of render geometry");
            public static readonly GUIContent transparency = EditorGUIUtility.TrTextContent("Transparency");
            public static readonly GUIContent viewDistance = EditorGUIUtility.TrTextContent("View Distance", "Lower bound on distance from camera to physics geometry.");
            public static readonly GUIContent terrainTilesMax = EditorGUIUtility.TrTextContent("Terrain Tiles Max", "Number of mesh tiles to drawn.");
            public static readonly GUIContent gizmosSection = EditorGUIUtility.TrTextContent("Gizmos settings:");
            #endregion

            #region Contacts
            public static readonly GUIContent contactColors = EditorGUIUtility.TrTextContent("Contact colors");
            public static readonly GUIContent contactColor = EditorGUIUtility.TrTextContent("Contact color");
            public static readonly GUIContent contactSeparationColor = EditorGUIUtility.TrTextContent("Contact separation color");
            public static readonly GUIContent contactImpulseColor = EditorGUIUtility.TrTextContent("Contact impulse color");
            public static readonly GUIContent showContacts = EditorGUIUtility.TrTextContent("Show Contacts", "Should contacts be shown? Enabling this at runtime won't have an effect");
            public static readonly GUIContent showAllContacts = EditorGUIUtility.TrTextContent("Show All Contacts", "Should all physics objects report contacts or only the ones that were requested by MonoBehaviour scripts");
            public static readonly GUIContent showImpulse = EditorGUIUtility.TrTextContent("Show Impulse", "Show per contact point impulse");
            public static readonly GUIContent showSeparation = EditorGUIUtility.TrTextContent("Show Separation", "Show contact separation");
            public static readonly GUIContent useContactFiltering = EditorGUIUtility.TrTextContent("Use Filtering settings", "Should Filtering settings be taken into account when displaying contacts?");
            public static readonly GUIContent useVariedColors = EditorGUIUtility.TrTextContent("Use varied colors", "Uses collider instance ID to hash it into a color");
            #endregion

            #region Queries
            public static readonly GUIContent showQueries = EditorGUIUtility.TrTextContent("Show queries", "Should queries be visualized?");
            public static readonly GUIContent queryDuration = EditorGUIUtility.TrTextContent("Query duration", "How longs should the query be visible after it is performed?");
            public static readonly GUIContent queryColor = EditorGUIUtility.TrTextContent("Query color", "Color used for query visualization");
            public static readonly GUIContent sphereQueries = EditorGUIUtility.TrTextContent("Sphere", "Should sphere shaped queries be visualized?");
            public static readonly GUIContent boxQueries = EditorGUIUtility.TrTextContent("Box", "Should box shaped queries be visualized?");
            public static readonly GUIContent capsuleQueries = EditorGUIUtility.TrTextContent("Capsule", "Should capsule shaped queries be visualized?");
            public static readonly GUIContent rayQueries = EditorGUIUtility.TrTextContent("Ray", "Should ray shaped queries be visualized?");
            public static readonly GUIContent overlapQueries = EditorGUIUtility.TrTextContent("Overlap", "Should overlap type queries be visualized?");
            public static readonly GUIContent checkQueries = EditorGUIUtility.TrTextContent("Check", "Should check type queries be visualized?");
            public static readonly GUIContent castQueries = EditorGUIUtility.TrTextContent("Cast", "Should cast type queries be visualized?");
            public static readonly GUIContent showTypes = EditorGUIUtility.TrTextContent("Show types:");
            public static readonly GUIContent showShapes = EditorGUIUtility.TrTextContent("Show shapes:");
            public static readonly GUIContent maxNumberOfQueries = EditorGUIUtility.TrTextContent("Max Queries", "Maximum number of queries that will be visualized");
            #endregion

            #region Overlay
            public static readonly GUIContent showCollisionGeometry = EditorGUIUtility.TrTextContent("Collision Geometry");
            public static readonly GUIContent enableMouseSelect = EditorGUIUtility.TrTextContent("Mouse Select");
            public static readonly GUIContent useSceneCam = EditorGUIUtility.TrTextContent("Use Scene Cam");
            #endregion

            #region Buttons
            public static readonly GUIContent showAll               = EditorGUIUtility.TrTextContent("Show All");
            public static readonly GUIContent showNone              = EditorGUIUtility.TrTextContent("Show None");
            public static readonly GUIContent resetButton           = EditorGUIUtility.TrTextContent("Reset", "Reset visualization settings and locked objects");
            #endregion

            #region Info tables
            public static readonly GUIContent infoSpeed             = EditorGUIUtility.TrTextContent("Speed");
            public static readonly GUIContent infoVel               = EditorGUIUtility.TrTextContent("Velocity");
            public static readonly GUIContent infoAngVel            = EditorGUIUtility.TrTextContent("Angular Velocity");
            public static readonly GUIContent infoInertiaTensor     = EditorGUIUtility.TrTextContent("Inertia Tensor");
            public static readonly GUIContent infoInertiaTensorRotation = EditorGUIUtility.TrTextContent("Inertia Tensor Rotation");
            public static readonly GUIContent infoLocalCenterOfMass = EditorGUIUtility.TrTextContent("Local Center of Mass");
            public static readonly GUIContent infoWorldCenterOfMass = EditorGUIUtility.TrTextContent("World Center of Mass");
            public static readonly GUIContent infoSleepState        = EditorGUIUtility.TrTextContent("Sleep State");
            public static readonly GUIContent infoSleepThreshold    = EditorGUIUtility.TrTextContent("Sleep Threshold");
            public static readonly GUIContent infoMaxLinVel         = EditorGUIUtility.TrTextContent("Max Linear Velocity");
            public static readonly GUIContent infoMaxAngVel         = EditorGUIUtility.TrTextContent("Max Angular Velocity");
            public static readonly GUIContent infoSolverIterations  = EditorGUIUtility.TrTextContent("Solver Iterations");
            public static readonly GUIContent infoSolverVelIterations = EditorGUIUtility.TrTextContent("Solver Velocity Iterations");
            public static readonly GUIContent sleep = EditorGUIUtility.TrTextContent("Asleep");
            public static readonly GUIContent awake = EditorGUIUtility.TrTextContent("Awake");

            public static readonly GUIContent infoBodyIndex         = EditorGUIUtility.TrTextContent("Body Index");
            public static readonly GUIContent infoJointInfo         = EditorGUIUtility.TrTextContent("Joint Info");
            public static readonly GUIContent infoJointPosition     = EditorGUIUtility.TrTextContent("Position");
            public static readonly GUIContent infoJointVelocity     = EditorGUIUtility.TrTextContent("Velocity");
            public static readonly GUIContent infoJointForce        = EditorGUIUtility.TrTextContent("Force");
            public static readonly GUIContent infoJointAcceleration = EditorGUIUtility.TrTextContent("Acceleration");
            #endregion

            public static readonly GUIStyle tabBarStyle             = GUI.skin.button;
            public static readonly string[] tabs                    = new string[] { "Info", "Filtering", "Rendering", "Contacts", "Queries" };

            static Style()
            {
                tabBarStyle.margin = new RectOffset(0, 0, 1, 0);
            }
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
            EditorSceneManager.sceneOpened += OnSceneOpen;
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
            Physics.ContactEvent += ReadContacts_Internal;
            PhysicsDebugDraw.OnRetrievePooledQueries += OnQueriesRetrieved;
            PhysicsDebugDraw.OnDestroyPhysicsScene += OnPhysicsSceneDestoryed;
            PhysicsDebugDraw.OnBeforeSimulate += OnBeforeSimulate;
            SetPickingEnabled(PhysicsVisualizationSettings.showCollisionGeometry
                && PhysicsVisualizationSettings.enableMouseSelect);

            LoadDictionary();
            ClearInvalidInfoObjects();
            UpdateSelection();

            PhysicsDebugDraw.ClearAllPools();

            wantsLessLayoutEvents = true;
        }

        public void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            Selection.selectionChanged -= UpdateSelection;
            EditorSceneManager.sceneClosed -= OnSceneClose;
            EditorSceneManager.sceneOpened -= OnSceneOpen;
            EditorApplication.playModeStateChanged -= PlayModeStateChanged;
            Physics.ContactEvent -= ReadContacts_Internal;
            PhysicsDebugDraw.OnRetrievePooledQueries -= OnQueriesRetrieved;
            PhysicsDebugDraw.OnDestroyPhysicsScene -= OnPhysicsSceneDestoryed;
            PhysicsDebugDraw.OnBeforeSimulate -= OnBeforeSimulate;
            SetPickingEnabled(false);

            SaveDictionary();
            ClearInvalidInfoObjects();

            ClearAllPoolsAndStoredQueries();
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
            PhysicsVisualizationSettings.isDebuggerActive = true;
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
            PhysicsVisualizationSettings.isDebuggerActive = false;
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

        void OnInspectorUpdate()
        {
            if(s_Window != null && m_AnyItems && m_CurrentTab == (int)Tabs.Info)
                Repaint();
        }

        private void OnBeforeSimulate(PhysicsScene sceneHandle)
        {
            m_FixedUpdateOccured = true;

            if (m_ContactsToDraw.ContainsKey(sceneHandle))
                m_ContactsToDraw[sceneHandle].CompleteJob();
        }

        private void Update()
        {
            bool canFetch = s_Window != null && PhysicsVisualizationSettings.GetQueryFilterState(PhysicsVisualizationSettings.QueryFilter.ShowQueries);

            if (canFetch && m_FixedUpdateOccured)
            {
                m_ShapesToDraw.Clear();
                PhysicsDebugDraw.GetPooledQueries();
                m_FixedUpdateOccured = false;
            }
        }

        private bool MouseInAnySceneViews(Vector2 mousePos)
        {
            var sceneViews = SceneView.sceneViews;

            foreach(SceneView view in sceneViews)
            {
                Rect sceneViewRect = new Rect(0, EditorGUI.kWindowToolbarHeight, view.position.width, view.position.height - EditorGUI.kWindowToolbarHeight);
                if (sceneViewRect.Contains(mousePos))
                    return true;
            }

            return false;
        }

        void OnSceneGUI(SceneView view)
        {
            var dirtyCount = PhysicsVisualizationSettings.dirtyCount;
            Vector2 mousePos = Event.current.mousePosition;
            bool mouseInAnyOfTheSceneViews = MouseInAnySceneViews(mousePos);

            bool allowInteraction = PhysicsVisualizationSettings.showCollisionGeometry && PhysicsVisualizationSettings.enableMouseSelect && mouseInAnyOfTheSceneViews;

            // Disregard these events as the mouse position is wrong during them. This prevent some of the flickering
            if (Event.current.type != EventType.Layout && Event.current.type != EventType.Repaint)
            {
                if (allowInteraction)
                {
                    AddMouseLeaveListener();

                    // mouse-over highlight
                    if (Event.current.type == EventType.MouseMove)
                        PhysicsVisualizationSettings.UpdateMouseHighlight_Internal(HandleUtility.GUIPointToScreenPixelCoordinate(mousePos), view.camera);

                    if (Event.current.type == EventType.MouseDrag)
                        PhysicsVisualizationSettings.ClearMouseHighlight();
                }
                else
                {
                    PhysicsVisualizationSettings.ClearMouseHighlight();
                }
            }

            if (Event.current.type == EventType.Repaint)
            {
                m_Camera = view.camera;
                DrawCastsAndOverlaps();
                DrawContacts();
                DrawComAndInertia();
            }

            if (dirtyCount != PhysicsVisualizationSettings.dirtyCount)
                RepaintSceneAndGameViews();

        }

        void OnGUI()
        {
            var dirtyCount = PhysicsVisualizationSettings.dirtyCount;

            if (!EditorApplication.isPlaying && !EditorApplication.isPaused)
            {
                m_ShowAllContactsWhenEnteredPlayMode = PhysicsVisualizationSettings.showAllContacts;
                m_ShowContactsWhenEnteredPlayMode = PhysicsVisualizationSettings.showContacts;
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            m_CurrentTab.value = GUILayout.Toolbar(m_CurrentTab.value, Style.tabs, Style.tabBarStyle);

            if (GUILayout.Button(Style.resetButton, EditorStyles.toolbarButton, Style.maxWidth75))
            {
                PhysicsVisualizationSettings.Reset();
                ClearAllLockedObjects();
                m_ShapesToDraw.Clear();
            }

            EditorGUILayout.EndHorizontal();

            m_MainScrollPos = GUILayout.BeginScrollView(m_MainScrollPos);

            switch ((Tabs)m_CurrentTab.value)
            {
                case Tabs.Info:
                    DrawInfoTab();
                    break;
                case Tabs.Filtering:
                    DrawFilteringTab();
                    break;
                case Tabs.Rendering:
                    DrawRenderingTab();
                    break;
                case Tabs.Contacts:
                    DrawContactsTab();
                    break;
                case Tabs.Queries:
                    DrawQueriesTab();
                    break;
            }

            GUILayout.EndScrollView();

            if (dirtyCount != PhysicsVisualizationSettings.dirtyCount)
                RepaintSceneAndGameViews();
        }

        private void DrawFilteringTab()
        {
            int sceneCount = SceneManager.sceneCount;
            m_SceneList.Clear();
            for (int i = 0; i < sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                m_SceneList.Add(string.Format("{0} ", scene.name));
            }

            var sceneArray = m_SceneList.ToArray();

            int newPhysicsSceneMask = EditorGUILayout.MaskField(Style.showPhysicsScenes, PhysicsVisualizationSettings.GetShowPhysicsSceneMask(), sceneArray);
            int newUnitySceneMask = EditorGUILayout.MaskField(Style.showUnityScenes, PhysicsVisualizationSettings.GetShowUnitySceneMask(), sceneArray);

            PhysicsVisualizationSettings.SetShowPhysicsSceneMask(newPhysicsSceneMask);
            PhysicsVisualizationSettings.SetShowUnitySceneMask(newUnitySceneMask);

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
                EditorGUIUtility.labelWidth = 200f;

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

            GUILayout.Space(4f);

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

            EditorGUILayout.LabelField(Style.gizmosSection);
            EditorGUI.indentLevel++;

            PhysicsVisualizationSettings.centerOfMassUseScreenSize = EditorGUILayout.Toggle(Style.centerOfMassUseScreenSize
                , PhysicsVisualizationSettings.centerOfMassUseScreenSize);

            PhysicsVisualizationSettings.inertiaTensorScale = EditorGUILayout.Slider(Style.inertiaTensorScale
                , PhysicsVisualizationSettings.inertiaTensorScale, 0f, 1f);

            EditorGUI.indentLevel--;
        }

        private void DrawInfoTab()
        {
            m_AnyItems = DrawInfoTabHeader();

            var totalItems = m_TransformsToRender.Count;
            var index = 0;
            var rows = Mathf.CeilToInt((float)totalItems / (float)m_Collumns);

            if (m_TotalItems != totalItems || m_CollumnsPrev != m_Collumns.value)
            {
                RecalculateItemsPerRow(totalItems, rows);
                m_TotalItems = totalItems;
                m_CollumnsPrev = m_Collumns;
            }

            EditorGUILayout.Space(10f);

            m_InfoTabScrollPos = EditorGUILayout.BeginScrollView(m_InfoTabScrollPos);

            for (int row = 0; row < rows; row++)
            {
                bool isRowFull = m_NumberOfItemPerRow[row] == m_Collumns;

                if (!isRowFull && row > 0)
                {
                    float maxWidth = m_LastValidWidth == 0f ? 0f : m_NumberOfItemPerRow[row] * m_LastValidWidth + (m_NumberOfItemPerRow[row] - 1) * 10f;
                    EditorGUILayout.BeginHorizontal(GUILayout.Width(maxWidth));
                }
                else
                    EditorGUILayout.BeginHorizontal();

                for (int column = 0; column < m_NumberOfItemPerRow[row]; column++)
                {
                    bool isLastItem = column == m_NumberOfItemPerRow[row] - 1;

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

                if (dirtyCount != PhysicsVisualizationSettings.dirtyCount)
                    RepaintSceneAndGameViews();
            }
        }
    }
}
