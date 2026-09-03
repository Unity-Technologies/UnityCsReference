// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitAuthoringFramework not yet converted
using System.Collections.Generic;
using UnityEngine.UIElements;
using Unity.Scripting.LifecycleManagement;

namespace Unity.UIToolkit.Editor
{
    // Inner value field for a GridLine placement, built like the length fields: a text
    // value showing "auto" or the line/span number, with a flush auto / line / span unit popup on the right.
    [UxmlElement]
    [NoAutoStaticsCleanup] // Immutable USS-class/unit lookup constants; safe to persist across reloads.
    internal partial class GridLineValueField : BaseField<GridLine>
    {
        static readonly string k_UssClassName = "unity-grid-line-field";
        static readonly List<string> k_Units = new() { "auto", "line", "span" };

        readonly TextField m_Text;
        readonly PopupField<string> m_Unit;
        int m_Number = 1; // remembered line/span count

        public GridLineValueField() : this(null) {}

        public GridLineValueField(string label) : base(label, new VisualElement())
        {
            AddToClassList(k_UssClassName);
            visualInput.style.flexDirection = FlexDirection.Row;

            m_Text = new TextField { isDelayed = true };
            m_Text.AddToClassList("unity-style-field__text-field"); // flex-grow, flush left
            m_Text.style.flexGrow = 1;

            // Like StyleLengthField: the unit popup floats in an absolutely-positioned container over the
            // right edge of the full-width text field so it sits flush, reusing the same USS classes.
            m_Unit = new PopupField<string>(k_Units, 0);
            m_Unit.AddToClassList("unity-style-field__options-popup");
            m_Unit.style.width = 46; // wider than the length unit (24px) to fit "span" / "line" / "auto"

            var popupContainer = new VisualElement();
            popupContainer.AddToClassList("unity-style-field__options-popup-container");
            popupContainer.Add(m_Unit);

            visualInput.Add(m_Text);
            visualInput.Add(popupContainer);

            m_Text.RegisterValueChangedCallback(OnTextChanged);
            m_Unit.RegisterValueChangedCallback(OnUnitChanged);

            // Initialize to `auto` so an unset property reads "auto / auto" from the first frame,
            // rather than the raw constructor defaults (empty text + first unit).
            SetValueWithoutNotify(GridLine.Auto);
        }

        void OnTextChanged(ChangeEvent<string> evt)
        {
            evt.StopPropagation();
            var str = evt.newValue?.Trim();
            if (string.IsNullOrEmpty(str))
            {
                value = GridLine.Auto;
                return;
            }
            if (GridLine.TryParse(str, out var parsed))
            {
                if (parsed.isSpan) { m_Number = parsed.span; value = GridLine.Span(m_Number); }
                else if (parsed.isLine)
                {
                    m_Number = parsed.line;
                    // A bare number keeps the current line/span unit; from auto it becomes a line.
                    value = m_Unit.value == "span" ? GridLine.Span(m_Number) : GridLine.AtLine(m_Number);
                }
                else value = GridLine.Auto;
            }
            else
            {
                SetValueWithoutNotify(value); // invalid input -> restore
            }
        }

        void OnUnitChanged(ChangeEvent<string> evt)
        {
            evt.StopPropagation();
            value = evt.newValue switch
            {
                "line" => GridLine.AtLine(m_Number),
                "span" => GridLine.Span(m_Number),
                _ => GridLine.Auto
            };
        }

        public override void SetValueWithoutNotify(GridLine newValue)
        {
            base.SetValueWithoutNotify(newValue);
            if (newValue.isSpan)
            {
                m_Number = newValue.span;
                m_Text.SetValueWithoutNotify(m_Number.ToString());
                m_Unit.SetValueWithoutNotify("span");
            }
            else if (newValue.isLine)
            {
                m_Number = newValue.line;
                m_Text.SetValueWithoutNotify(m_Number.ToString());
                m_Unit.SetValueWithoutNotify("line");
            }
            else
            {
                m_Text.SetValueWithoutNotify("auto");
                m_Unit.SetValueWithoutNotify("auto");
            }
        }
    }

    // Style field for editing a StyleGridLine (grid-column/row-start/end).
    [UxmlElement]
    internal partial class StyleGridLineField : StylePropertyField<StyleGridLine, GridLineValueField, GridLine>
    {
        public StyleGridLineField() : this(null) {}

        public StyleGridLineField(string label) : base(label, new GridLineValueField()) {}

        protected override GridLineValueField CreateValueField() => new GridLineValueField();

        protected override StyleGridLine CreateStyleValue(GridLine v) => v;

        internal override bool EqualsCurrentValue(StyleGridLine v) => v == value;
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
