// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.AnimatedValues;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace UnityEditor
{
    class SceneViewMotion
    {
        const string k_TemporaryPanTool2D1 = "Scene View/Temporary Pan Tool for 2D Mode 1";
        const string k_TemporaryPanTool2D2 = "Scene View/Temporary Pan Tool for 2D Mode 2";
        const string k_TemporaryPanTool1 = "Scene View/Temporary Pan Tool 1";
        const string k_TemporaryPanTool2 = "Scene View/Temporary Pan Tool 2";
        const string k_TemporaryPanTool3 = "Scene View/Temporary Pan Tool 3";
        const string k_TemporaryPanTool4 = "Scene View/Temporary Pan Tool 4";
        const string k_TemporaryZoomTool1 = "Scene View/Temporary Zoom Tool 1";
        const string k_TemporaryZoomTool2 = "Scene View/Temporary Zoom Tool 2";
        const string k_TemporaryOrbitTool = "Scene View/Temporary Orbit Tool";
        const string k_TemporaryFpsTool = "Scene View/Temporary FPS Tool";
        const string k_PanFocusTool = "Scene View/Pan Focus Tool";
        const string k_LockedPanTool = "Scene View/Locked Pan Tool";
        const string k_LockedPanFocusTool = "Scene View/Locked Pan Focus Tool";

        const string k_PanFocusEventCommandName = "SceneViewPanFocusEventCommand";
        internal const string k_SetSceneViewMotionHotControlEventCommandName = "SetSceneViewMotionHotControlEventCommand"; // Also used in tests.

        bool m_Moving;
        static readonly CameraFlyModeContext s_CameraFlyModeContext = new CameraFlyModeContext();

        // used by Tests/EditModeAndPlayModeTests/SceneView/CameraFlyModeContextTests
        internal AnimVector3 m_FlySpeed = new AnimVector3(Vector3.zero);

        public static event Action viewToolActiveChanged;

        Vector3 m_Motion;

        Vector2 m_StartMousePosition; // The start mouse position is used for the pan focus tool.

        float m_FlySpeedTarget = 0f;
        float m_StartZoom = 0f;
        float m_ZoomSpeed = 0f;
        float m_TotalMotion = 0f;
        float m_FPSScrollWheelMultiplier = .01f;
        const float k_FlySpeedAcceleration = 1.8f;
        public const float k_FlySpeed = 9f; // Also used in tests.

        readonly int k_ViewToolID = GUIUtility.GetPermanentControlID();

        readonly char[] k_TrimChars = new char[] { '0' };

        public Vector3 cameraSpeed
        {
            get { return m_FlySpeed.value; }
        }

        bool m_Drag;
        public bool isDragging // This is used in SceneView.
        {
            get { return m_Drag; }
        }

        static bool s_ViewToolIsActive = false;
        public static bool viewToolIsActive => UpdateViewToolState();

        [InitializeOnLoadMethod]
        static void RegisterShortcutContexts() => EditorApplication.delayCall += () =>
        {
            ShortcutIntegration.instance.contextManager.RegisterToolContext(new SceneViewViewport());
            ShortcutIntegration.instance.contextManager.RegisterToolContext(new SceneViewViewport2D());
            ShortcutIntegration.instance.contextManager.RegisterToolContext(new SceneViewViewport3D());
            ShortcutIntegration.instance.contextManager.RegisterToolContext(new SceneViewViewportLockedPanTool());
            ShortcutIntegration.instance.contextManager.RegisterToolContext(s_CameraFlyModeContext);
        };

        internal abstract class SceneViewContext : IShortcutToolContext
        {
            public SceneView window => EditorWindow.focusedWindow as SceneView;
            public virtual bool active => ViewHasFocus;
            public static bool ViewHasFocus => (EditorWindow.focusedWindow is SceneView view) && view.sceneViewMotion != null;
        }

        [ReserveModifiers(ShortcutModifiers.Shift)]
        internal class SceneViewViewport : SceneViewContext
        {
        }

        [ReserveModifiers(ShortcutModifiers.Shift)]
        class SceneViewViewport2D : SceneViewContext
        {
            public override bool active => ViewHasFocus && (window.in2DMode || window.isRotationLocked);
        }

        [ReserveModifiers(ShortcutModifiers.Shift)]
        class SceneViewViewport3D : SceneViewContext
        {
            public override bool active => ViewHasFocus && !window.in2DMode && !window.isRotationLocked;
        }

        [ReserveModifiers(ShortcutModifiers.Shift)]
        class SceneViewViewportLockedPanTool : SceneViewContext
        {
            public override bool active => ViewHasFocus && Tools.current == Tool.View;
        }

        [Shortcut(k_PanFocusTool, typeof(SceneViewViewport), KeyCode.Mouse2)]
        [Shortcut(k_LockedPanFocusTool, typeof(SceneViewViewportLockedPanTool), KeyCode.Mouse0)]
        static void PanFocus(ShortcutArguments args)
        {
            // Delaying the picking to a command event is necessary because some HandleUtility methods
            // need to be called in an OnGUI.
            if (args.context is SceneViewContext ctx && ctx.window != null)
                ctx.window.SendEvent(EditorGUIUtility.CommandEvent(k_PanFocusEventCommandName));
        }

        void PanFocus(Vector2 mousePos, SceneView currentSceneView, Event evt)
        {
            // Move pivot to clicked point.
            RaycastHit hit;
            if (RaycastWorld(mousePos, out hit))
            {
                Vector3 currentPosition = currentSceneView.pivot - currentSceneView.rotation * Vector3.forward * currentSceneView.cameraDistance;
                float targetSize = currentSceneView.size;

                if (!currentSceneView.orthographic)
                    targetSize = currentSceneView.size * Vector3.Dot(hit.point - currentPosition, currentSceneView.rotation * Vector3.forward) / currentSceneView.cameraDistance;

                currentSceneView.LookAt(hit.point, currentSceneView.rotation, targetSize);
            }

            evt.Use();
        }

        [ClutchShortcut(k_TemporaryPanTool2D1, typeof(SceneViewViewport2D), KeyCode.Mouse1)]
        [ClutchShortcut(k_TemporaryPanTool2D2, typeof(SceneViewViewport2D), KeyCode.Mouse0, ShortcutModifiers.Alt)]
        [ClutchShortcut(k_TemporaryPanTool1, typeof(SceneViewViewport), KeyCode.Mouse2)]
        [ClutchShortcut(k_TemporaryPanTool2, typeof(SceneViewViewport), KeyCode.Mouse2, ShortcutModifiers.Alt)]
        [ClutchShortcut(k_TemporaryPanTool3, typeof(SceneViewViewport), KeyCode.Mouse0, ShortcutModifiers.Action | ShortcutModifiers.Alt)]
        [ClutchShortcut(k_TemporaryPanTool4, typeof(SceneViewViewport), KeyCode.Mouse2, ShortcutModifiers.Action | ShortcutModifiers.Alt)]
        [ClutchShortcut(k_LockedPanTool, typeof(SceneViewViewportLockedPanTool), KeyCode.Mouse0)]
        static void TemporaryPan(ShortcutArguments args)
        {
            if (args.context is SceneViewContext ctx && ctx.window != null && ctx.window.sceneViewMotion != null)
                ctx.window.sceneViewMotion.HandleSceneViewMotionTool(args, ViewTool.Pan, ctx.window);
        }

        [ClutchShortcut(k_TemporaryZoomTool1, typeof(SceneViewViewport), KeyCode.Mouse1, ShortcutModifiers.Alt)]
        [ClutchShortcut(k_TemporaryZoomTool2, typeof(SceneViewViewport), KeyCode.Mouse1, ShortcutModifiers.Action | ShortcutModifiers.Alt)]
        static void TemporaryZoom(ShortcutArguments args)
        {
            if (args.context is SceneViewContext ctx && ctx.window != null && ctx.window.sceneViewMotion != null)
                ctx.window.sceneViewMotion.HandleSceneViewMotionTool(args, ViewTool.Zoom, ctx.window);
        }

        [ClutchShortcut(k_TemporaryOrbitTool, typeof(SceneViewViewport3D), KeyCode.Mouse0, ShortcutModifiers.Alt)]
        static void TemporaryOrbit(ShortcutArguments args)
        {
            if (args.context is SceneViewContext ctx && ctx.window != null && ctx.window.sceneViewMotion != null)
                ctx.window.sceneViewMotion.HandleSceneViewMotionTool(args, ViewTool.Orbit, ctx.window);
        }

        void HandleSceneViewMotionTool(ShortcutArguments args, ViewTool viewTool, SceneView view)
        {
            if (args.stage == ShortcutStage.Begin && GUIUtility.hotControl == 0)
                StartSceneViewMotionTool(viewTool, view);
            else if (args.stage == ShortcutStage.End && Tools.s_LockedViewTool == viewTool)
                CompleteSceneViewMotionTool();
        }

        [ClutchShortcut(k_TemporaryFpsTool, typeof(SceneViewViewport3D), typeof(CameraFlyModeContext), KeyCode.Mouse1)]
        static void TemporaryFPS(ShortcutArguments args)
        {
            if (!(args.context is SceneViewViewport3D context)
                || context.window == null
                || context.window.sceneViewMotion == null)
                return;

            if (args.stage == ShortcutStage.Begin && GUIUtility.hotControl == 0)
            {
                s_CameraFlyModeContext.window = context.window;
                context.window.sceneViewMotion.StartSceneViewMotionTool(ViewTool.FPS, context.window);
            }
            else if (args.stage == ShortcutStage.End && Tools.s_LockedViewTool == ViewTool.FPS)
            {
                s_CameraFlyModeContext.window = null;
                context.window.sceneViewMotion.CompleteSceneViewMotionTool();
            }
        }

        // slightly different logic for arrow keys - forward and backward axes are swapped for up and down when in
        // orthographic or 2D mode
        static void SetCameraMoveDirectionArrow(ShortcutArguments args, SceneInputAxis axis)
        {
            if (!(args.context is SceneViewContext ctx) || !(ctx.window is SceneView view))
                return;

            if (args.stage == ShortcutStage.Begin)
                view.sceneViewMotion.StartSceneViewMotionTool(ViewTool.FPS, view);
            else if (args.stage == ShortcutStage.End)
                view.sceneViewMotion.CompleteSceneViewMotionTool();

            if (view.in2DMode || view.orthographic)
            {
                axis = axis switch
                {
                    SceneInputAxis.Forward => SceneInputAxis.Up,
                    SceneInputAxis.Backward => SceneInputAxis.Down,
                    _ => axis
                };
            }

            SceneNavigationInput.input[axis] = args.stage == ShortcutStage.Begin;
            ctx.window.Repaint();
        }

        [ClutchShortcut("3D Viewport/Fly Mode Forward (Alt)", typeof(SceneViewViewport), KeyCode.UpArrow)]
        static void WalkForwardArrow(ShortcutArguments args) => SetCameraMoveDirectionArrow(args, SceneInputAxis.Forward);

        [ClutchShortcut("3D Viewport/Fly Mode Backward (Alt)", typeof(SceneViewViewport), KeyCode.DownArrow)]
        static void WalkBackwardArrow(ShortcutArguments args) => SetCameraMoveDirectionArrow(args, SceneInputAxis.Backward);

        [ClutchShortcut("3D Viewport/Fly Mode Left (Alt)", typeof(SceneViewViewport), KeyCode.LeftArrow)]
        static void WalkLeftArrow(ShortcutArguments args) => SetCameraMoveDirectionArrow(args, SceneInputAxis.Left);

        [ClutchShortcut("3D Viewport/Fly Mode Right (Alt)", typeof(SceneViewViewport), KeyCode.RightArrow)]
        static void WalkRightArrow(ShortcutArguments args) => SetCameraMoveDirectionArrow(args, SceneInputAxis.Right);

        static void SetInputVector(ShortcutArguments args, SceneInputAxis axis)
        {
            SceneNavigationInput.input[axis] = args.stage == ShortcutStage.Begin;
            var context = (CameraFlyModeContext) args.context;
            context.window.Repaint();
        }

        [ClutchShortcut("3D Viewport/Fly Mode Forward", typeof(CameraFlyModeContext), KeyCode.W)]
        [FormerlyPrefKeyAs("View/FPS Forward", "w")]
        static void WalkForward(ShortcutArguments args) => SetInputVector(args, SceneInputAxis.Forward);

        [ClutchShortcut("3D Viewport/Fly Mode Backward", typeof(CameraFlyModeContext), KeyCode.S)]
        [FormerlyPrefKeyAs("View/FPS Back", "s")]
        static void WalkBackward(ShortcutArguments args) => SetInputVector(args, SceneInputAxis.Backward);

        [ClutchShortcut("3D Viewport/Fly Mode Left", typeof(CameraFlyModeContext), KeyCode.A)]
        [FormerlyPrefKeyAs("View/FPS Strafe Left", "a")]
        static void WalkLeft(ShortcutArguments args) => SetInputVector(args, SceneInputAxis.Left);

        [ClutchShortcut("3D Viewport/Fly Mode Right", typeof(CameraFlyModeContext), KeyCode.D)]
        [FormerlyPrefKeyAs("View/FPS Strafe Right", "d")]
        static void WalkRight(ShortcutArguments args) => SetInputVector(args, SceneInputAxis.Right);

        [ClutchShortcut("3D Viewport/Fly Mode Up", typeof(CameraFlyModeContext), KeyCode.E)]
        [FormerlyPrefKeyAs("View/FPS Strafe Up", "e")]
        static void WalkUp(ShortcutArguments args) => SetInputVector(args, SceneInputAxis.Up);

        [ClutchShortcut("3D Viewport/Fly Mode Down", typeof(CameraFlyModeContext), KeyCode.Q)]
        [FormerlyPrefKeyAs("View/FPS Strafe Down", "q")]
        static void WalkDown(ShortcutArguments args) => SetInputVector(args, SceneInputAxis.Down);

        void StartSceneViewMotionTool(ViewTool viewTool, SceneView view)
        {
            Tools.s_LockedViewTool = Tools.viewTool = viewTool;

            // Set up zoom parameters
            m_StartZoom = view.size;
            m_ZoomSpeed = Mathf.Max(Mathf.Abs(m_StartZoom), .3f);
            m_TotalMotion = 0;

            if (Toolbar.get)
                Toolbar.get.Repaint();

            EditorGUIUtility.SetWantsMouseJumping(1);

            UpdateViewToolState();

            // The hot control needs to be set in an OnGUI call.
            view.SendEvent(EditorGUIUtility.CommandEvent(k_SetSceneViewMotionHotControlEventCommandName));
        }

        public void CompleteSceneViewMotionTool()
        {
            Tools.viewTool = ViewTool.Pan;
            Tools.s_LockedViewTool = ViewTool.None;
            GUIUtility.hotControl = 0;


            if (viewToolActiveChanged != null)
                viewToolActiveChanged.Invoke();

            Tools.s_ButtonDown = -1;

            if (Toolbar.get)
                Toolbar.get.Repaint();

            EditorGUIUtility.SetWantsMouseJumping(0);

            m_Drag = false;
        }

        public void DoViewTool()
        {
            var view = SceneView.lastActiveSceneView;
            if (view == null)
                return;

            // In FPS mode we update the pivot for Orbit mode (see below and inside HandleMouseDrag)
            if (Tools.s_LockedViewTool == ViewTool.FPS)
                view.FixNegativeSize();

            SceneNavigationInput.Update();
            if (SceneNavigationInput.moving)
                view.viewIsLockedToObject = false;
            m_Motion = SceneNavigationInput.currentInputVector;

            // If a different mouse button is clicked while the current mouse button is held down,
            // reset the hot control to the correct id.
            if (GUIUtility.hotControl == 0 && Tools.s_LockedViewTool != ViewTool.None)
                GUIUtility.hotControl = k_ViewToolID;

            var evt = Event.current;
            switch (evt.GetTypeForControl(k_ViewToolID))
            {
                case EventType.ScrollWheel:
                    // Default to zooming to mouse position in 2D mode without alt.
                    HandleScrollWheel(view, view.in2DMode == evt.alt);
                    break;
                case EventType.MouseDown:
                    if (GUIUtility.hotControl == 0)
                        m_StartMousePosition = evt.mousePosition;
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == k_ViewToolID)
                        GUIUtility.hotControl = 0;
                    break;
                case EventType.KeyDown: // Escape
                    HandleKeyDown();
                    break;
                case EventType.MouseDrag:
                    HandleMouseDrag(view);
                    break;
                case EventType.Layout:
                    if (GUIUtility.hotControl == k_ViewToolID || m_FlySpeed.isAnimating || m_Moving)
                    {
                        view.pivot = view.pivot + view.rotation * GetMovementDirection(view);
                        view.Repaint();
                    }
                    break;
                case EventType.ExecuteCommand:
                    if (evt.commandName == k_PanFocusEventCommandName)
                    {
                        PanFocus(m_StartMousePosition, view, evt);
                    }
                    else if (evt.commandName == k_SetSceneViewMotionHotControlEventCommandName)
                    {
                        GUIUtility.hotControl = k_ViewToolID;
                        evt.Use();
                    }
                    break;
            }
        }

        static bool UpdateViewToolState()
        {
            bool shouldBeActive = Tools.s_LockedViewTool != ViewTool.None || Tools.current == Tool.View;
            if (shouldBeActive != s_ViewToolIsActive)
            {
                s_ViewToolIsActive = shouldBeActive;

                if (viewToolActiveChanged != null)
                    viewToolActiveChanged.Invoke();
            }

            return s_ViewToolIsActive;
        }

        Vector3 GetMovementDirection(SceneView view)
        {
            m_Moving = m_Motion.sqrMagnitude > 0f;
            var deltaTime = SceneNavigationInput.deltaTime;
            var speedModifier = view.cameraSettings.speed;

            if (Event.current.shift)
                speedModifier *= 5f;

            if (m_Moving)
            {
                if (view.cameraSettings.accelerationEnabled)
                    m_FlySpeedTarget = m_FlySpeedTarget < Mathf.Epsilon ? k_FlySpeed : m_FlySpeedTarget * Mathf.Pow(k_FlySpeedAcceleration, deltaTime);
                else
                    m_FlySpeedTarget = k_FlySpeed;
            }
            else
            {
                m_FlySpeedTarget = 0f;
            }

            if (view.cameraSettings.easingEnabled)
            {
                m_FlySpeed.speed = 1f / view.cameraSettings.easingDuration;
                m_FlySpeed.target = m_Motion.normalized * m_FlySpeedTarget * speedModifier;
            }
            else
            {
                m_FlySpeed.value = m_Motion.normalized * m_FlySpeedTarget * speedModifier;
            }

            return m_FlySpeed.value * deltaTime;
        }

        public void ResetMotion()
        {
            m_Motion = Vector3.zero;
            m_FlySpeed.value = Vector3.zero;
            m_Moving = false;
        }

        bool RaycastWorld(Vector2 position, out RaycastHit hit)
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

        private void OrbitCameraBehavior(SceneView view)
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

        private void HandleMouseDrag(SceneView view)
        {
            if (!Event.current.isMouse || GUIUtility.hotControl != k_ViewToolID)
                return;

            m_Drag = true;
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
                            m_TotalMotion += zoomDelta;

                            if (m_TotalMotion < 0)
                                view.size = m_StartZoom * (1 + m_TotalMotion * .001f);
                            else
                                view.size = view.size + zoomDelta * m_ZoomSpeed * .003f;
                        }
                    }
                    break;
            }

            evt.Use();
        }

        public Vector3 ScreenToWorldDistance(SceneView view, Vector2 delta)
        {
            // Ensures that the camera matrix doesn't end up with gigantic or minuscule values in the clip to world matrix
            const float k_MaxCameraSizeForWorldToScreen = 2.5E+7f;

            // store original camera and view values
            var camera = view.camera;
            var near = camera.nearClipPlane;
            var far = camera.farClipPlane;
            var pos = camera.transform.position;
            var rotation = camera.transform.rotation;
            var size = view.size;

            // set camera transform and clip values to safe values
            view.size = Mathf.Min(view.size, k_MaxCameraSizeForWorldToScreen);
            // account for the distance clamping
            var scale = size / view.size;
            var clip = view.GetDynamicClipPlanes();
            view.camera.nearClipPlane = clip.x;
            view.camera.farClipPlane = clip.y;
            view.camera.transform.position = Vector3.zero;
            view.camera.transform.rotation = Quaternion.identity;

            // do the distance calculation
            Vector3 pivotWorld = camera.transform.rotation * new Vector3(0f, 0f, view.cameraDistance);
            Vector3 pivotScreen = camera.WorldToScreenPoint(pivotWorld);
            pivotScreen += new Vector3(delta.x, delta.y, 0);

            Vector3 worldDelta = camera.ScreenToWorldPoint(pivotScreen) - pivotWorld;
            // We're clearing z here as ScreenToWorldPoint(WorldToScreenPoint(worldPoint)) does not always result in the exact same worldPoint that was inputed (for example, when camera is ortho).
            // https://jira.unity3d.com/browse/UUM-56425
            worldDelta.z = 0f;
            worldDelta = rotation * worldDelta;
            worldDelta *= EditorGUIUtility.pixelsPerPoint * scale;

            // restore original cam and scene values
            view.size = size;
            view.camera.nearClipPlane = near;
            view.camera.farClipPlane = far;
            view.camera.transform.position = pos;
            view.camera.transform.rotation = rotation;

            return worldDelta;
        }

        // This can't be modified into a shortcut because Escape is an invalid key code binding.
        private void HandleKeyDown()
        {
            if (Event.current.keyCode == KeyCode.Escape && GUIUtility.hotControl == k_ViewToolID)
                CompleteSceneViewMotionTool();
        }

        void HandleScrollWheel(SceneView view, bool zoomTowardsCenter)
        {
            var evt = Event.current;
            float scrollDelta = evt.delta.magnitude * Mathf.Sign(Mathf.Min(evt.delta.x,  evt.delta.y));

            if (Tools.s_LockedViewTool == ViewTool.FPS)
            {
                // On some OSs, macOS for example, holding Shift while scrolling is interpreted as horizontal scroll at the system level
                // and that would cause the scroll delta to be set on the x coord instead of y. Therefore here we're taking the magnitude instead of specific component's value.
                float scrollWheelDelta = scrollDelta * m_FPSScrollWheelMultiplier;
                view.cameraSettings.speedNormalized -= scrollWheelDelta;
                float cameraSettingsSpeed = view.cameraSettings.speed;
                string cameraSpeedDisplayValue = cameraSettingsSpeed.ToString(
                    cameraSettingsSpeed < 0.0001f ? "F6" :
                    cameraSettingsSpeed < 0.001f ? "F5" :
                    cameraSettingsSpeed < 0.01f ? "F4" :
                    cameraSettingsSpeed < 0.1f ? "F3" :
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
                // When in camera view mode, change the FoV of the selected camera instead.
                if (view.viewpoint.hasActiveViewpoint)
                {
                    view.viewpoint.HandleScrollWheel(view);
                    return;
                }

                float targetSize;

                if (!view.orthographic)
                {
                    float relativeDelta = Mathf.Abs(view.size) * scrollDelta * .015f;
                    const float k_MinZoomDelta = .0001f;
                    if (relativeDelta > 0 && relativeDelta < k_MinZoomDelta)
                        relativeDelta = k_MinZoomDelta;
                    else if (relativeDelta < 0 && relativeDelta > -k_MinZoomDelta)
                        relativeDelta = -k_MinZoomDelta;

                    targetSize = view.size + relativeDelta;
                }
                else
                {
                    targetSize = Mathf.Abs(view.size) * (scrollDelta * .015f + 1.0f);
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
    }
} //namespace
