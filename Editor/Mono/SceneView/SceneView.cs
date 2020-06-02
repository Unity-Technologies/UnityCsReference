// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEditor.AnimatedValues;
using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UnityEditor.EditorTools;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.Profiling;
using UnityEditor.Snap;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using Component = UnityEngine.Component;
using FrameCapture = UnityEngine.Apple.FrameCapture;
using FrameCaptureDestination = UnityEngine.Apple.FrameCaptureDestination;

namespace UnityEditor
{
    [EditorWindowTitle(title = "Scene", useTypeNameAsIconName = true)]
    public class SceneView : SearchableEditorWindow, IHasCustomMenu
    {
        [Serializable]
        public struct CameraMode
        {
            internal CameraMode(DrawCameraMode drawMode, string name, string section)
            {
                this.drawMode = drawMode;
                this.name = name;
                this.section = section;
            }

            public DrawCameraMode drawMode;
            public string name;
            public string section;

            public static bool operator==(CameraMode a, CameraMode z)
            {
                return a.drawMode == z.drawMode && a.name == z.name && a.section == z.section;
            }

            public static bool operator!=(CameraMode a, CameraMode z)
            {
                return !(a == z);
            }

            public override bool Equals(System.Object otherObject)
            {
                if (ReferenceEquals(otherObject, null))
                    return false;
                if (!(otherObject is CameraMode))
                    return false;
                return this == (CameraMode)otherObject;
            }

            public override int GetHashCode()
            {
                return ToString().GetHashCode();
            }

            public override string ToString()
            {
                return UnityString.Format("{0}||{1}||{2}", drawMode, name, section);
            }
        }

        private static SceneView s_LastActiveSceneView;
        private static SceneView s_CurrentDrawingSceneView;

        public static SceneView lastActiveSceneView
        {
            get
            {
                if (s_LastActiveSceneView == null && s_SceneViews.Count > 0)
                    s_LastActiveSceneView = s_SceneViews[0] as SceneView;
                return s_LastActiveSceneView;
            }
        }

        public static SceneView currentDrawingSceneView { get { return s_CurrentDrawingSceneView; } }

        internal static readonly PrefColor kSceneViewBackground = new PrefColor("Scene/Background", 0.278431f, 0.278431f, 0.278431f, 0);
        internal static readonly PrefColor kSceneViewPrefabBackground = new PrefColor("Scene/Background for Prefabs", 0.132f, 0.231f, 0.330f, 0);
        static readonly PrefColor kSceneViewWire = new PrefColor("Scene/Wireframe", 0.0f, 0.0f, 0.0f, 0.5f);
        static readonly PrefColor kSceneViewWireOverlay = new PrefColor("Scene/Wireframe Overlay", 0.0f, 0.0f, 0.0f, 0.25f);
        static readonly PrefColor kSceneViewSelectedOutline = new PrefColor("Scene/Selected Outline", 255.0f / 255.0f, 102.0f / 255.0f, 0.0f / 255.0f, 0.0f / 255.0f);
        static readonly PrefColor kSceneViewSelectedChildrenOutline = new PrefColor("Scene/Selected Children Outline", 94.0f / 255.0f, 119.0f / 255.0f, 155.0f / 255.0f, 0.0f / 255.0f);
        static readonly PrefColor kSceneViewSelectedWire = new PrefColor("Scene/Wireframe Selected", 94.0f / 255.0f, 119.0f / 255.0f, 155.0f / 255.0f, 64.0f / 255.0f);

        static readonly PrefColor kSceneViewMaterialValidateLow = new PrefColor("Scene/Material Validator Value Too Low", 255.0f / 255.0f, 0.0f, 0.0f, 1.0f);
        static readonly PrefColor kSceneViewMaterialValidateHigh = new PrefColor("Scene/Material Validator Value Too High", 0.0f, 0.0f, 255.0f / 255.0f, 1.0f);
        static readonly PrefColor kSceneViewMaterialValidatePureMetal = new PrefColor("Scene/Material Validator Pure Metal", 255.0f / 255.0f, 255.0f / 255.0f, 0.0f, 1.0f);

        static readonly PrefColor kSceneViewMaterialNoContributeGI = new PrefColor("Scene/Contribute GI Off", 229.0f / 255.0f, 203.0f / 255.0f, 132.0f / 255.0f, 1.0f);
        static readonly PrefColor kSceneViewMaterialReceiveGILightmaps = new PrefColor("Scene/Contribute GI / Receive GI Lightmaps", 89.0f / 255.0f, 148.0f / 255.0f, 161.0f / 255.0f, 1.0f);
        static readonly PrefColor kSceneViewMaterialReceiveGILightProbes = new PrefColor("Scene/Contribute GI / Receive GI Light Probes", 221.0f / 255.0f, 115.0f / 255.0f, 91.0f / 255.0f, 1.0f);

        internal static Color kSceneViewFrontLight = new Color(0.769f, 0.769f, 0.769f, 1);
        internal static Color kSceneViewUpLight = new Color(0.212f, 0.227f, 0.259f, 1);
        internal static Color kSceneViewMidLight = new Color(0.114f, 0.125f, 0.133f, 1);
        internal static Color kSceneViewDownLight = new Color(0.047f, 0.043f, 0.035f, 1);

        public static Color selectedOutlineColor => kSceneViewSelectedOutline.Color;

        [SerializeField]
        bool m_ShowContextualTools;


        internal static SavedBool s_PreferenceEnableFilteringWhileSearching = new SavedBool("SceneView.enableFilteringWhileSearching", true);
        internal static SavedBool s_PreferenceEnableFilteringWhileLodGroupEditing = new SavedBool("SceneView.enableFilteringWhileLodGroupEditing", true);

        internal bool displayToolModes
        {
            get { return m_ShowContextualTools; }
            set { m_ShowContextualTools = value; }
        }

        internal static Transform GetDefaultParentObjectIfSet()
        {
            Transform parentObject = null;
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            string activeSceneGUID = prefabStage != null ? prefabStage.scene.guid : EditorSceneManager.GetActiveScene().guid;
            int id = SceneHierarchy.GetDefaultParentForSession(activeSceneGUID);
            if (id != 0)
            {
                var objectFromInstanceID = EditorUtility.InstanceIDToObject(id) as GameObject;
                parentObject = objectFromInstanceID?.gameObject?.transform;
            }

            return parentObject;
        }

        static void OnSelectedObjectWasDestroyed(int unused)
        {
            s_ActiveEditorsDirty = true;
        }

        static void OnEditorTrackerRebuilt()
        {
            s_ActiveEditorsDirty = true;
        }

        static List<Editor> s_ActiveEditors = new List<Editor>();

        static bool s_ActiveEditorsDirty;

        internal static IEnumerable<Editor> activeEditors
        {
            get
            {
                CollectActiveEditors();
                return s_ActiveEditors;
            }
        }

        static void CollectActiveEditors()
        {
            if (!s_ActiveEditorsDirty)
                return;

            s_ActiveEditorsDirty = false;

            s_ActiveEditors.Clear();

            if (s_SharedTracker == null)
                s_SharedTracker = ActiveEditorTracker.sharedTracker;

            foreach (var editor in s_SharedTracker.activeEditors)
                s_ActiveEditors.Add(editor);

            foreach (var inspector in InspectorWindow.GetInspectors())
            {
                if (inspector.isLocked)
                {
                    foreach (var editor in inspector.tracker.activeEditors)
                        s_ActiveEditors.Add(editor);
                }
            }
        }

        bool m_SkipFadingPending;
        internal void SkipFading()
        {
            m_SkipFadingPending = true;
        }

        [SerializeField]
        string m_WindowGUID;
        internal string windowGUID => m_WindowGUID;

        [SerializeField] bool m_Gizmos = true;
        public bool drawGizmos
        {
            get { return m_Gizmos; }
            set { m_Gizmos = value; }
        }

        internal bool showToolbar { get; set; } = true;

        Scene m_CustomScene;
        protected internal Scene customScene
        {
            get { return m_CustomScene; }
            set
            {
                m_CustomScene = value;
                m_Camera.scene = m_CustomScene;

                var stage = StageUtility.GetStageHandle(m_CustomScene);
                StageUtility.SetSceneToRenderInStage(m_CustomLightsScene, stage);
            }
        }

        [SerializeField] ulong m_OverrideSceneCullingMask;
        internal ulong overrideSceneCullingMask
        {
            get { return m_OverrideSceneCullingMask; }
            set
            {
                m_OverrideSceneCullingMask = value;
                m_Camera.overrideSceneCullingMask = value;
            }
        }

        SceneViewStageHandling m_StageHandling;

        float toolbarHeight
        {
            get
            {
                var _toolbarHeight = showToolbar ? EditorGUI.kWindowToolbarHeight.value : 0;
                return (m_StageHandling != null && m_StageHandling.isShowingBreadcrumbBar)
                    ? m_StageHandling.breadcrumbHeight + _toolbarHeight
                    : _toolbarHeight;
            }
        }

        float sceneViewHeight => position.height - toolbarHeight;

        // Returns the calculated rect where we render the camera in the SceneView (in window space coordinates)
        internal Rect cameraRect
        {
            get { return new Rect(0, toolbarHeight, position.width, sceneViewHeight); }
        }

        Transform m_CustomParentForNewGameObjects;
        protected internal Transform customParentForDraggedObjects
        {
            get { return customParentForNewGameObjects; }
            set { customParentForNewGameObjects = value; }
        }

        internal Transform customParentForNewGameObjects
        {
            get { return m_CustomParentForNewGameObjects; }
            set { m_CustomParentForNewGameObjects = value; }
        }

        [NonSerialized]
        static readonly Quaternion kDefaultRotation = Quaternion.LookRotation(new Vector3(-1, -.7f, -1));

        const float kDefaultViewSize = 10f;

        [NonSerialized]
        static readonly Vector3 kDefaultPivot = Vector3.zero;

        const float kOrthoThresholdAngle = 3f;
        const float kOneOverSqrt2 = 0.707106781f;
        // Don't allow scene view zoom/size to go to crazy high values, or otherwise various
        // operations will start going to infinities etc.
        internal const float k_MaxSceneViewSize = 3.2e34f;
        // Limit the max draw distance to Sqrt(float.MaxValue) because transparent sorting function uses dist^2, and
        // Asserts that values are finite.
        internal const float k_MaxCameraFarClip = 1.844674E+19f;
        internal const float k_MinCameraNearClip = 1e-5f;

        [NonSerialized]
        static ActiveEditorTracker s_SharedTracker;

        [SerializeField]
        bool m_SceneIsLit = true;

