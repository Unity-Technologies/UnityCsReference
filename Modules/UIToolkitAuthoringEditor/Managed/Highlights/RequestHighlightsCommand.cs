// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Command to request items to be highlighted.
/// </summary>
class RequestHighlightsCommand : Command<RequestHighlightsCommand>
{
    public static RequestHighlightsCommand GetPooled(VisualElement element)
    {
        var pooled = GetPooled();
        pooled.Element = element;
        return pooled;
    }

    public static RequestHighlightsCommand GetPooled(StyleRule rule)
    {
        var pooled = GetPooled();
        pooled.Rule = rule;
        return pooled;
    }

    public static RequestHighlightsCommand GetPooled(int veaId)
    {
        var pooled = GetPooled();
        pooled.ElementId = veaId;
        return pooled;
    }

    public VisualElement Element { get; private set; }
    public int? ElementId { get; private set; }
    public StyleRule Rule { get; private set; }

    protected override void Init()
    {
        Element = null;
        Rule = null;
        ElementId = null;
        base.Init();
    }

    public override bool Validate() => Element != null || Rule != null || ElementId.HasValue;
}
