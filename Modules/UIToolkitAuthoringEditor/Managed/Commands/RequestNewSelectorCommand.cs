// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Requests that the style sheets view of a context put <see cref="SelectorText"/> in its new selector field and
/// focus it, so the user can review the selector before the rule is created.
/// </summary>
class RequestNewSelectorCommand : Command<RequestNewSelectorCommand>
{
    public static void Execute(AuthoringContext context, object source, string selectorText)
    {
        using var command = GetPooled(source, selectorText);
        context.Messages.Execute(command);
    }

    public static RequestNewSelectorCommand GetPooled(object source, string selectorText)
    {
        var pooled = GetPooled();
        pooled.Source = source;
        pooled.SelectorText = selectorText;
        return pooled;
    }

    public string SelectorText { get; private set; }

    protected override void Init()
    {
        SelectorText = null;
        base.Init();
    }
}
