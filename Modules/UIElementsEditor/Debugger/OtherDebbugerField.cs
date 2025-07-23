// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Transactions;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Debugger
{
    internal class AngleField : BaseField<Angle>
    {
        EnumField m_unitField = new();
        FloatField m_floatField = new("Angle", 20) { style = { flexGrow = 0, width = StyleKeyword.Initial } };

        public AngleField(string label = null) : this(label, Angle.None())
        {

        }

        public AngleField(string label, Angle angle) : base(label)
        {
            style.flexBasis = StyleKeyword.Auto;
            style.flexShrink = 0;
            m_floatField.style.flexGrow = 1;
            m_unitField.style.flexGrow = 0.5f;

            VisualElement content = new() { style = { flexDirection = FlexDirection.Row, flexShrink = 0 } };
            content.Add(m_floatField);
            m_floatField.textInputBase.textElement.style.width = 40;
            content.Add(m_unitField);
            m_unitField.Init(AngleUnit.Degree);
            visualInput = content;


            m_floatField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != value.value)
                {
                    var newVal = value;
                    newVal.value = e.newValue;
                    value = newVal;
                }
            });

            m_unitField.RegisterValueChangedCallback(e =>
            {
                if ((AngleUnit)e.newValue != value.unit)
                {
                    var newVal = value;
                    newVal.ConvertTo((AngleUnit)e.newValue);
                    value = newVal;
                }
            });
        }

        public override void SetValueWithoutNotify(Angle angle)
        {
            base.SetValueWithoutNotify(angle);
            m_unitField.SetValueWithoutNotify(value.unit);
            m_floatField.SetValueWithoutNotify(value.value);
        }
    }

    internal class StyleRatioField : TextValueField<StyleRatio>
    {
        // This property to alleviate the fact we have to cast all the time
        RatioInput lengthInput => (RatioInput)textInputBase;

        record struct CommonOption(string Name, float w, float h);


        static readonly List<CommonOption> commonOptions = new()
     {
         new CommonOption("auto", 0, 0),
         new CommonOption("1/1", 1, 1),
         new CommonOption("3/2", 3, 2), // Ipads
         new CommonOption("4/3", 4, 3), // Common 35mm film/TV 
         new CommonOption("5/4", 5, 4), // Old SVGA monitors
         new CommonOption("16/9", 16, 9),  // common widescreen video
     };

        PopupField<CommonOption> commonField = new(commonOptions, commonOptions[0], FormatItemForUITK, FormatItemForOs) { style = { marginLeft = 0 } };
        static string FormatItemForOs(CommonOption myData)
        {
            //To not display the / as a sub-menu in the contextMenu
            return myData.Name.Replace("/", "\u200A\u2044\u200A");
        }

        static string FormatItemForUITK(CommonOption myData)
        {
            return myData.Name;
        }

        public StyleRatioField() : this((string)null) { }

        public StyleRatioField(int maxLength)
            : this(null, maxLength) { }

        public StyleRatioField(string label, int maxLength = kMaxValueFieldLength)
            : base(label, maxLength, new RatioInput())
        {
            value = Ratio.Auto();
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
            AddLabelDragger<StyleRatio>();
            commonField.RegisterValueChangedCallback(e => {
                text = e.newValue.Name;
                SetValueWithoutNotify((e.newValue.w == 0 || e.newValue.h == 0) ? Ratio.Auto() : new Ratio(e.newValue.w / e.newValue.h));
            });
            Insert(childCount-1, commonField);
        }

        public override void SetValueWithoutNotify(StyleRatio t)
        {
            base.SetValueWithoutNotify(t);

            if (t.keyword == StyleKeyword.Auto || t.value.IsAuto())
            {
                commonField.SetValueWithoutNotify(commonOptions[0]);
                return;
            }
            else
            {
                const float epsilon = 0.0015f;

                foreach (var option in commonOptions)
                {
                    if (Math.Abs(option.w / option.h - t.value) < epsilon)
                    {
                        commonField.SetValueWithoutNotify(option);
                        return;
                    }

                }
            }
            commonField.SetValueWithoutNotify(new("Custorm", 0, 0));
        }

        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, StyleRatio startValue)
        {
            lengthInput.ApplyInputDeviceDelta(delta, speed, startValue);
        }

        protected override StyleRatio StringToValue(string str)
        {
            return ParseString(str, value);
        }

        protected override string ValueToString(StyleRatio value)
        {
            // When what is in the text box is equivalent, don't change anything
            // Explicitly handle auto to remove any leftover like "autoooo" => auto
            if (value.keyword == StyleKeyword.Auto || value.value.IsAuto() || (!value.value.IsAuto() && StringToValue(text) != value))
                return ValueToNiceString(value.value); ;

            return text;
        }

        internal override void UpdateTextFromValue()
        {
            if (value.keyword == StyleKeyword.Auto || value.value.IsAuto())
            {
                base.UpdateTextFromValue();
                return;
            }

            if (StringToValue(text) == value)
            return;

            base.UpdateTextFromValue();
        }

        private static StyleRatio ParseString(string str, StyleRatio defaultValue)
        {
            if(str.StartsWith("auto", StringComparison.OrdinalIgnoreCase))
            {
                return StyleRatio.Auto();
            }

            if (str.Contains("/") || str.Contains(":"))
            {
                var parts = str.Split('/', ':');
                if (parts.Length == 2)
                {
                    if (double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double w) && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double h))
                    {
                        if(h <=  0 || w <=0 || double.IsNaN(h) || double.IsNaN(w) )
                        {
                            return defaultValue;
                        }

                        return new StyleRatio(new Ratio((float)(w/ h)));
                    }
                }
            }

            if( double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                if ( value == 0 || double.IsNaN(value) )
                {
                    return defaultValue;
                }
                return new StyleRatio(new Ratio((float)value));
            }

            return defaultValue;
        }

        static string ValueToNiceString(StyleRatio v)
        {
            if (v.keyword == StyleKeyword.Auto || v.value.IsAuto())
            {
                return "auto";
            }

            if (GetIntegerAspectRatioFromFloat(v.value.value, out int w, out int h))
            {
                return $"{w}/{h}";
            }

            return v.ToString();
        }

        public static bool GetIntegerAspectRatioFromFloat(float aspect, out int w, out int h)
        {
            w = h = 1;
            const float epsilon = 0.00015f;

            for (h = 1; h < 16; ++h)
            {
                w = Mathf.RoundToInt(aspect * h);
                {
                    if (Mathf.Abs(aspect - (float)w / h) < epsilon)
                    {
                        return true;
                    }
                }
            }

            //can't find a good one
            return false;
        }

        class RatioInput : TextValueInput
        {
            StyleRatioField parentLengthField => (StyleRatioField)parent;

            protected override string allowedCharacters
            {
                get { return "0123456789autone.,/:"; }
            }

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, StyleRatio _)
            {

                var sensitivity = 1f/300f;//300 pixels to go form one end to to the other.
                float acceleration = NumericFieldDraggerUtility.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);


                var currentValue = StringToValue(text);
                float v = currentValue.value.IsAuto() ? 1 : currentValue.value.value;


                var linearDelta = Mathf.Clamp( NumericFieldDraggerUtility.NiceDelta(delta, acceleration) * sensitivity, -0.5f, 0.5f);

                v *= (1 + linearDelta) / (1 - linearDelta);

                v = Mathf.Clamp(v, 0.01f, 100);

                // Assumes the field is not delayed
                // By setting the value directly, we have no control over the end result of the toString and fractions may pop while dragging. We might neeed something more specific
                parentLengthField.value = v;

            }

            protected override string ValueToString(StyleRatio value)
            {
                return ValueToNiceString(value);
            }

            protected override StyleRatio StringToValue(string str)
            {
                return ParseString(str, parentLengthField.value);
            }
        }
    }


    internal class RotateField : BaseField<Rotate>
    {
        AngleField m_angleField = new();
        Vector3Field m_axisField = new("Axis");

         public RotateField() : this(null) { }
        public RotateField(string label) : this(label, Rotate.None()) { }

        public RotateField(string label, Rotate rotate) : base(label)
        {
            style.flexBasis = StyleKeyword.Auto;
            style.flexShrink = 0;
            m_angleField.style.flexGrow = 1;
            m_axisField.style.flexGrow = 0.2f;

            //FlexWrap has a bug
            VisualElement content = new() { style = { flexDirection = FlexDirection.Row, flexShrink = 0/*, flexWrap = Wrap.Wrap*/ } };
            content.Add(m_angleField);
            content.Add(m_axisField);
            visualInput = content;
            content.Query<Label>().ForEach(l => l.style.minWidth = StyleKeyword.Initial);


            m_angleField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != value.angle)
                {
                    var newVal = value;
                    newVal.angle = e.newValue;
                    value = newVal;
                }
            });

            m_axisField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != value.axis)
                {
                    var newVal = value;
                    newVal.axis = e.newValue;
                    value = newVal;
                }
            });

            SetValueWithoutNotify(rotate);
        }

        public override void SetValueWithoutNotify(Rotate rotate)
        {
            base.SetValueWithoutNotify(rotate);
            m_angleField.SetValueWithoutNotify(value.angle);
            m_axisField.SetValueWithoutNotify(value.axis);
        }
    }

    internal class TextAutoSizeField : BaseField<TextAutoSize>
    {
        EnumField mode = new EnumField("Mode");
        StyleLengthField minSize = new StyleLengthField("Min Size");
        StyleLengthField maxSize = new StyleLengthField("Max Size");

        public TextAutoSizeField() : this(null) { }
        public TextAutoSizeField(string label) : this(label, TextAutoSize.None()) { }
        public TextAutoSizeField(string label, TextAutoSize tas) : base(label)
        {
            style.flexBasis = StyleKeyword.Auto;
            style.flexShrink = 0;
            mode.style.flexGrow = 1;
            minSize.style.flexGrow = 1;
            maxSize.style.flexGrow = 1;

            VisualElement content = new() { style = { flexDirection = FlexDirection.Row, flexShrink = 0 } };
            content.Add(mode);
            content.Add(minSize);
            content.Add(maxSize);
            visualInput = content;
            content.Query<Label>().ForEach(l => l.style.minWidth = 0);

            mode.Init(TextAutoSizeMode.None);
            mode.RegisterValueChangedCallback(e =>
            {
                if ((TextAutoSizeMode)e.newValue != value.mode)
                {
                    var newVal = value;
                    newVal.mode = (TextAutoSizeMode)e.newValue;
                    value = newVal;
                }
            });

            minSize.RegisterValueChangedCallback(e =>
            {
                if (!Mathf.Approximately(e.newValue.value.value, value.minSize.value))
                {
                    var newVal = value;
                    newVal.minSize = e.newValue.value;
                    value = newVal;
                }
            });

            maxSize.RegisterValueChangedCallback(e =>
            {
                if (!Mathf.Approximately(e.newValue.value.value, value.maxSize.value))
                {
                    var newVal = value;
                    newVal.maxSize = e.newValue.value;
                    value = newVal;
                }
            });

            SetValueWithoutNotify(tas);
        }

        public override void SetValueWithoutNotify(TextAutoSize tas)
        {
            base.SetValueWithoutNotify(tas);
            mode.SetValueWithoutNotify(value.mode);
            minSize.SetValueWithoutNotify(value.minSize.value);
            maxSize.SetValueWithoutNotify(value.maxSize.value);
        }
    }

    internal class TranslateField : BaseField<Translate>
    {
        StyleLengthField x = new("x");
        StyleLengthField y = new("y");
        FloatField z = new("z");

        public TranslateField() : this(null) { }
        public TranslateField(string label) : this(label, Translate.None()) { }

        public TranslateField(string label, Translate t) : base(label)
        {
            style.flexBasis = StyleKeyword.Auto;
            style.flexShrink = 0;
            style.flexGrow = 1;
            x.style.flexGrow = 1;
            y.style.flexGrow = 1;
            z.style.flexGrow = 1;

            VisualElement content = new() { style = { flexDirection = FlexDirection.Row, flexShrink = 0, flexGrow = 1/*, flexWrap = Wrap.Wrap*/ } };
            content.Add(x);
            content.Add(y);
            content.Add(z);
            visualInput = content;
            content.Query<Label>().ForEach(l => l.style.minWidth = 0);

            x.RegisterValueChangedCallback(e =>
            {
                if (e.newValue.ToLength() != value.x)
                {
                    var newVal = value;
                    newVal.x = e.newValue.ToLength();
                    value = newVal;
                }
            });

            y.RegisterValueChangedCallback(e =>
            {
                if (e.newValue.ToLength() != value.y)
                {
                    var newVal = value;
                    newVal.y = e.newValue.ToLength();
                    value = newVal;
                }
            });

            z.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != value.z)
                {
                    var newVal = value;
                    newVal.z = e.newValue;
                    value = newVal;
                }
            });

            SetValueWithoutNotify(t);
        }
        public override void SetValueWithoutNotify(Translate t)
        {
            base.SetValueWithoutNotify(t);
            x.SetValueWithoutNotify(value.x);
            y.SetValueWithoutNotify(value.y);
            z.SetValueWithoutNotify(value.z);
        }
    }

    internal class TransformOriginField : BaseField<TransformOrigin>
    {
        StyleLengthField x = new("x");
        StyleLengthField y = new("y");
        FloatField z = new("z");

        public TransformOriginField() : this(null) { }
        public TransformOriginField(string label) : this(label, TransformOrigin.Initial()) { }

        public TransformOriginField(string label, TransformOrigin to) : base(label)
        {
            style.flexBasis = StyleKeyword.Auto;
            style.flexShrink = 0;
            x.style.flexGrow = 1;
            y.style.flexGrow = 1;
            z.style.flexGrow = 1;

            VisualElement content = new() { style = { flexDirection = FlexDirection.Row, flexShrink = 0/*, flexWrap = Wrap.Wrap */} };
            content.Add(x);
            content.Add(y);
            content.Add(z);
            visualInput = content;
            content.Query<Label>().ForEach(l => l.style.minWidth = 0);

            x.RegisterValueChangedCallback(e =>
            {
                if (e.newValue.ToLength() != value.x)
                {
                    var newVal = value;
                    newVal.x = e.newValue.ToLength();
                    value = newVal;
                }
            });

            y.RegisterValueChangedCallback(e =>
            {
                if (e.newValue.ToLength() != value.y)
                {
                    var newVal = value;
                    newVal.y = e.newValue.ToLength();
                    value = newVal;
                }
            });

            z.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != value.z)
                {
                    var newVal = value;
                    newVal.z = e.newValue;
                    value = newVal;
                }
            });

            SetValueWithoutNotify(to);
        }

        public override void SetValueWithoutNotify(TransformOrigin to)
        {
            base.SetValueWithoutNotify(to);
            x.SetValueWithoutNotify(value.x);
            y.SetValueWithoutNotify(value.y);
            z.SetValueWithoutNotify(value.z);
        }

    }

    internal class BackgroundRepeatField : BaseField<BackgroundRepeat>
    {
        EnumField x = new("x");
        EnumField y = new("y");

        public BackgroundRepeatField() : this(null) { }
        public BackgroundRepeatField(string label) : this(label, BackgroundRepeat.Initial()) { }

        public BackgroundRepeatField(string label, BackgroundRepeat br) : base(label)
        {
            style.flexBasis = StyleKeyword.Auto;
            style.flexShrink = 0;
            x.style.flexGrow = 1;
            y.style.flexGrow = 1;

            VisualElement content = new() { style = { flexDirection = FlexDirection.Row, flexShrink = 0/*, flexWrap = Wrap.Wrap */} };
            x.Init(br.x);
            y.Init(br.y);
            content.Add(x);
            content.Add(y);
            visualInput = content;
            content.Query<Label>().ForEach(l => l.style.minWidth = 0);

            x.RegisterValueChangedCallback(e =>
            {
                if ((Repeat)e.newValue != value.x)
                {
                    var newVal = value;
                    newVal.x = (Repeat)e.newValue;
                    value = newVal;
                }
            });

            y.RegisterValueChangedCallback(e =>
            {
                if ((Repeat)e.newValue != value.y)
                {
                    var newVal = value;
                    newVal.y = (Repeat)e.newValue;
                    value = newVal;
                }
            });

            SetValueWithoutNotify(br);
        }

        public override void SetValueWithoutNotify(BackgroundRepeat br)
        {
            base.SetValueWithoutNotify(br);
            x.SetValueWithoutNotify(value.x);
            y.SetValueWithoutNotify(value.y);
        }
    }
}

