// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using BlendMode = UnityEngine.Rendering.BlendMode;

[Overlay(typeof(SceneView), "Orientation", true)]
[Icon("Icons/Overlays/OrientationGizmo.png")]
sealed class SceneOrientationGizmo : IMGUIOverlay
{
    static readonly Color k_PickingClearColor = Color.magenta;
    const int kRotationSize = 100;
    const float kRotationLockedAlpha = 0.4f;
    const int kPerspOrthoLabelHeight = 16;
    const string k_ShowOrientationHeader = "overlay-show-orientation-header";
    const string k_ShowOrientationBackground = "overlay-show-orientation-background";

    static Quaternion[] kDirectionRotations =
    {
        Quaternion.LookRotation(new Vector3(-1, 0, 0)),
        Quaternion.LookRotation(new Vector3(0, -1, 0)),
        Quaternion.LookRotation(new Vector3(0, 0, -1)),
        Quaternion.LookRotation(new Vector3(1, 0, 0)),
        Quaternion.LookRotation(new Vector3(0, 1, 0)),
        Quaternion.LookRotation(new Vector3(0, 0, 1)),
    };

    internal static string[] kDirNames =
    { "Right", "Top", "Front", "Left", "Bottom", "Back", "Iso", "Persp", "2D" };

    internal static string[] kMenuDirNames =
    { "Free", "Right", "Top", "Front", "Left", "Bottom", "Back", "", "Perspective", "", "Show background" };

    static readonly GUIContent[] s_HandleAxisLabels =
    {
        new GUIContent("x"), new GUIContent("y"), new GUIContent("z")
    };

    bool showBackGround
    {
        get
        {
            if (!EditorPrefs.HasKey(k_ShowOrientationBackground))
                showBackGround = false;

            return EditorPrefs.GetBool(k_ShowOrientationBackground);
        }
        set
        {
            if (EditorPrefs.HasKey(k_ShowOrientationBackground))
            {
                var currentValue = EditorPrefs.GetBool(k_ShowOrientationBackground);
                if (currentValue == value) return;
            }

            EditorPrefs.SetBool(k_ShowOrientationBackground, value);
            UpdateHeaderAndBackground();
        }
    }

    int[] m_ViewDirectionControlIDs;
    int m_CenterButtonControlID;
    int m_RotationLockControlID;
    int m_PerspectiveIsoControlID;
    readonly Color k_RTBackground = new Color(1, 1, 1, 0);
    RenderTexture m_RenderTexture;
    Camera m_Camera;

    int currentDir = 7;
    internal AnimBool[] dirVisible = { new AnimBool(true), new AnimBool(true), new AnimBool(true) };

    internal AnimBool[] dirNameVisible =
    {
        new AnimBool(), new AnimBool(), new AnimBool(), new AnimBool(), new AnimBool(), new AnimBool(),
        new AnimBool(), new AnimBool(), new AnimBool()
    };

    float faded2Dgray
    {
        get { return dirNameVisible[8].faded; }
    }

    float fadedRotationLock
    {
        get { return Mathf.Lerp(kRotationLockedAlpha, 1.0f, m_RotationLocked.faded); }
    }

    AnimBool m_RotationLocked = new AnimBool();
    AnimBool m_Visible = new AnimBool();

    float fadedVisibility
    {
        get
        {
            return m_Visible.faded * fadedRotationLock;
        }
    }

    static class Styles
    {
        public static GUIStyle viewLabelStyleLeftAligned = "SC ViewLabelLeftAligned";
        public static GUIStyle viewLabelStyleCentered = "SC ViewLabelCentered";
        public static GUIStyle viewAxisLabelStyle = "SC ViewAxisLabel";
        public static GUIStyle lockStyle = "CenteredLabel";
        public static GUIContent unlockedRotationIcon = EditorGUIUtility.TrIconContent("LockIcon", "Click to lock the rotation in the current direction.");
        public static GUIContent lockedRotationIcon = EditorGUIUtility.TrIconContent("LockIcon-On", "Click to unlock the rotation.");
    }

    struct BlendingScope : IDisposable
    {
        int m_srcMode;
        int m_dstMode;

