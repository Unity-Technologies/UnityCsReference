// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor;

/// <summary>
/// The view for the <see cref="PortPreviewModel"/>, which is a marker that displays the value of a port for debugging purposes.
/// </summary>
class PortPreview : Marker
{
    static readonly CustomStyleProperty<int> k_HorizontalPaddingProperty = new("--horizontal-padding");
    static readonly CustomStyleProperty<int> k_VerticalPaddingProperty = new("--vertical-padding");
    static readonly CustomStyleProperty<int> k_BorderRadiusProperty = new("--border-radius");
    static readonly CustomStyleProperty<Color> k_BackgroundColorProperty = new("--background-color");
    static readonly CustomStyleProperty<Color> k_BorderColorProperty = new("--border-color");

    /// <summary>
    /// The name of the port preview element.
    /// </summary>
    public static readonly string portPreviewName = "port-preview";

    /// <summary>
    /// The USS class name of the port preview element.
    /// </summary>
    public new static readonly string ussClassName = "ge-" + portPreviewName;

    /// <summary>
    /// The USS class name of the icon in the port preview.
    /// </summary>
    public static readonly string iconUssClassName = ussClassName.WithUssElement(GraphElementHelper.iconName);

    /// <summary>
    /// The USS class name of the label in the port preview.
    /// </summary>
    public static readonly string labelUssClassName = ussClassName.WithUssElement(GraphElementHelper.labelName);

    /// <summary>
    /// The USS class name used to hide the port preview element when the port preview should not be shown based on the port model or when the target port is not visible.
    /// </summary>
    public new static readonly string hiddenUssClassName = GraphElementHelper.hiddenUssModifier;

    const string k_StylePath = "PortPreview.uss";
    const float k_PointerLength = 6f;
    const float k_PointerEdgeMargin = 4f;

    PortModel PortModel => ParentModel as PortModel;
    PortPreviewModel PortPreviewModel => Model as PortPreviewModel;

    Image m_DataTypeIcon;
    Label m_PortPreviewLabel;
    TypeHandle m_CurrentPortDataTypeHandle;
    string m_CurrentLabel;
    bool m_IconIsInline;
    bool m_PreviewVisualContentAlreadyDrawn;
    Vector2 m_Padding;
    int m_BorderRadius;
    Color m_BackgroundColor;
    Color m_BorderColor;

    internal string CurrentLabel => m_CurrentLabel;

    /// <inheritdoc />
    public override GraphElementModel ParentModel => PortPreviewModel?.PortModel;

    /// <inheritdoc />
    protected override void BuildUI()
    {
        if (PortModel == null || PortPreviewModel == null)
            return;

        name = portPreviewName;

        m_DataTypeIcon = new Image { name = GraphElementHelper.iconName };
        m_DataTypeIcon.AddToClassList(iconUssClassName);
        Add(m_DataTypeIcon);

        m_PortPreviewLabel = new Label();
        m_PortPreviewLabel.AddToClassList(labelUssClassName);
        Add(m_PortPreviewLabel);

        // By default, we hide the port preview until we know whether it should be shown or not based on the port model.
        EnableInClassList(hiddenUssClassName, true);

        // Set the initial label and icon based on the port and preview model.
        UpdateDataTypeIcon();
        m_CurrentPortDataTypeHandle = PortModel.DataTypeHandle;
    }

    protected override void OnCustomStyleResolved(CustomStyleResolvedEvent e)
    {
        base.OnCustomStyleResolved(e);

        if (e.customStyle.TryGetValue(k_HorizontalPaddingProperty, out var paddingX) && e.customStyle.TryGetValue(k_VerticalPaddingProperty, out var paddingY))
            m_Padding = new Vector2(paddingX, paddingY);

        if (e.customStyle.TryGetValue(k_BackgroundColorProperty, out var backgroundColor))
            m_BackgroundColor = backgroundColor;

        if (e.customStyle.TryGetValue(k_BorderRadiusProperty, out var borderRadius))
            m_BorderRadius = borderRadius;

        if (e.customStyle.TryGetValue(k_BorderColorProperty, out var borderColor))
            m_BorderColor = borderColor;
    }

