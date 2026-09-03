// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor;

/// <summary>
/// Categories of change that can be reported on a graph or state machine element.
/// </summary>
/// <remarks>
/// <see cref="ChangeKind"/> is a bit-flag enum, so a single value can describe multiple categories of change
/// at once. Combine values with `|` and test them with `&amp;` or <see cref="Enum.HasFlag"/>.
/// <br/>
/// <br/>
/// A value of <see cref="None"/> means no change categories are set.
/// </remarks>
/// <example>
/// Combine flags with the bitwise-or operator, and test them with bitwise-and or <see cref="Enum.HasFlag"/>:
/// <code lang="cs">
/// <![CDATA[
/// ChangeKind kinds = ChangeKind.Added | ChangeKind.Data;
///
/// if ((kinds & ChangeKind.Added) != 0) { /* newly added */ }
/// if (kinds.HasFlag(ChangeKind.Data)) { /* data changed */ }
/// ]]>
/// </code>
/// </example>
[Flags]
public enum ChangeKind
{
    /// <summary>No change categories.</summary>
    None          = 0,
    /// <summary>The position or dimension of the element changed.</summary>
    Layout        = 1 << 0,
    /// <summary>The visual style (color, etc.) of the element changed.</summary>
    Style         = 1 << 1,
    /// <summary>Model data (for example, an inspectable field) changed.</summary>
    Data          = 1 << 2,
    /// <summary>Graph or state machine topology changed; typically, a wire or transition was connected or disconnected.</summary>
    Topology = 1 << 3,
    /// <summary>Grouping of variables in the blackboard changed.</summary>
    Grouping      = 1 << 4,
    /// <summary>The element was added to the graph or state machine.</summary>
    Added         = 1 << 5,
    /// <summary>The element was removed from the graph or state machine.</summary>
    Removed       = 1 << 6,
    /// <summary>A port on the node changed; inspect <see cref="ChangedNode.ChangedPorts"/> for details.</summary>
    /// <remarks>This change kind is never reported by a state machine element.</remarks>
    PortChanged   = 1 << 7,
    /// <summary>The view for the element must be torn down and recreated (for example, a variable node flipped between Get and Set).</summary>
    RecreateView  = 1 << 8,
}

static class ChangeHintExtensions
{
    /// <summary>
    /// Maps an internal <see cref="ChangeHint"/> to its public <see cref="ChangeKind"/> flag.
    /// </summary>
    /// <param name="hint">The internal hint to map.</param>
    /// <remarks><see cref="ChangeHint.Unspecified"/>, <see cref="ChangeHint.NeedsRedraw"/>,
    /// <see cref="ChangeHint.UIHints"/>, and <see cref="ChangeHint.Animation"/> have no public equivalent and
    /// map to <see cref="ChangeKind.None"/>.</remarks>
    /// <returns>The matching public flag, or <see cref="ChangeKind.None"/> when there's no direct equivalent.</returns>
    internal static ChangeKind ToKind(this ChangeHint hint)
    {
        if (hint == ChangeHint.Layout)        return ChangeKind.Layout;
        if (hint == ChangeHint.Style)         return ChangeKind.Style;
        if (hint == ChangeHint.Data)          return ChangeKind.Data;
        if (hint == ChangeHint.GraphTopology) return ChangeKind.Topology;
        if (hint == ChangeHint.Grouping)      return ChangeKind.Grouping;
        if (hint == ChangeHint.RecreateView)  return ChangeKind.RecreateView;
        return ChangeKind.None;
    }
}
