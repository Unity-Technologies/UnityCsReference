using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    static class BuiltInTypeConverter
    {
        static Dictionary<string, Type> s_TypeDictionary;

        static BuiltInTypeConverter()
        {
            if (s_TypeDictionary == null)
            {
                s_TypeDictionary = new Dictionary<string, Type>();
                s_TypeDictionary.Add("bool", typeof(long));
                s_TypeDictionary.Add("byte", typeof(byte));
                s_TypeDictionary.Add("sbyte", typeof(sbyte));
                s_TypeDictionary.Add("char", typeof(char));
                s_TypeDictionary.Add("decimal", typeof(decimal));
                s_TypeDictionary.Add("double", typeof(double));
                s_TypeDictionary.Add("float", typeof(float));
                s_TypeDictionary.Add("int", typeof(int));
                s_TypeDictionary.Add("uint", typeof(uint));
                s_TypeDictionary.Add("long", typeof(long));
                s_TypeDictionary.Add("ulong", typeof(ulong));
                s_TypeDictionary.Add("object", typeof(object));
                s_TypeDictionary.Add("short", typeof(short));
                s_TypeDictionary.Add("ushort", typeof(ushort));
                s_TypeDictionary.Add("string", typeof(string));
            }
        }

        public static Type GetTypeFromName(string typeName)
        {
            Type t;
            if (!s_TypeDictionary.TryGetValue(typeName, out t))
            {
                t = Type.GetType(typeName);
            }

            return t;
        }
    }
    internal class PropertyControl<TType> : BaseField<TType>
    {
        public new class UxmlFactory : UxmlFactory<PropertyControl<TType>, UxmlTraits>
        {
            public override string uxmlName
            {
                get
                {
                    string name = typeof(PropertyControl<TType>).Name;
                    if (name.Contains("`"))
                    {
                        name = name.Substring(0, name.IndexOf("`"));
                    }
                    return name;
                }
            }

            public override string uxmlQualifiedName
            {
                get { return uxmlNamespace + "." + uxmlName; }
            }

            public override bool AcceptsAttributeBag(IUxmlAttributes bag, CreationContext cc)
            {
                string type = m_Traits.GetValueType(bag, cc);
                if (BuiltInTypeConverter.GetTypeFromName(type) == typeof(TType))
                {
                    return true;
                }

                return false;
            }
        }

        public new class UxmlTraits : BaseField<TType>.UxmlTraits
        {
            UxmlStringAttributeDescription m_TypeOf = new UxmlStringAttributeDescription { name = "value-type", obsoleteNames = new[] {"typeOf"}, use = UxmlAttributeDescription.Use.Required };
            UxmlStringAttributeDescription m_Value = new UxmlStringAttributeDescription { name = "value" };

            public UxmlTraits()
            {
                m_TypeOf.restriction = new UxmlEnumeration
                {
                    values = Enum.GetValues(typeof(DataType)).Cast<DataType>()
                        .Where(dt => dt != DataType.Unsupported)
                        .Select(dt => dt.ToString())
                };
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var initValue = m_Value.GetValueFromBag(bag, cc);
                ((PropertyControl<TType>)ve).SetValueWithoutNotify(((PropertyControl<TType>)ve).StringToValue(initValue));
                ((PropertyControl<TType>)ve).label = ((BaseField<TType>)ve).label;
                ((BaseField<TType>)ve).label = null;
            }

            public string GetValueType(IUxmlAttributes bag, CreationContext cc)
            {
                return m_TypeOf.GetValueFromBag(bag, cc);
            }
        }

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
        private BaseField<TType> m_Field;

        public new static readonly string ussClassName = "unity-property-control";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public static readonly string draggerUssClassName = ussClassName + "__dragger";
        public static readonly string controlUssClassName = ussClassName + "__control";

        public PropertyControl()
            : this("")
        {}

        public PropertyControl(string labelText)
            : base(null)
        {
            Init("", labelText);
        }

        private void Init(string initialValue, string labelText)
        {
            AddToClassList(ussClassName);
            focusable = true;
            CacheType();

            m_Label = new Label(labelText);
            m_Label.AddToClassList(labelUssClassName);

            Add(m_Label);
            CreateControl();
            SetValueWithoutNotify(StringToValue(initialValue));

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
            var dragger = new FieldMouseDragger<TDraggerType>((IValueField<TDraggerType>)m_Field);
            dragger.SetDragZone(m_Label);

            m_Label.AddToClassList(draggerUssClassName);
        }

        private static TTo ConvertType<TFrom, TTo>(TFrom value)
        {
            return (TTo)Convert.ChangeType(value, typeof(TTo));
        }

        private void CreateControl<TControlType, TDataType>() where TControlType : BaseField<TDataType>, new()
        {
            var c = new TControlType();
            GetValueDelegate = () => ConvertType<TDataType, TType>(c.value);
            SetValueDelegate = (x) => c.value = ConvertType<TType, TDataType>(x);
            m_Field = c as BaseField<TType>;
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

            if (m_Field == null)
                throw new NotSupportedException($"Unsupported type attribute: {typeof(TType)}");

            m_Field.AddToClassList(controlUssClassName);
            m_Field.RegisterValueChangedCallback(OnFieldValueChanged);
            Add(m_Field);
        }

        public new string label
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

        private void OnFieldValueChanged(ChangeEvent<TType> evt)
        {
            using (ChangeEvent<TType> newEvent = ChangeEvent<TType>.GetPooled(evt.previousValue, evt.newValue))
            {
                newEvent.target = this;
                SendEvent(newEvent);
            }
        }

        public override void SetValueWithoutNotify(TType newValue)
        {
            m_Field.SetValueWithoutNotify(newValue);
        }

        public override TType value
        {
            get { return GetValueDelegate(); }
            set { SetValueDelegate(value); }
        }

        protected override void UpdateMixedValueContent()
        {
        }
    }
}
