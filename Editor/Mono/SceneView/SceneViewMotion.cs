// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Profiling;

namespace UnityEditor
{
    internal class SceneViewMotion
    {
        static Vector3 s_Motion;
        static float s_FlySpeed = 0;
        const float kFlyAcceleration = 1.8f;
        static float s_StartZoom = 0, s_ZoomSpeed = 0f;
        static float s_TotalMotion = 0;

        enum MotionState
        {
            kInactive,
            kActive,
            kDragging
        }
        private static MotionState s_CurrentState;

        static int s_ViewToolID = GUIUtility.GetPermanentControlID();

        static PrefKey kFPSForward = new PrefKey("View/FPS Forward", "w");
        static PrefKey kFPSBack = new PrefKey("View/FPS Back", "s");
        static PrefKey kFPSLeft = new PrefKey("View/FPS Strafe Left", "a");
        static PrefKey kFPSRight = new PrefKey("View/FPS Strafe Right", "d");
        static PrefKey kFPSUp = new PrefKey("View/FPS Strafe Up", "e");
        static PrefKey kFPSDown = new PrefKey("View/FPS Strafe Down", "q");

        static TimeHelper s_FPSTiming = new TimeHelper();

        // CURSOR KEYS
        public static void ArrowKeys(SceneView sv)
        {
            Event evt = Event.current;
            int id = GUIUtility.GetControlID(FocusType.Passive);
            if (GUIUtility.hotControl == 0 || GUIUtility.hotControl == id)
            {
                if (EditorGUI.actionKey)
                    return;
                switch (evt.GetTypeForControl(id))
                {
                    case EventType.KeyDown:
                        switch (evt.keyCode)
                        {
                            case KeyCode.UpArrow:
                                sv.viewIsLockedToObject = false;
                                if (sv.m_Ortho.value)
                                    s_Motion.y = 1;
                                else
                                    s_Motion.z = 1;
                                GUIUtility.hotControl = id;
                                evt.Use();
                                break;
                            case KeyCode.DownArrow:
                                sv.viewIsLockedToObject = false;
                                if (sv.m_Ortho.value)
                                    s_Motion.y = -1;
                                else
                                    s_Motion.z = -1;
                                GUIUtility.hotControl = id;
                                evt.Use();
                                break;
                            case KeyCode.LeftArrow:
                                sv.viewIsLockedToObject = false;
                                s_Motion.x = -1;
                                GUIUtility.hotControl = id;
                                evt.Use();
                                break;
                            case KeyCode.RightArrow:
                                sv.viewIsLockedToObject = false;
                                s_Motion.x = 1;
                                GUIUtility.hotControl = id;
                                evt.Use();
                                break;
                        }
                        break;

                    case EventType.KeyUp:
                        if (GUIUtility.hotControl == id)
                        {
                            switch (evt.keyCode)
                            {
                                case KeyCode.UpArrow:
                                case KeyCode.DownArrow:
                                    s_Motion.z = 0;
                                    s_Motion.y = 0;
                                    evt.Use();
                                    break;
                                case KeyCode.LeftArrow:
                                case KeyCode.RightArrow:
                                    s_Motion.x = 0;
                                    evt.Use();
                                    break;
                            }
                        }

                        break;

                    case EventType.Layout:
                        if (GUIUtility.hotControl == id)
                        {
                            Vector3 fwd;
                            if (!sv.m_Ortho.value)
                            {
                                fwd = Camera.current.transform.forward + Camera.current.transform.up * .3f;
                                fwd.y = 0;
                                fwd.Normalize();
                            }
                            else
                            {
                                fwd = Camera.current.transform.forward;
                            }
                            Vector3 motion = GetMovementDirection();
                            sv.LookAtDirect(sv.pivot + Quaternion.LookRotation(fwd) * motion, sv.rotation);

                            // If we're done, stop animating
                            if (s_Motion.sqrMagnitude == 0)
                            {
                                sv.pivot = sv.pivot;
                                s_FlySpeed = 0;
                                GUIUtility.hotControl = 0;
                            }
                            else
                            {
                                sv.Repaint();
                            }
                        }
                        break;
                }
            }
        }

