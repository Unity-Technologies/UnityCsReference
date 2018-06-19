// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor.Experimental.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Internal;
using UnityEngine.StyleSheets;
using Debug = UnityEngine.Debug;

namespace UnityEditor.Experimental
{
    internal static class StyleKeyword
    {
        public readonly static int root = "root".GetHashCode();

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
        public readonly static int zIndex = "z-index".GetHashCode();
    }

    [DebuggerDisplay("topleft = ({top}, {left}), bottomright = ({bottom}, {right}), size = ({width} x {height})")]
    [StructLayout(LayoutKind.Explicit)]
    internal struct StyleRect
    {
        public static readonly StyleRect Nil = new StyleRect { left = float.NaN, right = float.NaN, top = float.NaN, bottom = float.NaN };
        public static readonly StyleRect Zero = new StyleRect { left = 0f, right = 0f, top = 0f, bottom = 0f };

        public static StyleRect Size(float width, float height)
        {
            return new StyleRect {width = width, height = height};
        }

        [FieldOffset(0)] public float top;
        [FieldOffset(4)] public float right;
        [FieldOffset(8)] public float bottom;
        [FieldOffset(12)] public float left;

        [FieldOffset(0)] public float width;
        [FieldOffset(4)] public float height;
    }

    [DebuggerDisplay("grow = {grow}, shrink = {shrink}, basis = {basis}")]
    internal struct StyleFlex
    {
        public float grow;
        public float shrink;
        public FloatOrKeyword basis;
    }

    [DebuggerDisplay("count = {count}")]
    internal struct StyleValueGroup
    {
        public const int k_MaxValueCount = 5;

