// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor;

abstract class VisualElementManipulator : VisualElement
{
    [Flags]
    protected enum TrackedStyles
    {
        None = 0,
        Width = 1,
        Height = 2,
        Left = 4,
        Top = 8,
        Right = 16,
        Bottom = 32,
    }

    static readonly TrackedStyles[] s_TrackedStyleValues =
    {
        TrackedStyles.Width, TrackedStyles.Height, TrackedStyles.Left, TrackedStyles.Top, TrackedStyles.Right,
        TrackedStyles.Bottom,
    };

    protected VisualElement Target { get; private set; }
    protected TrackedStyles BoundStyles { get; set; }

    public bool IsReadOnly { get; set; }

    public float ZoomScale { get; set; } = 1f;

    public event Action<string> MessageNotified;

    protected void NotifyMessage(string message) => MessageNotified?.Invoke(message);

    public virtual void Activate(VisualElement element, bool readOnly = false)
    {
        Target = element;
        IsReadOnly = readOnly;
        OnActivated();
    }

    public virtual void Deactivate()
    {
        Target = null;
        IsReadOnly = false;
        OnDeactivated();
    }

    // Called by VisualElementManipulatorOverlay after layout update.
    public void UpdateStyles()
    {
        UpdateBoundStyles();
        SetStylesFromTargetStyles();
    }

    protected virtual void OnActivated() { }
    protected virtual void OnDeactivated() { }
    protected virtual void SetStylesFromTargetStyles() { }

    protected virtual void UpdateBoundStyles()
    {
        BoundStyles = TrackedStyles.None;
        if (Target == null) return;
        foreach (var style in s_TrackedStyleValues)
        {
            if (DataBindingUtility.TryGetLastUIBindingResult(
                    new BindingId(GetStylePropertyPath(style)), Target, out var result)
                && result.status == BindingStatus.Success)
                BoundStyles |= style;
        }
    }

    protected bool AreStylesBound(TrackedStyles styles) => (BoundStyles & styles) != 0;

    protected static string GetStylePropertyPath(TrackedStyles style) => style switch
    {
        TrackedStyles.Width => "style.width",
        TrackedStyles.Height => "style.height",
        TrackedStyles.Left => "style.left",
        TrackedStyles.Top => "style.top",
        TrackedStyles.Right => "style.right",
        TrackedStyles.Bottom => "style.bottom",
        _ => throw new ArgumentOutOfRangeException(nameof(style)),
    };

    // --- TrackedStyles helpers ---

    protected static StylePropertyId GetStylePropertyId(TrackedStyles style) => style switch
    {
        TrackedStyles.Width => StylePropertyId.Width,
        TrackedStyles.Height => StylePropertyId.Height,
        TrackedStyles.Left => StylePropertyId.Left,
        TrackedStyles.Top => StylePropertyId.Top,
        TrackedStyles.Right => StylePropertyId.Right,
        TrackedStyles.Bottom => StylePropertyId.Bottom,
        _ => throw new ArgumentOutOfRangeException(nameof(style)),
    };

    protected static TrackedStyles GetOppositeStyle(TrackedStyles style) => style switch
    {
        TrackedStyles.Width => TrackedStyles.Height,
        TrackedStyles.Height => TrackedStyles.Width,
        TrackedStyles.Left => TrackedStyles.Right,
        TrackedStyles.Top => TrackedStyles.Bottom,
        TrackedStyles.Right => TrackedStyles.Left,
        TrackedStyles.Bottom => TrackedStyles.Top,
        _ => throw new ArgumentOutOfRangeException(nameof(style)),
    };

    protected static TrackedStyles GetLengthStyle(TrackedStyles style) => style switch
    {
        TrackedStyles.Width => TrackedStyles.Width,
        TrackedStyles.Height => TrackedStyles.Height,
        TrackedStyles.Left => TrackedStyles.Width,
        TrackedStyles.Top => TrackedStyles.Height,
        TrackedStyles.Right => TrackedStyles.Width,
        TrackedStyles.Bottom => TrackedStyles.Height,
        _ => throw new ArgumentOutOfRangeException(nameof(style)),
    };

    protected bool IsNoneOrAuto(TrackedStyles style)
    {
        if (Target == null) return false;
        return style switch
        {
            TrackedStyles.Width => IsLengthNoneOrAuto(Target.computedStyle.width),
            TrackedStyles.Height => IsLengthNoneOrAuto(Target.computedStyle.height),
            TrackedStyles.Left => IsLengthNoneOrAuto(Target.computedStyle.left),
            TrackedStyles.Top => IsLengthNoneOrAuto(Target.computedStyle.top),
            TrackedStyles.Right => IsLengthNoneOrAuto(Target.computedStyle.right),
            TrackedStyles.Bottom => IsLengthNoneOrAuto(Target.computedStyle.bottom),
            _ => false,
        };
    }

    static bool IsLengthNoneOrAuto(Length length) =>
        length.IsNone() || length.IsAuto();

    protected void SetInlineStylePixelValue(TrackedStyles trackedStyle, float value)
    {
        if (Target == null || IsReadOnly || AreStylesBound(trackedStyle)) return;

        SetInlineStylePropertyCommand<Length>.Execute(
            CommandSources.Viewport,
            Target,
            GetStylePropertyId(trackedStyle),
            static (p, s, v) => p.SetLength(s, v),
            Length.Pixels(Mathf.Round(value))
        );
    }
}
