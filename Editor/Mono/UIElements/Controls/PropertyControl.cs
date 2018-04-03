// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
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
    internal class PropertyControl<TType> : BaseControl<TType>
    {
        public class PropertyControlFactory : UxmlFactory<PropertyControl<TType>, PropertyControlUxmlTraits>
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

            public override bool AcceptsAttributeBag(IUxmlAttributes bag)
            {
                string type = m_Traits.GetValueType(bag);
                if (BuiltInTypeConverter.GetTypeFromName(type) == typeof(TType))
                {
                    return true;
                }

                return false;
            }
        }

        public class PropertyControlUxmlTraits : BaseControlUxmlTraits
        {
            UxmlStringAttributeDescription m_TypeOf;
            UxmlStringAttributeDescription m_Value;
            UxmlStringAttributeDescription m_Label;

            public PropertyControlUxmlTraits()
            {
                m_TypeOf = new UxmlStringAttributeDescription { name = "typeOf", use = UxmlAttributeDescription.Use.Required};
                UxmlEnumeration typeList = new UxmlEnumeration();
                typeList.values = new List<string>();
                foreach (DataType dt in Enum.GetValues(typeof(DataType)))
                {
                    if (dt != DataType.Unsupported)
                    {
                        typeList.values.Add(dt.ToString());
                    }
                }
                m_TypeOf.restriction = typeList;

                m_Value = new UxmlStringAttributeDescription { name = "value" };
                m_Label = new UxmlStringAttributeDescription { name = "label" };
            }

            public override IEnumerable<UxmlAttributeDescription> uxmlAttributesDescription
            {
                get
                {
                    foreach (var attr in base.uxmlAttributesDescription)
                    {
                        yield return attr;
                    }

                    yield return m_TypeOf;
                    yield return m_Value;
                    yield return m_Label;
                }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var initValue = m_Value.GetValueFromBag(bag);
                var text = m_Label.GetValueFromBag(bag);
                ((PropertyControl<TType>)ve).value = ((PropertyControl<TType>)ve).StringToValue(initValue);
                ((PropertyControl<TType>)ve).label = text;
            }

            public string GetValueType(IUxmlAttributes bag)
            {
                return m_TypeOf.GetValueFromBag(bag);
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
}
