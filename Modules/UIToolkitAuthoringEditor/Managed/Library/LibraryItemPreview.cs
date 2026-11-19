// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// A popup that previews a library item: the control, or the UXML document, rendered in a <see cref="PanelElement"/>
/// set up like the canvas of the document the library adds to (context type, panel settings, theme and canvas size).
/// The popup has the same width for every item, and the height of what it shows, up to a maximum: the content is laid
/// out wider than it and zoomed out to fit. It shows the background of <see cref="Canvas"/> behind the content, and
/// a thin outline around it.
/// </summary>
/// <remarks>
/// Add it to the element it floats over, the root of the window usually. It shows beside its
/// <see cref="LibraryView"/>, level with the pointer, while the pointer hovers an item of the library, and follows
/// the pointer while an item of that library is dragged over the element it floats over.
/// An item whose preview throws or logs an error is not previewed. What the preview logs while it is built is
/// dropped, so hovering an item that fails does not fill the console.
/// </remarks>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
sealed class LibraryItemPreview : VisualElement
{
    public const string UssClassName = "unity-library-item-preview";

    // Every preview has the same width, and at most the same height, in points, whatever the scale of the canvas.
    // The content is laid out at the canvas scale, wider than the preview so that wide controls (a Vector4Field, a
    // RectField) show their fields, and is always zoomed out to fit. Content too large to fit at the smallest scale
    // is cut.
    static readonly Vector2 k_MaxPreviewSize = new(400, 300);
    const float k_LayoutWidth = 640;
    const float k_MinContentScale = 0.2f;
    const float k_MinContentHeight = 20;

    // From the pointer to the popup, and from the popup to the edges of the element it floats over.
    const float k_PointerOffset = 16;
    const float k_EdgeMargin = 4;

    const float k_OutlineWidth = 1;
    static readonly Color k_OutlineColor = new(0, 0, 0, 0.7f);

    // The canvas size when no document is open, which only matters for panel settings that scale with the screen.
    static readonly Vector2 k_DefaultCanvasSize = new(1280, 720);

    readonly LibraryView m_LibraryView;
    readonly CanvasBackdropTexture m_BackdropTexture = new();

    PanelElement m_PanelElement;
    Vector2 m_Size = k_MaxPreviewSize;
    VisualElement m_Content;
    VisualTreeAsset m_GeneratedAsset;
    VisualElement m_DragArea;

    // The item built last, whether its preview is shown or failed, so that moving over it does not build it again.
    LibraryItem m_Item;
    bool m_IsBuilt;

    // The library drags a UXML document as an asset, which only the last item the pointer was over tells apart from
    // a document dragged from elsewhere.
    LibraryItem m_LastHoveredItem;

    public LibraryItemPreview(LibraryView libraryView)
    {
        m_LibraryView = libraryView ?? throw new ArgumentNullException(nameof(libraryView));

        AddToClassList(UssClassName);
        pickingMode = PickingMode.Ignore;
        style.position = Position.Absolute;
        style.display = DisplayStyle.None;

        style.width = m_Size.x;
        style.height = m_Size.y;

        // Outside the preview, so that it does not pass for part of the control, and sets the preview apart from a
        // canvas with the same background.
        var outline = new VisualElement { name = "library-item-preview-outline", pickingMode = PickingMode.Ignore };
        outline.style.position = Position.Absolute;
        outline.style.left = outline.style.top = outline.style.right = outline.style.bottom = -k_OutlineWidth;
        outline.style.borderLeftWidth = outline.style.borderTopWidth = k_OutlineWidth;
        outline.style.borderRightWidth = outline.style.borderBottomWidth = k_OutlineWidth;
        outline.style.borderLeftColor = outline.style.borderTopColor = k_OutlineColor;
        outline.style.borderRightColor = outline.style.borderBottomColor = k_OutlineColor;
        Add(outline);

        RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
    }

    /// <summary>
    /// The item the popup shows, or null when it is hidden.
    /// </summary>
    public LibraryItem Item => IsShowing ? m_Item : null;

    public bool IsShowing => style.display == DisplayStyle.Flex;

    /// <summary>
    /// The canvas whose background the preview shows behind the content, the way the canvas shows it behind the
    /// document. Without one, the preview has no background.
    /// </summary>
    public UICanvas Canvas { get; set; }

