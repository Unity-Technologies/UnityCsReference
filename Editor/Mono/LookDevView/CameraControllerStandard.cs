// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.ShortcutManagement;
using UnityEditorInternal;

namespace UnityEditor
{
    internal class CameraControllerStandard : CameraController
    {
        private ViewTool    m_CurrentViewTool = ViewTool.None;
        private float       m_StartZoom = 0.0f;
        private float       m_ZoomSpeed = 0.0f;
        private float       m_TotalMotion = 0.0f;
        private Vector3     m_Motion = new Vector3();
        private float       m_FlySpeed = 0;
        const float         kFlyAcceleration = 1.1f;
        private readonly CameraFlyModeContext m_CameraFlyModeContext = new CameraFlyModeContext();

        public ViewTool currentViewTool
        {
            get { return m_CurrentViewTool; }
        }

        private void ResetCameraControl()
        {
            m_CurrentViewTool = ViewTool.None;
            m_Motion = Vector3.zero;
        }

        private void HandleCameraScrollWheel(CameraState cameraState)
        {
            float zoomDelta = Event.current.delta.y;

            float relativeDelta = Mathf.Abs(cameraState.viewSize.value) * zoomDelta * .015f;
            const float deltaCutoff = .3f;
            if (relativeDelta > 0 && relativeDelta < deltaCutoff)
                relativeDelta = deltaCutoff;
            else if (relativeDelta < 0 && relativeDelta > -deltaCutoff)
                relativeDelta = -deltaCutoff;

            cameraState.viewSize.value += relativeDelta;
            Event.current.Use();
        }

        private void OrbitCameraBehavior(CameraState cameraState, Camera cam)
        {
            Event evt = Event.current;

            cameraState.FixNegativeSize();
            Quaternion rotation = cameraState.rotation.target;
            rotation = Quaternion.AngleAxis(evt.delta.y * .003f * Mathf.Rad2Deg, rotation * Vector3.right) * rotation;
            rotation = Quaternion.AngleAxis(evt.delta.x * .003f * Mathf.Rad2Deg, Vector3.up) * rotation;
            if (cameraState.viewSize.value < 0)
            {
                cameraState.pivot.value = cam.transform.position;
                cameraState.viewSize.value = 0;
            }
            cameraState.rotation.value = rotation;
        }

        private void HandleCameraMouseDrag(CameraState cameraState, Camera cam)
        {
            Event evt = Event.current;
            switch (m_CurrentViewTool)
            {
                case ViewTool.Orbit:
                {
                    OrbitCameraBehavior(cameraState, cam);
                }
                break;
                case ViewTool.FPS:
                {
                    Vector3 camPos = cameraState.pivot.value - cameraState.rotation.value * Vector3.forward * cameraState.GetCameraDistance();

                    // Normal FPS camera behavior
                    Quaternion rotation = cameraState.rotation.value;
                    rotation = Quaternion.AngleAxis(evt.delta.y * .003f * Mathf.Rad2Deg, rotation * Vector3.right) * rotation;
                    rotation = Quaternion.AngleAxis(evt.delta.x * .003f * Mathf.Rad2Deg, Vector3.up) * rotation;
                    cameraState.rotation.value = rotation;
                    cameraState.pivot.value = camPos + rotation * Vector3.forward * cameraState.GetCameraDistance();
                }
                break;
                case ViewTool.Pan:
                {
                    cameraState.FixNegativeSize();
                    Vector3 screenPos = cam.WorldToScreenPoint(cameraState.pivot.value);
                    screenPos += new Vector3(-Event.current.delta.x, Event.current.delta.y, 0);
                    Vector3 worldDelta = cam.ScreenToWorldPoint(screenPos) - cameraState.pivot.value;
                    if (evt.shift)
                        worldDelta *= 4;
                    cameraState.pivot.value += worldDelta;
                }
                break;
                case ViewTool.Zoom:
                {
                    float zoomDelta = HandleUtility.niceMouseDeltaZoom * (evt.shift ? 9 : 3);
                    m_TotalMotion += zoomDelta;
                    if (m_TotalMotion < 0)
                        cameraState.viewSize.value = m_StartZoom * (1 + m_TotalMotion * .001f);
                    else
                        cameraState.viewSize.value = cameraState.viewSize.value + zoomDelta * m_ZoomSpeed * .003f;
                }
                break;

                default:
                    break;
            }
            evt.Use();
        }

