// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal;

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
        Center = 0,
        Pivot = 1
    }

    public enum PivotRotation
    {
        Local = 0,
        Global = 1
    }

    public enum Tool
    {
        View = 0,
        Move = 1,
        Rotate = 2,
        Scale = 3,
        Rect = 4,
        Transform = 5,
        None = -1
    }

    public sealed partial class Tools : ScriptableObject
    {
        private static Tools get
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
        private static Tools s_Get;

        internal delegate void OnToolChangedFunc(Tool from, Tool to);
        internal static OnToolChangedFunc onToolChanged;

        public static Tool current
        {
            get { return get.currentTool; }
            set
            {
                if (get.currentTool != value)
                {
                    Tool oldTool = get.currentTool;
                    get.currentTool = value;
                    if (onToolChanged != null)
                        onToolChanged(oldTool, value);
                    Tools.RepaintAllToolViews();
                }
            }
        }

        public static ViewTool viewTool
        {
            get
            {
                Event evt = Event.current;
                if (evt != null && viewToolActive)
                {
                    if (s_LockedViewTool == ViewTool.None)
                    {
                        bool controlKeyOnMac = (evt.control && Application.platform == RuntimePlatform.OSXEditor);
                        bool actionKey = EditorGUI.actionKey;
                        bool noModifiers = (!actionKey && !controlKeyOnMac && !evt.alt);

                        if ((s_ButtonDown <= 0 && noModifiers)
                            || (s_ButtonDown <= 0 && actionKey)
                            || s_ButtonDown == 2
                            || SceneView.lastActiveSceneView != null && (SceneView.lastActiveSceneView.in2DMode || SceneView.lastActiveSceneView.isRotationLocked) && !(s_ButtonDown == 1 && evt.alt || s_ButtonDown <= 0 && controlKeyOnMac)
                            )
                        {
                            get.m_ViewTool = ViewTool.Pan;
                        }
                        else if ((s_ButtonDown <= 0 && controlKeyOnMac)
                                 || (s_ButtonDown == 1 && evt.alt))
                        {
                            get.m_ViewTool = ViewTool.Zoom;
                        }
                        else if (s_ButtonDown <= 0 && evt.alt)
                        {
                            get.m_ViewTool = ViewTool.Orbit;
                        }
                        else if (s_ButtonDown == 1 && !evt.alt)
                        {
                            get.m_ViewTool = ViewTool.FPS;
                        }
                    }
                }
                else
                {
                    get.m_ViewTool = ViewTool.Pan;
                }
                return get.m_ViewTool;
            }
            set { get.m_ViewTool = value; }
        }
        internal static ViewTool s_LockedViewTool = ViewTool.None;
        internal static int s_ButtonDown = -1;
        internal static bool viewToolActive
        {
            get
            {
                if (GUIUtility.hotControl != 0 && s_LockedViewTool == ViewTool.None)
                    return false;

                return s_LockedViewTool != ViewTool.None || (current == 0) || Event.current.alt || (s_ButtonDown == 1) || (s_ButtonDown == 2);
            }
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

                return GetHandlePosition();
            }
        }

        internal static Vector3 GetHandlePosition()
        {
            Transform t = Selection.activeTransform;
            if (!t)
                return new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);

            Vector3 totalOffset = handleOffset + handleRotation * localHandleOffset;

            switch (get.m_PivotMode)
            {
                case PivotMode.Center:
                {
                    if (current == Tool.Rect)
                        return handleRotation * InternalEditorUtility.CalculateSelectionBoundsInSpace(Vector3.zero, handleRotation, rectBlueprintMode).center + totalOffset;
                    else
                        return InternalEditorUtility.CalculateSelectionBounds(true, false).center + totalOffset;
                }
                case PivotMode.Pivot:
                {
                    if (current == Tool.Rect && rectBlueprintMode && InternalEditorUtility.SupportsRectLayout(t))
                        return t.parent.TransformPoint(new Vector3(t.localPosition.x, t.localPosition.y, 0)) + totalOffset;
                    else
                        return t.position + totalOffset;
                }
                default:
                {
                    return new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
                }
            }
        }

        public static Rect handleRect
        {
            get
            {
                Bounds bounds = InternalEditorUtility.CalculateSelectionBoundsInSpace(handlePosition, handleRotation, rectBlueprintMode);
                int axis = GetRectAxisForViewDir(bounds, handleRotation, SceneView.currentDrawingSceneView.camera.transform.forward);
                return GetRectFromBoundsForAxis(bounds, axis);
            }
        }

        public static Quaternion handleRectRotation
        {
            get
            {
                Bounds bounds = InternalEditorUtility.CalculateSelectionBoundsInSpace(handlePosition, handleRotation, rectBlueprintMode);
                int axis = GetRectAxisForViewDir(bounds, handleRotation, SceneView.currentDrawingSceneView.camera.transform.forward);
                return GetRectRotationForAxis(handleRotation, axis);
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
            get { return get.m_PivotMode; }
            set
            {
                if (get.m_PivotMode != value)
                {
                    get.m_PivotMode = value;
                    EditorPrefs.SetInt("PivotMode", (int)pivotMode);
                }
            }
        }
        private PivotMode m_PivotMode;

        internal static int GetPivotMode()
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
            get
            {
                switch (get.m_PivotRotation)
                {
                    case PivotRotation.Global:
                        return get.m_GlobalHandleRotation;
                    case PivotRotation.Local:
                        return handleLocalRotation;
                }
                return Quaternion.identity;
            }
            set
            {
                if (get.m_PivotRotation == PivotRotation.Global)
                    get.m_GlobalHandleRotation = value;
            }
        }

        public static PivotRotation pivotRotation
        {
            get { return get.m_PivotRotation; }
            set
            {
                if (get.m_PivotRotation != value)
                {
                    get.m_PivotRotation = value;
                    EditorPrefs.SetInt("PivotRotation", (int)pivotRotation);
                }
            }
        }
        private PivotRotation m_PivotRotation;

        internal static bool s_Hidden = false;

        public static bool hidden
        {
            get { return s_Hidden; }
            set { s_Hidden = value; }
        }

        internal static bool vertexDragging;

        static Vector3 s_LockHandlePosition;
        static bool s_LockHandlePositionActive = false;

        static int s_LockHandleRectAxis;
        static bool s_LockHandleRectAxisActive = false;

        public static int visibleLayers
        {
            get { return get.m_VisibleLayers; }
            set
            {
                if (get.m_VisibleLayers != value)
                {
                    get.m_VisibleLayers = value;
                    EditorGUIUtility.SetVisibleLayers(value);
                    EditorPrefs.SetInt("VisibleLayers", visibleLayers);
                }
            }
        }
        int m_VisibleLayers = -1;

        public static int lockedLayers
        {
            get { return get.m_LockedLayers; }
            set
            {
                if (get.m_LockedLayers != value)
                {
                    get.m_LockedLayers = value;
                    EditorGUIUtility.SetLockedLayers(value);
                    EditorPrefs.SetInt("LockedLayers", lockedLayers);
                }
            }
        }
        int m_LockedLayers = -1;

        private void OnEnable()
        {
            s_Get = this;
            EditorApplication.globalEventHandler += ControlsHack;
            pivotMode = (PivotMode)EditorPrefs.GetInt("PivotMode", 0);
            rectBlueprintMode = EditorPrefs.GetBool("RectBlueprintMode", false);
            pivotRotation = (PivotRotation)EditorPrefs.GetInt("PivotRotation", 0);
            visibleLayers = EditorPrefs.GetInt("VisibleLayers", -1);
            lockedLayers = EditorPrefs.GetInt("LockedLayers", 0);
        }

        private void OnDisable()
        {
            EditorApplication.globalEventHandler -= ControlsHack;
        }

        internal static void OnSelectionChange()
        {
            ResetGlobalHandleRotation();
            localHandleOffset = Vector3.zero;
        }

        internal static void ResetGlobalHandleRotation()
        {
            get.m_GlobalHandleRotation = Quaternion.identity;
        }

        internal Quaternion m_GlobalHandleRotation = Quaternion.identity;
        Tool currentTool = Tool.Move;
        ViewTool m_ViewTool = ViewTool.Pan;

        static PrefKey kViewKey = new PrefKey("Tools/View", "q");
        static PrefKey kMoveKey = new PrefKey("Tools/Move", "w");
        static PrefKey kRotateKey = new PrefKey("Tools/Rotate", "e");
        static PrefKey kScaleKey = new PrefKey("Tools/Scale", "r");
        static PrefKey kRectKey = new PrefKey("Tools/Rect Handles", "t");
        static PrefKey kTransformKey = new PrefKey("Tools/Transform Handles", "y");
        static PrefKey kPivotMode = new PrefKey("Tools/Pivot Mode", "z");
        static PrefKey kPivotRotation = new PrefKey("Tools/Pivot Rotation", "x");

        internal static void ControlsHack()
        {
            Event evt = Event.current;
            if (kViewKey.activated)
            {
                Tools.current = 0;
                ResetGlobalHandleRotation();
                evt.Use();
                if (Toolbar.get)
                    Toolbar.get.Repaint();
                else
                    Debug.LogError("Press Play twice for sceneview keyboard shortcuts to work");
            }
            if (kMoveKey.activated)
            {
                Tools.current = Tool.Move;
                ResetGlobalHandleRotation();
                evt.Use();
                if (Toolbar.get)
                    Toolbar.get.Repaint();
                else
                    Debug.LogError("Press Play twice for sceneview keyboard shortcuts to work");
            }
            if (kRotateKey.activated)
            {
                Tools.current = Tool.Rotate;
                ResetGlobalHandleRotation();
                evt.Use();
                if (Toolbar.get)
                    Toolbar.get.Repaint();
                else
                    Debug.LogError("Press Play twice for sceneview keyboard shortcuts to work");
            }
            if (kScaleKey.activated)
            {
                Tools.current = Tool.Scale;
                ResetGlobalHandleRotation();
                evt.Use();
                if (Toolbar.get)
                    Toolbar.get.Repaint();
                else
                    Debug.LogError("Press Play twice for sceneview keyboard shortcuts to work");
            }
            if (kRectKey.activated)
            {
                Tools.current = Tool.Rect;
                ResetGlobalHandleRotation();
                evt.Use();
                if (Toolbar.get)
                    Toolbar.get.Repaint();
                else
                    Debug.LogError("Press Play twice for sceneview keyboard shortcuts to work");
            }
            if (kTransformKey.activated)
            {
                Tools.current = Tool.Transform;
                ResetGlobalHandleRotation();
                evt.Use();
                if (Toolbar.get)
                    Toolbar.get.Repaint();
                else
                    Debug.LogError("Press Play twice for sceneview keyboard shortcuts to work");
            }
            if (kPivotMode.activated)
            {
                pivotMode = (PivotMode)(1 - (int)pivotMode);
                ResetGlobalHandleRotation();
                evt.Use();
                Tools.RepaintAllToolViews();
            }
            if (kPivotRotation.activated)
            {
                pivotRotation = (PivotRotation)(1 - (int)pivotRotation);
                ResetGlobalHandleRotation();
                evt.Use();
                Tools.RepaintAllToolViews();
            }
        }

        internal static void RepaintAllToolViews()
        {
            if (Toolbar.get)
                Toolbar.get.Repaint();
            SceneView.RepaintAll();
            InspectorWindow.RepaintAllInspectors();
        }

        internal static void HandleKeys()
        {
            ControlsHack();
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
                Transform t = Selection.activeTransform;
                if (!t)
                    return Quaternion.identity;
                if (rectBlueprintMode && InternalEditorUtility.SupportsRectLayout(t))
                    return t.parent.rotation;
                return t.rotation;
            }
        }
    }
}
