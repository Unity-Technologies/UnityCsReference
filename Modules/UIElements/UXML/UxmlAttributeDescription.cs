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

        public UxmlAttributeDescription()
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
        public string type { get; protected set; }
        public string typeNamespace { get; protected set; }
        public abstract string defaultValueAsString { get; }
        public Use use { get; set; }
        public UxmlTypeRestriction restriction { get; set; }
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

        public string GetValueFromBag(IUxmlAttributes bag)
        {
            return bag.GetPropertyString(name, defaultValue);
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

        public float GetValueFromBag(IUxmlAttributes bag)
        {
            return bag.GetPropertyFloat(name, defaultValue);
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

        public double GetValueFromBag(IUxmlAttributes bag)
        {
            return bag.GetPropertyDouble(name, defaultValue);
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

        public int GetValueFromBag(IUxmlAttributes bag)
        {
            return bag.GetPropertyInt(name, defaultValue);
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

        public long GetValueFromBag(IUxmlAttributes bag)
        {
            return bag.GetPropertyLong(name, defaultValue);
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

        public bool GetValueFromBag(IUxmlAttributes bag)
        {
            return bag.GetPropertyBool(name, defaultValue);
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

        public Color GetValueFromBag(IUxmlAttributes bag)
        {
            return bag.GetPropertyColor(name, defaultValue);
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

        public T GetValueFromBag(IUxmlAttributes bag)
        {
            return bag.GetPropertyEnum<T>(name, defaultValue);
        }
    }
}
