// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Reflection;
using UnityEngine.Scripting;

namespace UnityEngine
{
    internal class SendMouseEvents
    {
        struct HitInfo
        {
            public GameObject target;
            public Camera camera;

            public void SendMessage(string name)
            {
                target.SendMessage(name, null, SendMessageOptions.DontRequireReceiver);
            }

            public static implicit operator bool(HitInfo exists)
            {
                return exists.target != null && exists.camera != null;
            }

            public static bool Compare(HitInfo lhs, HitInfo rhs)
            {
                return lhs.target == rhs.target && lhs.camera == rhs.camera;
            }
        }

        private const int m_HitIndexGUI = 0;
        private const int m_HitIndexPhysics3D = 1;
        private const int m_HitIndexPhysics2D = 2;
        private static bool s_MouseUsed = false;

        static readonly HitInfo[] m_LastHit = { new HitInfo(), new HitInfo(), new HitInfo() };
        static readonly HitInfo[] m_MouseDownHit = { new HitInfo(), new HitInfo(), new HitInfo() };
        static readonly HitInfo[] m_CurrentHit = { new HitInfo(), new HitInfo(), new HitInfo() };
        static Camera[] m_Cameras;

        [RequiredByNativeCode]
        static void SetMouseMoved()
        {
            s_MouseUsed = true;
        }

#pragma warning disable 0618
        private static void HitTestLegacyGUI(Camera camera, Vector3 mousePosition, ref HitInfo hitInfo)
        {
            // Did we hit any gui elements?
            var layer = camera.GetComponent<GUILayer>();
            if (layer)
            {
                var element = layer.HitTest(mousePosition);
                if (element)
                {
                    hitInfo.target = element.gameObject;
                    hitInfo.camera = camera;
                }
                else
                {
                    // We did not hit any object, so clear current hit
                    hitInfo.target = null;
                    hitInfo.camera = null;
                }
            }
        }

#pragma warning restore 0618