        [Obsolete("m_SceneLighting has been deprecated. Use sceneLighting instead (UnityUpgradable) -> UnityEditor.SceneView.sceneLighting", true)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool m_SceneLighting = true;

        public bool sceneLighting
        {
            get { return m_SceneIsLit; }
            set { m_SceneIsLit = value; }
        }

        public event Func<CameraMode, bool> onValidateCameraMode;
        public event Action<CameraMode>     onCameraModeChanged;
        public event Action<bool>           gridVisibilityChanged;

        private bool m_WasFocused = false;

        [Serializable]
        public class SceneViewState
        {
            [SerializeField, FormerlySerializedAs("showMaterialUpdate")]
            bool m_AlwaysRefresh;

            public bool showFog = true;

            // marked obsolete by @karlh 2020/4/14
            [EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("Obsolete msg (UnityUpgradable) -> alwaysRefresh")]
            public bool showMaterialUpdate
            {
                get => m_AlwaysRefresh;
                set => m_AlwaysRefresh = value;
            }

            public bool alwaysRefresh
            {
                get => m_AlwaysRefresh;
                set => m_AlwaysRefresh = value;
            }

            public bool showSkybox = true;
            public bool showFlares = true;
            public bool showImageEffects = true;
            public bool showParticleSystems = true;
            public bool showVisualEffectGraphs = true;

            public bool fogEnabled => fxEnabled && showFog;
            // marked obsolete by @karlh 2020/4/14
            [EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("Obsolete msg (UnityUpgradable) -> alwaysRefreshEnabled")]
            public bool materialUpdateEnabled => alwaysRefreshEnabled;
            public bool alwaysRefreshEnabled => fxEnabled && alwaysRefresh;
            public bool skyboxEnabled => fxEnabled && showSkybox;
            public bool flaresEnabled => fxEnabled && showFlares;
            public bool imageEffectsEnabled => fxEnabled && showImageEffects;
            public bool particleSystemsEnabled => fxEnabled && showParticleSystems;
            public bool visualEffectGraphsEnabled => fxEnabled && showVisualEffectGraphs;

            [SerializeField]
            bool m_FxEnabled = true;

            public SceneViewState()
            {
            }

            public SceneViewState(SceneViewState other)
            {
                fxEnabled = other.fxEnabled;
                showFog = other.showFog;
                alwaysRefresh = other.alwaysRefresh;
                showSkybox = other.showSkybox;
                showFlares = other.showFlares;
                showImageEffects = other.showImageEffects;
                showParticleSystems = other.showParticleSystems;
                showVisualEffectGraphs = other.showVisualEffectGraphs;
            }

            [Obsolete("IsAllOn() has been deprecated. Use allEnabled instead (UnityUpgradable) -> allEnabled")]
            public bool IsAllOn()
            {
                return allEnabled;
            }

            public bool allEnabled
            {
                get
                {
                    bool all =  showFog && alwaysRefresh && showSkybox && showFlares && showImageEffects && showParticleSystems;
                    if (UnityEngine.VFX.VFXManager.activateVFX)
                        all = all && showVisualEffectGraphs;
                    return all;
                }
            }

            [Obsolete("Toggle() has been deprecated. Use SetAllEnabled() instead (UnityUpgradable) -> SetAllEnabled(*)")]
            public void Toggle(bool value)
            {
                SetAllEnabled(value);
            }

            public void SetAllEnabled(bool value)
            {
                showFog = value;
                alwaysRefresh = value;
                showSkybox = value;
                showFlares = value;
                showImageEffects = value;
                showParticleSystems = value;
                showVisualEffectGraphs = value;
            }

            public bool fxEnabled
            {
                get { return m_FxEnabled; }
                set { m_FxEnabled = value; }
            }
        }

        [SerializeField]
        private bool m_2DMode;
        public bool in2DMode
        {
            get { return m_2DMode; }
            set
            {
                if (m_2DMode != value)
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
            get { return m_isRotationLocked; }
            set { m_isRotationLocked = value; }
        }

        internal static List<CameraMode> userDefinedModes { get; } = new List<CameraMode>();

        [SerializeField]
        bool m_PlayAudio = false;

        [Obsolete("m_AudioPlay has been deprecated. Use audioPlay instead (UnityUpgradable) -> audioPlay", true)]
        public bool m_AudioPlay = false;

        public bool audioPlay
        {
            get { return m_PlayAudio; }

            set
            {
                if (value == m_PlayAudio)
                    return;
                m_PlayAudio = value;
                RefreshAudioPlay();
            }
        }

        static SceneView s_AudioSceneView;

        [SerializeField]
        AnimVector3 m_Position = new AnimVector3(kDefaultPivot);

#pragma warning disable 618
        [Obsolete("OnSceneFunc() has been deprecated. Use System.Action instead.")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public delegate void OnSceneFunc(SceneView sceneView);

        // Marked obsolete 2018-11-28
        [Obsolete("onSceneGUIDelegate has been deprecated. Use duringSceneGui instead.")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static OnSceneFunc onSceneGUIDelegate;
#pragma warning restore 618

        public static event Action<SceneView> beforeSceneGui;
        public static event Action<SceneView> duringSceneGui;

        internal static event Func<SceneView, VisualElement> addCustomVisualElementToSceneView;

        // Used for performance tests
        internal static event Action<SceneView> onGUIStarted;
        internal static event Action<SceneView> onGUIEnded;

        [Obsolete("Use cameraMode instead", false)]
        public DrawCameraMode m_RenderMode = 0;

        [Obsolete("Use cameraMode instead", false)]
        public DrawCameraMode renderMode
        {
            get
            {
                return m_CameraMode.drawMode;
            }
            set
            {
                if (value == DrawCameraMode.UserDefined)
                    throw new ArgumentException("Use cameraMode to set user-defined modes");
                cameraMode = SceneRenderModeWindow.GetBuiltinCameraMode(value);
            }
        }

        [SerializeField]
        CameraMode m_CameraMode;

        public CameraMode cameraMode
        {
            get
            {
                // fix for case 969889 where the toolbar is empty when we haven't fully initialized the value
                if (string.IsNullOrEmpty(m_CameraMode.name))
                {
                    m_CameraMode = SceneRenderModeWindow.GetBuiltinCameraMode(m_CameraMode.drawMode);
                }
                return m_CameraMode;
            }
            set
            {
                if (!IsValidCameraMode(value))
                {
                    throw new ArgumentException(string.Format("The provided camera mode {0} is not registered!", value));
                }
                m_CameraMode = value;

                if (onCameraModeChanged != null)
                    onCameraModeChanged(m_CameraMode);
            }
        }

        [Obsolete("m_ValidateTrueMetals has been deprecated. Use validateTrueMetals instead (UnityUpgradable) -> validateTrueMetals", true)]
        public bool m_ValidateTrueMetals = false;

        [SerializeField]
        bool m_DoValidateTrueMetals = false;

        public bool validateTrueMetals
        {
            get { return m_DoValidateTrueMetals; }

            set
            {
                if (m_DoValidateTrueMetals == value)
                    return;

                m_DoValidateTrueMetals = value;
                Shader.SetGlobalInt("_CheckPureMetal", m_DoValidateTrueMetals ? 1 : 0);
            }
        }

        [SerializeField]
        float m_ExposureSliderValue = 0.0f;
        // this value can be altered by the user
        float m_ExposureSliderMax = 16f;

        Texture2D m_ExposureTexture = null;
        Texture2D m_EmptyExposureTexture = null;

        internal bool showExposureSettings
        {
            get
            {
                return (this.cameraMode.drawMode == DrawCameraMode.BakedEmissive || this.cameraMode.drawMode == DrawCameraMode.BakedLightmap ||
                    this.cameraMode.drawMode == DrawCameraMode.RealtimeEmissive || this.cameraMode.drawMode == DrawCameraMode.RealtimeIndirect);
            }
        }

        [SerializeField]
        SceneViewState m_SceneViewState;

        public SceneViewState sceneViewState
        {
            get { return m_SceneViewState; }
            set { m_SceneViewState = value; }
        }

        [SerializeField]
        SceneViewGrid m_Grid;

        public bool showGrid
        {
            get { return sceneViewGrids.showGrid; }
            set { sceneViewGrids.showGrid = value; }
        }

        [SerializeField]
        internal SceneViewRotation svRot;
        [SerializeField]
        internal AnimQuaternion m_Rotation = new AnimQuaternion(kDefaultRotation);

        // How large an area the scene view covers (measured vertically)
        [SerializeField]
        AnimFloat m_Size = new AnimFloat(kDefaultViewSize);

        [SerializeField]
        internal AnimBool m_Ortho = new AnimBool();

        [NonSerialized]
        Camera m_Camera;

        VisualElement m_CameraViewVisualElement;

        static readonly string s_CameraRectVisualElementName = "unity-scene-view-camera-rect";
        internal VisualElement cameraViewVisualElement => m_CameraViewVisualElement;

        [Serializable]
        public class CameraSettings
        {
            const float defaultEasingDuration = .4f;
            const float kAbsoluteSpeedMin = .001f;
            const float kAbsoluteSpeedMax = 99f;
            const float kAbsoluteEasingDurationMin = .1f;
            const float kAbsoluteEasingDurationMax = 2f;
            const float kMinSpeedMinMaxRange = .001f;

            [SerializeField]
            float m_Speed;
            [SerializeField]
            float m_SpeedNormalized;
            [SerializeField]
            float m_SpeedMin;
            [SerializeField]
            float m_SpeedMax;
            [SerializeField]
            bool m_EasingEnabled;
            [SerializeField]
            float m_EasingDuration;
            [SerializeField]
            bool m_AccelerationEnabled;

            [SerializeField]
            float m_FieldOfViewHorizontalOrVertical; // either horizontal or vertical depending on aspect ratio
            [SerializeField]
            float m_NearClip;
            [SerializeField]
            float m_FarClip;
            [SerializeField]
            bool m_DynamicClip;
            [SerializeField]
            bool m_OcclusionCulling;

            public CameraSettings()
            {
                m_Speed = 1f;
                m_SpeedNormalized = .5f;
                m_SpeedMin = kAbsoluteSpeedMin;
                m_SpeedMax = 2f;
                m_EasingEnabled = true;
                m_EasingDuration = defaultEasingDuration;
                fieldOfView = kDefaultPerspectiveFov;
                m_DynamicClip = true;
                m_OcclusionCulling = false;
                m_NearClip = .03f;
                m_FarClip = 10000f;
                m_AccelerationEnabled = true;
            }

            public float speed
            {
                get
                {
                    return m_Speed;
                }
                set
                {
                    speedNormalized = Mathf.InverseLerp(m_SpeedMin, m_SpeedMax, value);
                }
            }

            public float speedNormalized
            {
                get
                {
                    return m_SpeedNormalized;
                }
                set
                {
                    m_SpeedNormalized = Mathf.Clamp01(value);
                    m_Speed = Mathf.Lerp(m_SpeedMin, m_SpeedMax, m_SpeedNormalized);
                }
            }

            public float speedMin
            {
                get
                {
                    return m_SpeedMin;
                }
                set
                {
                    float[] m_Vector2Floats = { value, m_SpeedMax };
                    SetSpeedMinMax(m_Vector2Floats);
                }
            }

            public float speedMax
            {
                get
                {
                    return m_SpeedMax;
                }
                set
                {
                    float[] m_Vector2Floats = { m_SpeedMin, value };
                    SetSpeedMinMax(m_Vector2Floats);
                }
            }

            // Easing is applied when starting and stopping movement. When enabled, the camera will lerp from it's
            // current speed to the target speed over the course of `CameraSettings.easingDuration` seconds.
            public bool easingEnabled
            {
                get { return m_EasingEnabled; }
                set { m_EasingEnabled = value; }
            }

            // How many seconds should the camera take to go from stand-still to initial full speed. When setting an animated value
            // speed, use `1 / duration`.
            public float easingDuration
            {
                get
                {
                    return m_EasingDuration;
                }
                set
                {
                    // Clamp and round to 1 decimal point
                    m_EasingDuration = (float)Math.Round(Mathf.Clamp(value, kAbsoluteEasingDurationMin, kAbsoluteEasingDurationMax), 1);
                }
            }

            // When acceleration is enabled, camera speed is continuously increased while in motion. When acceleration
            // is disabled, speed is a constant value defined by `CameraSettings.speed`
            public bool accelerationEnabled
            {
                get { return m_AccelerationEnabled; }
                set { m_AccelerationEnabled = value; }
            }

            // this ensures that the resolution the slider snaps to is sufficient given the minimum speed, and the
            // range of appropriate values
            internal float RoundSpeedToNearestSignificantDecimal(float value)
            {
                if (value <= speedMin)
                    return speedMin;

                if (value >= speedMax)
                    return speedMax;

                float rng = speedMax - speedMin;
                int min_rnd = speedMin < .01f ? 3 : speedMin < .1f ? 2 : speedMin < 1f ? 1 : 0;
                int rng_rnd = rng < 1f ? 2 : rng < 10f ? 1 : 0;
                return (float)Math.Round(value, Mathf.Max(min_rnd, rng_rnd));
            }

            internal void SetSpeedMinMax(float[] floatValues)
            {
                // Clamp min to valid ranges
                float min = Mathf.Clamp(floatValues[0], kAbsoluteSpeedMin, kAbsoluteSpeedMax - kMinSpeedMinMaxRange);
                float minRange = min < .1f ? .01f : min < 1f ? .1f : 1f;
                float max = Mathf.Clamp(floatValues[1], min + minRange, kAbsoluteSpeedMax);

                m_SpeedMin = min;
                m_SpeedMax = max;

                // This will clamp the speed to the new range
                speed = m_Speed;
            }

            internal void SetClipPlanes(float near, float far)
            {
                farClip = Mathf.Clamp(far, float.Epsilon, k_MaxCameraFarClip);
                nearClip = Mathf.Max(k_MinCameraNearClip, near);
            }

            public float fieldOfView
            {
                get { return m_FieldOfViewHorizontalOrVertical; }
                set { m_FieldOfViewHorizontalOrVertical = value; }
            }

            public float nearClip
            {
                get { return m_NearClip; }
                set { m_NearClip = value; }
            }

            public float farClip
            {
                get { return m_FarClip; }
                set { m_FarClip = value; }
            }

            public bool dynamicClip
            {
                get { return m_DynamicClip; }
                set { m_DynamicClip = value; }
            }

            public bool occlusionCulling
            {
                get { return m_OcclusionCulling; }
                set { m_OcclusionCulling = value; }
            }
        }

        [SerializeField]
        private CameraSettings m_CameraSettings;

        public CameraSettings cameraSettings
        {
            get { return m_CameraSettings; }
            set { m_CameraSettings = value; }
        }

        internal Vector2 GetDynamicClipPlanes()
        {
            float farClip = Mathf.Clamp(2000f * size, 1000f, k_MaxCameraFarClip);
            return new Vector2(farClip * 0.000005f, farClip);
        }

        internal SceneViewGrid sceneViewGrids
        {
            get { return m_Grid; }
        }

        public void ResetCameraSettings()
        {
            m_CameraSettings = new CameraSettings();
        }

        // Thomas Tu: 2019-06-20. Will be marked as Obsolete.
        // We need to deal with code dependency in packages first.
        internal bool showGlobalGrid { get { return showGrid; } set { showGrid = value; } }

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
            var eventType = Event.current.type;
            if (eventType == EventType.Repaint || eventType == EventType.MouseMove)
                s_MouseRects.Add(new CursorRect(rect, cursor));
        }

        static float GetPerspectiveCameraDistance(float objectSize, float fov)
        {
            //        A
            //        |\        We want to place camera at a
            //        | \       distance that, at the given FOV,
            //        |  \      would enclose a sphere of radius
            //     _..+.._\     "size". Here |BC|=size, and we
            //   .'   |   '\    need to find |AB|. ACB is a right
            //  /     |    _C   angle, andBAC is half the FOV. So
            // |      | _-   |  that gives: sin(BAC)=|BC|/|AB|,
            // |      B      |  and thus |AB|=|BC|/sin(BAC).
            // |             |
            //  \           /
            //   '._     _.'
            //      `````
            return objectSize / Mathf.Sin(fov * 0.5f * Mathf.Deg2Rad);
        }

        public float cameraDistance
        {
            get
            {
                float res;
                if (!camera.orthographic)
                {
                    float fov = m_Ortho.Fade(perspectiveFov, 0);
                    res = GetPerspectiveCameraDistance(size, fov);
                }
                else
                    res = size * 2f;

                // clamp to allowed range in case scene view size was huge
                return Mathf.Clamp(res, -k_MaxSceneViewSize, k_MaxSceneViewSize);
            }
        }

        [System.NonSerialized]
        Scene m_CustomLightsScene;
        [System.NonSerialized]
        Light[] m_Light = new Light[3];

        RectSelection m_RectSelection;

        const float kDefaultPerspectiveFov = 60;

        static ArrayList s_SceneViews = new ArrayList();
        public static ArrayList sceneViews { get { return s_SceneViews; } }

        static List<Camera> s_AllSceneCameraList = new List<Camera>();
        static Camera[] s_AllSceneCameras = new Camera[] {};

        static Material s_AlphaOverlayMaterial;
        static Material s_DeferredOverlayMaterial;
        static Shader s_ShowOverdrawShader;
        static Shader s_ShowMipsShader;
        static Shader s_ShowTextureStreamingShader;
        static Shader s_AuraShader;
        static Material s_FadeMaterial;
        static Material s_ApplyFilterMaterial;
        static Texture2D s_MipColorsTexture;

        // Handle Dragging of stuff over scene view
        //static ArrayList s_DraggedEditors = null;
        //static GameObject[] s_PickedObject = { null };

        internal static class Styles
        {
            public static GUIContent toolsContent = EditorGUIUtility.TrIconContent("SceneViewTools", "Hide or show the Component Editor Tools panel in the Scene view.");
            public static GUIContent lighting = EditorGUIUtility.TrIconContent("SceneviewLighting", "When toggled on, the Scene lighting is used. When toggled off, a light attached to the Scene view camera is used.");
            public static GUIContent fx = EditorGUIUtility.TrIconContent("SceneviewFx", "Toggle skybox, fog, and various other effects.");
            public static GUIContent audioPlayContent = EditorGUIUtility.TrIconContent("SceneviewAudio", "Toggle audio on or off.");
            public static GUIContent gizmosContent = EditorGUIUtility.TrTextContent("Gizmos", "Toggle visibility of all Gizmos in the Scene view");
            public static GUIContent gizmosDropDownContent = EditorGUIUtility.TrTextContent("", "Toggle the visibility of different Gizmos in the Scene view.");
            public static GUIContent mode2DContent = EditorGUIUtility.TrIconContent("SceneView2D", "When toggled on, the Scene is in 2D view. When toggled off, the Scene is in 3D view.");
            public static GUIContent gridXToolbarContent = EditorGUIUtility.TrIconContent("GridAxisX", "Toggle the visibility of the grid");
            public static GUIContent gridYToolbarContent = EditorGUIUtility.TrIconContent("GridAxisY", "Toggle the visibility of the grid");
            public static GUIContent gridZToolbarContent = EditorGUIUtility.TrIconContent("GridAxisZ", "Toggle the visibility of the grid");
            public static GUIContent isolationModeExitButton = EditorGUIUtility.TrTextContent("Exit", "Exit isolation mode");
            public static GUIContent renderDocContent;
            public static GUIContent metalFrameCaptureContent = EditorGUIUtility.TrIconContent("FrameCapture", "Capture the current view and open in Xcode frame debugger");
            public static GUIContent sceneVisToolbarButtonContent = EditorGUIUtility.TrIconContent("SceneViewVisibility", "Number of hidden objects, click to toggle scene visibility");
            public static GUIStyle gizmoButtonStyle;
            public static GUIContent sceneViewCameraContent = EditorGUIUtility.TrIconContent("SceneViewCamera", "Settings for the Scene view camera.");
            public static GUIContent contributeGIOff = EditorGUIUtility.TrTextContent("Contribute GI Off");
            public static GUIContent receiveGILightmaps = EditorGUIUtility.TrTextContent("Contribute GI / Receive GI Lightmaps");
            public static GUIContent receiveGILightProbes = EditorGUIUtility.TrTextContent("Contribute GI / Receive GI Light Probes");

            static Styles()
            {
                gizmoButtonStyle = "GV Gizmo DropDown";
                renderDocContent = EditorGUIUtility.TrIconContent("FrameCapture", UnityEditor.RenderDocUtil.openInRenderDocLabel);
            }
        }

        double m_StartSearchFilterTime = -1;
        RenderTexture m_SceneTargetTexture;
        int m_MainViewControlID;

        public Camera camera { get { return m_Camera; } }

        [SerializeField]
        private Shader m_ReplacementShader;
        [SerializeField]
        private string m_ReplacementString;
        [SerializeField]
        private bool m_SceneVisActive = true;

        private string m_SceneVisHiddenCount = "0";

        OverlayWindow m_SceneVisOverlayWindow;
        OverlayWindow m_PBRSettingsOverlayWindow;
        OverlayWindow m_LightingExposureSettingsOverlayWindow;
        OverlayWindow m_GIContributorsReceiversOverlayWindow;
        OverlayWindow m_EditorToolsOverlayWindow;

        public void SetSceneViewShaderReplace(Shader shader, string replaceString)
        {
            m_ReplacementShader = shader;
            m_ReplacementString = replaceString;
        }

        internal bool m_ShowSceneViewWindows = false;
        SceneViewOverlay m_SceneViewOverlay;
        internal EditorCache m_DragEditorCache;

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
            return lastActiveSceneView.SendEvent(EditorGUIUtility.CommandEvent(EventCommandNames.FrameSelected));
        }

        [RequiredByNativeCode]
        public static bool FrameLastActiveSceneViewWithLock()
        {
            if (lastActiveSceneView == null)
                return false;
            return lastActiveSceneView.SendEvent(EditorGUIUtility.CommandEvent(EventCommandNames.FrameSelectedWithLock));
        }

        private static List<Camera> GetAllSceneCamerasAsList()
        {
            s_AllSceneCameraList.Clear();
            for (int i = 0; i < s_SceneViews.Count; ++i)
            {
                Camera cam = ((SceneView)s_SceneViews[i]).m_Camera;
                if (cam != null)
                    s_AllSceneCameraList.Add(cam);
            }

            return s_AllSceneCameraList;
        }

        public static Camera[] GetAllSceneCameras()
        {
            List<Camera> newSceneCameras = GetAllSceneCamerasAsList();
            if (newSceneCameras.Count == s_AllSceneCameras.Length)
            {
                bool cacheValid = true;
                for (int i = 0; i < newSceneCameras.Count; ++i)
                {
                    if (!Object.ReferenceEquals(s_AllSceneCameras[i], newSceneCameras[i]))
                    {
                        cacheValid = false;
                        break;
                    }
                }
                if (cacheValid)
                    return s_AllSceneCameras;
            }

            s_AllSceneCameras = newSceneCameras.ToArray();
            return s_AllSceneCameras;
        }

        public static void RepaintAll()
        {
            foreach (SceneView sv in s_SceneViews)
            {
                sv.Repaint();
            }
        }

        internal override void SetSearchFilter(string searchFilter, SearchMode mode, bool setAll, bool delayed)
        {
            if (m_SearchFilter == "" || searchFilter == "")
                m_StartSearchFilterTime = EditorApplication.timeSinceStartup;

            base.SetSearchFilter(searchFilter, mode, setAll, delayed);
        }

        internal void OnLostFocus()
        {
            if (s_LastActiveSceneView == this)
                SceneViewMotion.ResetMotion();
        }

        public override void OnEnable()
        {
            titleContent = GetLocalizedTitleContent();
            m_RectSelection = new RectSelection(this);
            SceneViewMotion.ResetDragState();

            if (m_Grid == null)
                m_Grid = new SceneViewGrid();

            sceneViewGrids.OnEnable(this);

            ResetGridPivot();

            if (svRot == null)
                svRot = new SceneViewRotation();
            svRot.Register(this);

            autoRepaintOnSceneChange = true;

            m_Rotation.valueChanged.AddListener(Repaint);
            m_Position.valueChanged.AddListener(Repaint);
            m_Size.valueChanged.AddListener(Repaint);
            m_Ortho.valueChanged.AddListener(Repaint);
            sceneViewGrids.gridVisibilityChanged += GridOnGridVisibilityChanged;

            wantsMouseMove = true;
            wantsLessLayoutEvents = true;
            wantsMouseEnterLeaveWindow = true;
            s_SceneViews.Add(this);

            m_SceneViewOverlay = new SceneViewOverlay(this);
            m_SceneVisOverlayWindow = new OverlayWindow(EditorGUIUtility.TrTextContent("Isolation View", ""),
                OverlayWindowGUI, (int)SceneViewOverlay.Ordering.ParticleEffect + 100,
                null,
                SceneViewOverlay.WindowDisplayOption.OneWindowPerTarget);
            m_PBRSettingsOverlayWindow = new OverlayWindow(EditorGUIUtility.TrTextContent("PBR Validation Settings"), DrawPBRSettings, (int)SceneViewOverlay.Ordering.PhysicsDebug, this, SceneViewOverlay.WindowDisplayOption.OneWindowPerTarget);
            m_LightingExposureSettingsOverlayWindow = new OverlayWindow(EditorGUIUtility.TrTextContent("Lighting Exposure"), DrawLightingExposureSettings, (int)SceneViewOverlay.Ordering.PhysicsDebug, this, SceneViewOverlay.WindowDisplayOption.OneWindowPerTarget);
            m_GIContributorsReceiversOverlayWindow = new OverlayWindow(EditorGUIUtility.TrTextContent("GI Contributors / Receivers"), DrawGIContributorsReceiversSettings, (int)SceneViewOverlay.Ordering.PhysicsDebug, this, SceneViewOverlay.WindowDisplayOption.OneWindowPerTarget);
            m_EditorToolsOverlayWindow = new OverlayWindow(EditorGUIUtility.TrTextContent("Tools"), EditorToolGUI.DoContextualToolbarOverlay, int.MaxValue, null, SceneViewOverlay.WindowDisplayOption.MultipleWindowsPerTarget);
            m_EditorToolsOverlayWindow.editorWindow = this;

            UpdateHiddenObjectCount();

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            EditorApplication.modifierKeysChanged += RepaintAll; // Because we show handles on shift
            SceneVisibilityManager.visibilityChanged += VisibilityChanged;
            SceneVisibilityManager.currentStageIsolated += CurrentStageIsolated;
            ActiveEditorTracker.editorTrackerRebuilt += OnEditorTrackerRebuilt;
            Selection.selectedObjectWasDestroyed += OnSelectedObjectWasDestroyed;
            Lightmapping.lightingDataUpdated += RepaintAll;

            m_DraggingLockedState = DraggingLockedState.NotDragging;

            CreateSceneCameraAndLights();

            if (m_2DMode)
                LookAt(pivot, Quaternion.identity, size, true, true);

            if (m_CameraMode.drawMode == DrawCameraMode.UserDefined && !userDefinedModes.Contains(m_CameraMode))
                AddCameraMode(m_CameraMode.name, m_CameraMode.section);

            base.OnEnable();

            if (SupportsStageHandling())
            {
                m_StageHandling = new SceneViewStageHandling(this);
                m_StageHandling.OnEnable();
            }

            s_ActiveEditorsDirty = true;

            showToolbar = ModeService.HasCapability("scene_view_toolbar", true);

            m_CameraViewVisualElement = CreateCameraRectVisualElement();

            rootVisualElement.Add(m_CameraViewVisualElement);

            Repaint();
        }

        VisualElement CreateCameraRectVisualElement()
        {
            var root = new VisualElement()
            {
                name = VisualElementUtils.GetUniqueName(s_CameraRectVisualElementName),
                pickingMode = PickingMode.Ignore, // do not eat events so IMGUI gets them
                viewDataKey = name,
                renderHints = RenderHints.ClipWithScissors
            };
            root.pseudoStates |= PseudoStates.Root;
            EditorUIService.instance.AddDefaultEditorStyleSheets(root);
            root.style.overflow = UnityEngine.UIElements.Overflow.Hidden;
            root.style.position = Position.Absolute;

            if (addCustomVisualElementToSceneView != null)
            {
                foreach (var del in addCustomVisualElementToSceneView.GetInvocationList())
                {
                    root.Add((VisualElement)del.DynamicInvoke(this));
                }
            }
            return root;
        }

        void OverlayWindowGUI(Object target, SceneView view)
        {
            if (GUILayout.Button(Styles.isolationModeExitButton, GUILayout.MinWidth(120)))
            {
                SceneVisibilityManager.instance.ExitIsolation();
            }
        }

        void GridOnGridVisibilityChanged(bool visible)
        {
            if (gridVisibilityChanged != null)
                gridVisibilityChanged(visible);
        }

        protected virtual bool SupportsStageHandling()
        {
            return true;
        }

        private void CurrentStageIsolated()
        {
            m_SceneVisActive = true;
            Repaint();
        }

        private void VisibilityChanged()
        {
            UpdateHiddenObjectCount();
            Repaint();
        }

        private void UpdateHiddenObjectCount()
        {
            int hiddenGameObjects = SceneVisibilityState.GetHiddenObjectCount();
            m_SceneVisHiddenCount = hiddenGameObjects.ToString();
        }

        public SceneView()
        {
            m_HierarchyType = HierarchyType.GameObjects;

            // Note: Rendering for Scene view picking depends on the depth buffer of the window
            depthBufferBits = 32;
        }

        internal void Awake()
        {
            if (string.IsNullOrEmpty(m_WindowGUID))
                m_WindowGUID = GUID.Generate().ToString();

            if (sceneViewState == null)
                m_SceneViewState = new SceneViewState();

            if (m_CameraSettings == null)
                m_CameraSettings = new CameraSettings();

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
                    view.MoveToView(go.transform);
            }
        }

        internal static Camera GetLastActiveSceneViewCamera()
        {
            SceneView view = s_LastActiveSceneView;
            return view ? view.camera : null;
        }

        public override void OnDisable()
        {
            EditorApplication.modifierKeysChanged -= RepaintAll;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            SceneVisibilityManager.visibilityChanged -= VisibilityChanged;
            SceneVisibilityManager.currentStageIsolated -= CurrentStageIsolated;
            Lightmapping.lightingDataUpdated -= RepaintAll;
            ActiveEditorTracker.editorTrackerRebuilt -= OnEditorTrackerRebuilt;
            Selection.selectedObjectWasDestroyed -= OnSelectedObjectWasDestroyed;
            sceneViewGrids.gridVisibilityChanged -= GridOnGridVisibilityChanged;

            sceneViewGrids.OnDisable(this);

            if (m_Camera)
                DestroyImmediate(m_Camera.gameObject, true);
            if (m_Light[0])
                DestroyImmediate(m_Light[0].gameObject, true);
            if (m_Light[1])
                DestroyImmediate(m_Light[1].gameObject, true);
            if (m_Light[2])
                DestroyImmediate(m_Light[2].gameObject, true);

            EditorSceneManager.ClosePreviewScene(m_CustomLightsScene);

            if (s_MipColorsTexture)
                DestroyImmediate(s_MipColorsTexture, true);

            if (m_ExposureTexture)
                DestroyImmediate(m_ExposureTexture, true);

            if (m_EmptyExposureTexture)
                DestroyImmediate(m_EmptyExposureTexture, true);

            s_SceneViews.Remove(this);
            if (s_LastActiveSceneView == this)
            {
                if (s_SceneViews.Count > 0)
                    s_LastActiveSceneView = s_SceneViews[0] as SceneView;
                else
                    s_LastActiveSceneView = null;
            }

            CleanupEditorDragFunctions();
            if (m_StageHandling != null)
                m_StageHandling.OnDisable();
            SceneViewMotion.DeactivateFlyModeContext();
            base.OnDisable();
        }

        public void OnDestroy()
        {
            if (audioPlay)
                audioPlay = false;
        }

        internal void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (audioPlay)
                audioPlay = false;
        }

