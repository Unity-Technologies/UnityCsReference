// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.EditorTools;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    [EditorToolbarElement("Tools/Snap Settings", typeof(DefaultMainToolbar))]
    sealed class SnapSettings : VisualElement
    {
        readonly EditorToolbarToggle m_GridSnapEnabledToggle;

        public SnapSettings()
        {
            name = "SnapSettings";

            m_GridSnapEnabledToggle = new EditorToolbarToggle
            {
                name = "SnappingToggle",
                tooltip = L10n.Tr("Toggle Grid Snapping on and off. Available when you set tool handle rotation to Global."),
            };

            m_GridSnapEnabledToggle.RegisterValueChangedCallback(OnGridSnapEnableValueChanged);
            UpdateGridSnapEnableState();
            UpdateGridSnapEnableValue();
            Add(m_GridSnapEnabledToggle);

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            Tools.pivotRotationChanged += UpdateGridSnapEnableState;
            EditorSnapSettings.gridSnapEnabledChanged += UpdateGridSnapEnableValue;
            ToolManager.activeToolChanged += UpdateGridSnapEnableState;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Tools.pivotRotationChanged -= UpdateGridSnapEnableState;
            EditorSnapSettings.gridSnapEnabledChanged -= UpdateGridSnapEnableValue;
            ToolManager.activeToolChanged -= UpdateGridSnapEnableState;
        }

        void UpdateGridSnapEnableState()
        {
            m_GridSnapEnabledToggle.SetEnabled(EditorSnapSettings.activeToolGridSnapEnabled);
        }

        void OnGridSnapEnableValueChanged(ChangeEvent<bool> evt)
        {
            EditorSnapSettings.gridSnapEnabled = !EditorSnapSettings.gridSnapEnabled;
        }

        void UpdateGridSnapEnableValue()
        {
            m_GridSnapEnabledToggle.SetValueWithoutNotify(EditorSnapSettings.gridSnapEnabled);
        }
    }
}