    /// <inheritdoc />
    protected override void PostBuildUI()
    {
        base.PostBuildUI();

        AddToClassList(ussClassName);
        this.AddPackageStylesheet(k_StylePath);
    }

    /// <inheritdoc />
    protected override void Attach()
    {
        var port = PortModel?.GetView<Port>(GraphView);
        if (port == null)
            return;

        var target = (port.PartList.GetPart(Port.connectorPartName) as PortConnectorPart)?.CreateFromPortHitBoxElement ?? port;

        if (PortModel.Orientation == PortOrientation.Horizontal)
        {
            AttachTo(target,
                PortModel.Direction == PortDirection.Input
                    ? SpriteAlignment.LeftCenter
                    : SpriteAlignment.RightCenter);
        }
        else
        {
            AttachTo(target,
                PortModel.Direction == PortDirection.Input
                    ? SpriteAlignment.TopCenter
                    : SpriteAlignment.BottomCenter);
        }
    }

    /// <inheritdoc />
    protected override DynamicBorder CreateDynamicBorder()
    {
        // Port previews don't have borders in phase 1 of the design, so we return null here.
        return null;
    }

    /// <summary>
    /// Sets the string value to display in the port preview and update the visual content. If the string value is null, the port preview will be hidden.
    /// </summary>
    /// <param name="stringValue">The string value.</param>
    /// <param name="show">Whether to show the preview or hide it.</param>
    public void Set(string stringValue, bool show)
    {
        if (stringValue == null)
        {
            // If the string is null, we hide the port preview.
            EnableInClassList(hiddenUssClassName, true);
            return;
        }

        m_PortPreviewLabel.text = stringValue;
        m_CurrentLabel = stringValue;
        tooltip = PortModel.Title + " port data: " + stringValue;

        EnableInClassList(hiddenUssClassName, !show);

        // Regenerate the visual content to update the preview.
        MarkDirtyRepaint();
    }

    public void Show(bool show)
    {
        EnableInClassList(hiddenUssClassName, !show);
        MarkDirtyRepaint();
    }

    /// <inheritdoc />
    public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
    {
        base.UpdateUIFromModel(visitor);

        if (PortModel == null || PortPreviewModel == null)
            return;

        var port = PortModel.GetView<Port>(GraphView);
        if (port != null)
        {
            // The port preview is only shown if the target port is visible, its display style is not set to none, and the port model indicates that the port preview should be shown.
            var shouldHide = !port.visible || port.resolvedStyle.display == DisplayStyle.None || PortModel.Options.HasFlag(PortModelOptions.Hidden);
            EnableInClassList(hiddenUssClassName, shouldHide);
        }

        if (visitor.ChangeHints.Contains(ChangeHint.Data) && m_CurrentPortDataTypeHandle != PortModel.DataTypeHandle)
        {
            // Update the icon and its tint color based on the port data type.
            UpdateDataTypeIcon();
            m_CurrentPortDataTypeHandle = PortModel.DataTypeHandle;
        }
    }

