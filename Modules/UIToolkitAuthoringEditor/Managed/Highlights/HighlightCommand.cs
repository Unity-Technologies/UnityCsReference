// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Command to process requested items to be highlighted.
/// </summary>
class HighlightCommand : Command<HighlightCommand>
{
    public static HighlightCommand GetPooled(HashSet<VisualElement> elementsToHighlight, HashSet<StyleRule> rulesToHighlight)
    {
        var pooled = GetPooled();
        pooled.Elements = elementsToHighlight;
        pooled.Rules = rulesToHighlight;
        return pooled;
    }

    /// <summary>
    /// Elements to highlight.
    /// </summary>
    /// <remarks> Can be null or empty. </remarks>
    public HashSet<VisualElement> Elements { get; private set; }

    /// <summary>
    /// Rules to highlight.
    /// </summary>
    /// <remarks> Can be null or empty. </remarks>
    public HashSet<StyleRule> Rules { get; private set; }

    public override CommandCategory Category { get; } = CommandCategory.Highlight;

    protected override void Init()
    {
        Elements = null;
        Rules = null;
        base.Init();
    }
}