    /// <summary>
    /// The panel the preview renders in, while the popup is attached.
    /// </summary>
    internal PanelElement PanelElement => m_PanelElement;

    /// <summary>
    /// What the preview of the shown item holds, in its panel.
    /// </summary>
    internal VisualElement Content => m_Content;

    /// <summary>
    /// Shows <paramref name="item"/> next to <paramref name="pointerPosition"/>, a world position, building its
    /// preview unless it is the item shown already, in a panel still set up like the canvas. Returns false, and
    /// hides the popup, when the item has no preview: its control or document throws or logs an error.
    /// </summary>
    public bool Show(LibraryItem item, Vector2 pointerPosition) => Show(item, pointerPosition, besideLibrary: false);

    /// <summary>
    /// Shows <paramref name="item"/> the way <see cref="Show(LibraryItem, Vector2)"/> does, but beside the library
    /// rather than next to the pointer, level with <paramref name="pointerPosition"/>: the popup leaves the rows of
    /// the library uncovered while the pointer moves over them.
    /// </summary>
    public bool ShowBesideLibrary(LibraryItem item, Vector2 pointerPosition) => Show(item, pointerPosition, besideLibrary: true);

    bool Show(LibraryItem item, Vector2 pointerPosition, bool besideLibrary)
    {
        if (item == null || panel == null)
        {
            Hide();
            return false;
        }

        if (item != m_Item || !MatchesCanvasSetup())
        {
            m_Item = item;
            m_IsBuilt = TryBuild(item);
        }

        if (!m_IsBuilt)
        {
            style.display = DisplayStyle.None;
            return false;
        }

        style.display = DisplayStyle.Flex;
        if (besideLibrary)
            PlaceBesideLibrary(pointerPosition);
        else
            Place(pointerPosition);
        return true;
    }

    public void Hide()
    {
        style.display = DisplayStyle.None;
        m_Item = null;
        m_IsBuilt = false;

        if (m_Content != null && (m_Content.childCount > 0 || m_GeneratedAsset != null))
        {
            using var _ = new LogCapture();
            ClearContent();
        }
    }

    void OnAttachToPanel(AttachToPanelEvent evt)
    {
        m_LibraryView.RegisterCallback<PointerMoveEvent>(OnLibraryPointerMove, TrickleDown.TrickleDown);
        m_LibraryView.RegisterCallback<PointerLeaveEvent>(OnLibraryPointerLeave);
        m_LibraryView.RegisterCallback<WheelEvent>(OnLibraryWheel, TrickleDown.TrickleDown);

        m_DragArea = parent;
        m_DragArea.RegisterCallback<DragUpdatedEvent>(OnDragUpdated, TrickleDown.TrickleDown);
        m_DragArea.RegisterCallback<DragLeaveEvent>(OnDragLeave, TrickleDown.TrickleDown);
        m_DragArea.RegisterCallback<DragExitedEvent>(OnDragEnded, TrickleDown.TrickleDown);
        m_DragArea.RegisterCallback<DragPerformEvent>(OnDragEnded, TrickleDown.TrickleDown);
    }

    void OnDetachFromPanel(DetachFromPanelEvent evt)
    {
        m_LibraryView.UnregisterCallback<PointerMoveEvent>(OnLibraryPointerMove, TrickleDown.TrickleDown);
        m_LibraryView.UnregisterCallback<PointerLeaveEvent>(OnLibraryPointerLeave);
        m_LibraryView.UnregisterCallback<WheelEvent>(OnLibraryWheel, TrickleDown.TrickleDown);

        if (m_DragArea != null)
        {
            m_DragArea.UnregisterCallback<DragUpdatedEvent>(OnDragUpdated, TrickleDown.TrickleDown);
            m_DragArea.UnregisterCallback<DragLeaveEvent>(OnDragLeave, TrickleDown.TrickleDown);
            m_DragArea.UnregisterCallback<DragExitedEvent>(OnDragEnded, TrickleDown.TrickleDown);
            m_DragArea.UnregisterCallback<DragPerformEvent>(OnDragEnded, TrickleDown.TrickleDown);
            m_DragArea = null;
        }

        Hide();
        m_LastHoveredItem = null;
        style.backgroundImage = StyleKeyword.Null;

        if (m_PanelElement != null)
        {
            m_PanelElement.SubPanelRecreated -= OnSubPanelRecreated;
            m_PanelElement.OnBeforeRender -= OnBeforeRender;
            m_PanelElement.DestroyPanelPermanently();
            m_PanelElement = null;
            m_Content = null;
        }

        m_BackdropTexture.Release();
    }

