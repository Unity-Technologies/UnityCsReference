// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// The "Editor Extension Authoring" setting of a UXML document: whether it is editor UI, which may use the
/// editor-only controls and is previewed with the editor themes. The document stores it as an attribute of its
/// root <c>UXML</c> element, the way the UI Builder writes it.
/// </summary>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
static class EditorExtensionAuthoring
{
    public const string AttributeName = "editor-extension-mode";

    /// <summary>
    /// Whether <paramref name="document"/> is editor UI. A document that does not say follows the project's
    /// default (<see cref="UIToolkitProjectSettings.enableEditorExtensionModeByDefault"/>).
    /// </summary>
    public static bool IsEnabled(VisualTreeAsset document)
    {
        var root = document != null ? document.visualTree : null;
        if (root == null)
            return false;

        if (root.HasAttribute(AttributeName)
            && bool.TryParse(root.GetAttributeValue(AttributeName), out var enabled))
            return enabled;

        return UIToolkitProjectSettings.enableEditorExtensionModeByDefault;
    }

    /// <summary>
    /// Whether the document edited by the <see cref="AuthoringContext"/> that owns <paramref name="element"/> is editor UI.
    /// </summary>
    public static bool IsEnabledFor(VisualElement element)
    {
        return element != null && IsEnabled(AuthoringContext.For(element).EditedVisualTreeAsset);
    }

    /// <summary>
    /// Sets the setting of <paramref name="document"/> through <see cref="SetEditorExtensionModeCommand"/>, so
    /// the change can be undone and saved with the document. The open libraries list or drop the editor controls
    /// to match.
    /// </summary>
    public static void SetEnabled(object source, VisualTreeAsset document, bool enabled)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        SetEditorExtensionModeCommand.Execute(source, document, enabled);
        UIElementsProvider.InvalidateAndRefresh();
    }
}
