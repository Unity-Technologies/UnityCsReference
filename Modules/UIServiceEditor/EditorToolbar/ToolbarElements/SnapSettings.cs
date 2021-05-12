// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEditor.Snap;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    [EditorToolbarElement("Tools/Snap Settings")]
    sealed class SnapSettingsElement : DropdownToggle
    {
        public SnapSettingsElement()
        {
            name = "SnapSettings";
            tooltip = L10n.Tr(
                "Toggle Grid Snapping on and off. Available when you set tool handle rotation to Global.");

            this.RegisterValueChangedCallback(delegate(ChangeEvent<bool> evt)
            {
                EditorSnapSettings.gridSnapEnabled = evt.newValue;
            });

            UpdateGridSnapEnableState();
            UpdateGridSnapEnableValue();

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            SceneViewToolbarElements.AddStyleSheets(this);
        }

        void OnClickableOnclicked()
        {
            OverlayPopupWindow.ShowOverlayPopup<SnapSettingsWindow>(this, new Vector2(300, 66));
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            Tools.pivotRotationChanged += UpdateGridSnapEnableState;
            EditorSnapSettings.gridSnapEnabledChanged += UpdateGridSnapEnableValue;
            ToolManager.activeToolChanged += UpdateGridSnapEnableState;
            dropdownButton.clicked += OnClickableOnclicked;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Tools.pivotRotationChanged -= UpdateGridSnapEnableState;
            EditorSnapSettings.gridSnapEnabledChanged -= UpdateGridSnapEnableValue;
            ToolManager.activeToolChanged -= UpdateGridSnapEnableState;
            dropdownButton.clicked -= OnClickableOnclicked;
        }

        void UpdateGridSnapEnableState()
        {
            SetEnabled(EditorSnapSettings.activeToolGridSnapEnabled);
        }

        void UpdateGridSnapEnableValue()
        {
            SetValueWithoutNotify(EditorSnapSettings.gridSnapEnabled);
        }
    }
}