    void OnLibraryPointerMove(PointerMoveEvent evt)
    {
        if (m_LibraryView.TryGetItemAtPosition(evt.position, out var item))
        {
            m_LastHoveredItem = item;
            ShowBesideLibrary(item, evt.position);
        }
        else
        {
            Hide();
        }
    }

    void OnLibraryPointerLeave(PointerLeaveEvent evt) => Hide();

    // The rows move under a pointer that does not.
    void OnLibraryWheel(WheelEvent evt) => Hide();

    void OnDragUpdated(DragUpdatedEvent evt)
    {
        var item = GetDraggedItem();
        if (item != null)
            Show(item, evt.mousePosition);
        else if (IsShowing)
            Hide();
    }

    // Leaving the area is leaving the window; leaving an element inside it is moving to another one.
    void OnDragLeave(DragLeaveEvent evt)
    {
        if (evt.target == m_DragArea)
            Hide();
    }

    void OnDragEnded(EventBase evt) => Hide();

    LibraryItem GetDraggedItem()
    {
        if (DragAndDrop.GetGenericData(LibraryItem.DragDataKey) is LibraryItem item)
            return item;

        if (m_LastHoveredItem is { isAsset: true } hovered &&
            DragAndDrop.paths is { Length: 1 } paths && paths[0] == hovered.assetPath &&
            DragAndDrop.objectReferences is { Length: 1 } objects && objects[0] is VisualTreeAsset)
            return hovered;

        return null;
    }

    void Place(Vector2 pointerPosition)
    {
        if (parent == null)
            return;

        var area = parent.layout.size;
        var pointer = parent.WorldToLocal(pointerPosition);
        var size = m_Size;

        style.left = PlaceAlong(pointer.x, size.x, area.x);
        style.top = PlaceAlong(pointer.y, size.y, area.y);
    }

    // Right of the library, else left of it when the window ends first; the top level with the pointer, raised as
    // much as the bottom of the window needs.
    void PlaceBesideLibrary(Vector2 pointerPosition)
    {
        if (parent == null)
            return;

        var area = parent.layout.size;
        var library = parent.WorldToLocal(m_LibraryView.worldBound);
        var pointer = parent.WorldToLocal(pointerPosition);
        var size = m_Size;

        var left = library.xMax + k_EdgeMargin;
        if (left + size.x > area.x - k_EdgeMargin)
            left = library.xMin - k_EdgeMargin - size.x;
        style.left = Mathf.Max(k_EdgeMargin, Mathf.Min(left, area.x - k_EdgeMargin - size.x));
        style.top = Mathf.Max(k_EdgeMargin, Mathf.Min(pointer.y, area.y - k_EdgeMargin - size.y));
    }

    // After the pointer when the popup fits there, else before it, and inside the area either way.
    static float PlaceAlong(float pointer, float size, float area)
    {
        var position = pointer + k_PointerOffset;
        if (position + size > area - k_EdgeMargin)
            position = pointer - k_PointerOffset - size;
        return Mathf.Max(k_EdgeMargin, Mathf.Min(position, area - k_EdgeMargin - size));
    }

    bool TryBuild(LibraryItem item)
    {
        EnsurePanel();

        // What the previous item logs as it is removed is not this item's failure.
        using (new LogCapture())
            ClearContent();

        // Without a host panel, the sub-panel renders inside the capture, rather than in the next repaint of the
        // window, so what rendering throws is caught too.
        m_PanelElement.CursorHost = null;

        var failed = false;
        using (var log = new LogCapture())
        {
            try
            {
                ApplyCanvasSetup();
                failed = !TryClone(item);
                if (!failed)
                {
                    // The first update dispatches attach and geometry events, and runs what is scheduled without a
                    // delay, all of which may dirty the panel again.
                    m_PanelElement.FrameUpdate();
                    m_PanelElement.FrameUpdate();
                    failed = !TryMeasureContent(out var contentSize);
                    if (!failed)
                    {
                        m_Size = FitContent(contentSize);
                        m_PanelElement.ResizeRenderTexture(m_Size);
                        m_PanelElement.FrameUpdate();
                    }
                }
            }
            catch (Exception)
            {
                failed = true;
            }

            failed |= log.HasErrors;

            // Inside the capture: what failed may throw again as it is removed.
            if (failed)
                ClearContent();
        }

        m_PanelElement.CursorHost = this;
        if (failed)
            return false;

        style.width = m_Size.x;
        style.height = m_Size.y;
        style.backgroundImage = Background.FromRenderTexture(m_PanelElement.DisplayTexture);

        // Editor content may hold IMGUI, which draws only in the repaint of the window.
        if (m_PanelElement.ContextType == ContextType.Editor)
        {
            m_PanelElement.RequestRender();
            MarkDirtyRepaint();
        }

        return true;
    }

