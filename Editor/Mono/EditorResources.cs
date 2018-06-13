// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor.Experimental.UIElements;
using UnityEngine;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Internal;
using UnityEngine.StyleSheets;
using Debug = UnityEngine.Debug;

namespace UnityEditor.Experimental
{
    internal static class StyleKeyword
    {
        public readonly static int background = "background".GetHashCode();
        public readonly static int backgroundAttachment = "background-attachment".GetHashCode();
        public readonly static int backgroundColor = "background-color".GetHashCode();
        public readonly static int backgroundImage = "background-image".GetHashCode();
        public readonly static int backgroundPosition = "background-position".GetHashCode();
        public readonly static int backgroundRepeat = "background-repeat".GetHashCode();
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
        public readonly static int left = "left".GetHashCode();
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
        public readonly static int top = "top".GetHashCode();
        public readonly static int verticalAlign = "vertical-align".GetHashCode();
        public readonly static int visibility = "visibility".GetHashCode();
        public readonly static int width = "width".GetHashCode();
        public readonly static int zIndex = "z-index".GetHashCode();
    }

    [DebuggerDisplay("topleft = ({top}, {left}), bottomright = ({bottom}, {right}), size = ({right - left} x {bottom - top})")]
    internal struct StyleRect
    {
        public float top;
        public float right;
        public float bottom;
        public float left;
    }

    internal struct StyleFlex
    {
        public float grow;
        public float shrink;
        public FloatOrKeyword basis;
    }

    [DebuggerDisplay("key = {key}, type = {type}, index = {index}")]
    internal struct StyleValue
    {
        public enum Type
        {
            Keyword = StyleValueType.Keyword,
            Number = StyleValueType.Float,
            Color = StyleValueType.Color,
            Text = StyleValueType.String,
            Rect,
            Flex
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

        private int GetValueIndex(string key, StyleValue.Type type)
        {
            return GetValueIndex(key.GetHashCode(), type);
        }

        private int GetValueIndex(int hash, StyleValue.Type type)
        {
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
            if (m_Blocks.Length == 0)
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

            m_NameCollisionTable.Clear();

            for (int i = 0; i < sheets.Length; ++i)
            {
                var path = sheets[i];
                var styleSheet = EditorResources.Load<UnityEngine.Object>(path, false) as StyleSheet;
                if (!styleSheet)
                    continue;

                CompileSheet(styleSheet, numbers, colors, strings, rects, flexs, blocks);
            }

            buffers = new StyleBuffers
            {
                strings = strings.ToArray(),
                numbers = numbers.ToArray(),
                colors = colors.ToArray(),
                rects = rects.ToArray(),
                flexs = flexs.ToArray()
            };

            blocks.Sort((l, r) => l.name.CompareTo(r.name));
            m_Blocks = blocks.ToArray();
        }

        private void CompileSheet(StyleSheet styleSheet,
            List<float> numbers, List<Color> colors, List<string> strings, List<StyleRect> rects, List<StyleFlex> flexs,
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

                    values.Add(CompileValue(styleSheet, property, numbers, colors, strings, rects, flexs));
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

        private StyleValue CompileRect(ref StyleSheet styleSheet, StyleProperty property, List<StyleRect> rects, int topIndex, int rightIndex, int bottomIndex, int leftIndex)
        {
            return new StyleValue
            {
                key = GetNameKey(property.name),
                type = StyleValue.Type.Rect,
                index = SetIndex(rects, new StyleRect
                {
                    top = styleSheet.ReadFloat(property.values[topIndex]),
                    right = styleSheet.ReadFloat(property.values[rightIndex]),
                    bottom = styleSheet.ReadFloat(property.values[bottomIndex]),
                    left = styleSheet.ReadFloat(property.values[leftIndex])
                })
            };
        }

        private bool CompileFlex(ref StyleSheet styleSheet, StyleProperty property, List<StyleFlex> flexs, out StyleValue sv)
        {
            float grow;
            float shrink;
            FloatOrKeyword basis;
            bool valid = StyleSheetApplicator.CompileFlexShorthand(styleSheet, property.values, out grow, out shrink, out basis);

            sv = new StyleValue
            {
                key = GetNameKey(property.name),
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
            List<float> numbers, List<Color> colors, List<string> strings, List<StyleRect> rects, List<StyleFlex> flexs)
        {
            if (property.name == "flex")
            {
                StyleValue sv;
                if (CompileFlex(ref styleSheet, property, flexs, out sv))
                {
                    return sv;
                }
            }

            if (property.values.Length == 1)
            {
                var handle = property.values[0];
                StyleValue sv = new StyleValue
                {
                    key = GetNameKey(property.name),
                    type = ReduceStyleValueType(handle.valueType),
                    index = MergeValue(styleSheet, handle, numbers, colors, strings)
                };
                return sv;
            }

            if (property.values.Length == 2 &&
                property.values[0].valueType == StyleValueType.Float &&
                property.values[1].valueType == StyleValueType.Float)
                return CompileRect(ref styleSheet, property, rects, 0, 1, 0, 1);

            if (property.values.Length == 3 &&
                property.values[0].valueType == StyleValueType.Float &&
                property.values[1].valueType == StyleValueType.Float &&
                property.values[2].valueType == StyleValueType.Float)
                return CompileRect(ref styleSheet, property, rects, 0, 1, 2, 1);

            if (property.values.Length == 4 &&
                property.values[0].valueType == StyleValueType.Float &&
                property.values[1].valueType == StyleValueType.Float &&
                property.values[2].valueType == StyleValueType.Float &&
                property.values[3].valueType == StyleValueType.Float)
                return CompileRect(ref styleSheet, property, rects, 0, 1, 2, 3);

            Debug.LogWarning($"Failed to compile style block property {property.name} with {property.values.Length} values from {styleSheet.name}", styleSheet);
            return new StyleValue
            {
                key = GetNameKey(property.name),
                type = StyleValue.Type.Keyword,
                index = (int)StyleValue.Keyword.Unset
            };
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
            return (path.Contains("/Editor/") || path.Contains("/Editor Default Resources/")) && path.EndsWith(".uss");
        }

        private static void DelayInitialization()
        {
            EditorApplication.update -= DelayInitialization;
            s_DelayedInitialization = true;
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

                if (reloadStyles)
                    s_StyleCatalog.Refresh();
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
