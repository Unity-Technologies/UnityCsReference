// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using UnityEditor.UIElements.Debugger;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.UIElements
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

