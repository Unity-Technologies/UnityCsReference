// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.UIElements
{
    public abstract class UxmlAttributeDescription
    {
        protected const string xmlSchemaNamespace = "http://www.w3.org/2001/XMLSchema";

        protected UxmlAttributeDescription()
        {
            use = Use.Optional;
            restriction = null;
        }

        public enum Use
        {
            None = 0,
            Optional = 1,
            Prohibited = 2,
            Required = 3
        }

        public string name { get; set; }

        string[] m_ObsoleteNames;

        public IEnumerable<string> obsoleteNames
        {
            get { return m_ObsoleteNames; }
            set { m_ObsoleteNames = value.ToArray(); }
        }

        public string type { get; protected set; }
        public string typeNamespace { get; protected set; }
        public abstract string defaultValueAsString { get; }
        public Use use { get; set; }
        public UxmlTypeRestriction restriction { get; set; }

        internal bool TryGetValueFromBagAsString(IUxmlAttributes bag, CreationContext cc, out string value)
        {
            // Regardless of whether the attribute is overwridden or not, we want to error here
            // if there is no valid name.
            if (name == null && (m_ObsoleteNames == null || m_ObsoleteNames.Length == 0))
            {
                Debug.LogError("Attribute description has no name.");
                value = null;
                return false;
            }

            string elementName;
            bag.TryGetAttributeValue("name", out elementName);
            if (!string.IsNullOrEmpty(elementName) && cc.attributeOverrides != null)
            {
                for (var i = 0; i < cc.attributeOverrides.Count; ++i)
                {
                    if (cc.attributeOverrides[i].m_ElementName != elementName)
                        continue;

                    if (cc.attributeOverrides[i].m_AttributeName != name)
                    {
                        if (m_ObsoleteNames != null)
                        {
                            bool matchedObsoleteName = false;
                            for (var j = 0; j < m_ObsoleteNames.Length; j++)
                            {
                                if (cc.attributeOverrides[i].m_AttributeName == m_ObsoleteNames[j])
                                {
                                    matchedObsoleteName = true;
                                    break;
                                }
                            }

                            if (!matchedObsoleteName)
                                continue;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    value = cc.attributeOverrides[i].m_Value;
                    return true;
                }
            }

            if (name == null)
            {
                // Check for:
                // (m_ObsoleteNames == null || m_ObsoleteNames.Length == 0)
                // moved at the top of method so we can assume we have obsolete names here.

                for (var i = 0; i < m_ObsoleteNames.Length; i++)
                {
                    if (bag.TryGetAttributeValue(m_ObsoleteNames[i], out value))
                    {
                        if (cc.visualTreeAsset != null)
                        {
                        }

                        return true;
                    }
                }

                value = null;
                return false;
            }

            if (!bag.TryGetAttributeValue(name, out value))
            {
                if (m_ObsoleteNames != null)
                {
                    for (var i = 0; i < m_ObsoleteNames.Length; i++)
                    {
                        if (bag.TryGetAttributeValue(m_ObsoleteNames[i], out value))
                        {
                            if (cc.visualTreeAsset != null)
                            {
                            }

                            return true;
                        }
                    }
                }

                value = null;
                return false;
            }

            return true;
        }

        protected bool TryGetValueFromBag<T>(IUxmlAttributes bag, CreationContext cc, Func<string, T, T> converterFunc, T defaultValue, ref T value)
        {
            string stringValue;
            if (TryGetValueFromBagAsString(bag, cc, out stringValue))
            {
                if (converterFunc != null)
                {
                    value = converterFunc(stringValue, defaultValue);
                }
                else
                {
                    value = defaultValue;
                }

                return true;
            }

            return false;
        }

        protected T GetValueFromBag<T>(IUxmlAttributes bag, CreationContext cc, Func<string, T, T> converterFunc, T defaultValue)
        {
            if (converterFunc == null)
            {
                throw new ArgumentNullException(nameof(converterFunc));
            }

            string value;
            if (TryGetValueFromBagAsString(bag, cc, out value))
            {
                return converterFunc(value, defaultValue);
            }

            return defaultValue;
        }
    }

    public abstract class TypedUxmlAttributeDescription<T> : UxmlAttributeDescription
    {
        public abstract T GetValueFromBag(IUxmlAttributes bag, CreationContext cc);

        public T defaultValue { get; set; }

        public override string defaultValueAsString { get { return defaultValue.ToString(); } }
    }

    public class UxmlStringAttributeDescription : TypedUxmlAttributeDescription<string>
    {
        public UxmlStringAttributeDescription()
        {
            type = "string";
            typeNamespace = xmlSchemaNamespace;
            defaultValue = "";
        }

        public override string defaultValueAsString { get { return defaultValue; } }

        public override string GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            return GetValueFromBag(bag, cc, (s, t) => s, defaultValue);
        }

        public bool TryGetValueFromBag(IUxmlAttributes bag, CreationContext cc, ref string value)
        {
            return TryGetValueFromBag(bag, cc, (s, t) => s , defaultValue, ref value);
        }
    }

    public class UxmlFloatAttributeDescription : TypedUxmlAttributeDescription<float>
    {
        public UxmlFloatAttributeDescription()
        {
            type = "float";
            typeNamespace = xmlSchemaNamespace;
            defaultValue = 0.0f;
        }

        public override string defaultValueAsString { get { return defaultValue.ToString(CultureInfo.InvariantCulture.NumberFormat); } }

        public override float GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            return GetValueFromBag(bag, cc, (s, f) => ConvertValueToFloat(s, f), defaultValue);
        }

        public bool TryGetValueFromBag(IUxmlAttributes bag, CreationContext cc, ref float value)
        {
            return TryGetValueFromBag(bag, cc, (s, f) => ConvertValueToFloat(s, f), defaultValue, ref value);
        }

        static float ConvertValueToFloat(string v, float defaultValue)
        {
            float l;
            if (v == null || !Single.TryParse(v, out l))
                return defaultValue;
            return l;
        }
    }

    public class UxmlDoubleAttributeDescription : TypedUxmlAttributeDescription<double>
    {
        public UxmlDoubleAttributeDescription()
        {
            type = "double";
            typeNamespace = xmlSchemaNamespace;
            defaultValue = 0.0;
        }

        public override string defaultValueAsString { get { return defaultValue.ToString(CultureInfo.InvariantCulture.NumberFormat); } }

        public override double GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            return GetValueFromBag(bag, cc, (s, d) => ConvertValueToDouble(s, d), defaultValue);
        }

        public bool TryGetValueFromBag(IUxmlAttributes bag, CreationContext cc, ref double value)
        {
            return TryGetValueFromBag(bag, cc, (s, d) => ConvertValueToDouble(s, d), defaultValue, ref value);
        }

        static double ConvertValueToDouble(string v, double defaultValue)
        {
            double l;
            if (v == null || !Double.TryParse(v, out l))
                return defaultValue;
            return l;
        }
    }

    public class UxmlIntAttributeDescription : TypedUxmlAttributeDescription<int>
    {
        public UxmlIntAttributeDescription()
        {
            type = "int";
            typeNamespace = xmlSchemaNamespace;
            defaultValue = 0;
        }

        public override string defaultValueAsString { get { return defaultValue.ToString(CultureInfo.InvariantCulture.NumberFormat); } }

        public override int GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            return GetValueFromBag(bag, cc, (s, i) => ConvertValueToInt(s, i), defaultValue);
        }

        public bool TryGetValueFromBag(IUxmlAttributes bag, CreationContext cc, ref int value)
        {
            return TryGetValueFromBag(bag, cc, (s, i) => ConvertValueToInt(s, i), defaultValue, ref value);
        }

        static int ConvertValueToInt(string v, int defaultValue)
        {
            int l;
            if (v == null || !Int32.TryParse(v, out l))
                return defaultValue;

            return l;
        }
    }

    public class UxmlLongAttributeDescription : TypedUxmlAttributeDescription<long>
    {
        public UxmlLongAttributeDescription()
        {
            type = "long";
            typeNamespace = xmlSchemaNamespace;
            defaultValue = 0;
        }

        public override string defaultValueAsString { get { return defaultValue.ToString(CultureInfo.InvariantCulture.NumberFormat); } }

        public override long GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            return GetValueFromBag(bag, cc, (s, l) => ConvertValueToLong(s, l), defaultValue);
        }

        public bool TryGetValueFromBag(IUxmlAttributes bag, CreationContext cc, ref long value)
        {
            return TryGetValueFromBag(bag, cc, (s, l) => ConvertValueToLong(s, l), defaultValue, ref value);
        }

        static long ConvertValueToLong(string v, long defaultValue)
        {
            long l;
            if (v == null || !long.TryParse(v, out l))
                return defaultValue;
            return l;
        }
    }

    public class UxmlBoolAttributeDescription : TypedUxmlAttributeDescription<bool>
    {
        public UxmlBoolAttributeDescription()
        {
            type = "boolean";
            typeNamespace = xmlSchemaNamespace;
            defaultValue = false;
        }

        public override string defaultValueAsString { get { return defaultValue.ToString().ToLower(); } }

        public override bool GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            return GetValueFromBag(bag, cc, (s, b) => ConvertValueToBool(s, b), defaultValue);
        }

        public bool TryGetValueFromBag(IUxmlAttributes bag, CreationContext cc, ref bool value)
        {
            return TryGetValueFromBag(bag, cc, (s, b) => ConvertValueToBool(s, b), defaultValue, ref value);
        }

        static bool ConvertValueToBool(string v, bool defaultValue)
        {
            bool l;
            if (v == null || !bool.TryParse(v, out l))
                return defaultValue;

            return l;
        }
    }

    public class UxmlColorAttributeDescription : TypedUxmlAttributeDescription<Color>
    {
        public UxmlColorAttributeDescription()
        {
            type = "string";
            typeNamespace = xmlSchemaNamespace;
            defaultValue = new Color(0, 0, 0, 1);
        }

        public override string defaultValueAsString { get { return defaultValue.ToString(); } }

        public override Color GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            return GetValueFromBag(bag, cc, (s, color) => ConvertValueToColor(s, color), defaultValue);
        }

        public bool TryGetValueFromBag(IUxmlAttributes bag, CreationContext cc, ref Color value)
        {
            return TryGetValueFromBag(bag, cc, (s, color) => ConvertValueToColor(s, color), defaultValue, ref value);
        }

        static Color ConvertValueToColor(string v, Color defaultValue)
        {
            Color l;
            if (v == null || !ColorUtility.TryParseHtmlString(v, out l))
                return defaultValue;
            return l;
        }
    }

    public class UxmlTypeAttributeDescription<TBase> : TypedUxmlAttributeDescription<Type>
    {
        public UxmlTypeAttributeDescription()
        {
            type = "string";
            typeNamespace = xmlSchemaNamespace;
            defaultValue = null;
        }

        public override string defaultValueAsString
        {
            get { return defaultValue == null ? "null" : defaultValue.FullName; }
        }

        public override Type GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            return GetValueFromBag(bag, cc, (s, type1) => ConvertValueToType(s, type1), defaultValue);
        }

        public bool TryGetValueFromBag(IUxmlAttributes bag, CreationContext cc, ref Type value)
        {
            return TryGetValueFromBag(bag, cc, (s, type1) => ConvertValueToType(s, type1), defaultValue, ref value);
        }

        Type ConvertValueToType(string v, Type defaultValue)
        {
            if (string.IsNullOrEmpty(v))
                return defaultValue;

            try
            {
                var type = Type.GetType(v, true);

                if (!typeof(TBase).IsAssignableFrom(type))
                    Debug.LogError($"Type: Invalid type \"{v}\". Type must derive from {typeof(TBase).FullName}.");
                else
                    return type;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            return defaultValue;
        }
    }

    public class UxmlEnumAttributeDescription<T> : TypedUxmlAttributeDescription<T> where T : struct, IConvertible
    {
        public UxmlEnumAttributeDescription()
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("T must be an enumerated type");
            }

            type = "string";
            typeNamespace = xmlSchemaNamespace;
            defaultValue = new T();

            UxmlEnumeration enumRestriction = new UxmlEnumeration();

            var values = new List<string>();
            foreach (T item in Enum.GetValues(typeof(T)))
            {
                values.Add(item.ToString(CultureInfo.InvariantCulture));
            }
            enumRestriction.values = values;

            restriction = enumRestriction;
        }

        public override string defaultValueAsString { get { return defaultValue.ToString(CultureInfo.InvariantCulture.NumberFormat); } }

        public override T GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            return GetValueFromBag(bag, cc, (s, convertible) => ConvertValueToEnum(s, convertible), defaultValue);
        }

        public bool TryGetValueFromBag(IUxmlAttributes bag, CreationContext cc, ref T value)
        {
            return TryGetValueFromBag(bag, cc, (s, convertible) => ConvertValueToEnum(s, convertible), defaultValue, ref value);
        }

        static U ConvertValueToEnum<U>(string v, U defaultValue)
        {
            if (v == null || !Enum.IsDefined(typeof(U), v))
                return defaultValue;

            var l = (U)Enum.Parse(typeof(U), v);
            return l;
        }
    }
}
