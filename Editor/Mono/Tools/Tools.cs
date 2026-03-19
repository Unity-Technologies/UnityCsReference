// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using Unity.Profiling;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEditorInternal;
using UnityEditor.EditorTools;

namespace UnityEditor
{
    public enum ViewTool
    {
        None = -1,
        Orbit = 0,
        Pan = 1,
        Zoom = 2,
        FPS = 3
    }

    public enum PivotMode
    {
        Custom = -1,
        Center = 0,
        Pivot = 1
    }
    
    public enum PivotRotation
    {
        Custom = -1,
        Local = 0,
        Global = 1,
        Grid = 2
    }
    
    public enum Tool
    {
        View = 0,
        Move = 1,
        Rotate = 2,
        Scale = 3,
        Rect = 4,
        Transform = 5,
        Custom = 6,
        None = -1
    }

    public sealed partial class Tools : ScriptableObject
    {
        static Tools get
        {
            get
            {
                if (!s_Get)
                {
                    s_Get = ScriptableObject.CreateInstance<Tools>();
                    s_Get.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_Get;
            }
        }

        static Tools s_Get;

#pragma warning disable 618
        [System.Obsolete("Use EditorTools.activeToolDidChange or EditorTools.activeToolWillChange")]
        internal delegate void OnToolChangedFunc(Tool from, Tool to);
        [System.Obsolete("Use EditorTools.activeToolDidChange or EditorTools.activeToolWillChange")]
        internal static OnToolChangedFunc onToolChanged;
#pragma warning restore 618 

        public static event Action pivotModeChanged;
        internal static event Action<Type> pivotModeChangedForOwner;
        public static event Action pivotRotationChanged;
        internal static event Action<Type> pivotRotationChangedForOwner;
        public static event Action viewToolChanged;

        public static Tool current
        {
            get => GetCurrent(typeof(SceneView));
            set => SetCurrent(value, typeof(SceneView));
        }

        internal static Tool GetCurrent(Type toolOwner)
        {
            var state = EditorToolManager.GetEditorToolStateForOwner(toolOwner);
            EditorToolContext activeCtx = null;
            if (state != null)
                activeCtx = state.activeToolContext;
            
            return EditorToolUtility.GetEnumWithEditorTool(EditorToolManager.GetActiveTool(toolOwner), activeCtx);
        }

        internal static void SetCurrent(Tool tool, Type toolOwner)
        {
            var toolInstance = EditorToolUtility.GetEditorToolWithEnum(tool, toolOwner);

            //In case the new tool is leading to an incorrect tool type, return and leave the current tool as it is.
            if (tool != Tool.None && toolInstance is NoneTool)
                return;

            EditorToolManager.SetActiveTool(toolInstance, toolOwner);
            ShortcutManager.RegisterTag(tool);
        }

        internal static void SyncToolEnum()
        {
            RepaintAllToolViews();
        }

        internal static void InvokePivotModeChangedCallback(Type toolOwner)
        {
            if (toolOwner == typeof(SceneView))
                pivotModeChanged?.Invoke();
            
            pivotModeChangedForOwner?.Invoke(toolOwner);
        }

        internal static void InvokePivotRotationChangedCallback(Type toolOwner)
        {
            if (toolOwner == typeof(SceneView))
                pivotRotationChanged?.Invoke();
            
            pivotRotationChangedForOwner?.Invoke(toolOwner);
        }

        public static ViewTool viewTool
        {
            get { return get.m_ViewTool; }
            set
            {
                if (viewTool == value)
                    return;

                get.m_ViewTool = value;
                ShortcutManager.RegisterTag(get.m_ViewTool);

                viewToolChanged?.Invoke();
            }
        }

        internal static ViewTool s_LockedViewTool = ViewTool.None;
        internal static int s_ButtonDown = -1;
        public static bool viewToolActive => SceneViewMotion.viewToolIsActive;

        static Vector3 s_HandlePosition;
        static bool s_HandlePositionComputed;
        internal static Vector3 cachedHandlePosition
        {
            get
            {
                if (!s_HandlePositionComputed)
                {
                    s_HandlePosition = GetHandlePosition();
                    s_HandlePositionComputed = true;
                }
                return s_HandlePosition;
            }
        }

        internal static void InvalidateHandlePosition()
        {
            s_HandlePositionComputed = false;
        }

        public static Vector3 handlePosition
        {
            get
            {
                Transform t = Selection.activeTransform;
                if (!t)
                    return new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);

                if (s_LockHandlePositionActive)
                    return s_LockHandlePosition;

                return cachedHandlePosition;
            }
        }