    bool TryClone(LibraryItem item)
    {
        VisualTreeAsset source;
        if (item.isAsset)
        {
            source = item.visualTreeAsset;
        }
        else
        {
            // Kept until the preview is cleared, as the clone may still read from it.
            m_GeneratedAsset = UIElementsProvider.CreatePreviewVisualTreeAsset(item);
            source = m_GeneratedAsset;
        }

        if (source == null)
            return false;

        source.CloneTree(m_Content);
        return m_Content.childCount > 0;
    }

    bool TryMeasureContent(out Vector2 size)
    {
        size = default;
        var subRoot = m_PanelElement.subRootVisualElement;
        var pixelsPerPoint = m_PanelElement.SubPanelPixelsPerPoint;
        if (subRoot == null || !(pixelsPerPoint > 0))
            return false;

        // What overflows the content, a document of a fixed size wider than the preview for one, counts too.
        var bounds = m_Content.worldBound;
        var hasBounds = true;
        VisualElementBoundsUtility.TryEncapsulateDescendants(m_Content, ref bounds, ref hasBounds);
        size = (bounds.max - subRoot.worldBound.position) / pixelsPerPoint;
        return !float.IsNaN(size.x) && !float.IsNaN(size.y) && size.x >= 1 && size.y >= 1;
    }

    // Scales the content down when it is larger than the preview, and centers it across. Returns the size of the
    // preview: its width, and the height of the scaled content, up to the maximum. Content still too large at the
    // smallest scale shows its top left corner.
    Vector2 FitContent(Vector2 contentSize)
    {
        var scale = Mathf.Min(1, k_MaxPreviewSize.x / contentSize.x, k_MaxPreviewSize.y / contentSize.y);
        scale = Mathf.Max(scale, k_MinContentScale);
        var shownSize = contentSize * scale;

        m_PanelElement.ScaleFactor = scale;
        m_PanelElement.Offset = new Vector2(Mathf.Max(0, (k_MaxPreviewSize.x - shownSize.x) / 2), 0);
        return new Vector2(k_MaxPreviewSize.x, Mathf.Ceil(Mathf.Min(shownSize.y, k_MaxPreviewSize.y)));
    }

    // The background of the canvas, drawn into the target first, as the canvas draws it behind the document.
    void OnBeforeRender()
    {
        var renderTexture = m_PanelElement?.RenderTexture;
        if (Canvas == null || renderTexture == null)
            return;

        var area = new Rect(0, 0, renderTexture.width, renderTexture.height);
        var settings = Canvas.GetBackdropSettings(area, m_PanelElement.SubPanelPixelsPerPoint);
        var texture = m_BackdropTexture.Update(renderTexture.width, renderTexture.height, settings);
        if (texture == null)
            return;

        // The backdrop is drawn in the project color space; an editor sub-panel target stores gamma-encoded colors.
        PanelElement.CopyStoredColors(texture, renderTexture);
        m_PanelElement.MarkTargetPrefilled();
    }

    // The canvas of the document the library adds to decides the panel the preview renders in.
    PanelElement GetCanvas()
    {
        var context = m_LibraryView.Context;
        return context is { IsDisposed: false } && context.Session is { IsDisposed: false } session
            ? session.PanelElement
            : null;
    }

    // Whether the preview was built in a panel set up like the canvas is now: the document, its panel settings or
    // the theme it is previewed with may have changed since.
    bool MatchesCanvasSetup()
    {
        var canvas = GetCanvas();
        return m_PanelElement != null &&
            m_PanelElement.ContextType == (canvas?.ContextType ?? ContextType.Player) &&
            m_PanelElement.PanelSettings == canvas?.PanelSettings &&
            m_PanelElement.Theme == canvas?.AppliedTheme &&
            m_PanelElement.Size == (canvas != null ? canvas.Size : k_DefaultCanvasSize);
    }

