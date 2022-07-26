// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.AnimatedValues;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace UnityEditor
{
    internal static class SceneViewMotion
    {
        [NonSerialized]
        static bool s_Initialized;
        static SceneView s_CurrentSceneView; // The SceneView that is calling OnGUI
        static SceneView s_ActiveSceneView; // The SceneView that is being navigated
        static Vector3 s_Motion;
        internal static float k_FlySpeed = 9f;
        static float s_FlySpeedTarget = 0f;
        const float k_FlySpeedAcceleration = 1.8f;
        static float s_StartZoom = 0f, s_ZoomSpeed = 0f;
        static float s_TotalMotion = 0f;
        static float s_FPSScrollWheelMultiplier = .01f;
        static bool s_Moving;
        static bool s_Drag;
        static AnimVector3 s_FlySpeed = new AnimVector3(Vector3.zero);

        internal static Vector3 cameraSpeed
        {
            get { return s_FlySpeed.value; }
        }

        enum MotionState
        {
            kInactive,
            kActive,
            kDragging
        }

        static MotionState s_CurrentState;

        static int s_ViewToolID = GUIUtility.GetPermanentControlID();

        static readonly CameraFlyModeContext s_CameraFlyModeContext = new CameraFlyModeContext();

        static bool s_ViewToolActive = false;
        internal static bool viewToolActive
        {
            get
            {
                if (Event.current != null)
                    UpdateViewToolState(Event.current);
                return s_ViewToolActive;
            }
        }
        internal static event Action viewToolActiveChanged;

        static void Init()
        {
            if (s_Initialized)
                return;
            ShortcutIntegration.instance.contextManager.RegisterToolContext(new SceneViewViewport());
            ShortcutIntegration.instance.contextManager.RegisterToolContext(new SceneViewViewport2D());
            ShortcutIntegration.instance.contextManager.RegisterToolContext(new SceneViewViewport3D());
            s_Initialized = true;
        }

        class SceneViewViewport : IShortcutToolContext
        {
            public bool active => IsActive;

            public static bool IsActive
            {
                get => EditorWindow.focusedWindow?.GetType() == typeof(SceneView)
                    && SceneView.lastActiveSceneView.rootVisualElement.worldBound.Contains(Event.current.mousePosition);
            }
        }

        class SceneViewViewport2D : IShortcutToolContext
        {
            public bool active => SceneViewViewport.IsActive
                && ((SceneView.lastActiveSceneView?.in2DMode ?? false) || (SceneView.lastActiveSceneView?.isRotationLocked ?? false));
        }

        class SceneViewViewport3D : IShortcutToolContext
        {
            public bool active => SceneViewViewport.IsActive
                && ((!SceneView.lastActiveSceneView?.in2DMode ?? false) && (!SceneView.lastActiveSceneView?.isRotationLocked ?? false));
        }

        [ClutchShortcut("Scene View/Temporary Pan Tool for 2D Mode", typeof(SceneViewViewport2D), KeyCode.Mouse1)]
        [ClutchShortcut("Scene View/Temporary Pan Tool 1", typeof(SceneViewViewport), KeyCode.Mouse2)]
        [ClutchShortcut("Scene View/Temporary Pan Tool 2", typeof(SceneViewViewport), KeyCode.Mouse2, ShortcutModifiers.Alt)]
        static void TemporaryPan(ShortcutArguments args)
        {
            if (args.stage == ShortcutStage.Begin) TemporaryTool(ViewTool.Pan);
            else HandleMouseUp(s_CurrentSceneView, s_ViewToolID, 0, 0);
        }

        [ClutchShortcut("Scene View/Temporary Zoom Tool", typeof(SceneViewViewport), KeyCode.Mouse1, ShortcutModifiers.Alt)]
        static void TemporaryZoom(ShortcutArguments args)
        {
            if (args.stage == ShortcutStage.Begin) TemporaryTool(ViewTool.Zoom);
            else HandleMouseUp(s_CurrentSceneView, s_ViewToolID, 0, 0);
        }

        [ClutchShortcut("Scene View/Temporary Orbit Tool", typeof(SceneViewViewport), KeyCode.Mouse0, ShortcutModifiers.Alt)]
        static void TemporaryOrbit(ShortcutArguments args)
        {
            if (args.stage == ShortcutStage.Begin) TemporaryTool(ViewTool.Orbit);
            else HandleMouseUp(s_CurrentSceneView, s_ViewToolID, 0, 0);
        }

        [ClutchShortcut("Scene View/Temporary FPS Tool", typeof(SceneViewViewport3D), KeyCode.Mouse1)]
        static void TemporaryFPS(ShortcutArguments args)
        {
            if (args.stage == ShortcutStage.Begin)
            {
                TemporaryTool(ViewTool.FPS);
                s_CameraFlyModeContext.active = true;
            }
            else
            {
                HandleMouseUp(s_CurrentSceneView, s_ViewToolID, 0, 0);
                s_CameraFlyModeContext.active = false;
            }
        }

        static KeyCode shortcutKey;
        static void TemporaryTool(ViewTool tool)
        {
            Tools.s_LockedViewTool = Tools.viewTool = tool;
            s_CurrentState = MotionState.kDragging;
            HandleMouseDown(SceneView.lastActiveSceneView, s_ViewToolID, Event.current?.button ?? 0);
            UpdateViewToolState(Event.current);
            if(Event.current != null) shortcutKey = Event.current.isMouse ? KeyCode.Mouse0 + Event.current.button : Event.current.keyCode;
            else shortcutKey = KeyCode.None;
        }

        public static void DoViewTool(SceneView view)
        {
            Init();

            s_CurrentSceneView = view;

            // If a SceneView is currently taking input, don't let other views accept input
            if (s_ActiveSceneView != null && s_CurrentSceneView != s_ActiveSceneView)
                return;

            Event evt = Event.current;

            // Ensure we always call the GetControlID the same number of times
            int id = s_ViewToolID;

            EventType eventType = evt.GetTypeForControl(id);

            // In FPS mode we update the pivot for Orbit mode (see below and inside HandleMouseDrag)
            if (view && Tools.s_LockedViewTool == ViewTool.FPS)
            {
                view.FixNegativeSize();
            }

            using (var inputSamplingScope = new CameraFlyModeContext.InputSamplingScope(s_CameraFlyModeContext, Tools.s_LockedViewTool, id, view, view.orthographic))
            {
                if (inputSamplingScope.currentlyMoving)
                    view.viewIsLockedToObject = false;

                s_Motion = inputSamplingScope.currentInputVector;
            }

            switch (eventType)
            {
                case EventType.ScrollWheel: HandleScrollWheel(view, view.in2DMode == evt.alt); break; // Default to zooming to mouse position in 2D mode without alt
                case EventType.MouseDown: HandleMouseDown(view, id, evt.button); break;
                case EventType.KeyUp:
                case EventType.MouseUp: HandleMouseUp(view, id, evt.button, evt.clickCount); break;
                case EventType.KeyDown: HandleKeyDown(view, id); break;
                case EventType.MouseMove:
                case EventType.MouseDrag: HandleMouseDrag(view, id); break;
                case EventType.Layout:
                    if (GUIUtility.hotControl == id || s_FlySpeed.isAnimating || s_Moving)
                    {
                        view.pivot = view.pivot + view.rotation * GetMovementDirection();
                        view.Repaint();
                    }
                    break;
            }

            if (s_CurrentState == MotionState.kDragging && evt.type == EventType.Repaint)
            {
                HandleMouseDrag(view, id);
            }

            if (shortcutKey != KeyCode.None && Tools.viewTool != ViewTool.None) GUIUtility.hotControl = s_ViewToolID;
        }

        static void UpdateViewToolState(Event evt)
        {
            bool shouldBeActive = Tools.s_LockedViewTool != ViewTool.None;
            if (shouldBeActive != s_ViewToolActive)
            {
                s_ViewToolActive = shouldBeActive;
                viewToolActiveChanged?.Invoke();
            }
        }

        static Vector3 GetMovementDirection()
        {
            s_Moving = s_Motion.sqrMagnitude > 0f;
            var deltaTime = CameraFlyModeContext.deltaTime;
            var speedModifier = s_CurrentSceneView.cameraSettings.speed;

            if (Event.current.shift)
                speedModifier *= 5f;

            if (s_Moving)
            {
                if (s_CurrentSceneView.cameraSettings.accelerationEnabled)
                    s_FlySpeedTarget = s_FlySpeedTarget < Mathf.Epsilon ? k_FlySpeed : s_FlySpeedTarget * Mathf.Pow(k_FlySpeedAcceleration, deltaTime);
                else
                    s_FlySpeedTarget = k_FlySpeed;
            }
            else
            {
                s_FlySpeedTarget = 0f;
            }

            if (s_CurrentSceneView.cameraSettings.easingEnabled)
            {
                s_FlySpeed.speed = 1f / s_CurrentSceneView.cameraSettings.easingDuration;
                s_FlySpeed.target = s_Motion.normalized * s_FlySpeedTarget * speedModifier;
            }
            else
            {
                s_FlySpeed.value = s_Motion.normalized * s_FlySpeedTarget * speedModifier;
            }

            return s_FlySpeed.value * deltaTime;
        }

        private static void HandleMouseDown(SceneView view, int id, int button)
        {
            Event evt = Event.current;

            // Set up zoom parameters
            s_StartZoom = view.size;
            s_ZoomSpeed = Mathf.Max(Mathf.Abs(s_StartZoom), .3f);
            s_TotalMotion = 0;

            if (view)
                view.Focus();

            if (Toolbar.get)
                Toolbar.get.Repaint();
            EditorGUIUtility.SetWantsMouseJumping(1);
            s_ActiveSceneView = s_CurrentSceneView;

            if (Tools.s_LockedViewTool == ViewTool.None && Tools.current == Tool.View)
            {
                bool controlKeyOnMac = (evt.control && Application.platform == RuntimePlatform.OSXEditor);
                bool actionKey = EditorGUI.actionKey;
                bool noModifiers = (!actionKey && !controlKeyOnMac && !evt.alt);
                if (evt.button == 0 && noModifiers) TemporaryPan(new ShortcutArguments() { stage = ShortcutStage.Begin});
            }
        }

        internal static void ResetDragState()
        {
            s_ActiveSceneView = null;
            if (GUIUtility.hotControl == s_ViewToolID)
                GUIUtility.hotControl = 0;
            s_CurrentState = MotionState.kInactive;
            Tools.s_LockedViewTool = ViewTool.None;
            Tools.s_ButtonDown = -1;
            if (Toolbar.get)
                Toolbar.get.Repaint();
            EditorGUIUtility.SetWantsMouseJumping(0);
            s_Drag = false;
        }

        internal static void ResetMotion()
        {
            s_Motion = Vector3.zero;
            s_FlySpeed.value = Vector3.zero;
            s_Moving = false;
        }

        private static void HandleMouseUp(SceneView view, int id, int button, int clickCount)
        {
            if (GUIUtility.hotControl == id && (shortcutKey == KeyCode.None || shortcutKey == (Event.current.keyCode == KeyCode.None ? KeyCode.Mouse0 + Event.current.button : Event.current.keyCode)))
            {
                // Move pivot to clicked point.
                if (Tools.s_LockedViewTool == ViewTool.Pan && !s_Drag)
                {
                    RaycastHit hit;
                    if (RaycastWorld(Event.current.mousePosition, out hit))
                    {
                        Vector3 currentPosition = view.pivot - view.rotation * Vector3.forward * view.cameraDistance;
                        float targetSize = view.size;
                        if (!view.orthographic)
                            targetSize = view.size * Vector3.Dot(hit.point - currentPosition, view.rotation * Vector3.forward) / view.cameraDistance;
                        view.LookAt(hit.point, view.rotation, targetSize);
                    }
                }

                Tools.viewTool = ViewTool.Pan;
                Tools.s_LockedViewTool = ViewTool.None;
                shortcutKey = KeyCode.None;
                ResetDragState();
                viewToolActiveChanged?.Invoke();
            }
        }

        static bool RaycastWorld(Vector2 position, out RaycastHit hit)
        {
            hit = new RaycastHit();
            GameObject picked = HandleUtility.PickGameObject(position, false);
            if (!picked)
                return false;

            Ray mouseRay = HandleUtility.GUIPointToWorldRay(position);

            // Loop through all meshes and find the RaycastHit closest to the ray origin.
            MeshFilter[] meshFil = picked.GetComponentsInChildren<MeshFilter>();
            float minT = Mathf.Infinity;
            foreach (MeshFilter mf in meshFil)
            {
                Mesh mesh = mf.sharedMesh;
                if (!mesh || !mesh.canAccess)
                    continue;

                RaycastHit localHit;
                if (HandleUtility.IntersectRayMesh(mouseRay, mesh, mf.transform.localToWorldMatrix, out localHit))
                {
                    if (localHit.distance < minT)
                    {
                        hit = localHit;
                        minT = hit.distance;
                    }
                }
            }

            if (minT == Mathf.Infinity)
            {
                // If we didn't find any surface based on meshes, try with colliders.
                Collider[] colliders = picked.GetComponentsInChildren<Collider>();
                foreach (Collider col in colliders)
                {
                    RaycastHit localHit;
                    if (col.Raycast(mouseRay, out localHit, Mathf.Infinity))
                    {
                        if (localHit.distance < minT)
                        {
                            hit = localHit;
                            minT = hit.distance;
                        }
                    }
                }
            }

            if (minT == Mathf.Infinity)
            {
                // If we didn't hit any mesh or collider surface, then use the transform position projected onto the ray.
                hit.point = Vector3.Project(picked.transform.position - mouseRay.origin, mouseRay.direction) + mouseRay.origin;
            }
            return true;
        }

        private static void OrbitCameraBehavior(SceneView view)
        {
            Event evt = Event.current;

            view.FixNegativeSize();
            Quaternion rotation = view.m_Rotation.target;
            rotation = Quaternion.AngleAxis(evt.delta.y * .003f * Mathf.Rad2Deg, rotation * Vector3.right) * rotation;
            rotation = Quaternion.AngleAxis(evt.delta.x * .003f * Mathf.Rad2Deg, Vector3.up) * rotation;
            if (view.size < 0)
            {
                view.pivot = view.camera.transform.position;
                view.size = 0;
            }
            view.rotation = rotation;
        }

        private static void HandleMouseDrag(SceneView view, int id)
        {
            if (!Event.current.isMouse || GUIUtility.hotControl != id) return;

            s_Drag = true;
            Event evt = Event.current;
            switch (Tools.s_LockedViewTool)
            {
                case ViewTool.Orbit:
                {
                    if (!view.in2DMode && !view.isRotationLocked)
                    {
                        OrbitCameraBehavior(view);
                        // todo gizmo update label
                        // view.m_OrientationGizmo.UpdateGizmoLabel(view, view.rotation * Vector3.forward, view.m_Ortho.target);
                    }
                }
                break;
                case ViewTool.FPS:
                {
                    if (!view.in2DMode && !view.isRotationLocked)
                    {
                        if (!view.orthographic)
                        {
                            view.viewIsLockedToObject = false;

                            // The reason we calculate the camera position from the pivot, rotation and distance,
                            // rather than just getting it from the camera transform is that the camera transform
                            // is the *output* of camera motion calculations. It shouldn't be input and output at the same time,
                            // otherwise we easily get accumulated error.
                            // We did get accumulated error before when we did this - the camera would continuously move slightly in FPS mode
                            // even when not holding down any arrow/ASDW keys or moving the mouse.
                            Vector3 camPos = view.pivot - view.rotation * Vector3.forward * view.cameraDistance;

                            // Normal FPS camera behavior
                            Quaternion rotation = view.rotation;
                            rotation = Quaternion.AngleAxis(evt.delta.y * .003f * Mathf.Rad2Deg, rotation * Vector3.right) * rotation;
                            rotation = Quaternion.AngleAxis(evt.delta.x * .003f * Mathf.Rad2Deg, Vector3.up) * rotation;
                            view.rotation = rotation;

                            view.pivot = camPos + rotation * Vector3.forward * view.cameraDistance;
                        }
                        else
                        {
                            // We want orbit behavior in orthograpic when using FPS
                            OrbitCameraBehavior(view);
                        }

                        // todo gizmo update label
                        // view.m_OrientationGizmo.UpdateGizmoLabel(view, view.rotation * Vector3.forward, view.m_Ortho.target);
                    }
                }
                break;
                case ViewTool.Pan:
                {
                    view.viewIsLockedToObject = false;
                    view.FixNegativeSize();

                    Vector2 screenDelta = Event.current.delta;
                    Vector3 worldDelta = ScreenToWorldDistance(view, new Vector2(-screenDelta.x, screenDelta.y));

                    if (evt.shift)
                        worldDelta *= 4;

                    view.pivot += worldDelta;
                }
                break;
                case ViewTool.Zoom:
                {
                    float zoomDelta = HandleUtility.niceMouseDeltaZoom * (evt.shift ? 9 : 3);

                    if (view.orthographic)
                    {
                        view.size = Mathf.Max(.0001f, view.size * (1 + zoomDelta * .001f));
                    }
                    else
                    {
                        s_TotalMotion += zoomDelta;

                        if (s_TotalMotion < 0)
                            view.size = s_StartZoom * (1 + s_TotalMotion * .001f);
                        else
                            view.size = view.size + zoomDelta * s_ZoomSpeed * .003f;
                    }
                }
                break;
            }
        }

        internal static Vector3 ScreenToWorldDistance(SceneView view, Vector2 delta)
        {
            // Ensures that the camera matrix doesn't end up with gigantic or minuscule values in the clip to world matrix
            const float k_MaxCameraSizeForWorldToScreen = 2.5E+7f;

            // store original camera and view values
            var camera = view.camera;
            var near = camera.nearClipPlane;
            var far = camera.farClipPlane;
            var pos = camera.transform.position;
            var size = view.size;

            // set camera transform and clip values to safe values
            view.size = Mathf.Min(view.size, k_MaxCameraSizeForWorldToScreen);
            // account for the distance clamping
            var scale = size / view.size;
            var clip = view.GetDynamicClipPlanes();
            view.camera.nearClipPlane = clip.x;
            view.camera.farClipPlane = clip.y;
            view.camera.transform.position = Vector3.zero;

            // do the distance calculation
            Vector3 pivot = camera.transform.rotation * new Vector3(0f, 0f, view.cameraDistance);
            Vector3 screenPos = camera.WorldToScreenPoint(pivot);
            screenPos += new Vector3(delta.x, delta.y, 0);
            Vector3 worldDelta = camera.ScreenToWorldPoint(screenPos) - pivot;
            worldDelta *= EditorGUIUtility.pixelsPerPoint * scale;

            // restore original cam and scene values
            view.size = size;
            view.camera.nearClipPlane = near;
            view.camera.farClipPlane = far;
            view.camera.transform.position = pos;

            return worldDelta;
        }

        private static void HandleKeyDown(SceneView sceneView, int id)
        {
            if (Event.current.keyCode == KeyCode.Escape && GUIUtility.hotControl == s_ViewToolID)
            {
                GUIUtility.hotControl = 0;
                ResetDragState();
            }
        }

        static readonly char[] k_TrimChars = new char[] { '0' };

        private static void HandleScrollWheel(SceneView view, bool zoomTowardsCenter)
        {
            if (Tools.s_LockedViewTool == ViewTool.FPS)
            {
                float scrollWheelDelta = Event.current.delta.y * s_FPSScrollWheelMultiplier;
                view.cameraSettings.speedNormalized -= scrollWheelDelta;
                float cameraSettingsSpeed = view.cameraSettings.speed;
                string cameraSpeedDisplayValue = cameraSettingsSpeed.ToString(
                    cameraSettingsSpeed < 0.0001f  ? "F6" :
                    cameraSettingsSpeed < 0.001f  ? "F5" :
                    cameraSettingsSpeed < 0.01f  ? "F4" :
                    cameraSettingsSpeed < 0.1f  ? "F3" :
                    cameraSettingsSpeed < 10f ? "F2" :
                    "F0");

                if (cameraSettingsSpeed < 0.1f)
                    cameraSpeedDisplayValue = cameraSpeedDisplayValue.Trim(k_TrimChars);

                GUIContent cameraSpeedContent = EditorGUIUtility.TempContent(string.Format("{0}{1}",
                    cameraSpeedDisplayValue,
                    view.cameraSettings.accelerationEnabled ? "x" : ""));

                view.ShowNotification(cameraSpeedContent, .5f);
            }
            else
            {
                float zoomDelta = Event.current.delta.y;
                float targetSize;

                if (!view.orthographic)
                {
                    float relativeDelta = Mathf.Abs(view.size) * zoomDelta * .015f;
                    const float k_MinZoomDelta = .0001f;
                    if (relativeDelta > 0 && relativeDelta < k_MinZoomDelta)
                        relativeDelta = k_MinZoomDelta;
                    else if (relativeDelta < 0 && relativeDelta > -k_MinZoomDelta)
                        relativeDelta = -k_MinZoomDelta;

                    targetSize = view.size + relativeDelta;
                }
                else
                {
                    targetSize = Mathf.Abs(view.size) * (zoomDelta * .015f + 1.0f);
                }

                var initialDistance = view.cameraDistance;

                if (!(float.IsNaN(targetSize) || float.IsInfinity(targetSize)))
                {
                    targetSize = Mathf.Min(SceneView.k_MaxSceneViewSize, targetSize);
                    view.size = targetSize;
                }

                if (!zoomTowardsCenter && Mathf.Abs(view.cameraDistance) < 1.0e7f)
                {
                    var percentage = 1f - (view.cameraDistance / initialDistance);

                    var mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                    var mousePivot = mouseRay.origin + mouseRay.direction * initialDistance;
                    var pivotVector = mousePivot - view.pivot;

                    view.pivot += pivotVector * percentage;
                }
            }

            Event.current.Use();
        }

        public static void DeactivateFlyModeContext()
        {
            ShortcutIntegration.instance.contextManager.DeregisterToolContext(s_CameraFlyModeContext);
        }
    }
} //namespace