        // This has to be called explicitly from SceneViewStageHandling to ensure,
        // this happens *after* SceneViewStageHandling has updated the camera scene.
        // Thus, we can't just register to the StageNavigationManager.instance.stageChanged
        // event since that would not guarantee the order dependency.
        internal void OnStageChanged(Stage previousStage, Stage newStage)
        {
            VisibilityChanged();
            // audioPlay may be different in the new stage,
            // so update regardless of whether it's on or off.
            // Not if we're in Play Mode however, as audio preview
            // is entirely disabled in that case.
            if (!EditorApplication.isPlaying)
                RefreshAudioPlay();

            RefreshToolbarHeight();
        }

        internal override void OnMaximized()
        {
            SceneViewMotion.ResetDragState();
            Repaint();
        }

        void RefreshToolbarHeight()
        {
            if (m_CameraViewVisualElement != null)
                m_CameraViewVisualElement.style.top = toolbarHeight;
        }

        void ToolbarDisplayStateGUI()
        {
            // render mode popup
            GUIContent modeContent = EditorGUIUtility.TextContent(cameraMode.name);
            modeContent.tooltip = L10n.Tr("The Draw Mode used to display the Scene.");
            Rect modeRect = GUILayoutUtility.GetRect(modeContent, EditorStyles.toolbarDropDownLeft, GUILayout.Width(120));
            if (EditorGUI.DropdownButton(modeRect, modeContent, FocusType.Passive, EditorStyles.toolbarDropDownLeft))
            {
                Rect rect = GUILayoutUtility.topLevel.GetLast();
                PopupWindow.Show(rect, new SceneRenderModeWindow(this));
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.Space();

            in2DMode = GUILayout.Toggle(in2DMode, Styles.mode2DContent, EditorStyles.toolbarButton);

            EditorGUILayout.Space();

            m_SceneIsLit = GUILayout.Toggle(m_SceneIsLit, Styles.lighting, EditorStyles.toolbarButton) || cameraMode.drawMode == DrawCameraMode.ShadowCascades;

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                audioPlay = GUILayout.Toggle(audioPlay, Styles.audioPlayContent, EditorStyles.toolbarButton);
            }

            var enabled = sceneViewState.fxEnabled;
            if (EditorGUILayout.DropDownToggle(ref enabled, Styles.fx, EditorStyles.toolbarDropDownToggle))
            {
                Rect rect = GUILayoutUtility.topLevel.GetLast();
                PopupWindow.Show(rect, new SceneFXWindow(this));
                GUIUtility.ExitGUI();
            }

            sceneViewState.fxEnabled = enabled;
        }