    void UpdateDataTypeIcon()
    {
        if (m_DataTypeIcon == null)
            return;

        RootView.TypeHandleInfos.RemoveUssClasses(GraphElementHelper.iconDataTypeClassPrefix, m_DataTypeIcon, m_CurrentPortDataTypeHandle);

        // Use registered style for the type if any.
        bool overrideIcon = true;
        var newPortDataType = PortModel.PortDataType;
        var typeStyle = PortModel.GraphModel?.GetDataTypeStyle(newPortDataType);

        if (!typeStyle.HasValue && PortModel.PortDataType.IsListOrArray())
        {
            typeStyle = PortModel.GraphModel?.GetDataTypeStyle(newPortDataType.GetCollectionElementType());
            overrideIcon = false;
        }

        if (typeStyle.HasValue)
        {
            if (!overrideIcon)
                CreateNewImageWhenIconIsInline();

            m_DataTypeIcon.tintColor = typeStyle.Value.color;

            if (overrideIcon)
            {
                m_IconIsInline = true;
                m_DataTypeIcon.image = typeStyle.Value.icon;
            }
        }
        else
        {
            // If the icon was previously set inline by a registered type style, we need to remove it and create a new Image so that Image.m_TintColorIsInline and Image.m_ImageIsInline are reset to enable USS styling.
            CreateNewImageWhenIconIsInline();
        }

        RootView.TypeHandleInfos.AddUssClasses(GraphElementHelper.iconDataTypeClassPrefix, m_DataTypeIcon, PortModel.DataTypeHandle);
        return;

        void CreateNewImageWhenIconIsInline()
        {
            if (!m_IconIsInline)
                return;

            // Remove old icon
            var index = IndexOf(m_DataTypeIcon);
            Remove(m_DataTypeIcon);

            // Create and assign new icon
            m_DataTypeIcon = new Image();
            m_DataTypeIcon.AddToClassList(iconUssClassName);
            Insert(index, m_DataTypeIcon);
            m_IconIsInline = false;
        }
    }

    protected override void OnGeometryChanged(GeometryChangedEvent evt)
    {
        base.OnGeometryChanged(evt);
        if (m_PreviewVisualContentAlreadyDrawn || Attacher == null)
            return;

        generateVisualContent = OnGeneratePortPreviewVisualContent;
        m_PreviewVisualContentAlreadyDrawn = true;
    }

    enum PointerEdge
    {
        None,
        Left,
        Top,
        Right,
        Bottom
    }

    void OnGeneratePortPreviewVisualContent(MeshGenerationContext mgc)
    {
        var targetPort = PortModel?.GetView<Port>(GraphView);
        if (targetPort == null)
            return;

        // Side to draw the pointer
        var side = GetClosestSide((targetPort.worldBound.center - worldBound.center).normalized);

        var containerRect = new Rect(
            -m_Padding.x,
            -m_Padding.y,
            localBound.width + m_Padding.x * 2f,
            localBound.height + m_Padding.y * 2f);

        var painter = mgc.painter2D;
        painter.fillColor = m_BackgroundColor;
        painter.strokeColor = m_BorderColor;

        DrawRoundedRectWithPointer(painter, containerRect, m_BorderRadius, side, k_PointerLength, k_PointerEdgeMargin);
        painter.Fill();
        painter.Stroke();
    }

