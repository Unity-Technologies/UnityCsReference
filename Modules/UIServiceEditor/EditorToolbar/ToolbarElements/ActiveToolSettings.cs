// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    [EditorToolbarElement("Tools/Tool Settings", typeof(DefaultMainToolbar))]
    sealed class ActiveToolSettings : VisualElement
    {
        readonly EditorToolbarCycleButton m_PivotModeButton;
        readonly EditorToolbarCycleButton m_PivotRotationButton;

        public ActiveToolSettings()
        {
            name = "ToolSettings";
            m_PivotModeButton = new EditorToolbarCycleButton
            {
                name = "PivotMode",
                iconNames = new[] { "CenterIcon", "PivotIcon" },
                content = new[]
                {
                    EditorGUIUtility.TrTextContentWithIcon("Center", "Toggle Tool Handle Position\n\nThe tool handle is placed at the center of the selection.", "ToolHandleCenter"),
                    EditorGUIUtility.TrTextContentWithIcon("Pivot", "Toggle Tool Handle Position\n\nThe tool handle is placed at the active object's pivot point.", "ToolHandlePivot"),
                },
                value = (int)Tools.pivotMode,
            };
            m_PivotModeButton.valueChanged += (value) => Tools.pivotMode = (PivotMode)value;
            Add(m_PivotModeButton);

            m_PivotRotationButton = new EditorToolbarCycleButton
            {
                name = "PivotRotation",
                iconNames = new[] { "LocalIcon", "GlobalIcon" },
                content = new[]
                {
                    EditorGUIUtility.TrTextContent("Local", "Toggle Tool Handle Rotation\n\nTool handles are in the active object's rotation."),
                    EditorGUIUtility.TrTextContent("Global", "Toggle Tool Handle Rotation\n\nTool handles are in global rotation."),
                },
                value = (int)Tools.pivotRotation
            };
            m_PivotRotationButton.valueChanged += (value) => Tools.pivotRotation = (PivotRotation)value;
            Add(m_PivotRotationButton);

            EditorToolbarUtility.SetupChildrenAsButtonStrip(this);

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            Tools.pivotRotationChanged += OnPivotRotationChanged;
            Tools.pivotModeChanged += OnPivotModeChanged;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Tools.pivotRotationChanged -= OnPivotRotationChanged;
            Tools.pivotModeChanged -= OnPivotModeChanged;
        }

        void OnPivotModeChanged()
        {
            m_PivotModeButton.value = (int)Tools.pivotMode;
            SceneView.RepaintAll();
        }

        void OnPivotRotationChanged()
        {
            m_PivotRotationButton.value = (int)Tools.pivotRotation;
            SceneView.RepaintAll();
        }
    }
}