        void ToolbarGridDropdownGUI()
        {
            bool toggled = sceneViewGrids.showGrid;

            GUIContent gridIcon = GUIContent.none;
            switch (sceneViewGrids.gridAxis)
            {
                case SceneViewGrid.GridRenderAxis.X:
                    gridIcon = Styles.gridXToolbarContent;
                    break;

                case SceneViewGrid.GridRenderAxis.Y:
                    gridIcon = Styles.gridYToolbarContent;
                    break;

                case SceneViewGrid.GridRenderAxis.Z:
                    gridIcon = Styles.gridZToolbarContent;
                    break;
            }

            EditorGUI.BeginChangeCheck();
            if (EditorGUILayout.DropDownToggle(ref toggled, gridIcon, EditorStyles.toolbarDropDownToggle))
            {
                Rect rect = GUILayoutUtility.topLevel.GetLast();
                PopupWindow.Show(rect, new GridSettingsWindow(this));
                GUIUtility.ExitGUI();
            }

            if (EditorGUI.EndChangeCheck())
                sceneViewGrids.showGrid = toggled;
        }

        void ToolbarGizmosDropdownGUI()
        {
            bool toggled = drawGizmos;
            if (EditorGUILayout.DropDownToggle(ref toggled, Styles.gizmosContent, EditorStyles.toolbarDropDownToggle))
            {
                Rect rect = GUILayoutUtility.topLevel.GetLast();
                if (AnnotationWindow.ShowAtPosition(rect, false))
                {
                    GUIUtility.ExitGUI();
                }
            }
            else
            {
                drawGizmos = toggled;
            }
        }

        void ToolbarRenderDocGUI()
        {
            if (RenderDoc.IsLoaded())
            {
                using (new EditorGUI.DisabledScope(!RenderDoc.IsSupported()))
                {
                    if (GUILayout.Button(Styles.renderDocContent, EditorStyles.toolbarButton))
                    {
                        m_Parent.CaptureRenderDocScene();
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        void ToolbarMetalCaptureGUI()
        {
            bool canCapture = FrameCapture.IsDestinationSupported(FrameCaptureDestination.DevTools)
                || FrameCapture.IsDestinationSupported(FrameCaptureDestination.GPUTraceDocument);

            if (canCapture)
            {
                if (GUILayout.Button(Styles.metalFrameCaptureContent, EditorStyles.toolbarButton))
                {
                    m_Parent.CaptureMetalScene();
                    GUIUtility.ExitGUI();
                }
            }
        }

        void ToolbarSearchFieldGUI()
        {
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

            SearchFieldGUI(EditorGUILayout.kLabelFloatMaxW);
        }

        void DoToolbarGUI()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                ToolbarDisplayStateGUI();
                ToolbarSceneVisibilityGUI();
                ToolbarGridDropdownGUI();

                GUILayout.FlexibleSpace();

                ToolbarRenderDocGUI();
                ToolbarMetalCaptureGUI();
                ToolbarSceneToolsGUI();
                ToolbarSceneCameraGUI();
                ToolbarGizmosDropdownGUI();

                EditorGUILayout.Space();

                ToolbarSearchFieldGUI();
            }
            GUILayout.EndHorizontal();

            if (m_StageHandling != null && m_StageHandling.isShowingBreadcrumbBar)
                m_StageHandling.BreadcrumbGUI();
        }

        private void ToolbarSceneVisibilityGUI()
        {
            Styles.sceneVisToolbarButtonContent.text = m_SceneVisHiddenCount;
            m_SceneVisActive = GUILayout.Toggle(m_SceneVisActive, Styles.sceneVisToolbarButtonContent, EditorStyles.toolbarButton);
        }

        private void DoSceneVisibilityOverlay()
        {
            if (SceneVisibilityManager.instance.IsCurrentStageIsolated())
            {
                SceneViewOverlay.ShowWindow(m_SceneVisOverlayWindow);
            }
        }

        void ToolbarSceneToolsGUI()
        {
            displayToolModes = GUILayout.Toggle(displayToolModes, Styles.toolsContent, EditorStyles.toolbarButton);
        }

        void ToolbarSceneCameraGUI()
        {
            Rect sceneCameraRect = GUILayoutUtility.GetRect(Styles.sceneViewCameraContent, EditorStyles.toolbarDropDown);

            var evt = Event.current;
            if (evt.type == EventType.ContextClick && sceneCameraRect.Contains(evt.mousePosition))
                SceneViewCameraWindow.ShowContextMenu(this);

            if (EditorGUI.DropdownButton(sceneCameraRect, Styles.sceneViewCameraContent, FocusType.Passive, EditorStyles.toolbarDropDown))
            {
                PopupWindow.Show(sceneCameraRect, new SceneViewCameraWindow(this));
                GUIUtility.ExitGUI();
            }
        }

        // This method should be called after the audio play button has been toggled,
        // and after other events that require a refresh.
        void RefreshAudioPlay()
        {
            if ((s_AudioSceneView != null) && (s_AudioSceneView != this))
            {
                // turn *other* sceneview off
                if (s_AudioSceneView.m_PlayAudio)
                {
                    s_AudioSceneView.m_PlayAudio = false;
                    s_AudioSceneView.Repaint();
                }
            }

            // We have to find all loaded AudioSources, not just the ones in main scenes.
            var sources = (AudioSource[])Resources.FindObjectsOfTypeAll(typeof(AudioSource));
            foreach (AudioSource source in sources)
            {
                if (EditorUtility.IsPersistent(source))
                    continue;

                if (source.playOnAwake)
                {
                    if (!m_PlayAudio || !StageUtility.IsGameObjectRenderedByCamera(source.gameObject, m_Camera))
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

            // We have to find all loaded ReverbZones, not just the ones in main scenes.
            var zones = (AudioReverbZone[])Resources.FindObjectsOfTypeAll(typeof(AudioReverbZone));
            foreach (AudioReverbZone zone in zones)
            {
                if (EditorUtility.IsPersistent(zone))
                    continue;

                zone.active = m_PlayAudio && StageUtility.IsGameObjectRenderedByCamera(zone.gameObject, m_Camera);
            }

            AudioUtil.SetListenerTransform(m_PlayAudio ? m_Camera.transform : null);

            s_AudioSceneView = this;


            if (m_PlayAudio)
            {
                AudioMixerWindow.RepaintAudioMixerWindow();
            }
        }

        void OnSelectionChange()
        {
            if (Selection.activeObject != null && m_LastLockedObject != Selection.activeObject)
                viewIsLockedToObject = false;

            m_WasFocused = false;

            Repaint();
        }

        private void LoadRenderDoc()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                ShaderUtil.RequestLoadRenderDoc();
            }
        }

        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            if (RenderDoc.IsInstalled() && !RenderDoc.IsLoaded())
            {
                menu.AddItem(EditorGUIUtility.TrTextContent(UnityEditor.RenderDocUtil.loadRenderDocLabel), false, LoadRenderDoc);
            }
        }

        [MenuItem("GameObject/Set as first sibling %=")]
        internal static void MenuMoveToFront()
        {
            foreach (Transform t in Selection.transforms)
            {
                Undo.SetTransformParent(t, t.parent, "Set as first sibling");
                t.SetAsFirstSibling();
            }
        }

        [MenuItem("GameObject/Set as first sibling %=", true)]
        internal static bool ValidateMenuMoveToFront()
        {
            if (Selection.activeTransform != null)
            {
                Transform parent = Selection.activeTransform.parent;
                return (parent != null && parent.GetChild(0) != Selection.activeTransform);
            }
            return false;
        }

        [MenuItem("GameObject/Set as last sibling %-")]
        internal static void MenuMoveToBack()
        {
            foreach (Transform t in Selection.transforms)
            {
                Undo.SetTransformParent(t, t.parent, "Set as last sibling");
                t.SetAsLastSibling();
            }
        }

        [MenuItem("GameObject/Set as last sibling %-", true)]
        internal static bool ValidateMenuMoveToBack()
        {
            if (Selection.activeTransform != null)
            {
                Transform parent = Selection.activeTransform.parent;
                return (parent != null && parent.GetChild(parent.childCount - 1) != Selection.activeTransform);
            }
            return false;
        }

        [MenuItem("GameObject/Move To View %&f")]
        internal static void MenuMoveToView()
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
        internal static void MenuAlignWithView()
        {
            if (ValidateAlignWithView())
                s_LastActiveSceneView.AlignWithView();
        }

        [MenuItem("GameObject/Align With View %#f", true)]
        internal static bool ValidateAlignWithView()
        {
            return s_LastActiveSceneView != null && (Selection.activeTransform != null);
        }

        [MenuItem("GameObject/Align View to Selected")]
        internal static void MenuAlignViewToSelected()
        {
            if (ValidateAlignViewToSelected())
                s_LastActiveSceneView.AlignViewToObject(Selection.activeTransform);
        }

        [MenuItem("GameObject/Align View to Selected", true)]
        internal static bool ValidateAlignViewToSelected()
        {
            return s_LastActiveSceneView != null && (Selection.activeTransform != null);
        }

        [MenuItem("GameObject/Toggle Active State &#a")]
        internal static void ActivateSelection()
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
        internal static bool ValidateActivateSelection()
        {
            return (Selection.activeTransform != null);
        }

        private static void CreateMipColorsTexture()
        {
            if (s_MipColorsTexture)
                return;
            s_MipColorsTexture = new Texture2D(32, 32, TextureFormat.RGBA32, true) {hideFlags = HideFlags.HideAndDontSave};
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

        private bool m_ForceSceneViewFiltering;
        private bool m_ForceSceneViewFilteringForLodGroupEditing;
        private bool m_ForceSceneViewFilteringForStageHandling;
        private double m_lastRenderedTime;

        internal void SetSceneViewFiltering(bool enable)
        {
            m_ForceSceneViewFiltering = enable;
        }

        internal void SetSceneViewFilteringForLODGroups(bool enable)
        {
            m_ForceSceneViewFilteringForLodGroupEditing = enable;
        }

        internal void SetSceneViewFilteringForStages(bool enable)
        {
            m_ForceSceneViewFilteringForStageHandling = enable;
        }

        bool forceSceneViewFilteringForLodGroupEditing => m_ForceSceneViewFilteringForLodGroupEditing && s_PreferenceEnableFilteringWhileLodGroupEditing;

        bool UseSceneFiltering()
        {
            return (!string.IsNullOrEmpty(m_SearchFilter) && s_PreferenceEnableFilteringWhileSearching) || forceSceneViewFilteringForLodGroupEditing || m_ForceSceneViewFilteringForStageHandling || m_ForceSceneViewFiltering;
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
                if (!sceneViewState.fogEnabled)
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
            // make sure we actually support R16G16B16A16_SFloat
            GraphicsFormat format = (hdr && SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, FormatUsage.Render)) ? GraphicsFormat.R16G16B16A16_SFloat : SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);

            if (m_SceneTargetTexture != null)
            {
                if (m_SceneTargetTexture.graphicsFormat != format)
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
                m_SceneTargetTexture = new RenderTexture(0, 0, 24, format)
                {
                    name = "SceneView RT",
                    antiAliasing = 1,
                    hideFlags = HideFlags.HideAndDontSave
                };
            }
            if (m_SceneTargetTexture.width != width || m_SceneTargetTexture.height != height)
            {
                m_SceneTargetTexture.Release();
                m_SceneTargetTexture.width = width;
                m_SceneTargetTexture.height = height;
            }
            m_SceneTargetTexture.Create();
        }

        public bool IsCameraDrawModeEnabled(CameraMode mode)
        {
            if (!Handles.IsCameraDrawModeEnabled(m_Camera, mode.drawMode))
                return false;
            return (onValidateCameraMode == null ||
                onValidateCameraMode.GetInvocationList().All(validate => ((Func<CameraMode, bool>)validate)(mode)));
        }

        internal bool IsSceneCameraDeferred()
        {
            bool usingScriptableRenderPipeline = (GraphicsSettings.currentRenderPipeline != null);
            if (m_Camera == null || usingScriptableRenderPipeline)
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
            return mode == DrawCameraMode.Textured || mode == DrawCameraMode.TexturedWire;
        }

