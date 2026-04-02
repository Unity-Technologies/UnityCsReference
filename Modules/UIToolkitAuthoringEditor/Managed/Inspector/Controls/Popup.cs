// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine.Bindings;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// This class represents a popup as a real window or an in-UI popup.
/// </summary>
[UxmlElement]
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
class Popup : VisualElement
{
    [UnityEngine.Internal.ExcludeFromDocs, Serializable]
    public new class UxmlSerializedData : VisualElement.UxmlSerializedData
    {
        [RegisterUxmlCache]
        [Conditional("UNITY_EDITOR")]
        public new static void Register()
        {
            UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), [
                new UxmlAttributeNames(nameof(UsesRealWindow), "uses-real-window")
            ], true);
        }

#pragma warning disable 649
        [SerializeField] bool UsesRealWindow;
        [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags UsesRealWindow_UxmlAttributeFlags;
#pragma warning restore 649

        public override object CreateInstance() => new Popup();

        public override void Deserialize(object obj)
        {
            base.Deserialize(obj);

            var e = (Popup)obj;

            if (ShouldWriteAttributeValue(UsesRealWindow_UxmlAttributeFlags))
                e.UsesRealWindow = UsesRealWindow;
        }
    }

    /// <summary>
    /// The USS class name of the popup. This class is added to the popup element and can be used to style the popup in
    /// USS. The real window variant of the popup also has a specific USS class name that can be used to style it
    /// differently from the in-UI variant.
    /// </summary>
    public const string UssClassName = "unity-popup";

    /// <summary>
    /// The USS class name added to the popup when it is displayed as a real window.
    /// This can be used to style the real window variant of the popup differently from the in-UI variant.
    /// </summary>
    public const string RealWindowUssClassName = UssClassName + "--real-window";

    /// <summary>
    /// Cached screen rect of the anchored element.
    /// This is used to position the popup when it is displayed as a real window.
    /// The anchored element should set this property before showing the popup to ensure that the popup is positioned
    /// correctly. If this property is not set, the popup will use the current screen rect of the anchored element,
    /// which may not be accurate if the anchored element has moved since the last time it was shown.
    /// </summary>
    public const string k_AnchoredElementCachedScreenRectVEPropertyName = "Popup_AnchoredElementCachedScreenRect";

    private const string k_StyleSheet = "UIToolkitAuthoring/Inspector/Controls/Popup.uss";
    private const string k_StyleSheetDark = "UIToolkitAuthoring/Inspector/Controls/PopupDark.uss";
    private const string k_StyleSheetLight = "UIToolkitAuthoring/Inspector/Controls/PopupLight.uss";

    const int k_PopupWindowMinWidth = 200;
    const int k_PopupWindowMaxWidth = 1000;
    const int k_PopupWindowMaxHeight = 1000;
    const int k_PopupElementMinWidth = 350;

    VisualElement m_AnchoredElement;
    Rect m_AnchoredElementScreenPos;

    bool m_UsesRealWindow;
    PopupWindow m_Window;

    /// <summary>
    /// Indicates whether the popup is currently opened. For a real window, this means that the window is not null.
    /// For an in-UI popup, this means that the display style is not none.
    /// </summary>
    public bool IsOpened => m_UsesRealWindow ? m_Window != null : resolvedStyle.display == DisplayStyle.Flex;

    /// <summary>
    /// The UI element to which the popup is anchored. The popup will be displayed next to this element. If null, the
    /// popup will be displayed at the mouse position.
    /// </summary>
    public VisualElement AnchoredElement
    {
        get => m_AnchoredElement;
        set
        {
            if (m_AnchoredElement == value)
                return;

            if (m_AnchoredElement != null)
            {
                m_AnchoredElement.UnregisterCallback<GeometryChangedEvent>(OnAnchoredElementGeometryChanged);
                m_AnchoredElement.UnregisterCallback<DetachFromPanelEvent>(OnAnchoredElementDetachedFromPath);
            }

            m_AnchoredElement = value;

            if (m_AnchoredElement != null)
            {
                m_AnchoredElement.RegisterCallback<GeometryChangedEvent>(OnAnchoredElementGeometryChanged);
                m_AnchoredElement.RegisterCallback<DetachFromPanelEvent>(OnAnchoredElementDetachedFromPath);
            }
        }
    }

    /// <summary>
    /// Indicates whether the popup should be displayed as a real window or as an in-UI popup. If true, the popup will
    /// be displayed as a real window.If false, the popup will be displayed as an in-UI popup. The default value is
    /// false.
    /// </summary>
    public bool UsesRealWindow
    {
        get => m_UsesRealWindow;
        set
        {
            if (m_UsesRealWindow == value)
                return;

            m_UsesRealWindow = value;
            EnableInClassList(RealWindowUssClassName, value);
        }
    }

    /// <summary>
    /// The real window used to display the popup. This will be null if the popup is displayed as an in-UI popup.
    /// </summary>
    public EditorWindow Window => m_Window;

    /// <summary>
    /// Sent when the popup is shown.
    /// </summary>
    public event Action OnShow;

    /// <summary>
    /// Sent when the popup is hidden.
    /// </summary>
    public event Action OnHide;

    /// <summary>
    /// Constructs a new instance of the Popup class. The popup is hidden by default and will be displayed when
    /// the Show method is called.
    /// </summary>
    public Popup()
    {
        AddToClassList(UssClassName);

        // Popup is hidden by default
        style.display = DisplayStyle.None;

        RegisterCallback<GeometryChangedEvent>(_ => EnsureVisibilityInParent());

        // Prevent PointerDownEvent on a child from switching focus.
        RegisterCallback<PointerDownEvent>(e => focusController.IgnoreEvent(e), TrickleDown.TrickleDown);

        // Load assets.
        var mainUSS = EditorGUIUtility.Load(k_StyleSheet) as StyleSheet;
        var themeUSSPath = EditorGUIUtility.isProSkin ? k_StyleSheetDark : k_StyleSheetLight;
        var themeUSS = EditorGUIUtility.Load(themeUSSPath) as StyleSheet;

        styleSheets.Add(mainUSS);
        styleSheets.Add(themeUSS);
    }

    /// <summary>
    /// Shows the popup. If the popup is anchored to a UI element, it will be displayed next to that element.
    /// If the popup is not anchored, it will be displayed at the mouse position. When showing the popup, it will
    /// adjust its position to ensure that it is fully visible within its parent.
    /// If the popup is displayed as a real window, it will also adjust its position when the geometry of the anchored
    /// element changes to ensure that it stays next to the anchored element.
    /// </summary>
    public virtual void Show()
    {
        if (m_AnchoredElement == null)
        {
            var mousePos = Event.current != null ? GUIUtility.GUIToScreenPoint(Event.current.mousePosition) : Vector2.zero;
            m_AnchoredElementScreenPos = new Rect(mousePos, Vector2.zero);
        }
        else
        {
            if (m_AnchoredElement.HasProperty(k_AnchoredElementCachedScreenRectVEPropertyName))
            {
                m_AnchoredElementScreenPos =
                    (Rect)m_AnchoredElement.GetProperty(k_AnchoredElementCachedScreenRectVEPropertyName);
            }
            else
            {
                m_AnchoredElementScreenPos = GUIUtility.GUIToScreenRect(m_AnchoredElement.worldBound);
            }
        }

        OnShow?.Invoke();
        m_Window?.Close();

        style.display = DisplayStyle.Flex;

        // Create a new window
        if (UsesRealWindow)
        {
            m_Window = ScriptableObject.CreateInstance<PopupWindow>();
            var windowWidth = Mathf.Max(k_PopupWindowMinWidth, m_AnchoredElement != null ? m_AnchoredElement.worldBound.width : 0);
            const float windowHeight = 30;

            m_Window.ShowAsDropDown(m_AnchoredElementScreenPos, new Vector2(windowWidth, windowHeight), null, ShowMode.PopupMenu, false);

            // Reset the min and max size of the window
            m_Window.minSize = Vector2.zero;
            m_Window.maxSize = new Vector2(k_PopupWindowMaxWidth, k_PopupWindowMaxHeight);
            m_Window.minSize = new Vector2(k_PopupWindowMinWidth, 0);
            m_Window.Content = this;
            m_Window.Closed += () => Hide(true);
        }
        else
        {
            VisualElement root = null;

            if (m_AnchoredElement != null)
            {
                root = m_AnchoredElement?.GetRootVisualContainer();
            }
            else
            {
                root = GetRootVisualContainer();
            }

            root?.Add(this);
            style.position = Position.Absolute;
            AdjustGeometry();
        }
    }

    /// <summary>
    /// Hides the popup. If the popup is displayed as a real window, it will be closed.
    /// If the popup is displayed as an in-UI popup, it will be removed from its parent.
    /// </summary>
    public virtual void Hide()
    {
        if (!IsOpened)
            return;
        Hide(false);
    }

    void Hide(bool closingWindow)
    {
        if (UsesRealWindow)
        {
            if (closingWindow)
            {
                RemoveFromHierarchy();
                OnHide?.Invoke();
                m_Window = null;
            }
            else
            {
                m_Window.Close();
            }
        }
        else
        {
            style.display = DisplayStyle.None;
            OnHide?.Invoke();
        }
    }

    void OnAnchoredElementDetachedFromPath(DetachFromPanelEvent e)
    {
        Hide();
    }

    void OnAnchoredElementGeometryChanged(GeometryChangedEvent e)
    {
        AdjustGeometry();
    }

    /// <summary>
    /// Adjusts the geometry of the popup to ensure that it is displayed next to the anchored element and that it is
    /// fully visible within its parent. This method is called when the geometry of the anchored element changes and
    /// when the popup is shown. If the popup is displayed as a real window, it will also adjust its position to ensure
    /// that it stays next to the anchored element when the geometry of the anchored element changes.
    /// </summary>
    public virtual void AdjustGeometry()
    {
        if (m_AnchoredElement is { visible: true } && parent != null)
        {
            if (UsesRealWindow)
            {
                if (m_Window != null && !float.IsNaN(layout.width) && !float.IsNaN(layout.height))
                {
                    var pos = m_AnchoredElementScreenPos;

                    pos.y += m_AnchoredElement.layout.height;

                    var h = resolvedStyle.height;
                    var size = m_Window.position.size;

                    size.y = h;
                    m_Window.position = new Rect(pos.position, size);
                }
            }
            else
            {
                var pos = m_AnchoredElement.ChangeCoordinatesTo(parent, Vector2.zero);

                style.left = pos.x;
                style.top = pos.y + m_AnchoredElement.layout.height;
                style.width = Math.Max(k_PopupElementMinWidth, m_AnchoredElement.resolvedStyle.width);
            }
        }
    }

    /// <summary>
    /// Gets the adjusted position of the popup to ensure that it is fully visible within its parent.
    /// If the popup is anchored to a UI element, it will also ensure that the popup is displayed next to the anchored
    /// element. This method is used when the popup is displayed as an in-UI popup to adjust its position to ensure
    /// that it is fully visible within its parent. If the popup is not anchored, it will simply ensure that the popup
    /// is fully visible within its parent.
    /// </summary>
    /// <returns>The adjusted position of the popup.</returns>
    protected virtual Vector2 GetAdjustedPosition()
    {
        if (m_AnchoredElement == null)
        {
            return new Vector2(Mathf.Min(style.left.value.value, parent.layout.width - resolvedStyle.width),
                Mathf.Min(style.top.value.value, parent.layout.height - resolvedStyle.height));
        }

        var currentPos = new Vector2(style.left.value.value, style.top.value.value);
        var newPos = new Vector2(Mathf.Min(currentPos.x, parent.layout.width - resolvedStyle.width), currentPos.y);
        var fieldTopLeft = m_AnchoredElement.ChangeCoordinatesTo(parent, Vector2.zero);
        var fieldBottom = fieldTopLeft.y + m_AnchoredElement.layout.height;
        const float tolerance = 2f;

        newPos.y = (fieldBottom < parent.layout.height / 2) ? (currentPos.y) : (fieldTopLeft.y - resolvedStyle.height);

        if (Math.Abs(newPos.x - currentPos.x) > tolerance || Math.Abs(newPos.y - currentPos.y) > tolerance)
            return newPos;
        return currentPos;
    }

    void EnsureVisibilityInParent()
    {
        if (parent != null && !float.IsNaN(layout.width) && !float.IsNaN(layout.height) && !UsesRealWindow)
        {
            var pos = GetAdjustedPosition();

            style.left = pos.x;
            style.top = pos.y;
        }
    }
}
