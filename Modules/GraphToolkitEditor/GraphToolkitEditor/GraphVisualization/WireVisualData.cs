// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace Unity.GraphToolkit.Editor.GraphVisualization;

/// <summary>
/// Contains the data for a wire visual override on the graph canvas.
/// </summary>
/// <remarks>
/// Use <see cref="Unity.GraphToolkit.Editor.GraphVisualization.WireVisualManager"/> (for example <see cref="Unity.GraphToolkit.Editor.GraphVisualization.Context.WireVisuals"/>) to assign this data to a <see cref="WireReference"/>.
/// </remarks>
class WireVisualData
{
    /// <summary>
    /// Whether the wire plays a flow animation on the graph canvas.
    /// </summary>
    public bool IsAnimating { get; set; }

    /// <summary>
    /// The animation speed used when <see cref="IsAnimating"/> is true.
    /// </summary>
    public float AnimationSpeed { get; set; } = 1f;

    /// <summary>
    /// Whether the wire is drawn with a dashed pattern.
    /// </summary>
    public bool IsDashed { get; set; }

    /// <summary>
    /// The line width to use on the graph canvas, or <c>0f</c> to use the default width.
    /// </summary>
    public float WidthOverride { get; set; }

    /// <summary>
    /// The opacity multiplier applied to the wire on the graph canvas.
    /// </summary>
    public float Opacity { get; set; } = 1f;

    internal bool IsDefaultVisualData()
    {
        return !IsAnimating
            && Mathf.Approximately(AnimationSpeed, 1f)
            && !IsDashed
            && Mathf.Approximately(WidthOverride, 0f)
            && Mathf.Approximately(Opacity, 1f);
    }
}