        private static ProfilerMarker s_GetHandlePositionMarker = new ProfilerMarker($"{nameof(Tools)}.{nameof(GetHandlePosition)}");
        internal static Vector3 GetHandlePosition()
        {
            Transform t = Selection.activeTransform;
            if (!t)
                return new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);

            Vector3 totalOffset = handleOffset + handleRotation * localHandleOffset;
            
            using (s_GetHandlePositionMarker.Auto())
            {
                return EditorPivotManager.GetActivePivotMode(typeof(SceneView)).position + totalOffset;
            }
        }
        
        public static Rect handleRect
        {
            get
            {
                var rotation = handleRotation;
                var activeRotationTracker = EditorPivotManager.activeRotationTracker;
                if ((pivotRotation == PivotRotation.Custom || pivotRotation == PivotRotation.Grid) && activeRotationTracker.isRotationControlHot)
                    rotation = activeRotationTracker.rotation;

                Bounds bounds = InternalEditorUtility.CalculateSelectionBoundsInSpace(handlePosition, rotation, rectBlueprintMode);
                int axis = GetRectAxisForViewDir(bounds, rotation, SceneView.currentDrawingSceneView.camera.transform.forward);
                return GetRectFromBoundsForAxis(bounds, axis);
            }
        }

        public static Quaternion handleRectRotation
        {
            get
            {
                var rotation = handleRotation;
                var activeRotationTracker = EditorPivotManager.activeRotationTracker;
                if ((pivotRotation == PivotRotation.Custom || pivotRotation == PivotRotation.Grid) && activeRotationTracker.isRotationControlHot)
                    rotation = activeRotationTracker.rotation;

                Bounds bounds = InternalEditorUtility.CalculateSelectionBoundsInSpace(handlePosition, rotation, rectBlueprintMode);
                int axis = GetRectAxisForViewDir(bounds, rotation, SceneView.currentDrawingSceneView.camera.transform.forward);
                return GetRectRotationForAxis(rotation, axis);
            }
        }

