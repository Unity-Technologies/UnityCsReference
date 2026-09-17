// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// The one attribute field an inspector focuses the next time it shows it.
/// </summary>
/// <remarks>
/// For an action that changes which element the inspector shows: the field it wants focused does not exist
/// until the new selection has rebuilt the inspector, well after the action returns. The field claims the
/// request itself, once it is bound and in sync. A request names the inspector as well as the field, because
/// every unlocked inspector rebuilds on the same selection and holds a field for the same element and path.
/// </remarks>
static partial class AttributeFieldFocusRequest
{
    [AutoStaticsCleanupOnCodeReload]
    static VisualElement s_Element;

    [AutoStaticsCleanupOnCodeReload]
    static BindingId s_Path;

    [AutoStaticsCleanupOnCodeReload]
    static IPanel s_InspectorPanel;

    /// <summary>
    /// Asks the inspector on <paramref name="inspectorPanel"/> to focus <paramref name="element"/>'s field at
    /// <paramref name="path"/>, replacing any request still unclaimed.
    /// </summary>
    public static void Set(VisualElement element, BindingId path, IPanel inspectorPanel)
    {
        s_Element = element;
        s_Path = path;
        s_InspectorPanel = inspectorPanel;
    }

    public static void Clear()
    {
        s_Element = null;
        s_Path = default;
        s_InspectorPanel = null;
    }

    /// <summary>
    /// Whether the request names this field of this inspector, and its element is still the selected one.
    /// Answers true to at most one field, and is spent whether or not it answered true.
    /// </summary>
    public static bool TryConsume(VisualElement element, BindingId path, IPanel inspectorPanel)
    {
        if (element == null || s_Element != element || s_InspectorPanel != inspectorPanel || s_Path != path)
            return false;

        var stillSelected = Selection.activeObject is VisualElementSelection selection
            && selection.Element == element;

        // Spent either way, so a request the user navigated away from cannot fire if they come back.
        Clear();
        return stillSelected;
    }
}
