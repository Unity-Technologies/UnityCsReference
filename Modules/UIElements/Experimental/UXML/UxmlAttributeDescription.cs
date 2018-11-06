// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;

namespace UnityEngine.Experimental.UIElements
{
    public abstract class UxmlAttributeDescription
    {
        protected const string k_XmlSchemaNamespace = "http://www.w3.org/2001/XMLSchema";

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
        public string[] obsoleteNames { get; set; }
        public string type { get; protected set; }
        public string typeNamespace { get; protected set; }
        public abstract string defaultValueAsString { get; }
        public Use use { get; set; }
        public UxmlTypeRestriction restriction { get; set; }

        internal bool TryGetValueFromBagAsString(IUxmlAttributes bag, CreationContext cc, out string value)
        {
            if (name == null)
            {
                if (obsoleteNames == null || obsoleteNames.Length == 0)
                {
                    Debug.LogError("Attribute description has no name.");
                    value = null;
                    return false;
                }

                for (var i = 0; i < obsoleteNames.Length; i++)
                {
                    if (bag.TryGetAttributeValue(obsoleteNames[i], out value))
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
                if (obsoleteNames != null)
                {
                    for (var i = 0; i < obsoleteNames.Length; i++)
                    {
                        if (bag.TryGetAttributeValue(obsoleteNames[i], out value))
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

        protected T GetValueFromBag<T>(IUxmlAttributes bag, CreationContext cc, Func<string, T, T> converterFunc, T defaultValue)
        {
            string value;
            if (TryGetValueFromBagAsString(bag, cc, out value))
            {
                return converterFunc(value, defaultValue);
            }

            return defaultValue;
        }
    }

    public class UxmlStringAttributeDescription : UxmlAttributeDescription
    {
        public UxmlStringAttributeDescription()
        {
            type = "string";
            typeNamespace = k_XmlSchemaNamespace;
            defaultValue = "";
        }

        public string defaultValue { get; set; }

        public override string defaultValueAsString { get { return defaultValue; } }

        [Obsolete("Pass a creation context to the method.")]
        public string GetValueFromBag(IUxmlAttributes bag)
        {
            return GetValueFromBag(bag, new CreationContext());
        }

        public string GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            return GetValueFromBag(bag, cc, ConvertValueToString, defaultValue);
        }

        static string ConvertValueToString(string v, string defaultValue)
        {
            return v;
        }
    }

    public class UxmlFloatAttributeDescription : UxmlAttributeDescription
    {
        public UxmlFloatAttributeDescription()
        {
            type = "float";
            typeNamespace = k_XmlSchemaNamespace;
            defaultValue = 0.0f;
        }

        public float defaultValue { get; set; }

        public override string defaultValueAsString { get { return defaultValue.ToString(); } }

        [Obsolete("Pass a creation context to the method.")]
        public float GetValueFromBag(IUxmlAttributes bag)
        {
            return GetValueFromBag(bag, new CreationContext());
        }

        public float GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            return GetValueFromBag(bag, cc, ConvertValueToFloat, defaultValue);
        }

        static float ConvertValueToFloat(string v, float defaultValue)
        {
            float l;
            if (v == null || !Single.TryParse(v, out l))
                return defaultValue;
            return l;
        }
    }

    public class UxmlDoubleAttributeDescription : UxmlAttributeDescription
    {
        public UxmlDoubleAttributeDescription()
        {
            type = "double";
            typeNamespace = k_XmlSchemaNamespace;
            defaultValue = 0.0;
        }

        public double defaultValue { get; set; }

        public override string defaultValueAsString { get { return defaultValue.ToString(); } }

        [Obsolete("Pass a creation context to the method.")]
        public double GetValueFromBag(IUxmlAttributes bag)
        {
            return GetValueFromBag(bag, new CreationContext());
        }

        public double GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            return GetValueFromBag(bag, cc, ConvertValueToDouble, defaultValue);
        }

        static double ConvertValueToDouble(string v, double defaultValue)
        {
            double l;
            if (v == null || !Double.TryParse(v, out l))
                return defaultValue;
            return l;
        }
    }

    public class UxmlIntAttributeDescription : UxmlAttributeDescription
    {
        public UxmlIntAttributeDescription()
        {
            type = "int";
            typeNamespace = k_XmlSchemaNamespace;
            defaultValue = 0;
        }

        public int defaultValue { get; set; }

        public override string defaultValueAsString { get { return defaultValue.ToString(); } }

        [Obsolete("Pass a creation context to the method.")]
        public int GetValueFromBag(IUxmlAttributes bag)
        {
            return GetValueFromBag(bag, new CreationContext());
        }

        public int GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            return GetValueFromBag(bag, cc, ConvertValueToInt, defaultValue);
        }

        static int ConvertValueToInt(string v, int defaultValue)
        {
            int l;
            if (v == null || !Int32.TryParse(v, out l))
                return defaultValue;

            return l;
        }
    }

