// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Overlays;
using UnityEditor.Snap;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    [EditorToolbarElement("Tools/Snap Size", typeof(SceneView))]
    sealed class SnapSize : EditorToolbarFloatField
    {
        public SnapSize()
        {
            name = "SceneViewSnapSize";
            value = GridSettings.size.x;
            showMixedValue = !GridSettings.linked;

            GridSettings.sizeChanged += value =>
            {
                SetValueWithoutNotify(value.x);
                showMixedValue = !GridSettings.linked;
            };

            this.RegisterValueChangedCallback(evt =>
            {
                GridSettings.size = Vector3.one * evt.newValue;
            });

            SceneViewToolbarStyles.AddStyleSheets(this);
        }
    }

    [EditorToolbarElement("Tools/Snap Settings", typeof(SceneView))]
    sealed class SnapSettings : EditorToolbarDropdownToggle
    {
        public SnapSettings()
        {
            name = "SnappingToggle";
            tooltip = L10n.Tr("Toggle Grid Snapping on and off. Available when you set tool handle rotation to Global.");
            icon = EditorGUIUtility.FindTexture("Snap/SceneViewSnap");

            this.RegisterValueChangedCallback(OnGridSnapEnableValueChanged);
            UpdateGridSnapEnableValue();

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            EditorSnapSettings.snapEnabledChanged += UpdateGridSnapEnableValue;
            dropdownClicked += OnDropdownClicked;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            EditorSnapSettings.snapEnabledChanged -= UpdateGridSnapEnableValue;
            dropdownClicked -= OnDropdownClicked;
        }

        void OnDropdownClicked()
        {
            OverlayPopupWindow.Show<SnapSettingsWindow>(this, new Vector2(320, 124));
        }

        void OnGridSnapEnableValueChanged(ChangeEvent<bool> evt)
        {
            EditorSnapSettings.snapEnabled = !EditorSnapSettings.snapEnabled;
        }

        void UpdateGridSnapEnableValue()
        {
            SetValueWithoutNotify(EditorSnapSettings.snapEnabled);
        }
    }
}
