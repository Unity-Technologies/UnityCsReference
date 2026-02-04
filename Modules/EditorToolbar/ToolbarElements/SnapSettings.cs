// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Overlays;
using UnityEditor.Snap;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    sealed class SnapSizeField : EditorToolbarFloatField
    {
        public event Action<float> valueChanged;
        public SnapSizeField(string name, float initialValue, bool linked)
        {
            this.name = name;
            value = initialValue;
            showMixedValue = !linked;
            isDelayed = true;
            
            this.RegisterValueChangedCallback(OnValueChanged);
            
            SceneViewToolbarStyles.AddStyleSheets(this);
        }

        public void SetValueWithoutNotify(float newValue, bool linked)
        {
            this.UnregisterValueChangedCallback(OnValueChanged);
            // [UUM-46865] Setting forceUpdateDisplay to true will refresh the field even if the value is the same.
            // Ths is needed in case of showMixedValue is rest to false and the x-value is the same, otherwise the field will not be updated.
            forceUpdateDisplay = true;
            showMixedValue = !linked;
            if (!showMixedValue)
                base.SetValueWithoutNotify(newValue);

            // [UUM-46865] Forcing the MixedValueContent to update, this is needed in case of the newValue.x has changed and
            // showMixedValue hasn't, without this call the field will displayed newValue.x instead of the MixedValueContent.
            UpdateMixedValueContent();
                
            this.RegisterValueChangedCallback(OnValueChanged);
        }
        
        void OnValueChanged(ChangeEvent<float> evt)
        {
            valueChanged?.Invoke(evt.newValue);
        }
    }
    
    [EditorToolbarElement("SceneView/GridSettings", typeof(SceneView))]
    sealed class GridSettingsElement : EditorToolbarDropdown
    {
        SceneView m_SceneView;
        
        public SceneView sceneView { set => m_SceneView = value; }
        
        public GridSettingsElement(SceneView sceneView)
        {
            name = "GridSettings";
            tooltip = "Settings for the Scene view grid.";

            SceneViewToolbarStyles.AddStyleSheets(this);
            
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);
        }
        
        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            clicked += OnDropdownClicked;
        }

        void OnDetachedFromPanel(DetachFromPanelEvent evt)
        {
            clicked -= OnDropdownClicked;
        }

        void OnDropdownClicked()
        {
            PopupWindowBase.Show<SnapSettingsWindow>(this, new Vector2(352, 287));
        }
    }

    [EditorToolbarElement("Tools/Snap Settings", typeof(SceneView))]
    sealed class SnapSettings : VisualElement, IAccessContainerWindow
    {    
        public EditorWindow containerWindow { get; set; }
        SceneView sceneView => containerWindow as SceneView;
        
        readonly EditorToolbarToggle m_GridSnapToggle;
        readonly EditorToolbarToggle m_IncrementSnapToggle;
        readonly SnapSizeField m_IncrementalSnapSizeField;

        public SnapSettings()
        {
            SceneViewToolbarStyles.AddStyleSheets(this);
            
            m_GridSnapToggle = new EditorToolbarToggle();
            m_GridSnapToggle.name = "GridSnappingToggle";
            m_GridSnapToggle.tooltip = L10n.Tr("Toggle absolute Grid Snapping on and off.\n\n" + 
                                               "Grid Snapping will fallback to Incremental Snapping if handle rotation is not aligned to grid.");
            m_GridSnapToggle.RegisterValueChangedCallback((evt) =>
            {
                EditorSnapSettings.gridSnapEnabled = evt.newValue;
                EditorSnapSettings.snapEnabled = evt.newValue;
                
            });
            
            m_IncrementSnapToggle = new EditorToolbarToggle();
            m_IncrementSnapToggle.name = "IncrementalSnappingToggle";
            m_IncrementSnapToggle.tooltip = L10n.Tr("Toggle Incremental Snapping on and off");
            m_IncrementSnapToggle.RegisterValueChangedCallback((evt) =>
            {
                EditorSnapSettings.snapEnabled = evt.newValue;
                EditorSnapSettings.gridSnapEnabled = false;
            });
            Add(m_IncrementSnapToggle);
            
            UpdateGridSnapButtonStates();
            
            m_IncrementalSnapSizeField = new SnapSizeField("IncrementalSnapSize", EditorSnapSettings.move.x, EditorSnapSettings.moveLinked);
            m_IncrementalSnapSizeField.valueChanged += ((value) =>
            {
                var newSnapSize = Vector3.one * value;
                if (newSnapSize != EditorSnapSettings.move)
                {
                    EditorSnapSettings.move = newSnapSize;
                }
            });
            m_IncrementalSnapSizeField.tooltip = L10n.Tr("Incremental Snapping size");
            Add(m_IncrementalSnapSizeField);
            
            EditorToolbarUtility.SetupChildrenAsButtonStrip(this);
            
            Insert(0, m_GridSnapToggle);
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            EditorSnapSettings.snapEnabledChanged += UpdateGridSnapButtonStates;
            EditorSnapSettings.gridSnapEnabledChanged += UpdateGridSnapButtonStates;
            EditorSnapSettings.moveChanged += OnIncrementalSnapChanged;
        }
        
        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            EditorSnapSettings.snapEnabledChanged -= UpdateGridSnapButtonStates;
            EditorSnapSettings.gridSnapEnabledChanged -= UpdateGridSnapButtonStates;
            EditorSnapSettings.moveChanged -= OnIncrementalSnapChanged; 
        }

        void OnIncrementalSnapChanged(Vector3 newValue)
        {
            m_IncrementalSnapSizeField.SetValueWithoutNotify(newValue.x, EditorSnapSettings.moveLinked);
        }
        
        void UpdateGridSnapButtonStates()
        {
            if (EditorSnapSettings.snapEnabled)
            {
                m_IncrementSnapToggle.SetValueWithoutNotify(!EditorSnapSettings.gridSnapEnabled);
                m_GridSnapToggle.SetValueWithoutNotify(EditorSnapSettings.gridSnapEnabled);
            }
            else
            {
                m_IncrementSnapToggle.SetValueWithoutNotify(false);
                m_GridSnapToggle.SetValueWithoutNotify(false);
            }
        }
    }
    
    [EditorToolbarElement("Tools/Angle Snap Settings", typeof(SceneView))]
    sealed class AngleSnapSettings : VisualElement
    {    
        EditorToolbarToggle m_Toggle;
        EditorToolbarFloatField m_FloatField;
        
        public AngleSnapSettings()
        {
            m_Toggle = new EditorToolbarToggle();
            m_Toggle.name = "AngleSnappingToggle";
            m_Toggle.tooltip = L10n.Tr("Toggle Angle Snapping on and off");
            m_Toggle.RegisterValueChangedCallback((evt) => EditorSnapSettings.angleSnapEnabled = !EditorSnapSettings.angleSnapEnabled);
            UpdateGridAngleSnapEnableValue();
            Add(m_Toggle);
            
            m_FloatField = new EditorToolbarFloatField();
            m_FloatField.name = "SceneViewAngleSnapSize";
            m_FloatField.RegisterValueChangedCallback((evt) => EditorSnapSettingsData.instance.snapSettings.rotation = evt.newValue);
            m_FloatField.SetValueWithoutNotify(EditorSnapSettingsData.instance.snapSettings.rotation);
            m_FloatField.tooltip = L10n.Tr("Incremental angle snap size");
            Add(m_FloatField);
            
            EditorToolbarUtility.SetupChildrenAsButtonStrip(this);
            SceneViewToolbarStyles.AddStyleSheets(this);
               
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            EditorSnapSettings.angleSnapEnabledChanged += UpdateGridAngleSnapEnableValue;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            EditorSnapSettings.angleSnapEnabledChanged -= UpdateGridAngleSnapEnableValue;
        }
        
        void UpdateGridAngleSnapEnableValue()
        {
            m_Toggle.SetValueWithoutNotify(EditorSnapSettings.angleSnapEnabled);
        }
    }

    [EditorToolbarElement("Tools/Scale Snap Settings", typeof(SceneView))]
    sealed class ScaleSnapSettings : VisualElement
    {
        EditorToolbarToggle m_Toggle;
        EditorToolbarFloatField m_FloatField;
        
        public ScaleSnapSettings()
        {
            m_Toggle = new EditorToolbarToggle();
            m_Toggle.name = "ScaleSnappingToggle";
            m_Toggle.tooltip = L10n.Tr("Toggle Scale Snapping on and off");
            m_Toggle.RegisterValueChangedCallback((evt) => EditorSnapSettings.scaleSnapEnabled = !EditorSnapSettings.scaleSnapEnabled);
            UpdateGridScaleSnapEnableValue();
            Add(m_Toggle);
            
            m_FloatField = new EditorToolbarFloatField();
            m_FloatField.name = "SceneViewScaleSnapSize";
            m_FloatField.RegisterValueChangedCallback((evt) => EditorSnapSettingsData.instance.snapSettings.scale = evt.newValue);
            m_FloatField.SetValueWithoutNotify(EditorSnapSettingsData.instance.snapSettings.scale);
            m_FloatField.tooltip = L10n.Tr("Scale snap multiplier");
            Add(m_FloatField);

            EditorToolbarUtility.SetupChildrenAsButtonStrip(this);
            SceneViewToolbarStyles.AddStyleSheets(this);

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            EditorSnapSettings.scaleSnapEnabledChanged += UpdateGridScaleSnapEnableValue;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            EditorSnapSettings.scaleSnapEnabledChanged -= UpdateGridScaleSnapEnableValue;
        }

        void UpdateGridScaleSnapEnableValue()
        {
            m_Toggle.SetValueWithoutNotify(EditorSnapSettings.scaleSnapEnabled);
        }
    }
}
