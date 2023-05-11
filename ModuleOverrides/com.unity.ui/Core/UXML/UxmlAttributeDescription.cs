// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Base class for describing an XML attribute.
    /// </summary>
    public abstract class UxmlAttributeDescription
    {
        protected const string xmlSchemaNamespace = "http://www.w3.org/2001/XMLSchema";

        /// <summary>
        /// Constructor.
        /// </summary>
        protected UxmlAttributeDescription()
        {
            use = Use.Optional;
            restriction = null;
        }

        /// <summary>
        /// An enum to describe attribute use.
        /// </summary>
        public enum Use
        {
            /// <summary>
            /// There is no restriction on the use of this attribute with the element.
            /// </summary>
            None = 0,
            /// <summary>
            /// The attribute is optional for the element.
            /// </summary>
            Optional = 1,
            /// <summary>
            /// The attribute should not appear for the element.
            /// </summary>
            Prohibited = 2,
            /// <summary>
            /// The attribute must appear in the element tag.
            /// </summary>
            Required = 3
        }

        /// <summary>
        /// The attribute name.
        /// </summary>
        public string name { get; set; }

        string[] m_ObsoleteNames;

        /// <summary>
        /// A list of obsolete names for this attribute.
        /// </summary>
        public IEnumerable<string> obsoleteNames
        {
            get { return m_ObsoleteNames; }
            set { m_ObsoleteNames = value.ToArray(); }
        }

        /// <summary>
        /// Attribute type.
        /// </summary>
        public string type { get; protected set; }
        /// <summary>
        /// Attribute namespace.
        /// </summary>
        public string typeNamespace { get; protected set; }
        /// <summary>
        /// The default value for the attribute, as a string.
        /// </summary>
        public abstract string defaultValueAsString { get; }
        /// <summary>
        /// Whether the attribute is optional, required or prohibited.
        /// </summary>
        public Use use { get; set; }
        /// <summary>
        /// Restrictions on the possible values of the attribute.
        /// </summary>
        public UxmlTypeRestriction restriction { get; set; }

        internal bool TryFindValueInAttributeOverrides(string elementName, List<TemplateAsset.AttributeOverride> attributeOverrides, out string value)
        {
            value = null;
            foreach (TemplateAsset.AttributeOverride attributeOverride in attributeOverrides)
            {
                if (attributeOverride.m_ElementName != elementName)
                    continue;

                if (attributeOverride.m_AttributeName != name)
                {
                    if (m_ObsoleteNames != null)
                    {
                        bool matchedObsoleteName = false;
                        for (var j = 0; j < m_ObsoleteNames.Length; j++)
                        {
                            if (attributeOverride.m_AttributeName == m_ObsoleteNames[j])
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

                value = attributeOverride.m_Value;
                return true;
            }
            return false;
        }

        internal bool TryGetValueFromBagAsString(IUxmlAttributes bag, CreationContext cc, out string value)
        {
            return TryGetValueFromBagAsString(bag, cc, out value, out _);
        }

        // This method is necessary for attributes which may need to do further processing on the attribute value
        // And thus require from which VisualTreeAsset instance was the attribute extracted
        internal bool TryGetValueFromBagAsString(IUxmlAttributes bag, CreationContext cc, out string value, out VisualTreeAsset sourceAsset)
        {
            value = null;
            sourceAsset = null;

            // Regardless of whether the attribute is overridden or not, we want to error here
            // if there is no valid name.
            if (name == null && (m_ObsoleteNames == null || m_ObsoleteNames.Length == 0))
            {
                Debug.LogError("Attribute description has no name.");
                return false;
            }

            string elementName;
            bag.TryGetAttributeValue("name", out elementName);
            if (!string.IsNullOrEmpty(elementName) && cc.attributeOverrides != null)
            {
                foreach (CreationContext.AttributeOverrideRange attributeOverrideRange in cc.attributeOverrides)
                {
                    if (TryFindValueInAttributeOverrides(elementName, attributeOverrideRange.attributeOverrides, out value))
                    {
                        sourceAsset = attributeOverrideRange.sourceAsset;
                        return true;
                    }
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
                        sourceAsset = cc.visualTreeAsset;
                        return true;
                    }
                }
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
                            sourceAsset = cc.visualTreeAsset;
                            return true;
                        }
                    }
                }
                return false;
            }
            sourceAsset = cc.visualTreeAsset;
            return true;
        }

        /// <summary>
        /// Tries to get the attribute value from the attribute bag.
        /// </summary>
        /// <param name="bag">A bag contains attributes and their values as strings.</param>
        /// <param name="cc">The context in which the values are retrieved.</param>
        /// <param name="converterFunc">A function to convert a string value to type T.</param>
        /// <param name="defaultValue">The value to return if the attribute is not found in the bag.</param>
        /// <param name="value">If the attribute could be retrieved, the retrieved value converted by the conversion function or the default value if the retrieved value could not de converted.</param>
        /// <returns>True if the value is found, false otherwise.</returns>
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

        /// <summary>
        /// Get the attribute value from the attribute bag.
        /// </summary>
        /// <param name="bag">A bag containing attributes and their values as strings.</param>
        /// <param name="cc">The context in which the values are retrieved.</param>
        /// <param name="converterFunc">A function to convert a string value to type T.</param>
        /// <param name="defaultValue">The value to return if the attribute is not found in the bag.</param>
        /// <remarks>
        /// The attribute is looked up using <see cref="name"/>. If it is not found, each <see cref="obsoleteNames"/> is tried in turn, from first to last.
        /// </remarks>
        /// <returns>The attribute value from the bag, or defaultValue if the attribute is not found.</returns>
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

    /// <summary>
    /// Base class for all the UXML specific attributes.
    /// </summary>
    public abstract class TypedUxmlAttributeDescription<T> : UxmlAttributeDescription
    {
        /// <summary>
        /// Use this method to obtain the actual value of the attribute.
        /// </summary>
        /// <param name="bag">The bag of attributes where to get the actual value.</param>
        /// <param name="cc">The creation context.</param>
        /// <returns>The value of type T.</returns>
        public abstract T GetValueFromBag(IUxmlAttributes bag, CreationContext cc);

        /// <summary>
        /// The default value to be used for that specific attribute.
        /// </summary>
        public T defaultValue { get; set; }

        /// <summary>
        /// The string representation of the default value of the UXML attribute.
        /// </summary>
        public override string defaultValueAsString { get { return defaultValue.ToString(); } }
    }

    /// <summary>
    /// Describes a UXML <c>string</c> attribute.
    /// </summary>
    public class UxmlStringAttributeDescription : TypedUxmlAttributeDescription<string>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UxmlStringAttributeDescription()
        {
            type = "string";
            typeNamespace = xmlSchemaNamespace;
            defaultValue = "";
        }

        /// <summary>
        /// The default value for the attribute, as a string.
        /// </summary>
        public override string defaultValueAsString { get { return defaultValue; } }

        /// <summary>
        /// Retrieves the value of this attribute from the attribute bag. Returns the value if found; otherwise, it returns <see cref="defaultValue"/>.
        /// </summary>
        /// <param name="bag">The bag of attributes.</param>
        /// <param name="cc">The context in which the values are retrieved.</param>
        /// <returns>The value of the attribute.</returns>
        public override string GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            return GetValueFromBag(bag, cc, (s, t) => s, defaultValue);
        }

        /// <summary>
        /// Attempts to retrieve the value of this attribute from the attribute bag and returns true if found, otherwise false.
        /// </summary>
        /// <param name="bag">The bag of attributes.</param>
        /// <param name="cc">The context in which the values are retrieved.</param>
        /// <param name="value">The value of the attribute.</param>
        /// <returns>True if the value is found, false otherwise.</returns>
        public bool TryGetValueFromBag(IUxmlAttributes bag, CreationContext cc, ref string value)
        {
            return TryGetValueFromBag(bag, cc, (s, t) => s , defaultValue, ref value);
        }
    }

    /// <summary>
    /// Describes a UXML <c>float</c> attribute.
    /// </summary>
    public class UxmlFloatAttributeDescription : TypedUxmlAttributeDescription<float>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UxmlFloatAttributeDescription()
        {
            type = "float";
            typeNamespace = xmlSchemaNamespace;
            defaultValue = 0.0f;
        }

        /// <summary>
        /// The default value for the attribute, as a string.
        /// </summary>
        public override string defaultValueAsString { get { return defaultValue.ToString(CultureInfo.InvariantCulture.NumberFormat); } }

        /// <summary>
        /// Retrieves the value of this attribute from the attribute bag. Returns the value if found; otherwise, it returns <see cref="defaultValue"/>.
        /// </summary>
        /// <param name="bag">The bag of attributes.</param>
        /// <param name="cc">The context in which the values are retrieved.</param>
        /// <returns>The value of the attribute.</returns>
        public override float GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            return GetValueFromBag(bag, cc, (s, f) => ConvertValueToFloat(s, f), defaultValue);
        }

        /// <summary>
        /// Attempts to retrieve the value of this attribute from the attribute bag and returns true if found, otherwise false.
        /// </summary>
        /// <param name="bag">The bag of attributes.</param>
        /// <param name="cc">The context in which the values are retrieved.</param>
        /// <param name="value">The value of the attribute.</param>
        /// <returns>True if the value is found, false otherwise.</returns>
        public bool TryGetValueFromBag(IUxmlAttributes bag, CreationContext cc, ref float value)
        {
            return TryGetValueFromBag(bag, cc, (s, f) => ConvertValueToFloat(s, f), defaultValue, ref value);
        }

        static float ConvertValueToFloat(string v, float defaultValue)
        {
            float l;
            if (v == null || !float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out l))
                return defaultValue;
            return l;
        }
    }

    /// <summary>
    /// Describes a UXML <c>double</c> attribute.
    /// </summary>
    public class UxmlDoubleAttributeDescription : TypedUxmlAttributeDescription<double>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UxmlDoubleAttributeDescription()
        {
            type = "double";
            typeNamespace = xmlSchemaNamespace;
            defaultValue = 0.0;
        }

        /// <summary>
        /// The default value for the attribute, as a string.
        /// </summary>
        public override string defaultValueAsString { get { return defaultValue.ToString(CultureInfo.InvariantCulture.NumberFormat); } }

        /// <summary>
        /// Retrieves the value of this attribute from the attribute bag. Returns the value if found; otherwise, it returns <see cref="defaultValue"/>.
        /// </summary>
        /// <param name="bag">The bag of attributes.</param>
        /// <param name="cc">The context in which the values are retrieved.</param>
        /// <returns>The value of the attribute.</returns>
        public override double GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            return GetValueFromBag(bag, cc, (s, d) => ConvertValueToDouble(s, d), defaultValue);
        }

        /// <summary>
        /// Attempts to retrieve the value of this attribute from the attribute bag and returns true if found, otherwise false.
        /// </summary>
        /// <param name="bag">The bag of attributes.</param>
        /// <param name="cc">The context in which the values are retrieved.</param>
        /// <param name="value">The value of the attribute.</param>
        /// <returns>True if the value is found, false otherwise.</returns>
        public bool TryGetValueFromBag(IUxmlAttributes bag, CreationContext cc, ref double value)
        {
            return TryGetValueFromBag(bag, cc, (s, d) => ConvertValueToDouble(s, d), defaultValue, ref value);
        }

        static double ConvertValueToDouble(string v, double defaultValue)
        {
            double l;
            if (v == null || !double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out l))
                return defaultValue;
            return l;
        }
    }

    /// <summary>
    /// Describes a UXML <c>int</c> attribute.
    /// </summary>
    public class UxmlIntAttributeDescription : TypedUxmlAttributeDescription<int>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UxmlIntAttributeDescription()
        {
            type = "int";
            typeNamespace = xmlSchemaNamespace;
            defaultValue = 0;
        }

        /// <summary>
        /// The default value for the attribute, as a string.
        /// </summary>
        public override string defaultValueAsString { get { return defaultValue.ToString(CultureInfo.InvariantCulture.NumberFormat); } }

        /// <summary>
        /// Retrieves the value of this attribute from the attribute bag. Returns the value if found; otherwise, it returns <see cref="defaultValue"/>.
        /// </summary>
        /// <param name="bag">The bag of attributes.</param>
        /// <param name="cc">The context in which the values are retrieved.</param>
        /// <returns>The value of the attribute.</returns>
        public override int GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            return GetValueFromBag(bag, cc, (s, i) => ConvertValueToInt(s, i), defaultValue);
        }

        /// <summary>
        /// Attempts to retrieve the value of this attribute from the attribute bag and returns true if found, otherwise false.
        /// </summary>
        /// <param name="bag">The bag of attributes.</param>
        /// <param name="cc">The context in which the values are retrieved.</param>
        /// <param name="value">The value of the attribute.</param>
        /// <returns>True if the value is found, false otherwise.</returns>
        public bool TryGetValueFromBag(IUxmlAttributes bag, CreationContext cc, ref int value)
        {
            return TryGetValueFromBag(bag, cc, (s, i) => ConvertValueToInt(s, i), defaultValue, ref value);
        }

        static int ConvertValueToInt(string v, int defaultValue)
        {
            int l;
            if (v == null || !int.TryParse(v, out l))
                return defaultValue;

            return l;
        }
    }

    /// <summary>
    /// Describes a UXML <c>uint</c> attribute.
    /// </summary>
    public class UxmlUnsignedIntAttributeDescription : TypedUxmlAttributeDescription<uint>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UxmlUnsignedIntAttributeDescription()
        {
            type = "unsignedInt";
            typeNamespace = xmlSchemaNamespace;
            defaultValue = 0;
        }

        /// <summary>
        /// The default value for the attribute, as a string.
        /// </summary>
        public override string defaultValueAsString { get { return defaultValue.ToString(CultureInfo.InvariantCulture.NumberFormat); } }

        /// <summary>
        /// Retrieves the value of this attribute from the attribute bag. Returns the value if found; otherwise, it returns <see cref="defaultValue"/>.
        /// </summary>
        /// <param name="bag">The bag of attributes.</param>
        /// <param name="cc">The context in which the values are retrieved.</param>
        /// <returns>The value of the attribute.</returns>
        public override uint GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            return GetValueFromBag(bag, cc, (s, i) => ConvertValueToUInt(s, i), defaultValue);
        }

        /// <summary>
        /// Attempts to retrieve the value of this attribute from the attribute bag and returns true if found, otherwise false.
        /// </summary>
        /// <param name="bag">The bag of attributes.</param>
        /// <param name="cc">The context in which the values are retrieved.</param>
        /// <param name="value">The value of the attribute.</param>
        /// <returns>True if the value is found, false otherwise.</returns>
        public bool TryGetValueFromBag(IUxmlAttributes bag, CreationContext cc, ref uint value)
        {
            return TryGetValueFromBag(bag, cc, (s, i) => ConvertValueToUInt(s, i), defaultValue, ref value);
        }

        static uint ConvertValueToUInt(string v, uint defaultValue)
        {
            if (v == null || !uint.TryParse(v, out var l))
                return defaultValue;

            return l;
        }
    }

    /// <summary>
    /// Describes a UXML <c>ulong</c> attribute.
    /// </summary>
    public class UxmlUnsignedLongAttributeDescription : TypedUxmlAttributeDescription<ulong>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UxmlUnsignedLongAttributeDescription()
        {
            type = "unsignedLong";
            typeNamespace = xmlSchemaNamespace;
            defaultValue = 0;
        }

        /// <summary>
        /// The default value for the attribute, as a string.
        /// </summary>
        public override string defaultValueAsString { get { return defaultValue.ToString(CultureInfo.InvariantCulture.NumberFormat); } }

        /// <summary>
        /// Retrieves the value of this attribute from the attribute bag. Returns the value if found; otherwise, it returns <see cref="defaultValue"/>.
        /// </summary>
        /// <param name="bag">The bag of attributes.</param>
        /// <param name="cc">The context in which the values are retrieved.</param>
        /// <returns>The value of the attribute.</returns>
        public override ulong GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            return GetValueFromBag(bag, cc, (s, l) => ConvertValueToUlong(s, l), defaultValue);
        }

        /// <summary>
        /// Attempts to retrieve the value of this attribute from the attribute bag and returns true if found, otherwise false.
        /// </summary>
        /// <param name="bag">The bag of attributes.</param>
        /// <param name="cc">The context in which the values are retrieved.</param>
        /// <param name="value">The value of the attribute.</param>
        /// <returns>True if the value is found, false otherwise.</returns>
        public bool TryGetValueFromBag(IUxmlAttributes bag, CreationContext cc, ref ulong value)
        {
            return TryGetValueFromBag(bag, cc, (s, l) => ConvertValueToUlong(s, l), defaultValue, ref value);
        }

        static ulong ConvertValueToUlong(string v, ulong defaultValue)
        {
            ulong l;
            if (v == null || !ulong.TryParse(v, out l))
                return defaultValue;
            return l;
        }
    }

    /// <summary>
    /// Describes a UXML <c>long</c> attribute.
    /// </summary>
    public class UxmlLongAttributeDescription : TypedUxmlAttributeDescription<long>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UxmlLongAttributeDescription()
        {
            type = "long";
            typeNamespace = xmlSchemaNamespace;
            defaultValue = 0;
        }

        /// <summary>
        /// The default value for the attribute, as a string.
        /// </summary>
        public override string defaultValueAsString { get { return defaultValue.ToString(CultureInfo.InvariantCulture.NumberFormat); } }

        /// <summary>
        /// Retrieves the value of this attribute from the attribute bag. Returns the value if found; otherwise, it returns <see cref="defaultValue"/>.
        /// </summary>
        /// <param name="bag">The bag of attributes.</param>
        /// <param name="cc">The context in which the values are retrieved.</param>
        /// <returns>The value of the attribute.</returns>
        public override long GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            return GetValueFromBag(bag, cc, (s, l) => ConvertValueToLong(s, l), defaultValue);
        }

        /// <summary>
        /// Attempts to retrieve the value of this attribute from the attribute bag and returns true if found, otherwise false.
        /// </summary>
        /// <param name="bag">The bag of attributes.</param>
        /// <param name="cc">The context in which the values are retrieved.</param>
        /// <param name="value">The value of the attribute.</param>
        /// <returns>True if the value is found, false otherwise.</returns>
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

    /// <summary>
    /// Describes a UXML <c>bool</c> attribute.
    /// </summary>
    public class UxmlBoolAttributeDescription : TypedUxmlAttributeDescription<bool>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UxmlBoolAttributeDescription()
        {
            type = "boolean";
            typeNamespace = xmlSchemaNamespace;
            defaultValue = false;
        }

        /// <summary>
        /// The default value for the attribute, as a string.
        /// </summary>
        public override string defaultValueAsString { get { return defaultValue.ToString().ToLower(); } }

        /// <summary>
        /// Retrieves the value of this attribute from the attribute bag. Returns the value if found; otherwise, it returns <see cref="defaultValue"/>.
        /// </summary>
        /// <param name="bag">The bag of attributes.</param>
        /// <param name="cc">The context in which the values are retrieved.</param>
        /// <returns>The value of the attribute.</returns>
        public override bool GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            return GetValueFromBag(bag, cc, (s, b) => ConvertValueToBool(s, b), defaultValue);
        }

        /// <summary>
        /// Attempts to retrieve the value of this attribute from the attribute bag and returns true if found, otherwise false.
        /// </summary>
        /// <param name="bag">The bag of attributes.</param>
        /// <param name="cc">The context in which the values are retrieved.</param>
        /// <param name="value">The value of the attribute.</param>
        /// <returns>True if the value is found, false otherwise.</returns>
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

    /// <summary>
    /// Describes a UXML attribute representing a <see cref="Color"/> as a <c>string</c>.
    /// </summary>
    public class UxmlColorAttributeDescription : TypedUxmlAttributeDescription<Color>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UxmlColorAttributeDescription()
        {
            type = "string";
            typeNamespace = xmlSchemaNamespace;
            defaultValue = new Color(0, 0, 0, 1);
        }

        /// <summary>
        /// The default value for the attribute, as a string.
        /// </summary>
        public override string defaultValueAsString { get { return defaultValue.ToString(); } }

        /// <summary>
        /// Retrieves the value of this attribute from the attribute bag. Returns the value if found; otherwise, it returns <see cref="defaultValue"/>.
        /// </summary>
        /// <param name="bag">The bag of attributes.</param>
        /// <param name="cc">The context in which the values are retrieved.</param>
        /// <returns>The value of the attribute.</returns>
        public override Color GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            return GetValueFromBag(bag, cc, (s, color) => ConvertValueToColor(s, color), defaultValue);
        }

        /// <summary>
        /// Attempts to retrieve the value of this attribute from the attribute bag and returns true if found, otherwise false.
        /// </summary>
        /// <param name="bag">The bag of attributes.</param>
        /// <param name="cc">The context in which the values are retrieved.</param>
        /// <param name="value">The value of the attribute.</param>
        /// <returns>True if the value is found, false otherwise.</returns>
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

    /// <summary>
    /// Describes an XML <c>System.Type</c> attribute.
    /// </summary>
    public class UxmlTypeAttributeDescription<TBase> : TypedUxmlAttributeDescription<Type>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UxmlTypeAttributeDescription()
        {
            type = "string";
            typeNamespace = xmlSchemaNamespace;
            defaultValue = null;
        }

        /// <summary>
        /// The default value for the attribute, as a string.
        /// </summary>
        public override string defaultValueAsString
        {
            get { return defaultValue == null ? "null" : defaultValue.FullName; }
        }

        /// <summary>
        /// Method that retrieves an attribute's value from an attribute bag. Returns it if it is found, otherwise return <see cref="defaultValue"/>.
        /// </summary>
        /// <param name="bag">The attribute bag.</param>
        /// <param name="cc">The context in which the method retrieves attribute values.</param>
        /// <returns>The attribute's value. If the method cannot find the value, returns <see cref="defaultValue"/>.</returns>
        public override Type GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            return GetValueFromBag(bag, cc, (s, type1) => ConvertValueToType(s, type1), defaultValue);
        }

        /// <summary>
        /// Method that tries to retrieve an attribute's value from an attribute bag.. Returns true if it is found, otherwise returns false.
        /// </summary>
        /// <param name="bag">The attribute bag.</param>
        /// <param name="cc">The context in which the method retrieves attribute values.</param>
        /// <param name="value">The attribute's value.</param>
        /// <returns>True if the method can retrieve the attribute's value. False otherwise.</returns>
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

    /// <summary>
    /// Describes a UXML attribute representing an <c>enum</c> as a <c>string</c>.
    /// </summary>
    public class UxmlEnumAttributeDescription<T> : TypedUxmlAttributeDescription<T> where T : struct, IConvertible
    {
        /// <summary>
        /// Constructor.
        /// </summary>
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

        /// <summary>
        /// The default value for the attribute, as a string.
        /// </summary>
        public override string defaultValueAsString { get { return defaultValue.ToString(CultureInfo.InvariantCulture.NumberFormat); } }

        /// <summary>
        /// Retrieves the value of this attribute from the attribute bag. Returns the value if found; otherwise, it returns <see cref="defaultValue"/>.
        /// </summary>
        /// <param name="bag">The bag of attributes.</param>
        /// <param name="cc">The context in which the values are retrieved.</param>
        /// <returns>The value of the attribute.</returns>
        public override T GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            return GetValueFromBag(bag, cc, (s, convertible) => ConvertValueToEnum(s, convertible), defaultValue);
        }

        /// <summary>
        /// Attempts to retrieve the value of this attribute from the attribute bag and returns true if found, otherwise false.
        /// </summary>
        /// <param name="bag">The bag of attributes.</param>
        /// <param name="cc">The context in which the values are retrieved.</param>
        /// <param name="value">The value of the attribute.</param>
        /// <returns>True if the value is found, false otherwise.</returns>
        public bool TryGetValueFromBag(IUxmlAttributes bag, CreationContext cc, ref T value)
        {
            return TryGetValueFromBag(bag, cc, (s, convertible) => ConvertValueToEnum(s, convertible), defaultValue, ref value);
        }

        static U ConvertValueToEnum<U>(string v, U defaultValue)
            where U : struct
        {
            try
            {
                if (string.IsNullOrEmpty(v))
                    return defaultValue;

                return (U) Enum.Parse(typeof(U), v, true);
            }
            catch (ArgumentException)
            {
                Debug.LogError(GetEnumNameErrorMessage(v, typeof(U)));
            }
            catch (OverflowException)
            {
                Debug.LogError(GetEnumRangeErrorMessage(v, typeof(U)));
            }
            return defaultValue;
        }

        static string GetEnumNameErrorMessage(string v, Type enumType)
        {
            return $"The {enumType.Name} enum does not contain the value `{v}`. Value must be in range [{string.Join(" | ", Enum.GetNames(enumType))}].";
        }

        static string GetEnumRangeErrorMessage(string v, Type enumType)
        {
            return $"{v} is outside of the range of possible values for the {enumType.Name} enum.";
        }
    }

    /// <summary>
    /// Describes a UXML <c>Hash128</c> attribute.
    /// </summary>
    public class UxmlHash128AttributeDescription : TypedUxmlAttributeDescription<Hash128>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UxmlHash128AttributeDescription()
        {
            type = "string";
            typeNamespace = xmlSchemaNamespace;
            defaultValue = new Hash128();
        }

        /// <summary>
        /// The default value for the attribute, as a string.
        /// </summary>
        public override string defaultValueAsString { get { return defaultValue.ToString(); } }

        /// <summary>
        /// Retrieves the value of this attribute from the attribute bag. Returns the value if found; otherwise, it returns <see cref="defaultValue"/>.
        /// </summary>
        /// <param name="bag">The bag of attributes.</param>
        /// <param name="cc">The context in which the values are retrieved.</param>
        /// <returns>The value of the attribute.</returns>
        public override Hash128 GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            return GetValueFromBag(bag, cc, (s, i) => i = Hash128.Parse(s), defaultValue);
        }

        /// <summary>
        /// Attempts to retrieve the value of this attribute from the attribute bag and returns true if found, otherwise false.
        /// </summary>
        /// <param name="bag">The bag of attributes.</param>
        /// <param name="cc">The context in which the values are retrieved.</param>
        /// <param name="value">The value of the attribute.</param>
        /// <returns>True if the value is found, false otherwise.</returns>
        public bool TryGetValueFromBag(IUxmlAttributes bag, CreationContext cc, ref Hash128 value)
        {
            return TryGetValueFromBag(bag, cc, (s, i) => i = Hash128.Parse(s), defaultValue, ref value);
        }
    }

    /// <summary>
    /// Describes a UXML object defined as a child in Uxml. The UXML object then has a set of attributes.
    /// </summary>
    /// <typeparam name="T">Type of the created UXML object.</typeparam>
    /// <remarks>UXML objects are defined using <see cref="UxmlObjectFactory{TCreatedType,TTraits}"/> and <see cref="UxmlObjectTraits{T}"/> on a simple class or structure.</remarks>
    internal class UxmlObjectAttributeDescription<T> where T : new()
    {
        /// <summary>
        /// The default value to be used for that specific attribute.
        /// </summary>
        public T defaultValue { get; set; }

        /// <summary>
        /// Retrieves the value of this attribute from the attribute bag. Returns the value if found; otherwise, it returns <see cref="defaultValue"/>.
        /// </summary>
        /// <param name="bag">The bag of attributes.</param>
        /// <param name="cc">The context in which the values are retrieved.</param>
        /// <returns>The initialized UXML object.</returns>
        public virtual T GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            var uxmlObjects = cc.visualTreeAsset?.GetUxmlObjects<T>(bag, cc);
            if (uxmlObjects != null)
            {
                foreach (var child in uxmlObjects)
                {
                    return child;
                }
            }

            return defaultValue;
        }
    }

    /// <summary>
    /// Describes a list of UXML objects defined as children in UXML. Each UXML object then has a set of attributes.
    /// </summary>
    /// <typeparam name="T">Type of the created UXML objects.</typeparam>
    /// <remarks>UXML objects are defined using <see cref="UxmlObjectFactory{TCreatedType,TTraits}"/> and <see cref="UxmlObjectTraits{T}"/> on a simple class or structure.</remarks>
    internal class UxmlObjectListAttributeDescription<T> : UxmlObjectAttributeDescription<List<T>> where T : new()
    {
        /// <summary>
        /// Retrieves the value of this attribute from the attribute bag. Returns the value if found; otherwise, it returns <see cref="defaultValue"/>.
        /// </summary>
        /// <param name="bag">The bag of attributes.</param>
        /// <param name="cc">The context in which the values are retrieved.</param>
        /// <returns>The list of initialized UXML objects.</returns>
        public override List<T> GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            var uxmlObjects = cc.visualTreeAsset?.GetUxmlObjects<T>(bag, cc);
            if (uxmlObjects != null)
            {
                List<T> list = null;

                foreach (var child in uxmlObjects)
                {
                    if (list == null)
                        list = new List<T>();

                    list.Add(child);
                }

                return list;
            }

            return defaultValue;
        }
    }
}
