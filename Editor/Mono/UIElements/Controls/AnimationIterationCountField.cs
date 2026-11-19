// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// A field for entering an <see cref="AnimationIterationCount"/>: a finite number of iterations, or an
    /// "infinite" toggle button.
    /// </summary>
    [UxmlElement]
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal partial class AnimationIterationCountField : TextValueField<AnimationIterationCount>
    {
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-animation-iteration-count-field";
        public static readonly string infiniteToggleUssClassName = ussClassName + "__infinite-toggle";

        const float k_DefaultFiniteCount = 1f;

        readonly ToggleButtonGroup m_InfiniteToggle;

        public AnimationIterationCountField() : this(null) { }

        public AnimationIterationCountField(int maxLength) : this(null, maxLength) { }

        public AnimationIterationCountField(string label, int maxLength = kMaxValueFieldLength)
            : base(label, maxLength, new AnimationIterationCountInput())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
            AddLabelDragger<AnimationIterationCount>();

            m_InfiniteToggle = new ToggleButtonGroup { isMultipleSelection = true, allowEmptySelection = true };
            m_InfiniteToggle.AddToClassList(infiniteToggleUssClassName);
            m_InfiniteToggle.Add(new Button { text = "∞", tooltip = "Repeat forever" });
            m_InfiniteToggle.RegisterValueChangedCallback(OnInfiniteToggled);
            hierarchy.Add(m_InfiniteToggle);

            SetValueWithoutNotify(new AnimationIterationCount(k_DefaultFiniteCount));
        }

        void OnInfiniteToggled(ChangeEvent<ToggleButtonGroupState> evt)
        {
            if (evt.target != m_InfiniteToggle)
                return;

            value = IsToggleChecked(evt.newValue) ? AnimationIterationCount.Infinite() : new AnimationIterationCount(k_DefaultFiniteCount);
            evt.StopPropagation();
        }

        static bool IsToggleChecked(ToggleButtonGroupState state)
        {
            Span<int> active = stackalloc int[state.length];
            return state.GetActiveOptions(active).Length > 0;
        }

        public override void SetValueWithoutNotify(AnimationIterationCount newValue)
        {
            base.SetValueWithoutNotify(newValue);

            var state = new ToggleButtonGroupState(0, 1);
            state[0] = newValue.IsInfinite();
            m_InfiniteToggle?.SetValueWithoutNotify(state);
        }

        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, AnimationIterationCount startValue)
        {
            if (value.IsInfinite())
                return;
            ((AnimationIterationCountInput)textInputBase).ApplyInputDeviceDelta(delta, speed, startValue);
        }

        protected override AnimationIterationCount StringToValue(string str)
        {
            return ParseString(str);
        }

        protected override string ValueToString(AnimationIterationCount v)
        {
            return v.ToString();
        }

        static AnimationIterationCount ParseString(string str)
        {
            str = str?.Trim();
            if (IsInfiniteToken(str))
                return AnimationIterationCount.Infinite();
            if (float.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                return new AnimationIterationCount(v);
            return new AnimationIterationCount(k_DefaultFiniteCount);
        }

        static bool IsInfiniteToken(string val)
        {
            return string.Equals(val, "infinite", StringComparison.OrdinalIgnoreCase)
                || string.Equals(val, "infinity", StringComparison.OrdinalIgnoreCase)
                || string.Equals(val, "∞", StringComparison.OrdinalIgnoreCase);
        }

        class AnimationIterationCountInput : TextValueInput
        {
            AnimationIterationCountField parentField => (AnimationIterationCountField)parent;

            protected override string allowedCharacters => "0123456789inftye.∞";

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, AnimationIterationCount _)
            {
                var current = parentField.value;
                if (current.IsInfinite())
                    return;

                var acceleration = NumericFieldDraggerUtility.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);
                var next = current.value + NumericFieldDraggerUtility.NiceDelta(delta, acceleration) * 0.03f;
                parentField.value = new AnimationIterationCount(next);
            }

            protected override string ValueToString(AnimationIterationCount value)
            {
                return value.ToString();
            }

            protected override AnimationIterationCount StringToValue(string str)
            {
                return ParseString(str);
            }
        }
    }
}