        private void HandleCameraKeyDown()
        {
            if (Event.current.keyCode == KeyCode.Escape)
            {
                ResetCameraControl();
            }
        }

        private void HandleCameraMouseUp()
        {
            ResetCameraControl();
            Event.current.Use();
        }

        private Vector3 GetMovementDirection()
        {
            var deltaTime = CameraFlyModeContext.deltaTime;
            if (m_Motion.sqrMagnitude == 0)
            {
                m_FlySpeed = 0;
                return Vector3.zero;
            }
            else
            {
                float speed = Event.current.shift ? 5 : 1;
                if (m_FlySpeed == 0)
                    m_FlySpeed = 9;
                else
                    m_FlySpeed = m_FlySpeed * Mathf.Pow(kFlyAcceleration, deltaTime);
                return m_Motion.normalized * m_FlySpeed * speed * deltaTime;
            }
        }

        public override void Update(CameraState cameraState, Camera cam, EditorWindow window)
        {
            Event evt = Event.current;

            if (evt.type == EventType.MouseUp)
            {
                m_CurrentViewTool = ViewTool.None;
            }

            if (evt.type == EventType.MouseDown)
            {
                int button = evt.button;

                bool controlKeyOnMac = (evt.control && Application.platform == RuntimePlatform.OSXEditor);

                var rightMouseButton = false;
                if (button == 2)
                {
                    m_CurrentViewTool = ViewTool.Pan;
                }
                else if ((button <= 0 && controlKeyOnMac)
                         || (button == 1 && evt.alt))
                {
                    m_CurrentViewTool = ViewTool.Zoom;

                    m_StartZoom = cameraState.viewSize.value;
                    m_ZoomSpeed = Mathf.Max(Mathf.Abs(m_StartZoom), .3f);
                    m_TotalMotion = 0;
                    rightMouseButton = button == 1;
                }
                else if (button <= 0)
                {
                    m_CurrentViewTool = ViewTool.Orbit;
                }
                else if (button == 1 && !evt.alt)
                {
                    m_CurrentViewTool = ViewTool.FPS;
                    rightMouseButton = true;
                }

                // see also SceneView.HandleClickAndDragToFocus()
                if (rightMouseButton && Application.platform == RuntimePlatform.OSXEditor)
                    window.Focus();
            }

            var id = GUIUtility.GetControlID(FocusType.Passive);
            using (var inputSamplingScope = new CameraFlyModeContext.InputSamplingScope(m_CameraFlyModeContext, m_CurrentViewTool, id, window))
            {
                if (inputSamplingScope.inputVectorChanged)
                    m_FlySpeed = 0;
                m_Motion = inputSamplingScope.currentInputVector;
            }

            switch (evt.type)
            {
                case EventType.ScrollWheel: HandleCameraScrollWheel(cameraState); break;
                case EventType.MouseUp:     HandleCameraMouseUp(); break;
                case EventType.MouseDrag:   HandleCameraMouseDrag(cameraState, cam); break;
                case EventType.KeyDown:     HandleCameraKeyDown(); break;
                case EventType.Layout:
                {
                    Vector3 motion = GetMovementDirection();
                    if (motion.sqrMagnitude != 0)
                    {
                        cameraState.pivot.value = cameraState.pivot.value + cameraState.rotation.value * motion;
                    }
                }
                break;
            }
        }

        public void DeactivateFlyModeContext()
        {
            ShortcutIntegration.instance.contextManager.DeregisterToolContext(m_CameraFlyModeContext);
        }
    }
}
