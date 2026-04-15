// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[UxmlElement(visibility = LibraryVisibility.Hidden)]
sealed partial class UICanvasResizerHandle : VisualElement
{
    [Serializable]
    public new class UxmlSerializedData : VisualElement.UxmlSerializedData
    {
        [Conditional("UNITY_EDITOR")]
        public new static void Register()
        {
            UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), [
                new UxmlAttributeNames(nameof(Position), "position")
            ], true);
        }

#pragma warning disable 649
        [SerializeField] Anchor Position;
        [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags Position_UxmlAttributeFlags;
#pragma warning restore 649


        public override object CreateInstance() => new UICanvasResizerHandle();

        public override void Deserialize(object obj)
        {
            base.Deserialize(obj);
            var element = (UICanvasResizerHandle)obj;
            if (ShouldWriteAttributeValue(Position_UxmlAttributeFlags))
                element.Position = Position;
        }
    }

    public const string UssClass = "unity-ui-viewport__canvas-resizer";

    public enum Anchor
    {
        Top,
        TopRight,
        Right,
        BottomRight,
        Bottom,
        BottomLeft,
        Left,
        TopLeft
    }

    Anchor m_Position;

    [UxmlAttribute]
    public Anchor Position
    {
        get => m_Position;
        set
        {
            if (m_Position == value)
                return;

            SetClassFromPosition(m_Position, false);
            m_Position = value;
            SetClassFromPosition(m_Position, true);
        }
    }

    public UICanvasResizerHandle()
    {
        pickingMode = PickingMode.Position;
        AddToClassList(UssClass);
        SetClassFromPosition(m_Position, true);
        generateVisualContent += DrawResizer;
    }

    protected override void HandleEventBubbleUp(EventBase evt)
    {
        switch (evt)
        {
            case AttachToPanelEvent attachToPanelEvent:
                RegisterCallback<PointerEnterEvent, UICanvasResizerHandle>(OnPointerEnter, this);
                RegisterCallback<PointerLeaveEvent, UICanvasResizerHandle>(OnPointerLeave, this);
                break;
            case DetachFromPanelEvent detachFromPanelEvent:
                UnregisterCallback<PointerEnterEvent, UICanvasResizerHandle>(OnPointerEnter);
                UnregisterCallback<PointerLeaveEvent, UICanvasResizerHandle>(OnPointerLeave);
                break;
        }
        base.HandleEventBubbleUp(evt);
    }

    static void OnPointerLeave(PointerLeaveEvent evt, UICanvasResizerHandle self)
    {
        self.MarkDirtyRepaint();
    }

    static void OnPointerEnter(PointerEnterEvent evt, UICanvasResizerHandle self)
    {
        self.MarkDirtyRepaint();
    }

    void SetClassFromPosition(Anchor position, bool enabled)
    {
        switch (position)
        {
            case Anchor.Top:
                EnableInClassList(UssClass + "--top", enabled);
                EnableInClassList(UssClass + "--horizontal", enabled);
                break;
            case Anchor.TopRight:
                EnableInClassList(UssClass + "--top-right", enabled);
                EnableInClassList(UssClass + "--corner", enabled);
                break;
            case Anchor.Right:
                EnableInClassList(UssClass + "--right", enabled);
                EnableInClassList(UssClass + "--vertical", enabled);
                break;
            case Anchor.BottomRight:
                EnableInClassList(UssClass + "--bottom-right", enabled);
                EnableInClassList(UssClass + "--corner", enabled);
                break;
            case Anchor.Bottom:
                EnableInClassList(UssClass + "--bottom", enabled);
                EnableInClassList(UssClass + "--horizontal", enabled);
                break;
            case Anchor.BottomLeft:
                EnableInClassList(UssClass + "--bottom-left", enabled);
                EnableInClassList(UssClass + "--corner", enabled);
                break;
            case Anchor.Left:
                EnableInClassList(UssClass + "--left", enabled);
                EnableInClassList(UssClass + "--vertical", enabled);
                break;
            case Anchor.TopLeft:
                EnableInClassList(UssClass + "--top-left", enabled);
                EnableInClassList(UssClass + "--corner", enabled);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(position), position, null);
        }
    }

    void DrawResizer(MeshGenerationContext ctx)
    {
        var painter = ctx.painter2D;
        if (!hasHoverPseudoState)
            return;

        painter.lineWidth = 4;
        painter.strokeColor = Color.white;
        painter.fillColor = Color.white;
        const float cornerSize = 12;

        painter.BeginPath();
        switch (Position)
        {
            case Anchor.Top:
                painter.MoveTo(new Vector2(contentRect.xMin, contentRect.yMax - 2));
                painter.LineTo(new Vector2(contentRect.xMax, contentRect.yMax - 2));
                break;
            case Anchor.TopRight:
                painter.MoveTo(new Vector2(contentRect.xMin, contentRect.yMax));
                painter.LineTo(new Vector2(contentRect.xMin, contentRect.yMax - cornerSize));
                painter.LineTo(new Vector2(contentRect.xMin + cornerSize, contentRect.yMax - cornerSize));
                painter.LineTo(new Vector2(contentRect.xMin + cornerSize, contentRect.yMax));
                painter.LineTo(new Vector2(contentRect.xMin, contentRect.yMax));
                break;
            case Anchor.Right:
                painter.MoveTo(new Vector2(contentRect.xMin + 2, contentRect.yMin));
                painter.LineTo(new Vector2(contentRect.xMin + 2, contentRect.yMax));
                break;
            case Anchor.BottomRight:
                painter.MoveTo(new Vector2(contentRect.xMin, contentRect.yMin));
                painter.LineTo(new Vector2(contentRect.xMin + cornerSize, contentRect.yMin));
                painter.LineTo(new Vector2(contentRect.xMin + cornerSize, contentRect.yMin + cornerSize));
                painter.LineTo(new Vector2(contentRect.xMin, contentRect.yMin + cornerSize));
                painter.LineTo(new Vector2(contentRect.xMin, contentRect.yMin));
                break;
            case Anchor.Bottom:
                painter.MoveTo(new Vector2(contentRect.xMin, contentRect.yMin + 2));
                painter.LineTo(new Vector2(contentRect.xMax, contentRect.yMin + 2));
                break;
            case Anchor.BottomLeft:
                painter.MoveTo(new Vector2(contentRect.xMax, contentRect.yMin));
                painter.LineTo(new Vector2(contentRect.xMax, contentRect.yMin + cornerSize));
                painter.LineTo(new Vector2(contentRect.xMax - cornerSize, contentRect.yMin + cornerSize));
                painter.LineTo(new Vector2(contentRect.xMax - cornerSize, contentRect.yMin));
                painter.LineTo(new Vector2(contentRect.xMax, contentRect.yMin));
                break;
            case Anchor.Left:
                painter.MoveTo(new Vector2(contentRect.xMax - 2, contentRect.yMin));
                painter.LineTo(new Vector2(contentRect.xMax - 2, contentRect.yMax));
                break;
            case Anchor.TopLeft:
                painter.MoveTo(new Vector2(contentRect.xMax, contentRect.yMax));
                painter.LineTo(new Vector2(contentRect.xMax - cornerSize, contentRect.yMax));
                painter.LineTo(new Vector2(contentRect.xMax - cornerSize, contentRect.yMax - cornerSize));
                painter.LineTo(new Vector2(contentRect.xMax, contentRect.yMax - cornerSize));
                painter.LineTo(new Vector2(contentRect.xMax, contentRect.yMax));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        switch (Position)
        {
            case Anchor.Top:
            case Anchor.Right:
            case Anchor.Bottom:
            case Anchor.Left:
                painter.Stroke();
                break;
            case Anchor.TopRight:
            case Anchor.BottomRight:
            case Anchor.BottomLeft:
            case Anchor.TopLeft:
                painter.Fill();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

    }
}
