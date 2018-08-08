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
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.StyleSheets;
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

    internal static class StyleKeyword
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
        public readonly static int backgroundPosition = "background-position".GetHashCode();
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
        public readonly static int overflow = "overflow".GetHashCode();
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
        public SVC(int property, T defaultValue = default(T)) : this(StyleKeyword.root, property, defaultValue, StyleState.root) {}
        public SVC(string property, T defaultValue = default(T)) : this(StyleKeyword.root, property.GetHashCode(), defaultValue, StyleState.root) {}

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
    internal struct StyleRect
    {
        public static readonly StyleRect Nil = new StyleRect { left = float.NaN, right = float.NaN, top = float.NaN, bottom = float.NaN };
        public static readonly StyleRect Zero = new StyleRect { left = 0f, right = 0f, top = 0f, bottom = 0f };

        public static StyleRect Size(float width, float height)
        {
            return new StyleRect {width = width, height = height, bottom = width, left = height};
        }

        public StyleRect(RectOffset offset)
        {
            width = height = 0;
            top = offset.top;
            right = offset.right;
            bottom = offset.bottom;
            left = offset.left;
        }

        [FieldOffset(0)] public float top;
        [FieldOffset(4)] public float right;
        [FieldOffset(8)] public float bottom;
        [FieldOffset(12)] public float left;

        [FieldOffset(0)] public float width;
        [FieldOffset(4)] public float height;
    }

    [DebuggerDisplay("count = {count}")]
    internal struct StyleValueGroup
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
    }

    [DebuggerDisplay("key = {key}, type = {type}, index = {index}, state = {state}")]
    internal struct StyleValue
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
                    throw new ArgumentOutOfRangeException(nameof(svk), svk, null);
            }
        }

        public int key;
        public Type type;
        public int index;
        public StyleState state;
    }

    [DebuggerDisplay("name = {name}")]
    internal struct StyleBlock
    {
        public int name;
        public StyleState[] states;
        public StyleValue[] values;
        public StyleCatalog catalog;

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

        public StyleValue.Keyword GetKeyword(int key)
        {
            return (StyleValue.Keyword)GetValueIndex(key, StyleValue.Type.Keyword);
        }

        public StyleValue.Keyword GetKeyword(string key)
        {
            return (StyleValue.Keyword)GetValueIndex(key, StyleValue.Type.Keyword);
        }

        public bool GetBool(int key)
        {
            return (StyleValue.Keyword)GetValueIndex(key, StyleValue.Type.Keyword) == StyleValue.Keyword.True;
        }

        public bool GetBool(string key)
        {
            return (StyleValue.Keyword)GetValueIndex(key, StyleValue.Type.Keyword) == StyleValue.Keyword.True;
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

        public float GetRectFloat(int key, float defaultValue = 0.0f)
        {
            var bufferIndex = GetValueIndex(key, StyleValue.Type.Number);
            if (bufferIndex != -1)
                return catalog.buffers.numbers[bufferIndex];
            return defaultValue;
        }

        public float GetRectFloat(string key, float defaultValue = 0.0f)
        {
            var bufferIndex = GetValueIndex(key, StyleValue.Type.Number);
            if (bufferIndex != -1)
                return catalog.buffers.numbers[bufferIndex];

            var parts = key.Split('-');
            if (parts.Length != 2)
                return defaultValue;

            var rect = GetRect(parts[0]);
            if (parts[1] == "left")
                return rect.left;
            if (parts[1] == "right")
                return rect.right;
            if (parts[1] == "top")
                return rect.top;
            if (parts[1] == "bottom")
                return rect.bottom;
            return defaultValue;
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
            var bufferIndex = GetValueIndex(key, StyleValue.Type.Rect);
            if (bufferIndex == -1)
            {
                return new StyleRect
                {
                    top = GetFloat(key + "-top", defaultValue.top),
                    right = GetFloat(key + "-right", defaultValue.right),
                    bottom = GetFloat(key + "-bottom", defaultValue.bottom),
                    left = GetFloat(key + "-left", defaultValue.left)
                };
            }
            return catalog.buffers.rects[bufferIndex];
        }

        public int GetInt(int key, int defaultValue = 0)
        {
            return (int)GetFloat(key, defaultValue);
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            return (int)GetFloat(key, defaultValue);
        }

        public T GetResource<T>(int key) where T : UnityEngine.Object
        {
            return EditorResources.Load<UnityEngine.Object>(GetText(key), false) as T;
        }

        public T GetResource<T>(string key) where T : UnityEngine.Object
        {
            return EditorResources.Load<UnityEngine.Object>(GetText(key), false) as T;
        }

        public Texture2D GetTexture(int key, bool autoScale = false)
        {
            var resourcePath = GetText(key);
            if (String.IsNullOrEmpty(resourcePath))
                return null;

            float systemScale = GUIUtility.pixelsPerPoint;
            if (autoScale && systemScale > 1f)
            {
                int scale = Mathf.RoundToInt(systemScale);
                string dirName = Path.GetDirectoryName(resourcePath).Replace('\\', '/');
                string fileName = Path.GetFileNameWithoutExtension(resourcePath);
                string fileExt = Path.GetExtension(resourcePath);
                for (int s = scale; scale > 1; --scale)
                {
                    string scaledResourcePath = $"{dirName}/{fileName}@{s}x{fileExt}";
                    var scaledResource = EditorResources.Load<Texture2D>(scaledResourcePath, false);
                    if (scaledResource != null)
                        return scaledResource;
                }
            }

            return EditorResources.Load<Texture2D>(resourcePath, false);
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
            var group = catalog.buffers.groups[groupIndex];

            var sv = group[elementIndex];
            if (typeof(T) == typeof(string) && sv.type == StyleValue.Type.Text)
                return (T)(object)catalog.buffers.strings[sv.index];
            if ((typeof(T) == typeof(float) || typeof(T) == typeof(double)) && sv.type == StyleValue.Type.Number)
                return (T)(object)catalog.buffers.numbers[sv.index];
            if (typeof(T) == typeof(Color) && sv.type == StyleValue.Type.Color)
                return (T)(object)catalog.buffers.colors[sv.index];
            if (typeof(T) == typeof(StyleRect) && sv.type == StyleValue.Type.Rect)
                return (T)(object)catalog.buffers.rects[sv.index];

            if (typeof(T) == typeof(int))
            {
                if (sv.type == StyleValue.Type.Number)
                    return (T)(object)catalog.buffers.numbers[sv.index];
                if (sv.type == StyleValue.Type.Text)
                    return (T)(object)catalog.buffers.strings[sv.index].GetHashCode();
                if (sv.type == StyleValue.Type.Keyword)
                    return (T)(object)sv.index;
                return defaultValue;
            }

            return defaultValue;
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
        }

        private static readonly StyleValue[] k_NoValue = {};
        private static readonly StyleState[] k_NoState = {};
        private static readonly StyleState[] k_RegularBlockStates = { StyleState.normal };
        private static readonly StyleBlock k_ElementNotFound = new StyleBlock { name = -1, states = k_NoState, values = k_NoValue, catalog = null };

        private StyleBlock[] m_Blocks;
        private readonly Dictionary<int, string> m_NameCollisionTable = new Dictionary<int, string>();

        public string[] sheets { get; private set; } = {};
        internal StyleBuffers buffers { get; private set; }

        public StyleCatalog()
        {
            buffers = new StyleBuffers();
        }

        public int FindStyleIndex(int key)
        {
            return FindStyleIndex(key, m_Blocks);
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
            int location = FindStyleIndex(selectorKey);
            if (location != -1)
            {
                var foundStyle = m_Blocks[location];
                states = states.Length == 0 ? k_RegularBlockStates
                    : states.Distinct().Where(s => s != StyleState.none).ToArray();
                return new StyleBlock
                {
                    states = states.Length == 0 ? k_RegularBlockStates : states,
                    name = foundStyle.name,
                    values = foundStyle.values,
                    catalog = foundStyle.catalog
                };
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

        public bool AddPath(string ussPath)
        {
            return AddPaths(ussPath);
        }

        public bool AddPaths(params string[] paths)
        {
            return AddPaths((IEnumerable<string>)paths);
        }

        public bool AddPaths(IEnumerable<string> paths)
        {
            var validPaths = paths.Where(p =>
            {
                if (sheets.Contains(p))
                    return false;

                // Check if path can be loaded
                var loadableStyleSheet = EditorResources.Load<UnityEngine.Object>(p, false) as StyleSheet;
                if (!loadableStyleSheet)
                    return false;

                return true;
            });

            sheets = sheets.Concat(validPaths).ToArray();
            return true;
        }

        public void Load(string ussPath)
        {
            if (AddPath(ussPath))
                Refresh();
        }

        public void Refresh()
        {
            var strings = new List<string>();
            var numbers = new List<float>();
            var rects = new List<StyleRect>();
            var colors = new List<Color>();
            var blocks = new List<StyleBlock>();
            var groups = new List<StyleValueGroup>();

            m_NameCollisionTable.Clear();
            try
            {
                var resolver = new StyleSheetResolver();
                resolver.AddStyleSheets(sheets);
                resolver.ResolveSheets();
                resolver.ResolveExtend();
                Compile(resolver, numbers, colors, strings, rects, groups, blocks);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Error while refreshing stylesheet catalog: " + string.Join(", ", sheets) + "\n" + ex.Message);
            }

            buffers = new StyleBuffers
            {
                strings = strings.ToArray(),
                numbers = numbers.ToArray(),
                colors = colors.ToArray(),
                rects = rects.ToArray(),
                groups = groups.ToArray()
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

        private static StyleValue[] MergeValues(IEnumerable<StyleValue> values, IEnumerable<StyleValue> newValues)
        {
            var mergedBlockValues = new List<StyleValue>(values);
            foreach (var newValue in newValues)
            {
                bool valueMerged = false;
                for (int j = 0; j < mergedBlockValues.Count; ++j)
                {
                    var blockValue = mergedBlockValues[j];
                    if (newValue.key == blockValue.key && newValue.state == blockValue.state)
                    {
                        blockValue.type = newValue.type;
                        blockValue.index = newValue.index;
                        mergedBlockValues[j] = blockValue;
                        valueMerged = true;
                        break;
                    }
                }

                if (!valueMerged)
                    mergedBlockValues.Add(newValue);
            }

            return mergedBlockValues.ToArray();
        }

        private bool CompileElement(string name, IList<StyleBlock> blocks, StyleValue[] values)
        {
            int key = GetNameKey(name);

            int existingStyleIndex = FindStyleIndex(key, blocks);
            if (existingStyleIndex == -1)
            {
                blocks.Add(new StyleBlock { name = key, states = k_NoState, values = values, catalog = this });
                return true;
            }

            var mergedBlockValues = MergeValues(blocks[existingStyleIndex].values, values);
            blocks[existingStyleIndex] = new StyleBlock { name = key, states = k_NoState, values = mergedBlockValues, catalog = this };
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
            List<float> numbers, List<Color> colors, List<string> strings, List<StyleRect> rects, List<StyleValueGroup> groups,
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
                    var newValue = CompileValue(resolver, property, stateFlags, numbers, colors, strings, rects, groups);
                    values.Add(newValue);
                }

                if (CompileElement(blockName, blocks, values.ToArray()))
                    blocks.Sort((l, r) => l.name.CompareTo(r.name));
            }
        }

        private StyleValue CompileValue(StyleSheetResolver resolver, StyleSheetResolver.Property property, StyleState stateFlags,
            List<float> numbers, List<Color> colors, List<string> strings, List<StyleRect> rects, List<StyleValueGroup> groups)
        {
            var values = resolver.ResolveValues(property);
            if (values.Count == 1)
            {
                return CompileBaseValue(property.Name, stateFlags, values[0], numbers, colors, strings);
            }

            if (values.Count == 2 &&
                values[0].ValueType == StyleValueType.Float &&
                values[1].ValueType == StyleValueType.Float)
                return CompileRect(property.Name, values, stateFlags, rects, 0, 1, 0, 1);

            if (values.Count == 3 &&
                values[0].ValueType == StyleValueType.Float &&
                values[1].ValueType == StyleValueType.Float &&
                values[2].ValueType == StyleValueType.Float)
                return CompileRect(property.Name, values, stateFlags, rects, 0, 1, 2, 1);

            if (values.Count == 4 &&
                values[0].ValueType == StyleValueType.Float &&
                values[1].ValueType == StyleValueType.Float &&
                values[2].ValueType == StyleValueType.Float &&
                values[3].ValueType == StyleValueType.Float)
                return CompileRect(property.Name, values, stateFlags, rects, 0, 1, 2, 3);

            // Compile list of primitive values
            if (values.Count >= 2 && values.Count <= 5)
                return CompileValueGroup(property.Name, values, stateFlags, groups, numbers, colors, strings);

            // Value form not supported, lets report it and keep a undefined value to the property.
            Debug.LogWarning($"Failed to compile style block property {property.Name} " +
                $"with {values.Count} values");
            return StyleValue.Undefined(GetNameKey(property.Name), stateFlags);
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

        private StyleValue CompileValueGroup(string propertyName, List<StyleSheetResolver.Value> values, StyleState stateFlags, List<StyleValueGroup> groups, List<float> numbers, List<Color> colors, List<string> strings)
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

        private StyleValue CompileBaseValue(string propertyName, StyleState stateFlags, StyleSheetResolver.Value value, List<float> numbers, List<Color> colors, List<string> strings)
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
                case StyleValueType.AssetReference:
                case StyleValueType.ResourcePath:
                case StyleValueType.Enum:
                    var str = value.AsString();
                    // Try a few conversions
                    Color parsedColor;
                    if (ColorUtility.TryParseHtmlString(str, out parsedColor))
                        return StyleValue.Type.Color;

                    return StyleValue.Type.Text;
                case StyleValueType.String:
                {
                    return StyleValue.Type.Text;
                }

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
                case StyleValueType.Color:
                    return SetIndex(colors, value.AsColor());
                case StyleValueType.ResourcePath:
                    return SetIndex(strings, value.ToString());
                case StyleValueType.Enum:
                    var str = value.AsString();
                    // Try a few conversions
                    Color parsedColor;
                    if (ColorUtility.TryParseHtmlString(str, out parsedColor))
                        return SetIndex(colors, parsedColor);
                    return SetIndex(strings, str);
                case StyleValueType.AssetReference:
                    return SetIndex(strings, AssetDatabase.GetAssetPath(value.AsAssetReference()));
                case StyleValueType.String:
                {
                    return SetIndex(strings, value.AsString());
                }
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
                var blockName = m_NameCollisionTable[catBlock.name];
                var block = m_Blocks[FindStyleIndex(catBlock.name)];

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
                            sb.AppendLine($"Property {m_NameCollisionTable[value1.key]} not found in block {blockName}");
                            continue;
                        }

                        var value2 = catBlock.values[value2Index];
                        if (value1.type != value2.type)
                        {
                            sb.AppendLine($"Property: {m_NameCollisionTable[value1.key]} has different type in block {blockName}");
                        }
                        else if (!CompareValue(block, value1, catBlock, value2))
                        {
                            sb.AppendLine($"Property: {m_NameCollisionTable[value1.key]} has different value in block {blockName}");
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
                    sb.AppendLine("    " + m_NameCollisionTable[block.name]);
                }
            }

            return sameStyles;
        }

        internal static int GetComparableValue(StyleValue v1, StyleBlock block)
        {
            for (var valueIndex = 0; valueIndex < block.values.Length; ++valueIndex)
            {
                var v2 = block.values[valueIndex];
                if (v1.key == v2.key && v1.state == v2.state)
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