        public BlendingScope(BlendMode srcMode, BlendMode dstMode)
        {
            m_srcMode = (int)HandleUtility.handleMaterial.GetFloat("_BlendSrcMode");
            HandleUtility.handleMaterial.SetFloat("_BlendSrcMode", (int)srcMode);

            m_dstMode = (int)HandleUtility.handleMaterial.GetFloat("_BlendDstMode");
            HandleUtility.handleMaterial.SetFloat("_BlendDstMode", (int)dstMode);
        }

        public void Dispose()
        {
            HandleUtility.handleMaterial.SetFloat("_BlendSrcMode", m_srcMode);
            HandleUtility.handleMaterial.SetFloat("_BlendDstMode", m_dstMode);
        }
    }

    public SceneOrientationGizmo()
    {
        collapsedChanged += OnCollapsedChanged;
    }

    void OnCollapsedChanged(bool _)
    {
        UpdateHeaderAndBackground();
    }

    const string k_OrientationVisibilityOff = "orientation-visibility-off";
    void UpdateOverlayDisplay(bool visibility)
    {
        m_HasMenuEntry = visibility;
        rootVisualElement.EnableInClassList(k_OrientationVisibilityOff, !visibility);
    }

    void UpdateHeaderAndBackground()
    {
        var headerElement = rootVisualElement.Q(className: "overlay-header");
        if (headerElement != null)
            if (!collapsed)
                headerElement.BringToFront();
            else
                headerElement.SendToBack();

        rootVisualElement.EnableInClassList(k_ShowOrientationHeader, !collapsed);
        rootVisualElement.EnableInClassList(k_ShowOrientationBackground, !collapsed && !showBackGround);
    }

    internal void SkipFading()
    {
        for (int i = dirVisible.Length - 1; i >= 0; i--)
            dirVisible[i].SkipFading();

        for (int i = dirNameVisible.Length - 1; i >= 0; i--)
            dirNameVisible[i].SkipFading();

        m_RotationLocked.SkipFading();
        m_Visible.SkipFading();
    }

    public override void OnCreated()
    {
        if (!(containerWindow is SceneView view))
        {
            Debug.LogError("Scene Orientation Overlay was added to an EditorWindow that is not a Scene View.");
            return;
        }

        SetupRenderTexture();

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

        GameObject cameraGO = EditorUtility.CreateGameObjectWithHideFlags("SceneCamera", HideFlags.HideAndDontSave, typeof(Camera));
        m_Camera = cameraGO.GetComponent<Camera>();
        m_Camera.enabled = false;
        m_Camera.cameraType = CameraType.SceneView;

        SwitchDirNameVisible(labelIndex);

        if (m_ViewDirectionControlIDs == null)
        {
            m_ViewDirectionControlIDs = new int[kDirectionRotations.Length];
            for (int i = 0; i < m_ViewDirectionControlIDs.Length; ++i)
                m_ViewDirectionControlIDs[i] = GUIUtility.GetPermanentControlID();

            m_CenterButtonControlID = GUIUtility.GetPermanentControlID();
            m_RotationLockControlID = GUIUtility.GetPermanentControlID();
            m_PerspectiveIsoControlID = GUIUtility.GetPermanentControlID();
        }
    }

    public override void OnWillBeDestroyed()
    {
        if (m_RenderTexture != null)
            m_RenderTexture.Release();
        Object.DestroyImmediate(m_RenderTexture);
        if (m_Camera != null)
            Object.DestroyImmediate(m_Camera.gameObject);
    }

    internal override void OnContentRebuild()
    {
        UpdateHeaderAndBackground();
    }

