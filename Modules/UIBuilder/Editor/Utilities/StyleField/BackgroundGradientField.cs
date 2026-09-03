// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIBuilder not yet converted
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    // Composite field for BackgroundGradient — wraps the built-in GradientField (color/alpha
    // key editing) with the type / angle / radial controls it doesn't expose. Gradient's
    // 8+8 keys are sampled down to BackgroundGradient's MaxStops-per-side model.
    [UsedImplicitly]
    [UxmlElement]
    internal partial class BackgroundGradientField : BaseField<BackgroundGradient>
    {
        const string k_FieldClassName = "unity-background-gradient-style-field";
        const string k_RowClassName = "unity-background-gradient-style-field__row";
        const string k_RadialOnlyClassName = "unity-background-gradient-style-field__radial-only";

        readonly EnumField m_TypeField;
        readonly GradientField m_GradientField;
        readonly FloatField m_AngleField;
        readonly VisualElement m_RadialOnlyContainer;
        // Shape control omitted — the hash-cached baker can't honor Circle on non-square elements.
        readonly EnumField m_SizeField;
        readonly Vector2Field m_PositionField;

        bool m_SuppressChangeEvents;

        public BackgroundGradientField() : this(null) {}

        public BackgroundGradientField(string label) : base(label, new VisualElement())
        {
            AddToClassList(BuilderConstants.InspectorContainerClassName);
            AddToClassList(k_FieldClassName);

            visualInput.style.flexDirection = FlexDirection.Column;

            m_TypeField = new EnumField("Type", GradientType.Linear);
            m_TypeField.AddToClassList(k_RowClassName);
            m_TypeField.RegisterValueChangedCallback(_ => OnControlChanged());
            visualInput.Add(m_TypeField);

            m_GradientField = new GradientField("Color")
            {
                value = new Gradient(),
                tooltip = "The colors and alpha stops of the gradient. Up to 4 stops are supported.",
            };
            m_GradientField.AddToClassList(k_RowClassName);
            m_GradientField.RegisterValueChangedCallback(_ => OnControlChanged());
            visualInput.Add(m_GradientField);

            m_AngleField = new FloatField("Angle (deg)") { value = 180f };
            m_AngleField.AddToClassList(k_RowClassName);
            m_AngleField.RegisterValueChangedCallback(_ => OnControlChanged());
            visualInput.Add(m_AngleField);

            m_RadialOnlyContainer = new VisualElement();
            m_RadialOnlyContainer.AddToClassList(k_RadialOnlyClassName);

            m_SizeField = new EnumField("Extent", BackgroundGradientSize.FarthestCorner);
            m_SizeField.AddToClassList(k_RowClassName);
            m_SizeField.RegisterValueChangedCallback(_ => OnControlChanged());
            m_RadialOnlyContainer.Add(m_SizeField);

            m_PositionField = new Vector2Field("Position") { value = new Vector2(0.5f, 0.5f) };
            m_PositionField.AddToClassList(k_RowClassName);
            m_PositionField.RegisterValueChangedCallback(_ => OnControlChanged());
            m_RadialOnlyContainer.Add(m_PositionField);

            visualInput.Add(m_RadialOnlyContainer);

            UpdateModeVisibility(GradientType.Linear);
        }

        // Force-dispatches a ChangeEvent with the current value, bypassing the equality guard.
        internal void NotifyCurrentValue()
        {
            using var evt = ChangeEvent<BackgroundGradient>.GetPooled(value, value);
            evt.target = this;
            SendEvent(evt);
        }

        public override void SetValueWithoutNotify(BackgroundGradient newValue)
        {
            base.SetValueWithoutNotify(newValue);
            PushModelToControls(newValue);
        }

        // Sensible starting gradient (CSS "to bottom", white → white) shown when nothing is set.
        internal static BackgroundGradient defaultAuthoringGradient
            => UnityEditor.UIElements.BackgroundField.defaultAuthoringGradient;

        void PushModelToControls(in BackgroundGradient model)
        {
            // Substitute the authoring default so empty gradients don't show as 0°/(0,0).
            var effective = model.IsEmpty() ? defaultAuthoringGradient : model;
            m_SuppressChangeEvents = true;
            try
            {
                m_TypeField.SetValueWithoutNotify(effective.type);
                m_AngleField.SetValueWithoutNotify(effective.angle * Mathf.Rad2Deg);
                m_SizeField.SetValueWithoutNotify(effective.size);
                m_PositionField.SetValueWithoutNotify(effective.position);
                m_GradientField.SetValueWithoutNotify(BackgroundGradientToUnityGradient(effective));
                UpdateModeVisibility(effective.type);
            }
            finally
            {
                m_SuppressChangeEvents = false;
            }
        }

        void OnControlChanged()
        {
            if (m_SuppressChangeEvents)
                return;

            var type = (GradientType)m_TypeField.value;
            UpdateModeVisibility(type);

            var built = new BackgroundGradient
            {
                type = type,
                angle = m_AngleField.value * Mathf.Deg2Rad,
                shape = BackgroundGradientShape.Ellipse, // Circle disabled
                size = (BackgroundGradientSize)m_SizeField.value,
                position = m_PositionField.value,
                stops = UnityGradientToBackgroundStops(m_GradientField.value),
            };
            value = built;
        }

        void UpdateModeVisibility(GradientType type)
        {
            bool isLinear = type == GradientType.Linear;
            m_AngleField.style.display = isLinear ? DisplayStyle.Flex : DisplayStyle.None;
            m_RadialOnlyContainer.style.display = isLinear ? DisplayStyle.None : DisplayStyle.Flex;
        }

        // Model conversion between BackgroundGradient stops and UnityEngine.Gradient keys
        // lives on UnityEditor.UIElements.BackgroundField (see BackgroundGradientToUnityGradient
        // / UnityGradientToBackgroundStops there); we call into the shared implementation to
        // avoid the two copies drifting.
        static Gradient BackgroundGradientToUnityGradient(in BackgroundGradient bg)
            => UnityEditor.UIElements.BackgroundField.BackgroundGradientToUnityGradient(bg);
        static BackgroundGradientStop[] UnityGradientToBackgroundStops(Gradient g)
            => UnityEditor.UIElements.BackgroundField.UnityGradientToBackgroundStops(g);
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
