// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;

namespace UnityEditor.Experimental.UIElements
{
    internal class PropertyControl<TType> : BaseControl<TType>
    {
        enum DataType
        {
            Long,
            Double,
            Int,
            Float,
            String,
            Unsupported
        }

        private Func<TType> GetValueDelegate;
        private Action<TType> SetValueDelegate;

        private Label m_Label;

        // TODO : In the future we want to support all controls type (not only the one based on TextField).
        // m_Control will need to be of type BaseControl<TType> at this point.
        // However, to do this we need to refactor all controls that inherit BaseTextControl.
        private BaseTextControl<TType> m_Control;

        public PropertyControl()
            : this("")
        {}

        public PropertyControl(string labelText)
        {
            Init("", labelText);
        }

        public PropertyControl(IUxmlAttributes bag)
        {
            var initValue = bag?.GetPropertyString("value") ?? "";
            var text = bag?.GetPropertyString("label") ?? "";
            Init(initValue, text);
        }

        private void Init(string initialValue, string labelText)
        {
            CacheType();

            AddToClassList("propertyControl");
            m_Label = new Label(labelText);

            Add(m_Label);
            CreateControl();
            value = StringToValue(initialValue);

            EnableMouseDraggerForNumericType();
        }

        DataType dataType { get; set; }

        private void CacheType()
        {
            var ttype = typeof(TType);
            if (ttype == typeof(long))
                dataType = DataType.Long;
            else if (ttype == typeof(int))
                dataType = DataType.Int;
            else if (ttype == typeof(float))
                dataType = DataType.Float;
            else if (ttype == typeof(double))
                dataType = DataType.Double;
            else if (ttype == typeof(string))
                dataType = DataType.String;
            else
            {
                dataType = DataType.Unsupported;
                throw new NotSupportedException($"Unsupported type for PropertyControl {ttype}");
            }
        }

        private void EnableMouseDraggerForNumericType()
        {
            switch (dataType)
            {
                case DataType.Long:
                    AddLabelDragger<long>();
                    break;
                case DataType.Int:
                    AddLabelDragger<int>();
                    break;
                case DataType.Float:
                    AddLabelDragger<float>();
                    break;
                case DataType.Double:
                    AddLabelDragger<double>();
                    break;
                default:
                    break;
            }
        }

        private void AddLabelDragger<TDraggerType>()
        {
            var dragger = new FieldMouseDragger<TDraggerType>((IValueField<TDraggerType>)m_Control);
            dragger.SetDragZone(m_Label);

            m_Label.AddToClassList("propertyControlDragger");
        }

        private static TTo ConvertType<TFrom, TTo>(TFrom value)
        {
            return (TTo)Convert.ChangeType(value, typeof(TTo));
        }

        private void CreateControl<TControlType, TDataType>() where TControlType : BaseTextControl<TDataType>, new()
        {
            var c = new TControlType();
            GetValueDelegate = () => ConvertType<TDataType, TType>(c.value);
            SetValueDelegate = (x) => c.value = ConvertType<TType, TDataType>(x);
            m_Control = c as BaseTextControl<TType>;
        }

        private void CreateControl()
        {
            switch (dataType)
            {
                case DataType.Long:
                    CreateControl<LongField, long>();
                    break;
                case DataType.Int:
                    CreateControl<IntegerField, int>();
                    break;
                case DataType.Float:
                    CreateControl<FloatField, float>();
                    break;
                case DataType.Double:
                    CreateControl<DoubleField, double>();
                    break;
                case DataType.String:
                    CreateControl<TextField, string>();
                    break;
            }

            if (m_Control == null)
                throw new NotSupportedException($"Unsupported type attribute: {typeof(TType)}");

            m_Control.AddToClassList("propertyControlControl");
            Add(m_Control);
        }

        public string label
        {
            get { return m_Label.text; }
            set { m_Label.text = value; }
        }

        private TType StringToValue(string str)
        {
            switch (dataType)
            {
                case DataType.Long:
                case DataType.Int:
                {
                    long v;
                    EditorGUI.StringToLong(str, out v);
                    return ConvertType<long, TType>(v);
                }
                case DataType.Double:
                case DataType.Float:
                {
                    double v;
                    EditorGUI.StringToDouble(str, out v);
                    return ConvertType<double, TType>(v);
                }
            }
            return ConvertType<string, TType>(str);
        }

        public override void OnValueChanged(EventCallback<ChangeEvent<TType>> callback)
        {
            m_Control.OnValueChanged(callback);
        }

        public override void SetValueAndNotify(TType newValue)
        {
            m_Control.SetValueAndNotify(newValue);
        }

        public override TType value
        {
            get { return GetValueDelegate(); }
            set { SetValueDelegate(value); }
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            // Delegate focus to the control
            if (evt.GetEventTypeId() == FocusEvent.TypeId())
                m_Control.Focus();
        }
    }

    internal static class PropertyControlFactory
    {
        public static VisualElement Create(IUxmlAttributes bag, CreationContext cc)
        {
            var type = bag.GetPropertyString("typeOf") ?? "";

            switch (type.ToLower())
            {
                case "long":
                    return new PropertyControl<long>(bag);
                case "int":
                    return new PropertyControl<int>(bag);
                case "float":
                    return new PropertyControl<float>(bag);
                case "double":
                    return new PropertyControl<double>(bag);
                case "string":
                    return new PropertyControl<string>(bag);
            }
            throw new NotSupportedException($"Unsupported type attribute: {type}");
        }
    }
}