    static void DrawRoundedRectWithPointer(Painter2D painter, Rect bounds, float cornerRadius, SpriteAlignment side, float pointerLength, float pointerEdgeMargin)
    {
        var maxRadius = Mathf.Min(bounds.width, bounds.height) * 0.5f;
        var radius = Mathf.Clamp(cornerRadius, 0f, maxRadius);
        if (radius <= 0f)
            return;

        var pointerHalfBase = GetPointerHalfBase(bounds, side, radius, pointerLength, pointerEdgeMargin);
        var pointerEdge = pointerHalfBase > 0f ? ToPointerEdge(side) : PointerEdge.None;

        var xMin = bounds.xMin;
        var xMax = bounds.xMax;
        var yMin = bounds.yMin;
        var yMax = bounds.yMax;
        var cx = bounds.center.x;
        var cy = bounds.center.y;

        painter.BeginPath();
        painter.MoveTo(new Vector2(xMin, yMax - radius));

        // The edges are drawn in clockwise order starting from the bottom left corner.
        // For each edge, if the pointer should be drawn on that edge, we draw the pointer before continuing to draw the edge.
        DrawEdgeWithOptionalPointer(
            painter,
            pointerEdge,
            PointerEdge.Left,
            new Vector2(xMin, cy),
            tangent: new Vector2(0f, -1f),
            outwardNormal: new Vector2(-1f, 0f),
            pointerHalfBase,
            pointerLength,
            edgeEnd: new Vector2(xMin, yMin + radius));
        painter.ArcTo(new Vector2(xMin, yMin), new Vector2(xMin + radius, yMin), radius);

        DrawEdgeWithOptionalPointer(
            painter,
            pointerEdge,
            PointerEdge.Top,
            new Vector2(cx, yMin),
            tangent: new Vector2(1f, 0f),
            outwardNormal: new Vector2(0f, -1f),
            pointerHalfBase,
            pointerLength,
            edgeEnd: new Vector2(xMax - radius, yMin));
        painter.ArcTo(new Vector2(xMax, yMin), new Vector2(xMax, yMin + radius), radius);

        DrawEdgeWithOptionalPointer(
            painter,
            pointerEdge,
            PointerEdge.Right,
            new Vector2(xMax, cy),
            tangent: new Vector2(0f, 1f),
            outwardNormal: new Vector2(1f, 0f),
            pointerHalfBase,
            pointerLength,
            edgeEnd: new Vector2(xMax, yMax - radius));
        painter.ArcTo(new Vector2(xMax, yMax), new Vector2(xMax - radius, yMax), radius);

        DrawEdgeWithOptionalPointer(
            painter,
            pointerEdge,
            PointerEdge.Bottom,
            new Vector2(cx, yMax),
            tangent: new Vector2(-1f, 0f),
            outwardNormal: new Vector2(0f, 1f),
            pointerHalfBase,
            pointerLength,
            edgeEnd: new Vector2(xMin + radius, yMax));
        painter.ArcTo(new Vector2(xMin, yMax), new Vector2(xMin, yMax - radius), radius);

        painter.ClosePath();
    }

    static PointerEdge ToPointerEdge(SpriteAlignment side)
    {
        return side switch
        {
            SpriteAlignment.LeftCenter => PointerEdge.Left,
            SpriteAlignment.TopCenter => PointerEdge.Top,
            SpriteAlignment.RightCenter => PointerEdge.Right,
            SpriteAlignment.BottomCenter => PointerEdge.Bottom,
            _ => PointerEdge.None
        };
    }

    static void DrawEdgeWithOptionalPointer(
        Painter2D painter,
        PointerEdge pointerEdge,
        PointerEdge currentEdge,
        Vector2 center,
        Vector2 tangent,
        Vector2 outwardNormal,
        float pointerHalfBase,
        float pointerLength,
        Vector2 edgeEnd)
    {
        if (pointerEdge == currentEdge)
        {
            painter.LineTo(center - (tangent * pointerHalfBase));
            painter.LineTo(center + (outwardNormal * pointerLength));
            painter.LineTo(center + (tangent * pointerHalfBase));
        }

        painter.LineTo(edgeEnd);
    }

    static float GetPointerHalfBase(Rect bounds, SpriteAlignment side, float radius, float pointerLength, float pointerEdgeMargin)
    {
        if (pointerLength <= 0f)
            return 0f;

        var sideSpan = side is SpriteAlignment.LeftCenter or SpriteAlignment.RightCenter ? bounds.height : bounds.width;
        var maxHalfBase = (sideSpan - 2f * radius) * 0.5f - pointerEdgeMargin;
        return Mathf.Max(0f, Mathf.Min(pointerLength, maxHalfBase));
    }

    static SpriteAlignment GetClosestSide(Vector2 direction)
    {
        if (direction.sqrMagnitude <= Mathf.Epsilon)
            return SpriteAlignment.RightCenter;

        var absX = Mathf.Abs(direction.x);
        var absY = Mathf.Abs(direction.y);

        if (absX >= absY)
            return direction.x >= 0f ? SpriteAlignment.RightCenter : SpriteAlignment.LeftCenter;

        return direction.y >= 0f ? SpriteAlignment.BottomCenter : SpriteAlignment.TopCenter;
    }
}