        [RequiredByNativeCode]
        static void DoSendMouseEvents(int skipRTCameras)
        {
            var mousePosition = Input.mousePosition;

            int camerasCount = Camera.allCamerasCount;
            if (m_Cameras == null || m_Cameras.Length != camerasCount)
                m_Cameras = new Camera[camerasCount];

            // Fetch all cameras.
            Camera.GetAllCameras(m_Cameras);

            // Clear the HitInfos from last time
            for (var hitIndex = 0; hitIndex < m_CurrentHit.Length; ++hitIndex)
                m_CurrentHit[hitIndex] = new HitInfo();

            // If UnityGUI has the mouse over, we simply don't do any mouse hit detection.
            // That way, it will appear as if the mouse has missed everything.
            if (!s_MouseUsed)
            {
                foreach (var camera in m_Cameras)
                {
                    // we do not want to check cameras that are rendering to textures, starting with 4.0
                    if (camera == null || skipRTCameras != 0 && camera.targetTexture != null)
                        continue;

                    int displayIndex = camera.targetDisplay;

                    var eventPosition = Display.RelativeMouseAt(mousePosition);
                    if (eventPosition != Vector3.zero)
                    {
                        // We support multiple display and display identification based on event position.
                        int eventDisplayIndex = (int)eventPosition.z;

                        // Discard events that are not part of this display so the user does not interact with multiple displays at once.
                        if (eventDisplayIndex != displayIndex)
                            continue;

                        // Multiple display support only when not the main display. For display 0 the reported
                        // resolution is always the desktop resolution since it's part of the display API,
                        // so we use the standard non multiple display method.
                        float w = Screen.width;
                        float h = Screen.height;
                        if (displayIndex > 0 && displayIndex < Display.displays.Length)
                        {
                            w = Display.displays[displayIndex].systemWidth;
                            h = Display.displays[displayIndex].systemHeight;
                        }

                        Vector2 pos = new Vector2(eventPosition.x / w, eventPosition.y / h);

                        // If the mouse is outside the display bounds, do nothing
                        if (pos.x < 0f || pos.x > 1f || pos.y < 0f || pos.y > 1f)
                            continue;
                    }
                    else
                    {
                        // The multiple display system is not supported on all platforms, when it is not supported the returned position
                        // will be all zeros so when the returned index is 0 we will default to the mouse position to be safe.
                        eventPosition = mousePosition;
                    }

                    // Is the mouse inside the cameras viewport?
                    var rect = camera.pixelRect;
                    if (!rect.Contains(eventPosition))
                        continue;

                    HitTestLegacyGUI(camera, eventPosition, ref m_CurrentHit[m_HitIndexGUI]);

                    // There is no need to continue if the camera shouldn't be sending out events
                    if (camera.eventMask == 0)
                        continue;

                    // Calculate common physics projection and distance.
                    var screenProjectionRay = camera.ScreenPointToRay(eventPosition);
                    var projectionDirection = screenProjectionRay.direction.z;
                    var distanceToClipPlane = Mathf.Approximately(0.0f, projectionDirection) ? Mathf.Infinity : Mathf.Abs((camera.farClipPlane - camera.nearClipPlane) / projectionDirection);

                    // Did we hit any 3D colliders?
                    var hit3D = camera.RaycastTry(screenProjectionRay, distanceToClipPlane, camera.cullingMask & camera.eventMask);
                    if (hit3D != null)
                    {
                        m_CurrentHit[m_HitIndexPhysics3D].target = hit3D;
                        m_CurrentHit[m_HitIndexPhysics3D].camera = camera;
                    }
                    // We did not hit anything with a raycast from this camera. But our camera
                    // clears the screen and renders on top of whatever was below, thus making things
                    // rendered before invisible. So clear any previous hit we have found.
                    else if (camera.clearFlags == CameraClearFlags.Skybox || camera.clearFlags == CameraClearFlags.SolidColor)
                    {
                        m_CurrentHit[m_HitIndexPhysics3D].target = null;
                        m_CurrentHit[m_HitIndexPhysics3D].camera = null;
                    }

                    // Did we hit any 2D colliders?
                    var hit2D = camera.RaycastTry2D(screenProjectionRay, distanceToClipPlane, camera.cullingMask & camera.eventMask);
                    if (hit2D != null)
                    {
                        m_CurrentHit[m_HitIndexPhysics2D].target = hit2D;
                        m_CurrentHit[m_HitIndexPhysics2D].camera = camera;
                    }
                    // We did not hit anything with a raycast from this camera. But our camera
                    // clears the screen and renders on top of whatever was below, thus making things
                    // rendered before invisible. So clear any previous hit we have found.
                    else if (camera.clearFlags == CameraClearFlags.Skybox || camera.clearFlags == CameraClearFlags.SolidColor)
                    {
                        m_CurrentHit[m_HitIndexPhysics2D].target = null;
                        m_CurrentHit[m_HitIndexPhysics2D].camera = null;
                    }
                }
            }

            // Send hit events.
            for (var hitIndex = 0; hitIndex < m_CurrentHit.Length; ++hitIndex)
                SendEvents(hitIndex, m_CurrentHit[hitIndex]);

            s_MouseUsed = false;
        }

        /// <summary>
        /// Old-style mouse events used prior to the new event system of 4.2.
        /// </summary>

        static void SendEvents(int i, HitInfo hit)
        {
            // Handle MouseDown, MouseDrag, MouseUp
            bool mouseDownThisFrame = Input.GetMouseButtonDown(0);
            bool mousePressed = Input.GetMouseButton(0);

            if (mouseDownThisFrame)
            {
                if (hit)
                {
                    m_MouseDownHit[i] = hit;
                    m_MouseDownHit[i].SendMessage("OnMouseDown");
                }
            }
            else if (!mousePressed)
            {
                if (m_MouseDownHit[i])
                {
                    // For button like behavior only fire this event if same as on MouseDown
                    if (HitInfo.Compare(hit, m_MouseDownHit[i]))
                        m_MouseDownHit[i].SendMessage("OnMouseUpAsButton");

                    // For backwards compatibility we keep the event name OnMouseUp
                    m_MouseDownHit[i].SendMessage("OnMouseUp");
                    m_MouseDownHit[i] = new HitInfo();
                }
            }
            else if (m_MouseDownHit[i])
            {
                m_MouseDownHit[i].SendMessage("OnMouseDrag");
            }


            // Handle MouseOver, MouseEnter, MouseExit
            if (HitInfo.Compare(hit, m_LastHit[i]))
            {
                if (hit)
                    hit.SendMessage("OnMouseOver");
            }
            else
            {
                if (m_LastHit[i])
                {
                    m_LastHit[i].SendMessage("OnMouseExit");
                }

                if (hit)
                {
                    hit.SendMessage("OnMouseEnter");
                    hit.SendMessage("OnMouseOver");
                }
            }
            m_LastHit[i] = hit;
        }
    }
}
