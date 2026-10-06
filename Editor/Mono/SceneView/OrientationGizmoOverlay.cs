// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

// Keeps the pre-existing "Orientation" id, so a user's saved layout carries over.
[Overlay(typeof(SceneView), k_Id, true,
    priority = (int)OverlayPriority.Orientation,
    defaultDockZone = DockZone.RightColumn,
    defaultDockPosition = DockPosition.Top,
    defaultDockIndex = 0,
    group = OverlayAttribute.unityGroup)]
[Icon("Icons/Overlays/OrientationGizmo.png")]
sealed class OrientationGizmoOverlay : Overlay
{
    const string k_Id = "Orientation";
    const string k_ExpandedClass = "overlay-orientation-gizmo-expanded";
    const string k_HideBackgroundClass = "overlay-hide-orientation-gizmo-background";
    const string k_ShowBackgroundPref = "overlay-show-orientation-background";
    internal const string k_StylePath = "StyleSheets/Overlays/OrientationGizmo.uss";
    const float k_DragHandleInset = 3f;

    SceneView m_SceneView;
    OrientationGizmoElement m_Element;

    internal override void PopulateRoot(VisualElement root)
    {
        base.PopulateRoot(root);
        root.styleSheets.Add(EditorGUIUtility.Load(k_StylePath) as StyleSheet);
    }

    internal bool showBackground
    {
        get => EditorPrefs.GetBool(k_ShowBackgroundPref, true);
        set
        {
            if (showBackground == value)
                return;
            EditorPrefs.SetBool(k_ShowBackgroundPref, value);
            UpdateHeaderAndBackground();
        }
    }

    public OrientationGizmoOverlay()
    {
        collapsedChanged += OnCollapsedChanged;
    }

    void OnCollapsedChanged(bool _)
    {
        UpdateHeaderAndBackground();
    }

    void UpdateHeaderAndBackground()
    {
        var headerElement = rootVisualElement.Q(className: "overlay-header");
        if (headerElement != null)
        {
            if (!collapsed)
            {
                headerElement.BringToFront();
                headerElement.style.top = k_DragHandleInset;
                headerElement.style.left = k_DragHandleInset;
                // Clamped short of the lock icon and the top axis marker, or the header (in front,
                // full-width by default) would swallow their clicks.
                headerElement.style.width = OrientationGizmoElement.k_LockIconLeftEdge - k_DragHandleInset;
                headerElement.style.height = OrientationGizmoElement.k_TopAxisMarkerEdge - k_DragHandleInset;
            }
            else
            {
                headerElement.SendToBack();
                headerElement.style.top = StyleKeyword.Null;
                headerElement.style.left = StyleKeyword.Null;
                headerElement.style.width = StyleKeyword.Null;
                headerElement.style.height = StyleKeyword.Null;
            }
        }

        rootVisualElement.EnableInClassList(k_ExpandedClass, !collapsed);
        rootVisualElement.EnableInClassList(k_HideBackgroundClass, !collapsed && !showBackground);
    }

    public override void OnCreated()
    {
        // Not UpdateHeaderAndBackground() here: BringToFront()'ing the header before PopulateRoot
        // adds the drop zones/resize target would displace it from its normal top position.

        // Subscribed on the Overlay rather than the content element: SetForceHidden tears down that
        // content, which would unsubscribe a content-owned listener before 2D mode ends.
        m_SceneView = containerWindow as SceneView;
        if (m_SceneView != null)
        {
            SetHiddenFor2DMode(m_SceneView.in2DMode);
            m_SceneView.modeChanged2D += SetHiddenFor2DMode;
            SceneView.duringSceneGui += OnDuringSceneGui;
        }
    }

    public override void OnWillBeDestroyed()
    {
        if (m_SceneView != null)
        {
            m_SceneView.modeChanged2D -= SetHiddenFor2DMode;
            SceneView.duringSceneGui -= OnDuringSceneGui;
        }
    }

    internal void SetHiddenFor2DMode(bool hidden)
    {
        SetForceHidden(hidden);
    }

    // Trackpad two-finger swipe (macOS only - see EditorGUIUtility.swipeGestureEventType).
    void OnDuringSceneGui(SceneView view)
    {
        if (view != m_SceneView || view.in2DMode)
            return;

        // panel == null distinguishes a stale, detached m_Element from a live one: hiding or
        // collapsing tears down the content without clearing the field.
        if (!displayed || collapsed || m_Element == null || m_Element.panel == null)
            return;

        var evt = Event.current;
        if (evt.type != EditorGUIUtility.swipeGestureEventType)
            return;

        m_Element.HandleSwipeGesture(evt.delta);
        evt.Use();
    }

    public override VisualElement CreatePanelContent()
    {
        UpdateHeaderAndBackground();

        var target = CreateTarget(containerWindow);
        if (target == null)
        {
            Debug.LogError("OrientationGizmoOverlay was added to an EditorWindow that is not a SceneView.");
            return new VisualElement();
        }

        m_Element = new OrientationGizmoElement(target, this);
        return m_Element;
    }

    static OrientationGizmoTarget CreateTarget(EditorWindow window)
    {
        return window is SceneView view ? new SceneViewGizmoTarget(view) : null;
    }
}