        private void PrepareCameraTargetTexture(Rect cameraRect)
        {
            // Always render camera into a RT
            bool hdr = SceneViewIsRenderingHDR();
            CreateCameraTargetTexture(cameraRect, hdr);
            m_Camera.targetTexture = m_SceneTargetTexture;

            // Do not use deferred rendering when using search filtering or wireframe/overdraw/mipmaps rendering modes.
            if (UseSceneFiltering() || !DoesCameraDrawModeSupportDeferred(m_CameraMode.drawMode))
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
            Handles.SetSceneViewColors(kSceneViewWire, kSceneViewWireOverlay, kSceneViewSelectedOutline, kSceneViewSelectedChildrenOutline, kSceneViewSelectedWire);

            // Setup shader replacement if needed by overlay mode
            if (m_CameraMode.drawMode == DrawCameraMode.Overdraw)
            {
                // show overdraw
                if (!s_ShowOverdrawShader)
                    s_ShowOverdrawShader = EditorGUIUtility.LoadRequired("SceneView/SceneViewShowOverdraw.shader") as Shader;
                m_Camera.SetReplacementShader(s_ShowOverdrawShader, "RenderType");
            }
            else if (m_CameraMode.drawMode == DrawCameraMode.Mipmaps)
            {
                Texture.SetStreamingTextureMaterialDebugProperties();

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
            else if (m_CameraMode.drawMode == DrawCameraMode.TextureStreaming)
            {
                Texture.SetStreamingTextureMaterialDebugProperties();

                // show mip levels
                if (!s_ShowTextureStreamingShader)
                    s_ShowTextureStreamingShader = EditorGUIUtility.LoadRequired("SceneView/SceneViewShowTextureStreaming.shader") as Shader;
                if (s_ShowTextureStreamingShader != null && s_ShowTextureStreamingShader.isSupported)
                {
                    m_Camera.SetReplacementShader(s_ShowTextureStreamingShader, "RenderType");
                }
                else
                {
                    m_Camera.SetReplacementShader(m_ReplacementShader, m_ReplacementString);
                }
            }
            else
            {
                m_Camera.SetReplacementShader(m_ReplacementShader, m_ReplacementString);
            }
        }

        bool SceneCameraRendersIntoRT()
        {
            return m_Camera.targetTexture != null;
        }

        private void DoDrawCamera(Rect windowSpaceCameraRect, Rect groupSpaceCameraRect, out bool pushedGUIClip)
        {
            pushedGUIClip = false;
            if (!m_Camera.gameObject.activeInHierarchy)
                return;

            bool oldAsync = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = EditorSettings.asyncShaderCompilation;

            DrawGridParameters gridParam = sceneViewGrids.PrepareGridRender(camera, pivot, m_Rotation.target, size, m_Ortho.target);
            if (m_SkipFadingPending)
                sceneViewGrids.SkipFading(); // Called AFTER fade target values have been updated.

            Event evt = Event.current;
            if (UseSceneFiltering())
            {
                if (evt.type == EventType.Repaint)
                    RenderFilteredScene(groupSpaceCameraRect);

                if (evt.type == EventType.Repaint)
                    RenderTexture.active = null;
                GUI.EndGroup();

                GUI.BeginGroup(windowSpaceCameraRect);
                if (evt.type == EventType.Repaint)
                    Graphics.DrawTexture(groupSpaceCameraRect, m_SceneTargetTexture, new Rect(0, 0, 1, 1), 0, 0, 0, 0, GUI.color, GUI.blitMaterial);
                Handles.SetCamera(groupSpaceCameraRect, m_Camera);
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
                Handles.DrawCameraStep1(groupSpaceCameraRect, m_Camera, m_CameraMode.drawMode, gridParam, drawGizmos);

                DrawRenderModeOverlay(groupSpaceCameraRect);
            }
            ShaderUtil.allowAsyncCompilation = oldAsync;
        }

        void RenderFilteredScene(Rect groupSpaceCameraRect)
        {
            var oldRenderingPath = m_Camera.renderingPath;

            // First pass: Draw the scene normally in destination render texture, save color buffer for later
            DoClearCamera(groupSpaceCameraRect);
            Handles.DrawCamera(groupSpaceCameraRect, m_Camera, m_CameraMode.drawMode, drawGizmos);

            var colorDesc = m_SceneTargetTexture.descriptor;
            colorDesc.depthBufferBits = 0;
            var colorRT = RenderTexture.GetTemporary(colorDesc);
            colorRT.name = "SavedColorRT";
            Graphics.Blit(m_SceneTargetTexture, colorRT);

            // Second pass: Blit the scene faded out in the scene target texture
            float fade = UseSceneFiltering() ? 1f : Mathf.Clamp01((float)(EditorApplication.timeSinceStartup - m_StartSearchFilterTime));
            if (!s_FadeMaterial)
                s_FadeMaterial = EditorGUIUtility.LoadRequired("SceneView/SceneViewGrayscaleEffectFade.mat") as Material;
            s_FadeMaterial.SetFloat("_Fade", fade);
            Graphics.Blit(colorRT, m_SceneTargetTexture, s_FadeMaterial);

            // Third pass: Draw aura for objects which meet the search filter, but are occluded. Save color buffer for later.
            m_Camera.renderingPath = RenderingPath.Forward;
            if (!s_AuraShader)
                s_AuraShader = EditorGUIUtility.LoadRequired("SceneView/SceneViewAura.shader") as Shader;
            m_Camera.SetReplacementShader(s_AuraShader, "");
            Handles.SetCameraFilterMode(m_Camera, Handles.CameraFilterMode.ShowFiltered);
            Handles.DrawCamera(groupSpaceCameraRect, m_Camera, m_CameraMode.drawMode, drawGizmos);

            var fadedDesc = m_SceneTargetTexture.descriptor;
            colorDesc.depthBufferBits = 0;
            var fadedRT = RenderTexture.GetTemporary(fadedDesc);
            fadedRT.name = "FadedColorRT";
            Graphics.Blit(m_SceneTargetTexture, fadedRT);

            // Fourth pass: Draw objects which do meet filter in a mask
            RenderTexture.active = m_SceneTargetTexture;
            GL.Clear(false, true, Color.clear);
            m_Camera.ResetReplacementShader();
            Handles.DrawCamera(groupSpaceCameraRect, m_Camera, m_CameraMode.drawMode, drawGizmos);

            // Final pass: Blit the faded scene where the mask isn't set
            if (!s_ApplyFilterMaterial)
                s_ApplyFilterMaterial = EditorGUIUtility.LoadRequired("SceneView/SceneViewApplyFilter.mat") as Material;
            s_ApplyFilterMaterial.SetTexture("_MaskTex", m_SceneTargetTexture);
            Graphics.Blit(fadedRT, colorRT, s_ApplyFilterMaterial);
            Graphics.Blit(colorRT, m_SceneTargetTexture);

            RenderTexture.ReleaseTemporary(colorRT);
            RenderTexture.ReleaseTemporary(fadedRT);

            // Reset camera
            m_Camera.SetReplacementShader(m_ReplacementShader, m_ReplacementString);
            m_Camera.renderingPath = oldRenderingPath;

            if (fade < 1)
                Repaint();
        }

        void DoClearCamera(Rect cameraRect)
        {
            // Clear (color/skybox)
            // We do funky FOV interpolation when switching between ortho and perspective. However,
            // for the skybox we always want to use the same FOV.
            float skyboxFOV = GetVerticalFOV(m_CameraSettings.fieldOfView);
            float realFOV = m_Camera.fieldOfView;

            var clearFlags = m_Camera.clearFlags;
            if (GraphicsSettings.currentRenderPipeline != null)
                m_Camera.clearFlags = CameraClearFlags.Color;
            m_Camera.fieldOfView = skyboxFOV;
            Handles.ClearCamera(cameraRect, m_Camera);
            m_Camera.clearFlags = clearFlags;
            m_Camera.fieldOfView = realFOV;
        }

        void SetupCustomSceneLighting()
        {
            if (m_SceneIsLit)
                return;
            m_Light[0].transform.rotation = m_Camera.transform.rotation;
            if (Event.current.type == EventType.Repaint)
                InternalEditorUtility.SetCustomLighting(m_Light, kSceneViewMidLight);
        }

        void CleanupCustomSceneLighting()
        {
            if (m_SceneIsLit)
                return;
            if (Event.current.type == EventType.Repaint)
                InternalEditorUtility.RemoveCustomLighting();
        }

        // Give editors a chance to kick in. Disable in search mode, editors rendering to the scene
        void HandleViewToolCursor(Rect cameraRect)
        {
            if (!Tools.viewToolActive || Event.current.type != EventType.Repaint)
                return;

            var cursor = MouseCursor.Arrow;
            switch (Tools.viewTool)
            {
                case ViewTool.Pan:
                    cursor = MouseCursor.Pan;
                    break;
                case ViewTool.Orbit:
                    cursor = MouseCursor.Orbit;
                    break;
                case ViewTool.FPS:
                    cursor = MouseCursor.FPS;
                    break;
                case ViewTool.Zoom:
                    cursor = MouseCursor.Zoom;
                    break;
            }
            if (cursor != MouseCursor.Arrow)
                AddCursorRect(cameraRect, cursor);
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

        private GUIStyle m_NoContributeGIStyle = null;
        private GUIStyle m_ReceiveGILightmapsStyle = null;
        private GUIStyle m_ReceiveGILightProbesStyle = null;

        void UpdateSwatchTexture(Texture2D t, Color c, bool colorCorrect = false)
        {
            // Some colors were created in gamma space and should be viewed as such when in linear
            if (PlayerSettings.colorSpace == ColorSpace.Linear && colorCorrect)
            {
                c = c.gamma; // offset linear to gamma correction that happens in IMGUI, by doing the inverse beforehand
            }
            c.a = 1.0f;
            t.SetPixel(0, 0, c);
            t.Apply();
        }

        GUIStyle CreateSwatchStyleForColor(Color c, bool colorCorrect = false)
        {
            Texture2D t = new Texture2D(1, 1);
            UpdateSwatchTexture(t, c, colorCorrect);
            return new GUIStyle {normal = {background = t}};
        }

        String CreateSwatchDescriptionForName(float minLum, float maxLum)
        {
            return "Luminance (" + minLum.ToString("F2", CultureInfo.InvariantCulture.NumberFormat) + " - " + maxLum.ToString("F2", CultureInfo.InvariantCulture.NumberFormat) + ")";
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

            m_AlbedoSwatchColorStyles[0] = CreateSwatchStyleForColor(Color.gray, true);
            m_AlbedoSwatchDescriptions[0] = "Default Luminance";
            m_AlbedoSwatchGUIContent[0] = new GUIContent(m_AlbedoSwatchDescriptions[0]);
            m_AlbedoSwatchLuminanceStrings[0] = CreateSwatchDescriptionForName(0.012f, 0.9f);
            for (int i = 1; i < m_AlbedoSwatchInfos.Length + 1; i++)
            {
                m_AlbedoSwatchColorStyles[i] = CreateSwatchStyleForColor(m_AlbedoSwatchInfos[i - 1].color, true);
                m_AlbedoSwatchDescriptions[i] = m_AlbedoSwatchInfos[i - 1].name;
                m_AlbedoSwatchGUIContent[i] = new GUIContent(m_AlbedoSwatchDescriptions[i]);
                m_AlbedoSwatchLuminanceStrings[i] = CreateSwatchDescriptionForName(m_AlbedoSwatchInfos[i - 1].minLuminance, m_AlbedoSwatchInfos[i - 1].maxLuminance);
            }
        }

        void UpdatePBRColorLegend()
        {
            if (m_TooLowColorStyle == null || m_TooLowColorStyle.normal.background == null)
            {
                m_TooLowColorStyle = CreateSwatchStyleForColor(kSceneViewMaterialValidateLow.Color);
                m_TooHighColorStyle = CreateSwatchStyleForColor(kSceneViewMaterialValidateHigh.Color);
                m_PureMetalColorStyle = CreateSwatchStyleForColor(kSceneViewMaterialValidatePureMetal.Color);
            }
            else
            {
                UpdateSwatchTexture(m_TooLowColorStyle.normal.background, kSceneViewMaterialValidateLow.Color);
                UpdateSwatchTexture(m_TooHighColorStyle.normal.background, kSceneViewMaterialValidateHigh.Color);
                UpdateSwatchTexture(m_PureMetalColorStyle.normal.background, kSceneViewMaterialValidatePureMetal.Color);
            }

            if (PlayerSettings.colorSpace == ColorSpace.Linear)
            {
                Shader.SetGlobalColor("unity_MaterialValidateLowColor", kSceneViewMaterialValidateLow.Color.linear);
                Shader.SetGlobalColor("unity_MaterialValidateHighColor", kSceneViewMaterialValidateHigh.Color.linear);
                Shader.SetGlobalColor("unity_MaterialValidatePureMetalColor", kSceneViewMaterialValidatePureMetal.Color.linear);
            }
            else
            {
                Shader.SetGlobalColor("unity_MaterialValidateLowColor", kSceneViewMaterialValidateLow.Color);
                Shader.SetGlobalColor("unity_MaterialValidateHighColor", kSceneViewMaterialValidateHigh.Color);
                Shader.SetGlobalColor("unity_MaterialValidatePureMetalColor", kSceneViewMaterialValidatePureMetal.Color);
            }
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
            Shader.SetGlobalInt("_CheckPureMetal", m_DoValidateTrueMetals ? 1 : 0);
        }

        internal void DrawTrueMetalCheckbox()
        {
            EditorGUI.BeginChangeCheck();
            m_DoValidateTrueMetals = EditorGUILayout.ToggleLeft(EditorGUIUtility.TrTextContent("Check Pure Metals", "Check if albedo is black for materials with an average specular color above 0.45"), m_DoValidateTrueMetals);
            if (EditorGUI.EndChangeCheck())
            {
                Shader.SetGlobalInt("_CheckPureMetal", m_DoValidateTrueMetals ? 1 : 0);
            }
        }

        internal void DrawPBRSettingsForScene()
        {
            if (m_CameraMode.drawMode == DrawCameraMode.ValidateAlbedo)
            {
                if (PlayerSettings.colorSpace == ColorSpace.Gamma)
                {
                    EditorGUILayout.HelpBox("Albedo Validation doesn't work when Color Space is set to gamma space", MessageType.Warning);
                }

                EditorGUIUtility.labelWidth = 140;

                m_SelectedAlbedoSwatchIndex = EditorGUILayout.Popup(EditorGUIUtility.TrTextContent("Luminance Validation:", "Select default luminance validation or validate against a configured albedo swatch"), m_SelectedAlbedoSwatchIndex, m_AlbedoSwatchGUIContent);
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
                        m_AlbedoSwatchHueTolerance = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent("Hue Tolerance:", "Check that the hue of the albedo value of a material is within the tolerance of the hue of the albedo swatch being validated against"), m_AlbedoSwatchHueTolerance, 0f, 0.5f);

                        m_AlbedoSwatchSaturationTolerance = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent("Saturation Tolerance:", "Check that the saturation of the albedo value of a material is within the tolerance of the saturation of the albedo swatch being validated against"), m_AlbedoSwatchSaturationTolerance, 0f, 0.5f);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        UpdateAlbedoSwatch();
                    }
                }
            }

            EditorGUILayout.LabelField("Color Legend:");
            EditorGUI.indentLevel++;
            string modeString;

            if (m_CameraMode.drawMode == DrawCameraMode.ValidateAlbedo)
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

        void PrepareValidationUI()
        {
            if (m_AlbedoSwatchInfos == null)
            {
                CreateAlbedoSwatchData();
                UpdatePBRColorLegend();
            }

            if (PlayerSettings.colorSpace != m_LastKnownColorSpace)
            {
                UpdateAlbedoSwatchGUI();
                UpdateAlbedoSwatch();
                UpdatePBRColorLegend();
            }
        }

        static void DrawPBRSettings(Object target, SceneView sceneView)
        {
            sceneView.DrawTrueMetalCheckbox();
            sceneView.DrawPBRSettingsForScene();
        }

        internal void DrawLightingExposureSlider()
        {
            m_ExposureSliderValue = EditorGUIInternal.ExposureSlider(m_ExposureSliderValue, ref m_ExposureSliderMax, EditorStyles.toolbarSlider);

            Unsupported.SetSceneViewDebugModeExposureNoDirty(m_ExposureSliderValue);
        }

        static void DrawLightingExposureSettings(Object target, SceneView sceneView)
        {
            sceneView.DrawLightingExposureSlider();
        }

