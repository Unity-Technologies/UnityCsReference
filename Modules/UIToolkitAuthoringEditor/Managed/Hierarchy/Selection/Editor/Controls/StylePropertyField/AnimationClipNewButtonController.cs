// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.UIToolkit.Editor.Utilities;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

// Wires the "New..." / "Edit..." button next to a UI animation clip field: with no clip assigned it
// creates a new clip asset, with one assigned it opens the Animation window.
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal static class AnimationClipNewButtonController
{
    // Single source of truth; must match the UXML and is re-exported from BuilderConstants.
    internal const string AnimationClipFieldName = "animation-clip-field";
    internal const string AnimationClipNewButtonName = "animation-clip-new-button";

    const string k_DetachKey = "AnimationClipNewButtonController.Detach";

    const long k_ActivateClipRetryIntervalMs = 16;
    const int k_ActivateClipAttempts = 30;

    static readonly string k_SaveDialogTitle = L10n.Tr("Create New UI Animation Clip", null);
    static readonly string k_SaveDialogDefaultName = L10n.Tr("New UI Animation Clip", null);
    static readonly string k_SaveDialogMessageFormat = L10n.Tr("Create a new UI animation clip for {0}:", null);
    static readonly string k_DefaultMessageSubject = L10n.Tr("the selected element", null);

    static readonly string k_NewButtonText = L10n.Tr("New...");
    static readonly string k_EditButtonText = L10n.Tr("Edit...");
    static readonly string k_NewButtonTooltip = L10n.Tr("Create a new UI Animation Clip asset and assign it to this animation.");
    static readonly string k_EditButtonTooltip = L10n.Tr("Open the Animation window to edit the assigned clip.");

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

        ConnectButtonToField(button, field,
            v => v.value,
            () => CreateAndAssignNewUIAnimationClipFromDialog(field, getDialogSubject?.Invoke()));
    }

    // Re-connecting detaches the previous subscriptions; pooled inspectors rebind the same Button to a
    // different field. Hosts that push values with SetValueWithoutNotify must also call UpdateButtonMode.
    static void ConnectButtonToField<TValue>(Button button, BaseField<TValue> field,
        Func<TValue, UIAnimationClip> readClipFromFieldValue, Action createNew)
    {
        if (button.GetProperty(k_DetachKey) is Action detachPrevious)
            detachPrevious();

        // Replace clickable (not `clicked +=`) so re-binding doesn't stack handlers; the enabled mirror
        // plus VisualElement's own disabled-event suppression are enough — no extra enabled guard needed.
        button.clickable = new Clickable(() =>
        {
            var clip = readClipFromFieldValue(field.value);
            if (clip != null)
                OpenAnimationWindow(clip);
            else
                createNew();
        });

        EventCallback<ChangeEvent<TValue>> onValueChanged =
            evt => UpdateButtonMode(button, readClipFromFieldValue(evt.newValue) != null);
        EventCallback<PropertyChangedEvent> onFieldPropertyChanged = evt =>
        {
            if (evt.property == VisualElement.enabledSelfProperty)
                button.SetEnabled(field.enabledSelf);
        };

        field.RegisterCallback(onValueChanged);
        field.RegisterCallback(onFieldPropertyChanged);
        button.SetProperty(k_DetachKey, (Action)(() =>
        {
            field.UnregisterCallback(onValueChanged);
            field.UnregisterCallback(onFieldPropertyChanged);
        }));

        UpdateButtonMode(button, readClipFromFieldValue(field.value) != null);
        button.SetEnabled(field.enabledSelf);
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal static void UpdateButtonMode(Button button, bool hasClip)
    {
        button.text = hasClip ? k_EditButtonText : k_NewButtonText;
        button.tooltip = hasClip ? k_EditButtonTooltip : k_NewButtonTooltip;
    }

    // Focusing the window is not enough to edit a specific clip: its selection keeps whichever clip is
    // already active and otherwise falls back to the target's first one, so a row past the first would
    // silently edit another row's clip.
    internal static void OpenAnimationWindow(UIAnimationClip clip)
    {
        var window = EditorWindow.GetWindow<AnimationWindow>();
        if (clip == null || TryActivateClip(window, clip))
            return;

        // A window this click just created has no usable selection until it has laid out; the retry rides
        // the window's own scheduler so it dies with the window instead of outliving it.
        var activated = false;
        var attemptsLeft = k_ActivateClipAttempts;
        window.rootVisualElement.schedule
            .Execute(() =>
            {
                activated = TryActivateClip(window, clip);
                attemptsLeft--;
            })
            .Every(k_ActivateClipRetryIntervalMs)
            .Until(() => activated || attemptsLeft <= 0);
    }

    static bool TryActivateClip(AnimationWindow window, UIAnimationClip clip)
    {
        return window != null
            && window.selection is UIToolkitAnimationSelectionItemBase target
            && target.TrySetActiveClip(clip);
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

    // Overloads for the multi-property animation row, whose clip field is a raw UIAnimationClipField (the
    // single-clip inspector above uses the StyleUIAnimationClipField wrapper). Assigning the field value routes
    // the created clip through the row's change handler, so each host writes it back through its own path.
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal static UIAnimationClip CreateAndAssignNewUIAnimationClip(BaseField<UIAnimationClip> field, string path)
    {
        return UIAnimationClipFactory.CreateAssetAndAssignToField(path, loaded => field.value = loaded);
    }

    // canEditInAnimationWindow is read on each click and clip change, not captured: a host can flip the
    // capability after its rows are built, and list rows are recycled across hosts.
    internal static void ConnectRowButton(Button button, BaseField<UIAnimationClip> field, Func<bool> canEditInAnimationWindow)
    {
        ConnectButtonToField(button, field,
            v => canEditInAnimationWindow() ? v : null,
            () => CreateNewUIAnimationClipFromDialog(null, loaded => field.value = loaded));
    }

    // UI Builder's animation clip row uses a plain ObjectField and its own undo/USS write-back flow, passed
    // in as createNew. It reports no clip because the Animation window follows the global selection, which
    // the Builder does not drive, so the button stays in create mode.
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal static void ConnectObjectFieldButton(Button button, ObjectField field, Action createNew)
    {
        ConnectButtonToField(button, field, _ => null, createNew);
    }
}
