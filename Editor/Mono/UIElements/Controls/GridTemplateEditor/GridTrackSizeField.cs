// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitFramework not yet converted
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    [NoAutoStaticsCleanup] // Immutable unit lookup tables; safe to persist across reloads.
    internal class GridTrackSizeField : VisualElement
    {
        // Unit popup values. "min"/"max"/"fit" are short labels for min-content/max-content/fit-content.
        const string k_Px = "px", k_Pct = "%", k_Fr = "fr", k_Auto = "auto",
            k_Min = "min", k_Max = "max", k_Minmax = "minmax", k_Fit = "fit";
        const int k_UnitWidth = 54;

        static readonly List<string> k_FullUnits = new() { k_Px, k_Pct, k_Fr, k_Auto, k_Min, k_Max, k_Minmax, k_Fit };
        static readonly List<string> k_SimpleUnits = new() { k_Px, k_Pct, k_Fr, k_Auto, k_Min, k_Max };

        readonly VisualElement m_ValueArea;
        readonly TextField m_Value;
        readonly PopupField<string> m_Unit;
        readonly Label m_Arrow;
        readonly GridTrackSizeField m_Min;
        readonly GridTrackSizeField m_Max;
        readonly GridTrackSizeField m_Fit;

        float m_Number = 1;                    // remembered numeric value across unit switches
        bool m_Notify = true;

        public event Action<GridTrackSize> changed;

        public GridTrackSizeField(bool allowFunctions = true)
        {
            AddToClassList(ussClassName);
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;

            // The value area fills the field; its content is swapped by UpdateLayout. The unit popup floats
            // flush at the right edge (the options-popup-container is absolutely positioned by the core USS).
            m_ValueArea = new VisualElement();
            m_ValueArea.AddToClassList(valueAreaUssClassName);
            m_ValueArea.style.flexDirection = FlexDirection.Row;
            m_ValueArea.style.flexGrow = 1;
            m_ValueArea.style.alignItems = Align.Center;
            Add(m_ValueArea);

            m_Value = new TextField { isDelayed = true };
            m_Value.AddToClassList("unity-style-field__text-field");
            m_Value.style.flexGrow = 1;
            m_Value.RegisterValueChangedCallback(OnTextChanged);

            var unitContainer = new VisualElement();
            unitContainer.AddToClassList("unity-style-field__options-popup-container");
            m_Unit = new PopupField<string>(allowFunctions ? k_FullUnits : k_SimpleUnits, k_Fr);
            m_Unit.AddToClassList("unity-style-field__options-popup");
            m_Unit.style.width = k_UnitWidth; // wider than the length unit (24px) to fit "minmax" / "auto"
            m_Unit.RegisterValueChangedCallback(OnUnitChanged);
            unitContainer.Add(m_Unit);
            Add(unitContainer);

            if (allowFunctions)
            {
                m_Min = new GridTrackSizeField(false) { style = { flexGrow = 1 } };
                m_Max = new GridTrackSizeField(false) { style = { flexGrow = 1 } };
                m_Fit = new GridTrackSizeField(false) { style = { flexGrow = 1 } };
                m_Min.changed += _ => Notify();
                m_Max.changed += _ => Notify();
                m_Fit.changed += _ => Notify();

                m_Arrow = new Label("→");
                m_Arrow.style.marginLeft = 3;
                m_Arrow.style.marginRight = 3;
                m_Arrow.style.flexShrink = 0;
            }

            UpdateLayout();
        }

        public GridTrackSize value
        {
            get => Compose();
            set => SetValueWithoutNotify(value);
        }

        public void SetValueWithoutNotify(GridTrackSize track)
        {
            m_Notify = false;
            try
            {
                if (track.isMinmax && m_Min != null)
                {
                    m_Unit.SetValueWithoutNotify(k_Minmax);
                    m_Min.SetValueWithoutNotify(FromUnit(track.minValue, track.minUnit));
                    m_Max.SetValueWithoutNotify(FromUnit(track.maxValue, track.maxUnit));
                }
                else if (track.isFitContent && m_Fit != null)
                {
                    m_Unit.SetValueWithoutNotify(k_Fit);
                    m_Fit.SetValueWithoutNotify(FromUnit(track.maxValue, track.maxUnit));
                }
                else
                {
                    var unit = UnitOf(track.maxUnit);
                    m_Unit.SetValueWithoutNotify(unit);
                    if (unit is k_Px or k_Pct or k_Fr)
                        m_Number = track.maxValue;
                }
                UpdateLayout();
            }
            finally
            {
                m_Notify = true;
            }
        }

        GridTrackSize Compose()
        {
            switch (m_Unit.value)
            {
                case k_Px: return GridTrackSize.Pixels(m_Number);
                case k_Pct: return GridTrackSize.Percent(m_Number);
                case k_Fr: return GridTrackSize.Fraction(m_Number);
                case k_Auto: return GridTrackSize.Auto();
                case k_Min: return GridTrackSize.MinContent();
                case k_Max: return GridTrackSize.MaxContent();
                case k_Minmax:
                    return GridTrackSize.Minmax(m_Min?.value ?? GridTrackSize.Auto(), m_Max?.value ?? GridTrackSize.Fraction(1));
                case k_Fit:
                    var len = m_Fit?.value ?? GridTrackSize.Pixels(100);
                    return GridTrackSize.FitContent(len.maxValue, len.maxUnit);
                default: return GridTrackSize.Auto();
            }
        }

        void OnTextChanged(ChangeEvent<string> evt)
        {
            evt.StopPropagation();
            if (float.TryParse(evt.newValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var n))
            {
                m_Number = n;
                // Typing a number while on a keyword unit (auto / min / max) means the user wants a size:
                // switch to a fractional track so the value takes effect.
                if (m_Unit.value is k_Auto or k_Min or k_Max)
                {
                    m_Unit.SetValueWithoutNotify(k_Fr);
                    UpdateLayout();
                }
                Notify();
            }
            else
            {
                UpdateLayout(); // invalid input -> restore the displayed value
            }
        }

        void OnUnitChanged(ChangeEvent<string> evt)
        {
            evt.StopPropagation();
            UpdateLayout();
            Notify();
        }

        void UpdateLayout()
        {
            var unit = m_Unit.value;
            bool isMinmax = unit == k_Minmax;
            bool isFit = unit == k_Fit;
            bool isNumeric = unit is k_Px or k_Pct or k_Fr;

            m_ValueArea.Clear();
            // Reserve room for the (absolutely positioned) unit popup only when the nested fields fill the
            // value area; a plain text box intentionally runs under the unit for the flush look.
            m_ValueArea.style.paddingRight = (isMinmax || isFit) ? k_UnitWidth : 0;

            if (isMinmax)
            {
                m_ValueArea.Add(m_Min);
                m_ValueArea.Add(m_Arrow);
                m_ValueArea.Add(m_Max);
            }
            else if (isFit)
            {
                m_ValueArea.Add(m_Fit);
            }
            else
            {
                m_Value.SetValueWithoutNotify(isNumeric ? m_Number.ToString(CultureInfo.InvariantCulture) : string.Empty);
                m_ValueArea.Add(m_Value);
            }
        }

        void Notify()
        {
            if (m_Notify)
                changed?.Invoke(Compose());
        }

        static GridTrackSize FromUnit(float v, GridTrackSizeUnit u) => u switch
        {
            GridTrackSizeUnit.Pixel => GridTrackSize.Pixels(v),
            GridTrackSizeUnit.Percent => GridTrackSize.Percent(v),
            GridTrackSizeUnit.Fraction => GridTrackSize.Fraction(v),
            GridTrackSizeUnit.MinContent => GridTrackSize.MinContent(),
            GridTrackSizeUnit.MaxContent => GridTrackSize.MaxContent(),
            _ => GridTrackSize.Auto()
        };

        static string UnitOf(GridTrackSizeUnit u) => u switch
        {
            GridTrackSizeUnit.Pixel => k_Px,
            GridTrackSizeUnit.Percent => k_Pct,
            GridTrackSizeUnit.Fraction => k_Fr,
            GridTrackSizeUnit.MinContent => k_Min,
            GridTrackSizeUnit.MaxContent => k_Max,
            _ => k_Auto
        };

        public static readonly string ussClassName = "grid-track-size-field";
        public static readonly string valueAreaUssClassName = ussClassName + "__value-area";
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