        void UpdateGIContributorsReceiversColors()
        {
            if (m_NoContributeGIStyle == null || m_NoContributeGIStyle.normal.background == null)
            {
                m_NoContributeGIStyle = CreateSwatchStyleForColor(kSceneViewMaterialNoContributeGI.Color);
                m_ReceiveGILightmapsStyle = CreateSwatchStyleForColor(kSceneViewMaterialReceiveGILightmaps.Color);
                m_ReceiveGILightProbesStyle = CreateSwatchStyleForColor(kSceneViewMaterialReceiveGILightProbes.Color);
            }
            else
            {
                UpdateSwatchTexture(m_NoContributeGIStyle.normal.background, kSceneViewMaterialNoContributeGI.Color);
                UpdateSwatchTexture(m_ReceiveGILightmapsStyle.normal.background, kSceneViewMaterialReceiveGILightmaps.Color);
                UpdateSwatchTexture(m_ReceiveGILightProbesStyle.normal.background, kSceneViewMaterialReceiveGILightProbes.Color);
            }

            Handles.SetSceneViewModeGIContributorsReceiversColors(kSceneViewMaterialNoContributeGI.Color, kSceneViewMaterialReceiveGILightmaps.Color, kSceneViewMaterialReceiveGILightProbes.Color);
        }

