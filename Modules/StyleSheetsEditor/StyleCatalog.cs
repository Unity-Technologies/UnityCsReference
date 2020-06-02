// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace UnityEditor.StyleSheets
{
    [Flags]
    internal enum StyleState : long
    {
        none = 0L,

        all = 0x7FFFFFFFFFFFFFFF,

        normal = 1L << 0,
        active = 1L << 1,
        anyLink = 1L << 2,
        @checked = 1L << 3,
        @default = 1L << 4,
        defined = 1L << 5,
        disabled = 1L << 6,
        empty = 1L << 7,
        enabled = 1L << 8,
        first = 1L << 9,
        firstChild = 1L << 10,
        firstOfType = 1L << 11,
        fullscreen = 1L << 12,
        focus = 1L << 13,
        focusVisible = 1L << 14,
        host = 1L << 15,
        hover = 1L << 16,
        indeterminate = 1L << 17,
        inRange = 1L << 18,
        invalid = 1L << 19,
        lastChild = 1L << 20,
        lastOfType = 1L << 21,
        link = 1L << 22,
        onlyChild = 1L << 23,
        onlyOfType = 1L << 24,
        optional = 1L << 25,
        outOfRange = 1L << 26,
        readOnly = 1L << 27,
        readWrite = 1L << 28,
        required = 1L << 29,
        scope = 1L << 30,
        target = 1L << 31,
        valid = 1L << 32,
        visited = 1L << 33,
        root = 1L << 34,

        any = 1L << 63
    }

    internal static class StyleCatalogKeyword
    {
        public readonly static int root = "".GetHashCode();

        public readonly static int left = "left".GetHashCode();
        public readonly static int right = "right".GetHashCode();
        public readonly static int top = "top".GetHashCode();
        public readonly static int bottom = "bottom".GetHashCode();

        public readonly static int background = "background".GetHashCode();
        public readonly static int backgroundAttachment = "background-attachment".GetHashCode();
        public readonly static int backgroundColor = "background-color".GetHashCode();
        public readonly static int backgroundImage = "background-image".GetHashCode();
        public readonly static int scaledBackgroundImage = "-unity-scaled-backgrounds".GetHashCode();
        public readonly static int backgroundPosition = "background-position".GetHashCode();
        public readonly static int backgroundPositionX = "background-position-x".GetHashCode();
        public readonly static int backgroundPositionY = "background-position-y".GetHashCode();

        public readonly static int contentImageOffsetX = "-unity-content-image-offset-x".GetHashCode();
        public readonly static int contentImageOffsetY = "-unity-content-image-offset-y".GetHashCode();

        public readonly static int backgroundRepeat = "background-repeat".GetHashCode();
        public readonly static int backgroundSize = "background-size".GetHashCode();
        public readonly static int border = "border".GetHashCode();
        public readonly static int borderBottom = "border-bottom".GetHashCode();
        public readonly static int borderBottomColor = "border-bottom-color".GetHashCode();
        public readonly static int borderBottomStyle = "border-bottom-style".GetHashCode();
        public readonly static int borderBottomWidth = "border-bottom-width".GetHashCode();
        public readonly static int borderColor = "border-color".GetHashCode();
        public readonly static int borderLeft = "border-left".GetHashCode();
        public readonly static int borderLeftColor = "border-left-color".GetHashCode();
        public readonly static int borderLeftStyle = "border-left-style".GetHashCode();
        public readonly static int borderLeftWidth = "border-left-width".GetHashCode();
        public readonly static int borderRadius = "border-radius".GetHashCode();
        public readonly static int borderRight = "border-right".GetHashCode();
        public readonly static int borderRightColor = "border-right-color".GetHashCode();
        public readonly static int borderRightStyle = "border-right-style".GetHashCode();
        public readonly static int borderRightWidth = "border-right-width".GetHashCode();
        public readonly static int borderStyle = "border-style".GetHashCode();
        public readonly static int borderTop = "border-top".GetHashCode();
        public readonly static int borderTopColor = "border-top-color".GetHashCode();
        public readonly static int borderTopStyle = "border-top-style".GetHashCode();
        public readonly static int borderTopWidth = "border-top-width".GetHashCode();
        public readonly static int borderWidth = "border-width".GetHashCode();
        public readonly static int borderTopLeftRadius = "border-top-left-radius".GetHashCode();
        public readonly static int borderTopRightRadius = "border-top-right-radius".GetHashCode();
        public readonly static int borderBottomLeftRadius = "border-bottom-left-radius".GetHashCode();
        public readonly static int borderBottomRightRadius = "border-bottom-right-radius".GetHashCode();
        public readonly static int clear = "clear".GetHashCode();
        public readonly static int clip = "clip".GetHashCode();
        public readonly static int color = "color".GetHashCode();
        public readonly static int cursor = "cursor".GetHashCode();
        public readonly static int display = "display".GetHashCode();
        public readonly static int filter = "filter".GetHashCode();
        public readonly static int cssFloat = "float".GetHashCode();
        public readonly static int font = "font".GetHashCode();
        public readonly static int fontFamily = "font-family".GetHashCode();
        public readonly static int fontSize = "font-size".GetHashCode();
        public readonly static int fontVariant = "font-variant".GetHashCode();
        public readonly static int fontWeight = "font-weight".GetHashCode();
        public readonly static int height = "height".GetHashCode();
        public readonly static int letterSpacing = "letter-spacing".GetHashCode();
        public readonly static int lineHeight = "line-height".GetHashCode();
        public readonly static int listStyle = "list-style".GetHashCode();
        public readonly static int listStyleImage = "list-style-image".GetHashCode();
        public readonly static int listStylePosition = "list-style-position".GetHashCode();
        public readonly static int listStyleType = "list-style-type".GetHashCode();
        public readonly static int margin = "margin".GetHashCode();
        public readonly static int marginBottom = "margin-bottom".GetHashCode();
        public readonly static int marginLeft = "margin-left".GetHashCode();
        public readonly static int marginRight = "margin-right".GetHashCode();
        public readonly static int marginTop = "margin-top".GetHashCode();
        public readonly static int opacity = "opacity".GetHashCode();
        public readonly static int overflow = "overflow".GetHashCode();
        public readonly static int overflowX = "overflow-x".GetHashCode();
        public readonly static int overflowY = "overflow-y".GetHashCode();
        public readonly static int padding = "padding".GetHashCode();
        public readonly static int paddingBottom = "padding-bottom".GetHashCode();
        public readonly static int paddingLeft = "padding-left".GetHashCode();
        public readonly static int paddingRight = "padding-right".GetHashCode();
        public readonly static int paddingTop = "padding-top".GetHashCode();
        public readonly static int pageBreakAfter = "page-break-after".GetHashCode();
        public readonly static int pageBreakBefore = "page-break-before".GetHashCode();
        public readonly static int position = "position".GetHashCode();
        public readonly static int size = "size".GetHashCode();
        public readonly static int strokeDasharray = "stroke-dasharray".GetHashCode();
        public readonly static int strokeDashoffset = "stroke-dashoffset".GetHashCode();
        public readonly static int strokeWidth = "stroke-width".GetHashCode();
        public readonly static int textAlign = "text-align".GetHashCode();
        public readonly static int textDecoration = "text-decoration".GetHashCode();
        public readonly static int textDecorationColor = "text-decoration-color".GetHashCode();
        public readonly static int textIndent = "text-indent".GetHashCode();
        public readonly static int textTransform = "text-transform".GetHashCode();
        public readonly static int verticalAlign = "vertical-align".GetHashCode();
        public readonly static int visibility = "visibility".GetHashCode();
        public readonly static int width = "width".GetHashCode();
        public readonly static int minWidth = "min-width".GetHashCode();
        public readonly static int maxWidth = "max-width".GetHashCode();
        public readonly static int zIndex = "z-index".GetHashCode();
    }

    [DebuggerDisplay("{value}")]
    internal class SVC<T>
    {
        protected T m_Value;
        protected int m_Key;
        protected int m_Prop;
        protected StyleState[] m_States;
        protected bool m_Initialized;
        protected Func<T> m_LateInitHandler;

        public SVC(int key, int property, T defaultValue = default(T), params StyleState[] states)
        {
            m_Key = key;
            m_Prop = property;
            m_States = states;
            m_Value = defaultValue;
            m_Initialized = false;
            m_LateInitHandler = () => m_Value;
        }

        public SVC(string name, string property, T defaultValue = default(T), params StyleState[] states) : this(name.GetHashCode(), property.GetHashCode(), defaultValue, states) {}
        public SVC(string name, int property, T defaultValue = default(T), params StyleState[] states) : this(name.GetHashCode(), property, defaultValue, states) {}
        public SVC(int name, string property, T defaultValue = default(T), params StyleState[] states) : this(name, property.GetHashCode(), defaultValue, states) {}

        public SVC(string name, int property, Func<T> lateInitHandler, params StyleState[] states) : this(name.GetHashCode(), property, default(T), states)
        {
            m_LateInitHandler = lateInitHandler;
        }

        // Root access, i.e. :root {...}
        public SVC(int property, T defaultValue = default(T)) : this(StyleCatalogKeyword.root, property, defaultValue, StyleState.root) {}
        public SVC(string property, T defaultValue = default(T)) : this(StyleCatalogKeyword.root, property.GetHashCode(), defaultValue, StyleState.root) {}

        public static implicit operator T(SVC<T> sc) { return sc.value; }

        public T value
        {
            get
            {
                if (!m_Initialized)
                {
                    m_Value = ReadValue(m_LateInitHandler());
                    m_Initialized = true;
                }
                return m_Value;
            }
        }

        private T ReadValue(T defaultValue)
        {
            var block = EditorResources.GetStyle(m_Key, m_States);
            if (!block.IsValid())
                return defaultValue;
            if (!block.HasValue(m_Prop))
                return defaultValue;

            var valueType = block.GetValueType(m_Prop);

            if (valueType == StyleValue.Type.Number)
                return (T)(object)block.GetFloat(m_Prop);
            if (valueType == StyleValue.Type.Color)
                return (T)(object)block.GetColor(m_Prop);
            if (valueType == StyleValue.Type.Keyword)
            {
                if (typeof(T) == typeof(bool))
                    return (T)(object)(block.GetKeyword(m_Prop) == StyleValue.Keyword.True);
                return (T)(object)block.GetKeyword(m_Prop);
            }
            if (valueType == StyleValue.Type.Rect)
                return (T)(object)block.GetRect(m_Prop);
            if (valueType == StyleValue.Type.Text)
                return (T)(object)block.GetText(m_Prop);

            Debug.LogWarning($"Style constant value conversion of type {valueType} not supported.");
            return defaultValue;
        }
    }

    [DebuggerDisplay("topleft = ({top}, {left}), bottomright = ({bottom}, {right}), size = ({width} x {height})")]
    [StructLayout(LayoutKind.Explicit)]
    internal struct StyleRect : IEquatable<StyleRect>
    {
        public static readonly StyleRect Nil = new StyleRect { left = float.NaN, right = float.NaN, top = float.NaN, bottom = float.NaN };
        public static readonly StyleRect Zero = new StyleRect { left = 0f, right = 0f, top = 0f, bottom = 0f };

        public static StyleRect Size(float width, float height)
        {
            return new StyleRect {width = width, height = height, bottom = width, left = height};
        }

        public StyleRect(float top, float right, float bottom, float left)
        {
            width = height = 0;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
            this.left = left;
        }

        public StyleRect(RectOffset offset)
        {
            width = height = 0;
            top = offset.top;
            right = offset.right;
            bottom = offset.bottom;
            left = offset.left;
        }

        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return top;
                    case 1: return right;
                    case 2: return bottom;
                    case 3: return left;
                    default:
                        throw new IndexOutOfRangeException("invalid rect index");
                }
            }
        }

        public bool incomplete => top == float.MaxValue || right == float.MaxValue || bottom == float.MaxValue || left == float.MaxValue;

        internal StyleRect @fixed => new StyleRect
        {
            top = top == float.MaxValue ? 0 : top,
            right = right == float.MaxValue ? 0 : right,
            bottom = bottom == float.MaxValue ? 0 : bottom,
            left = left == float.MaxValue ? 0 : left
        };

        [FieldOffset(0)] public float top;
        [FieldOffset(4)] public float right;
        [FieldOffset(8)] public float bottom;
        [FieldOffset(12)] public float left;

        [FieldOffset(0)] public float width;
        [FieldOffset(4)] public float height;

        public bool Equals(StyleRect other)
        {
            return top.Equals(other.top) && right.Equals(other.right) && bottom.Equals(other.bottom) && left.Equals(other.left) && width.Equals(other.width) && height.Equals(other.height);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is StyleRect && Equals((StyleRect)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = top.GetHashCode();
                hashCode = (hashCode * 397) ^ right.GetHashCode();
                hashCode = (hashCode * 397) ^ bottom.GetHashCode();
                hashCode = (hashCode * 397) ^ left.GetHashCode();
                hashCode = (hashCode * 397) ^ width.GetHashCode();
                hashCode = (hashCode * 397) ^ height.GetHashCode();
                return hashCode;
            }
        }
    }

    internal struct StyleFunction : IEquatable<StyleFunction>
    {
        public string name;
        public List<StyleValue[]> args;

        public bool Equals(StyleFunction other)
        {
            if (name != other.name)
                return false;

            if (args.Count != other.args.Count)
                return false;

            int argListIndex = 0;
            foreach (var argList in args)
            {
                if (argList.Length != other.args[argListIndex].Length)
                    return false;

                int argIndex = 0;
                foreach (var arg in argList)
                {
                    if (!arg.Equals(other.args[argListIndex][argIndex]))
                        return false;
                }

                argListIndex++;
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is StyleFunction && Equals((StyleFunction)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = name.GetHashCode();
                foreach (var argList in args)
                    foreach (var arg in argList)
                        hashCode = (hashCode * 397) ^ arg.GetHashCode();
                return hashCode;
            }
        }
    }

    [DebuggerDisplay("{width} {style} {color}")]
    internal struct StyleLine
    {
        public StyleLine(float width, string style, Color color)
        {
            this.width = width;
            this.style = style;
            this.color = color;
        }

        public float width;
        public string style;
        public Color color;
    }

    [DebuggerDisplay("count = {count}")]
    internal struct StyleValueGroup : IEquatable<StyleValueGroup>
    {
        public const int k_MaxValueCount = 5;

        public StyleValueGroup(int key, int count)
        {
            this.count = count;
            v1 = v2 = v3 = v4 = v5 = StyleValue.Undefined(key);
        }

        public StyleValue this[int i]
        {
            get { return GetValueAt(i); }
            set { SetValueAt(i, value); }
        }

        public StyleValue GetValueAt(int i)
        {
            switch (i)
            {
                case 0: return v1;
                case 1: return v2;
                case 2: return v3;
                case 3: return v4;
                case 4: return v5;
                default:
                    throw new ArgumentOutOfRangeException(nameof(i), $"Index must be between [0, {k_MaxValueCount - 1}]");
            }
        }

        internal void SetValueAt(int i, StyleValue value)
        {
            switch (i)
            {
                case 0: v1 = value; break;
                case 1: v2 = value; break;
                case 2: v3 = value; break;
                case 3: v4 = value; break;
                case 4: v5 = value; break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(i), $"Index must be between [0, {k_MaxValueCount - 1}]");
            }
        }

        public int count;
        public StyleValue v1;
        public StyleValue v2;
        public StyleValue v3;
        public StyleValue v4;
        public StyleValue v5;

        public bool Equals(StyleValueGroup other)
        {
            return count == other.count && v1.Equals(other.v1) && v2.Equals(other.v2) && v3.Equals(other.v3) && v4.Equals(other.v4) && v5.Equals(other.v5);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is StyleValueGroup && Equals((StyleValueGroup)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = count;
                hashCode = (hashCode * 397) ^ v1.GetHashCode();
                hashCode = (hashCode * 397) ^ v2.GetHashCode();
                hashCode = (hashCode * 397) ^ v3.GetHashCode();
                hashCode = (hashCode * 397) ^ v4.GetHashCode();
                hashCode = (hashCode * 397) ^ v5.GetHashCode();
                return hashCode;
            }
        }
    }

    [DebuggerDisplay("key = {key}, type = {type}, index = {index}, state = {state}")]
    internal struct StyleValue : IEquatable<StyleValue>
    {
        public static StyleValue Undefined(int key, StyleState state = StyleState.none) { return new StyleValue { key = key, state = state, type = Type.Undefined, index = 0 }; }
        public static StyleValue Undefined(string name, StyleState state = StyleState.none) { return Undefined(name.GetHashCode(), state); }

        public enum Type
        {
            Undefined = -1,
            Keyword = StyleValueType.Keyword,
            Number = StyleValueType.Float,
            Color = StyleValueType.Color,
            Text = StyleValueType.String,
            Rect,
            Group,
            Function,

            Any = 0x7FFFFFFF
        }

        public enum Keyword
        {
            Invalid = -1,
            None = 0,
            False = 0,
            True,
            Inherit,
            Auto,
            Unset
        }

        public bool IsValid()
        {
            return type != Type.Undefined && index != -1;
        }

        internal static Keyword ConvertKeyword(StyleValueKeyword svk)
        {
            switch (svk)
            {
                case StyleValueKeyword.Inherit: return Keyword.Inherit;
                case StyleValueKeyword.Auto: return Keyword.Auto;
                case StyleValueKeyword.Unset: return Keyword.Unset;
                case StyleValueKeyword.True: return Keyword.True;
                case StyleValueKeyword.False: return Keyword.False;
                case StyleValueKeyword.None: return Keyword.None;
                default:
                    return Keyword.Invalid;
            }
        }

        public int key;
        public Type type;
        public int index;
        public StyleState state;

        public bool Equals(StyleValue other)
        {
            return key == other.key && type == other.type && index == other.index && state == other.state;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is StyleValue && Equals((StyleValue)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = key;
                hashCode = (hashCode * 397) ^ (int)type;
                hashCode = (hashCode * 397) ^ index;
                hashCode = (hashCode * 397) ^ state.GetHashCode();
                return hashCode;
            }
        }
    }

    internal struct StyleFunctionCall
    {
        private readonly StyleBlock block;

        public readonly string name;
        public readonly int valueKey;
        public readonly List<StyleValue[]> args;
        public int blockKey => block.name;

        public StyleFunctionCall(StyleBlock block, int valueKey, string name, List<StyleValue[]> args)
        {
            this.block = block;
            this.valueKey = valueKey;
            this.name = name;
            this.args = args;
        }

        public StyleValue.Type GetValueType(int argIndex, int valueIndex = -1)
        {
            if (valueIndex == -1 && args[argIndex].Length == 1)
                valueIndex = 0;
            return args[argIndex][valueIndex].type;
        }

        public Color GetColor(int argIndex, int valueIndex = -1)
        {
            if (valueIndex == -1 && args[argIndex].Length == 1)
                valueIndex = 0;
            return block.catalog.buffers.colors[args[argIndex][valueIndex].index];
        }

        public float GetNumber(int argIndex, int valueIndex = -1)
        {
            if (valueIndex == -1 && args[argIndex].Length == 1)
                valueIndex = 0;
            return block.catalog.buffers.numbers[args[argIndex][valueIndex].index];
        }

        public string GetString(int argIndex, int valueIndex = -1)
        {
            if (valueIndex == -1 && args[argIndex].Length == 1)
                valueIndex = 0;
            return block.catalog.buffers.strings[args[argIndex][valueIndex].index];
        }
    }

    [DebuggerDisplay("name = {name}")]
    internal readonly struct StyleBlock
    {
        public readonly int name;
        public readonly StyleState[] states;
        public readonly StyleValue[] values;
        public readonly StyleCatalog catalog;

        public StyleBlock(int name, StyleState[] states, StyleValue[] values, StyleCatalog catalog)
        {
            this.name = name;
            this.states = states;
            this.values = values;
            this.catalog = catalog;
        }

        public bool IsValid()
        {
            return name != -1;
        }

        public bool HasKeyword(int key)
        {
            return GetValueIndex(key, StyleValue.Type.Keyword) > 0;
        }

        public bool HasKeyword(string key)
        {
            return GetValueIndex(key, StyleValue.Type.Keyword) > 0;
        }

        public StyleValue.Keyword GetKeyword(int key, StyleValue.Keyword defaultValue = StyleValue.Keyword.Invalid)
        {
            var bufferIndex = GetValueIndex(key, StyleValue.Type.Keyword);
            if (bufferIndex == -1)
                return defaultValue;

            return (StyleValue.Keyword)bufferIndex;
        }

        public StyleValue.Keyword GetKeyword(string key, StyleValue.Keyword defaultValue = StyleValue.Keyword.Invalid)
        {
            return GetKeyword(key.GetHashCode(), defaultValue);
        }

        public bool GetBool(int key, bool defaultValue = false)
        {
            var bufferIndex = GetValueIndex(key, StyleValue.Type.Keyword);
            if (bufferIndex == -1)
                return defaultValue;
            return (StyleValue.Keyword)bufferIndex == StyleValue.Keyword.True;
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            return GetBool(key.GetHashCode(), defaultValue);
        }

        public float GetFloat(int key, float defaultValue = 0.0f)
        {
            var bufferIndex = GetValueIndex(key, StyleValue.Type.Number);
            if (bufferIndex == -1)
                return defaultValue;
            return catalog.buffers.numbers[bufferIndex];
        }

        public float GetFloat(string key, float defaultValue = 0.0f)
        {
            return GetFloat(key.GetHashCode(), defaultValue);
        }

        public Color GetColor(int key, Color defaultValue = default(Color))
        {
            var bufferIndex = GetValueIndex(key, StyleValue.Type.Color);
            if (bufferIndex == -1)
                return defaultValue;
            return catalog.buffers.colors[bufferIndex];
        }

        public Color GetColor(string key, Color defaultValue = default(Color))
        {
            return GetColor(key.GetHashCode(), defaultValue);
        }

        public string GetText(int key, string defaultValue = "")
        {
            var bufferIndex = GetValueIndex(key, StyleValue.Type.Text);
            if (bufferIndex == -1)
                return defaultValue;
            return catalog.buffers.strings[bufferIndex];
        }

        public string GetText(string key, string defaultValue = "")
        {
            return GetText(key.GetHashCode(), defaultValue);
        }

        public StyleRect GetRect(int key, StyleRect defaultValue = default(StyleRect))
        {
            var bufferIndex = GetValueIndex(key, StyleValue.Type.Rect);
            if (bufferIndex == -1)
                return defaultValue;
            return catalog.buffers.rects[bufferIndex];
        }

        public StyleRect GetRect(string key, StyleRect defaultValue = default(StyleRect))
        {
            return GetRect(key.GetHashCode(), defaultValue);
        }

        public int GetInt(int key, int defaultValue = 0)
        {
            return (int)GetFloat(key, defaultValue);
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            return (int)GetFloat(key, defaultValue);
        }

        public T GetResource<T>(int key, T defaultValue = null) where T : UnityEngine.Object
        {
            var resourceStr = GetText(key);
            if (string.IsNullOrEmpty(resourceStr))
            {
                return defaultValue;
            }

            var resource = EditorResources.Load<UnityEngine.Object>(resourceStr, false) as T;
            if (resource == null)
            {
                resource = Resources.Load<UnityEngine.Object>(resourceStr) as T;
            }

            return resource;
        }

        public T GetResource<T>(string key, T defaultValue = null) where T : UnityEngine.Object
        {
            return GetResource<T>(key.GetHashCode(), defaultValue);
        }

        static class TexturesByDPIScale
        {
            private static Dictionary<int, Dictionary<string, Texture2D>> s_TexturesByDPIScale = new Dictionary<int, Dictionary<string, Texture2D>>();

            static TexturesByDPIScale()
            {
                for (int i = 1; i < 4; ++i)
                    s_TexturesByDPIScale[i] = new Dictionary<string, Texture2D>();
            }

            public static Texture2D GetTextureByDPIScale(string resourcePath, bool autoScale, float systemScale)
            {
                Texture2D tex = null;
                int scale = Mathf.CeilToInt(systemScale);

                if (autoScale && systemScale > 1f)
                {
                    if (TryGetTexture(scale, resourcePath, out tex))
                        return tex;

                    string dirName = Path.GetDirectoryName(resourcePath).Replace('\\', '/');
                    string fileName = Path.GetFileNameWithoutExtension(resourcePath);
                    string fileExt = Path.GetExtension(resourcePath);
                    for (int s = scale; scale > 1; --scale)
                    {
                        string scaledResourcePath = $"{dirName}/{fileName}@{s}x{fileExt}";
                        var scaledResource = StoreTextureByScale(scale, scaledResourcePath, resourcePath, false);
                        if (scaledResource != null)
                            return scaledResource;
                    }
                }

                if (TryGetTexture(scale, resourcePath, out tex))
                    return tex;
                return StoreTextureByScale(scale, resourcePath, resourcePath, true);
            }

            private static Texture2D StoreTextureByScale(int scale, string scaledPath, string resourcePath, bool logError)
            {
                var tex = EditorResources.Load<Texture2D>(scaledPath, false);
                if (tex)
                {
                    if (!s_TexturesByDPIScale.ContainsKey(scale))
                        s_TexturesByDPIScale[scale] = new Dictionary<string, Texture2D>();
                    s_TexturesByDPIScale[scale][resourcePath] = tex;
                }
                else if (logError)
                {
                    Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, null, $"Failed to store {resourcePath} > {scaledPath}");
                }
                return tex;
            }

            private static bool TryGetTexture(int scale, string path, out Texture2D tex)
            {
                tex = null;
                if (s_TexturesByDPIScale.ContainsKey(scale) && s_TexturesByDPIScale[scale].TryGetValue(path, out tex) && tex != null)
                    return true;
                return false;
            }
        }

        public Texture2D GetTexture(int key, bool autoScale = false)
        {
            var resourcePath = GetText(key);
            if (String.IsNullOrEmpty(resourcePath))
                return null;

            return TexturesByDPIScale.GetTextureByDPIScale(resourcePath, autoScale, GUIUtility.pixelsPerPoint);
        }

        public Texture2D GetTexture(string key)
        {
            return GetTexture(key.GetHashCode());
        }

        public Tuple<T1, T2> GetTuple<T1, T2>(int key)
        {
            return new Tuple<T1, T2>(
                GetTupleElementValue(key, 0, default(T1)),
                GetTupleElementValue(key, 1, default(T2)));
        }

        public Tuple<T1, T2, T3> GetTuple<T1, T2, T3>(int key)
        {
            return new Tuple<T1, T2, T3>(
                GetTupleElementValue(key, 0, default(T1)),
                GetTupleElementValue(key, 1, default(T2)),
                GetTupleElementValue(key, 2, default(T3)));
        }

        public Tuple<T1, T2, T3, T4> GetTuple<T1, T2, T3, T4>(int key)
        {
            return new Tuple<T1, T2, T3, T4>(
                GetTupleElementValue(key, 0, default(T1)),
                GetTupleElementValue(key, 1, default(T2)),
                GetTupleElementValue(key, 2, default(T3)),
                GetTupleElementValue(key, 3, default(T4)));
        }

        public Tuple<T1, T2, T3, T4, T5> GetTuple<T1, T2, T3, T4, T5>(int key)
        {
            return new Tuple<T1, T2, T3, T4, T5>(
                GetTupleElementValue(key, 0, default(T1)),
                GetTupleElementValue(key, 1, default(T2)),
                GetTupleElementValue(key, 2, default(T3)),
                GetTupleElementValue(key, 3, default(T4)),
                GetTupleElementValue(key, 4, default(T5)));
        }

        public Tuple<T1, T2> GetTuple<T1, T2>(string key) { return GetTuple<T1, T2>(key.GetHashCode()); }
        public Tuple<T1, T2, T3> GetTuple<T1, T2, T3>(string key) { return GetTuple<T1, T2, T3>(key.GetHashCode()); }
        public Tuple<T1, T2, T3, T4> GetTuple<T1, T2, T3, T4>(string key) { return GetTuple<T1, T2, T3, T4>(key.GetHashCode()); }
        public Tuple<T1, T2, T3, T4, T5> GetTuple<T1, T2, T3, T4, T5>(string key) { return GetTuple<T1, T2, T3, T4, T5>(key.GetHashCode()); }

        private T GetTupleElementValue<T>(int key, int elementIndex, T defaultValue)
        {
            var groupIndex = GetValueIndex(key, StyleValue.Type.Group);
            if (groupIndex == -1)
                return defaultValue;
            return GetItemValue(elementIndex, defaultValue, groupIndex, catalog.buffers.groups, catalog.buffers.strings, catalog.buffers.numbers, catalog.buffers.colors, catalog.buffers.rects);
        }

        public unsafe T GetStruct<T>(int key, T defaultValue = default(T)) where T : struct
        {
            var groupIndex = GetValueIndex(key, StyleValue.Type.Group);
            if (groupIndex == -1)
                return defaultValue;

            var s = defaultValue;
            int structSize = UnsafeUtility.SizeOf<T>();
            byte* sptr = (byte*)UnsafeUtility.AddressOf(ref s);

            int offset = 0;
            var group = catalog.buffers.groups[groupIndex];
            for (int i = 0; i < group.count; ++i)
            {
                var sv = group.GetValueAt(i);

                switch (sv.type)
                {
                    case StyleValue.Type.Keyword:
                    {
                        var v = sv.index;
                        UnsafeUtility.MemCpy(sptr + offset, &v, 4);
                        offset += 4;
                        break;
                    }
                    case StyleValue.Type.Text:
                    {
                        var v = catalog.buffers.strings[sv.index].GetHashCode();
                        UnsafeUtility.MemCpy(sptr + offset, &v, 4);
                        offset += 4;
                        break;
                    }
                    case StyleValue.Type.Number:
                    {
                        var v = catalog.buffers.numbers[sv.index];
                        UnsafeUtility.MemCpy(sptr + offset, &v, 4);
                        offset += 4;
                        break;
                    }
                    case StyleValue.Type.Color:
                    {
                        var v = catalog.buffers.colors[sv.index];
                        UnsafeUtility.MemCpy(sptr + offset, &v, 16);
                        offset += 16;
                        break;
                    }
                    case StyleValue.Type.Rect:
                    {
                        var v = catalog.buffers.rects[sv.index];
                        UnsafeUtility.MemCpy((sptr + offset), &v, 16);
                        offset += 16;
                        break;
                    }
                    default:
                        throw new NotSupportedException("group struct value type not supported");
                }

                if (offset >= structSize)
                    return s;
            }

            return s;
        }

        internal static T GetItemValue<T>(int elementIndex, T defaultValue, int groupIndex, IList<StyleValueGroup> groups, IList<string> strings, IList<float> numbers, IList<Color> colors, IList<StyleRect> rects)
        {
            var sv = groups[groupIndex][elementIndex];
            if (typeof(T) == typeof(string) && sv.type == StyleValue.Type.Text)
                return (T)(object)strings[sv.index];
            if ((typeof(T) == typeof(float) || typeof(T) == typeof(double)) && sv.type == StyleValue.Type.Number)
                return (T)(object)numbers[sv.index];
            if (typeof(T) == typeof(Color) && sv.type == StyleValue.Type.Color)
                return (T)(object)colors[sv.index];
            if (typeof(T) == typeof(StyleRect) && sv.type == StyleValue.Type.Rect)
                return (T)(object)rects[sv.index];

            if (typeof(T) == typeof(int))
            {
                if (sv.type == StyleValue.Type.Number)
                    return (T)(object)numbers[sv.index];
                if (sv.type == StyleValue.Type.Text)
                    return (T)(object)strings[sv.index].GetHashCode();
                if (sv.type == StyleValue.Type.Keyword)
                    return (T)(object)sv.index;
                return defaultValue;
            }

            return defaultValue;
        }

        public bool IsFunction(int key)
        {
            return GetValueIndex(key, StyleValue.Type.Function) != -1;
        }

        public T Execute<T>(int key, Func<StyleFunctionCall, T> callback)
        {
            var index = GetValueIndex(key, StyleValue.Type.Function);
            if (index == -1)
                return default(T);
            var func = catalog.buffers.functions[index];
            var callInfo = new StyleFunctionCall(this, key, func.name, func.args);
            return callback(callInfo);
        }

        public T Execute<T, C>(int key, Func<StyleFunctionCall, C, T> callback, C c)
        {
            var index = GetValueIndex(key, StyleValue.Type.Function);
            if (index == -1)
                return default(T);
            var func = catalog.buffers.functions[index];
            var callInfo = new StyleFunctionCall(this, key, func.name, func.args);
            return callback(callInfo, c);
        }

        public bool HasValue(int key, StyleValue.Type type = StyleValue.Type.Any)
        {
            return GetValueIndex(key, type) != -1;
        }

        public bool HasValue(string key, StyleValue.Type type = StyleValue.Type.Any)
        {
            return HasValue(key.GetHashCode(), type);
        }

        public StyleValue.Type GetValueType(int key)
        {
            StyleValue.Type type = StyleValue.Type.Any;
            int vindex = GetValueIndex(key, ref type);
            if (vindex == -1)
                return StyleValue.Type.Undefined;

            return type;
        }

        public StyleValue.Type GetValueType(string key)
        {
            return GetValueType(key.GetHashCode());
        }

        private int GetValueIndex(string key, StyleValue.Type type)
        {
            return GetValueIndex(key.GetHashCode(), type);
        }

        private int GetValueIndex(int hash, StyleValue.Type type)
        {
            return GetValueIndex(hash, ref type);
        }

        private int GetValueIndex(int hash, ref StyleValue.Type type)
        {
            if (values == null)
                return -1;

            foreach (var stateFlag in states)
            {
                if (stateFlag == StyleState.none)
                    continue;
                bool anyPossibleMatch = false;
                for (int i = 0; i != values.Length; ++i)
                {
                    var v = values[i];
                    if (v.key != hash)
                        continue;
                    if (type != StyleValue.Type.Any && v.type != type)
                        continue;

                    anyPossibleMatch = true;
                    if (v.state == stateFlag)
                    {
                        type = v.type;
                        return v.index;
                    }
                }

                if (!anyPossibleMatch)
                    return -1;
            }
            return -1;
        }

        internal static int GetValueIndex(int hash, IEnumerable<StyleValue> values, StyleValue.Type type, StyleState stateFlag)
        {
            foreach (var v in values)
            {
                if (v.key != hash)
                    continue;
                if (type != StyleValue.Type.Any && v.type != type)
                    continue;

                if (v.state == stateFlag)
                    return v.index;
            }

            return -1;
        }
    }

    internal class StyleCatalog
    {
        internal struct StyleBuffers
        {
            public string[] strings;
            public float[] numbers;
            public Color[] colors;
            public StyleRect[] rects;
            public StyleValueGroup[] groups;
            public StyleFunction[] functions;
        }

        private static readonly StyleValue[] k_NoValue = {};
        private static readonly StyleState[] k_NoState = {};
        private static readonly StyleState[] k_RegularBlockStates = { StyleState.normal };
        private static readonly StyleBlock k_ElementNotFound = new StyleBlock(-1, k_NoState, k_NoValue, null);

        private StyleBlock[] m_Blocks;
        private readonly Dictionary<int, string> m_NameCollisionTable = new Dictionary<int, string>();

        internal StyleBuffers buffers { get; private set; }

        public StyleCatalog()
        {
            buffers = new StyleBuffers();
        }

        private string[] ReadStringBuffer(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            var buffer = new string[length];
            for (int i = 0; i < length; ++i)
                buffer[i] = reader.ReadString();
            return buffer;
        }

        private void WriteBuffer(BinaryWriter writer, string[] buffer)
        {
            writer.Write(buffer.Length);
            foreach (var i in buffer)
                writer.Write(i ?? String.Empty);
        }

        private float[] ReadNumberBuffer(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            var buffer = new float[length];
            for (int i = 0; i < length; ++i)
                buffer[i] = reader.ReadSingle();
            return buffer;
        }

        private void WriteBuffer(BinaryWriter writer, float[] buffer)
        {
            writer.Write(buffer.Length);
            foreach (var i in buffer)
                writer.Write(i);
        }

        private Color[] ReadColorBuffer(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            var buffer = new Color[length];
            for (int i = 0; i < length; ++i)
            {
                var r = reader.ReadSingle();
                var g = reader.ReadSingle();
                var b = reader.ReadSingle();
                var a = reader.ReadSingle();
                buffer[i] = new Color(r, g, b, a);
            }
            return buffer;
        }

        private void WriteBuffer(BinaryWriter writer, Color[] buffer)
        {
            writer.Write(buffer.Length);
            foreach (var i in buffer)
            {
                writer.Write(i.r);
                writer.Write(i.g);
                writer.Write(i.b);
                writer.Write(i.a);
            }
        }

        private StyleRect[] ReadRectBuffer(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            var buffer = new StyleRect[length];
            for (int i = 0; i < length; ++i)
            {
                var top = reader.ReadSingle();
                var right = reader.ReadSingle();
                var bottom = reader.ReadSingle();
                var left = reader.ReadSingle();
                buffer[i] = new StyleRect(top, right, bottom, left);
            }
            return buffer;
        }

        private void WriteBuffer(BinaryWriter writer, StyleRect[] buffer)
        {
            writer.Write(buffer.Length);
            foreach (var i in buffer)
            {
                writer.Write(i.top);
                writer.Write(i.right);
                writer.Write(i.bottom);
                writer.Write(i.left);
            }
        }

        private StyleValueGroup[] ReadValueGroupBuffer(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            var buffer = new StyleValueGroup[length];
            for (int i = 0; i < length; ++i)
            {
                var valueCount = reader.ReadInt32();
                buffer[i] = new StyleValueGroup(0, valueCount)
                {
                    v1 = ReadValue(reader),
                    v2 = ReadValue(reader),
                    v3 = ReadValue(reader),
                    v4 = ReadValue(reader),
                    v5 = ReadValue(reader)
                };
            }
            return buffer;
        }

        private void WriteBuffer(BinaryWriter writer, StyleValueGroup[] buffer)
        {
            writer.Write(buffer.Length);
            foreach (var i in buffer)
            {
                writer.Write(i.count);
                WriteValue(writer, i.v1);
                WriteValue(writer, i.v2);
                WriteValue(writer, i.v3);
                WriteValue(writer, i.v4);
                WriteValue(writer, i.v5);
            }
        }

        private StyleFunction[] ReadFunctionBuffer(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            var buffer = new StyleFunction[length];
            for (int i = 0; i < length; ++i)
            {
                var funcName = reader.ReadString();
                var argsCount = reader.ReadInt32();
                var funcArgs = new List<StyleValue[]>(argsCount);
                for (int argListIndex = 0; argListIndex < argsCount; ++argListIndex)
                {
                    var argListCount = reader.ReadInt32();
                    var values = new StyleValue[argListCount];
                    for (int valueIndex = 0; valueIndex < argListCount; ++valueIndex)
                        values[valueIndex] = ReadValue(reader);
                    funcArgs.Add(values);
                }

                buffer[i] = new StyleFunction
                {
                    name = funcName,
                    args = funcArgs
                };
            }
            return buffer;
        }

        private void WriteBuffer(BinaryWriter writer, StyleFunction[] buffer)
        {
            writer.Write(buffer.Length);
            foreach (var i in buffer)
            {
                writer.Write(i.name);
                writer.Write(i.args.Count);
                foreach (var arg in i.args)
                {
                    writer.Write(arg.Length);
                    foreach (var value in arg)
                        WriteValue(writer, value);
                }
            }
        }

        private StyleValue ReadValue(BinaryReader reader)
        {
            StyleValue v = new StyleValue
            {
                key = reader.ReadInt32(),
                type = (StyleValue.Type)reader.ReadInt32(),
                index = reader.ReadInt32(),
                state = (StyleState)reader.ReadInt64()
            };
            return v;
        }

        private void WriteValue(BinaryWriter writer, StyleValue v)
        {
            writer.Write(v.key);
            writer.Write((int)v.type);
            writer.Write(v.index);
            writer.Write((long)v.state);
        }

        private StyleBlock ReadBlock(BinaryReader reader)
        {
            int name = reader.ReadInt32();

            int styleStateCount = reader.ReadInt32();
            var states = new StyleState[styleStateCount];
            for (int i = 0; i < styleStateCount; ++i)
                states[i] = (StyleState)reader.ReadInt64();

            int valueCount = reader.ReadInt32();
            var values = new StyleValue[valueCount];
            for (int i = 0; i < valueCount; ++i)
                values[i] = ReadValue(reader);

            return new StyleBlock(name, states, values, this);
        }

        private void WriteBlock(BinaryWriter writer, StyleBlock b)
        {
            writer.Write(b.name);
            writer.Write(b.states.Length);
            foreach (var s in b.states)
                writer.Write((int)s);
            writer.Write(b.values.Length);
            foreach (var v in b.values)
                WriteValue(writer, v);
        }

        public bool Load(BinaryReader reader)
        {
            // version
            if (reader.ReadInt32() != k_CacheVersion)
                return false;

            // name table
            m_NameCollisionTable.Clear();
            var nameCount = reader.ReadInt32();
            for (int i = 0; i < nameCount; ++i)
            {
                var key = reader.ReadInt32();
                var value = reader.ReadString();
                m_NameCollisionTable[key] = value;
            }

            // buffers
            buffers = new StyleBuffers
            {
                strings = ReadStringBuffer(reader),
                numbers = ReadNumberBuffer(reader),
                colors = ReadColorBuffer(reader),
                rects = ReadRectBuffer(reader),
                groups = ReadValueGroupBuffer(reader),
                functions = ReadFunctionBuffer(reader)
            };

            // blocks
            var blockCount = reader.ReadInt32();
            m_Blocks = new StyleBlock[blockCount];
            for (int i = 0; i < blockCount; ++i)
                m_Blocks[i] = ReadBlock(reader);

            return true;
        }

        const int k_CacheVersion = 4;
        public void Save(BinaryWriter writer)
        {
            // version
            writer.Write(k_CacheVersion);

            // name table
            writer.Write(m_NameCollisionTable.Count);
            foreach (var kvp in m_NameCollisionTable)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }

            // buffers
            WriteBuffer(writer, buffers.strings);
            WriteBuffer(writer, buffers.numbers);
            WriteBuffer(writer, buffers.colors);
            WriteBuffer(writer, buffers.rects);
            WriteBuffer(writer, buffers.groups);
            WriteBuffer(writer, buffers.functions);

            // blocks
            writer.Write(m_Blocks.Length);
            foreach (var b in m_Blocks)
                WriteBlock(writer, b);
        }

        public int FindStyleIndex(int key)
        {
            return FindStyleIndex(key, m_Blocks);
        }

        public static int FindStyleIndex(int key, StyleBlock[] blocks)
        {
            if (blocks == null || blocks.Length == 0)
                return -1;

            int low = 0;
            int high = blocks.Length - 1;
            int middle = (low + high + 1) / 2;
            do
            {
                int currentKey = blocks[middle].name;
                if (key == currentKey)
                    return middle;

                if (key < currentKey)
                    high = middle - 1;
                else
                    low = middle + 1;
                middle = (low + high + 1) / 2;
            }
            while (low <= high);

            return -1;
        }

        public static int FindStyleIndex(int key, IList<StyleBlock> blocks)
        {
            if (blocks == null || blocks.Count == 0)
                return -1;

            int low = 0;
            int high = blocks.Count - 1;
            int middle = (low + high + 1) / 2;
            do
            {
                int currentKey = blocks[middle].name;
                if (key == currentKey)
                    return middle;

                if (key < currentKey)
                    high = middle - 1;
                else
                    low = middle + 1;
                middle = (low + high + 1) / 2;
            }
            while (low <= high);

            return -1;
        }

        public StyleBlock GetStyle(int selectorKey, params StyleState[] states)
        {
            int location = FindStyleIndex(selectorKey, m_Blocks);
            if (location != -1)
            {
                var foundStyle = m_Blocks[location];
                return new StyleBlock(foundStyle.name, states.Length == 0 ? k_RegularBlockStates : states, foundStyle.values, foundStyle.catalog);
            }
            return k_ElementNotFound;
        }

        public StyleBlock GetStyle(string selectorName, params StyleState[] states)
        {
            return GetStyle(selectorName.GetHashCode(), states);
        }

        public StyleBlock GetComposedStyle(int selectorKey, params StyleState[] states)
        {
            return GetStyle(selectorKey, states.Concat(new[] {StyleState.normal}).ToArray());
        }

        public void Load(params string[] paths)
        {
            Load((IEnumerable<string>)paths);
        }

        public void Load(IEnumerable<string> paths)
        {
            var sheets = paths.Select(p =>
            {
                var x = EditorResources.Load<UnityEngine.Object>(p, false) as StyleSheet;

                if (x == null)
                    Debug.Log("Could not load " + p);
                return x;
            })
                .Where(s => s != null).Distinct();

            Load(sheets);
        }

        public void Load(IEnumerable<StyleSheet> sheets)
        {
            try
            {
                var resolver = new StyleSheetResolver();
                resolver.AddStyleSheets(sheets.ToArray());
                resolver.Resolve();
                Load(resolver);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Cannot load Style Catalog, StyleSheet resolving fails: " + ex);
            }
        }

        public void Load(StyleSheetResolver resolver)
        {
            var strings = new List<string>();
            var numbers = new List<float>();
            var rects = new List<StyleRect>();
            var colors = new List<Color>();
            var blocks = new List<StyleBlock>();
            var groups = new List<StyleValueGroup>();
            var functions = new List<StyleFunction>();

            m_NameCollisionTable.Clear();
            try
            {
                Compile(resolver, numbers, colors, strings, rects, groups, functions, blocks);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Cannot compile StyleCatalog: " + ex);
            }

            buffers = new StyleBuffers
            {
                strings = strings.ToArray(),
                numbers = numbers.ToArray(),
                colors = colors.ToArray(),
                rects = rects.ToArray(),
                groups = groups.ToArray(),
                functions = functions.ToArray()
            };

            m_Blocks = blocks.ToArray();
        }

        private static StyleState GetSelectorStateFlags(StyleSelector ss)
        {
            StyleState stateFlags = 0;
            for (int p = 0; p < ss.parts.Length; ++p)
            {
                var part = ss.parts[p];
                if (part.type == StyleSelectorType.PseudoClass)
                {
                    try
                    {
                        stateFlags |= (StyleState)Enum.Parse(typeof(StyleState), part.value.Replace("-", ""), true);
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }

            if (stateFlags == 0)
                stateFlags = StyleState.normal;

            return stateFlags;
        }

        private static string GetSelectorKeyName(StyleSelector ss)
        {
            string name = String.Empty;
            for (int p = 0; p < ss.parts.Length; ++p)
            {
                var part = ss.parts[p];
                if (part.type == StyleSelectorType.ID ||
                    part.type == StyleSelectorType.Type ||
                    part.type == StyleSelectorType.Class ||
                    part.type == StyleSelectorType.Wildcard)
                {
                    if (p > 0)
                        name += ".";
                    name += part.value;
                }
            }
            return name;
        }

        private int GetNameKey(string name)
        {
            int key = name.GetHashCode();
            if (m_NameCollisionTable.ContainsKey(key) && m_NameCollisionTable[key] != name)
                throw new ArgumentException($"Style key name `{name}` collides with `{m_NameCollisionTable[key]}`. You should tweak either name.", nameof(name));
            m_NameCollisionTable[key] = name;
            return key;
        }

        private string GetKeyName(int key)
        {
            string name;
            if (m_NameCollisionTable.TryGetValue(key, out name))
                return name;
            return "<unknown>";
        }

        private static StyleValue[] MergeValues(IEnumerable<StyleValue> values, IEnumerable<StyleValue> newValues)
        {
            var mergedBlockValues = new List<StyleValue>(values);
            return MergeListValues(mergedBlockValues, newValues).ToArray();
        }

        private static List<StyleValue> MergeListValues(List<StyleValue> mergedBlockValues, IEnumerable<StyleValue> newValues)
        {
            foreach (var newValue in newValues)
            {
                bool valueMerged = false;
                for (int j = 0; j < mergedBlockValues.Count; ++j)
                {
                    var blockValue = mergedBlockValues[j];
                    if (newValue.key == blockValue.key && newValue.state == blockValue.state && blockValue.type == newValue.type)
                    {
                        blockValue.index = newValue.index;
                        mergedBlockValues[j] = blockValue;
                        valueMerged = true;
                        break;
                    }
                }

                if (!valueMerged)
                    mergedBlockValues.Add(newValue);
            }

            return mergedBlockValues;
        }

        private void CompileRects(StyleBlock block, StyleValue[] values, IList<StyleRect> rects)
        {
            // Fix rect default values with normal state.
            foreach (var v in values)
            {
                if (v.type != StyleValue.Type.Rect)
                    continue;

                var rect = rects[v.index];
                if (!rect.incomplete)
                    continue;

                if (v.state == StyleState.normal)
                    continue;

                // Merge missing values with normal state or default
                var normalRectBufferIndex = StyleBlock.GetValueIndex(v.key, block.values, StyleValue.Type.Rect, StyleState.normal);
                if (normalRectBufferIndex == -1)
                {
                    rects[v.index] = rect.@fixed;
                    continue;
                }

                var normalRect = rects[normalRectBufferIndex];
                rects[v.index] = new StyleRect
                {
                    top = rect.top == float.MaxValue ? normalRect.top : rect.top,
                    right = rect.right == float.MaxValue ? normalRect.right : rect.right,
                    bottom = rect.bottom == float.MaxValue ? normalRect.bottom : rect.bottom,
                    left = rect.left == float.MaxValue ? normalRect.left : rect.left,
                }.@fixed;
            }
        }

        private bool CompileElement(string name, IList<StyleBlock> blocks, StyleValue[] values, IList<StyleRect> rects)
        {
            int key = GetNameKey(name);

            int existingStyleIndex = FindStyleIndex(key, blocks);
            if (existingStyleIndex == -1)
            {
                blocks.Add(new StyleBlock(key, k_NoState, values, this));
                return true;
            }

            var block = blocks[existingStyleIndex];
            CompileRects(block, values, rects);

            var mergedBlockValues = MergeValues(block.values, values);
            blocks[existingStyleIndex] = new StyleBlock(key, k_NoState, mergedBlockValues, this);
            return false;
        }

        private static int SetIndex<T>(IList<T> buffer, T c)
        {
            var bufferIndex = buffer.IndexOf(c);
            if (bufferIndex == -1)
            {
                buffer.Add(c);
                return buffer.Count - 1;
            }

            return bufferIndex;
        }

        private void Compile(StyleSheetResolver resolver,
            List<float> numbers, List<Color> colors, List<string> strings, List<StyleRect> rects,
            List<StyleValueGroup> groups, List<StyleFunction> functions,
            List<StyleBlock> blocks)
        {
            foreach (var rule in resolver.Rules.Values)
            {
                // We do not yet support block with multiple selectors.
                if (rule.Selector.selectors.Length != 1 || rule.Selector.selectors[0].parts.Length == 0)
                    continue;

                var selector = rule.Selector.selectors[0];
                string blockName = GetSelectorKeyName(selector);
                StyleState stateFlags = GetSelectorStateFlags(selector);

                List<StyleValue> values = new List<StyleValue>(rule.Properties.Count);
                foreach (var property in rule.Properties.Values)
                {
                    var newValue = CompileValue(property, stateFlags, numbers, colors, strings, rects, groups, functions);
                    if (newValue.IsValid())
                        values.Add(newValue);
                }

                values = ExpandValues(values, numbers, colors, strings, rects, groups);

                if (CompileElement(blockName, blocks, values.ToArray(), rects))
                    blocks.Sort((l, r) => l.name.CompareTo(r.name));

                // Fix rects
                for (int r = 0; r < rects.Count; ++r)
                {
                    if (rects[r].incomplete)
                        rects[r] = rects[r].@fixed;
                }
            }
        }

        private List<StyleValue> ExpandValues(List<StyleValue> values,
            List<float> numbers, List<Color> colors, List<string> strings, List<StyleRect> rects, List<StyleValueGroup> groups)
        {
            var states = values.Select(v => v.state).Distinct().ToArray();

            // Rects
            values = ExpandRect(states, values, numbers, rects, StyleCatalogKeyword.position, StyleCatalogKeyword.top, StyleCatalogKeyword.right, StyleCatalogKeyword.bottom, StyleCatalogKeyword.left);
            values = ExpandRect(states, values, numbers, rects, StyleCatalogKeyword.margin, StyleCatalogKeyword.marginTop, StyleCatalogKeyword.marginRight, StyleCatalogKeyword.marginBottom, StyleCatalogKeyword.marginLeft);
            values = ExpandRect(states, values, numbers, rects, StyleCatalogKeyword.padding, StyleCatalogKeyword.paddingTop, StyleCatalogKeyword.paddingRight, StyleCatalogKeyword.paddingBottom, StyleCatalogKeyword.paddingLeft);
            values = ExpandRect(states, values, numbers, rects, "-unity-overflow".GetHashCode(), "-unity-overflow-top".GetHashCode(), "-unity-overflow-right".GetHashCode(), "-unity-overflow-bottom".GetHashCode(), "-unity-overflow-left".GetHashCode());
            values = ExpandRect(states, values, numbers, rects, "-unity-slice".GetHashCode(), "-unity-slice-top".GetHashCode(), "-unity-slice-right".GetHashCode(), "-unity-slice-bottom".GetHashCode(), "-unity-slice-left".GetHashCode());

            // Lines
            values = ExpandLine(states, values, numbers, colors, strings, rects, groups, StyleCatalogKeyword.border, StyleCatalogKeyword.borderWidth, StyleCatalogKeyword.borderStyle, StyleCatalogKeyword.borderColor);

            // Extended styles
            values = ExpandRect(states, values, numbers, rects, StyleCatalogKeyword.borderWidth, StyleCatalogKeyword.borderTopWidth, StyleCatalogKeyword.borderRightWidth, StyleCatalogKeyword.borderBottomWidth, StyleCatalogKeyword.borderLeftWidth);
            values = ExpandRect(states, values, numbers, rects, StyleCatalogKeyword.borderRadius, StyleCatalogKeyword.borderTopLeftRadius, StyleCatalogKeyword.borderTopRightRadius, StyleCatalogKeyword.borderBottomRightRadius, StyleCatalogKeyword.borderBottomLeftRadius);

            return values;
        }

        private List<StyleValue> ExpandLine(StyleState[] states, List<StyleValue> values, List<float> numbers, List<Color> colors, List<string> strings, List<StyleRect> rects, List<StyleValueGroup> groups,
            int borderKey, int borderWidthKey, int borderStyleKey, int borderColorKey)
        {
            if (!ExpandHasMembers(values, borderKey, borderWidthKey, borderStyleKey, borderColorKey))
                return values;

            //using (new Profiling.EditorPerformanceTracker("BuildCatalog.ExpandLine"))
            for (int stateIndex = 0; stateIndex < states.Length; ++stateIndex)
            {
                bool applyValues = false;
                var currentState = states[stateIndex];
                var lineValues = new List<StyleValue>();
                var line = new StyleLine();
                for (int i = 0; i < values.Count; ++i)
                {
                    var value = values[i];
                    if (value.state != currentState)
                        continue;

                    if (value.key == borderKey && value.type == StyleValue.Type.Group)
                    {
                        var width = StyleBlock.GetItemValue(0, 0f, value.index, groups, strings, numbers, colors, rects);
                        var style = StyleBlock.GetItemValue(1, "solid", value.index, groups, strings, numbers, colors, rects);
                        var color = StyleBlock.GetItemValue(2, Color.red, value.index, groups, strings, numbers, colors, rects);
                        line = new StyleLine(width, style, color);
                    }
                    else if (value.key == borderWidthKey && value.type == StyleValue.Type.Number) line.width = numbers[value.index];
                    else if (value.key == borderStyleKey && value.type == StyleValue.Type.Text) line.style = strings[value.index];
                    else if (value.key == borderColorKey && value.type == StyleValue.Type.Color) line.color = colors[value.index];
                    else
                    {
                        // No match
                        continue;
                    }

                    applyValues = true;
                }

                if (!applyValues)
                    continue;

                StyleValueGroup vg = new StyleValueGroup(borderKey, 3)
                {
                    v1 = new StyleValue { key = borderWidthKey, state = currentState, type = StyleValue.Type.Number, index = SetIndex(numbers, line.width) },
                    v2 = new StyleValue { key = borderStyleKey, state = currentState, type = StyleValue.Type.Text, index = SetIndex(strings, line.style) },
                    v3 = new StyleValue { key = borderColorKey, state = currentState, type = StyleValue.Type.Color, index = SetIndex(colors, line.color) }
                };

                lineValues.Add(new StyleValue { key = borderKey, state = currentState, type = StyleValue.Type.Group, index = SetIndex(groups, vg) });
                lineValues.Add(vg.v1);
                lineValues.Add(vg.v2);
                lineValues.Add(vg.v3);

                if (lineValues.Count > 0)
                    values = MergeListValues(values, lineValues);
            }

            return values;
        }

        private List<StyleValue> ExpandRect(StyleState[] states, List<StyleValue> values, List<float> numbers, List<StyleRect> rects,
            int rectKey, int topKey, int rightKey, int bottomKey, int leftKey)
        {
            if (!ExpandHasMembers(values, rectKey, topKey, rightKey, bottomKey, leftKey))
                return values;

            for (int stateIndex = 0; stateIndex < states.Length; ++stateIndex)
            {
                bool applyValues = false;
                var currentState = states[stateIndex];
                var rect = new StyleRect { top = float.MaxValue, right = float.MaxValue, bottom = float.MaxValue, left = float.MaxValue };
                for (int i = 0; i < values.Count; ++i)
                {
                    var value = values[i];
                    if (!value.IsValid() || value.state != currentState)
                        continue;

                    if (value.key == rectKey && value.type == StyleValue.Type.Rect) rect = rects[value.index];
                    else if (value.key == rectKey && value.type == StyleValue.Type.Number)
                    {
                        var defaultValue = numbers[value.index];
                        if (rect.top == float.MaxValue) rect.top = defaultValue;
                        if (rect.right == float.MaxValue) rect.right = defaultValue;
                        if (rect.bottom == float.MaxValue) rect.bottom = defaultValue;
                        if (rect.left == float.MaxValue) rect.left = defaultValue;
                    }
                    else if (value.key == topKey && value.type == StyleValue.Type.Number) rect.top = numbers[value.index];
                    else if (value.key == rightKey && value.type == StyleValue.Type.Number) rect.right = numbers[value.index];
                    else if (value.key == bottomKey && value.type == StyleValue.Type.Number) rect.bottom = numbers[value.index];
                    else if (value.key == leftKey && value.type == StyleValue.Type.Number) rect.left = numbers[value.index];
                    else
                    {
                        // No match
                        continue;
                    }

                    applyValues = true;
                }

                if (!applyValues)
                    continue;

                var rectValues = new List<StyleValue>();
                if (rect.left != float.MaxValue || rect.right != float.MaxValue || rect.top != float.MaxValue || rect.bottom != float.MaxValue)
                    rectValues.Add(new StyleValue { key = rectKey, state = currentState, type = StyleValue.Type.Rect, index = SetIndex(rects, rect) });

                if (rect.top != float.MaxValue)
                    rectValues.Add(new StyleValue { key = topKey, state = currentState, type = StyleValue.Type.Number, index = SetIndex(numbers, rect.top) });

                if (rect.right != float.MaxValue)
                    rectValues.Add(new StyleValue { key = rightKey, state = currentState, type = StyleValue.Type.Number, index = SetIndex(numbers, rect.right) });

                if (rect.bottom != float.MaxValue)
                    rectValues.Add(new StyleValue { key = bottomKey, state = currentState, type = StyleValue.Type.Number, index = SetIndex(numbers, rect.bottom) });

                if (rect.left != float.MaxValue)
                    rectValues.Add(new StyleValue { key = leftKey, state = currentState, type = StyleValue.Type.Number, index = SetIndex(numbers, rect.left) });

                if (rectValues.Count > 0)
                    values = MergeListValues(values, rectValues);
            }

            return values;
        }

        private static bool ExpandHasMembers(List<StyleValue> values, int k1, int k2, int k3, int k4)
        {
            for (int i = 0; i < values.Count; ++i)
            {
                var k = values[i].key;
                if (k == k1 || k == k2 || k == k3 || k == k4)
                    return true;
            }

            return false;
        }

        private static bool ExpandHasMembers(List<StyleValue> values, int k1, int k2, int k3, int k4, int k5)
        {
            for (int i = 0; i < values.Count; ++i)
            {
                var k = values[i].key;
                if (k == k1 || k == k2 || k == k3 || k == k4 || k == k5)
                    return true;
            }

            return false;
        }

        private StyleValue CompileValue(StyleSheetResolver.Property property, StyleState stateFlags,
            List<float> numbers, List<Color> colors, List<string> strings, List<StyleRect> rects, List<StyleValueGroup> groups, List<StyleFunction> functions)
        {
            var values = property.Values;

            if (values.Count == 0)
                return StyleValue.Undefined(property.Name, stateFlags);

            if (values.Count == 1)
            {
                if (values[0].ValueType == StyleValueType.Function)
                    return CompileFunction(property.Name, values[0], stateFlags, numbers, colors, strings, functions);

                return CompileBaseValue(property.Name, stateFlags, values[0], numbers, colors, strings);
            }

            if (values.Count == 2 &&
                values[0].IsFloat() &&
                values[1].IsFloat())
                return CompileRect(property.Name, values, stateFlags, rects, 0, 1, 0, 1);

            if (values.Count == 3 &&
                values[0].IsFloat() &&
                values[1].IsFloat() &&
                values[2].IsFloat())
                return CompileRect(property.Name, values, stateFlags, rects, 0, 1, 2, 1);

            if (values.Count == 4 &&
                values[0].IsFloat() &&
                values[1].IsFloat() &&
                values[2].IsFloat() &&
                values[3].IsFloat())
                return CompileRect(property.Name, values, stateFlags, rects, 0, 1, 2, 3);

            // Compile list of primitive values
            if (values.Count >= 2 && values.Count <= 5)
                return CompileValueGroup(property.Name, values, stateFlags, groups, numbers, colors, strings);

            // Value form not supported, lets report it and keep a undefined value to the property.
            Debug.LogWarning($"Failed to compile style block property {property.Name} " +
                $"with {values.Count} values");
            return StyleValue.Undefined(GetNameKey(property.Name), stateFlags);
        }

        private StyleValue CompileFunction(string propertyName, StyleSheetResolver.Value value, StyleState stateFlags,
            List<float> numbers, List<Color> colors, List<string> strings, List<StyleFunction> functions)
        {
            var func = value as StyleSheetResolver.Function;
            var funcName = value.Obj as string;
            var funcArgs = new List<StyleValue[]>();

            foreach (var argList in func.args)
            {
                int argIndex = 0;
                var argValues = new StyleValue[argList.Length];
                foreach (var arg in argList)
                {
                    argValues[argIndex] = CompileBaseValue(argIndex.ToString(), stateFlags, arg, numbers, colors, strings);
                    argIndex++;
                }
                funcArgs.Add(argValues);
            }

            return new StyleValue
            {
                key = GetNameKey(propertyName),
                state = stateFlags,
                type = StyleValue.Type.Function,
                index = SetIndex(functions, new StyleFunction
                {
                    name = funcName,
                    args = funcArgs
                })
            };
        }

        private StyleValue CompileRect(string propertyName, List<StyleSheetResolver.Value> values, StyleState stateFlags,
            IList<StyleRect> rects, int topIndex, int rightIndex, int bottomIndex, int leftIndex)
        {
            return new StyleValue
            {
                key = GetNameKey(propertyName),
                state = stateFlags,
                type = StyleValue.Type.Rect,
                index = SetIndex(rects, new StyleRect
                {
                    top = values[topIndex].AsFloat(),
                    right = values[rightIndex].AsFloat(),
                    bottom = values[bottomIndex].AsFloat(),
                    left = values[leftIndex].AsFloat()
                })
            };
        }

        private StyleValue CompileValueGroup(string propertyName, List<StyleSheetResolver.Value> values, StyleState stateFlags,
            List<StyleValueGroup> groups, List<float> numbers, List<Color> colors, List<string> strings)
        {
            int propertyKey = GetNameKey(propertyName);
            StyleValueGroup vg = new StyleValueGroup(propertyKey, values.Count);
            for (int i = 0; i < values.Count; ++i)
                vg[i] = CompileBaseValue(propertyName, stateFlags, values[i], numbers, colors, strings);

            return new StyleValue
            {
                key = propertyKey,
                state = stateFlags,
                type = StyleValue.Type.Group,
                index = SetIndex(groups, vg)
            };
        }

        private StyleValue CompileBaseValue(string propertyName, StyleState stateFlags, StyleSheetResolver.Value value,
            List<float> numbers, List<Color> colors, List<string> strings)
        {
            return new StyleValue
            {
                key = GetNameKey(propertyName),
                state = stateFlags,
                type = ReduceStyleValueType(value),
                index = MergeValue(value, numbers, colors, strings)
            };
        }

        private static StyleValue.Type ReduceStyleValueType(StyleSheetResolver.Value value)
        {
            switch (value.ValueType)
            {
                case StyleValueType.Color:
                case StyleValueType.Float:
                case StyleValueType.Keyword:
                    return (StyleValue.Type)value.ValueType;
                case StyleValueType.Dimension:
                    return StyleValue.Type.Number;
                case StyleValueType.AssetReference:
                case StyleValueType.ResourcePath:
                case StyleValueType.Enum:
                case StyleValueType.Variable:
                case StyleValueType.ScalableImage:
                {
                    var str = value.AsString();
                    // Try a few conversions
                    Color parsedColor;
                    if (ColorUtility.TryParseHtmlString(str, out parsedColor))
                        return StyleValue.Type.Color;

                    return StyleValue.Type.Text;
                }
                case StyleValueType.String:
                    return StyleValue.Type.Text;

                case StyleValueType.Function:
                    return StyleValue.Type.Function;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static int MergeValue(StyleSheetResolver.Value value,
            List<float> numbers, List<Color> colors, List<string> strings)
        {
            switch (value.ValueType)
            {
                case StyleValueType.Keyword:
                    return (int)StyleValue.ConvertKeyword(value.AsKeyword());
                case StyleValueType.Float:
                    return SetIndex(numbers, value.AsFloat());
                case StyleValueType.Dimension:
                    return SetIndex(numbers, value.AsDimension().value);
                case StyleValueType.Color:
                    return SetIndex(colors, value.AsColor());
                case StyleValueType.ResourcePath:
                    return SetIndex(strings, value.ToString());
                case StyleValueType.Enum:
                case StyleValueType.Variable:
                    var str = value.AsString();
                    // Try a few conversions
                    Color parsedColor;
                    if (ColorUtility.TryParseHtmlString(str, out parsedColor))
                        return SetIndex(colors, parsedColor);
                    return SetIndex(strings, str);
                case StyleValueType.AssetReference:
                    return SetIndex(strings, AssetDatabase.GetAssetPath(value.AsAssetReference()));
                case StyleValueType.ScalableImage:
                    return SetIndex(strings, AssetDatabase.GetAssetPath(value.AsScalableImage().normalImage));
                case StyleValueType.String:
                    return SetIndex(strings, value.AsString());
                default:
                    throw new Exception("Unknown value type: " + value.ValueType);
            }
        }

        #region ComparisonUtilities

        internal bool CompareCatalog(StyleCatalog catalog, out string msg)
        {
            var sb = new StringBuilder();
            CompareBlocks(catalog, "Missing Styles", sb);
            catalog.CompareBlocks(this, "Sup Styles", sb);

            if (sb.Length > 0)
            {
                msg = sb.ToString();
                return false;
            }

            // All the blocks are the same, compare their values;
            foreach (var catBlock in catalog.m_Blocks)
            {
                var blockName = GetKeyName(catBlock.name);
                var block = m_Blocks[FindStyleIndex(catBlock.name, m_Blocks)];

                if (catBlock.name != block.name)
                {
                    sb.AppendLine("Block ordering not the same: " + blockName);
                    break;
                }

                if (!block.states.Equals(catBlock.states))
                {
                    sb.AppendLine("Differences in States in block: " + blockName);
                }
                else if (block.values.Length != catBlock.values.Length)
                {
                    sb.AppendLine("Not same number of values in block: " + blockName);
                }
                else
                {
                    for (var i = 0; i < block.values.Length; ++i)
                    {
                        var value1 = block.values[i];
                        var value2Index = GetComparableValue(value1, catBlock);
                        if (value2Index == -1)
                        {
                            sb.AppendLine($"Property {GetKeyName(value1.key)} not found in block {blockName}");
                            continue;
                        }

                        var value2 = catBlock.values[value2Index];
                        if (value1.type != value2.type)
                        {
                            sb.AppendLine($"Property: {GetKeyName(value1.key)} has different type in block {blockName}");
                        }
                        else if (!CompareValue(block, value1, catBlock, value2))
                        {
                            sb.AppendLine($"Property: {GetKeyName(value1.key)} has different value in block {blockName}");
                        }
                    }
                }
            }

            msg = sb.ToString();
            return msg.Length == 0;
        }

        internal bool CompareBlocks(StyleCatalog catalog, string title, StringBuilder sb)
        {
            var sameStyles = true;
            foreach (var block in m_Blocks)
            {
                if (catalog.FindStyleIndex(block.name) == -1)
                {
                    if (sameStyles)
                    {
                        sb.AppendLine(title);
                        sameStyles = false;
                    }
                    sb.AppendLine("    " + GetKeyName(block.name));
                }
            }

            return sameStyles;
        }

        internal static int GetComparableValue(StyleValue v1, StyleBlock block)
        {
            for (var valueIndex = 0; valueIndex < block.values.Length; ++valueIndex)
            {
                var v2 = block.values[valueIndex];
                if (v1.key == v2.key && v1.state == v2.state && v1.type == v2.type)
                {
                    return valueIndex;
                }
            }

            return -1;
        }

        internal static bool CompareValue(StyleBlock b1, StyleValue value1, StyleBlock b2, StyleValue value2)
        {
            switch (value1.type)
            {
                case StyleValue.Type.Color:
                {
                    var v1 = b1.GetColor(value1.key);
                    var v2 = b2.GetColor(value2.key);
                    return v1 == v2;
                }
                case StyleValue.Type.Keyword:
                {
                    var v1 = b1.GetKeyword(value1.key);
                    var v2 = b2.GetKeyword(value2.key);
                    return v1 == v2;
                }
                case StyleValue.Type.Number:
                {
                    var v1 = b1.GetFloat(value1.key);
                    var v2 = b2.GetFloat(value2.key);
                    return Mathf.Abs(v1 - v2) < 0.001f;
                }
                case StyleValue.Type.Text:
                {
                    var v1 = b1.GetText(value1.key);
                    var v2 = b2.GetText(value2.key);
                    return v1 == v2;
                }
                case StyleValue.Type.Group:
                {
                    var group1 = b1.catalog.buffers.groups[value1.index];
                    var group2 = b2.catalog.buffers.groups[value2.index];

                    return CompareValue(b1, group1.v1, b2, group2.v1) &&
                        CompareValue(b1, group1.v2, b2, group2.v2) &&
                        CompareValue(b1, group1.v3, b2, group2.v3) &&
                        CompareValue(b1, group1.v4, b2, group2.v4) &&
                        CompareValue(b1, group1.v5, b2, group2.v5);
                }
                case StyleValue.Type.Rect:
                {
                    var v1 = b1.catalog.buffers.rects[value1.index];
                    var v2 = b2.catalog.buffers.rects[value2.index];
                    return Mathf.Abs(v1.left - v2.left) < 0.001f ||
                        Mathf.Abs(v1.right - v2.right) < 0.001f ||
                        Mathf.Abs(v1.top - v2.top) < 0.001f ||
                        Mathf.Abs(v1.bottom - v2.bottom) < 0.001f;
                }
            }

            return true;
        }

        #endregion
    }
}