        public StyleValueGroup(int key, int count)
        {
            this.count = count;
            v1 = new StyleValue {key = key, type = StyleValue.Type.Undefined, index = -1};
            v2 = new StyleValue { key = key, type = StyleValue.Type.Undefined, index = -1 };
            v3 = new StyleValue { key = key, type = StyleValue.Type.Undefined, index = -1 };
            v4 = new StyleValue { key = key, type = StyleValue.Type.Undefined, index = -1 };
            v5 = new StyleValue { key = key, type = StyleValue.Type.Undefined, index = -1 };
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

    [DebuggerDisplay("key = {key}, type = {type}, index = {index}")]
    internal struct StyleValue
    {
        public static StyleValue Undefined(int key) { return new StyleValue { key = key, type = Type.Undefined, index = -1 }; }
        public static StyleValue Undefined(string name) { return new StyleValue {key = name.GetHashCode(), type = Type.Undefined, index = -1}; }

        public enum Type
        {
            Undefined = -1,
            Keyword = StyleValueType.Keyword,
            Number = StyleValueType.Float,
            Color = StyleValueType.Color,
            Text = StyleValueType.String,
            Rect,
            Flex,
            Group
        }

        public enum Keyword
        {
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
    }

    [DebuggerDisplay("name = {name}")]
    internal struct StyleBlock
    {
        public int name;
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

        public FloatOrKeyword GetFloatOrKeyword(string key, FloatOrKeyword defaultValue = default(FloatOrKeyword))
        {
            var bufferIndex = GetValueIndex(key, StyleValue.Type.Number);
            if (bufferIndex == -1)
            {
                bufferIndex = GetValueIndex(key, StyleValue.Type.Keyword);
                if (bufferIndex == -1)
                    return defaultValue;
                else
                    return new FloatOrKeyword((StyleValueKeyword)GetValueIndex(key, StyleValue.Type.Keyword));
            }
            else
                return new FloatOrKeyword(catalog.buffers.numbers[bufferIndex]);
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

        public StyleFlex GetFlex(string key, StyleFlex defaultValue = default(StyleFlex))
        {
            var bufferIndex = GetValueIndex(key, StyleValue.Type.Flex);
            if (bufferIndex == -1)
            {
                return new StyleFlex
                {
                    grow = GetFloat("flex-grow", defaultValue.grow),
                    shrink = GetFloat("flex-shrink", defaultValue.shrink),
                    basis = GetFloatOrKeyword("flex-basis", defaultValue.basis),
                };
            }
            return catalog.buffers.flexs[bufferIndex];
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

            if (typeof(T) == typeof(string))
                return (T)(object)catalog.buffers.strings[group[elementIndex].index];
            if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                return (T)(object)catalog.buffers.numbers[group[elementIndex].index];
            if (typeof(T) == typeof(Color))
                return (T)(object)catalog.buffers.colors[group[elementIndex].index];
            if (typeof(T) == typeof(StyleFlex))
                return (T)(object)catalog.buffers.flexs[group[elementIndex].index];
            if (typeof(T) == typeof(StyleRect))
                return (T)(object)catalog.buffers.rects[group[elementIndex].index];

            if (typeof(T) == typeof(int))
            {
                var sv = group[elementIndex];
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

        public bool HasValue(int key)
        {
            if (values == null)
                return false;

            for (int i = 0; i != values.Length; ++i)
            {
                var v = values[i];
                if (v.key == key)
                    return true;
            }

            return false;
        }

        public bool HasValue(string key)
        {
            return HasValue(key.GetHashCode());
        }

        public StyleValue.Type GetValueType(int key)
        {
            if (values == null)
                return StyleValue.Type.Undefined;

            for (int i = 0; i != values.Length; ++i)
            {
                var v = values[i];
                if (v.key == key)
                    return v.type;
            }

            return StyleValue.Type.Undefined;
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
            if (values == null)
                return -1;

            for (int i = 0; i != values.Length; ++i)
            {
                var v = values[i];
                if (v.key == hash && v.type == type)
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
            public StyleFlex[] flexs;
            public StyleValueGroup[] groups;
        }

        private StyleBlock[] m_Blocks;
        private readonly StyleBlock k_ElementNotFound = new StyleBlock { name = -1 };
        private readonly Dictionary<int, string> m_NameCollisionTable = new Dictionary<int, string>();

        public string[] sheets { get; private set; } = {};
        internal StyleBuffers buffers { get; private set; }

        public StyleCatalog()
        {
            buffers = new StyleBuffers();
        }

        public int FindStyleIndex(int key)
        {
            if (m_Blocks == null || m_Blocks.Length == 0)
                return -1;

            int low = 0;
            int high = m_Blocks.Length - 1;
            int middle = (low + high + 1) / 2;
            do
            {
                int currentKey = m_Blocks[middle].name;
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

        public StyleBlock GetStyle(int selectorNameHash)
        {
            int location = FindStyleIndex(selectorNameHash);
            if (location != -1)
                return m_Blocks[location];
            return k_ElementNotFound;
        }

        public StyleBlock GetStyle(string selectorName)
        {
            int selectorNameHash = selectorName.GetHashCode();
            return GetStyle(selectorNameHash);
        }

        public bool AddPath(string ussPath)
        {
            if (sheets.Contains(ussPath))
                return false;

            // Check if path can be loaded
            var loadableStyleSheet = EditorResources.Load<UnityEngine.Object>(ussPath, false) as StyleSheet;
            if (!loadableStyleSheet)
                return false;

            sheets = sheets.Concat(new[] { ussPath }).ToArray();
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
            var flexs = new List<StyleFlex>();
            var groups = new List<StyleValueGroup>();

            m_NameCollisionTable.Clear();

            for (int i = 0; i < sheets.Length; ++i)
            {
                var path = sheets[i];
                var styleSheet = EditorResources.Load<UnityEngine.Object>(path, false) as StyleSheet;
                if (!styleSheet)
                    continue;

                CompileSheet(styleSheet, numbers, colors, strings, rects, flexs, groups, blocks);
            }

            buffers = new StyleBuffers
            {
                strings = strings.ToArray(),
                numbers = numbers.ToArray(),
                colors = colors.ToArray(),
                rects = rects.ToArray(),
                flexs = flexs.ToArray(),
                groups = groups.ToArray()
            };

            blocks.Sort((l, r) => l.name.CompareTo(r.name));
            m_Blocks = blocks.ToArray();
        }

        private void CompileSheet(StyleSheet styleSheet,
            List<float> numbers, List<Color> colors, List<string> strings, List<StyleRect> rects,
            List<StyleFlex> flexs, List<StyleValueGroup> groups,
            List<StyleBlock> blocks)
        {
            foreach (var cs in styleSheet.complexSelectors)
            {
                List<StyleValue> values = new List<StyleValue>(cs.rule.properties.Length);
                foreach (var property in cs.rule.properties)
                {
                    // TODO figure out what to do in EditorResources
                    if (property.values.Length > 0 && property.values[0].valueType == StyleValueType.AssetReference)
                        continue;

                    values.Add(CompileValue(styleSheet, property, numbers, colors, strings, rects, flexs, groups));
                }

                CompileElement(GetComplexSelectorKeyName(cs), blocks, values.ToArray());
            }
        }

        private static string GetComplexSelectorKeyName(StyleComplexSelector cs)
        {
            string name = String.Empty;
            for (int s = 0; s < cs.selectors.Length; ++s)
            {
                if (s > 0)
                    name += ">";
                for (int p = 0; p < cs.selectors[s].parts.Length; ++p)
                {
                    if (p > 0)
                        name += ".";
                    name += cs.selectors[s].parts[p].value;
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

        private void CompileElement(string name, IList<StyleBlock> blocks, StyleValue[] values)
        {
            int key = GetNameKey(name);
            bool wasMerged = false;
            for (int i = 0; i < blocks.Count; ++i)
            {
                var block = blocks[i];
                if (block.name == key)
                {
                    var mergedBlockValues = new List<StyleValue>(block.values);
                    foreach (var newValue in values)
                    {
                        bool valueMerged = false;
                        for (int j = 0; j < mergedBlockValues.Count; ++j)
                        {
                            var blockValue = mergedBlockValues[j];
                            if (newValue.key == blockValue.key)
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

                    blocks[i] = new StyleBlock { name = key, values = mergedBlockValues.ToArray(), catalog = this };
                    wasMerged = true;
                    break;
                }
            }

            if (!wasMerged)
                blocks.Add(new StyleBlock { name = key, values = values, catalog = this });
        }

        private StyleValue CompileRect(StyleSheet styleSheet, string propertyName, StyleValueHandle[] values,
            IList<StyleRect> rects, int topIndex, int rightIndex, int bottomIndex, int leftIndex)
        {
            return new StyleValue
            {
                key = GetNameKey(propertyName),
                type = StyleValue.Type.Rect,
                index = SetIndex(rects, new StyleRect
                {
                    top = styleSheet.ReadFloat(values[topIndex]),
                    right = styleSheet.ReadFloat(values[rightIndex]),
                    bottom = styleSheet.ReadFloat(values[bottomIndex]),
                    left = styleSheet.ReadFloat(values[leftIndex])
                })
            };
        }

        private bool CompileFlex(ref StyleSheet styleSheet, string propertyName, StyleValueHandle[] values, List<StyleFlex> flexs, out StyleValue sv)
        {
            float grow;
            float shrink;
            FloatOrKeyword basis;
            bool valid = StyleSheetApplicator.CompileFlexShorthand(styleSheet, values, out grow, out shrink, out basis);

            sv = new StyleValue
            {
                key = GetNameKey(propertyName),
                type = StyleValue.Type.Flex,
                index = SetIndex(flexs, new StyleFlex
                {
                    grow = grow,
                    shrink = shrink,
                    basis = basis
                })
            };

            return valid;
        }

        private StyleValue CompileValue(StyleSheet styleSheet, StyleProperty property,
            List<float> numbers, List<Color> colors, List<string> strings, List<StyleRect> rects, List<StyleFlex> flexs,
            List<StyleValueGroup> groups)
        {
            return CompileValue(styleSheet, property.name, property.values, numbers, colors, strings, rects, flexs, groups);
        }

        private StyleValue CompileValue(StyleSheet styleSheet, string propertyName, StyleValueHandle[] values,
            List<float> numbers, List<Color> colors, List<string> strings, List<StyleRect> rects, List<StyleFlex> flexs, List<StyleValueGroup> groups)
        {
            StyleValue builtinValue;
            if (CompileBuiltInNameProperty(ref styleSheet, propertyName, values, flexs, out builtinValue))
                return builtinValue;

            if (values.Length == 1)
                return CompileBaseValue(styleSheet, propertyName, values[0], numbers, colors, strings);

            if (values.Length == 2 &&
                values[0].valueType == StyleValueType.Float &&
                values[1].valueType == StyleValueType.Float)
                return CompileRect(styleSheet, propertyName, values, rects, 0, 1, 0, 1);

            if (values.Length == 3 &&
                values[0].valueType == StyleValueType.Float &&
                values[1].valueType == StyleValueType.Float &&
                values[2].valueType == StyleValueType.Float)
                return CompileRect(styleSheet, propertyName, values, rects, 0, 1, 2, 1);

            if (values.Length == 4 &&
                values[0].valueType == StyleValueType.Float &&
                values[1].valueType == StyleValueType.Float &&
                values[2].valueType == StyleValueType.Float &&
                values[3].valueType == StyleValueType.Float)
                return CompileRect(styleSheet, propertyName, values, rects, 0, 1, 2, 3);

            // Compile list of primitive values
            if (values.Length >= 2 && values.Length <= 5)
                return CompileValueGroup(styleSheet, propertyName, values, groups, numbers, colors, strings);

            // Value form not supported, lets report it and keep a undefined value to the property.
            Debug.LogWarning($"Failed to compile style block property {propertyName} " +
                $"with {values.Length} values from {styleSheet.name}", styleSheet);
            return StyleValue.Undefined(GetNameKey(propertyName));
        }

        private StyleValue CompileValueGroup(StyleSheet styleSheet, string propertyName, StyleValueHandle[] values, List<StyleValueGroup> groups, List<float> numbers, List<Color> colors, List<string> strings)
        {
            int propertyKey = GetNameKey(propertyName);
            StyleValueGroup vg = new StyleValueGroup(propertyKey, values.Length);
            for (int i = 0; i < values.Length; ++i)
                vg[i] = CompileBaseValue(styleSheet, propertyName, values[i], numbers, colors, strings);

            return new StyleValue
            {
                key = propertyKey,
                type = StyleValue.Type.Group,
                index = SetIndex(groups, vg)
            };
        }

        private StyleValue CompileBaseValue(StyleSheet styleSheet, string propertyName, StyleValueHandle handle, List<float> numbers, List<Color> colors, List<string> strings)
        {
            return new StyleValue
            {
                key = GetNameKey(propertyName),
                type = ReduceStyleValueType(handle.valueType),
                index = MergeValue(styleSheet, handle, numbers, colors, strings)
            };
        }

        private bool CompileBuiltInNameProperty(ref StyleSheet styleSheet, string propertyName, StyleValueHandle[] values, List<StyleFlex> flexs, out StyleValue compileValue)
        {
            if (propertyName == "flex" && CompileFlex(ref styleSheet, propertyName, values, flexs, out compileValue))
                return true;

            compileValue = StyleValue.Undefined(propertyName);
            return false;
        }

        private static StyleValue.Type ReduceStyleValueType(StyleValueType type)
        {
            switch (type)
            {
                case StyleValueType.Color:
                case StyleValueType.Float:
                case StyleValueType.Keyword:
                    return (StyleValue.Type)type;
                case StyleValueType.ResourcePath:
                case StyleValueType.Enum:
                case StyleValueType.String:
                    return StyleValue.Type.Text;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static int MergeValue(StyleSheet styleSheet, StyleValueHandle handle,
            List<float> numbers, List<Color> colors, List<string> strings)
        {
            switch (handle.valueType)
            {
                case StyleValueType.Keyword:
                    return (int)StyleValue.ConvertKeyword(styleSheet.ReadKeyword(handle));
                case StyleValueType.Float:
                    return SetIndex(numbers, styleSheet.ReadFloat(handle));
                case StyleValueType.Color:
                    return SetIndex(colors, styleSheet.ReadColor(handle));
                case StyleValueType.ResourcePath:
                    return SetIndex(strings, styleSheet.ReadResourcePath(handle));
                case StyleValueType.Enum:
                    return SetIndex(strings, styleSheet.ReadEnum(handle));
                case StyleValueType.String:
                    return SetIndex(strings, styleSheet.ReadString(handle));
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
    }

    [ExcludeFromDocs]
    public partial class EditorResources
    {
        private static bool s_DelayedInitialization;
        private static bool s_IgnoreFirstPostProcessImport;
        private static bool s_EditorResourcesPackageLoaded;
        private static bool s_RefreshGlobalCatalog;

        // Editor resources package root path.
        private static readonly string packagePathPrefix = $"Packages/{packageName}";

        // Global editor styles
        private static readonly StyleCatalog s_StyleCatalog;

        static EditorResources()
        {
            s_IgnoreFirstPostProcessImport = true;
            s_StyleCatalog = new StyleCatalog();
            s_StyleCatalog.AddPath(UIElementsEditorUtility.s_DefaultCommonStyleSheetPath);
            s_StyleCatalog.AddPath(EditorGUIUtility.isProSkin
                ? UIElementsEditorUtility.s_DefaultCommonDarkStyleSheetPath
                : UIElementsEditorUtility.s_DefaultCommonLightStyleSheetPath);
            foreach (var editorUssPath in AssetDatabase.GetAllAssetPaths().Where(IsEditorStyleSheet))
                s_StyleCatalog.AddPath(editorUssPath);
            s_StyleCatalog.Refresh();

            EditorApplication.update -= DelayInitialization;
            EditorApplication.update += DelayInitialization;
        }

        private static bool IsEditorStyleSheet(string path)
        {
            var pathLowerCased = path.ToLower();
            return pathLowerCased.Contains("/editor") && (pathLowerCased.EndsWith("common.uss") ||
                                                          pathLowerCased.EndsWith(EditorGUIUtility.isProSkin ? "dark.uss" : "light.uss"));
        }

        private static void DelayInitialization()
        {
            EditorApplication.update -= DelayInitialization;
            s_DelayedInitialization = true;
        }

        private static void RefreshGlobalCatalog()
        {
            EditorApplication.update -= RefreshGlobalCatalog;
            s_StyleCatalog.Refresh();
            InternalEditorUtility.RepaintAllViews();

            s_RefreshGlobalCatalog = false;
        }

        internal class StylePostprocessor : AssetPostprocessor
        {
            internal static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPath)
            {
                if (!s_DelayedInitialization)
                    return;

                if (s_IgnoreFirstPostProcessImport)
                {
                    s_IgnoreFirstPostProcessImport = false;
                    return;
                }

                bool reloadStyles = false;
                foreach (var path in importedAssets)
                {
                    if (s_StyleCatalog.sheets.Contains(path))
                    {
                        reloadStyles = true;
                    }
                    else if (IsEditorStyleSheet(path))
                    {
                        if (s_StyleCatalog.AddPath(path))
                            reloadStyles = true;
                    }
                }

                if (reloadStyles && !s_RefreshGlobalCatalog)
                {
                    s_RefreshGlobalCatalog = true;
                    EditorApplication.update += RefreshGlobalCatalog;
                }
            }
        }

        // Checks if the editor resources are mounted as a package.
        public static bool EditorResourcesPackageAvailable
        {
            get
            {
                if (s_EditorResourcesPackageLoaded)
                    return true;
                bool isRootFolder, isReadonly;
                bool validPath = AssetDatabase.GetAssetFolderInfo(packagePathPrefix, out isRootFolder, out isReadonly);
                s_EditorResourcesPackageLoaded = validPath && isRootFolder;
                return s_EditorResourcesPackageLoaded;
            }
        }

        // Returns the editor resources absolute file system path.
        public static string dataPath
        {
            get
            {
                if (EditorResourcesPackageAvailable)
                    return new DirectoryInfo(Path.Combine(packagePathPrefix, "Assets")).FullName;
                return Application.dataPath;
            }
        }

        // Resolve an editor resource asset path.
        public static string ExpandPath(string path)
        {
            if (!EditorResourcesPackageAvailable)
                return path;
            if (path.StartsWith(packagePathPrefix))
                return path.Replace("\\", "/");
            return Path.Combine(packagePathPrefix, path).Replace("\\", "/");
        }

        // Returns the full file system path of an editor resource asset path.
        public static string GetFullPath(string path)
        {
            if (File.Exists(path))
                return path;
            return new FileInfo(ExpandPath(path)).FullName;
        }

        // Checks if an editor resource asset path exists.
        public static bool Exists(string path)
        {
            if (File.Exists(path))
                return true;
            return File.Exists(ExpandPath(path));
        }

        // Loads an editor resource asset.
        public static T Load<T>(string assetPath, bool isRequired = true) where T : UnityEngine.Object
        {
            var obj = Load(assetPath, typeof(T));
            if (!obj && isRequired)
                throw new FileNotFoundException("Could not find editor resource " + assetPath);
            return obj as T;
        }

        // Returns a globally defined style element by name.
        internal static StyleBlock GetStyle(string selectorName)
        {
            return s_StyleCatalog.GetStyle(selectorName);
        }

        // Returns a globally defined style element by key.
        internal static StyleBlock GetStyle(int selectorKey)
        {
            return s_StyleCatalog.GetStyle(selectorKey);
        }

        // Loads an USS asset into the global style catalog
        internal static void LoadStyles(string ussPath)
        {
            s_StyleCatalog.Load(ussPath);
        }

        // Creates a new style catalog from a set of USS assets.
        internal static StyleCatalog LoadCatalog(string[] ussPaths)
        {
            var catalog = new StyleCatalog();
            foreach (var path in ussPaths)
                catalog.AddPath(path);
            catalog.Refresh();
            return catalog;
        }

        // Mount the editor resources folder as a package.
        private static bool LoadEditorResourcesPackage(string editorResourcesPath)
        {
            // Make sure the folder contains a package.
            if (!File.Exists(Path.Combine(editorResourcesPath, "package.json")))
            {
                Debug.LogError(editorResourcesPath + "does not contain a package descriptor.");
                return false;
            }

            // We need editor resources meta files to be visible to prevent build issues.
            EditorSettings.Internal_UserGeneratedProjectSuffix = "-testable";
            EditorSettings.externalVersionControl = ExternalVersionControl.Generic;
            AssetDatabase.SaveAssets();

            PackageManager.Client.Add($"{packageName}@file:{editorResourcesPath}");
            return true;
        }

        [MenuItem("Edit/Tools/Load Editor Resources", false, 215, true)]
        internal static void LoadEditorResourcesIntoProject()
        {
            // Set default editor resources project.
            var editorResourcesPath = Path.Combine(Unsupported.GetBaseUnityDeveloperFolder(), "External/Resources/editor_resources");
            if (!Directory.Exists(editorResourcesPath))
                editorResourcesPath = Directory.GetCurrentDirectory();

            // Ask the user to select the editor resources package folder.
            editorResourcesPath = EditorUtility.OpenFolderPanel("Select editor resources folder", editorResourcesPath, "");
            if (String.IsNullOrEmpty(editorResourcesPath))
                return;

            // Make sure the editor_resources project does not contain any Library/ folder which could make the asset database crash if imported.
            var editorResourcesLibraryPath = Path.Combine(editorResourcesPath, "Library");
            if (Directory.Exists(editorResourcesLibraryPath))
            {
                Debug.LogError($"Please dispose of the Library folder under {editorResourcesPath} as it might fail to be imported.");
                return;
            }

            if (LoadEditorResourcesPackage(editorResourcesPath))
                EditorApplication.OpenProject(Path.Combine(Application.dataPath, ".."), Environment.GetCommandLineArgs());
        }
    }
}