        private static int GetRectAxisForViewDir(Bounds bounds, Quaternion rotation, Vector3 viewDir)
        {
            if (s_LockHandleRectAxisActive)
            {
                return s_LockHandleRectAxis;
            }
            if (viewDir == Vector3.zero)
            {
                return 2;
            }
            else
            {
                if (bounds.size == Vector3.zero)
                    bounds.size = Vector3.one;
                int axis = -1;
                float bestScore = -1;
                for (int normalAxis = 0; normalAxis < 3; normalAxis++)
                {
                    Vector3 edge1 = Vector3.zero;
                    Vector3 edge2 = Vector3.zero;
                    int axis1 = (normalAxis + 1) % 3;
                    int axis2 = (normalAxis + 2) % 3;
                    edge1[axis1] = bounds.size[axis1];
                    edge2[axis2] = bounds.size[axis2];
                    float score = Vector3.Cross(Vector3.ProjectOnPlane(rotation * edge1, viewDir), Vector3.ProjectOnPlane(rotation * edge2, viewDir)).magnitude;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        axis = normalAxis;
                    }
                }
                return axis;
            }
        }

        private static Rect GetRectFromBoundsForAxis(Bounds bounds, int axis)
        {
            switch (axis)
            {
                case 0: return new Rect(-bounds.max.z, bounds.min.y, bounds.size.z, bounds.size.y);
                case 1: return new Rect(bounds.min.x, -bounds.max.z, bounds.size.x, bounds.size.z);
                case 2:
                default: return new Rect(bounds.min.x, bounds.min.y, bounds.size.x, bounds.size.y);
            }
        }

        private static Quaternion GetRectRotationForAxis(Quaternion rotation, int axis)
        {
            switch (axis)
            {
                case 0: return rotation * Quaternion.Euler(0, 90, 0);
                case 1: return rotation * Quaternion.Euler(-90, 0, 0);
                case 2:
                default: return rotation;
            }
        }

        internal static void LockHandleRectRotation()
        {
            Bounds bounds = InternalEditorUtility.CalculateSelectionBoundsInSpace(handlePosition, handleRotation, rectBlueprintMode);
            s_LockHandleRectAxis = GetRectAxisForViewDir(bounds, handleRotation, SceneView.currentDrawingSceneView.camera.transform.forward);
            s_LockHandleRectAxisActive = true;
        }

        internal static void UnlockHandleRectRotation()
        {
            s_LockHandleRectAxisActive = false;
        }

        public static PivotMode pivotMode
        {
            get => EditorToolManager.instance.defaultState.pivotMode;
            set => EditorToolManager.instance.defaultState.pivotMode = value;
        }

        [RequiredByNativeCode]
        internal static int GetPivotModeInternal()
        {
            return (int)pivotMode;
        }

        public static bool rectBlueprintMode
        {
            get { return get.m_RectBlueprintMode; }
            set
            {
                if (get.m_RectBlueprintMode != value)
                {
                    get.m_RectBlueprintMode = value;
                    EditorPrefs.SetBool("RectBlueprintMode", rectBlueprintMode);
                }
            }
        }
        private bool m_RectBlueprintMode;
        
        public static Quaternion handleRotation
        {
            get => EditorPivotManager.GetActivePivotRotation(typeof(SceneView)).rotation;
            set
            {
                var state = EditorToolManager.instance.defaultState;
                if (state.pivotRotation == PivotRotation.Global)
                    get.m_GlobalHandleRotation = value;
                else if (state.pivotRotation == PivotRotation.Grid)
                    get.m_GlobalHandleRotation = value * Quaternion.Inverse(GridSettings.instance.rotation);
            }
        }

        public static PivotRotation pivotRotation
        {
            get => EditorToolManager.instance.defaultState.pivotRotation;
            set => EditorToolManager.instance.defaultState.pivotRotation = value;
        }

        internal static bool s_Hidden
        {
            get { return hidden; }
            set { hidden = value; }
        }

        public static bool hidden
        {
            get { return GetHidden(typeof(SceneView)); }
            set { SetHidden(value, typeof(SceneView)); }
        }

        internal static bool GetHidden(Type toolOwnerType)
        {
            var state = EditorToolManager.GetEditorToolStateForOwner(toolOwnerType);
            if (state != null)
                return state.hidden;

            return false;
        }
        
        internal static void SetHidden(bool hide, Type toolOwnerType)
        {
            var state = EditorToolManager.GetEditorToolStateForOwner(toolOwnerType);
            if (state != null)
                state.hidden = hide;
        }

        internal static bool vertexDragging;

        static Event m_VertexDraggingShortcutEvent;
        internal static Event vertexDraggingShortcutEvent
        {
            get
            {
                if(m_VertexDraggingShortcutEvent == null)
                {
                    var vertexSnappingBinding = ShortcutManager.instance.GetShortcutBinding(VertexSnapping.k_VertexSnappingShortcut);
#pragma warning disable UA2002 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    if(!vertexSnappingBinding.keyCombinationSequence.Any())
#pragma warning restore UA2002
                        m_VertexDraggingShortcutEvent = new Event();
                    else
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                        m_VertexDraggingShortcutEvent = vertexSnappingBinding.keyCombinationSequence.First().ToKeyboardEvent();
#pragma warning restore UA2001
                }

                return m_VertexDraggingShortcutEvent;
            }
            set => m_VertexDraggingShortcutEvent = value;
        }

        static Vector3 s_LockHandlePosition;
        static bool s_LockHandlePositionActive = false;

        static int s_LockHandleRectAxis;
        static bool s_LockHandleRectAxisActive = false;

        struct LayerSettings
        {
            public int visibleLayersValue;
            public int lockedLayersValue;
            public LayerSettings(int visible, int locked)
            {
                visibleLayersValue = visible;
                lockedLayersValue = locked;
            }
        }

        LayerSettings m_LayerSettings = new LayerSettings(-1, -1);

        static StateCache<LayerSettings> s_LayersStateCache = new StateCache<LayerSettings>("Library/StateCache/LayerSettings/");
        static Hash128 m_LayerSettingsKey = Hash128.Compute("LayerSettings");

        public static int visibleLayers
        {
            get { return get.m_LayerSettings.visibleLayersValue; }
            set
            {
                if (get.m_LayerSettings.visibleLayersValue != value)
                {
                    get.m_LayerSettings.visibleLayersValue = value;
                    EditorGUIUtility.SetVisibleLayers(value);
                    s_LayersStateCache.SetState(m_LayerSettingsKey, get.m_LayerSettings);
                }
            }
        }

        public static int lockedLayers
        {
            get { return get.m_LayerSettings.lockedLayersValue; }
            set
            {
                if (get.m_LayerSettings.lockedLayersValue != value)
                {
                    get.m_LayerSettings.lockedLayersValue = value;
                    EditorGUIUtility.SetLockedLayers(value);
                    s_LayersStateCache.SetState(m_LayerSettingsKey, get.m_LayerSettings);
                }
            }
        }

        void OnEnable()
        {
            s_Get = this;

            rectBlueprintMode = EditorPrefs.GetBool("RectBlueprintMode", false);
            
            EditorPivotManager.instance.SyncToolsPivotStateIfNeeded();
            
            var layerSettings = s_LayersStateCache.GetState(m_LayerSettingsKey, new LayerSettings(-1, 0));
            visibleLayers = layerSettings.visibleLayersValue;
            lockedLayers = layerSettings.lockedLayersValue;
            Selection.selectionChanged += OnSelectionChange;
            Undo.undoRedoEvent += OnUndoRedo;

            ShortcutManager.instance.activeProfileChanged += args => vertexDraggingShortcutEvent = null;
            ShortcutManager.instance.shortcutBindingChanged += args => vertexDraggingShortcutEvent = null;

            EditorToolManager.activeToolChanged += (previous, active) =>
            {
#pragma warning disable 618
                if (onToolChanged != null)
                    onToolChanged(
                        EditorToolUtility.GetEnumWithEditorTool(previous),
                        EditorToolUtility.GetEnumWithEditorTool(active));
#pragma warning restore 618
            };
        }

        void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChange;
            Undo.undoRedoEvent -= OnUndoRedo;
        }

        internal static void OnSelectionChange()
        {
            ResetGlobalHandleRotation();
            InvalidateHandlePosition();
            localHandleOffset = Vector3.zero;
        }

        internal static void OnUndoRedo(in UndoRedoInfo info)
        {
            OnSelectionChange();
        }

        internal static void ResetGlobalHandleRotation()
        {
            if (pivotRotation == PivotRotation.Global)
            {
                handleRotation = Quaternion.identity;
            }
            else if (pivotRotation == PivotRotation.Grid)
            {
                handleRotation = GridSettings.instance.rotation;
            }
        }

        internal Quaternion m_GlobalHandleRotation = Quaternion.identity;
        internal static Quaternion globalHandleRotation => get.m_GlobalHandleRotation;

        ViewTool m_ViewTool = ViewTool.Pan;

        internal static PivotMode GetPivotMode(Type toolOwnerType)
        {
            var ownerState = EditorToolManager.GetEditorToolStateForOwner(toolOwnerType);
            if (ownerState != null)
                return ownerState.pivotMode;

            return default;
        }

        internal static void SetPivotMode(PivotMode pivotMode, Type toolOwnerType)
        {
            var ownerState = EditorToolManager.GetEditorToolStateForOwner(toolOwnerType);
            if (ownerState != null)
                ownerState.pivotMode = pivotMode;
        }

        internal static PivotRotation GetPivotRotation(Type toolOwnerType)
        {
            var ownerState = EditorToolManager.GetEditorToolStateForOwner(toolOwnerType);
            if (ownerState != null)
                return ownerState.pivotRotation;

            return default;
        }

        internal static void SetPivotRotation(PivotRotation pivotRotation, Type toolOwnerType)
        {
            var ownerState = EditorToolManager.GetEditorToolStateForOwner(toolOwnerType);
            if (ownerState != null)
                ownerState.pivotRotation = pivotRotation;
        }

        static void SetToolMode(Tool toolMode)
        {
            SetToolMode(toolMode, typeof(SceneView));
        }
        
        static void SetToolMode(Tool toolMode, Type toolOwner)
        {
            SetCurrent(toolMode, toolOwner);
            Toolbar.instance?.Repaint();
            ResetGlobalHandleRotation();
        }

        [Shortcut("Tools/View", KeyCode.Q)]
        [FormerlyPrefKeyAs("Tools/View", "q")]
        static void SetToolModeView(ShortcutArguments args)
        {
            var toolOwner = EditorToolUtility.GetToolOwnerFromFocusedWindow();
            SetToolMode(Tool.View, toolOwner);
        }

        [Shortcut("Tools/Move", KeyCode.W)]
        [FormerlyPrefKeyAs("Tools/Move", "w")]
        static void SetToolModeMove(ShortcutArguments args)
        {
            var toolOwner = EditorToolUtility.GetToolOwnerFromFocusedWindow();
            SetToolMode(Tool.Move, toolOwner);
        }

        [Shortcut("Tools/Rotate", KeyCode.E)]
        [FormerlyPrefKeyAs("Tools/Rotate", "e")]
        static void SetToolModeRotate(ShortcutArguments args)
        {
            var toolOwner = EditorToolUtility.GetToolOwnerFromFocusedWindow();
            SetToolMode(Tool.Rotate, toolOwner);
        }

        [Shortcut("Tools/Scale", KeyCode.R)]
        [FormerlyPrefKeyAs("Tools/Scale", "r")]
        static void SetToolModeScale(ShortcutArguments args)
        {
            var toolOwner = EditorToolUtility.GetToolOwnerFromFocusedWindow();
            SetToolMode(Tool.Scale, toolOwner);
        }

        [Shortcut("Tools/Rect", KeyCode.T)]
        [FormerlyPrefKeyAs("Tools/Rect Handles", "t")]
        static void SetToolModeRect(ShortcutArguments args)
        {
            var toolOwner = EditorToolUtility.GetToolOwnerFromFocusedWindow();
            SetToolMode(Tool.Rect, toolOwner);
        }

        [Shortcut("Tools/Transform", KeyCode.Y)]
        [FormerlyPrefKeyAs("Tools/Transform Handles", "y")]
        static void SetToolModeTransform(ShortcutArguments args)
        {
            var toolOwner = EditorToolUtility.GetToolOwnerFromFocusedWindow();
            SetToolMode(Tool.Transform, toolOwner);
        }

        [Shortcut("Tools/Toggle Pivot Position", KeyCode.Z)]
        [FormerlyPrefKeyAs("Tools/Pivot Mode", "z")]
        static void TogglePivotMode(ShortcutArguments args)
        {
            var toolsOwner = EditorToolUtility.GetToolOwnerFromFocusedWindow();
            var pivotState = EditorPivotManager.instance.GetOrCreateStateForType(toolsOwner);
            if (pivotState != null)
            {
                var nextModeType = pivotState.GetNextPivotModeType();
                PivotManager.SetActivePivotMode(nextModeType, toolsOwner);

                ResetGlobalHandleRotation();
                RepaintAllToolViews();
            }
        }

        [Shortcut("Tools/Toggle Pivot Orientation", KeyCode.X)]
        [FormerlyPrefKeyAs("Tools/Pivot Rotation", "x")]
        static void TogglePivotRotation(ShortcutArguments args)
        {
            var toolsOwner = EditorToolUtility.GetToolOwnerFromFocusedWindow();
            var pivotState = EditorPivotManager.instance.GetOrCreateStateForType(toolsOwner);
            if (pivotState != null)
            {
                var nextRotationType = pivotState.GetNextPivotRotationType();
                PivotManager.SetActivePivotRotation(nextRotationType, toolsOwner);

                ResetGlobalHandleRotation();
                RepaintAllToolViews();
            }
        }

        internal static void RepaintAllToolViews()
        {
            Toolbar.RepaintToolbar();
            SceneView.RepaintAll();
            InspectorWindow.RepaintAllInspectors();
        }

        internal static void LockHandlePosition(Vector3 pos)
        {
            s_LockHandlePosition = pos;
            s_LockHandlePositionActive = true;
        }

        internal static Vector3 handleOffset;

        internal static Vector3 localHandleOffset;

        internal static void LockHandlePosition()
        {
            LockHandlePosition(handlePosition);
        }

        internal static void UnlockHandlePosition()
        {
            s_LockHandlePositionActive = false;
        }

        internal static Quaternion handleLocalRotation
        {
            get
            {
                return LocalPivotRotation.RetrieveLocalRotation();
            }
        }
    }
}