        internal void DrawGIContributorsReceiversSettings()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUIUtility.labelWidth = 2;
                EditorGUILayout.LabelField("", m_NoContributeGIStyle);
                EditorGUIUtility.labelWidth = 200;
                EditorGUILayout.LabelField(Styles.contributeGIOff);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUIUtility.labelWidth = 2;
                EditorGUILayout.LabelField("", m_ReceiveGILightmapsStyle);
                EditorGUIUtility.labelWidth = 200;
                EditorGUILayout.LabelField(Styles.receiveGILightmaps);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUIUtility.labelWidth = 2;
                EditorGUILayout.LabelField("", m_ReceiveGILightProbesStyle);
                EditorGUIUtility.labelWidth = 200;
                EditorGUILayout.LabelField(Styles.receiveGILightProbes);
            }
        }

        static void DrawGIContributorsReceiversSettings(Object target, SceneView sceneView)
        {
            sceneView.DrawGIContributorsReceiversSettings();
        }

        void DrawSceneViewSwatch()
        {
            if (this.cameraMode.drawMode == DrawCameraMode.ValidateAlbedo || this.cameraMode.drawMode == DrawCameraMode.ValidateMetalSpecular)
            {
                PrepareValidationUI();
                SceneViewOverlay.ShowWindow(m_PBRSettingsOverlayWindow);
            }

            if (this.showExposureSettings)
            {
                SceneViewOverlay.ShowWindow(m_LightingExposureSettingsOverlayWindow);
            }

            if (this.cameraMode.drawMode == DrawCameraMode.GIContributorsReceivers)
            {
                UpdateGIContributorsReceiversColors();
                SceneViewOverlay.ShowWindow(m_GIContributorsReceiversOverlayWindow);
            }
        }

        void UpdateGizmoExposure()
        {
            if (this.showExposureSettings)
            {
                if (m_ExposureTexture == null)
                    m_ExposureTexture = new Texture2D(1, 1, GraphicsFormat.R32G32_SFloat, TextureCreationFlags.None);

                m_ExposureTexture.SetPixel(0, 0, new Color(Mathf.Pow(2.0f, m_ExposureSliderValue), 0.0f, 0.0f));
                m_ExposureTexture.Apply();

                Gizmos.exposure = m_ExposureTexture;
            }
            else
            {
                if (m_EmptyExposureTexture == null)
                {
                    m_EmptyExposureTexture = new Texture2D(1, 1, GraphicsFormat.R32G32_SFloat, TextureCreationFlags.None);
                    m_EmptyExposureTexture.SetPixel(0, 0, new Color(1.0f, 0.0f, 0.0f));
                    m_EmptyExposureTexture.Apply();
                }

                Gizmos.exposure = m_EmptyExposureTexture;
            }
        }

        void RepaintGizmosThatAreRenderedOnTopOfSceneView()
        {
            if (Event.current.type == EventType.Repaint)
            {
                svRot.OnGUI(this);
            }
        }

        void InputForGizmosThatAreRenderedOnTopOfSceneView()
        {
            if (Event.current.type != EventType.Repaint)
            {
                svRot.OnGUI(this);
                if (m_SkipFadingPending)
                    svRot.SkipFading(); // Called AFTER fade target values have been updated.
            }
        }

        protected virtual void OnGUI()
        {
            onGUIStarted?.Invoke(this);

            s_CurrentDrawingSceneView = this;

            Event evt = Event.current;

            if (evt.type == EventType.Repaint)
            {
                s_MouseRects.Clear();
                Tools.InvalidateHandlePosition(); // Some cases that should invalidate the cached position are not handled correctly yet so we refresh it once per frame
            }

            sceneViewGrids.UpdateGridColor();

            Color origColor = GUI.color;
            Rect origCameraRect = m_Camera.rect;
            Rect windowSpaceCameraRect = cameraRect;

            HandleClickAndDragToFocus();

            m_ShowSceneViewWindows = (lastActiveSceneView == this);
            m_SceneViewOverlay.Begin();

            bool oldFog;
            float oldShadowDistance;
            SetupFogAndShadowDistance(out oldFog, out oldShadowDistance);

            if (showToolbar)
                DoToolbarGUI();
            GUI.skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);

            // Don't apply any playmode tinting to scene views
            GUI.color = Color.white;

            EditorGUIUtility.labelWidth = 100;

            SetupCamera();
            RenderingPath oldRenderingPath = m_Camera.renderingPath;

            // Use custom scene RenderSettings (if currently showing a custom scene)
            bool restoreOverrideRenderSettings = false;
            if (m_CustomScene.IsValid())
                restoreOverrideRenderSettings = Unsupported.SetOverrideLightingSettings(m_CustomScene);

            if (m_StageHandling != null)
                m_StageHandling.StartOnGUI();

            SetupCustomSceneLighting();

            GUI.BeginGroup(windowSpaceCameraRect);

            Rect groupSpaceCameraRect = new Rect(0, 0, windowSpaceCameraRect.width, windowSpaceCameraRect.height);
            Rect groupSpaceCameraRectInPixels = EditorGUIUtility.PointsToPixels(groupSpaceCameraRect);

            HandleViewToolCursor(windowSpaceCameraRect);

            PrepareCameraTargetTexture(groupSpaceCameraRectInPixels);
            DoClearCamera(groupSpaceCameraRectInPixels);

            m_Camera.cullingMask = Tools.visibleLayers;

            InputForGizmosThatAreRenderedOnTopOfSceneView();

            DoOnPreSceneGUICallbacks(groupSpaceCameraRectInPixels);

            PrepareCameraReplacementShader();

            // Unfocus search field on mouse clicks into content, so that key presses work to navigate.
            m_MainViewControlID = GUIUtility.GetControlID(FocusType.Keyboard);
            if (evt.GetTypeForControl(m_MainViewControlID) == EventType.MouseDown && groupSpaceCameraRect.Contains(evt.mousePosition))
                GUIUtility.keyboardControl = m_MainViewControlID;

            // Draw camera
            bool pushedGUIClip;
            DoDrawCamera(windowSpaceCameraRect, groupSpaceCameraRect, out pushedGUIClip);

            DoSceneVisibilityOverlay();

            CleanupCustomSceneLighting();

            if (restoreOverrideRenderSettings)
                Unsupported.RestoreOverrideLightingSettings();

            //Ensure that the target texture is clamped [0-1]
            //This is needed because otherwise gizmo rendering gets all
            //messed up (think HDR target with value of 50 + alpha blend gizmo... gonna be white!)

            bool hdrDisplayActive = (m_Parent != null && m_Parent.actualView == this && m_Parent.hdrActive);
            if (!UseSceneFiltering() && evt.type == EventType.Repaint && GraphicsFormatUtility.IsIEEE754Format(m_SceneTargetTexture.graphicsFormat) && !hdrDisplayActive)
            {
                var currentDepthBuffer = Graphics.activeDepthBuffer;
                var rtDesc = m_SceneTargetTexture.descriptor;
                rtDesc.graphicsFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
                rtDesc.depthBufferBits = 0;
                RenderTexture ldrSceneTargetTexture = RenderTexture.GetTemporary(rtDesc);
                ldrSceneTargetTexture.name = "LDRSceneTarget";
                Graphics.Blit(m_SceneTargetTexture, ldrSceneTargetTexture);
                Graphics.Blit(ldrSceneTargetTexture, m_SceneTargetTexture);
                Graphics.SetRenderTarget(m_SceneTargetTexture.colorBuffer, currentDepthBuffer);
                RenderTexture.ReleaseTemporary(ldrSceneTargetTexture);
            }

            if (!UseSceneFiltering())
            {
                // Blit to final target RT in deferred mode
                if (m_Camera.gameObject.activeInHierarchy)
                    Handles.DrawCameraStep2(m_Camera, m_CameraMode.drawMode, drawGizmos);
            }

            RestoreFogAndShadowDistance(oldFog, oldShadowDistance);

            m_Camera.renderingPath = oldRenderingPath;

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
                    Graphics.DrawTexture(groupSpaceCameraRect, m_SceneTargetTexture, new Rect(0, 0, 1, 1), 0, 0, 0, 0, GUI.color, EditorGUIUtility.GUITextureBlit2SRGBMaterial);
                    Profiler.EndSample();
                }
            }

            // By this time the 3D scene is done being drawn, and we're left with gizmos, handles and SceneViewGUI stuff.
            // Reusing the same 3D scene render target, we draw those things and blit them on the back buffer without
            // doing sRGB conversions on them since they were always intended to draw without sRGB conversions.
            GUIClip.Push(new Rect(0f, 0f, m_SceneTargetTexture.width, m_SceneTargetTexture.height), Vector2.zero, Vector2.zero, true);

            if (evt.type == EventType.Repaint)
            {
                Graphics.SetRenderTarget(m_SceneTargetTexture);
                GL.Clear(false, true, new Color(0, 0, 0, 0)); // Only clear color. Keep depth intact.
            }

            if (displayToolModes)
            {
                SceneViewOverlay.ShowWindow(m_EditorToolsOverlayWindow);
            }

            DrawSceneViewSwatch();
            UpdateGizmoExposure();

            // Calling OnSceneGUI before DefaultHandles, so users can use events before the Default Handles
            HandleSelectionAndOnSceneGUI();

            // Draw default scene manipulation tools (Move/Rotate/...)
            DefaultHandles();

            // Handle scene view motion when this scene view is active (always after duringSceneGui and Tools, so that
            // user tools can access RMB and alt keys if they want to override the event)
            // Do not pass the camera transform to the SceneViewMotion calculations.
            // The camera transform is calculation *output* not *input*.
            // Avoiding using it as input too avoids errors accumulating.
            SceneViewMotion.DoViewTool(this);

            Handles.SetCameraFilterMode(Camera.current, UseSceneFiltering() ? Handles.CameraFilterMode.ShowFiltered : Handles.CameraFilterMode.Off);

            // Handle scene commands after EditorTool.OnSceneGUI so that tools can handle commands
            if (evt.type == EventType.ExecuteCommand || evt.type == EventType.ValidateCommand || evt.keyCode == KeyCode.Escape)
                CommandsGUI();

            Handles.SetCameraFilterMode(Camera.current, Handles.CameraFilterMode.Off);
            Handles.SetCameraFilterMode(m_Camera, Handles.CameraFilterMode.Off);

            // Handle Dragging of stuff over scene view
            HandleDragging();

            if (evt.type == EventType.Repaint)
                Graphics.SetRenderTarget(null);

            GUIClip.Pop();

            GUI.EndGroup();
            GUI.BeginGroup(windowSpaceCameraRect);

            if (evt.type == EventType.Repaint)
            {
                // Blit the results with a pre-multiplied alpha shader to compose them correctly on top of the 3D scene on the back buffer
                Graphics.DrawTexture(groupSpaceCameraRect, m_SceneTargetTexture, new Rect(0, 0, 1, 1), 0, 0, 0, 0, GUI.color, EditorGUIUtility.GUITextureBlitSceneGUIMaterial);
            }

            RepaintGizmosThatAreRenderedOnTopOfSceneView();

            GUI.EndGroup();
            GUI.color = origColor;
            m_SkipFadingPending = false;

            m_SceneViewOverlay.End();

            HandleMouseCursor();

            s_CurrentDrawingSceneView = null;
            m_Camera.rect = origCameraRect;

            onGUIEnded?.Invoke(this);
            if (m_StageHandling != null)
                m_StageHandling.EndOnGUI();

            if (m_CameraViewVisualElement != null && m_Parent != null)
            {
                var margins = m_Parent.borderSize;
                m_CameraViewVisualElement.style.bottom = margins.bottom;
                m_CameraViewVisualElement.style.left = margins.left;
                m_CameraViewVisualElement.style.right = margins.right;
            }
            RefreshToolbarHeight();
        }

        [Shortcut("Scene View/Toggle 2D Mode", typeof(SceneView), KeyCode.Alpha2)]
        [FormerlyPrefKeyAs("Tools/2D Mode", "2")]
        static void Toggle2DMode(ShortcutArguments args)
        {
            var window = args.context as SceneView;
            if (window != null)
                window.in2DMode = !window.in2DMode;
        }

        [Shortcut("Scene View/Toggle Orthographic Projection", typeof(SceneView))]
        static void ToggleOrthoView(ShortcutArguments args)
        {
            var view = args.context as SceneView;
            if (view != null)
            {
                if (!view.isRotationLocked)
                    view.svRot.ViewSetOrtho(view, !view.orthographic);
            }
        }

        [Shortcut("Scene View/Set Orthographic Right View", typeof(SceneView))]
        static void SetOrthoRightView(ShortcutArguments args)
        {
            var view = args.context as SceneView;
            if (view != null)
            {
                if (!view.isRotationLocked)
                    view.svRot.ViewAxisDirection(view, 0, true);
            }
        }

        [Shortcut("Scene View/Set Right View", typeof(SceneView))]
        static void SetRightView(ShortcutArguments args)
        {
            var view = args.context as SceneView;
            if (view != null)
            {
                if (!view.isRotationLocked)
                    view.svRot.ViewAxisDirection(view, 0, view.orthographic);
            }
        }

        [Shortcut("Scene View/Set Top View", typeof(SceneView))]
        static void SetTopView(ShortcutArguments args)
        {
            var view = args.context as SceneView;
            if (view != null)
            {
                if (!view.isRotationLocked)
                    view.svRot.ViewAxisDirection(view, 1, view.orthographic);
            }
        }

        [Shortcut("Scene View/Set Orthographic Top View", typeof(SceneView))]
        static void SetOrthoTopView(ShortcutArguments args)
        {
            var view = args.context as SceneView;
            if (view != null)
            {
                if (!view.isRotationLocked)
                    view.svRot.ViewAxisDirection(view, 1, true);
            }
        }

        [Shortcut("Scene View/Set Front View", typeof(SceneView))]
        static void SetFrontView(ShortcutArguments args)
        {
            var view = args.context as SceneView;
            if (view != null)
            {
                if (!view.isRotationLocked)
                    view.svRot.ViewAxisDirection(view, 2, view.orthographic);
            }
        }

        [Shortcut("Scene View/Set Orthographic Front View", typeof(SceneView))]
        static void SetOrthoFrontView(ShortcutArguments args)
        {
            var view = args.context as SceneView;
            if (view != null)
            {
                if (!view.isRotationLocked)
                    view.svRot.ViewAxisDirection(view, 2, true);
            }
        }

        [Shortcut("Scene View/Set Left View", typeof(SceneView))]
        static void SetLeftView(ShortcutArguments args)
        {
            var view = args.context as SceneView;
            if (view != null)
            {
                if (!view.isRotationLocked)
                    view.svRot.ViewAxisDirection(view, 3, view.orthographic);
            }
        }

        [Shortcut("Scene View/Set Orthographic Left View", typeof(SceneView))]
        static void SetOrthoLeftView(ShortcutArguments args)
        {
            var view = args.context as SceneView;
            if (view != null)
            {
                if (!view.isRotationLocked)
                    view.svRot.ViewAxisDirection(view, 3, true);
            }
        }

        [Shortcut("Scene View/Set Bottom View", typeof(SceneView))]
        static void SetBottomView(ShortcutArguments args)
        {
            var view = args.context as SceneView;
            if (view != null)
            {
                if (!view.isRotationLocked)
                    view.svRot.ViewAxisDirection(view, 4, view.orthographic);
            }
        }

        [Shortcut("Scene View/Set Orthographic Bottom View", typeof(SceneView))]
        static void SetOrthoBottomView(ShortcutArguments args)
        {
            var view = args.context as SceneView;
            if (view != null)
            {
                if (!view.isRotationLocked)
                    view.svRot.ViewAxisDirection(view, 4, true);
            }
        }

        [Shortcut("Scene View/Set Back View", typeof(SceneView))]
        static void SetBackView(ShortcutArguments args)
        {
            var view = args.context as SceneView;
            if (view != null)
            {
                if (!view.isRotationLocked)
                    view.svRot.ViewAxisDirection(view, 5, view.orthographic);
            }
        }

        [Shortcut("Scene View/Set Orthographic Back View", typeof(SceneView))]
        static void SetOrthoBackView(ShortcutArguments args)
        {
            var view = args.context as SceneView;
            if (view != null)
            {
                if (!view.isRotationLocked)
                    view.svRot.ViewAxisDirection(view, 5, true);
            }
        }

        [Shortcut("Scene View/Set Free View", typeof(SceneView))]
        static void SetFreeView(ShortcutArguments args)
        {
            var view = args.context as SceneView;
            if (view != null)
            {
                if (!view.isRotationLocked)
                    view.svRot.ViewFromNiceAngle(view, false);
            }
        }

        void HandleMouseCursor()
        {
            Event evt = Event.current;
            Rect cursorRect = new Rect(0, 0, position.width, position.height);
            var checkMouseRects = evt.type == EventType.MouseMove || evt.type == EventType.Repaint;

            if (GUIUtility.hotControl == 0)
                s_DraggingCursorIsCached = false;

            if (!s_DraggingCursorIsCached)
            {
                // Determine if mouse is inside a new cursor rect
                bool repaintView = false;
                MouseCursor cursor = MouseCursor.Arrow;
                if (checkMouseRects)
                {
                    foreach (CursorRect r in s_MouseRects)
                    {
                        if (r.rect.Contains(evt.mousePosition))
                        {
                            cursor = r.cursor;
                            cursorRect = r.rect;
                            repaintView = true;
                        }
                    }

                    if (GUIUtility.hotControl != 0)
                        s_DraggingCursorIsCached = true;

                    var cursorChanged = cursor != s_LastCursor;
                    if (cursorChanged)
                    {
                        s_LastCursor = cursor;
                        InternalEditorUtility.ResetCursor();
                    }
                    if (repaintView || cursorChanged)
                    {
                        Repaint();
                    }
                }
            }

            // Apply the one relevant cursor rect
            if (checkMouseRects && s_LastCursor != MouseCursor.Arrow)
                EditorGUIUtility.AddCursorRect(cursorRect, s_LastCursor);
        }

        void DrawRenderModeOverlay(Rect cameraRect)
        {
            // show destination alpha channel
            if (m_CameraMode.drawMode == DrawCameraMode.AlphaChannel)
            {
                if (!s_AlphaOverlayMaterial)
                    s_AlphaOverlayMaterial = EditorGUIUtility.LoadRequired("SceneView/SceneViewAlphaMaterial.mat") as Material;
                Handles.BeginGUI();
                if (Event.current.type == EventType.Repaint)
                    Graphics.DrawTexture(cameraRect, EditorGUIUtility.whiteTexture, s_AlphaOverlayMaterial);
                Handles.EndGUI();
            }

            // show one of deferred buffers
            if (m_CameraMode.drawMode == DrawCameraMode.DeferredDiffuse ||
                m_CameraMode.drawMode == DrawCameraMode.DeferredSpecular ||
                m_CameraMode.drawMode == DrawCameraMode.DeferredSmoothness ||
                m_CameraMode.drawMode == DrawCameraMode.DeferredNormal)
            {
                if (!s_DeferredOverlayMaterial)
                    s_DeferredOverlayMaterial = EditorGUIUtility.LoadRequired("SceneView/SceneViewDeferredMaterial.mat") as Material;
                Handles.BeginGUI();
                if (Event.current.type == EventType.Repaint)
                {
                    s_DeferredOverlayMaterial.SetInt("_DisplayMode", (int)m_CameraMode.drawMode - (int)DrawCameraMode.DeferredDiffuse);
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

        // Center point of the scene view. Modify it to move the sceneview immediately, or use LookAt to animate it nicely.
        public Vector3 pivot { get { return m_Position.value; } set { m_Position.value = value; } }

        // The direction of the scene view.
        public Quaternion rotation { get { return m_Rotation.value; } set { m_Rotation.value = value; } }

        static float ValidateSceneSize(float value)
        {
            if (value == 0f || float.IsNaN(value))
                return float.Epsilon;
            if (value > k_MaxSceneViewSize)
                return k_MaxSceneViewSize;
            if (value < -k_MaxSceneViewSize)
                return -k_MaxSceneViewSize;
            return value;
        }

        public float size
        {
            get { return m_Size.value; }
            set { m_Size.value = ValidateSceneSize(value); }
        }

        // ReSharper disable once UnusedMember.Global - used only in editor tests
        internal float targetSize
        {
            get { return m_Size.target; }
            set { m_Size.target = ValidateSceneSize(value); }
        }

        float perspectiveFov => m_CameraSettings.fieldOfView;

        public bool orthographic
        {
            get { return m_Ortho.value; }
            set
            {
                m_Ortho.value = value;
                svRot.UpdateGizmoLabel(this, m_Rotation.target * Vector3.forward, m_Ortho.target);
            }
        }

        public void FixNegativeSize()
        {
            if (size == 0f)
                size = float.Epsilon;

            float fov = perspectiveFov;

            if (size < 0)
            {
                float distance = GetPerspectiveCameraDistance(size, fov);
                Vector3 p = m_Position.value + rotation * new Vector3(0, 0, -distance);
                size = -size;
                distance = GetPerspectiveCameraDistance(size, fov);
                m_Position.value = p + rotation * new Vector3(0, 0, distance);
            }
        }

        float CalcCameraDist()
        {
            float fov = m_Ortho.Fade(perspectiveFov, 0);
            if (fov > kOrthoThresholdAngle)
            {
                m_Camera.orthographic = false;
                return GetPerspectiveCameraDistance(size, fov);
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

        internal static Camera GetMainCamera()
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
        internal static RenderingPath GetSceneViewRenderingPath()
        {
            var mainCamera = GetMainCamera();
            if (mainCamera != null)
                return mainCamera.renderingPath;
            return RenderingPath.UsePlayerSettings;
        }

        internal static bool IsUsingDeferredRenderingPath()
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
            if (!m_SceneIsLit || !DoesCameraDrawModeSupportHDR(m_CameraMode.drawMode))
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
            if (m_CameraMode.drawMode == DrawCameraMode.Overdraw)
            {
                // overdraw
                m_Camera.backgroundColor = Color.black;
            }
            else
            {
                if (m_StageHandling != null)
                    m_Camera.backgroundColor = StageNavigationManager.instance.currentStage.GetBackgroundColor();
                else
                    m_Camera.backgroundColor = kSceneViewBackground;
            }

            if (Event.current.type == EventType.Repaint)
            {
                bool enableImageEffects = m_CameraMode.drawMode == DrawCameraMode.Textured && sceneViewState.imageEffectsEnabled;
                UpdateImageEffects(enableImageEffects);
            }

            EditorUtility.SetCameraAnimateMaterials(m_Camera, sceneViewState.alwaysRefreshEnabled);
            ParticleSystemEditorUtils.renderInSceneView = m_SceneViewState.particleSystemsEnabled;
            UnityEngine.VFX.VFXManager.renderInSceneView = m_SceneViewState.visualEffectGraphsEnabled;
            SceneVisibilityManager.instance.enableSceneVisibility = m_SceneVisActive;
            ResetIfNaN();

            m_Camera.transform.rotation = m_Rotation.value;

            float fov = m_Ortho.Fade(perspectiveFov, 0);

            if (fov > kOrthoThresholdAngle)
            {
                m_Camera.orthographic = false;
                m_Camera.fieldOfView = GetVerticalFOV(fov);
            }
            else
            {
                m_Camera.orthographic = true;
                m_Camera.orthographicSize = GetVerticalOrthoSize();
            }

            if (m_CameraSettings.dynamicClip)
            {
                var clip = GetDynamicClipPlanes();
                m_Camera.nearClipPlane = clip.x;
                m_Camera.farClipPlane = clip.y;
            }
            else
            {
                m_Camera.nearClipPlane = m_CameraSettings.nearClip;
                m_Camera.farClipPlane = m_CameraSettings.farClip;
            }

            m_Camera.useOcclusionCulling = m_CameraSettings.occlusionCulling;
            m_Camera.transform.position = m_Position.value + m_Camera.transform.rotation * new Vector3(0, 0, -cameraDistance);

            // In 2D mode, camera position z should not go to positive value.
            if (m_2DMode && m_Camera.transform.position.z >= 0)
            {
                var p = m_Camera.transform.position;
                p.z = -(m_Camera.nearClipPlane + 0.01f);
                m_Camera.transform.position = p;
            }

            m_Camera.renderingPath = GetSceneViewRenderingPath();
            if (!CheckDrawModeForRenderingPath(m_CameraMode.drawMode))
                m_CameraMode = GetBuiltinCameraMode(DrawCameraMode.Textured);
            SetSceneCameraHDRAndDepthModes();

            if (m_CameraMode.drawMode == DrawCameraMode.Textured ||
                m_CameraMode.drawMode == DrawCameraMode.TexturedWire ||
                m_CameraMode.drawMode == DrawCameraMode.UserDefined)
            {
                Handles.EnableCameraFlares(m_Camera, sceneViewState.flaresEnabled);
                Handles.EnableCameraSkybox(m_Camera, sceneViewState.skyboxEnabled);
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
            if (m_PlayAudio)
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
            if (sceneViewState.alwaysRefreshEnabled && m_lastRenderedTime + 0.033f < EditorApplication.timeSinceStartup)
            {
                m_lastRenderedTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        // ReSharper disable once UnusedMember.Global - used in tests
        internal Quaternion cameraTargetRotation => m_Rotation.target;

        // ReSharper disable once UnusedMember.Global - used in tests
        internal Vector3 cameraTargetPosition => m_Position.target + m_Rotation.target * new Vector3(0, 0, -cameraDistance);

        // ReSharper disable once MemberCanBePrivate.Global - used in tests
        internal float GetVerticalFOV(float aspectNeutralFOV)
        {
            // We want Scene view camera "FOV" to be the vertical FOV if the
            // Scene view is wider than tall, and the horizontal FOV otherwise.
            float multiplier = 1.0f;
            if (m_Camera.aspect < 1)
                multiplier /= m_Camera.aspect;
            float halfFovRad = aspectNeutralFOV * 0.5f * Mathf.Deg2Rad;
            float halfFovTan = Mathf.Tan(halfFovRad) * multiplier;
            return Mathf.Atan(halfFovTan) * 2 * Mathf.Rad2Deg;
        }

        float GetVerticalOrthoSize()
        {
            // We want scene view ortho size to enclose sphere of
            // radius "size". If scene view is more tall than wide,
            // we want to take that into account so that the bounds
            // fit in horizontally.
            float res = size;
            if (m_Camera.aspect < 1.0)
                res /= m_Camera.aspect;
            return res;
        }

        // Look at a specific point.
        public void LookAt(Vector3 point)
        {
            FixNegativeSize();
            m_Position.target = point;
        }

        // Look at a specific point from a given direction.
        public void LookAt(Vector3 point, Quaternion direction)
        {
            FixNegativeSize();
            m_Position.target = point;
            m_Rotation.target = direction;
            // Update name in the top-right handle
            svRot.UpdateGizmoLabel(this, direction * Vector3.forward, m_Ortho.target);
        }

        // Look directly at a specific point from a given direction.
        public void LookAtDirect(Vector3 point, Quaternion direction)
        {
            FixNegativeSize();
            m_Position.value = point;
            m_Rotation.value = direction;
            // Update name in the top-right handle
            svRot.UpdateGizmoLabel(this, direction * Vector3.forward, m_Ortho.target);
        }

        // Look at a specific point from a given direction with a given zoom level.
        public void LookAt(Vector3 point, Quaternion direction, float newSize)
        {
            FixNegativeSize();
            m_Position.target = point;
            m_Rotation.target = direction;
            m_Size.target = ValidateSceneSize(Mathf.Abs(newSize));
            // Update name in the top-right handle
            svRot.UpdateGizmoLabel(this, direction * Vector3.forward, m_Ortho.target);
        }

        // Look directionally at a specific point from a given direction with a given zoom level.
        public void LookAtDirect(Vector3 point, Quaternion direction, float newSize)
        {
            FixNegativeSize();
            m_Position.value = point;
            m_Rotation.value = direction;
            size = Mathf.Abs(newSize);
            // Update name in the top-right handle
            svRot.UpdateGizmoLabel(this, direction * Vector3.forward, m_Ortho.target);
        }

        // Look at a specific point from a given direction with a given zoom level, enabling and disabling perspective
        public void LookAt(Vector3 point, Quaternion direction, float newSize, bool ortho)
        {
            LookAt(point, direction, newSize, ortho, false);
        }

        // Look at a specific point from a given direction with a given zoom level, enabling and disabling perspective
        public void LookAt(Vector3 point, Quaternion direction, float newSize, bool ortho, bool instant)
        {
            SceneViewMotion.ResetMotion();
            FixNegativeSize();

            if (instant)
            {
                m_Position.value = point;
                m_Rotation.value = direction;
                size = Mathf.Abs(newSize);
                m_Ortho.value = ortho;
                draggingLocked = DraggingLockedState.NotDragging;
            }
            else
            {
                m_Position.target = point;
                m_Rotation.target = direction;
                m_Size.target = ValidateSceneSize(Mathf.Abs(newSize));
                m_Ortho.target = ortho;
            }

            // Update name in the top-right handle
            svRot.UpdateGizmoLabel(this, direction * Vector3.forward, m_Ortho.target);
        }

        void DefaultHandles()
        {
            // Note event state.
            EditorGUI.BeginChangeCheck();
            bool IsDragEvent = Event.current.GetTypeForControl(GUIUtility.hotControl) == EventType.MouseDrag;
            bool IsMouseUpEvent = Event.current.GetTypeForControl(GUIUtility.hotControl) == EventType.MouseUp;

            EditorToolContext.OnToolGUI(this);

            // If we are actually dragging the object(s) then disable 2D physics movement.
            if (EditorGUI.EndChangeCheck() && EditorApplication.isPlaying && IsDragEvent)
                Physics2D.SetEditorDragMovement(true, Selection.gameObjects);

            // If we have finished dragging the object(s) then enable 2D physics movement.
            if (EditorApplication.isPlaying && IsMouseUpEvent)
                Physics2D.SetEditorDragMovement(false, Selection.gameObjects);
        }

        void CleanupEditorDragFunctions()
        {
            m_DragEditorCache?.Dispose();
            m_DragEditorCache = null;
        }

        bool CallEditorDragFunctions(IList<Object> dragAndDropObjects)
        {
            Event evt = Event.current;

            SpriteUtility.OnSceneDrag(this);

            if (evt.type == EventType.Used || dragAndDropObjects.Count == 0) return true;

            if (m_DragEditorCache == null)
                m_DragEditorCache = new EditorCache(EditorFeatures.OnSceneDrag);

            bool allHandled = true;

            // We iterate through dragged items backwards to preserve the alphabetical order
            // of GameObjects when they are created in hierarchy once drag is performed
            for (int i = dragAndDropObjects.Count - 1; i >= 0; i--)
            {
                if (dragAndDropObjects[i] == null)
                    continue;

                EditorWrapper w = m_DragEditorCache[dragAndDropObjects[i]];

                if (w == null)
                {
                    allHandled = false;
                    continue;
                }
                w.OnSceneDrag(this, dragAndDropObjects.Count - 1 - i);
            }

            return allHandled;
        }

        internal static bool CanDoDrag(ICollection<Object> objects)
        {
            if (objects.Count < 2) return true;

            int gameObjectCount = 0;
            int assetCount = 0;
            int materialCount = 0;

            // Only allow dragging multiple GameObjects, or multiple non-GameObjects, but not mixed sets.
            // For example when dragging GameObjects and Materials would sometimes apply material to
            // already existing scene object, and other times to the object being spawned. It depends
            // on the order in which the user selects those assets. We decided it was not an intuitive
            // behavior and it should just not be allowed.
            // Also we don't want multiple materials be dropped into scene because there is no case
            // where we can handle it in a way that benefit the user. For example multiple skybox
            // materials doesn't make sense and dropping multiple materials onto geometry will only
            // drop the first material on the hovered material entry.
            foreach (Object obj in objects)
            {
                if (obj.GetType() == typeof(GameObject))
                {
                    gameObjectCount++;
                }
                else
                {
                    assetCount++;
                    if (obj.GetType() == typeof(Material))
                    {
                        materialCount++;
                    }
                }

                if (gameObjectCount > 0 && assetCount > 0 || materialCount > 1) return false;
            }

            return true;
        }

        void HandleDragging()
        {
            Event evt = Event.current;

            Object[] dragAndDropObjects = DragAndDrop.objectReferences;

            switch (evt.type)
            {
                case EventType.DragPerform:
                case EventType.DragUpdated:
                    if (evt.type == EventType.DragPerform && GameObjectInspector.s_CyclicNestingDetected)
                    {
                        PrefabUtility.ShowCyclicNestingWarningDialog();
                        return;
                    }

                    if (!CanDoDrag(dragAndDropObjects))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                        break;
                    }

                    bool allHandled = CallEditorDragFunctions(dragAndDropObjects);

                    if (evt.type == EventType.Used || allHandled)
                        break;

                    bool isPerform = evt.type == EventType.DragPerform;
                    // call old-style C++ dragging handlers
                    if (DragAndDrop.visualMode != DragAndDropVisualMode.Copy)
                    {
                        var defaultParentObject = GetDefaultParentObjectIfSet();
                        var parent = defaultParentObject != null ? defaultParentObject : customParentForDraggedObjects;

                        GameObject go = HandleUtility.PickGameObject(Event.current.mousePosition, true);
                        DragAndDrop.visualMode = DragAndDropService.Drop(DragAndDropService.kSceneDropDstId, go, pivot, Event.current.mousePosition, parent, isPerform);
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
                    CallEditorDragFunctions(dragAndDropObjects);
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
                case EventCommandNames.Find:
                    if (execute)
                        FocusSearchField();
                    Event.current.Use();
                    break;
                case EventCommandNames.FrameSelected:
                    if (execute && Tools.s_ButtonDown != 1)
                    {
                        FrameSelected(false);
                    }
                    Event.current.Use();
                    break;
                case EventCommandNames.FrameSelectedWithLock:
                    if (execute && Tools.s_ButtonDown != 1)
                    {
                        FrameSelected(true);
                    }
                    Event.current.Use();
                    break;
                case EventCommandNames.SoftDelete:
                case EventCommandNames.Delete:
                    if (execute)
                        Unsupported.DeleteGameObjectSelection();
                    Event.current.Use();
                    break;
                case EventCommandNames.Duplicate:
                    if (execute)
                    {
                        CutCopyPasteUtility.DuplicateGO(customParentForNewGameObjects);
                    }
                    Event.current.Use();
                    break;
                case EventCommandNames.Copy:
                    if (execute)
                    {
                        CutCopyPasteUtility.CopyGO();
                    }
                    Event.current.Use();
                    break;
                case EventCommandNames.Cut:
                    if (execute)
                    {
                        CutCopyPasteUtility.CutGO();
                    }
                    Event.current.Use();
                    break;
                case EventCommandNames.Paste:
                    if (execute)
                    {
                        CutCopyPasteUtility.PasteGO(customParentForNewGameObjects);
                    }
                    Event.current.Use();
                    break;
                case EventCommandNames.SelectAll:
                    if (execute)
                        Selection.objects = FindObjectsOfType(typeof(GameObject));
                    Event.current.Use();
                    break;
                case EventCommandNames.DeselectAll:
                    if (execute)
                        Selection.activeGameObject = null;
                    Event.current.Use();
                    break;
                case EventCommandNames.InvertSelection:
                    if (execute)
                        Selection.objects = FindObjectsOfType(typeof(GameObject)).Except(Selection.gameObjects).ToArray();
                    Event.current.Use();
                    break;
                case EventCommandNames.SelectChildren:
                    if (execute)
                    {
                        List<GameObject> gameObjects = new List<GameObject>(Selection.gameObjects);
                        foreach (var gameObject in Selection.gameObjects)
                        {
                            gameObjects.AddRange(gameObject.transform.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject));
                        }
                        Selection.objects = gameObjects.Distinct().Cast<Object>().ToArray();
                    }
                    Event.current.Use();
                    break;
                case EventCommandNames.SelectPrefabRoot:
                    if (execute)
                    {
                        List<GameObject> gameObjects = new List<GameObject>(Selection.gameObjects.Length);
                        foreach (var gameObject in Selection.gameObjects)
                        {
                            var root = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
                            if (root != null)
                            {
                                gameObjects.Add(root);
                            }
                        }
                        Selection.objects = gameObjects.Distinct().Cast<Object>().ToArray();
                    }
                    Event.current.Use();
                    break;
            }
            // Detect if we are canceling 'Cut' operation
            if (Event.current.keyCode == KeyCode.Escape && CutBoard.hasCutboardData)
            {
                CutCopyPasteUtility.ResetCutboardAndRepaintHierarchyWindows();
                Repaint();
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

        internal bool IsGameObjectInThisSceneView(GameObject gameObject)
        {
            if (gameObject == null)
                return false;

            return StageUtility.IsGameObjectRenderedByCamera(gameObject, camera);
        }

        public bool FrameSelected()
        {
            return FrameSelected(false);
        }

        public bool FrameSelected(bool lockView)
        {
            return FrameSelected(lockView, false);
        }

        public virtual bool FrameSelected(bool lockView, bool instant)
        {
            if (!IsGameObjectInThisSceneView(Selection.activeGameObject))
                return false;

            viewIsLockedToObject = lockView;
            FixNegativeSize();

            Bounds bounds;
            if (!m_WasFocused)
            {
                bounds = InternalEditorUtility.CalculateSelectionBounds(false, Tools.pivotMode == PivotMode.Pivot, true);
            }
            else
            {
                bounds = new Bounds(Tools.handlePosition, Vector3.one);
            }

            // Check active editor for OnGetFrameBounds
            foreach (Editor editor in activeEditors)
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

            m_WasFocused = !m_WasFocused;
            return Frame(bounds, EditorApplication.isPlaying || instant);
        }

        public bool Frame(Bounds bounds, bool instant = true)
        {
            float newSize = bounds.extents.magnitude;

            if (float.IsInfinity(newSize))
                return false;

            // If we have no size to focus on, bound default 10 units
            if (newSize < Mathf.Epsilon)
                newSize = 10;

            // We snap instantly into target on playmode, because things might be moving fast and lerping lags behind
            LookAt(bounds.center, m_Rotation.target, newSize, m_Ortho.value, instant);

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
            m_Camera.scene = m_CustomScene;
            if (m_OverrideSceneCullingMask != 0)
                m_Camera.overrideSceneCullingMask = m_OverrideSceneCullingMask;

            m_CustomLightsScene = EditorSceneManager.NewPreviewScene();
            m_CustomLightsScene.name = "CustomLightsScene-SceneView" + m_WindowGUID;
            for (int i = 0; i < 3; i++)
            {
                GameObject lightGO = EditorUtility.CreateGameObjectWithHideFlags("SceneLight", HideFlags.HideAndDontSave, typeof(Light));
                m_Light[i] = lightGO.GetComponent<Light>();
                m_Light[i].type = LightType.Directional;
                m_Light[i].intensity = 1.0f;
                m_Light[i].enabled = false;
                SceneManager.MoveGameObjectToScene(lightGO, m_CustomLightsScene);
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
        }

        void CallOnSceneGUI()
        {
            foreach (Editor editor in activeEditors)
            {
                if (!drawGizmos || !EditorGUIUtility.IsGizmosAllowedForObject(editor.target))
                    continue;

                MethodInfo method = editor.GetType().GetMethod(
                    "OnSceneGUI",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy,
                    null,
                    Type.EmptyTypes,
                    null);

                if (method != null)
                {
                    MethodInfo methodEnabled = editor.GetType().GetMethod(
                        "IsSceneGUIEnabled",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy,
                        null,
                        Type.EmptyTypes,
                        null);

                    bool enabled = (methodEnabled != null) ? (bool)methodEnabled.Invoke(null, null) : true;
                    if (enabled)
                    {
                        using (new EditorPerformanceTracker($"Editor.{editor.GetType().Name}.OnSceneGUI"))
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
                    // This would mean that OnSceneGUI has changed the scene and it is not drawn
                    if (s_CurrentDrawingSceneView == null)
                        GUIUtility.ExitGUI();
                }
            }

            EditorToolContext.InvokeOnSceneGUICustomEditorTools();

            if (duringSceneGui != null)
            {
                ResetOnSceneGUIState();

                if (duringSceneGui != null)
                    duringSceneGui(this);

#pragma warning disable 618
                if (onSceneGUIDelegate != null)
                    onSceneGUIDelegate(this);
#pragma warning restore 618

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
            foreach (Editor editor in activeEditors)
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

            if (beforeSceneGui != null)
            {
                Handles.ClearHandles();
                beforeSceneGui(this);
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

        static void ShowCompileErrorNotification()
        {
            ShowNotification("All compiler errors have to be fixed before you can enter playmode!");
        }

        internal static void ShowSceneViewPlayModeSaveWarning()
        {
            // In this case, we want to explicitly try the GameView before passing it on to whatever notificationView we have
            var playModeView = (PlayModeView)WindowLayout.FindEditorWindowOfType(typeof(PlayModeView));
            if (playModeView != null && playModeView.hasFocus)
                playModeView.ShowNotification(EditorGUIUtility.TrTextContent("You must exit play mode to save the scene!"));
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
                    size = kDefaultViewSize;
                    m_Ortho.value = true;

                    m_LastSceneViewRotation = kDefaultRotation;
                    m_LastSceneViewOrtho = false;
                    break;

                default: // Default to 3D mode (BUGFIX:569204)
                    m_2DMode = false;
                    m_Rotation.value = kDefaultRotation;
                    m_Position.value = kDefaultPivot;
                    size = kDefaultViewSize;
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
                lastSceneViewRotation = m_Rotation.target;
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

        public static CameraMode AddCameraMode(string name, string section)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Cannot be null or empty", "name");
            if (string.IsNullOrEmpty(section))
                throw new ArgumentException("Cannot be null or empty", "section");
            var newMode = new CameraMode(DrawCameraMode.UserDefined, name, section);
            if (userDefinedModes.Contains(newMode))
                throw new InvalidOperationException(string.Format("A mode named {0} already exists in section {1}", name, section));
            userDefinedModes.Add(newMode);
            return newMode;
        }

        private static bool IsValidCameraMode(CameraMode cameraMode)
        {
            foreach (var mode in Enum.GetValues(typeof(DrawCameraMode)))
            {
                if (SceneRenderModeWindow.DrawCameraModeExists((DrawCameraMode)mode) && cameraMode == GetBuiltinCameraMode((DrawCameraMode)mode))
                {
                    return true;
                }
            }

            foreach (var tempCameraMode in userDefinedModes)
            {
                if (tempCameraMode == cameraMode)
                {
                    return true;
                }
            }
            return false;
        }

        public static void ClearUserDefinedCameraModes()
        {
            userDefinedModes.Clear();
        }

        public static CameraMode GetBuiltinCameraMode(DrawCameraMode mode)
        {
            return SceneRenderModeWindow.GetBuiltinCameraMode(mode);
        }

        internal void RebuildBreadcrumbBar()
        {
            if (SupportsStageHandling())
                m_StageHandling.RebuildBreadcrumbBar();
        }

        internal static void RebuildBreadcrumbBarInAll()
        {
            foreach (SceneView sv in s_SceneViews)
            {
                sv.RebuildBreadcrumbBar();
            }
        }

        internal void ResetGridPivot()
        {
            sceneViewGrids.SetAllGridsPivot(Vector3.zero);
        }
    }
} // namespace
