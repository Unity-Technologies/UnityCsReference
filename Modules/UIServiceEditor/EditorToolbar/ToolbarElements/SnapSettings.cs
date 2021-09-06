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
    [EditorToolbarElement("Tools/Snap Settings", typeof(SceneView))]
    sealed class SnapSettings : EditorToolbarDropdownToggle
    {
        public SnapSettings()
        {
            name = "SnappingToggle";
            tooltip = L10n.Tr("Toggle Grid Snapping on and off. Available when you set tool handle rotation to Global.");
            icon = EditorGUIUtility.FindTexture("Snap/SceneViewSnap");

            this.RegisterValueChangedCallback(OnGridSnapEnableValueChanged);
            UpdateGridSnapEnableState();
            UpdateGridSnapEnableValue();

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            Tools.pivotRotationChanged += UpdateGridSnapEnableState;
            EditorSnapSettings.gridSnapEnabledChanged += UpdateGridSnapEnableValue;
            ToolManager.activeToolChanged += UpdateGridSnapEnableState;
            dropdownClicked += OnOnclicked;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Tools.pivotRotationChanged -= UpdateGridSnapEnableState;
            EditorSnapSettings.gridSnapEnabledChanged -= UpdateGridSnapEnableValue;
            ToolManager.activeToolChanged -= UpdateGridSnapEnableState;
            dropdownClicked -= OnOnclicked;
        }

        void OnOnclicked()
        {
            OverlayPopupWindow.Show<SnapSettingsWindow>(this, new Vector2(300, 88));
        }

        void UpdateGridSnapEnableState()
        {
            SetEnabled(EditorSnapSettings.activeToolGridSnapEnabled);
        }

        void OnGridSnapEnableValueChanged(ChangeEvent<bool> evt)
        {
            EditorSnapSettings.gridSnapEnabled = !EditorSnapSettings.gridSnapEnabled;
        }

        void UpdateGridSnapEnableValue()
        {
            SetValueWithoutNotify(EditorSnapSettings.gridSnapEnabled);
        }
    }
}
