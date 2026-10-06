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
    public static RequestHighlightsCommand GetPooled(object source, VisualElement element)
    {
        var pooled = GetPooled();
        pooled.Source = source;
        pooled.Element = element;
        return pooled;
    }

    public static void Execute(object source, VisualElement element)
    {
        using var command = GetPooled(source, element);
        UICommandQueue.Execute(command);
    }

    public static RequestHighlightsCommand GetPooled(object source, StyleRule rule)
    {
        var pooled = GetPooled();
        pooled.Source = source;
        pooled.Rule = rule;
        return pooled;
    }

    public static void Execute(object source, StyleRule rule)
    {
        using var command = GetPooled(source, rule);
        UICommandQueue.Execute(command);
    }

    public static RequestHighlightsCommand GetPooled(object source, int veaId, VisualTreeAsset elementDocument)
    {
        var pooled = GetPooled();
        pooled.Source = source;
        pooled.ElementId = veaId;
        pooled.ElementDocument = elementDocument;
        return pooled;
    }

    public static void Execute(object source, int veaId, VisualTreeAsset elementDocument)
    {
        using var command = GetPooled(source, veaId, elementDocument);
        UICommandQueue.Execute(command);
    }

    public VisualElement Element { get; private set; }

    /// <summary>
    /// The authored element to highlight, for a requester that knows an id rather than an instance.
    /// </summary>
    public int? ElementId { get; private set; }

    /// <summary>
    /// The document <see cref="ElementId"/> was numbered in.
    /// </summary>
    /// <remarks>
    /// Required alongside the id, because an id is only unique inside its own document and the request is
    /// answered against every document on screen — two unrelated UXMLs would otherwise collide on it.
    /// </remarks>
    public VisualTreeAsset ElementDocument { get; private set; }

    public StyleRule Rule { get; private set; }

    protected override void Init()
    {
        Element = null;
        Rule = null;
        ElementId = null;
        ElementDocument = null;
        base.Init();
    }

    public override bool Validate() => Element != null || Rule != null || ElementId.HasValue;
}
