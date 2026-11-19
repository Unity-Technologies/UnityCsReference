// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Requests that a UI element (or the current selection when null) be framed in whichever views are
/// listening — the Scene view and the UI Viewport. Lets triggers (Hierarchy, shortcuts, menus) ask
/// for framing without knowing which views are open.
/// </summary>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
class RequestFramingCommand : Command<RequestFramingCommand>
{
    public static void Execute(AuthoringContext context, object source, VisualElement element, bool orientToFace)
    {
        using var command = GetPooled(source, element, orientToFace);
        context.Messages.Execute(command);
    }

    public static RequestFramingCommand GetPooled(object source, VisualElement element, bool orientToFace)
    {
        var pooled = GetPooled();
        pooled.Source = source;
        pooled.Element = element;
        pooled.OrientToFace = orientToFace;
        return pooled;
    }

    public VisualElement Element { get; private set; }
    public bool OrientToFace { get; private set; }

    protected override void Init()
    {
        Element = null;
        OrientToFace = false;
        base.Init();
    }
}
