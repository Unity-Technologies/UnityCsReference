// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System.IO;

namespace UnityEditor.Search
{
    readonly struct PropertyRange
    {
        public readonly float min;
        public readonly float max;

        public PropertyRange(float min, float max)
        {
            this.min = min;
            this.max = max;
        }

        public bool Contains(float f)
        {
            if (f >= min && f <= max)
                return true;
            return false;
        }
    }

    readonly struct SearchColor : IEquatable<SearchColor>, IComparable<SearchColor>
    {
        public readonly byte r;
        public readonly byte g;
        public readonly byte b;
        public readonly byte a;

        public SearchColor(Color c)
        {
            r = (byte)Mathf.RoundToInt(c.r * 255f);
            g = (byte)Mathf.RoundToInt(c.g * 255f);
            b = (byte)Mathf.RoundToInt(c.b * 255f);
            a = (byte)Mathf.RoundToInt(c.a * 255f);
        }

        public SearchColor(byte r, byte g, byte b, byte a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public byte this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return r;
                    case 1: return g;
                    case 2: return b;
                    case 3: return a;
                    default:
                        throw new IndexOutOfRangeException("Invalid Color index(" + index + ")!");
                }
            }
        }

        public bool Equals(SearchColor other)
        {
            for (var i = 0; i < 4; ++i)
            {
                if (this[i] != other[i])
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            if (obj is SearchColor ic)
                return base.Equals(ic);
            return false;
        }

        public override int GetHashCode()
        {
            return r.GetHashCode() ^ (g.GetHashCode() << 2) ^ (b.GetHashCode() >> 2) ^ (a.GetHashCode() >> 1);
        }

        public int CompareTo(SearchColor other)
        {
            for (var i = 0; i < 4; ++i)
            {
                if (this[i] > other[i])
                    return 1;
                if (this[i] < other[i])
                    return -1;
            }

            return 0;
        }

        public static bool operator==(SearchColor lhs, SearchColor rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator!=(SearchColor lhs, SearchColor rhs)
        {
            return !lhs.Equals(rhs);
        }

        public static bool operator>(SearchColor lhs, SearchColor rhs)
        {
            return lhs.CompareTo(rhs) > 0;
        }

        public static bool operator<(SearchColor lhs, SearchColor rhs)
        {
            return lhs.CompareTo(rhs) < 0;
        }

        public static bool operator>=(SearchColor lhs, SearchColor rhs)
        {
            return lhs.CompareTo(rhs) >= 0;
        }

        public static bool operator<=(SearchColor lhs, SearchColor rhs)
        {
            return lhs.CompareTo(rhs) <= 0;
        }

        public override string ToString()
        {
            return $"RGBA({r}, {g}, {b}, {a})";
        }
    }

    public readonly struct SearchValue
    {
        public enum ValueType : byte
        {
            Nil = 0,
            Bool,
            Number,
            Text,
            Color,
            Enum,
            Object,
            Vector2,
            Vector3,
            Vector4
        }

        public readonly ValueType type;
        [Obsolete("Change type to a float. Please use floatNumber instead.", false)]
        public readonly double number = double.NaN;
        public readonly float floatNumber;
        public readonly string text;
        internal readonly Vector4 v4;
        internal readonly SearchColor color;

        public bool boolean => type == ValueType.Bool && floatNumber == 1d;
        public bool valid => type != ValueType.Nil;

        public static SearchValue invalid = new SearchValue();

        public SearchValue(bool v)
        {
            this.type = ValueType.Bool;
            this.floatNumber = v ? 1f : 0f;
            this.text = null;
            this.color = default;
            this.v4 = Vector4.zero;
        }

        public SearchValue(float number)
        {
            this.type = ValueType.Number;
            this.floatNumber = number;
            this.text = null;
            this.color = default;
            this.v4 = Vector4.zero;
        }
        
        public SearchValue(double number)
        {
            this.type = ValueType.Number;
            this.floatNumber = (float)number;
            this.text = null;
            this.color = default;
            this.v4 = Vector4.zero;
        }

        public SearchValue(string text)
        {
            this.type = ValueType.Text;
            this.floatNumber = float.NaN;
            this.text = text;
            this.color = default;
            this.v4 = Vector4.zero;
        }

        public SearchValue(Color color)
        {
            this.type = ValueType.Color;
            this.floatNumber = float.NaN;
            this.text = null;
            this.color = new SearchColor(color);
            this.v4 = Vector4.zero;
        }

        internal SearchValue(SearchColor color)
        {
            this.type = ValueType.Color;
            this.floatNumber = float.NaN;
            this.text = null;
            this.color = color;
            this.v4 = Vector4.zero;
        }

        internal SearchValue(Vector4 v, int dim)
        {
            this.type = dim == 2 ? ValueType.Vector2 : (dim == 3 ? ValueType.Vector3 : ValueType.Vector4);
            this.floatNumber = float.NaN;
            this.text = null;
            this.color = default;
            this.v4 = v;
        }

        internal SearchValue(UnityEngine.Object obj)
        {
            this.type = ValueType.Object;
            this.text = SearchUtils.GetObjectPath(obj);
            this.floatNumber = float.NaN;
            this.color = default;
            this.v4 = default;
        }

        internal SearchValue(UnityEngine.Object obj, in string path)
        {
            this.type = ValueType.Object;
            this.text = obj ? SearchUtils.GetObjectPath(obj) : path;
            this.floatNumber = float.NaN;
            this.color = default;
            this.v4 = default;
        }

        internal SearchValue(int enumIntegerValue, string enumStringValue)
        {
            this.type = ValueType.Enum;
            this.floatNumber = enumIntegerValue;
            this.text = enumStringValue;
            this.color = default;
            this.v4 = default;
        }

        public SearchValue(object v)
        {
            this.type = ValueType.Nil;
            this.floatNumber = float.NaN;
            this.text = null;
            this.color = default;
            this.v4 = Vector4.zero;
            if (v is bool b)
            {
                this.type = ValueType.Bool;
                this.floatNumber = b ? 1 : 0;
            }
            else if (v is string s)
            {
                this.type = ValueType.Text;
                this.text = s;
            }
            else if (v is Color c)
            {
                this.type = ValueType.Color;
                this.color = new SearchColor(c);
            }
            else if (Utils.TryGetFloat(v, out var f))
            {
                this.type = ValueType.Number;
                this.floatNumber = f;
            }
            else if (v is Vector2 v2)
            {
                this.type = ValueType.Vector2;
                this.v4 = v2;
            }
            else if (v is Vector2 v3)
            {
                this.type = ValueType.Vector3;
                this.v4 = v3;
            }
            else if (v is Vector2 v4)
            {
                this.type = ValueType.Vector4;
                this.v4 = v4;
            }
            else if (v is UnityEngine.Object obj)
            {
                this.type = ValueType.Object;
                this.text = SearchUtils.GetObjectPath(obj);
            }
            else if (v != null)
            {
                this.type = ValueType.Text;
                this.text = v.ToString();
            }
        }

        public override string ToString()
        {
            switch (type)
            {
                case ValueType.Bool: return $"{boolean} [{type}]";
                case ValueType.Number: return $"{floatNumber} [{type}]";
                case ValueType.Text: return $"{text} [{type}]";
                case ValueType.Color: return $"{color} [{type}]";
                case ValueType.Vector2:
                case ValueType.Vector3:
                case ValueType.Vector4:
                    return $"{v4} [{type}]";
                case ValueType.Object:
                    return $"{text} [{type}]";
                case ValueType.Enum:
                    return $"{floatNumber} ({text}) [{type}]";
            }

            return "nil";
        }

        internal static bool IsSearchableProperty(in SerializedPropertyType type)
        {
            switch (type)
            {
                case SerializedPropertyType.Generic:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.AnimationCurve:
                case SerializedPropertyType.Gradient:
                case SerializedPropertyType.ExposedReference:
                case SerializedPropertyType.FixedBufferSize:
                case SerializedPropertyType.ManagedReference:
                    return false;
                default:
                    return true;
            }
        }

        public static SearchValue ConvertPropertyValue(in SerializedProperty sp)
        {
            switch (sp.propertyType)
            {
                case SerializedPropertyType.ArraySize:
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Character:
                    var managedType = sp.GetManagedType();
                    if (managedType != null && managedType.IsEnum)
                        return new SearchValue(sp.intValue, PropertySelectors.GetEnumValue(managedType, sp));
                    return new SearchValue(Convert.ToSingle(sp.intValue));

                case SerializedPropertyType.Boolean: return new SearchValue(sp.boolValue);
                case SerializedPropertyType.Float: return new SearchValue(sp.floatValue);
                case SerializedPropertyType.String: return new SearchValue(sp.stringValue);

                case SerializedPropertyType.Enum:
                    managedType = sp.GetManagedType();
                    if (managedType != null && managedType.IsEnum)
                        return new SearchValue(sp.intValue, PropertySelectors.GetEnumValue(managedType, sp));
                    return new SearchValue(sp.enumValueIndex, PropertySelectors.GetEnumValue(sp));

                case SerializedPropertyType.ObjectReference:
                    return new SearchValue(sp.objectReferenceValue);

                case SerializedPropertyType.Bounds: return new SearchValue(sp.boundsValue.size.magnitude);
                case SerializedPropertyType.BoundsInt: return new SearchValue(sp.boundsIntValue.size.magnitude);
                case SerializedPropertyType.Rect: return new SearchValue(new Vector4(sp.rectValue.x, sp.rectValue.y, sp.rectValue.width, sp.rectValue.height), 4);
                case SerializedPropertyType.RectInt: return new SearchValue(new Vector4(sp.rectIntValue.x, sp.rectIntValue.y, sp.rectIntValue.width, sp.rectIntValue.height), 4);
                case SerializedPropertyType.Color: return new SearchValue(sp.colorValue);
                case SerializedPropertyType.Vector2: return new SearchValue(new Vector4(sp.vector2Value.x, sp.vector2Value.y), 2);
                case SerializedPropertyType.Vector3: return new SearchValue(new Vector4(sp.vector3Value.x, sp.vector3Value.y, sp.vector3Value.z), 3);
                case SerializedPropertyType.Vector4: return new SearchValue(new Vector4(sp.vector4Value.x, sp.vector4Value.y, sp.vector4Value.z, sp.vector4Value.w), 4);
                case SerializedPropertyType.Quaternion:
                    {
                        var euler = sp.quaternionValue.eulerAngles;
                        return new SearchValue(new Vector4(euler.x, euler.y, euler.z), 3);
                    }
                case SerializedPropertyType.Vector2Int: return new SearchValue(new Vector4(sp.vector2IntValue.x, sp.vector2IntValue.y), 2);
                case SerializedPropertyType.Vector3Int: return new SearchValue(new Vector4(sp.vector3IntValue.x, sp.vector3IntValue.y, sp.vector3IntValue.z), 3);

                case SerializedPropertyType.Hash128:
                    return new SearchValue(sp.hash128Value.ToString());
            }

            if (sp.isArray)
                return new SearchValue(sp.arraySize);

            return SearchValue.invalid;
        }

        public static void SetupEngine<T>(QueryEngine<T> queryEngine)
        {
            queryEngine.AddOperatorHandler(":", (SearchValue v, PropertyRange range) => PropertyRangeCompare(SearchIndexOperator.Contains, v, range));
            queryEngine.AddOperatorHandler("=", (SearchValue v, PropertyRange range) => PropertyRangeCompare(SearchIndexOperator.Equal, v, range));
            queryEngine.AddOperatorHandler("!=", (SearchValue v, PropertyRange range) => PropertyRangeCompare(SearchIndexOperator.NotEqual, v, range));
            queryEngine.AddOperatorHandler("<=", (SearchValue v, PropertyRange range) => PropertyRangeCompare(SearchIndexOperator.LessOrEqual, v, range));
            queryEngine.AddOperatorHandler("<", (SearchValue v, PropertyRange range) => PropertyRangeCompare(SearchIndexOperator.Less, v, range));
            queryEngine.AddOperatorHandler(">", (SearchValue v, PropertyRange range) => PropertyRangeCompare(SearchIndexOperator.Greater, v, range));
            queryEngine.AddOperatorHandler(">=", (SearchValue v, PropertyRange range) => PropertyRangeCompare(SearchIndexOperator.GreaterOrEqual, v, range));

            queryEngine.AddOperatorHandler(":", (SearchValue v, float number, StringComparison sc) => PropertyFloatCompare(SearchIndexOperator.Contains, v, number));
            queryEngine.AddOperatorHandler("=", (SearchValue v, float number) => PropertyFloatCompare(SearchIndexOperator.Equal, v, number));
            queryEngine.AddOperatorHandler("!=", (SearchValue v, float number) => PropertyFloatCompare(SearchIndexOperator.NotEqual, v, number));
            queryEngine.AddOperatorHandler("<=", (SearchValue v, float number) => PropertyFloatCompare(SearchIndexOperator.LessOrEqual, v, number));
            queryEngine.AddOperatorHandler("<", (SearchValue v, float number) => PropertyFloatCompare(SearchIndexOperator.Less, v, number));
            queryEngine.AddOperatorHandler(">", (SearchValue v, float number) => PropertyFloatCompare(SearchIndexOperator.Greater, v, number));
            queryEngine.AddOperatorHandler(">=", (SearchValue v, float number) => PropertyFloatCompare(SearchIndexOperator.GreaterOrEqual, v, number));

            queryEngine.AddOperatorHandler(":", (Vector4 v, Vector4 v4, StringComparison sc) => PropertyVector4Compare(SearchIndexOperator.Contains, v, v4));
            queryEngine.AddOperatorHandler("=", (Vector4 v, Vector4 v4) => PropertyVector4Compare(SearchIndexOperator.Equal, v, v4));
            queryEngine.AddOperatorHandler("!=", (Vector4 v, Vector4 v4) => PropertyVector4Compare(SearchIndexOperator.NotEqual, v, v4));
            queryEngine.AddOperatorHandler("<=", (Vector4 v, Vector4 v4) => PropertyVector4Compare(SearchIndexOperator.LessOrEqual, v, v4));
            queryEngine.AddOperatorHandler("<", (Vector4 v, Vector4 v4) => PropertyVector4Compare(SearchIndexOperator.Less, v, v4));
            queryEngine.AddOperatorHandler(">", (Vector4 v, Vector4 v4) => PropertyVector4Compare(SearchIndexOperator.Greater, v, v4));
            queryEngine.AddOperatorHandler(">=", (Vector4 v, Vector4 v4) => PropertyVector4Compare(SearchIndexOperator.GreaterOrEqual, v, v4));

            queryEngine.AddOperatorHandler(":", (SearchValue v, Vector4 v4, StringComparison sc) => PropertyVector4Compare(SearchIndexOperator.Contains, v, v4));
            queryEngine.AddOperatorHandler("=", (SearchValue v, Vector4 v4) => PropertyVector4Compare(SearchIndexOperator.Equal, v, v4));
            queryEngine.AddOperatorHandler("!=", (SearchValue v, Vector4 v4) => PropertyVector4Compare(SearchIndexOperator.NotEqual, v, v4));
            queryEngine.AddOperatorHandler("<=", (SearchValue v, Vector4 v4) => PropertyVector4Compare(SearchIndexOperator.LessOrEqual, v, v4));
            queryEngine.AddOperatorHandler("<", (SearchValue v, Vector4 v4) => PropertyVector4Compare(SearchIndexOperator.Less, v, v4));
            queryEngine.AddOperatorHandler(">", (SearchValue v, Vector4 v4) => PropertyVector4Compare(SearchIndexOperator.Greater, v, v4));
            queryEngine.AddOperatorHandler(">=", (SearchValue v, Vector4 v4) => PropertyVector4Compare(SearchIndexOperator.GreaterOrEqual, v, v4));

            queryEngine.AddOperatorHandler("=", (SearchValue v, bool b) => PropertyBoolCompare(v, b, (f, r) => f == r));
            queryEngine.AddOperatorHandler(":", (SearchValue v, bool b) => PropertyBoolCompare(v, b, (f, r) => f == r));
            queryEngine.AddOperatorHandler("!=", (SearchValue v, bool b) => PropertyBoolCompare(v, b, (f, r) => f != r));

            queryEngine.AddOperatorHandler(":", (SearchValue v, string s, StringComparison sc) => PropertyStringCompare(v, s, (f, r) => StringContains(f, r, sc)));
            queryEngine.AddOperatorHandler("=", (SearchValue v, string s, StringComparison sc) => PropertyStringCompare(v, s, (f, r) => string.Equals(f, r, sc)));
            queryEngine.AddOperatorHandler("!=", (SearchValue v, string s, StringComparison sc) => PropertyStringCompare(v, s, (f, r) => !string.Equals(f, r, sc)));
            queryEngine.AddOperatorHandler("<=", (SearchValue v, string s, StringComparison sc) => PropertyStringCompare(v, s, (f, r) => string.Compare(f, r, sc) <= 0));
            queryEngine.AddOperatorHandler("<", (SearchValue v, string s, StringComparison sc) => PropertyStringCompare(v, s, (f, r) => string.Compare(f, r, sc) < 0));
            queryEngine.AddOperatorHandler(">", (SearchValue v, string s, StringComparison sc) => PropertyStringCompare(v, s, (f, r) => string.Compare(f, r, sc) > 0));
            queryEngine.AddOperatorHandler(">=", (SearchValue v, string s, StringComparison sc) => PropertyStringCompare(v, s, (f, r) => string.Compare(f, r, sc) >= 0));

            queryEngine.AddOperatorHandler(":", (SearchValue v, SearchColor c) => PropertyColorCompare(v, c, (f, r) => f == r));
            queryEngine.AddOperatorHandler("=", (SearchValue v, SearchColor c) => PropertyColorCompare(v, c, (f, r) => f == r));
            queryEngine.AddOperatorHandler("!=", (SearchValue v, SearchColor c) => PropertyColorCompare(v, c, (f, r) => f != r));
            queryEngine.AddOperatorHandler("<=", (SearchValue v, SearchColor c) => PropertyColorCompare(v, c, (f, r) => f <= r));
            queryEngine.AddOperatorHandler("<", (SearchValue v, SearchColor c) => PropertyColorCompare(v, c, (f, r) => f < r));
            queryEngine.AddOperatorHandler(">", (SearchValue v, SearchColor c) => PropertyColorCompare(v, c, (f, r) => f > r));
            queryEngine.AddOperatorHandler(">=", (SearchValue v, SearchColor c) => PropertyColorCompare(v, c, (f, r) => f >= r));

            queryEngine.AddTypeParser(arg =>
            {
                if (Utils.TryParseRange(arg, out var range))
                    return new ParseResult<PropertyRange>(true, range);
                return ParseResult<PropertyRange>.none;
            });

            queryEngine.AddTypeParser(s =>
            {
                if (!s.StartsWith("#"))
                    return new ParseResult<SearchColor?>(false, null);
                if (ColorUtility.TryParseHtmlString(s, out var color))
                    return new ParseResult<SearchColor?>(true, new SearchColor(color));
                return new ParseResult<SearchColor?>(false, null);
            });

            queryEngine.AddTypeParser(s =>
            {
                if (!Utils.TryParseVectorValue(s, out var vec, out _))
                    return new ParseResult<Vector4>(false, default);
                return new ParseResult<Vector4>(true, vec);
            });
        }

        private static bool StringContains(string ev, string fv, StringComparison sc)
        {
            if (ev == null || fv == null)
                return false;
            return ev.IndexOf(fv, sc) != -1;
        }

        private static bool PropertyVector4Compare(in SearchIndexOperator op, in SearchValue v, in Vector4 v4)
        {
            if (v.type != ValueType.Vector3 && v.type != ValueType.Vector2 && v.type != ValueType.Vector4)
                return false;
            return PropertyVector4Compare(op, v.v4, v4);
        }

        private static bool PropertyVector4Compare(in SearchIndexOperator op, in Vector4 v, in Vector4 v4)
        {
            bool hx = !float.IsNaN(v4.x),
                 hy = !float.IsNaN(v4.y),
                 hz = !float.IsNaN(v4.z),
                 hw = !float.IsNaN(v4.w);
            return (hx == false || Utils.NumberCompare(op, v.x, v4.x))
                && (hy == false || Utils.NumberCompare(op, v.y, v4.y))
                && (hz == false || Utils.NumberCompare(op, v.z, v4.z))
                && (hw == false || Utils.NumberCompare(op, v.w, v4.w));
        }

        private static bool PropertyRangeCompare(in SearchIndexOperator op, in SearchValue v, in PropertyRange range)
        {
            if (v.type != ValueType.Number)
                return false;
            switch(op)
            {
                case SearchIndexOperator.Equal:
                    return range.Contains(v.floatNumber);
                case SearchIndexOperator.Contains:
                    return range.Contains(v.floatNumber);
                case SearchIndexOperator.NotEqual:
                    return !range.Contains(v.floatNumber);
                case SearchIndexOperator.Greater:
                    return v.floatNumber > range.max;
                case SearchIndexOperator.GreaterOrEqual:
                    return v.floatNumber >= range.max;
                case SearchIndexOperator.Less:
                    return v.floatNumber < range.min;
                case SearchIndexOperator.LessOrEqual:
                    return v.floatNumber <= range.min;
            }
            return false;
        }

        private static bool PropertyFloatCompare(in SearchIndexOperator op, in SearchValue v, float value)
        {
            if (v.type == ValueType.Enum)
                return Utils.NumberCompare(op, v.floatNumber, value);
            if (v.type != ValueType.Number)
                return false;
            return Utils.NumberCompare(op, v.floatNumber, value);
        }

        private static bool PropertyBoolCompare(in SearchValue v, bool b, Func<bool, bool, bool> comparer)
        {
            if (v.type != ValueType.Bool)
                return false;
            return comparer(v.floatNumber == 1d, b);
        }

        private static bool PropertyStringCompare(in SearchValue v, string s, Func<string, string, bool> comparer)
        {
            if (v.type == ValueType.Enum)
                return comparer(v.text, s);
            if (v.type == ValueType.Object)
            {
                if (string.Equals(s, "none", StringComparison.Ordinal) && string.Equals(v.text, string.Empty, StringComparison.Ordinal))
                    return comparer(s, "none");
                if (string.IsNullOrEmpty(v.text))
                    return false;

                // Test with the value as is.
                if (comparer(v.text, s))
                    return true;

                // Could be an asset path, try to resolve it.
                if (!v.text.StartsWith("/") && File.Exists(v.text))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(v.text);

                    // Compare with the name of the asset.
                    return obj != null && comparer(obj.name, s);
                }

                // Might be a global object id, try to resolve it.
                // Note: do this last, as it is slow.
                if (v.text.StartsWith("GlobalObjectId", StringComparison.Ordinal) && GlobalObjectId.TryParse(v.text, out var goid))
                {
                    var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(goid);
                    if (obj == null)
                        return false;

                    // Compare name of the object.
                    if (comparer(obj.name, s))
                        return true;

                    if (obj is Component c)
                    {
                        var tp = SearchUtils.GetTransformPath(c.gameObject.transform);
                        return comparer(tp, s);
                    }

                    var assetPath = AssetDatabase.GetAssetPath(obj);
                    if (!string.IsNullOrEmpty(assetPath) && Utils.IsBuiltInResource(assetPath))
                        return comparer(assetPath, s);

                    if (obj is GameObject go)
                    {
                        var tp = SearchUtils.GetTransformPath(go.transform);
                        return comparer(tp, s);
                    }
                }
            }
            if (v.type == ValueType.Bool)
            {
                if (v.boolean && string.Equals(s, "on", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (!v.boolean && string.Equals(s, "off", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            else if (v.type != ValueType.Text || string.IsNullOrEmpty(v.text))
                return false;
            return comparer(v.text, s);
        }

        private static bool PropertyColorCompare(in SearchValue v, SearchColor value, Func<SearchColor, SearchColor, bool> comparer)
        {
            if (v.type != ValueType.Color)
                return false;
            return comparer(v.color, value);
        }

        [PropertyDatabaseSerializer(typeof(SearchValue))]
        internal static PropertyDatabaseRecordValue SearchValueSerializer(PropertyDatabaseSerializationArgs args)
        {
            // TODO: Figure out why vectors use strings instead of saving the data directly in the record value
            int stringSymbol;
            var gop = (SearchValue)args.value;
            switch (gop.type)
            {
                case ValueType.Nil:
                    return new PropertyDatabaseRecordValue((byte)PropertyDatabaseType.GameObjectProperty, (byte)gop.type);
                case ValueType.Bool:
                case ValueType.Number:
                    return new PropertyDatabaseRecordValue((byte)PropertyDatabaseType.GameObjectProperty, (byte)gop.type, BitConverter.SingleToInt32Bits(gop.floatNumber));
                case ValueType.Text:
                    stringSymbol = args.stringTableView.ToSymbol(gop.text);
                    return new PropertyDatabaseRecordValue((byte)PropertyDatabaseType.GameObjectProperty, (byte)gop.type, (int)stringSymbol);
                case ValueType.Color:
                    return new PropertyDatabaseRecordValue((byte)PropertyDatabaseType.GameObjectProperty, (byte)gop.type, (byte)gop.color.r, (byte)gop.color.g, (byte)gop.color.b, (byte)gop.color.a);
                case ValueType.Vector2:
                    stringSymbol = args.stringTableView.ToSymbol(Utils.ToString(gop.v4, 2));
                    return new PropertyDatabaseRecordValue((byte)PropertyDatabaseType.GameObjectProperty, (byte)gop.type, (int)stringSymbol);
                case ValueType.Vector3:
                    stringSymbol = args.stringTableView.ToSymbol(Utils.ToString(gop.v4, 3));
                    return new PropertyDatabaseRecordValue((byte)PropertyDatabaseType.GameObjectProperty, (byte)gop.type, (int)stringSymbol);
                case ValueType.Vector4:
                    stringSymbol = args.stringTableView.ToSymbol(Utils.ToString(gop.v4, 4));
                    return new PropertyDatabaseRecordValue((byte)PropertyDatabaseType.GameObjectProperty, (byte)gop.type, (int)stringSymbol);
                case ValueType.Object:
                    stringSymbol = args.stringTableView.ToSymbol(gop.text ?? string.Empty);
                    return new PropertyDatabaseRecordValue((byte)PropertyDatabaseType.GameObjectProperty, (byte)gop.type, (int)stringSymbol);
                case ValueType.Enum:
                    stringSymbol = args.stringTableView.ToSymbol(gop.text);
                    return new PropertyDatabaseRecordValue((byte)PropertyDatabaseType.GameObjectProperty, (byte)gop.type, (int)stringSymbol, (int)gop.floatNumber);
            }

            return PropertyDatabaseRecordValue.invalid;
        }

        [PropertyDatabaseDeserializer(PropertyDatabaseType.GameObjectProperty)]
        internal static object SearchValueDeserializer(PropertyDatabaseDeserializationArgs args)
        {
            var gopType = (ValueType)args.value[0];
            switch (gopType)
            {
                case ValueType.Nil:
                    return new SearchValue();
                case ValueType.Bool:
                    return new SearchValue(BitConverter.Int32BitsToSingle(args.value.int32_1) == 1d);
                case ValueType.Number:
                    return new SearchValue(BitConverter.Int32BitsToSingle(args.value.int32_1));
                case ValueType.Text:
                    var symbol = args.value.int32_1;
                    var str = args.stringTableView.GetString(symbol);
                    return new SearchValue(str);
                case ValueType.Color:
                    return new SearchValue(new SearchColor(args.value[1], args.value[2], args.value[3], args.value[4]));
                case ValueType.Vector2:
                case ValueType.Vector3:
                case ValueType.Vector4:
                    symbol = args.value.int32_1;
                    str = args.stringTableView.GetString(symbol);
                    if (Utils.TryParseVectorValue(str, out var v4, out int dim))
                        return new SearchValue(v4, dim);
                    break;
                case ValueType.Object:
                    symbol = args.value.int32_1;
                    str = args.stringTableView.GetString(symbol);
                    return new SearchValue((UnityEngine.Object)null, str);
                case ValueType.Enum:
                    symbol = args.value.int32_1;
                    str = args.stringTableView.GetString(symbol);
                    return new SearchValue(args.value.int32_2, str);
            }

            throw new Exception("Failed to deserialize game object property");
        }

    }

    class SearchItemQueryEngine : QueryEngine<SearchItem>
    {
        static Regex PropertyFilterRx = new Regex(@"[\@\$]([#\w\d\.\[\]]+)");

        SearchExpressionContext m_Context;

        public SearchItemQueryEngine()
        {
            Setup();
        }

        public IEnumerable<SearchItem> Where(SearchExpressionContext context, IEnumerable<SearchItem> dataSet, string queryStr)
        {
            m_Context = context;
            var query = ParseQuery(queryStr, true);
            if (query.errors.Count != 0)
            {
                var errorStr = string.Join("\n", query.errors.Select(err => $"Invalid where query expression at {err.index}: {err.reason}"));
                context.ThrowError(errorStr);
            }

            foreach (var item in dataSet)
            {
                if (item != null)
                {
                    if (query.Test(item))
                        yield return item;
                }
                else
                    yield return null;
            }
            m_Context = default;
        }

        public IEnumerable<SearchItem> WhereMainThread(SearchExpressionContext context, IEnumerable<SearchItem> dataSet, string queryStr)
        {
            m_Context = context;
            var query = ParseQuery(queryStr, true);
            if (query.errors.Count != 0)
            {
                var errorStr = string.Join("\n", query.errors.Select(err => $"Invalid where query expression at {err.index}: {err.reason}"));
                context.ThrowError(errorStr);
            }

            var results =  TaskEvaluatorManager.EvaluateMainThread(dataSet, item =>
            {
                if (query.Test(item))
                    return item;
                return null;
            }, 25);
            m_Context = default;
            return results;
        }

        private void Setup()
        {
            AddFilter(PropertyFilterRx, GetValue);
            AddFilter("p", GetValue, s => s, StringComparison.OrdinalIgnoreCase);

            SearchValue.SetupEngine(this);

            SetSearchDataCallback(GetSearchableData, StringComparison.OrdinalIgnoreCase);
        }

        IEnumerable<string> GetSearchableData(SearchItem item)
        {
            yield return item.value.ToString();
            yield return item.id;
            if (item.label != null)
                yield return item.label;
        }

        SearchValue GetValue(SearchItem item, string selector)
        {
            var v = SelectorManager.SelectValue(item, m_Context.search, selector);
            return new SearchValue(v);
        }
    }
}
