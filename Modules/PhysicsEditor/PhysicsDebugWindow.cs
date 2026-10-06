// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine;

using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using Unity.Scripting.LifecycleManagement;

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
            Internal = 5,
        }

        [SerializeField] Vector2 m_MainScrollPos = Vector2.zero;
        [SerializeField] Vector2 m_InfoTabScrollPos = Vector2.zero;

        private SavedBool m_ShowColliderTypeFoldout;
        private SavedInt m_CurrentTab;
        private SavedInt m_Collumns;

        bool m_MouseLeaveListenerAdded = false;
        bool m_SceneViewListenerAdded = false;

        [AutoStaticsCleanupOnCodeReload]
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
        private HashSet<Transform> m_TemporarySelection                       = new HashSet<Transform>();
        private LinkedList<RenderedTransform> m_ObjectsToAdd                  = new LinkedList<RenderedTransform>();
        private LinkedList<Transform> m_ObjectsToRemove                       = new LinkedList<Transform>();
        private List<string> m_SceneList                                      = new List<string>();
        private List<int> m_NumberOfItemPerRow                                = new List<int>();
        #endregion

        private static class Style
        {
            #region Info
            public static readonly GUIContent numOfItems = L10n.TextContent("Number of items per row:", null, null, null);
            public static readonly GUIContent clearLocked = L10n.TextContent("Clear locked objects", null, null, null);
            public static readonly GUIContent drawGizmosFor = L10n.TextContent("Draw Gizmos for:", null, null, null);
            public static readonly GUIContent gameObjectField = L10n.TextContent("GameObject:", null, null, null);
            public static readonly GUILayoutOption notExpandWidth = GUILayout.ExpandWidth(false);
            public static readonly GUILayoutOption maxWidth50 = GUILayout.MaxWidth(50f);
            public static readonly GUILayoutOption maxWidth75 = GUILayout.MaxWidth(75f);
            public static readonly GUILayoutOption maxWidth150 = GUILayout.MaxWidth(150f);
            public static readonly GUIContent lockToggle = L10n.TextContent("Lock", null, null, null);
            #endregion

            #region Filtering
            public static readonly GUIContent showLayers = L10n.TextContent("Show Layers", "Show selected layers", null, null);
            public static readonly GUIContent showPhysicsScenes = L10n.TextContent("Show Physics Scene", "Show selected physics scenes", null, null);
            public static readonly GUIContent showUnityScenes = L10n.TextContent("Show Unity Scene", "Show selected Unity scenes", null, null);
            public static readonly GUIContent showStaticCollider = L10n.TextContent("Show Static Colliders", "Show collision geometry from Colliders that do not have a Rigidbody", null, null);
            public static readonly GUIContent showTriggers = L10n.TextContent("Show Triggers", "Show collision geometry from Colliders that have 'isTrigger' enabled", null, null);
            public static readonly GUIContent showRigibodies = L10n.TextContent("Show Rigidbodies", "Show collision geometry from Rigidbodies", null, null);
            public static readonly GUIContent showKinematicBodies = L10n.TextContent("Show Kinematic Bodies", "Show collision geometry from Kinematic Rigidbodies", null, null);
            public static readonly GUIContent showArticulationBodies = L10n.TextContent("Show Articulation Bodies", "Show collision geometry from Articulation Bodies", null, null);
            public static readonly GUIContent showSleepingBodies = L10n.TextContent("Show Sleeping Bodies", "Show collision geometry from Sleeping Rigidbodies", null, null);
            public static readonly GUIContent colliderTypes = L10n.TextContent("Collider Types", null, null, null);
            public static readonly GUIContent showBoxCollider = L10n.TextContent("Show BoxColliders", "Show collision geometry that is BoxCollider", null, null);
            public static readonly GUIContent showSphereCollider = L10n.TextContent("Show SphereColliders", "Show collision geometry that is SphereCollider", null, null);
            public static readonly GUIContent showCapsuleCollider = L10n.TextContent("Show CapsuleColliders", "Show collision geometry that is CapsuleCollider", null, null);
            public static readonly GUIContent showConvexMeshCollider = L10n.TextContent("Show MeshColliders (convex)", "Show collision geometry that is Convex MeshCollider", null, null);
            public static readonly GUIContent showConcaveMeshCollider = L10n.TextContent("Show MeshColliders (concave)", "Show collision geometry that is Concave MeshCollider", null, null);
            public static readonly GUIContent showTerrainCollider = L10n.TextContent("Show TerrainColliders", "Show collision geometry that is TerrainCollider", null, null);
            #endregion

            #region Rendering
            public static readonly GUIContent staticColor = L10n.TextContent("Static Colliders", null, null, null);
            public static readonly GUIContent triggerColor = L10n.TextContent("Triggers", null, null, null);
            public static readonly GUIContent rigidbodyColor = L10n.TextContent("Rigidbodies", null, null, null);
            public static readonly GUIContent kinematicColor = L10n.TextContent("Kinematic Bodies", null, null, null);
            public static readonly GUIContent articulationBodyColor = L10n.TextContent("Articulation Bodies", null, null, null);
            public static readonly GUIContent sleepingBodyColor = L10n.TextContent("Sleeping Bodies", null, null, null);
            public static readonly GUIContent colorVariaition = L10n.TextContent("Variation", "Random color variation that is added on top of the base color", null, null);
            public static readonly GUIContent centerOfMassUseScreenSize = L10n.TextContent("Constant screen size", "Use constant screen size for the center of mass gizmos", null, null);
            public static readonly GUIContent inertiaTensorScale = L10n.TextContent("Inertia Tensor scale", "Scale by which the original inertia tensor is multiplied before drawing", null, null);
            public static readonly GUIContent useSceneCam = L10n.TextContent("Use Scene View Camera", "Draws Collider geometry onto the scene view if enabled. If disabled the geometry will be drawn onto the game view using the main camera.", null, null);
            public static readonly GUIContent forceOverdraw = L10n.TextContent("Force Overdraw", "Draws Collider geometry on top of render geometry", null, null);
            public static readonly GUIContent transparency = L10n.TextContent("Transparency", null, null, null);
            public static readonly GUIContent viewDistance = L10n.TextContent("View Distance", "Lower bound on distance from camera to physics geometry.", null, null);
            public static readonly GUIContent terrainTilesMax = L10n.TextContent("Terrain Tiles Max", "Number of terrain mesh tiles to draw.", null, null);
            public static readonly GUIContent gizmosSection = L10n.TextContent("Gizmos settings:", null, null, null);
            #endregion

            #region Contacts
            public static readonly GUIContent contactColors = L10n.TextContent("Contact colors", null, null, null);
            public static readonly GUIContent contactColor = L10n.TextContent("Contact color", null, null, null);
            public static readonly GUIContent contactSeparationColor = L10n.TextContent("Contact separation color", null, null, null);
            public static readonly GUIContent contactImpulseColor = L10n.TextContent("Contact impulse color", null, null, null);
            public static readonly GUIContent showContacts = L10n.TextContent("Show Contacts", "Should contacts be shown? Enabling this at runtime won't have an effect", null, null);
            public static readonly GUIContent showAllContacts = L10n.TextContent("Show All Contacts", "Should all physics objects report contacts or only the ones that were requested by MonoBehaviour scripts", null, null);
            public static readonly GUIContent showImpulse = L10n.TextContent("Show Impulse", "Show per contact point impulse", null, null);
            public static readonly GUIContent showSeparation = L10n.TextContent("Show Separation", "Show contact separation", null, null);
            public static readonly GUIContent useContactFiltering = L10n.TextContent("Use Filtering settings", "Should Filtering settings be taken into account when displaying contacts?", null, null);
            public static readonly GUIContent useVariedColors = L10n.TextContent("Use varied colors", "Uses collider instance ID to hash it into a color", null, null);
            #endregion

            #region Queries
            public static readonly GUIContent showQueries = L10n.TextContent("Show queries", "Should queries be visualized?", null, null);
            public static readonly GUIContent queryDuration = L10n.TextContent("Query duration", "How longs should the query be visible after it is performed?", null, null);
            public static readonly GUIContent queryColor = L10n.TextContent("Query color", "Color used for query visualization", null, null);
            public static readonly GUIContent sphereQueries = L10n.TextContent("Sphere", "Should sphere shaped queries be visualized?", null, null);
            public static readonly GUIContent boxQueries = L10n.TextContent("Box", "Should box shaped queries be visualized?", null, null);
            public static readonly GUIContent capsuleQueries = L10n.TextContent("Capsule", "Should capsule shaped queries be visualized?", null, null);
            public static readonly GUIContent rayQueries = L10n.TextContent("Ray", "Should ray shaped queries be visualized?", null, null);
            public static readonly GUIContent overlapQueries = L10n.TextContent("Overlap", "Should overlap type queries be visualized?", null, null);
            public static readonly GUIContent checkQueries = L10n.TextContent("Check", "Should check type queries be visualized?", null, null);
            public static readonly GUIContent castQueries = L10n.TextContent("Cast", "Should cast type queries be visualized?", null, null);
            public static readonly GUIContent showTypes = L10n.TextContent("Show types:", null, null, null);
            public static readonly GUIContent showShapes = L10n.TextContent("Show shapes:", null, null, null);
            public static readonly GUIContent maxNumberOfQueries = L10n.TextContent("Max Queries", "Maximum number of queries that will be visualized", null, null);
            #endregion

            #region Overlay
            public static readonly GUIContent showCollisionGeometry = L10n.TextContent("Collision Geometry", null, null, null);
            public static readonly GUIContent enableMouseSelect = L10n.TextContent("Mouse Select", null, null, null);
            #endregion

            #region Buttons
            public static readonly GUIContent showAll               = L10n.TextContent("Show All", null, null, null);
            public static readonly GUIContent showNone              = L10n.TextContent("Show None", null, null, null);
            public static readonly GUIContent resetButton           = L10n.TextContent("Reset", "Reset visualization settings and locked objects", null, null);
            public static readonly GUIContent connectSDKVisualDebugger = L10n.TextContent("Connect SDK Debugger","Only available with SDK debug builds.", null, null);
            public static readonly GUIContent disconnectSDKVisualDebugger = L10n.TextContent("Disconnect SDK Debugger", "Only available with SDK debug builds.", null, null);
            #endregion

            #region Info tables
            public static readonly GUIContent infoSpeed             = L10n.TextContent("Speed", null, null, null);
            public static readonly GUIContent infoVel               = L10n.TextContent("Velocity", null, null, null);
            public static readonly GUIContent infoAngVel            = L10n.TextContent("Angular Velocity", null, null, null);
            public static readonly GUIContent infoInertiaTensor     = L10n.TextContent("Inertia Tensor", null, null, null);
            public static readonly GUIContent infoInertiaTensorRotation = L10n.TextContent("Inertia Tensor Rotation", null, null, null);
            public static readonly GUIContent infoLocalCenterOfMass = L10n.TextContent("Local Center of Mass", null, null, null);
            public static readonly GUIContent infoWorldCenterOfMass = L10n.TextContent("World Center of Mass", null, null, null);
            public static readonly GUIContent infoSleepState        = L10n.TextContent("Sleep State", null, null, null);
            public static readonly GUIContent infoSleepThreshold    = L10n.TextContent("Sleep Threshold", null, null, null);
            public static readonly GUIContent infoMaxLinVel         = L10n.TextContent("Max Linear Velocity", null, null, null);
            public static readonly GUIContent infoMaxAngVel         = L10n.TextContent("Max Angular Velocity", null, null, null);
            public static readonly GUIContent infoSolverIterations  = L10n.TextContent("Solver Iterations", null, null, null);
            public static readonly GUIContent infoSolverVelIterations = L10n.TextContent("Solver Velocity Iterations", null, null, null);
            public static readonly GUIContent sleep = L10n.TextContent("Asleep", null, null, null);
            public static readonly GUIContent awake = L10n.TextContent("Awake", null, null, null);

            public static readonly GUIContent infoBodyIndex         = L10n.TextContent("Body Index", null, null, null);
            public static readonly GUIContent infoJointInfo         = L10n.TextContent("Joint Info", null, null, null);
            public static readonly GUIContent infoJointPosition     = L10n.TextContent("Position", null, null, null);
            public static readonly GUIContent infoJointVelocity     = L10n.TextContent("Velocity", null, null, null);
            public static readonly GUIContent infoJointForce        = L10n.TextContent("Force", null, null, null);
            public static readonly GUIContent infoJointAcceleration = L10n.TextContent("Acceleration", null, null, null);
            #endregion

            public static readonly GUIStyle tabBarStyle             = GUI.skin.button;
            public static readonly string[] tabs;

            static Style()
            {
                if(Unsupported.IsDeveloperMode())
                    tabs = new string[] { "Info", "Filtering", "Rendering", "Contacts", "Queries", "Internal" };
                else
                    tabs = new string[] { "Info", "Filtering", "Rendering", "Contacts", "Queries" };

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
                window.titleContent.text = "Physics Debugger";
                window.minSize = new Vector2(1000f, 500f);
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
            HandleUtility.pickClosestGameObjectDelegate = enabled ? PhysicsVisualizationSettings.PickClosestGameObject : null;
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

            if (GUILayout.Button(Style.resetButton, EditorStyles.toolbarButtonRight, Style.maxWidth75))
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
                case Tabs.Internal:
                    DrawInternalTab();
                    break;
                default: //default to info tab
                    DrawInfoTab();
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

            bool selectNone = GUILayout.Button(Style.showNone, EditorStyles.miniButtonLeft);
            bool selectAll = GUILayout.Button(Style.showAll, EditorStyles.miniButtonRight);
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

            PhysicsVisualizationSettings.useSceneCam = EditorGUILayout.Toggle(Style.useSceneCam
                , PhysicsVisualizationSettings.useSceneCam);

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

        [Overlay(typeof(SceneView), k_OverlayId, k_DisplayName, defaultDisplay = false, group = OverlayAttribute.unityGroup)]
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