    void AxisSelectors(SceneView view, Camera cam, float size, float sgn, bool hovered)
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
                case 0:
                    c = Handles.xAxisColor;
                    break;
                case 1:
                    c = Handles.yAxisColor;
                    break;
                case 2:
                    c = Handles.zAxisColor;
                    break;
                default:
                    c = Handles.centerColor;
                    break;
            }

            if (view.in2DMode)
            {
                c = Color.Lerp(c, Color.gray, faded2Dgray);
            }

            c.a *= a * fadedVisibility;

            //Not displaying the axis when rotation is locked and axis invisible otherwise One Zero blending not working fine
            if (!(view.isRotationLocked && c.a < Mathf.Epsilon))
            {
                if (QualitySettings.activeColorSpace == ColorSpace.Linear)
                    c = c.linear;

                Handles.color = c;

                if (c.a <= 0.1f || view.isRotationLocked)
                    GUI.enabled = false;

                // axis widget when drawn behind label
                if (sgn > 0 && Handles.Button(m_ViewDirectionControlIDs[h], q1 * Vector3.forward * size * -1.2f, q1,
                    size, size * 0.7f, Handles.ConeHandleCap, hovered))
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
                    Handles.Label(-pos, s_HandleAxisLabels[h], Styles.viewAxisLabelStyle);
                }

                // axis widget when drawn in front of label
                // Adding the hovered parameter to avoid all the axis proximity checks that are time consuming
                // when the cursor is not hovering the Orientation Gizmo
                if (sgn < 0 && Handles.Button(m_ViewDirectionControlIDs[h], q1 * Vector3.forward * size * -1.2f, q1,
                    size, size * 0.7f, Handles.ConeHandleCap, hovered))
                {
                    if (!view.in2DMode && !view.isRotationLocked)
                        ViewAxisDirection(view, h);
                }
            }

            Handles.color = Color.white;
            GUI.color = Color.white;
            GUI.enabled = true;
        }
    }

    void DisplayContextMenu(Rect buttonOrCursorRect, SceneView view)
    {
        int[] selectedItems = new int[view.orthographic ? 2 : 3];
        selectedItems[0] = currentDir >= 6 ? 0 : currentDir + 1;

        selectedItems[1] = showBackGround ? 10 : 0;

        if (!view.orthographic)
            selectedItems[2] = 8;

        EditorUtility.DisplayCustomMenu(buttonOrCursorRect, kMenuDirNames, selectedItems, ContextMenuDelegate, view);
        GUIUtility.ExitGUI();
    }

    void ContextMenuDelegate(object userData, string[] options, int selected)
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
            showBackGround = !showBackGround;
        }
    }

    void DrawIsoStatusSymbol(Vector3 center, SceneView view, float alpha)
    {
        float persp = 1 - Mathf.Clamp01(view.m_Ortho.faded * 1.2f - 0.1f);
        Vector3 up = Vector3.up * 3;
        Vector3 right = Vector3.right * 10;
        Vector3 pos = center - right * 0.5f;

        Handles.color = new Color(1, 1, 1, 0.6f * alpha);
        Handles.DrawAAPolyLine(pos + up * (1 - persp), pos + right + up * (1 + persp * 0.5f));
        Handles.DrawAAPolyLine(pos, pos + right);
        Handles.DrawAAPolyLine(pos - up * (1 - persp), pos + right - up * (1 + persp * 0.5f));
    }

    void DrawRotationLock(SceneView view, Rect rect)
    {
        const float clickWidth = 24;
        const float clickHeight = 24;
        float lockCenterX = (rect.width / EditorGUIUtility.pixelsPerPoint) - 16;
        float lockCenterY = 17;
        Rect lockRect = new Rect(lockCenterX - (clickWidth / 2), lockCenterY - (clickHeight / 2), clickWidth,
            clickHeight);
        Color c = Handles.centerColor;
        c.a *= m_Visible.faded;
        if (c.a > 0.0f)
        {
            var prevColor = GUI.color;
            GUI.color = c;
            var content = (view.isRotationLocked) ? Styles.lockedRotationIcon : Styles.unlockedRotationIcon;
            if (GUI.Button(lockRect, m_RotationLockControlID, content, Styles.lockStyle) && !view.in2DMode)
            {
                view.isRotationLocked = !view.isRotationLocked;
                m_RotationLocked.target = !view.isRotationLocked;
            }

            GUI.color = prevColor;
        }
    }

    void DrawLabels(SceneView view, Rect rect)
    {
        Rect labelRect = new Rect(
            rect.width - kRotationSize + 17,
            kRotationSize - 8,
            kRotationSize - 17 * 2,
            kPerspOrthoLabelHeight);

        // Button (overlayed over the labels) to toggle between iso and perspective
        if (!view.in2DMode && !view.isRotationLocked)
        {
            if (GUI.Button(labelRect, m_PerspectiveIsoControlID, GUIContent.none, Styles.viewLabelStyleLeftAligned))
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
                    width +=
                        Styles.viewLabelStyleLeftAligned.CalcSize(EditorGUIUtility.TempContent(kDirNames[i])).x *
                        dirNameVisible[i].faded;
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
                    GUI.Label(slidingLabelRect, kDirNames[i], Styles.viewLabelStyleLeftAligned);
                }
            }

            // Then draw just the label for 2D. It uses the original labelRect, and with a style where the text is horizontally centered.
            {
                Color c = Handles.centerColor;
                c.a *= faded2Dgray * fadedVisibility;
                if (c.a > 0.0f)
                {
                    GUI.color = c;
                    GUI.Label(labelRect, kDirNames[index2D], Styles.viewLabelStyleCentered);
                }
            }

            // Draw the iso status symbol - the passed Vector3 is the center
            if (faded2Dgray < 1)
            {
                DrawIsoStatusSymbol(new Vector3(slidingLabelRect.x - 8, slidingLabelRect.y + 8.5f, 0), view,
                    (1 - faded2Dgray) * fadedRotationLock);
            }
        }
    }

    void SetupRenderTexture()
    {
        GraphicsFormat format = SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, FormatUsage.Render)
            ? GraphicsFormat.R16G16B16A16_SFloat
            : SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);

        if (m_RenderTexture != null)
        {
            if (m_RenderTexture.graphicsFormat != format)
                Object.DestroyImmediate(m_RenderTexture);
        }

        int size = (int)(kRotationSize * EditorGUIUtility.pixelsPerPoint);

        if (m_RenderTexture == null)
            m_RenderTexture = new RenderTexture(size, size, 24, format, 1) { hideFlags = HideFlags.HideAndDontSave };

        if (m_RenderTexture.width != size || m_RenderTexture.height != size)
        {
            m_RenderTexture.Release();
            m_RenderTexture.width = m_RenderTexture.height = size;
        }

        m_RenderTexture.Create();
    }

    void SetupCamera(SceneView view)
    {
        var scene = view.camera.transform;
        m_Camera.CopyFrom(view.camera);
        m_Camera.transform.rotation = scene.rotation;

        m_Camera.enabled = false;
        m_Camera.allowHDR = false;
        m_Camera.allowMSAA = false;
        m_Camera.clearFlags = CameraClearFlags.Color;
        m_Camera.backgroundColor = k_PickingClearColor;

        m_Camera.targetTexture = m_RenderTexture;
        m_Camera.clearFlags = CameraClearFlags.SolidColor;
        m_Camera.backgroundColor = Color.clear;

        if (m_Camera.orthographic)
            m_Camera.orthographicSize = .5f;

        m_Camera.cullingMask = 0;
        m_Camera.transform.position = m_Camera.transform.rotation * new Vector3(0, 0, -5);
        m_Camera.clearFlags = CameraClearFlags.Nothing;
        m_Camera.nearClipPlane = .1f;
        m_Camera.farClipPlane = 10;
        m_Camera.fieldOfView = view.m_Ortho.Fade(70, 0);
    }

    public override void OnGUI()
    {
        if (!(containerWindow is SceneView view))
            return;

        Rect gizmoRect = new Rect(0, 0, kRotationSize, kRotationSize);
        GUILayoutUtility.GetRect(gizmoRect.width, gizmoRect.height + kPerspOrthoLabelHeight);

        Event evt = Event.current;

        if (evt.type == EventType.MouseDown && evt.button == 1
            && !view.in2DMode
            && !view.isRotationLocked
            && gizmoRect.Contains(evt.mousePosition))
        {
            DisplayContextMenu(new Rect(evt.mousePosition.x, evt.mousePosition.y, 0, 0), view);
            evt.Use();
        }

        // Usually this does nothing, but when dragging the window between monitors that have different scaling
        // it can resize the RT as necessary.
        SetupRenderTexture();
        var prevCamera = Camera.current;
        SetupCamera(view);

        //Handle transparency when rotation is locked
        using (new BlendingScope(
            view.isRotationLocked ? BlendMode.One : BlendMode.SrcAlpha,
            view.isRotationLocked ? BlendMode.Zero : BlendMode.OneMinusSrcAlpha))
        {
            using (new GUI.GroupScope(gizmoRect))
            {
                var temp = RenderTexture.active;
                Handles.SetCamera(gizmoRect, m_Camera);

                if (evt.type == EventType.Repaint)
                {
                    RenderTexture.active = m_RenderTexture;
                    GL.Clear(false, true, k_RTBackground);
                    Handles.ClearCamera(gizmoRect, m_Camera);
                    GUIClip.Internal_PushParentClip(Matrix4x4.identity, GUIClip.GetParentMatrix(), gizmoRect);
                }

                DoOrientationHandles(view, m_Camera, gizmoRect.Contains(evt.mousePosition));

                if (evt.type == EventType.Repaint)
                    GUIClip.Internal_PopParentClip();

                RenderTexture.active = temp;
            }

            GUI.Label(gizmoRect, m_RenderTexture);

            Rect labelRect = gizmoRect;
            labelRect.y += gizmoRect.height;
            DrawLabels(view, labelRect);
        }

        Camera.SetupCurrent(prevCamera);
    }

    void DoOrientationHandles(SceneView view, Camera camera, bool isMouseHovering)
    {
        Handles.BeginGUI();
        DrawRotationLock(view, camera.pixelRect);
        Handles.EndGUI();

        // animate visibility of three axis widgets
        for (int i = 0; i < 3; ++i)
        {
            Vector3 direction = kDirectionRotations[i] * Vector3.forward;
            dirVisible[i].target = Mathf.Abs(Vector3.Dot(camera.transform.forward, direction)) < 0.9f;
        }

        float size = HandleUtility.GetHandleSize(Vector3.zero) * .2f;

        // Do axes behind the center one
        AxisSelectors(view, camera, size, -1.0f, isMouseHovering);

        // Do center handle
        Color centerColor = Handles.centerColor;
        centerColor = Color.Lerp(centerColor, Color.gray, faded2Dgray);
        centerColor.a *= fadedVisibility;
        if (centerColor.a <= 0.1f || view.isRotationLocked)
            GUI.enabled = false;

        Handles.color = centerColor;
        if (Handles.Button(m_CenterButtonControlID, Vector3.zero, Quaternion.identity, size * 0.8f, size,
            Handles.CubeHandleCap) && !view.in2DMode && !view.isRotationLocked)
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
        AxisSelectors(view, camera, size, 1.0f, isMouseHovering);

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
    }

    internal void ViewAxisDirection(SceneView view, int dir, bool ortho)
    {
        view.LookAt(view.pivot, kDirectionRotations[dir], view.size, ortho);
        // Set label to according direction
        SwitchDirNameVisible(dir);
    }

    internal void ViewAxisDirection(SceneView view, int dir)
    {
        // If holding shift or clicking with middle mouse button, orthographic is enforced, otherwise not altered.
        // Note: This function can also be called from a context menu where Event.current is null.
        bool ortho = view.orthographic ||
            Event.current != null && (Event.current.shift || Event.current.button == 2);

        ViewAxisDirection(view, dir, ortho);
    }

    internal void ViewSetOrtho(SceneView view, bool ortho)
    {
        view.LookAt(view.pivot, view.rotation, view.size, ortho);
    }

    internal void ViewSetUnityDefault(SceneView view)
    {
        // Unity default point of view
        view.LookAt(view.pivot, Quaternion.LookRotation(new Vector3(-1, -.7f, -1)), view.size, view.orthographic);
    }

    internal void ViewSetMayaDefault(SceneView view)
    {
        // Maya default point of view
        view.LookAt(view.pivot, Quaternion.LookRotation(new Vector3(1, -.7f, -1)), view.size, view.orthographic);
    }

    internal void ViewSetMaxDefault(SceneView view)
    {
        // 3DSMax default point of view
        view.LookAt(view.pivot, Quaternion.LookRotation(new Vector3(1, -.7f, 1)), view.size, view.orthographic);
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

    internal void ViewFromNiceAngle(SceneView view, bool forcePerspective)
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

    bool IsAxisAligned(Vector3 v)
    {
        return (Mathf.Abs(v.x * v.y) < 0.0001f && Mathf.Abs(v.y * v.z) < 0.0001f && Mathf.Abs(v.z * v.x) < 0.0001f);
    }

    void SwitchDirNameVisible(int newVisible)
    {
        if (newVisible == currentDir)
            return;

        dirNameVisible[currentDir].target = false;
        currentDir = newVisible;
        dirNameVisible[currentDir].target = true;

        // Fade whole Scene View Gizmo in / out when switching 2D mode.
        if (newVisible == 8)
        {
            UpdateOverlayDisplay(false);
            m_Visible.speed = 0.3f;
        }
        else
        {
            UpdateOverlayDisplay(true);
            m_Visible.speed = 2f;
        }

        m_Visible.target = (newVisible != 8);
    }
}