        // HANDLE THE VIEW TOOL & ALT COMBOS
        public static void DoViewTool(SceneView view)
        {
            Event evt = Event.current;

            // Ensure we always call the GetControlID the same number of times
            int id = s_ViewToolID;

            EventType eventType = evt.GetTypeForControl(id);

            // In FPS mode we update the pivot for Orbit mode (see below and inside HandleMouseDrag)
            if (view && Tools.s_LockedViewTool == ViewTool.FPS)
            {
                view.FixNegativeSize();
            }

            switch (eventType)
            {
                case EventType.ScrollWheel:     HandleScrollWheel(view, view.in2DMode == evt.alt); break; // Default to zooming to mouse position in 2D mode without alt
                case EventType.MouseDown:       HandleMouseDown(view, id, evt.button); break;
                case EventType.MouseUp:         HandleMouseUp(view, id, evt.button, evt.clickCount); break;
                case EventType.MouseDrag:       HandleMouseDrag(view, id); break;
                case EventType.KeyDown:         HandleKeyDown(view); break;
                case EventType.KeyUp:           HandleKeyUp(); break;
                case EventType.Layout:
                {
                    Vector3 motion = GetMovementDirection();
                    // This seems to be the best way to have a continuously repeating event
                    if (GUIUtility.hotControl == id && motion.sqrMagnitude != 0)
                    {
                        view.pivot = view.pivot + view.rotation * motion;
                        view.Repaint();
                    }
                }
                break;
                case EventType.Used:
                    // since FPS tool acts on right click, nothing prevents a regular control
                    // from taking the control ID on left click, so some cleanup is necessary
                    // to not get locked into FPS mode (case 777346)
                    if (GUIUtility.hotControl != id && s_CurrentState != MotionState.kInactive)
                    {
                        ResetDragState();
                    }
                    break;
            }
        }

        static Vector3 GetMovementDirection()
        {
            float deltaTime = s_FPSTiming.Update();
            if (s_Motion.sqrMagnitude == 0)
            {
                s_FlySpeed = 0;
                return Vector3.zero;
            }
            else
            {
                float speed = Event.current.shift ? 5 : 1;
                if (s_FlySpeed == 0)
                    s_FlySpeed = 9;
                else
                    s_FlySpeed = s_FlySpeed * Mathf.Pow(kFlyAcceleration, deltaTime);
                return s_Motion.normalized * s_FlySpeed * speed * deltaTime;
            }
        }

        private static void HandleMouseDown(SceneView view, int id, int button)
        {
            s_CurrentState = MotionState.kInactive;

            if (Tools.viewToolActive)
            {
                ViewTool wantedViewTool = Tools.viewTool;

                // Check if we want to lock a view tool
                if (Tools.s_LockedViewTool != wantedViewTool)
                {
                    Event evt = Event.current;

                    // Set the hotcontrol and then lock the viewTool (important to set the hotControl AFTER the Tools.viewTool has been evaluated)
                    GUIUtility.hotControl = id;
                    Tools.s_LockedViewTool = wantedViewTool;

                    // Set up zoom parameters
                    s_StartZoom = view.size;
                    s_ZoomSpeed = Mathf.Max(Mathf.Abs(s_StartZoom), .3f);
                    s_TotalMotion = 0;

                    if (view)
                        view.Focus();

                    if (Toolbar.get)
                        Toolbar.get.Repaint();
                    EditorGUIUtility.SetWantsMouseJumping(1);

                    evt.Use();

                    // we're not dragging yet, but enter this state so we can cleanup correctly
                    s_CurrentState = MotionState.kActive;

                    // This prevents camera from moving back on mousedown... find out why... and write a comment
                    GUIUtility.ExitGUI();
                }
            }
        }

        private static void ResetDragState()
        {
            s_CurrentState = MotionState.kInactive;
            Tools.s_LockedViewTool = ViewTool.None;
            Tools.s_ButtonDown = -1;
            s_Motion = Vector3.zero;
            if (Toolbar.get)
                Toolbar.get.Repaint();
            EditorGUIUtility.SetWantsMouseJumping(0);
        }

