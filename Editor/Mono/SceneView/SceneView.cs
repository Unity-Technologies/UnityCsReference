// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.AnimatedValues;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEditorInternal;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;
using UnityEngine.Rendering;
using UnityEditor.Rendering;
using UnityEngine.Experimental.Rendering;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor
{
    [EditorWindowTitle(title = "Scene", useTypeNameAsIconName = true)]
    public class SceneView : SearchableEditorWindow, IHasCustomMenu
    {
        private static SceneView s_LastActiveSceneView;
        private static SceneView s_CurrentDrawingSceneView;

        public static SceneView lastActiveSceneView { get { return s_LastActiveSceneView; } }
        public static SceneView currentDrawingSceneView { get { return s_CurrentDrawingSceneView; } }

        static readonly PrefColor kSceneViewBackground = new PrefColor("Scene/Background", 0.278431f, 0.278431f, 0.278431f, 0);
        static readonly PrefColor kSceneViewWire = new PrefColor("Scene/Wireframe", 0.0f, 0.0f, 0.0f, 0.5f);
        static readonly PrefColor kSceneViewWireOverlay = new PrefColor("Scene/Wireframe Overlay", 0.0f, 0.0f, 0.0f, 0.25f);
        static readonly PrefColor kSceneViewSelectedOutline = new PrefColor("Scene/Selected Outline", 255.0f / 255.0f, 102.0f / 255.0f, 0.0f / 255.0f, 0.0f / 255.0f);
        static readonly PrefColor kSceneViewSelectedWire = new PrefColor("Scene/Wireframe Selected", 94.0f / 255.0f, 119.0f / 255.0f, 155.0f / 255.0f, 64.0f / 255.0f);

        static readonly PrefColor kSceneViewMaterialValidateLow = new PrefColor("Scene/Material Validator Value Too Low", 255.0f / 255.0f, 0.0f, 0.0f, 1.0f);
        static readonly PrefColor kSceneViewMaterialValidateHigh = new PrefColor("Scene/Material Validator Value Too High", 0.0f, 0.0f, 255.0f / 255.0f, 1.0f);
        static readonly PrefColor kSceneViewMaterialValidatePureMetal = new PrefColor("Scene/Material Validator Pure Metal", 255.0f / 255.0f, 255.0f / 255.0f, 0.0f, 1.0f);

        internal static Color kSceneViewFrontLight = new Color(0.769f, 0.769f, 0.769f, 1);
        internal static Color kSceneViewUpLight = new Color(0.212f, 0.227f, 0.259f, 1);
        internal static Color kSceneViewMidLight = new Color(0.114f, 0.125f, 0.133f, 1);
        internal static Color kSceneViewDownLight = new Color(0.047f, 0.043f, 0.035f, 1);

        [NonSerialized]
        static readonly Quaternion kDefaultRotation = Quaternion.LookRotation(new Vector3(-1, -.7f, -1));

        private const float kDefaultViewSize = 10f;

        private const CameraEvent kCommandBufferCameraEvent = CameraEvent.AfterImageEffectsOpaque;

        [NonSerialized]
        static readonly Vector3 kDefaultPivot = Vector3.zero;

        const float kOrthoThresholdAngle = 3f;
        const float kOneOverSqrt2 = 0.707106781f;

        [NonSerialized]
        ActiveEditorTracker m_Tracker;

        [SerializeField]
        public bool m_SceneLighting = true;

        public double lastFramingTime = 0;
        private const double k_MaxDoubleKeypressTime = 0.5;

        [Serializable]
        public class SceneViewState
        {
            public bool showFog = true;
            public bool showMaterialUpdate = false;
            public bool showSkybox = true;
            public bool showFlares = true;
            public bool showImageEffects = true;
            public bool showParticleSystems = true;

            public SceneViewState()
            {
            }

            public SceneViewState(SceneViewState other)
            {
                showFog = other.showFog;
                showMaterialUpdate = other.showMaterialUpdate;
                showSkybox = other.showSkybox;
                showFlares = other.showFlares;
                showImageEffects = other.showImageEffects;
                showParticleSystems = other.showParticleSystems;
            }

            public bool IsAllOn()
            {
                return showFog && showMaterialUpdate && showSkybox && showFlares && showImageEffects && showParticleSystems;
            }

            public void Toggle(bool value)
            {
                showFog = value;
                showMaterialUpdate = value;
                showSkybox = value;
                showFlares = value;
                showImageEffects = value;
                showParticleSystems = value;
            }
        }

        static readonly PrefKey k2DMode = new PrefKey("Tools/2D Mode", "2");
        private static bool waitingFor2DModeKeyUp;

        [SerializeField]
        private bool m_2DMode;
        public bool in2DMode
        {
            get { return m_2DMode; }
            set
            {
                if (m_2DMode != value && Tools.viewTool != ViewTool.FPS && Tools.viewTool != ViewTool.Orbit)
                {
                    m_2DMode = value;
                    On2DModeChange();
                }
            }
        }

        [SerializeField]
        bool m_isRotationLocked = false;
        public bool isRotationLocked
        {
            get
            {
                return m_isRotationLocked;
            }
            set
            {
                if (m_isRotationLocked != value)
                {
                    m_isRotationLocked = value;
                }
            }
        }

        internal Object m_OneClickDragObject;

        public bool m_AudioPlay = false;
        static SceneView s_AudioSceneView;

        [SerializeField]
        AnimVector3 m_Position = new AnimVector3(kDefaultPivot);

        public delegate void OnSceneFunc(SceneView sceneView);
        public static OnSceneFunc onSceneGUIDelegate;

        //TODO: we want to expose this callback to the users, so they have
        //the ability to draw custom things to the SceneView, without the need
        //for a CustomEditor.
        //We are waiting for some public API guidelines regarding delegates
        //to be able to expose this in a good way.
        //While exposing this one, we should also document onSceneGUIDelegate
        //And if possible AutoUpgrade it to the new way of doing this.
        internal static OnSceneFunc onPreSceneGUIDelegate;

        public DrawCameraMode m_RenderMode = 0;

        public DrawCameraMode renderMode
        {
            get { return m_RenderMode; }
            set
            {
                m_RenderMode = value;
                SetupPBRValidation();
            }
        }

        private DrawCameraMode lastRenderMode = 0;

        public bool m_ValidateTrueMetals = false;

        [SerializeField]
        private SceneViewState m_SceneViewState;

        public SceneViewState sceneViewState
        {
            get { return m_SceneViewState; }
        }

        [SerializeField]
        SceneViewGrid grid;
        [SerializeField]
        internal SceneViewRotation svRot;
        [SerializeField]
        internal AnimQuaternion m_Rotation = new AnimQuaternion(kDefaultRotation);

        /// How large an area the scene view covers (measured diagonally). Modify this for immediate effect, or use LookAt to animate it nicely.
        [SerializeField]
        AnimFloat m_Size = new AnimFloat(kDefaultViewSize);

        [SerializeField]
        internal AnimBool m_Ortho = new AnimBool();

        [NonSerialized]
        Camera m_Camera;

        [SerializeField]
        bool m_ShowGlobalGrid = true;
        internal bool showGlobalGrid { get { return m_ShowGlobalGrid; } set { m_ShowGlobalGrid = value; } }
        internal bool drawGlobalGrid { get { return AnnotationUtility.showGrid && showGlobalGrid; } }

        [SerializeField]
        private Quaternion m_LastSceneViewRotation;
        public Quaternion lastSceneViewRotation
        {
            get
            {
                if (m_LastSceneViewRotation == new Quaternion(0f, 0f, 0f, 0f))
                    m_LastSceneViewRotation = Quaternion.identity;
                return m_LastSceneViewRotation;
            }
            set { m_LastSceneViewRotation = value; }
        }
        [SerializeField]
        private bool m_LastSceneViewOrtho;

        // Cursor rect handling
        private struct CursorRect
        {
            public Rect rect;
            public MouseCursor cursor;
            public CursorRect(Rect rect, MouseCursor cursor)
            {
                this.rect = rect;
                this.cursor = cursor;
            }
        }
        private static MouseCursor s_LastCursor = MouseCursor.Arrow;
        private static readonly List<CursorRect> s_MouseRects = new List<CursorRect>();
        private bool s_DraggingCursorIsCached;
        internal static void AddCursorRect(Rect rect, MouseCursor cursor)
        {
            if (Event.current.type == EventType.Repaint)
                s_MouseRects.Add(new CursorRect(rect, cursor));
        }

        public float cameraDistance
        {
            get
            {
                float fov = m_Ortho.Fade(kPerspectiveFov, 0);

                if (!camera.orthographic)
                {
                    return size / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
                }
                return size * 2f;
            }
        }

        [System.NonSerialized]
        Light[] m_Light = new Light[3];

        RectSelection m_RectSelection;

        const float kPerspectiveFov = 90;

        static ArrayList s_SceneViews = new ArrayList();
        public static ArrayList sceneViews { get {return s_SceneViews; }}

        static Material s_AlphaOverlayMaterial;
        static Material s_DeferredOverlayMaterial;
        static Shader s_ShowOverdrawShader;
        static Shader s_ShowMipsShader;
        static Shader s_AuraShader;
        static Texture2D s_MipColorsTexture;

        // Handle Dragging of stuff over scene view
        //static ArrayList s_DraggedEditors = null;
        //static GameObject[] s_PickedObject = { null };
        GUIContent m_Lighting;
        GUIContent m_Fx;
        GUIContent m_AudioPlayContent;
        GUIContent m_GizmosContent;
        GUIContent m_2DModeContent;
        GUIContent m_RenderDocContent;

        // Which tool are we currently editing with.
        // This gets updated whenever hotControl == 0, so once the user has started sth, they can't change it mid-drag by e.g. pressing alt
        static Tool s_CurrentTool;

        double m_StartSearchFilterTime = -1;
        RenderTexture m_SceneTargetTexture;
        int m_MainViewControlID;

        public Camera camera { get { return m_Camera; } }

        [SerializeField]
        private Shader m_ReplacementShader;
        [SerializeField]
        private string m_ReplacementString;

        public void SetSceneViewShaderReplace(Shader shader, string replaceString)
        {
            m_ReplacementShader = shader;
            m_ReplacementString = replaceString;
        }

        internal bool m_ShowSceneViewWindows = false;
        SceneViewOverlay m_SceneViewOverlay;
        EditorCache m_DragEditorCache;

        // While Locking the view to object, we have different behaviour for different scenarios:
        // Smooth camera behaviour: User dragging the handles
        // Instant camera behaviour: Position changed externally (via inspector, physics or scripts etc.)
        internal enum DraggingLockedState
        {
            NotDragging, // Default state. Scene view camera is snapped to selected object instantly
            Dragging, // User is dragging from handles. Scene view camera holds still.
            LookAt // Temporary state after dragging or selection change, where we return scene view camera smoothly to selected object
        }
        DraggingLockedState m_DraggingLockedState;
        internal DraggingLockedState draggingLocked { set { m_DraggingLockedState = value; } get { return m_DraggingLockedState; } }

        [SerializeField]
        private Object m_LastLockedObject;

        [SerializeField]
        bool m_ViewIsLockedToObject;
        internal bool viewIsLockedToObject
        {
            get { return m_ViewIsLockedToObject; }
            set
            {
                if (value)
                    m_LastLockedObject = Selection.activeObject;
                else
                    m_LastLockedObject = null;

                m_ViewIsLockedToObject = value;
                draggingLocked = DraggingLockedState.LookAt;
            }
        }


        [RequiredByNativeCode]
        public static bool FrameLastActiveSceneView()
        {
            if (lastActiveSceneView == null)
                return false;
            return lastActiveSceneView.SendEvent(EditorGUIUtility.CommandEvent("FrameSelected"));
        }

        [RequiredByNativeCode]
        public static bool FrameLastActiveSceneViewWithLock()
        {
            if (lastActiveSceneView == null)
                return false;
            return lastActiveSceneView.SendEvent(EditorGUIUtility.CommandEvent("FrameSelectedWithLock"));
        }

        Editor[] GetActiveEditors()
        {
            if (m_Tracker == null)
                m_Tracker = ActiveEditorTracker.sharedTracker;
            return m_Tracker.activeEditors;
        }

        public static Camera[] GetAllSceneCameras()
        {
            ArrayList array = new ArrayList();
            for (int i = 0; i < s_SceneViews.Count; ++i)
            {
                Camera cam = ((SceneView)s_SceneViews[i]).m_Camera;
                if (cam != null)
                    array.Add(cam);
            }
            return (Camera[])array.ToArray(typeof(Camera));
        }

        public static void RepaintAll()
        {
            foreach (SceneView sv in s_SceneViews)
            {
                sv.Repaint();
            }
        }

        internal override void SetSearchFilter(string searchFilter, SearchMode mode, bool setAll, bool delayed = false)
        {
            if (m_SearchFilter == "" || searchFilter == "")
                m_StartSearchFilterTime = EditorApplication.timeSinceStartup;

            base.SetSearchFilter(searchFilter, mode, setAll, delayed);
        }

        internal void OnLostFocus()
        {
            // don't bleed our scene view rendering into game view
            GameView gameView = (GameView)WindowLayout.FindEditorWindowOfType(typeof(GameView));
            if (gameView && gameView.m_Parent != null && m_Parent != null && gameView.m_Parent == m_Parent)
            {
                gameView.m_Parent.backgroundValid = false;
            }

            if (s_LastActiveSceneView == this)
                SceneViewMotion.ResetMotion();
        }

        override public void OnEnable()
        {
            titleContent = GetLocalizedTitleContent();
            m_RectSelection = new RectSelection(this);
            if (grid == null)
                grid = new SceneViewGrid();
            grid.Register(this);
            if (svRot == null)
                svRot = new SceneViewRotation();
            svRot.Register(this);

            autoRepaintOnSceneChange = true;

            m_Rotation.valueChanged.AddListener(Repaint);
            m_Position.valueChanged.AddListener(Repaint);
            m_Size.valueChanged.AddListener(Repaint);
            m_Ortho.valueChanged.AddListener(Repaint);

            wantsMouseMove = true;
            wantsMouseEnterLeaveWindow = true;
            dontClearBackground = true;
            s_SceneViews.Add(this);

            m_Lighting = EditorGUIUtility.IconContent("SceneviewLighting", "Lighting|When toggled on, the Scene lighting is used. When toggled off, a light attached to the Scene view camera is used.");
            m_Fx = EditorGUIUtility.IconContent("SceneviewFx", "Effects|Toggle skybox, fog, and various other effects.");
            m_AudioPlayContent = EditorGUIUtility.IconContent("SceneviewAudio", "AudioPlay|Toggle audio on or off.");
            m_GizmosContent = EditorGUIUtility.TextContent("Gizmos|Toggle the visibility of different Gizmos in the Scene view.");
            m_2DModeContent = new GUIContent("2D", "When togggled on, the Scene is in 2D view. When toggled off, the Scene is in 3D view.");
            m_RenderDocContent = EditorGUIUtility.IconContent("renderdoc", "Capture|Capture the current view and open in RenderDoc.");

            m_SceneViewOverlay = new SceneViewOverlay(this);

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            EditorApplication.modifierKeysChanged += RepaintAll; // Because we show handles on shift
            m_DraggingLockedState = DraggingLockedState.NotDragging;

            CreateSceneCameraAndLights();

            if (m_2DMode)
                LookAt(pivot, Quaternion.identity, size, true, true);

            base.OnEnable();
        }

        public SceneView()
        {
            m_HierarchyType = HierarchyType.GameObjects;

            // Note: Rendering for Scene view picking depends on the depth buffer of the window
            depthBufferBits = 32;
        }

        internal void Awake()
        {
            if (sceneViewState == null)
                m_SceneViewState = new SceneViewState();

            if (m_2DMode || EditorSettings.defaultBehaviorMode == EditorBehaviorMode.Mode2D)
            {
                m_LastSceneViewRotation = Quaternion.LookRotation(new Vector3(-1, -.7f, -1));
                m_LastSceneViewOrtho = false;
                m_Rotation.value = Quaternion.identity;
                m_Ortho.value = true;
                m_2DMode = true;

                // Enforcing Rect tool as the default in 2D mode.
                if (Tools.current == Tool.Move)
                    Tools.current = Tool.Rect;
            }
        }

        internal static void PlaceGameObjectInFrontOfSceneView(GameObject go)
        {
            if (s_SceneViews.Count >= 1)
            {
                SceneView view = s_LastActiveSceneView;
                if (!view)
                    view = s_SceneViews[0] as SceneView;
                if (view)
                {
                    view.MoveToView(go.transform);
                }
            }
        }

        internal static Camera GetLastActiveSceneViewCamera()
        {
            SceneView view = s_LastActiveSceneView;
            return view ? view.camera : null;
        }

        override public void OnDisable()
        {
            EditorApplication.modifierKeysChanged -= RepaintAll;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            if (m_Camera)
                DestroyImmediate(m_Camera.gameObject, true);
            if (m_Light[0])
                DestroyImmediate(m_Light[0].gameObject, true);
            if (m_Light[1])
                DestroyImmediate(m_Light[1].gameObject, true);
            if (m_Light[2])
                DestroyImmediate(m_Light[2].gameObject, true);
            if (s_MipColorsTexture)
                DestroyImmediate(s_MipColorsTexture, true);
            s_SceneViews.Remove(this);
            if (s_LastActiveSceneView == this)
            {
                if (s_SceneViews.Count > 0)
                    s_LastActiveSceneView = s_SceneViews[0] as SceneView;
                else
                    s_LastActiveSceneView = null;
            }

            CleanupEditorDragFunctions();
            base.OnDisable();
        }

        public void OnDestroy()
        {
            if (m_AudioPlay)
            {
                m_AudioPlay = false;
                RefreshAudioPlay();
            }
        }

        internal void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (m_AudioPlay)
            {
                m_AudioPlay = false;
                RefreshAudioPlay();
            }
        }

        private static GUIStyle s_DropDownStyle;
        private GUIStyle effectsDropDownStyle
        {
            get
            {
                if (s_DropDownStyle == null)
                    s_DropDownStyle = "GV Gizmo DropDown";
                return s_DropDownStyle;
            }
        }

        void DoToolbarGUI()
        {
            GUILayout.BeginHorizontal("toolbar");
            {
                // render mode popup
                GUIContent modeContent = SceneRenderModeWindow.GetGUIContent(m_RenderMode);
                modeContent.tooltip = LocalizationDatabase.GetLocalizedString("The Draw Mode used to display the Scene.");
                Rect modeRect = GUILayoutUtility.GetRect(modeContent, EditorStyles.toolbarDropDown, GUILayout.Width(120));
                if (EditorGUI.DropdownButton(modeRect, modeContent, FocusType.Passive, EditorStyles.toolbarDropDown))
                {
                    Rect rect = GUILayoutUtility.topLevel.GetLast();
                    PopupWindow.Show(rect, new SceneRenderModeWindow(this));
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.Space();

                in2DMode = GUILayout.Toggle(in2DMode, m_2DModeContent, "toolbarbutton");

                EditorGUILayout.Space();

                m_SceneLighting = GUILayout.Toggle(m_SceneLighting, m_Lighting, "toolbarbutton");
                if (renderMode == DrawCameraMode.ShadowCascades) // cascade visualization requires actual lights with shadows
                    m_SceneLighting = true;

                GUI.enabled = !Application.isPlaying;
                GUI.changed = false;
                m_AudioPlay = GUILayout.Toggle(m_AudioPlay, m_AudioPlayContent, EditorStyles.toolbarButton);
                if (GUI.changed)
                    RefreshAudioPlay();

                GUI.enabled = true;

                Rect fxRect = GUILayoutUtility.GetRect(m_Fx, effectsDropDownStyle);
                Rect fxRightRect = new Rect(fxRect.xMax - effectsDropDownStyle.border.right, fxRect.y, effectsDropDownStyle.border.right, fxRect.height);
                if (EditorGUI.DropdownButton(fxRightRect, GUIContent.none, FocusType.Passive, GUIStyle.none))
                {
                    Rect rect = GUILayoutUtility.topLevel.GetLast();
                    PopupWindow.Show(rect, new SceneFXWindow(this));
                    GUIUtility.ExitGUI();
                }

                var allOn = GUI.Toggle(fxRect, sceneViewState.IsAllOn(), m_Fx, effectsDropDownStyle);
                if (allOn != sceneViewState.IsAllOn())
                    sceneViewState.Toggle(allOn);

                EditorGUILayout.Space();
                GUILayout.FlexibleSpace();

                if (m_MainViewControlID != GUIUtility.keyboardControl
                    && Event.current.type == EventType.KeyDown
                    && !string.IsNullOrEmpty(m_SearchFilter)
                    )
                {
                    switch (Event.current.keyCode)
                    {
                        case KeyCode.UpArrow:
                        case KeyCode.DownArrow:
                            if (Event.current.keyCode == KeyCode.UpArrow)
                                SelectPreviousSearchResult();
                            else
                                SelectNextSearchResult();

                            FrameSelected(false);
                            Event.current.Use();
                            GUIUtility.ExitGUI();
                            return;
                    }
                }

                if (RenderDoc.IsLoaded())
                {
                    using (new EditorGUI.DisabledScope(!RenderDoc.IsSupported()))
                    {
                        if (GUILayout.Button(m_RenderDocContent, EditorStyles.toolbarButton))
                        {
                            m_Parent.CaptureRenderDoc();
                            GUIUtility.ExitGUI();
                        }
                    }
                }

                Rect r = GUILayoutUtility.GetRect(m_GizmosContent, EditorStyles.toolbarDropDown);
                if (EditorGUI.DropdownButton(r, m_GizmosContent, FocusType.Passive, EditorStyles.toolbarDropDown))
                {
                    Rect rect = GUILayoutUtility.topLevel.GetLast();
                    if (AnnotationWindow.ShowAtPosition(rect, false))
                    {
                        GUIUtility.ExitGUI();
                    }
                }
                GUILayout.Space(6);

                SearchFieldGUI(EditorGUILayout.kLabelFloatMaxW);
            }
            GUILayout.EndHorizontal();
        }

        // This method should be called after the audio play button has been toggled,
        // and after other events that require a refresh.
        void RefreshAudioPlay()
        {
            if ((s_AudioSceneView != null) && (s_AudioSceneView != this))
            {
                // turn *other* sceneview off
                if (s_AudioSceneView.m_AudioPlay)
                {
                    s_AudioSceneView.m_AudioPlay = false;
                    s_AudioSceneView.Repaint();
                }
            }

            var sources = (AudioSource[])FindObjectsOfType(typeof(AudioSource));
            foreach (AudioSource source in sources)
            {
                if (source.playOnAwake)
                {
                    if (!m_AudioPlay)
                    {
                        source.Stop();
                    }
                    else
                    {
                        if (!source.isPlaying)
                            source.Play();
                    }
                }
            }
            AudioUtil.SetListenerTransform(m_AudioPlay ? m_Camera.transform : null);

            s_AudioSceneView = this;
        }

        /// TODO: Don't repaint sceneview unless either old or new selection is a scene object
        public void OnSelectionChange()
        {
            if (Selection.activeObject != null && m_LastLockedObject != Selection.activeObject)
            {
                viewIsLockedToObject = false;
            }
            Repaint();
        }

        private void LoadRenderDoc()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                RenderDoc.Load();
                ShaderUtil.RecreateGfxDevice();
            }
        }

        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            if (RenderDoc.IsInstalled() && !RenderDoc.IsLoaded())
            {
                menu.AddItem(new GUIContent("Load RenderDoc"), false, LoadRenderDoc);
            }
        }

        [MenuItem("GameObject/Set as first sibling %=")]
        static internal void MenuMoveToFront()
        {
            foreach (Transform t in Selection.transforms)
            {
                Undo.SetTransformParent(t, t.parent, "Set as first sibling");
                t.SetAsFirstSibling();
            }
        }

        [MenuItem("GameObject/Set as first sibling %=", true)]
        static internal bool ValidateMenuMoveToFront()
        {
            if (Selection.activeTransform != null)
            {
                Transform parent = Selection.activeTransform.parent;
                return (parent != null && parent.GetChild(0) != Selection.activeTransform);
            }
            return false;
        }

        [MenuItem("GameObject/Set as last sibling %-")]
        static internal void MenuMoveToBack()
        {
            foreach (Transform t in Selection.transforms)
            {
                Undo.SetTransformParent(t, t.parent, "Set as last sibling");
                t.SetAsLastSibling();
            }
        }

        [MenuItem("GameObject/Set as last sibling %-", true)]
        static internal bool ValidateMenuMoveToBack()
        {
            if (Selection.activeTransform != null)
            {
                Transform parent = Selection.activeTransform.parent;
                return (parent != null && parent.GetChild(parent.childCount - 1) != Selection.activeTransform);
            }
            return false;
        }

        [MenuItem("GameObject/Move To View %&f")]
        static internal void MenuMoveToView()
        {
            if (ValidateMoveToView())
                s_LastActiveSceneView.MoveToView();
        }

        [MenuItem("GameObject/Move To View %&f", true)]
        static bool ValidateMoveToView()
        {
            return s_LastActiveSceneView != null && (Selection.transforms.Length != 0);
        }

        [MenuItem("GameObject/Align With View %#f")]
        static internal void MenuAlignWithView()
        {
            if (ValidateAlignWithView())
                s_LastActiveSceneView.AlignWithView();
        }

        [MenuItem("GameObject/Align With View %#f", true)]
        static internal bool ValidateAlignWithView()
        {
            return s_LastActiveSceneView != null && (Selection.activeTransform != null);
        }

        [MenuItem("GameObject/Align View to Selected")]
        static internal void MenuAlignViewToSelected()
        {
            if (ValidateAlignViewToSelected())
                s_LastActiveSceneView.AlignViewToObject(Selection.activeTransform);
        }

        [MenuItem("GameObject/Align View to Selected", true)]
        static internal bool ValidateAlignViewToSelected()
        {
            return s_LastActiveSceneView != null && (Selection.activeTransform != null);
        }

        [MenuItem("GameObject/Toggle Active State &#a")]
        static internal void ActivateSelection()
        {
            if (Selection.activeTransform != null)
            {
                GameObject[] gos = Selection.gameObjects;
                Undo.RecordObjects(gos, "Toggle Active State");
                bool val = !Selection.activeGameObject.activeSelf;
                foreach (GameObject go in gos)
                    go.SetActive(val);
            }
        }

        [MenuItem("GameObject/Toggle Active State &#a", true)]
        static internal bool ValidateActivateSelection()
        {
            return (Selection.activeTransform != null);
        }

        static private void CreateMipColorsTexture()
        {
            if (s_MipColorsTexture)
                return;
            s_MipColorsTexture = new Texture2D(32, 32, TextureFormat.RGBA32, true);
            s_MipColorsTexture.hideFlags = HideFlags.HideAndDontSave;
            Color[] colors = new Color[6];
            colors[0] = new Color(0.0f, 0.0f, 1.0f, 0.8f);
            colors[1] = new Color(0.0f, 0.5f, 1.0f, 0.4f);
            colors[2] = new Color(1.0f, 1.0f, 1.0f, 0.0f); // optimal level
            colors[3] = new Color(1.0f, 0.7f, 0.0f, 0.2f);
            colors[4] = new Color(1.0f, 0.3f, 0.0f, 0.6f);
            colors[5] = new Color(1.0f, 0.0f, 0.0f, 0.8f);
            int mipCount = Mathf.Min(6, s_MipColorsTexture.mipmapCount);
            for (int mip = 0; mip < mipCount; ++mip)
            {
                int width = Mathf.Max(s_MipColorsTexture.width >> mip, 1);
                int height = Mathf.Max(s_MipColorsTexture.height >> mip, 1);
                Color[] cols = new Color[width * height];
                for (int i = 0; i < cols.Length; ++i)
                    cols[i] = colors[mip];
                s_MipColorsTexture.SetPixels(cols, mip);
            }
            s_MipColorsTexture.filterMode = FilterMode.Trilinear;
            s_MipColorsTexture.Apply(false);
            Shader.SetGlobalTexture("_SceneViewMipcolorsTexture", s_MipColorsTexture);
        }

        private bool m_RequestedSceneViewFiltering;
        private double m_lastRenderedTime;

        public void SetSceneViewFiltering(bool enable)
        {
            m_RequestedSceneViewFiltering = enable;
        }

        private bool UseSceneFiltering()
        {
            return !string.IsNullOrEmpty(m_SearchFilter) || m_RequestedSceneViewFiltering;
        }

        internal bool SceneViewIsRenderingHDR()
        {
            return m_Camera != null && m_Camera.allowHDR;
        }

        private void HandleClickAndDragToFocus()
        {
            Event evt = Event.current;
            if (evt.type == EventType.MouseDown || evt.type == EventType.MouseDrag)
                s_LastActiveSceneView = this;
            else if (s_LastActiveSceneView == null)
                s_LastActiveSceneView = this;

            if (evt.type == EventType.MouseDrag)
                draggingLocked = DraggingLockedState.Dragging;
            else if (GUIUtility.hotControl == 0 && draggingLocked == DraggingLockedState.Dragging)
                draggingLocked = DraggingLockedState.LookAt;

            if (evt.type == EventType.MouseDown)
            {
                Tools.s_ButtonDown = evt.button;
                if (evt.button == 1 && Application.platform == RuntimePlatform.OSXEditor)
                    Focus();
            }
            // this is necessary because FPS tool won't get is cleanup logic
            // executed if another control uses the Event (i.e OnSceneGUI) (case 777346)
            else if (evt.type == EventType.MouseUp && Tools.s_ButtonDown == evt.button)
            {
                Tools.s_ButtonDown = -1;
            }
        }

        private void SetupFogAndShadowDistance(out bool oldFog, out float oldShadowDistance)
        {
            oldFog = RenderSettings.fog;
            oldShadowDistance = QualitySettings.shadowDistance;
            if (Event.current.type == EventType.Repaint)
            {
                if (!sceneViewState.showFog)
                    Unsupported.SetRenderSettingsUseFogNoDirty(false);
                if (m_Camera.orthographic)
                    Unsupported.SetQualitySettingsShadowDistanceTemporarily(QualitySettings.shadowDistance + 0.5f * cameraDistance);
            }
        }

        private void RestoreFogAndShadowDistance(bool oldFog, float oldShadowDistance)
        {
            if (Event.current.type == EventType.Repaint)
            {
                Unsupported.SetRenderSettingsUseFogNoDirty(oldFog);
                Unsupported.SetQualitySettingsShadowDistanceTemporarily(oldShadowDistance);
            }
        }

        private void CreateCameraTargetTexture(Rect cameraRect, bool hdr)
        {
            bool useSRGBTarget = QualitySettings.activeColorSpace == ColorSpace.Linear;

            int msaa = Mathf.Max(1, QualitySettings.antiAliasing);
            // deferred does not support MSAA now, so not point in using it
            if (IsSceneCameraDeferred())
                msaa = 1;

            // TODO: 1st gen OSX Metal drivers (El Capitan) do not support
            // MSAA store+resolve action flag or deferred store action flags,
            // this can be lead to undefined behaviour depending on GPU driver
            // used. Some may work with multiple load/store passes or survive
            // setting the depth store action to DontCare with MSAA depth buffers
            //
            // MSAA should be ok with game view, but SceneView RT handling is
            // a bit more complicated that needs improvements later
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
                msaa = 1;

            // make sure we actually support ARGBHalf (ShaderModel20 emulation doesn't)
            RenderTextureFormat format = (hdr && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf)) ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
            if (m_SceneTargetTexture != null)
            {
                bool matchingSRGB = m_SceneTargetTexture != null && useSRGBTarget == m_SceneTargetTexture.sRGB;

                //ARGBHalf is always non srgb, so just force a match
                //stops texture always being recreated: Case:869375
                if (RenderTextureEditor.IsHDRFormat(format))
                    matchingSRGB = true;

                if (m_SceneTargetTexture.format != format || m_SceneTargetTexture.antiAliasing != msaa || !matchingSRGB)
                {
                    Object.DestroyImmediate(m_SceneTargetTexture);
                    m_SceneTargetTexture = null;
                }
            }

            Rect actualCameraRect = Handles.GetCameraRect(cameraRect);
            int width = (int)actualCameraRect.width;
            int height = (int)actualCameraRect.height;

            if (m_SceneTargetTexture == null)
            {
                m_SceneTargetTexture = new RenderTexture(0, 0, 24, format);
                m_SceneTargetTexture.name = "SceneView RT";
                m_SceneTargetTexture.antiAliasing = msaa;
                m_SceneTargetTexture.hideFlags = HideFlags.HideAndDontSave;
            }
            if (m_SceneTargetTexture.width != width || m_SceneTargetTexture.height != height)
            {
                m_SceneTargetTexture.Release();
                m_SceneTargetTexture.width = width;
                m_SceneTargetTexture.height = height;
            }
            m_SceneTargetTexture.Create();
            EditorGUIUtility.SetGUITextureBlitColorspaceSettings(EditorGUIUtility.GUITextureBlitColorspaceMaterial);
        }

        internal bool IsCameraDrawModeEnabled(DrawCameraMode mode)
        {
            return Handles.IsCameraDrawModeEnabled(m_Camera, mode);
        }

        internal bool IsSceneCameraDeferred()
        {
            if (m_Camera == null)
                return false;
            if (m_Camera.actualRenderingPath == RenderingPath.DeferredLighting || m_Camera.actualRenderingPath == RenderingPath.DeferredShading)
                return true;
            return false;
        }

        internal static bool DoesCameraDrawModeSupportDeferred(DrawCameraMode mode)
        {
            // many of special visualization modes don't support deferred shading/lighting
            // overdraw/mipmaps visualizations need special forward shader
            // various lightmaps/visualization modes, don't use deferred for safety (previous code also did not use deferred)
            return
                mode == DrawCameraMode.Normal ||
                mode == DrawCameraMode.Textured ||
                mode == DrawCameraMode.TexturedWire ||
                mode == DrawCameraMode.ShadowCascades ||
                mode == DrawCameraMode.RenderPaths ||
                mode == DrawCameraMode.AlphaChannel ||
                mode == DrawCameraMode.DeferredDiffuse ||
                mode == DrawCameraMode.DeferredSpecular ||
                mode == DrawCameraMode.DeferredSmoothness ||
                mode == DrawCameraMode.DeferredNormal ||
                mode == DrawCameraMode.RealtimeCharting ||
                mode == DrawCameraMode.Systems ||
                mode == DrawCameraMode.Clustering ||
                mode == DrawCameraMode.LitClustering ||
                mode == DrawCameraMode.RealtimeAlbedo ||
                mode == DrawCameraMode.RealtimeEmissive ||
                mode == DrawCameraMode.RealtimeIndirect ||
                mode == DrawCameraMode.RealtimeDirectionality ||
                mode == DrawCameraMode.BakedLightmap ||
                mode == DrawCameraMode.ValidateAlbedo ||
                mode == DrawCameraMode.ValidateMetalSpecular;
        }

        internal static bool DoesCameraDrawModeSupportHDR(DrawCameraMode mode)
        {
            // HDR/Tonemap only supported on regular views, and not on any special visualizations
            return
                mode == DrawCameraMode.Textured || mode == DrawCameraMode.TexturedWire;
        }

        private void PrepareCameraTargetTexture(Rect cameraRect)
        {
            // Always render camera into a RT
            bool hdr = SceneViewIsRenderingHDR();
            CreateCameraTargetTexture(cameraRect, hdr);
            m_Camera.targetTexture = m_SceneTargetTexture;

            // Do not use deferred rendering when using search filtering or wireframe/overdraw/mipmaps rendering modes.
            if (UseSceneFiltering() || !DoesCameraDrawModeSupportDeferred(m_RenderMode))
            {
                if (IsSceneCameraDeferred())
                    m_Camera.renderingPath = RenderingPath.Forward;
            }
        }

        private void PrepareCameraReplacementShader()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            // Set scene view colors
            Handles.SetSceneViewColors(kSceneViewWire, kSceneViewWireOverlay, kSceneViewSelectedOutline, kSceneViewSelectedWire);

            // Setup shader replacement if needed by overlay mode
            if (m_RenderMode == DrawCameraMode.Overdraw)
            {
                // show overdraw
                if (!s_ShowOverdrawShader)
                    s_ShowOverdrawShader = EditorGUIUtility.LoadRequired("SceneView/SceneViewShowOverdraw.shader") as Shader;
                m_Camera.SetReplacementShader(s_ShowOverdrawShader, "RenderType");
            }
            else if (m_RenderMode == DrawCameraMode.Mipmaps)
            {
                // show mip levels
                if (!s_ShowMipsShader)
                    s_ShowMipsShader = EditorGUIUtility.LoadRequired("SceneView/SceneViewShowMips.shader") as Shader;
                if (s_ShowMipsShader != null && s_ShowMipsShader.isSupported)
                {
                    CreateMipColorsTexture();
                    m_Camera.SetReplacementShader(s_ShowMipsShader, "RenderType");
                }
                else
                {
                    m_Camera.SetReplacementShader(m_ReplacementShader, m_ReplacementString);
                }
            }
            /*
            else if (m_RenderMode == DrawCameraMode.Lightmaps)
            {
                // show lightmaps
                if (!s_ShowLightmapsShader)
                    s_ShowLightmapsShader = EditorGUIUtility.LoadRequired ("SceneView/SceneViewShowLightmap.shader") as Shader;
                if (s_ShowLightmapsShader.isSupported)
                    m_Camera.SetReplacementShader (s_ShowLightmapsShader, "RenderType");
                else
                    m_Camera.SetReplacementShader (m_ReplacementShader, m_ReplacementString);
            }*/
            else
            {
                m_Camera.SetReplacementShader(m_ReplacementShader, m_ReplacementString);
            }
        }

        bool SceneCameraRendersIntoRT()
        {
            return m_Camera.targetTexture != null;
        }

        private void DoDrawCamera(Rect cameraRect, out bool pushedGUIClip)
        {
            pushedGUIClip = false;
            if (!m_Camera.gameObject.activeInHierarchy)
                return;

            DrawGridParameters gridParam = grid.PrepareGridRender(camera, pivot, m_Rotation.target, m_Size.value, m_Ortho.target, drawGlobalGrid);

            Event evt = Event.current;
            if (UseSceneFiltering())
            {
                if (evt.type == EventType.Repaint)
                {
                    // First pass: Draw objects which do not meet the search filter with grayscale image effect.
                    Handles.EnableCameraFx(m_Camera, true);

                    Handles.SetCameraFilterMode(m_Camera, Handles.FilterMode.ShowRest);

                    float fade = Mathf.Clamp01((float)(EditorApplication.timeSinceStartup - m_StartSearchFilterTime));
                    Handles.DrawCamera(cameraRect, m_Camera, m_RenderMode);
                    Handles.DrawCameraFade(m_Camera, fade);

                    // Second pass: Draw aura for objects which do meet search filter, but are occluded.
                    Handles.EnableCameraFx(m_Camera, false);
                    Handles.SetCameraFilterMode(m_Camera, Handles.FilterMode.ShowFiltered);
                    if (!s_AuraShader)
                        s_AuraShader = EditorGUIUtility.LoadRequired("SceneView/SceneViewAura.shader") as Shader;
                    m_Camera.SetReplacementShader(s_AuraShader, "");
                    Handles.DrawCamera(cameraRect, m_Camera, m_RenderMode);

                    // Third pass: Draw objects which do meet filter normally
                    m_Camera.SetReplacementShader(m_ReplacementShader, m_ReplacementString);
                    Handles.DrawCamera(cameraRect, m_Camera, m_RenderMode, gridParam);

                    if (fade < 1)
                        Repaint();
                }
                Rect r = cameraRect;
                if (evt.type == EventType.Repaint)
                    RenderTexture.active = null;
                GUI.EndGroup();
                GUI.BeginGroup(new Rect(0, EditorGUI.kWindowToolbarHeight, position.width, position.height - EditorGUI.kWindowToolbarHeight));
                if (evt.type == EventType.Repaint)
                {
                    GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
                    Graphics.DrawTexture(r, m_SceneTargetTexture, new Rect(0, 0, 1, 1), 0, 0, 0, 0, GUI.color, EditorGUIUtility.GUITextureBlitColorspaceMaterial);
                    GL.sRGBWrite = false;
                }
                Handles.SetCamera(cameraRect, m_Camera);

                HandleSelectionAndOnSceneGUI();
            }
            else
            {
                // If the camera is rendering into a Render Texture we need to reset the offsets of the GUIClip stack
                // otherwise all GUI drawing after here will get offset incorrectly.
                if (SceneCameraRendersIntoRT())
                {
                    GUIClip.Push(new Rect(0f, 0f, position.width, position.height), Vector2.zero, Vector2.zero, true);
                    pushedGUIClip = true;
                }
                Handles.DrawCameraStep1(cameraRect, m_Camera, m_RenderMode, gridParam);
                DrawRenderModeOverlay(cameraRect);
            }
        }

        void SetupPBRValidation()
        {
            if (m_RenderMode == DrawCameraMode.ValidateAlbedo)
            {
                CreateAlbedoSwatchData();
                UpdateAlbedoSwatch();
            }

            if ((m_RenderMode == DrawCameraMode.ValidateAlbedo || m_RenderMode == DrawCameraMode.ValidateMetalSpecular) &&
                lastRenderMode != DrawCameraMode.ValidateAlbedo && lastRenderMode != DrawCameraMode.ValidateMetalSpecular)
            {
                SceneView.onSceneGUIDelegate += DrawValidateAlbedoSwatches;
            }
            else if ((m_RenderMode != DrawCameraMode.ValidateAlbedo && m_RenderMode != DrawCameraMode.ValidateMetalSpecular) &&
                     (lastRenderMode == DrawCameraMode.ValidateAlbedo || lastRenderMode == DrawCameraMode.ValidateMetalSpecular))
            {
                SceneView.onSceneGUIDelegate -= DrawValidateAlbedoSwatches;
            }

            lastRenderMode = m_RenderMode;
        }

        void DoClearCamera(Rect cameraRect)
        {
            // Clear (color/skybox)
            // We do funky FOV interpolation when switching between ortho and perspective. However,
            // for the skybox we always want to use the same FOV.
            float skyboxFOV = GetVerticalFOV(kPerspectiveFov);
            float realFOV = m_Camera.fieldOfView;
            m_Camera.fieldOfView = skyboxFOV;
            Handles.ClearCamera(cameraRect, m_Camera);
            m_Camera.fieldOfView = realFOV;
        }

        void SetupCustomSceneLighting()
        {
            if (m_SceneLighting)
                return;
            m_Light[0].transform.rotation = m_Camera.transform.rotation;
            if (Event.current.type == EventType.Repaint)
                InternalEditorUtility.SetCustomLighting(m_Light, kSceneViewMidLight);
        }

        void CleanupCustomSceneLighting()
        {
            if (m_SceneLighting)
                return;
            if (Event.current.type == EventType.Repaint)
                InternalEditorUtility.RemoveCustomLighting();
        }

        // Give editors a chance to kick in. Disable in search mode, editors rendering to the scene
        void HandleViewToolCursor()
        {
            if (!Tools.viewToolActive || Event.current.type != EventType.Repaint)
                return;

            var cursor = MouseCursor.Arrow;
            switch (Tools.viewTool)
            {
                case ViewTool.Pan: cursor = MouseCursor.Pan; break;
                case ViewTool.Orbit: cursor = MouseCursor.Orbit; break;
                case ViewTool.FPS: cursor = MouseCursor.FPS; break;
                case ViewTool.Zoom: cursor = MouseCursor.Zoom; break;
            }
            if (cursor != MouseCursor.Arrow)
                AddCursorRect(new Rect(0, EditorGUI.kWindowToolbarHeight, position.width, position.height - EditorGUI.kWindowToolbarHeight), cursor);
        }

        private static bool ComponentHasImageEffectAttribute(Component c)
        {
            if (c == null)
                return false;
            return Attribute.IsDefined(c.GetType(), typeof(ImageEffectAllowedInSceneView));
        }

        void UpdateImageEffects(bool enable)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            Camera mainCam = GetMainCamera();
            if (!enable || mainCam == null)
            {
                ComponentUtility.DestroyComponentsMatching(m_Camera.gameObject, ComponentHasImageEffectAttribute);
                return;
            }

            ComponentUtility.ReplaceComponentsIfDifferent(mainCam.gameObject, m_Camera.gameObject, ComponentHasImageEffectAttribute);
        }

        void DoOnPreSceneGUICallbacks(Rect cameraRect)
        {
            // Don't do callbacks in search mode, as editors calling Handles.BeginGUI
            // will break camera setup.
            if (UseSceneFiltering())
                return;

            Handles.SetCamera(cameraRect, m_Camera);
            CallOnPreSceneGUI();
        }

        private AlbedoSwatchInfo[] m_AlbedoSwatchInfos;
        private GUIStyle[] m_AlbedoSwatchColorStyles;
        private String[] m_AlbedoSwatchDescriptions;
        private GUIContent[] m_AlbedoSwatchGUIContent;
        private String[] m_AlbedoSwatchLuminanceStrings;

        private GUIStyle m_TooLowColorStyle = null;
        private GUIStyle m_TooHighColorStyle = null;
        private GUIStyle m_PureMetalColorStyle = null;

        private int m_SelectedAlbedoSwatchIndex = 0;
        private float m_AlbedoSwatchHueTolerance = 0.1f;
        private float m_AlbedoSwatchSaturationTolerance = 0.2f;
        private ColorSpace m_LastKnownColorSpace = ColorSpace.Uninitialized;

        GUIStyle CreateSwatchStyleForColor(Color c)
        {
            Texture2D t = new Texture2D(1, 1);
            if (PlayerSettings.colorSpace == ColorSpace.Linear)
            {
                c = c.gamma; // offset linear to gamma correction that happens in IMGUI, by doing the inverse beforehand
            }
            t.SetPixel(0, 0, c);
            t.Apply();
            GUIStyle s = new GUIStyle();
            s.normal.background = t;
            return s;
        }

        String CreateSwatchDescriptionForName(float minLum, float maxLum)
        {
            return "Luminance (" + minLum.ToString("F2") + " - " + maxLum.ToString("F2") + ")";
        }

        void CreateAlbedoSwatchData()
        {
            AlbedoSwatchInfo[] graphicsSettingsSwatches = EditorGraphicsSettings.albedoSwatches;

            if (graphicsSettingsSwatches.Length != 0)
            {
                m_AlbedoSwatchInfos = graphicsSettingsSwatches;
            }
            else
            {
                m_AlbedoSwatchInfos = new AlbedoSwatchInfo[]
                {
                    // colors taken from http://www.babelcolor.com/index_htm_files/ColorChecker_RGB_and_spectra.xls
                    new AlbedoSwatchInfo()
                    {
                        name = "Black Acrylic Paint",
                        color = new Color(56f / 255f, 56f / 255f, 56f / 255f),
                        minLuminance = 0.03f,
                        maxLuminance = 0.07f
                    },
                    new AlbedoSwatchInfo()
                    {
                        name = "Dark Soil",
                        color = new Color(85f / 255f, 61f / 255f, 49f / 255f),
                        minLuminance = 0.05f,
                        maxLuminance = 0.14f
                    },

                    new AlbedoSwatchInfo()
                    {
                        name = "Worn Asphalt",
                        color = new Color(91f / 255f, 91f / 255f, 91f / 255f),
                        minLuminance = 0.10f,
                        maxLuminance = 0.15f
                    },
                    new AlbedoSwatchInfo()
                    {
                        name = "Dry Clay Soil",
                        color = new Color(137f / 255f, 120f / 255f, 102f / 255f),
                        minLuminance = 0.15f,
                        maxLuminance = 0.35f
                    },
                    new AlbedoSwatchInfo()
                    {
                        name = "Green Grass",
                        color = new Color(123f / 255f, 131f / 255f, 74f / 255f),
                        minLuminance = 0.16f,
                        maxLuminance = 0.26f
                    },
                    new AlbedoSwatchInfo()
                    {
                        name = "Old Concrete",
                        color = new Color(135f / 255f, 136f / 255f, 131f / 255f),
                        minLuminance = 0.17f,
                        maxLuminance = 0.30f
                    },
                    new AlbedoSwatchInfo()
                    {
                        name = "Red Clay Tile",
                        color = new Color(197f / 255f, 125f / 255f, 100f / 255f),
                        minLuminance = 0.23f,
                        maxLuminance = 0.33f
                    },
                    new AlbedoSwatchInfo()
                    {
                        name = "Dry Sand",
                        color = new Color(177f / 255f, 167f / 255f, 132f / 255f),
                        minLuminance = 0.20f,
                        maxLuminance = 0.45f
                    },
                    new AlbedoSwatchInfo()
                    {
                        name = "New Concrete",
                        color = new Color(185f / 255f, 182f / 255f, 175f / 255f),
                        minLuminance = 0.32f,
                        maxLuminance = 0.55f
                    },
                    new AlbedoSwatchInfo()
                    {
                        name = "White Acrylic Paint",
                        color = new Color(227f / 255f, 227f / 255f, 227f / 255f),
                        minLuminance = 0.75f,
                        maxLuminance = 0.85f
                    },
                    new AlbedoSwatchInfo()
                    {
                        name = "Fresh Snow",
                        color = new Color(243f / 255f, 243f / 255f, 243f / 255f),
                        minLuminance = 0.85f,
                        maxLuminance = 0.95f
                    },
                    new AlbedoSwatchInfo()
                    {
                        name = "Blue Sky",
                        color = new Color(93f / 255f, 123f / 255f, 157f / 255f),
                        minLuminance = new Color(93f / 255f, 123f / 255f, 157f / 255f).linear.maxColorComponent - 0.05f,
                        maxLuminance = new Color(93f / 255f, 123f / 255f, 157f / 255f).linear.maxColorComponent + 0.05f
                    },
                    new AlbedoSwatchInfo()
                    {
                        name = "Foliage",
                        color = new Color(91f / 255f, 108f / 255f, 65f / 255f),
                        minLuminance = new Color(91f / 255f, 108f / 255f, 65f / 255f).linear.maxColorComponent - 0.05f,
                        maxLuminance = new Color(91f / 255f, 108f / 255f, 65f / 255f).linear.maxColorComponent + 0.05f
                    },
                };
            }
            UpdateAlbedoSwatchGUI();
        }

        void UpdateAlbedoSwatchGUI()
        {
            m_LastKnownColorSpace = PlayerSettings.colorSpace;
            m_AlbedoSwatchColorStyles = new GUIStyle[m_AlbedoSwatchInfos.Length + 1];
            m_AlbedoSwatchGUIContent = new GUIContent[m_AlbedoSwatchInfos.Length + 1];
            m_AlbedoSwatchDescriptions = new String[m_AlbedoSwatchInfos.Length + 1];
            m_AlbedoSwatchLuminanceStrings = new String[m_AlbedoSwatchInfos.Length + 1];

            m_AlbedoSwatchColorStyles[0] = CreateSwatchStyleForColor(Color.gray);
            m_AlbedoSwatchDescriptions[0] = "Default Luminance";
            m_AlbedoSwatchGUIContent[0] = new GUIContent(m_AlbedoSwatchDescriptions[0]);
            m_AlbedoSwatchLuminanceStrings[0] = CreateSwatchDescriptionForName(0.012f, 0.9f);
            for (int i = 1; i < m_AlbedoSwatchInfos.Length + 1; i++)
            {
                m_AlbedoSwatchColorStyles[i] = CreateSwatchStyleForColor(m_AlbedoSwatchInfos[i - 1].color);
                m_AlbedoSwatchDescriptions[i] =  m_AlbedoSwatchInfos[i - 1].name;
                m_AlbedoSwatchGUIContent[i] = new GUIContent(m_AlbedoSwatchDescriptions[i]);
                m_AlbedoSwatchLuminanceStrings[i] = CreateSwatchDescriptionForName(m_AlbedoSwatchInfos[i - 1].minLuminance, m_AlbedoSwatchInfos[i - 1].maxLuminance);
            }
        }

        void UpdatePBRColorLegend()
        {
            m_TooLowColorStyle = CreateSwatchStyleForColor(kSceneViewMaterialValidateLow.Color);
            m_TooHighColorStyle = CreateSwatchStyleForColor(kSceneViewMaterialValidateHigh.Color);
            m_PureMetalColorStyle = CreateSwatchStyleForColor(kSceneViewMaterialValidatePureMetal.Color);
            Shader.SetGlobalColor("unity_MaterialValidateLowColor", kSceneViewMaterialValidateLow.Color.linear);
            Shader.SetGlobalColor("unity_MaterialValidateHighColor", kSceneViewMaterialValidateHigh.Color.linear);
            Shader.SetGlobalColor("unity_MaterialValidatePureMetalColor", kSceneViewMaterialValidatePureMetal.Color.linear);
        }

        void UpdateAlbedoSwatch()
        {
            Color color = Color.gray;
            if (m_SelectedAlbedoSwatchIndex != 0)
            {
                color = m_AlbedoSwatchInfos[m_SelectedAlbedoSwatchIndex - 1].color;
                Shader.SetGlobalFloat("_AlbedoMinLuminance", m_AlbedoSwatchInfos[m_SelectedAlbedoSwatchIndex - 1].minLuminance);
                Shader.SetGlobalFloat("_AlbedoMaxLuminance", m_AlbedoSwatchInfos[m_SelectedAlbedoSwatchIndex - 1].maxLuminance);
                Shader.SetGlobalFloat("_AlbedoHueTolerance", m_AlbedoSwatchHueTolerance);
                Shader.SetGlobalFloat("_AlbedoSaturationTolerance", m_AlbedoSwatchSaturationTolerance);
            }
            Shader.SetGlobalColor("_AlbedoCompareColor", color.linear);
            Shader.SetGlobalInt("_CheckAlbedo", (m_SelectedAlbedoSwatchIndex != 0) ? 1 : 0);
            Shader.SetGlobalInt("_CheckPureMetal", m_ValidateTrueMetals ? 1 : 0);
        }

        internal void DrawTrueMetalCheckbox()
        {
            EditorGUI.BeginChangeCheck();
            m_ValidateTrueMetals = EditorGUILayout.ToggleLeft(new GUIContent("Check Pure Metals", "Check if albedo is black for materials with an average specular color above 0.45"), m_ValidateTrueMetals);
            if (EditorGUI.EndChangeCheck())
            {
                Shader.SetGlobalInt("_CheckPureMetal", m_ValidateTrueMetals ? 1 : 0);
            }
        }

        internal void DrawPBRSettingsForScene()
        {
            if (m_RenderMode == DrawCameraMode.ValidateAlbedo)
            {
                if (PlayerSettings.colorSpace == ColorSpace.Gamma)
                {
                    EditorGUILayout.HelpBox("Albedo Validation doesn't work when Color Space is set to gamma space", MessageType.Warning);
                }

                EditorGUIUtility.labelWidth = 140;

                m_SelectedAlbedoSwatchIndex = EditorGUILayout.Popup(new GUIContent("Luminance Validation:", "Select default luminance validation or validate against a configured albedo swatch"), m_SelectedAlbedoSwatchIndex, m_AlbedoSwatchGUIContent);
                EditorGUI.indentLevel++;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUIUtility.labelWidth = 5;
                    EditorGUILayout.LabelField(" ", m_AlbedoSwatchColorStyles[m_SelectedAlbedoSwatchIndex]);
                    EditorGUIUtility.labelWidth = 140;
                    EditorGUILayout.LabelField(m_AlbedoSwatchLuminanceStrings[m_SelectedAlbedoSwatchIndex]);
                }

                UpdateAlbedoSwatch();

                EditorGUI.indentLevel--;
                using (new EditorGUI.DisabledScope(m_SelectedAlbedoSwatchIndex == 0))
                {
                    EditorGUI.BeginChangeCheck();
                    using (new EditorGUI.DisabledScope(m_SelectedAlbedoSwatchIndex == 0))
                    {
                        m_AlbedoSwatchHueTolerance = EditorGUILayout.Slider(new GUIContent("Hue Tolerance:", "Check that the hue of the albedo value of a material is within the tolerance of the hue of the albedo swatch being validated against"), m_AlbedoSwatchHueTolerance, 0f, 0.5f);

                        m_AlbedoSwatchSaturationTolerance = EditorGUILayout.Slider(new GUIContent("Saturation Tolerance:", "Check that the saturation of the albedo value of a material is within the tolerance of the saturation of the albedo swatch being validated against"), m_AlbedoSwatchSaturationTolerance, 0f, 0.5f);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        UpdateAlbedoSwatch();
                    }
                }
            }

            UpdatePBRColorLegend();
            EditorGUILayout.LabelField("Color Legend:");
            EditorGUI.indentLevel++;
            string modeString;

            if (m_RenderMode == DrawCameraMode.ValidateAlbedo)
            {
                modeString = "Luminance";
            }
            else
            {
                modeString = "Specular";
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUIUtility.labelWidth = 2;
                EditorGUILayout.LabelField("", m_TooLowColorStyle);
                EditorGUIUtility.labelWidth = 200;
                EditorGUILayout.LabelField("Below Minimum " + modeString + " Value");
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUIUtility.labelWidth = 2;
                EditorGUILayout.LabelField("", m_TooHighColorStyle);
                EditorGUIUtility.labelWidth = 200;
                EditorGUILayout.LabelField("Above Maximum " + modeString + " Value");
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUIUtility.labelWidth = 2;
                EditorGUILayout.LabelField("", m_PureMetalColorStyle);
                EditorGUIUtility.labelWidth = 200;
                EditorGUILayout.LabelField("Not A Pure Metal");
            }
        }

        internal void PrepareValidationUI()
        {
            if (m_AlbedoSwatchInfos == null)
                CreateAlbedoSwatchData();

            if (PlayerSettings.colorSpace != m_LastKnownColorSpace)
            {
                UpdateAlbedoSwatchGUI();
                UpdateAlbedoSwatch();
            }
        }

        static void DrawPBRSettings(Object target, SceneView sceneView)
        {
            sceneView.DrawTrueMetalCheckbox();
            sceneView.DrawPBRSettingsForScene();
        }

        void DrawValidateAlbedoSwatches(SceneView sceneView)
        {
            if (sceneView.m_RenderMode == DrawCameraMode.ValidateAlbedo || sceneView.m_RenderMode == DrawCameraMode.ValidateMetalSpecular)
            {
                sceneView.PrepareValidationUI();
                SceneViewOverlay.Window(new GUIContent("PBR Validation Settings"), DrawPBRSettings, (int)SceneViewOverlay.Ordering.PhysicsDebug, sceneView, SceneViewOverlay.WindowDisplayOption.OneWindowPerTarget);
            }
        }

        void RepaintGizmosThatAreRenderedOnTopOfSceneView()
        {
            svRot.OnGUI(this);
        }

        void InputForGizmosThatAreRenderedOnTopOfSceneView()
        {
            if (Event.current.type != EventType.Repaint)
            {
                svRot.OnGUI(this);
            }
        }

        internal void OnGUI()
        {
            s_CurrentDrawingSceneView = this;

            Event evt = Event.current;
            if (evt.type == EventType.Repaint)
            {
                s_MouseRects.Clear();
                Profiler.BeginSample("SceneView.Repaint");
            }

            Color origColor = GUI.color;
            Rect origCameraRect = m_Camera.rect;

            HandleClickAndDragToFocus();

            if (evt.type == EventType.Layout)
                m_ShowSceneViewWindows = (lastActiveSceneView == this);

            m_SceneViewOverlay.Begin();

            bool oldFog;
            float oldShadowDistance;
            SetupFogAndShadowDistance(out oldFog, out oldShadowDistance);

            DoToolbarGUI();
            GUI.skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);

            // Don't apply any playmode tinting to scene views
            GUI.color = Color.white;

            EditorGUIUtility.labelWidth = 100;

            SetupCamera();
            RenderingPath oldRenderingPath = m_Camera.renderingPath;

            SetupCustomSceneLighting();

            GUI.BeginGroup(new Rect(0, EditorGUI.kWindowToolbarHeight, position.width, position.height - EditorGUI.kWindowToolbarHeight));
            Rect guiRect = new Rect(0, 0, position.width, (position.height - EditorGUI.kWindowToolbarHeight));
            Rect cameraRect = EditorGUIUtility.PointsToPixels(guiRect);

            HandleViewToolCursor();

            PrepareCameraTargetTexture(cameraRect);
            DoClearCamera(cameraRect);

            m_Camera.cullingMask = Tools.visibleLayers;

            InputForGizmosThatAreRenderedOnTopOfSceneView();

            DoOnPreSceneGUICallbacks(cameraRect);

            PrepareCameraReplacementShader();

            // Unfocus search field on mouse clicks into content, so that key presses work to navigate.
            m_MainViewControlID = GUIUtility.GetControlID(FocusType.Keyboard);
            if (evt.GetTypeForControl(m_MainViewControlID) == EventType.MouseDown)
                GUIUtility.keyboardControl = m_MainViewControlID;

            // Draw camera
            bool pushedGUIClip;
            DoDrawCamera(guiRect, out pushedGUIClip);

            CleanupCustomSceneLighting();


            //Ensure that the target texture is clamped [0-1]
            //This is needed because otherwise gizmo rendering gets all
            //messed up (think HDR target with value of 50 + alpha blend gizmo... gonna be white!)
            var ldrSceneTargetTexture = m_SceneTargetTexture;
            if (!UseSceneFiltering() && evt.type == EventType.Repaint && RenderTextureEditor.IsHDRFormat(m_SceneTargetTexture.format))
            {
                var rtDesc = m_SceneTargetTexture.descriptor;
                rtDesc.colorFormat = RenderTextureFormat.ARGB32;
                rtDesc.depthBufferBits = 0;
                ldrSceneTargetTexture = RenderTexture.GetTemporary(rtDesc);
                Graphics.Blit(m_SceneTargetTexture, ldrSceneTargetTexture);
                Graphics.SetRenderTarget(ldrSceneTargetTexture.colorBuffer, m_SceneTargetTexture.depthBuffer);
            }

            if (!UseSceneFiltering())
            {
                // Blit to final target RT in deferred mode
                if (m_Camera.gameObject.activeInHierarchy)
                    Handles.DrawCameraStep2(m_Camera, m_RenderMode);

                // Give editors a chance to kick in. Disable in search mode, editors rendering to the scene
                // view won't be able to properly render to the rendertexture as needed.
                // Calling OnSceneGUI before DefaultHandles, so users can use events before the Default Handles
                bool sRGBWriteOld = GL.sRGBWrite;
                GL.sRGBWrite = false;
                HandleSelectionAndOnSceneGUI();
                GL.sRGBWrite = sRGBWriteOld;
            }

            // Handle commands
            if (evt.type == EventType.ExecuteCommand || evt.type == EventType.ValidateCommand)
                CommandsGUI();

            RestoreFogAndShadowDistance(oldFog, oldShadowDistance);

            m_Camera.renderingPath = oldRenderingPath;

            if (UseSceneFiltering())
                Handles.SetCameraFilterMode(Camera.current, Handles.FilterMode.ShowFiltered);
            else
                Handles.SetCameraFilterMode(Camera.current, Handles.FilterMode.Off);

            // Draw default scene manipulation tools (Move/Rotate/...)
            {
                bool sRGBWriteOld = GL.sRGBWrite;
                GL.sRGBWrite = false;
                DefaultHandles();
                GL.sRGBWrite = sRGBWriteOld;
            }

            if (!UseSceneFiltering())
            {
                if (evt.type == EventType.Repaint)
                {
                    Profiler.BeginSample("SceneView.BlitRT");
                    Graphics.SetRenderTarget(null);
                }
                // If we reset the offsets pop that clip off now.
                if (pushedGUIClip)
                    GUIClip.Pop();
                if (evt.type == EventType.Repaint)
                {
                    GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
                    Graphics.DrawTexture(guiRect, ldrSceneTargetTexture, new Rect(0, 0, 1, 1), 0, 0, 0, 0, GUI.color, EditorGUIUtility.GUITextureBlitColorspaceMaterial);
                    if (RenderTextureEditor.IsHDRFormat(m_SceneTargetTexture.format))
                        RenderTexture.ReleaseTemporary(ldrSceneTargetTexture);
                    GL.sRGBWrite = false;
                    Profiler.EndSample();
                }
            }

            Handles.SetCameraFilterMode(Camera.current, Handles.FilterMode.Off);
            Handles.SetCameraFilterMode(m_Camera, Handles.FilterMode.Off);

            // Handle Dragging of stuff over scene view
            HandleDragging();

            RepaintGizmosThatAreRenderedOnTopOfSceneView();

            // Handle scene view motion when this scene view is active
            if (s_LastActiveSceneView == this)
            {
                // Do not pass the camera transform to the SceneViewMotion calculations.
                // The camera transform is calculation *output* not *input*.
                // Avoiding using it as input too avoids errors accumulating.
                SceneViewMotion.ArrowKeys(this);
                SceneViewMotion.DoViewTool(this);
            }

            Handle2DModeSwitch();

            GUI.EndGroup();
            GUI.color = origColor;

            m_SceneViewOverlay.End();

            HandleMouseCursor();

            if (evt.type == EventType.Repaint)
            {
                Profiler.EndSample();
            }

            s_CurrentDrawingSceneView = null;
            m_Camera.rect = origCameraRect;
        }

        void Handle2DModeSwitch()
        {
            Event evt = Event.current;
            if (k2DMode.activated && !waitingFor2DModeKeyUp)
            {
                waitingFor2DModeKeyUp = true;
                in2DMode = !in2DMode;
                evt.Use();
            }
            else
            {
                if (evt.type == EventType.KeyUp && evt.keyCode == k2DMode.KeyboardEvent.keyCode)
                    waitingFor2DModeKeyUp = false;
            }
        }

        void HandleMouseCursor()
        {
            Event evt = Event.current;
            if (GUIUtility.hotControl == 0)
                s_DraggingCursorIsCached = false;
            Rect cursorRect = new Rect(0, 0, position.width, position.height);
            if (!s_DraggingCursorIsCached)
            {
                // Determine if mouse is inside a new cursor rect
                MouseCursor cursor = MouseCursor.Arrow;
                if (evt.type == EventType.MouseMove || evt.type == EventType.Repaint)
                {
                    foreach (CursorRect r in s_MouseRects)
                    {
                        if (r.rect.Contains(evt.mousePosition))
                        {
                            cursor = r.cursor;
                            cursorRect = r.rect;
                        }
                    }
                    if (GUIUtility.hotControl != 0)
                        s_DraggingCursorIsCached = true;
                    if (cursor != s_LastCursor)
                    {
                        s_LastCursor = cursor;
                        InternalEditorUtility.ResetCursor();
                        Repaint();
                    }
                }
            }
            // Apply the one relevant cursor rect
            if (evt.type == EventType.Repaint && s_LastCursor != MouseCursor.Arrow)
            {
                EditorGUIUtility.AddCursorRect(cursorRect, s_LastCursor);
                // GUI.color = Color.magenta; GUI.Box (rect, ""); EditorGUI.DropShadowLabel (rect, ""+s_LastCursor); GUI.color = Color.white;
            }
        }

        void DrawRenderModeOverlay(Rect cameraRect)
        {
            // show destination alpha channel
            if (m_RenderMode == DrawCameraMode.AlphaChannel)
            {
                if (!s_AlphaOverlayMaterial)
                    s_AlphaOverlayMaterial = EditorGUIUtility.LoadRequired("SceneView/SceneViewAlphaMaterial.mat") as Material;
                Handles.BeginGUI();
                if (Event.current.type == EventType.Repaint)
                    Graphics.DrawTexture(cameraRect, EditorGUIUtility.whiteTexture, s_AlphaOverlayMaterial);
                Handles.EndGUI();
            }

            // show one of deferred buffers
            if (m_RenderMode == DrawCameraMode.DeferredDiffuse ||
                m_RenderMode == DrawCameraMode.DeferredSpecular ||
                m_RenderMode == DrawCameraMode.DeferredSmoothness ||
                m_RenderMode == DrawCameraMode.DeferredNormal)
            {
                if (!s_DeferredOverlayMaterial)
                    s_DeferredOverlayMaterial = EditorGUIUtility.LoadRequired("SceneView/SceneViewDeferredMaterial.mat") as Material;
                Handles.BeginGUI();
                if (Event.current.type == EventType.Repaint)
                {
                    s_DeferredOverlayMaterial.SetInt("_DisplayMode", (int)m_RenderMode - (int)DrawCameraMode.DeferredDiffuse);
                    Graphics.DrawTexture(cameraRect, EditorGUIUtility.whiteTexture, s_DeferredOverlayMaterial);
                }
                Handles.EndGUI();
            }
        }

        private void HandleSelectionAndOnSceneGUI()
        {
            m_RectSelection.OnGUI();
            CallOnSceneGUI();
        }

        /// Center point of the scene view. Modify it to move the sceneview immediately, or use LookAt to animate it nicely.
        public Vector3 pivot { get { return m_Position.value; } set { m_Position.value = value; } }

        /// The direction of the scene view.
        public Quaternion rotation { get { return m_Rotation.value; } set { m_Rotation.value = value; } }

        public float size
        {
            get { return m_Size.value; }
            set
            {
                if (value > 40000f)
                    value = 40000;
                m_Size.value = value;
            }
        }


        /// Is the scene view ortho.
        public bool orthographic { get { return m_Ortho.value; } set {m_Ortho.value = value; } }

        public void FixNegativeSize()
        {
            float fov = kPerspectiveFov;
            if (size < 0)
            {
                float distance = size / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
                Vector3 p = m_Position.value + rotation * new Vector3(0, 0, -distance);
                size = -size;
                distance = size / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
                m_Position.value = p + rotation * new Vector3(0, 0, distance);
            }
        }

        float CalcCameraDist()
        {
            float fov = m_Ortho.Fade(kPerspectiveFov, 0);
            if (fov > kOrthoThresholdAngle)
            {
                m_Camera.orthographic = false;
                return size / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            }
            return 0;
        }

        void ResetIfNaN()
        {
            // If you zoom out enough, m_Position would get corrupted with no way to reset it,
            // even after restarting Unity. Crude hack to at least get the scene view working again!
            if (Single.IsInfinity(m_Position.value.x) || Single.IsNaN(m_Position.value.x))
                m_Position.value = Vector3.zero;
            if (Single.IsInfinity(m_Rotation.value.x) || Single.IsNaN(m_Rotation.value.x))
                m_Rotation.value = Quaternion.identity;
        }

        static internal Camera GetMainCamera()
        {
            // main camera, if we have any
            var mainCamera = Camera.main;
            if (mainCamera != null)
                return mainCamera;

            // if we have one camera, return it
            Camera[] allCameras = Camera.allCameras;
            if (allCameras != null && allCameras.Length == 1)
                return allCameras[0];

            // otherwise no "main" camera
            return null;
        }

        // Note: this can return "use player settings" value too!
        // In order to check things like "is using deferred", use IsUsingDeferredRenderingPath
        static internal RenderingPath GetSceneViewRenderingPath()
        {
            var mainCamera = GetMainCamera();
            if (mainCamera != null)
                return mainCamera.renderingPath;
            return RenderingPath.UsePlayerSettings;
        }

        static internal bool IsUsingDeferredRenderingPath()
        {
            RenderingPath renderingPath = GetSceneViewRenderingPath();
            return (renderingPath == RenderingPath.DeferredShading) ||
                (renderingPath == RenderingPath.UsePlayerSettings && Rendering.EditorGraphicsSettings.GetCurrentTierSettings().renderingPath == RenderingPath.DeferredShading);
        }

        internal bool CheckDrawModeForRenderingPath(DrawCameraMode mode)
        {
            RenderingPath path = m_Camera.actualRenderingPath;
            if (mode == DrawCameraMode.DeferredDiffuse ||
                mode == DrawCameraMode.DeferredSpecular ||
                mode == DrawCameraMode.DeferredSmoothness ||
                mode == DrawCameraMode.DeferredNormal)
            {
                return path == RenderingPath.DeferredShading;
            }
            return true;
        }

        private void SetSceneCameraHDRAndDepthModes()
        {
            if (!m_SceneLighting || !DoesCameraDrawModeSupportHDR(m_RenderMode))
            {
                m_Camera.allowHDR = false;
                m_Camera.depthTextureMode = DepthTextureMode.None;
                m_Camera.clearStencilAfterLightingPass = false;
                return;
            }
            var mainCamera = GetMainCamera();
            if (mainCamera == null)
            {
                m_Camera.allowHDR = false;
                m_Camera.depthTextureMode = DepthTextureMode.None;
                m_Camera.clearStencilAfterLightingPass = false;
                return;
            }
            m_Camera.allowHDR = mainCamera.allowHDR;
            m_Camera.depthTextureMode = mainCamera.depthTextureMode;
            m_Camera.clearStencilAfterLightingPass = mainCamera.clearStencilAfterLightingPass;
        }

        void SetupCamera()
        {
            if (m_RenderMode == DrawCameraMode.Overdraw)
            {
                // overdraw
                m_Camera.backgroundColor = Color.black;
            }
            else
            {
                m_Camera.backgroundColor = kSceneViewBackground;
            }

            if (Event.current.type == EventType.Repaint)
            {
                UpdateImageEffects(UseSceneFiltering() ? false : m_RenderMode == DrawCameraMode.Textured && sceneViewState.showImageEffects);
            }

            EditorUtility.SetCameraAnimateMaterials(m_Camera, sceneViewState.showMaterialUpdate);
            ParticleSystemEditorUtils.editorRenderInSceneView = m_SceneViewState.showParticleSystems;

            ResetIfNaN();

            m_Camera.transform.rotation = m_Rotation.value;

            float fov = m_Ortho.Fade(kPerspectiveFov, 0);
            if (fov > kOrthoThresholdAngle)
            {
                m_Camera.orthographic = false;

                // Old calculations were strange and were more zoomed in for tall aspect ratios than for wide ones.
                //m_Camera.fieldOfView = Mathf.Sqrt((fov * fov) / (1 + aspect));
                // 1:1: Sqrt((90*90) / (1+1))   = 63.63 degrees = atan(0.6204)  -  means we have 0.6204 x 0.6204 in tangents
                // 2:1: Sqrt((90*90) / (1+2))   = 51.96 degrees = atan(0.4873)  -  means we have 0.9746 x 0.4873 in tangents
                // 1:2: Sqrt((90*90) / (1+0.5)) = 73.48 degrees = atan(0.7465)  -  means we have 0.3732 x 0.7465 in tangents - 25% more zoomed in!

                m_Camera.fieldOfView = GetVerticalFOV(fov);
            }
            else
            {
                m_Camera.orthographic = true;

                //m_Camera.orthographicSize = Mathf.Sqrt((size * size) / (1 + aspect));
                m_Camera.orthographicSize = GetVerticalOrthoSize();
            }
            m_Camera.transform.position = m_Position.value + m_Camera.transform.rotation * new Vector3(0, 0, -cameraDistance);

            float farClip = Mathf.Max(1000f, 2000f * size);
            m_Camera.nearClipPlane = farClip * 0.000005f;
            m_Camera.farClipPlane = farClip;

            m_Camera.renderingPath = GetSceneViewRenderingPath();
            if (!CheckDrawModeForRenderingPath(m_RenderMode))
                m_RenderMode = DrawCameraMode.Textured;
            SetSceneCameraHDRAndDepthModes();

            if (m_RenderMode == DrawCameraMode.Textured || m_RenderMode == DrawCameraMode.TexturedWire)
            {
                Handles.EnableCameraFlares(m_Camera, sceneViewState.showFlares);
                Handles.EnableCameraSkybox(m_Camera, sceneViewState.showSkybox);
            }
            else
            {
                Handles.EnableCameraFlares(m_Camera, false);
                Handles.EnableCameraSkybox(m_Camera, false);
            }

            // Update the light
            m_Light[0].transform.position = m_Camera.transform.position;
            m_Light[0].transform.rotation = m_Camera.transform.rotation;

            // Update audio engine
            if (m_AudioPlay)
            {
                AudioUtil.SetListenerTransform(m_Camera.transform);
                AudioUtil.UpdateAudio();
            }

            if (m_ViewIsLockedToObject && Selection.gameObjects.Length > 0)
            {
                var bounds = InternalEditorUtility.CalculateSelectionBounds(false, Tools.pivotMode == PivotMode.Pivot);
                switch (draggingLocked)
                {
                    case (DraggingLockedState.Dragging):
                        // While dragging via handles, we don't want to move the camera
                        break;
                    case (DraggingLockedState.LookAt):
                        if (!m_Position.value.Equals(m_Position.target))
                            Frame(bounds, EditorApplication.isPlaying);
                        else
                            draggingLocked = DraggingLockedState.NotDragging;
                        break;
                    case (DraggingLockedState.NotDragging):
                        // Once framed, we only need to lock position rather than all the parameters Frame() sets
                        m_Position.value = bounds.center;
                        break;
                }
            }
        }

        void OnBecameVisible()
        {
            EditorApplication.update += UpdateAnimatedMaterials;
        }

        void OnBecameInvisible()
        {
            EditorApplication.update -= UpdateAnimatedMaterials;
        }

        void UpdateAnimatedMaterials()
        {
            if (sceneViewState.showMaterialUpdate && m_lastRenderedTime + 0.033f < EditorApplication.timeSinceStartup)
            {
                m_lastRenderedTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        internal Quaternion cameraTargetRotation { get { return m_Rotation.target; } }

        internal Vector3 cameraTargetPosition { get { return m_Position.target + m_Rotation.target * new Vector3(0, 0, cameraDistance); } }

        internal float GetVerticalFOV(float aspectNeutralFOV)
        {
            float verticalHalfFovTangent = Mathf.Tan(aspectNeutralFOV * 0.5f * Mathf.Deg2Rad) * kOneOverSqrt2 / Mathf.Sqrt(m_Camera.aspect);
            return Mathf.Atan(verticalHalfFovTangent) * 2 * Mathf.Rad2Deg;
        }

        internal float GetVerticalOrthoSize()
        {
            return size * kOneOverSqrt2 / Mathf.Sqrt(m_Camera.aspect);
        }

        // Look at a specific point.
        public void LookAt(Vector3 pos)
        {
            FixNegativeSize();
            m_Position.target = pos;
        }

        // Look at a specific point from a given direction.
        public void LookAt(Vector3 pos, Quaternion rot)
        {
            FixNegativeSize();
            m_Position.target = pos;
            m_Rotation.target = rot;
            // Update name in the top-right handle
            svRot.UpdateGizmoLabel(this, rot * Vector3.forward, m_Ortho.target);
        }

        // Look directly at a specific point from a given direction.
        public void LookAtDirect(Vector3 pos, Quaternion rot)
        {
            FixNegativeSize();
            m_Position.value = pos;
            m_Rotation.value = rot;
            // Update name in the top-right handle
            svRot.UpdateGizmoLabel(this, rot * Vector3.forward, m_Ortho.target);
        }

        // Look at a specific point from a given direction with a given zoom level.
        public void LookAt(Vector3 pos, Quaternion rot, float newSize)
        {
            FixNegativeSize();
            m_Position.target = pos;
            m_Rotation.target = rot;
            m_Size.target = Mathf.Abs(newSize);
            // Update name in the top-right handle
            svRot.UpdateGizmoLabel(this, rot * Vector3.forward, m_Ortho.target);
        }

        // Look directally at a specific point from a given direction with a given zoom level.
        public void LookAtDirect(Vector3 pos, Quaternion rot, float newSize)
        {
            FixNegativeSize();
            m_Position.value = pos;
            m_Rotation.value = rot;
            m_Size.value = Mathf.Abs(newSize);
            // Update name in the top-right handle
            svRot.UpdateGizmoLabel(this, rot * Vector3.forward, m_Ortho.target);
        }

        // Look at a specific point from a given direction with a given zoom level, enabling and disabling perspective
        public void LookAt(Vector3 pos, Quaternion rot, float newSize, bool ortho)
        {
            LookAt(pos, rot, newSize, ortho, false);
        }

        // Look at a specific point from a given direction with a given zoom level, enabling and disabling perspective
        public void LookAt(Vector3 pos, Quaternion rot, float newSize, bool ortho, bool instant)
        {
            FixNegativeSize();
            if (instant)
            {
                m_Position.value = pos;
                m_Rotation.value = rot;
                m_Size.value = Mathf.Abs(newSize);
                m_Ortho.value = ortho;
                draggingLocked = DraggingLockedState.NotDragging;
            }
            else
            {
                m_Position.target = pos;
                m_Rotation.target = rot;
                m_Size.target = Mathf.Abs(newSize);
                m_Ortho.target = ortho;
            }
            // Update name in the top-right handle
            svRot.UpdateGizmoLabel(this, rot * Vector3.forward, m_Ortho.target);
        }

        void DefaultHandles()
        {
            // Note event state.
            EditorGUI.BeginChangeCheck();
            bool IsDragEvent = Event.current.GetTypeForControl(GUIUtility.hotControl) == EventType.MouseDrag;
            bool IsMouseUpEvent = Event.current.GetTypeForControl(GUIUtility.hotControl) == EventType.MouseUp;

            // Only switch tools when we don't have a hot control.
            if (GUIUtility.hotControl == 0)
                s_CurrentTool = Tools.viewToolActive ? 0 : Tools.current;

            Tool tool = (Event.current.type == EventType.Repaint ? Tools.current : s_CurrentTool);

            switch (tool)
            {
                case Tool.None:
                case Tool.View:
                    break;
                case Tool.Move:
                    MoveTool.OnGUI(this);
                    break;
                case Tool.Rotate:
                    RotateTool.OnGUI(this);
                    break;
                case Tool.Scale:
                    ScaleTool.OnGUI(this);
                    break;
                case Tool.Rect:
                    RectTool.OnGUI(this);
                    break;
                case Tool.Transform:
                    TransformTool.OnGUI(this);
                    break;
            }

            // If we are actually dragging the object(s) then disable 2D physics movement.
            if (EditorGUI.EndChangeCheck() && EditorApplication.isPlaying && IsDragEvent)
                Physics2D.SetEditorDragMovement(true, Selection.gameObjects);

            // If we have finished dragging the object(s) then enable 2D physics movement.
            if (EditorApplication.isPlaying && IsMouseUpEvent)
                Physics2D.SetEditorDragMovement(false, Selection.gameObjects);
        }

        void CleanupEditorDragFunctions()
        {
            if (m_DragEditorCache != null)
                m_DragEditorCache.Dispose();
            m_DragEditorCache = null;
        }

        void CallEditorDragFunctions()
        {
            Event evt = Event.current;

            SpriteUtility.OnSceneDrag(this);

            if (evt.type == EventType.Used)
                return;

            if (DragAndDrop.objectReferences.Length == 0)
                return;

            if (m_DragEditorCache == null)
                m_DragEditorCache = new EditorCache(EditorFeatures.OnSceneDrag);

            foreach (Object o in DragAndDrop.objectReferences)
            {
                if (o == null)
                    continue;

                EditorWrapper w = m_DragEditorCache[o];
                if (w != null)
                    w.OnSceneDrag(this);

                if (evt.type == EventType.Used)
                    return;
            }
        }

        void HandleDragging()
        {
            Event evt = Event.current;

            switch (evt.type)
            {
                case EventType.DragPerform:
                case EventType.DragUpdated:
                    CallEditorDragFunctions();

                    if (evt.type == EventType.Used)
                        break;

                    bool isPerform = evt.type == EventType.DragPerform;
                    // call old-style C++ dragging handlers
                    if (DragAndDrop.visualMode != DragAndDropVisualMode.Copy)
                    {
                        GameObject go = HandleUtility.PickGameObject(Event.current.mousePosition, true);
                        DragAndDrop.visualMode = InternalEditorUtility.SceneViewDrag(go, pivot, Event.current.mousePosition, isPerform);
                    }

                    if (isPerform && DragAndDrop.visualMode != DragAndDropVisualMode.None)
                    {
                        DragAndDrop.AcceptDrag();
                        evt.Use();
                        // Bail out as state can be messed up by now.
                        GUIUtility.ExitGUI();
                    }

                    evt.Use();
                    break;
                case EventType.DragExited:
                    CallEditorDragFunctions();
                    CleanupEditorDragFunctions();
                    break;
            }
        }

        void CommandsGUI()
        {
            // @TODO: Validation should be more accurate based on what the view supports

            bool execute = Event.current.type == EventType.ExecuteCommand;
            switch (Event.current.commandName)
            {
                case "Find":
                    if (execute)
                        FocusSearchField();
                    Event.current.Use();
                    break;
                case "FrameSelected":
                    if (execute)
                    {
                        bool useLocking = EditorApplication.timeSinceStartup - lastFramingTime < k_MaxDoubleKeypressTime;

                        FrameSelected(useLocking);

                        lastFramingTime = EditorApplication.timeSinceStartup;
                    }
                    Event.current.Use();
                    break;
                case "FrameSelectedWithLock":
                    if (execute)
                        FrameSelected(true);
                    Event.current.Use();
                    break;
                case "SoftDelete":
                case "Delete":
                    if (execute)
                        Unsupported.DeleteGameObjectSelection();
                    Event.current.Use();
                    break;
                case "Duplicate":
                    if (execute)
                        Unsupported.DuplicateGameObjectsUsingPasteboard();
                    Event.current.Use();
                    break;
                case "Copy":
                    if (execute)
                        Unsupported.CopyGameObjectsToPasteboard();
                    Event.current.Use();
                    break;
                case "Paste":
                    if (execute)
                        Unsupported.PasteGameObjectsFromPasteboard();
                    Event.current.Use();
                    break;
                case "SelectAll":
                    if (execute)
                        Selection.objects = FindObjectsOfType(typeof(GameObject));
                    Event.current.Use();
                    break;
            }
        }

        public void AlignViewToObject(Transform t)
        {
            FixNegativeSize();
            size = 10;
            LookAt(t.position + t.forward * CalcCameraDist(), t.rotation);
        }

        public void AlignWithView()
        {
            FixNegativeSize();
            Vector3 center = camera.transform.position;
            Vector3 dif = center - Tools.handlePosition;
            Quaternion delta = Quaternion.Inverse(Selection.activeTransform.rotation) * camera.transform.rotation;
            float angle;
            Vector3 axis;
            delta.ToAngleAxis(out angle, out axis);
            axis = Selection.activeTransform.TransformDirection(axis);

            Undo.RecordObjects(Selection.transforms, "Align with view");

            foreach (Transform t in Selection.transforms)
            {
                t.position += dif;
                t.RotateAround(center, axis, angle);
            }
        }

        public void MoveToView()
        {
            FixNegativeSize();
            Vector3 dif = pivot - Tools.handlePosition;

            Undo.RecordObjects(Selection.transforms, "Move to view");

            foreach (Transform t in Selection.transforms)
            {
                t.position += dif;
            }
        }

        public void MoveToView(Transform target)
        {
            target.position = pivot;
        }

        public bool FrameSelected()
        {
            return FrameSelected(false);
        }

        public bool FrameSelected(bool lockView)
        {
            viewIsLockedToObject = lockView;
            FixNegativeSize();

            Bounds bounds = InternalEditorUtility.CalculateSelectionBounds(false, Tools.pivotMode == PivotMode.Pivot);

            // Check active editor for OnGetFrameBounds
            foreach (Editor editor in GetActiveEditors())
            {
                MethodInfo hasBoundsMethod = editor.GetType().GetMethod("HasFrameBounds", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

                if (hasBoundsMethod != null)
                {
                    object hasBounds = hasBoundsMethod.Invoke(editor, null);
                    if (hasBounds is bool && (bool)hasBounds)
                    {
                        MethodInfo getBoundsMethod = editor.GetType().GetMethod("OnGetFrameBounds", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

                        if (getBoundsMethod != null)
                        {
                            object obj = getBoundsMethod.Invoke(editor, null);
                            if (obj is Bounds)
                                bounds = (Bounds)obj;
                        }
                    }
                }
            }

            return Frame(bounds, EditorApplication.isPlaying);
        }

        public bool Frame(Bounds bounds, bool instant = true)
        {
            float newSize = bounds.extents.magnitude * 1.5f;
            if (newSize == Mathf.Infinity)
                return false;
            if (newSize == 0)
                newSize = 10;

            // We snap instantly into target on playmode, because things might be moving fast and lerping lags behind
            LookAt(bounds.center, m_Rotation.target, newSize * 2.2f, m_Ortho.value, instant);

            return true;
        }

        void CreateSceneCameraAndLights()
        {
            GameObject cameraGO = EditorUtility.CreateGameObjectWithHideFlags("SceneCamera", HideFlags.HideAndDontSave, typeof(Camera));
            cameraGO.AddComponentInternal("FlareLayer");
            cameraGO.AddComponentInternal("HaloLayer");

            m_Camera = cameraGO.GetComponent<Camera>();
            m_Camera.enabled = false;
            m_Camera.cameraType = CameraType.SceneView;

            for (int i = 0; i < 3; i++)
            {
                GameObject lightGO = EditorUtility.CreateGameObjectWithHideFlags("SceneLight", HideFlags.HideAndDontSave, typeof(Light));
                m_Light[i] = lightGO.GetComponent<Light>();
                m_Light[i].type = LightType.Directional;
                m_Light[i].intensity = 1.0f;
                m_Light[i].enabled = false;
            }
            m_Light[0].color = kSceneViewFrontLight;

            m_Light[1].color = kSceneViewUpLight - kSceneViewMidLight;
            m_Light[1].transform.LookAt(Vector3.down);
            m_Light[1].renderMode = LightRenderMode.ForceVertex;

            m_Light[2].color = kSceneViewDownLight - kSceneViewMidLight;
            m_Light[2].transform.LookAt(Vector3.up);
            m_Light[2].renderMode = LightRenderMode.ForceVertex;

            HandleUtility.handleMaterial.SetColor("_SkyColor", kSceneViewUpLight * 1.5f);
            HandleUtility.handleMaterial.SetColor("_GroundColor", kSceneViewDownLight * 1.5f);
            HandleUtility.handleMaterial.SetColor("_Color", kSceneViewFrontLight * 1.5f);

            SetupPBRValidation();
        }

        void CallOnSceneGUI()
        {
            foreach (Editor editor in GetActiveEditors())
            {
                if (!EditorGUIUtility.IsGizmosAllowedForObject(editor.target))
                    continue;
                /*
                // Don't call function for editors whose target's GameObject is not active.
                Component comp = editor.target as Component;
                if (comp && !comp.gameObject.activeInHierarchy)
                    continue;

                // No gizmo if component state is disabled
                if (!InternalEditorUtility.GetIsInspectorExpanded(comp))
                    continue;
                 * */

                MethodInfo method = editor.GetType().GetMethod("OnSceneGUI", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

                if (method != null)
                {
                    Editor.m_AllowMultiObjectAccess = true;
                    for (int n = 0; n < editor.targets.Length; n++)
                    {
                        ResetOnSceneGUIState();
                        editor.referenceTargetIndex = n;

                        EditorGUI.BeginChangeCheck();
                        // Ironically, only allow multi object access inside OnSceneGUI if editor does NOT support multi-object editing.
                        // since there's no harm in going through the serializedObject there if there's always only one target.
                        Editor.m_AllowMultiObjectAccess = !editor.canEditMultipleObjects;
                        method.Invoke(editor, null);
                        Editor.m_AllowMultiObjectAccess = true;
                        if (EditorGUI.EndChangeCheck())
                            editor.serializedObject.SetIsDifferentCacheDirty();
                    }
                    ResetOnSceneGUIState();
                }
            }

            if (onSceneGUIDelegate != null)
            {
                onSceneGUIDelegate(this);
                ResetOnSceneGUIState();
            }
        }

        void ResetOnSceneGUIState()
        {
            Handles.ClearHandles();
            HandleUtility.s_CustomPickDistance = HandleUtility.kPickDistance;
            EditorGUIUtility.ResetGUIState();

            // Don't apply any playmode tinting to scene views
            GUI.color = Color.white;
        }

        void CallOnPreSceneGUI()
        {
            foreach (Editor editor in GetActiveEditors())
            {
                // reset the handles matrix, OnPreSceneGUI calls may change it.
                Handles.ClearHandles();

                // Don't call function for editors whose target's GameObject is not active.
                Component comp = editor.target as Component;
                if (comp && !comp.gameObject.activeInHierarchy)
                    continue;

                MethodInfo method = editor.GetType().GetMethod("OnPreSceneGUI", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

                if (method != null)
                {
                    Editor.m_AllowMultiObjectAccess = true;
                    for (int n = 0; n < editor.targets.Length; n++)
                    {
                        editor.referenceTargetIndex = n;
                        // Ironically, only allow multi object access inside OnPreSceneGUI if editor does NOT support multi-object editing.
                        // since there's no harm in going through the serializedObject there if there's always only one target.
                        Editor.m_AllowMultiObjectAccess = !editor.canEditMultipleObjects;
                        method.Invoke(editor, null);
                        Editor.m_AllowMultiObjectAccess = true;
                    }
                }
            }

            if (onPreSceneGUIDelegate != null)
            {
                onPreSceneGUIDelegate(this);
            }

            // reset the handles matrix, calls above calls might have changed it
            Handles.ClearHandles();
        }

        internal static void ShowNotification(string notificationText)
        {
            Object[] allSceneViews = Resources.FindObjectsOfTypeAll(typeof(SceneView));
            var notificationViews = new List<EditorWindow>();
            foreach (SceneView sceneView in allSceneViews)
            {
                if (sceneView.m_Parent is DockArea)
                {
                    var dock = (DockArea)sceneView.m_Parent;
                    if (dock)
                    {
                        if (dock.actualView == sceneView)
                        {
                            notificationViews.Add(sceneView);
                        }
                    }
                }
            }

            if (notificationViews.Count > 0)
            {
                foreach (EditorWindow notificationView in notificationViews)
                    notificationView.ShowNotification(GUIContent.Temp(notificationText));
            }
            else
            {
                Debug.LogError(notificationText);
            }
        }

        public static void ShowCompileErrorNotification()
        {
            ShowNotification("All compiler errors have to be fixed before you can enter playmode!");
        }

        internal static void ShowSceneViewPlayModeSaveWarning()
        {
            // In this case, we wan't to explicitely try the GameView before passing it on to whatever notificationView we have
            var gameView = (GameView)WindowLayout.FindEditorWindowOfType(typeof(GameView));
            if (gameView != null && gameView.hasFocus)
                gameView.ShowNotification(new GUIContent("You must exit play mode to save the scene!"));
            else
                ShowNotification("You must exit play mode to save the scene!");
        }

        void ResetToDefaults(EditorBehaviorMode behaviorMode)
        {
            switch (behaviorMode)
            {
                case EditorBehaviorMode.Mode2D:
                    m_2DMode = true;
                    m_Rotation.value = Quaternion.identity;
                    m_Position.value = kDefaultPivot;
                    m_Size.value = kDefaultViewSize;
                    m_Ortho.value = true;

                    m_LastSceneViewRotation = kDefaultRotation;
                    m_LastSceneViewOrtho = false;
                    break;

                default: // Default to 3D mode (BUGFIX:569204)
                case EditorBehaviorMode.Mode3D:
                    m_2DMode = false;
                    m_Rotation.value = kDefaultRotation;
                    m_Position.value = kDefaultPivot;
                    m_Size.value = kDefaultViewSize;
                    m_Ortho.value = false;
                    break;
            }
        }

        internal void OnNewProjectLayoutWasCreated()
        {
            ResetToDefaults(EditorSettings.defaultBehaviorMode);
        }

        private void On2DModeChange()
        {
            if (m_2DMode)
            {
                lastSceneViewRotation = rotation;
                m_LastSceneViewOrtho = orthographic;
                LookAt(pivot, Quaternion.identity, size, true);
                if (Tools.current == Tool.Move)
                    Tools.current = Tool.Rect;
            }
            else
            {
                LookAt(pivot, lastSceneViewRotation, size, m_LastSceneViewOrtho);
                if (Tools.current == Tool.Rect)
                    Tools.current = Tool.Move;
            }

            // Let's not persist the vertex snapping mode on 2D/3D mode change
            HandleUtility.ignoreRaySnapObjects = null;
            Tools.vertexDragging = false;
            Tools.handleOffset = Vector3.zero;
        }
    }
} // namespace
