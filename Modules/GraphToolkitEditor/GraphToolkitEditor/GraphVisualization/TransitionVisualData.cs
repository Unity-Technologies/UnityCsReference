// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace Unity.GraphToolkit.Editor.GraphVisualization;

/// <summary>
/// Contains the data for a transition visual override on the graph canvas.
/// </summary>
class TransitionVisualData
{
    /// <summary>
    /// Whether the transition plays a flow animation on the graph canvas.
    /// </summary>
    public bool IsAnimating { get; set; }

    /// <summary>
    /// The animation speed used when <see cref="IsAnimating"/> is true.
    /// </summary>
    public float AnimationSpeed { get; set; } = 1f;

    /// <summary>
    /// Whether the transition is drawn with a dashed pattern, or <see langword="null"/> to use the transition's own value.
    /// </summary>
    /// <remarks>
    /// Nullable so that an explicit <c>false</c> is distinguishable from "never set".
    /// </remarks>
    public bool? IsDashed { get; set; }

    /// <summary>
    /// The line width to use on the graph canvas, or <c>0f</c> to use the default width.
    /// </summary>
    public float WidthOverride { get; set; }

    /// <summary>
    /// The opacity multiplier applied to the transition on the graph canvas.
    /// </summary>
    public float Opacity { get; set; } = 1f;

    /// <summary>
    /// The icon displayed for this transition in the transition inspector, or <see langword="null"/> if none is set.
    /// </summary>
    public Texture2D Icon { get; set; }

    /// <summary>
    /// The color to fill the transition's arrow with on the graph canvas, or <see langword="null"/> to use the default color.
    /// </summary>
    public Color? FillColor { get; set; }

    /// <summary>
    /// The color to draw the transition's line with on the graph canvas, or <see langword="null"/> to use the default color.
    /// </summary>
    public Color? LineColor { get; set; }

    internal bool IsDefaultVisualData()
    {
        return !IsAnimating
            && Mathf.Approximately(AnimationSpeed, 1f)
            && IsDashed == null
            && Mathf.Approximately(WidthOverride, 0f)
            && Mathf.Approximately(Opacity, 1f)
            && Icon == null
            && FillColor == null
            && LineColor == null;
    }
}