        private static void HandleMouseUp(SceneView view, int id, int button, int clickCount)
        {
            if (GUIUtility.hotControl == id)
            {
                GUIUtility.hotControl = 0;

                // Move pivot to clicked point.
                if (button == 2 && s_CurrentState != MotionState.kDragging)
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

                ResetDragState();

                Event.current.Use();
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
                if (!mesh)
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
            // we now are dragging for real
            s_CurrentState = MotionState.kDragging;

            if (GUIUtility.hotControl == id)
            {
                Event evt = Event.current;
                switch (Tools.s_LockedViewTool)
                {
                    case ViewTool.Orbit:
                    {
                        if (!view.in2DMode && !view.isRotationLocked)
                        {
                            OrbitCameraBehavior(view);
                            view.svRot.UpdateGizmoLabel(view, view.rotation * Vector3.forward, view.m_Ortho.target);
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
                                // is the *output* of camera motion calculations. It shouldn't be input and putput at the same time,
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
                            view.svRot.UpdateGizmoLabel(view, view.rotation * Vector3.forward, view.m_Ortho.target);
                        }
                    }
                    break;
                    case ViewTool.Pan:
                    {
                        view.viewIsLockedToObject = false;
                        view.FixNegativeSize();
                        Camera cam = view.camera;
                        Vector3 screenPos = cam.WorldToScreenPoint(view.pivot);
                        screenPos += new Vector3(-Event.current.delta.x, Event.current.delta.y, 0);
                        Vector3 worldDelta = Camera.current.ScreenToWorldPoint(screenPos) - view.pivot;
                        worldDelta *= EditorGUIUtility.pixelsPerPoint;
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

                    default:
                        Debug.Log("Enum value Tools.s_LockViewTool not handled");
                        break;
                }
                evt.Use();
            }
        }

        private static void HandleKeyDown(SceneView sceneView)
        {
            if (Event.current.keyCode == KeyCode.Escape && GUIUtility.hotControl == s_ViewToolID)
            {
                GUIUtility.hotControl = 0;
                ResetDragState();
            }

            if (Tools.s_LockedViewTool == ViewTool.FPS)
            {
                Event evt = Event.current;
                Vector3 lastMotion = s_Motion;
                if (evt.keyCode == ((Event)kFPSForward).keyCode)
                {
                    sceneView.viewIsLockedToObject = false;
                    s_Motion.z = 1;
                    evt.Use();
                }
                else if (evt.keyCode == ((Event)kFPSBack).keyCode)
                {
                    sceneView.viewIsLockedToObject = false;
                    s_Motion.z = -1;
                    evt.Use();
                }
                else if (evt.keyCode == ((Event)kFPSLeft).keyCode)
                {
                    sceneView.viewIsLockedToObject = false;
                    s_Motion.x = -1;
                    evt.Use();
                }
                else if (evt.keyCode == ((Event)kFPSRight).keyCode)
                {
                    sceneView.viewIsLockedToObject = false;
                    s_Motion.x = 1;
                    evt.Use();
                }
                else if (evt.keyCode == ((Event)kFPSUp).keyCode)
                {
                    sceneView.viewIsLockedToObject = false;
                    s_Motion.y = 1;
                    evt.Use();
                }
                else if (evt.keyCode == ((Event)kFPSDown).keyCode)
                {
                    sceneView.viewIsLockedToObject = false;
                    s_Motion.y = -1;
                    evt.Use();
                }

                if (evt.type != EventType.KeyDown && lastMotion.sqrMagnitude == 0)
                    s_FPSTiming.Begin();
            }
        }

        private static void HandleKeyUp()
        {
            if (Tools.s_LockedViewTool == ViewTool.FPS)
            {
                Event evt = Event.current;
                if (evt.keyCode == ((Event)kFPSForward).keyCode || evt.keyCode == ((Event)kFPSBack).keyCode)
                {
                    s_Motion.z = 0;
                    evt.Use();
                }
                else if (evt.keyCode == ((Event)kFPSLeft).keyCode || evt.keyCode == ((Event)kFPSRight).keyCode)
                {
                    s_Motion.x = 0;
                    evt.Use();
                }
                else if (evt.keyCode == ((Event)kFPSUp).keyCode || evt.keyCode == ((Event)kFPSDown).keyCode)
                {
                    s_Motion.y = 0;
                    evt.Use();
                }
            }
        }

        private static void HandleScrollWheel(SceneView view, bool zoomTowardsCenter)
        {
            var initialDistance = view.cameraDistance;
            var mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            var mousePivot = mouseRay.origin + mouseRay.direction * view.cameraDistance;
            var pivotVector = mousePivot - view.pivot;

            float zoomDelta = Event.current.delta.y;
            if (!view.orthographic)
            {
                float relativeDelta = Mathf.Abs(view.size) * zoomDelta * .015f;
                const float deltaCutoff = .3f;
                if (relativeDelta > 0 && relativeDelta < deltaCutoff)
                    relativeDelta = deltaCutoff;
                else if (relativeDelta < 0 && relativeDelta > -deltaCutoff)
                    relativeDelta = -deltaCutoff;

                view.size += relativeDelta;
            }
            else
            {
                view.size = Mathf.Abs(view.size) * (zoomDelta * .015f + 1.0f);
            }

            var percentage = 1f - (view.cameraDistance / initialDistance);
            if (!zoomTowardsCenter)
                view.pivot += pivotVector * percentage;

            Event.current.Use();
        }

        public static void ResetMotion()
        {
            s_Motion = Vector3.zero;
        }
    }

    [System.Serializable]
    internal class SceneViewRotation
    {
        private const int kRotationSize = 100;
        private const int kRotationMenuInset = 22;
        private const float kRotationLockedAlpha = 0.4f;

        static Quaternion[] kDirectionRotations =
        {
            Quaternion.LookRotation(new Vector3(-1, 0, 0)),
            Quaternion.LookRotation(new Vector3(0, -1, 0)),
            Quaternion.LookRotation(new Vector3(0, 0, -1)),
            Quaternion.LookRotation(new Vector3(1, 0, 0)),
            Quaternion.LookRotation(new Vector3(0, 1, 0)),
            Quaternion.LookRotation(new Vector3(0, 0, 1)),
        };

        static string[] kDirNames = { "Right", "Top", "Front", "Left", "Bottom", "Back", "Iso", "Persp", "2D" };
        static string[] kMenuDirNames = { "Free", "Right", "Top", "Front", "Left", "Bottom", "Back", "", "Perspective" };

        private static readonly GUIContent[] s_HandleAxisLabels =
        {
            new GUIContent("x"), new GUIContent("y"), new GUIContent("z")
        };

        private int[] m_ViewDirectionControlIDs;
        private int m_CenterButtonControlID;


        int currentDir = 7;
        AnimBool[] dirVisible = { new AnimBool(true), new AnimBool(true), new AnimBool(true) };
        AnimBool[] dirNameVisible = { new AnimBool(), new AnimBool(), new AnimBool(), new AnimBool(), new AnimBool(), new AnimBool(), new AnimBool(), new AnimBool(), new AnimBool() };
        float faded2Dgray { get { return dirNameVisible[8].faded; } }
        float fadedRotationLock { get { return Mathf.Lerp(kRotationLockedAlpha, 1.0f, m_RotationLocked.faded); } }

        AnimBool m_RotationLocked = new AnimBool();
        AnimBool m_Visible = new AnimBool();
        float fadedVisibility
        {
            get
            {
                return m_Visible.faded * fadedRotationLock;
            }
        }

        private class Styles
        {
            public GUIStyle viewLabelStyleLeftAligned;
            public GUIStyle viewLabelStyleCentered;
            public GUIStyle viewAxisLabelStyle;
            public GUIStyle lockStyle;
            public GUIContent unlockedRotationIcon;
            public GUIContent lockedRotationIcon;

            public Styles()
            {
                viewLabelStyleLeftAligned = new GUIStyle("SC ViewLabel");
                viewLabelStyleCentered = new GUIStyle("SC ViewLabel");
                unlockedRotationIcon = EditorGUIUtility.IconContent("LockIcon", "Lock Rotation|Click to lock the rotation in the current direction.");
                lockedRotationIcon = EditorGUIUtility.IconContent("LockIcon-On", "Lock Rotation|Click to unlock the rotation.");
                lockStyle = new GUIStyle("label");
                lockStyle.alignment = TextAnchor.MiddleCenter;
                viewLabelStyleLeftAligned.alignment = TextAnchor.MiddleLeft;
                viewLabelStyleCentered.alignment = TextAnchor.MiddleCenter;
                viewAxisLabelStyle = "SC ViewAxisLabel";
            }
        }
        private static Styles s_Styles;
        private static Styles styles
        {
            get
            {
                if (s_Styles == null)
                    s_Styles = new Styles();
                return s_Styles;
            }
        }

        public void Register(SceneView view)
        {
            // Register fade animators for cones
            for (int i = 0; i < dirVisible.Length; i++)
                dirVisible[i].valueChanged.AddListener(view.Repaint);
            for (int i = 0; i < dirNameVisible.Length; i++)
                dirNameVisible[i].valueChanged.AddListener(view.Repaint);

            m_RotationLocked.valueChanged.AddListener(view.Repaint);
            m_Visible.valueChanged.AddListener(view.Repaint);

            // Set correct label to be enabled from beginning
            int labelIndex = GetLabelIndexForView(view, view.rotation * Vector3.forward, view.orthographic);
            for (int i = 0; i < dirNameVisible.Length; i++)
                dirNameVisible[i].value = (i == labelIndex);
            m_RotationLocked.value = !view.isRotationLocked;
            m_Visible.value = (labelIndex != 8);

            SwitchDirNameVisible(labelIndex);

            if (m_ViewDirectionControlIDs == null)
            {
                m_ViewDirectionControlIDs = new int[kDirectionRotations.Length];
                for (int i = 0; i < m_ViewDirectionControlIDs.Length; ++i)
                {
                    m_ViewDirectionControlIDs[i] = GUIUtility.GetPermanentControlID();
                }

                m_CenterButtonControlID = GUIUtility.GetPermanentControlID();
            }
        }

        private void AxisSelectors(SceneView view, Camera cam, float size, float sgn, GUIStyle viewAxisLabelStyle)
        {
            for (int h = kDirectionRotations.Length - 1; h >= 0; h--)
            {
                Quaternion q1 = kDirectionRotations[h];
                float a = dirVisible[h % 3].faded;
                Vector3 direction = kDirectionRotations[h] * Vector3.forward;
                float dot = Vector3.Dot(view.camera.transform.forward, direction);

                if (dot <= 0.0 && sgn > 0.0f)
                    continue;

                if (dot > 0.0 && sgn < 0.0f)
                    continue;

                Color c;
                switch (h)
                {
                    case 0: c = Handles.xAxisColor; break;
                    case 1: c = Handles.yAxisColor; break;
                    case 2: c = Handles.zAxisColor; break;
                    default: c = Handles.centerColor; break;
                }

                if (view.in2DMode)
                {
                    c = Color.Lerp(c, Color.gray, faded2Dgray);
                }
                c.a *= a * fadedVisibility;

                Handles.color = c;

                if (c.a <= 0.1f || view.isRotationLocked)
                    GUI.enabled = false;

                // axis widget when drawn behind label
                if (sgn > 0 && Handles.Button(m_ViewDirectionControlIDs[h], q1 * Vector3.forward * size * -1.2f, q1, size, size * 0.7f, Handles.ConeHandleCap))
                {
                    if (!view.in2DMode && !view.isRotationLocked)
                        ViewAxisDirection(view, h);
                }

                // primary axes have text labels
                if (h < 3)
                {
                    GUI.color = new Color(1, 1, 1, dirVisible[h].faded * fadedVisibility);

                    // Label pos is a bit further out than the end of the cone
                    Vector3 pos = direction;
                    // Remove some of the perspective to avoid labels in front
                    // being much further away from the gizmo due to perspective
                    pos += dot * view.camera.transform.forward * -0.5f;
                    // Also remove some of the spacing difference caused by rotation
                    pos = (pos * 0.7f + pos.normalized * 1.5f) * size;
                    Handles.Label(-pos, s_HandleAxisLabels[h], styles.viewAxisLabelStyle);
                }

                // axis widget when drawn in front of label
                if (sgn < 0 && Handles.Button(m_ViewDirectionControlIDs[h], q1 * Vector3.forward * size * -1.2f, q1, size, size * 0.7f, Handles.ConeHandleCap))
                {
                    if (!view.in2DMode && !view.isRotationLocked)
                        ViewAxisDirection(view, h);
                }


                Handles.color = Color.white;
                GUI.color = Color.white;
                GUI.enabled = true;
            }
        }

        internal void HandleContextClick(SceneView view)
        {
            if (!view.in2DMode && !view.isRotationLocked)
            {
                Event evt = Event.current;
                if (evt.type == EventType.MouseDown && evt.button == 1)
                {
                    float screenSize = Mathf.Min(view.position.width, view.position.height);
                    if (screenSize < kRotationSize)
                        return;

                    Rect rotationRect = new Rect(view.position.width - kRotationSize + kRotationMenuInset, kRotationMenuInset, kRotationSize - kRotationMenuInset * 2, kRotationSize - kRotationMenuInset * 2);

                    if (rotationRect.Contains(evt.mousePosition))
                    {
                        DisplayContextMenu(new Rect(evt.mousePosition.x, evt.mousePosition.y, 0, 0), view);
                        evt.Use();
                    }
                }
            }
        }

        private void DisplayContextMenu(Rect buttonOrCursorRect, SceneView view)
        {
            int[] selectedItems = new int[view.orthographic ? 1 : 2];
            selectedItems[0] = currentDir >= 6 ? 0 : currentDir + 1;
            if (!view.orthographic)
                selectedItems[1] = 8;
            EditorUtility.DisplayCustomMenu(buttonOrCursorRect, kMenuDirNames, selectedItems, ContextMenuDelegate, view);
            GUIUtility.ExitGUI();
        }

        private void ContextMenuDelegate(object userData, string[] options, int selected)
        {
            SceneView view = userData as SceneView;
            if (view == null)
                return;

            if (selected == 0)
            {
                // "free" selected
                ViewFromNiceAngle(view, false);
            }
            else if (selected >= 1 && selected <= 6)
            {
                // one of axes was selected
                int axis = selected - 1;
                ViewAxisDirection(view, axis);
            }
            else if (selected == 8)
            {
                // perspective / ortho toggled
                ViewSetOrtho(view, !view.orthographic);
            }
            else if (selected == 10)
            {
                // Unity default point of view
                view.LookAt(view.pivot, Quaternion.LookRotation(new Vector3(-1, -.7f, -1)), view.size, view.orthographic);
            }
            else if (selected == 11)
            {
                // Maya default point of view
                view.LookAt(view.pivot, Quaternion.LookRotation(new Vector3(1, -.7f, -1)), view.size, view.orthographic);
            }
            else if (selected == 12)
            {
                // 3DSMax default point of view
                view.LookAt(view.pivot, Quaternion.LookRotation(new Vector3(1, -.7f, 1)), view.size, view.orthographic);
            }
        }

        private void DrawIsoStatusSymbol(Vector3 center, SceneView view, float alpha)
        {
            float persp = 1 - Mathf.Clamp01(view.m_Ortho.faded * 1.2f - 0.1f);
            Vector3 up = Vector3.up * 3;
            Vector3 right = Vector3.right * 10;
            Vector3 pos = center - right * 0.5f;

            Handles.color = new Color(1, 1, 1, 0.6f * alpha);
            Handles.DrawAAPolyLine(pos + up * (1 - persp), pos + right + up * (1 + persp * 0.5f));
            Handles.DrawAAPolyLine(pos                 , pos + right);
            Handles.DrawAAPolyLine(pos - up * (1 - persp), pos + right - up * (1 + persp * 0.5f));
        }

        void DrawRotationLock(SceneView view)
        {
            const float clickWidth  = 24;
            const float clickHeight = 24;
            float lockCenterX = view.position.width - 16;
            float lockCenterY = 17;
            Rect lockRect = new Rect(lockCenterX - (clickWidth / 2), lockCenterY - (clickHeight / 2), clickWidth, clickHeight);
            Color c = Handles.centerColor;
            c.a *= m_Visible.faded;
            if (c.a > 0.0f)
            {
                var prevColor = GUI.color;
                GUI.color = c;
                var content = (view.isRotationLocked) ? styles.lockedRotationIcon : styles.unlockedRotationIcon;
                if (GUI.Button(lockRect, content, styles.lockStyle) && !view.in2DMode)
                {
                    view.isRotationLocked = !view.isRotationLocked;
                    m_RotationLocked.target = !view.isRotationLocked;
                }
                GUI.color = prevColor;
            }
        }

        void DrawLabels(SceneView view)
        {
            Rect labelRect = new Rect(view.position.width - kRotationSize + 17, kRotationSize - 8, kRotationSize - 17 * 2, 16);

            // Button (overlayed over the labels) to toggle between iso and perspective
            if (!view.in2DMode && !view.isRotationLocked)
            {
                if (GUI.Button(labelRect, string.Empty, styles.viewLabelStyleLeftAligned))
                {
                    if (Event.current.button == 1)
                        DisplayContextMenu(labelRect, view);
                    else
                        ViewSetOrtho(view, !view.orthographic);
                }
            }

            // Labels
            if (Event.current.type == EventType.Repaint)
            {
                int index2D = 8;

                // Calculate the weighted average width of the labels so we can do smart centering of the labels.
                Rect slidingLabelRect = labelRect;
                float width = 0;
                float weightSum = 0;
                for (int i = 0; i < kDirNames.Length; i++)
                {
                    if (i == index2D) // Future proof even if we add more labels after the 2D one
                        continue;
                    weightSum += dirNameVisible[i].faded;
                    if (dirNameVisible[i].faded > 0)
                        width += styles.viewLabelStyleLeftAligned.CalcSize(EditorGUIUtility.TempContent(kDirNames[i])).x * dirNameVisible[i].faded;
                }
                if (weightSum > 0)
                    width /= weightSum;

                // Offset the label rect based on the label width
                slidingLabelRect.x += 37 - width * 0.5f;

                // Round to int AFTER the floating point calculations
                slidingLabelRect.x = Mathf.RoundToInt(slidingLabelRect.x);

                // Currently selected axis label. Since they cross-fade upon selection,
                // more than one might be drawn at the same time.
                // First draw the regular ones - all except the 2D label. They use the slidingLabelRect.
                for (int i = 0; i < dirNameVisible.Length && i < kDirNames.Length; i++)
                {
                    if (i == index2D) // Future proof even if we add more labels after the 2D one
                        continue;
                    Color c = Handles.centerColor;
                    c.a *= dirNameVisible[i].faded * fadedRotationLock;
                    if (c.a > 0.0f)
                    {
                        GUI.color = c;
                        GUI.Label(slidingLabelRect, kDirNames[i], styles.viewLabelStyleLeftAligned);
                    }
                }
                // Then draw just the label for 2D. It uses the original labelRect, and with a style where the text is horizontally centered.
                {
                    Color c = Handles.centerColor;
                    c.a *= faded2Dgray * fadedVisibility;
                    if (c.a > 0.0f)
                    {
                        GUI.color = c;
                        GUI.Label(labelRect, kDirNames[index2D], styles.viewLabelStyleCentered);
                    }
                }

                // Draw the iso status symbol - the passed Vector3 is the center
                if (faded2Dgray < 1)
                {
                    DrawIsoStatusSymbol(new Vector3(slidingLabelRect.x - 8, slidingLabelRect.y + 8.5f, 0), view, (1 - faded2Dgray) * fadedRotationLock);
                }
            }
        }

        // Ensure all controlIDs used in this OnGUI are permanent controlIDs so this method can be called
        // in different places for handling input early and rendering late.
        internal void OnGUI(SceneView view)
        {
            float screenSize = Mathf.Min(view.position.width, view.position.height);
            if (screenSize < kRotationSize)
                return;

            if (Event.current.type == EventType.Repaint)
                Profiler.BeginSample("SceneView.AxisSelector");

            HandleContextClick(view);

            // This is a pretty weird way of doing things, but it works, so...
            Camera cam = view.camera;

            HandleUtility.PushCamera(cam);
            if (cam.orthographic)
                cam.orthographicSize = .5f;

            cam.cullingMask = 0;
            cam.transform.position = cam.transform.rotation  * new Vector3(0, 0, -5);
            cam.clearFlags = CameraClearFlags.Nothing;
            cam.nearClipPlane = .1f;
            cam.farClipPlane = 10;
            cam.fieldOfView = view.m_Ortho.Fade(70, 0);

            SceneView.AddCursorRect(new Rect(view.position.width - kRotationSize + kRotationMenuInset, kRotationMenuInset, kRotationSize - 2 * kRotationMenuInset, kRotationSize + 24 - kRotationMenuInset), MouseCursor.Arrow);

            Handles.SetCamera(new Rect(view.position.width - kRotationSize, 0, kRotationSize, kRotationSize), cam);

            Handles.BeginGUI();

            DrawRotationLock(view);
            DrawLabels(view);

            Handles.EndGUI();

            // animate visibility of three axis widgets
            for (int i = 0; i < 3; ++i)
            {
                Vector3 direction = kDirectionRotations[i] * Vector3.forward;
                dirVisible[i].target = Mathf.Abs(Vector3.Dot(cam.transform.forward, direction)) < 0.9f;
            }

            float size = HandleUtility.GetHandleSize(Vector3.zero) * .2f;

            // Do axes behind the center one
            AxisSelectors(view, cam, size, -1.0f, styles.viewAxisLabelStyle);

            // Do center handle

            Color centerColor = Handles.centerColor;
            centerColor = Color.Lerp(centerColor, Color.gray, faded2Dgray);
            centerColor.a *= fadedVisibility;
            if (centerColor.a <= 0.1f || view.isRotationLocked)
                GUI.enabled = false;

            Handles.color = centerColor;
            if (Handles.Button(m_CenterButtonControlID, Vector3.zero, Quaternion.identity, size * 0.8f, size, Handles.CubeHandleCap) && !view.in2DMode && !view.isRotationLocked)
            {
                if (Event.current.clickCount == 2)
                    view.FrameSelected();
                else
                {
                    // If middle-click or shift-click, choose a perspective view from a nice angle,
                    // similar to in Unity 3.5.
                    if (Event.current.shift || Event.current.button == 2)
                        ViewFromNiceAngle(view, true);
                    // Else, toggle perspective
                    else
                        ViewSetOrtho(view, !view.orthographic);
                }
            }

            // Do axes in front of the center one
            AxisSelectors(view, cam, size, 1.0f, styles.viewAxisLabelStyle);

            GUI.enabled = true;

            if (!view.in2DMode && !view.isRotationLocked)
            {
                // Swipe handling
                if (Event.current.type == EditorGUIUtility.swipeGestureEventType)
                {
                    // Get swipe dir as Vector3
                    Event evt = Event.current;
                    Vector3 swipeDir;
                    if (evt.delta.y > 0)
                        swipeDir = Vector3.up;
                    else if (evt.delta.y < 0)
                        swipeDir = -Vector3.up;
                    else if (evt.delta.x < 0) // delta x inverted for some reason
                        swipeDir = Vector3.right;
                    else
                        swipeDir = -Vector3.right;

                    // Inverse swipe dir, so swiping down will go to top view etc.
                    // This is consistent with how we do orbiting, where moving the mouse down sees the object more from above etc.
                    // Also, make swipe dir point almost 45 degrees in towards the camera.
                    // This means the swipe will pick the closest axis in the swiped direction,
                    // instead of picking the one closest to being 90 degrees away.
                    Vector3 goalVector = -swipeDir - Vector3.forward * 0.9f;

                    // Transform swipe dir by camera transform, so up is camera's local up, etc.
                    goalVector = view.camera.transform.TransformDirection(goalVector);

                    // Get global axis that's closest to the swipe dir vector
                    float bestDotProduct = 0;
                    int dir = 0;
                    for (int i = 0; i < 6; i++)
                    {
                        // Note that kDirectionRotations are not the forward direction of each dir;
                        // it's the back direction *towards* the camera.
                        float dotProduct = Vector3.Dot(kDirectionRotations[i] * -Vector3.forward, goalVector);
                        if (dotProduct > bestDotProduct)
                        {
                            bestDotProduct = dotProduct;
                            dir = i;
                        }
                    }

                    // Look along chosen axis
                    ViewAxisDirection(view, dir);
                    Event.current.Use();
                }
            }

            HandleUtility.PopCamera(cam);
            Handles.SetCamera(cam);

            if (Event.current.type == EventType.Repaint)
                Profiler.EndSample();
        }

        private void ViewAxisDirection(SceneView view, int dir)
        {
            // If holding shift or clicking with middle mouse button, orthographic is enforced, otherwise not altered.
            // Note: This function can also be called from a context menu where Event.current is null.
            bool ortho = view.orthographic;
            if (Event.current != null && (Event.current.shift || Event.current.button == 2))
                ortho = true;

            view.LookAt(view.pivot, kDirectionRotations[dir] , view.size, ortho);
            // Set label to according direction
            SwitchDirNameVisible(dir);
        }

        private void ViewSetOrtho(SceneView view, bool ortho)
        {
            view.LookAt(view.pivot, view.rotation, view.size, ortho);
        }

        internal void UpdateGizmoLabel(SceneView view, Vector3 direction, bool ortho)
        {
            SwitchDirNameVisible(GetLabelIndexForView(view, direction, ortho));
        }

        internal int GetLabelIndexForView(SceneView view, Vector3 direction, bool ortho)
        {
            if (!view.in2DMode)
            {
                // If the view is axis aligned, find the correct axis.
                if (IsAxisAligned(direction))
                    for (int i = 0; i < 6; i++)
                        if (Vector3.Dot(kDirectionRotations[i] * Vector3.forward, direction) > 0.9f)
                            return i;

                // If the view is not axis aligned, set label to Iso or Persp.
                return ortho ? 6 : 7;
            }
            return 8; // 2D mode
        }

        private void ViewFromNiceAngle(SceneView view, bool forcePerspective)
        {
            // Use dir that's the same as the current one in the x-z plane, but placed a bit above middle vertically.
            // (Same as old dir except it had the x-z dir fixed.)
            Vector3 dir = view.rotation * Vector3.forward;
            dir.y = 0;
            if (dir == Vector3.zero)
                // When the view is top or bottom, the closest nice view is to look approx. towards z.
                dir = Vector3.forward;
            else
                // Otherwise pick dir based on existing dir.
                dir = dir.normalized;
            // Look at object a bit from above.
            dir.y = -0.5f;

            bool ortho = forcePerspective ? false : view.orthographic;
            view.LookAt(view.pivot, Quaternion.LookRotation(dir), view.size, ortho);
            SwitchDirNameVisible(ortho ? 6 : 7);
        }

        private bool IsAxisAligned(Vector3 v)
        {
            return (Mathf.Abs(v.x * v.y) < 0.0001f && Mathf.Abs(v.y * v.z) < 0.0001f && Mathf.Abs(v.z * v.x) < 0.0001f);
        }

        private void SwitchDirNameVisible(int newVisible)
        {
            if (newVisible == currentDir)
                return;

            dirNameVisible[currentDir].target = false;
            currentDir = newVisible;
            dirNameVisible[currentDir].target = true;

            // Fade whole Scene View Gizmo in / out when switching 2D mode.
            if (newVisible == 8)
                m_Visible.speed = 0.3f;
            else
                m_Visible.speed = 2f;

            m_Visible.target = (newVisible != 8);
        }
    }
} //namespace
