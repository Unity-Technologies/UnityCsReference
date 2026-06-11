// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.UIToolkit.Editor.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

// Wires the "New..." button in StyleInspectorDefaultContent+AnimationSection.uxml.
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal static class AnimationClipNewButtonController
{
    // Single source of truth; must match the UXML and is re-exported from BuilderConstants.
    internal const string AnimationClipFieldName = "animation-clip-field";
    internal const string AnimationClipNewButtonName = "animation-clip-new-button";

    const string k_MirroredFieldKey = "AnimationClipNewButtonController.MirroredField";
    const string k_MirrorCallbackKey = "AnimationClipNewButtonController.MirrorCallback";

    static readonly string k_SaveDialogTitle = L10n.Tr("Create New UI Animation Clip");
    static readonly string k_SaveDialogDefaultName = L10n.Tr("New UI Animation Clip");
    static readonly string k_SaveDialogMessageFormat = L10n.Tr("Create a new UI animation clip for {0}:");
    static readonly string k_DefaultMessageSubject = L10n.Tr("the selected element");

    public static Button FindButton(VisualElement content)
    {
        return content?.Q<Button>(AnimationClipNewButtonName);
    }

    public static StyleUIAnimationClipField FindField(VisualElement content)
    {
        return content?.Q<StyleUIAnimationClipField>(AnimationClipFieldName);
    }

    public static void ConnectButton(VisualElement content, Func<string> getDialogSubject)
    {
        var button = FindButton(content);
        var field = FindField(content);
        if (button == null || field == null)
            return;

        // Replace clickable (not `clicked +=`) so re-binding doesn't stack handlers; the mirror plus
        // VisualElement's own disabled-event suppression are enough — no extra enabled guard needed.
        button.clickable = new Clickable(() => CreateAndAssignNewUIAnimationClipFromDialog(field, getDialogSubject?.Invoke()));

        MirrorEnabledStateOntoButton(button, field);
    }

    // Mirrors the source field's enabled state onto the button; pooled inspectors rebind the same
    // Button to a different source on selection change, so the guard below detaches the prior
    // PropertyChangedEvent subscription before installing the new one.
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal static void MirrorEnabledStateOntoButton(Button button, VisualElement source)
    {
        if (button.GetProperty(k_MirroredFieldKey) is VisualElement previousSource &&
            button.GetProperty(k_MirrorCallbackKey) is EventCallback<PropertyChangedEvent> previousCallback)
        {
            previousSource.UnregisterCallback(previousCallback);
        }

        button.SetEnabled(source.enabledSelf);

        EventCallback<PropertyChangedEvent> callback = evt =>
        {
            if (evt.property == VisualElement.enabledSelfProperty)
                button.SetEnabled(source.enabledSelf);
        };
        source.RegisterCallback(callback);

        button.SetProperty(k_MirroredFieldKey, source);
        button.SetProperty(k_MirrorCallbackKey, callback);
    }

    static void CreateAndAssignNewUIAnimationClipFromDialog(StyleUIAnimationClipField field, string subjectName)
    {
        CreateNewUIAnimationClipFromDialog(subjectName, loaded => field.value = new StyleUIAnimationClip(loaded));
    }

    // Shared entry point for the save-dialog + factory + custom-assignment flow; used by the
    // inspector "New..." button and the Animation Window staging "Create" call-to-action.
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal static UIAnimationClip CreateNewUIAnimationClipFromDialog(string subjectName, Action<UIAnimationClip> assignToOwner)
    {
        if (assignToOwner == null)
            return null;

        var subject = string.IsNullOrEmpty(subjectName) ? k_DefaultMessageSubject : subjectName;
        var message = string.Format(k_SaveDialogMessageFormat, subject);
        var path = EditorUtility.SaveFilePanelInProject(
            k_SaveDialogTitle,
            k_SaveDialogDefaultName,
            "asset",
            message,
            "Assets");
        if (string.IsNullOrEmpty(path))
            return null;

        return UIAnimationClipFactory.CreateAssetAndAssignToField(path, assignToOwner);
    }

    internal static UIAnimationClip CreateAndAssignNewUIAnimationClip(StyleUIAnimationClipField field, string path)
    {
        return UIAnimationClipFactory.CreateAssetAndAssignToField(
            path,
            loaded => field.value = new StyleUIAnimationClip(loaded));
    }
}