    public class UxmlLongAttributeDescription : UxmlAttributeDescription
    {
        public UxmlLongAttributeDescription()
        {
            type = "long";
            typeNamespace = k_XmlSchemaNamespace;
            defaultValue = 0;
        }

        public long defaultValue { get; set; }

        public override string defaultValueAsString { get { return defaultValue.ToString(); } }

        [Obsolete("Pass a creation context to the method.")]
        public long GetValueFromBag(IUxmlAttributes bag)
        {
            return GetValueFromBag(bag, new CreationContext());
        }

        public long GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            return GetValueFromBag(bag, cc, ConvertValueToLong, defaultValue);
        }

        static long ConvertValueToLong(string v, long defaultValue)
        {
            long l;
            if (v == null || !long.TryParse(v, out l))
                return defaultValue;
            return l;
        }
    }

    public class UxmlBoolAttributeDescription : UxmlAttributeDescription
    {
        public UxmlBoolAttributeDescription()
        {
            type = "boolean";
            typeNamespace = k_XmlSchemaNamespace;
            defaultValue = false;
        }

        public bool defaultValue { get; set; }

        public override string defaultValueAsString { get { return defaultValue.ToString().ToLower(); } }

        [Obsolete("Pass a creation context to the method.")]
        public bool GetValueFromBag(IUxmlAttributes bag)
        {
            return GetValueFromBag(bag, new CreationContext());
        }

        public bool GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            return GetValueFromBag(bag, cc, ConvertValueToBool, defaultValue);
        }

        static bool ConvertValueToBool(string v, bool defaultValue)
        {
            bool l;
            if (v == null || !bool.TryParse(v, out l))
                return defaultValue;

            return l;
        }
    }

    public class UxmlColorAttributeDescription : UxmlAttributeDescription
    {
        public UxmlColorAttributeDescription()
        {
            type = "string";
            typeNamespace = k_XmlSchemaNamespace;
            defaultValue = new Color(0, 0, 0, 1);
        }

        public Color defaultValue { get; set; }

        public override string defaultValueAsString { get { return defaultValue.ToString(); } }

        [Obsolete("Pass a creation context to the method.")]
        public Color GetValueFromBag(IUxmlAttributes bag)
        {
            return GetValueFromBag(bag, new CreationContext());
        }

        public Color GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            return GetValueFromBag(bag, cc, ConvertValueToColor, defaultValue);
        }

        static Color ConvertValueToColor(string v, Color defaultValue)
        {
            Color l;
            if (v == null || !ColorUtility.TryParseHtmlString(v, out l))
                return defaultValue;
            return l;
        }
    }

    public class UxmlEnumAttributeDescription<T> : UxmlAttributeDescription where T : struct, IConvertible
    {
        public UxmlEnumAttributeDescription()
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("T must be an enumerated type");
            }

            type = "string";
            typeNamespace = k_XmlSchemaNamespace;
            defaultValue = new T();

            UxmlEnumeration enumRestriction = new UxmlEnumeration();
            foreach (T item in Enum.GetValues(typeof(T)))
            {
                enumRestriction.values.Add(item.ToString());
            }
            restriction = enumRestriction;
        }

        public T defaultValue { get; set; }

        public override string defaultValueAsString { get { return defaultValue.ToString(); } }

        [Obsolete("Pass a creation context to the method.")]
        public T GetValueFromBag(IUxmlAttributes bag)
        {
            return GetValueFromBag(bag, new CreationContext());
        }

        public T GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            return GetValueFromBag(bag, cc, ConvertValueToEnum, defaultValue);
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
