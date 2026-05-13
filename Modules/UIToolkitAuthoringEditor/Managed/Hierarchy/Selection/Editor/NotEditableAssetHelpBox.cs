// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[UxmlElement]
internal partial class NotEditableAssetHelpBox : HelpBox
{
    public static readonly string AssetNotEditableMessageWhenUIStagesEnabled = L10n.Tr("UI elements are not yet editable in this view. To edit, open in context with UI Staging Mode or open the asset in the UI Builder.");
    public static readonly string AssetNotEditableMessageWhenUIStagesDisabled = L10n.Tr("UI elements are not yet editable in this view. To edit, open the asset in the UI Builder.");

    public NotEditableAssetHelpBox()
    {
        messageType = HelpBoxMessageType.Info;
        UpdateText();

        RegisterCallback<AttachToPanelEvent>(_ => UIToolkitAuthoringSettings.UIStagesChanged += OnUIStagesEnabledChanged);
        RegisterCallback<DetachFromPanelEvent>(_ => UIToolkitAuthoringSettings.UIStagesChanged -= OnUIStagesEnabledChanged);
    }

    void OnUIStagesEnabledChanged(bool _)
    {
        UpdateText();
    }

    void UpdateText()
    {
        text = UIToolkitAuthoringSettings.EnableUIStages ? AssetNotEditableMessageWhenUIStagesEnabled : AssetNotEditableMessageWhenUIStagesDisabled;
    }
}