    void ApplyCanvasSetup()
    {
        var canvas = GetCanvas();
        m_PanelElement.ContextType = canvas?.ContextType ?? ContextType.Player;
        m_PanelElement.PanelSettings = canvas?.PanelSettings;
        m_PanelElement.Theme = canvas?.AppliedTheme;
        m_PanelElement.Size = canvas != null ? canvas.Size : k_DefaultCanvasSize;
        m_PanelElement.ScaleFactor = 1;
        m_PanelElement.Offset = Vector2.zero;
        m_PanelElement.SubPanelPixelsPerPoint = panel is Panel hostPanel ? hostPanel.pixelsPerPoint : 1;

        // The preview size is in points, the content is laid out in the units of the canvas.
        m_PanelElement.FrameUpdate();
        var unitsPerPoint = GetUnitsPerPoint();
        m_Content.style.width = k_LayoutWidth * unitsPerPoint;
        m_Content.style.minHeight = k_MinContentHeight * unitsPerPoint;
    }

    float GetUnitsPerPoint()
    {
        var subRoot = m_PanelElement.subRootVisualElement;
        var pixelsPerPoint = m_PanelElement.SubPanelPixelsPerPoint;
        var shownWidth = subRoot.worldBound.width / pixelsPerPoint;
        var unitsPerPoint = subRoot.layout.width / shownWidth;
        return float.IsNaN(unitsPerPoint) || float.IsInfinity(unitsPerPoint) || unitsPerPoint <= 0 ? 1 : unitsPerPoint;
    }

    void EnsurePanel()
    {
        if (m_PanelElement != null)
            return;

        m_Content = new VisualElement { name = "library-item-preview-content", pickingMode = PickingMode.Ignore };
        m_Content.style.position = Position.Absolute;
        m_Content.style.left = 0;
        m_Content.style.top = 0;

        // Never attached: the surface shows what it renders, as the canvas does.
        m_PanelElement = new PanelElement();
        m_PanelElement.CreateSubPanel();
        m_PanelElement.ForwardHierarchicalEvents = false;
        m_PanelElement.EnableAnimationSystem(false);
        m_PanelElement.SubPanelRecreated += OnSubPanelRecreated;
        m_PanelElement.OnBeforeRender += OnBeforeRender;
        OnSubPanelRecreated(m_PanelElement);
    }

    // A context type switch replaces the sub-panel, with an empty root.
    void OnSubPanelRecreated(PanelElement panelElement)
    {
        Binding.SetPanelLogLevel(panelElement.SubPanel, BindingLogLevel.None);
        panelElement.subRootVisualElement.Add(m_Content);
    }

    void ClearContent()
    {
        m_Content?.Clear();
        if (m_GeneratedAsset != null)
        {
            Object.DestroyImmediate(m_GeneratedAsset);
            m_GeneratedAsset = null;
        }
    }

    // Records the errors logged on the main thread while it is alive, instead of logging them. Other threads log as
    // usual.
    sealed class LogCapture : ILogHandler, IDisposable
    {
        readonly ILogger m_Logger;
        readonly ILogHandler m_Handler;
        readonly int m_ThreadId;

        public bool HasErrors { get; private set; }

        public LogCapture()
        {
            m_Logger = Debug.unityLogger;
            m_Handler = m_Logger.logHandler;
            m_ThreadId = Thread.CurrentThread.ManagedThreadId;
            m_Logger.logHandler = this;
        }

        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            if (Thread.CurrentThread.ManagedThreadId != m_ThreadId)
            {
                m_Handler.LogFormat(logType, context, format, args);
                return;
            }

            if (logType is LogType.Error or LogType.Assert or LogType.Exception)
                HasErrors = true;
        }

        public void LogException(Exception exception, Object context)
        {
            if (Thread.CurrentThread.ManagedThreadId != m_ThreadId)
            {
                m_Handler.LogException(exception, context);
                return;
            }

            HasErrors = true;
        }

        public void Dispose()
        {
            if (m_Logger.logHandler == this)
                m_Logger.logHandler = m_Handler;
        }
    }
}
