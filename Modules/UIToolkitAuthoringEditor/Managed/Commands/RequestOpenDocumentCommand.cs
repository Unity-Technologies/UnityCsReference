// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Requests that the window showing a local context open <see cref="Document"/> in place of the document it edits,
/// such as a template instance in isolation or in context.
/// </summary>
/// <remarks>
/// The global context navigates the UI Stage instead, so nothing answers this on <see cref="AuthoringContext.Global"/>.
/// </remarks>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
class RequestOpenDocumentCommand : Command<RequestOpenDocumentCommand>
{
    public static void Execute(AuthoringContext context, object source, VisualTreeAssetEditingContext document)
    {
        using var command = GetPooled(source, document);
        context.Messages.Execute(command);
    }

    public static RequestOpenDocumentCommand GetPooled(object source, VisualTreeAssetEditingContext document)
    {
        var pooled = GetPooled();
        pooled.Source = source;
        pooled.Document = document;
        return pooled;
    }

    public VisualTreeAssetEditingContext Document { get; private set; }

    protected override void Init()
    {
        Document = default;
        base.Init();
    }
}
