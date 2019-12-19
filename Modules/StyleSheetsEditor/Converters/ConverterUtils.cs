// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.StyleSheets
{
    internal enum SkinTarget
    {
        Shared,
        Light,
        Dark
    }

    internal class DebugLogTimer : IDisposable
    {
        private System.Diagnostics.Stopwatch m_Timer;
        public string msg { get; set; }

        public DebugLogTimer(string m)
        {
            msg = m;
            m_Timer = System.Diagnostics.Stopwatch.StartNew();
        }

        public static DebugLogTimer Start(string m)
        {
            return new DebugLogTimer(m);
        }

        public void Dispose()
        {
            m_Timer.Stop();
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, msg + " - " + m_Timer.ElapsedMilliseconds + "ms");
        }
    }

    internal static class ConverterUtils
    {
        public const string k_TextAlignment = "-unity-text-align";
        public const string k_Border = "-unity-slice";
        public const string k_Clipping = "-unity-clipping";
        public const string k_ContentOffset = "-unity-content-offset";
        public const string k_Overflow = "-unity-overflow";
        public const string k_Height = "height";
        public const string k_Width = "width";

        public const string k_Font = "-unity-font";
        public const string k_FontSize = "font-size";
        public const string k_FontStyle = "font-style";
        public const string k_FontWeight = "font-weight";
        public const string k_ImagePosition = "-unity-image-position";
        public const string k_Margin = "margin";
        public const string k_Padding = "padding";

        public const string k_RichText = "-unity-rich-text";
        public const string k_StretchHeight = "-unity-stretch-height";
        public const string k_StretchWidth = "-unity-stretch-width";
        public const string k_WordWrap = "-unity-word-wrap";
        public const string k_TextColor = "color";
        public const string k_BackgroundImage = "background-image";
        public const string k_ScaledBackground = "-unity-scaled-backgrounds";
        public const string k_Name = "-unity-name";
        public const string k_CustomAbstractGUIStyleSelectorPrefix = ".imgui-abstract-";
        public const string k_CustomGUIStyleSelectorPrefix = ".imgui-style-";
        public const string k_GUISettingsSelector = ".imgui-skin-settings";
        public const string k_SelectionColor = "selection-color";
        public const string k_CursorColor = "cursor-color";
        public const string k_CursorFlashSpeed = "-unity-cursor-flash-speed";
        public const string k_DoubleClickSelectsWord = "-unity-double-click-selects-word";
        public const string k_TripleClickSelectsLine = "-unity-triple-click-selects-line";

        public const string k_Extend = "-unity-extend";

        public const string k_GeneratedSkinPath = "Assets/Editor Default Resources/Builtin Skins/Generated/Skins";
        public const string k_BundleSkinPath = "Builtin Skins/Generated/Skins";

        public static Dictionary<string, string> k_GuiStyleTypeNames;
        public static HashSet<string> k_StyleProperties;
        public static List<string> k_SkinStylePrefixes;

        static ConverterUtils()
        {
            k_GuiStyleTypeNames = GetGUIStyleProperties().ToDictionary(p => p.Name.ToLower(), p => p.Name.Capitalize());

            k_StyleProperties = new HashSet<string>();
            k_StyleProperties.Add(k_TextAlignment);

            foreach (var propPrefix in new[] { k_Border, k_Margin, k_Padding, k_Overflow })
            {
                k_StyleProperties.Add(propPrefix + "-left");
                k_StyleProperties.Add(propPrefix + "-right");
                k_StyleProperties.Add(propPrefix + "-top");
                k_StyleProperties.Add(propPrefix + "-bottom");
            }

            k_StyleProperties.Add(k_Clipping);
            k_StyleProperties.Add(k_ContentOffset);
            k_StyleProperties.Add(k_Height);
            k_StyleProperties.Add(k_Width);
            k_StyleProperties.Add(k_Font);
            k_StyleProperties.Add(k_FontSize);
            k_StyleProperties.Add(k_FontStyle);
            k_StyleProperties.Add(k_FontWeight);
            k_StyleProperties.Add(k_ImagePosition);

            k_StyleProperties.Add(k_RichText);
            k_StyleProperties.Add(k_StretchHeight);
            k_StyleProperties.Add(k_StretchWidth);
            k_StyleProperties.Add(k_WordWrap);
            k_StyleProperties.Add(k_TextColor);
            k_StyleProperties.Add(k_BackgroundImage);
            k_StyleProperties.Add(k_ScaledBackground);
            k_StyleProperties.Add(k_Name);
            k_StyleProperties.Add(k_SelectionColor);
            k_StyleProperties.Add(k_RichText);
            k_StyleProperties.Add(k_CursorColor);
            k_StyleProperties.Add(k_CursorFlashSpeed);
            k_StyleProperties.Add(k_DoubleClickSelectsWord);
            k_StyleProperties.Add(k_TripleClickSelectsLine);
            k_StyleProperties.Add(k_Extend);

            k_SkinStylePrefixes = new List<string>();
            k_SkinStylePrefixes.Add(k_CustomAbstractGUIStyleSelectorPrefix);
            k_SkinStylePrefixes.Add(k_CustomGUIStyleSelectorPrefix);
            k_SkinStylePrefixes.Add(k_GUISettingsSelector);
        }

        public static string ToUssString(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.LowerCenter: return "lower-center";
                case TextAnchor.LowerLeft: return "lower-left";
                case TextAnchor.LowerRight: return "lower-right";
                case TextAnchor.UpperCenter: return "upper-center";
                case TextAnchor.UpperLeft: return "upper-left";
                case TextAnchor.UpperRight: return "upper-right";
                case TextAnchor.MiddleCenter: return "middle-center";
                case TextAnchor.MiddleLeft: return "middle-left";
                case TextAnchor.MiddleRight: return "middle-right";
            }
            return "";
        }

        public static TextAnchor ToTextAnchor(string anchor)
        {
            switch (anchor)
            {
                case "lower-center": return TextAnchor.LowerCenter;
                case "lower-left": return TextAnchor.LowerLeft;
                case "lower-right": return TextAnchor.LowerRight;
                case "upper-center": return TextAnchor.UpperCenter;
                case "upper-left": return TextAnchor.UpperLeft;
                case "upper-right": return TextAnchor.UpperRight;
                case "middle-center": return TextAnchor.MiddleCenter;
                case "middle-left": return TextAnchor.MiddleLeft;
                case "middle-right": return TextAnchor.MiddleRight;
            }
            return TextAnchor.LowerCenter;
        }

        public static string ToUssString(TextClipping clipping)
        {
            switch (clipping)
            {
                case TextClipping.Clip: return "clip";
                case TextClipping.Overflow: return "overflow";
            }
            return "";
        }

        internal static TextClipping ToTextClipping(string clipping)
        {
            switch (clipping)
            {
                case "clip": return TextClipping.Clip;
                case "overflow": return TextClipping.Overflow;
            }
            return TextClipping.Clip;
        }

        public static string ToUssString(ImagePosition imgPosition)
        {
            switch (imgPosition)
            {
                case ImagePosition.ImageAbove: return "image-above";
                case ImagePosition.ImageLeft: return "image-left";
                case ImagePosition.ImageOnly: return "image-only";
                case ImagePosition.TextOnly: return "text-only";
            }
            return "";
        }

        public static ImagePosition ToImagePosition(string imgPosition)
        {
            switch (imgPosition)
            {
                case "image-above": return ImagePosition.ImageAbove;
                case "image-left": return ImagePosition.ImageLeft;
                case "image-only": return ImagePosition.ImageOnly;
                case "text-only": return ImagePosition.TextOnly;
            }
            return ImagePosition.ImageAbove;
        }

        public static StyleSelectorPart CreateSelectorPart(string selectorStr)
        {
            return selectorStr[0] == '.' ? StyleSelectorPart.CreateClass(selectorStr.Substring(1)) : StyleSelectorPart.CreateType(selectorStr);
        }

        public static StyleSelectorPart[] GetStateRuleSelectorParts(string baseSelectorStr, string id)
        {
            var baseSelector = CreateSelectorPart(baseSelectorStr);
            switch (id)
            {
                case "active":
                    return new[]
                    {
                        baseSelector,
                        StyleSelectorPart.CreatePseudoClass("hover"),
                        StyleSelectorPart.CreatePseudoClass("active")
                    };
                case "focused":
                    return new[]
                    {
                        baseSelector,
                        StyleSelectorPart.CreatePseudoClass("focus")
                    };
                case "hover":
                    return new[]
                    {
                        baseSelector,
                        StyleSelectorPart.CreatePseudoClass("hover")
                    };
                case "onActive":
                    return new[]
                    {
                        baseSelector,
                        StyleSelectorPart.CreatePseudoClass("hover"),
                        StyleSelectorPart.CreatePseudoClass("active"),
                        StyleSelectorPart.CreatePseudoClass("checked")
                    };
                case "onFocused":
                    return new[]
                    {
                        baseSelector,
                        StyleSelectorPart.CreatePseudoClass("hover"),
                        StyleSelectorPart.CreatePseudoClass("focus"),
                        StyleSelectorPart.CreatePseudoClass("checked")
                    };
                case "onHover":
                    return new[]
                    {
                        baseSelector,
                        StyleSelectorPart.CreatePseudoClass("hover"),
                        StyleSelectorPart.CreatePseudoClass("checked")
                    };
                case "onNormal":
                    return new[]
                    {
                        baseSelector,
                        StyleSelectorPart.CreatePseudoClass("checked")
                    };
                default:
                    throw new Exception("Unsupported GUIStyleStateId: " + id);
            }
        }

        public static string GetNoPseudoSelector(string selectorName)
        {
            var colonIndex = selectorName.IndexOf(":");
            if (colonIndex != -1)
            {
                selectorName = selectorName.Substring(0, colonIndex);
            }
            return selectorName;
        }

        public static StyleComplexSelector CreateSelectorFromSource(StyleComplexSelector srcSelector, string newSelectorBase)
        {
            var newSelector = new StyleComplexSelector();
            var newSelectorParts = srcSelector.selectors[0].parts.ToArray();
            if (newSelectorBase[0] == '.')
            {
                newSelectorParts[0].type = StyleSelectorType.Class;
                newSelectorParts[0].value = newSelectorBase.Substring(1);
            }
            else
            {
                newSelectorParts[0].type = StyleSelectorType.Type;
                newSelectorParts[0].value = newSelectorBase;
            }

            newSelector.selectors = new[] { new StyleSelector() { previousRelationship = StyleSelectorRelationship.None, parts = newSelectorParts } };
            return newSelector;
        }

        public static StyleComplexSelector CreateSimpleSelector(string styleName)
        {
            var cs = new StyleComplexSelector();
            StyleSelectorPart[] parts;
            CSSSpec.ParseSelector(styleName, out parts);
            var selector = new StyleSelector();
            selector.parts = parts;
            cs.selectors = new[] { selector };
            return cs;
        }

        public static string Capitalize(this string s)
        {
            if (String.IsNullOrEmpty(s))
            {
                return String.Empty;
            }
            char[] a = s.ToCharArray();
            a[0] = Char.ToUpper(a[0]);
            return new string(a);
        }

        public static string Lowerize(this string s)
        {
            if (String.IsNullOrEmpty(s))
            {
                return String.Empty;
            }
            char[] a = s.ToCharArray();
            a[0] = Char.ToLower(a[0]);
            return new string(a);
        }

        public static void TryAdd<K, V>(this Dictionary<K, V> dict, K key, V value)
        {
            if (!dict.ContainsKey(key))
            {
                dict.Add(key, value);
            }
        }

        public static void Set<K, V>(this Dictionary<K, V> dict, K key, V value)
        {
            if (dict.ContainsKey(key))
            {
                dict[key] = value;
            }
            else
            {
                dict.Add(key, value);
            }
        }

        public static void Assign(this GUISettings settings, GUISettings src)
        {
            settings.cursorFlashSpeed = src.cursorFlashSpeed;
            settings.cursorColor = src.cursorColor;
            settings.doubleClickSelectsWord = src.doubleClickSelectsWord;
            settings.selectionColor = src.selectionColor;
            settings.tripleClickSelectsLine = src.tripleClickSelectsLine;
        }

        public static void Assign(this GUIStyleState state, GUIStyleState src)
        {
            state.background = src.background;
            state.textColor = src.textColor;
            state.scaledBackgrounds = src.scaledBackgrounds.ToArray();
        }

        public static void Assign(this GUIStyle style, GUIStyle src)
        {
            style.name = src.name;
            style.alignment = src.alignment;
            style.border = src.border;
            style.clipping = src.clipping;
            style.contentOffset = src.contentOffset;
            style.fixedHeight = src.fixedHeight;
            style.fixedWidth = src.fixedWidth;
            style.focused = src.focused;
            style.fontStyle = src.fontStyle;
            style.font = src.font;
            style.fontSize = src.fontSize;
            style.imagePosition = src.imagePosition;
            style.margin = src.margin;
            style.name = src.name;
            style.overflow = src.overflow;
            style.padding = src.padding;
            style.richText = src.richText;
            style.stretchHeight = src.stretchHeight;
            style.stretchWidth = src.stretchWidth;
            style.wordWrap = src.wordWrap;

            style.active.Assign(src.active);
            style.hover.Assign(src.hover);
            style.normal.Assign(src.normal);
            style.onActive.Assign(src.onActive);
            style.onFocused.Assign(src.onFocused);
            style.onHover.Assign(src.onHover);
            style.onNormal.Assign(src.onNormal);
        }

        public static void SetToDefault(GUIStyle style)
        {
            var name = style.name;
            style.Assign(GUIStyle.none);
            style.name = name;
        }

        public static void Assign(this GUISkin skin, GUISkin src)
        {
            skin.customStyles = src.customStyles.Select(style =>
            {
                var newStyle = new GUIStyle();
                newStyle.Assign(style);
                return newStyle;
            }).ToArray();
            skin.font = src.font;

            skin.settings.Assign(src.settings);
            skin.button.Assign(src.button);
            skin.box.Assign(src.box);
            skin.horizontalScrollbarThumb.Assign(src.horizontalScrollbarThumb);
            skin.horizontalScrollbar.Assign(src.horizontalScrollbar);
            skin.horizontalScrollbarLeftButton.Assign(src.horizontalScrollbarLeftButton);
            skin.horizontalScrollbarRightButton.Assign(src.horizontalScrollbarRightButton);
            skin.horizontalSlider.Assign(src.horizontalSlider);
            skin.horizontalSliderThumb.Assign(src.horizontalSliderThumb);
            skin.label.Assign(src.label);
            skin.scrollView.Assign(src.scrollView);
            skin.textArea.Assign(src.textArea);
            skin.textField.Assign(src.textField);
            skin.toggle.Assign(src.toggle);
            skin.verticalScrollbar.Assign(src.verticalScrollbar);
            skin.verticalScrollbarDownButton.Assign(src.verticalScrollbarDownButton);
            skin.verticalScrollbarThumb.Assign(src.verticalScrollbarThumb);
            skin.verticalScrollbarUpButton.Assign(src.verticalScrollbarUpButton);
            skin.verticalSlider.Assign(src.verticalSlider);
            skin.verticalSliderThumb.Assign(src.verticalSliderThumb);
            skin.window.Assign(src.window);
            skin.name = src.name;
        }

        public static void SetToDefault(GUISkin skin)
        {
            ForEachGUIStyleProperty(skin, (name, style) => SetToDefault(style));

            foreach (var style in skin.customStyles)
            {
                SetToDefault(style);
            }
        }

        public static IEnumerable<PropertyInfo> GetGUIStyleProperties()
        {
            return typeof(GUISkin).GetProperties().Where(p => p.PropertyType == typeof(GUIStyle)).OrderBy(style => style.Name);
        }

        public static IEnumerable<PropertyInfo> GetGUIStateProperties()
        {
            return typeof(GUIStyle).GetProperties().Where(p => p.PropertyType == typeof(GUIStyleState));
        }

        public static StyleProperty FindProperty(this StyleComplexSelector selector, string propertyName)
        {
            return Array.Find(selector.rule.properties, p => p.name == propertyName);
        }

        public static string FindStyleName(this StyleComplexSelector selector, string selectorStr, StyleSheet sheet)
        {
            var property = FindProperty(selector, k_Name);
            if (property == null)
            {
                return ToStyleName(selectorStr);
            }
            return sheet.ReadString(property.values[0]);
        }

        public static string FindExtend(this StyleComplexSelector selector, StyleSheet sheet)
        {
            var property = FindProperty(selector, k_Extend);
            if (property == null)
            {
                return null;
            }
            return sheet.ReadString(property.values[0]);
        }

        public static void ForEachGUIStyleProperty(this GUISkin skin, Action<string, GUIStyle> action)
        {
            var styleProperties = GetGUIStyleProperties();
            foreach (var property in styleProperties)
            {
                var style = property.GetValue(skin, null) as GUIStyle;
                action(property.Name, style);
            }
        }

        public static void ForEachGUIStateProperty(this GUIStyle skin, Action<string, GUIStyleState> action)
        {
            var properties = GetGUIStateProperties();
            foreach (var property in properties)
            {
                var state = property.GetValue(skin, null) as GUIStyleState;
                action(property.Name, state);
            }
        }

        public static GUIStyle GetStyleFromSkin(this GUISkin skin, string styleName)
        {
            var propertyInfo = typeof(GUISkin).GetProperties().FirstOrDefault(pi => pi.Name.ToLower() == styleName.ToLower());
            if (propertyInfo != null)
            {
                return propertyInfo.GetValue(skin, null) as GUIStyle;
            }

            return skin.customStyles.FirstOrDefault(s => s.name == styleName);
        }

        public static string EscapeSelectorName(string name)
        {
            return name.Replace(" ", "-").Replace(".", "-");
        }

        public static string ToStyleName(string selectorName)
        {
            // Type selector:
            var lowerSelector = selectorName.ToLower();
            if (k_GuiStyleTypeNames.ContainsKey(lowerSelector))
            {
                return lowerSelector;
            }

            return selectorName.Replace(k_CustomGUIStyleSelectorPrefix, "").Replace("-", " ").Replace(".", "");
        }

        public static string ToGUIStyleSelectorName(string guiStyleName)
        {
            if (k_GuiStyleTypeNames.ContainsKey(guiStyleName))
            {
                return EscapeSelectorName(k_GuiStyleTypeNames[guiStyleName]);
            }
            return k_CustomGUIStyleSelectorPrefix + EscapeSelectorName(guiStyleName);
        }

        public static string GetStateRuleSelectorStr(string baseSelectorStr, string id)
        {
            var parts = GetStateRuleSelectorParts(baseSelectorStr, id);
            var sb = new StringBuilder();
            StyleSheetToUss.ToUssString(StyleSelectorRelationship.None, parts, sb);
            return sb.ToString();
        }

        public static string ToUssPropertyName(params string[] values)
        {
            return String.Join("-", values.Where(v => !String.IsNullOrEmpty(v)).ToArray());
        }

        public static T LoadResource<T>(string path) where T : Object
        {
            var resource = StyleSheetResourceUtil.LoadResource(path, typeof(T)) as T;
            if (resource == null)
            {
                // It might be a builtin resource:
                resource = Resources.GetBuiltinResource<T>(path);
            }
            return resource;
        }

        public static T LoadResourceRequired<T>(string path) where T : Object
        {
            var resource = StyleSheetResourceUtil.LoadResource(path, typeof(T)) as T;
            if (resource == null)
            {
                // It might be a builtin resource:
                resource = Resources.GetBuiltinResource<T>(path);
            }

            if (resource == null)
            {
                throw new Exception("Cannot load resource: " + path);
            }

            return resource;
        }

        public static bool IsPseudoSelector(string selectorStr)
        {
            return selectorStr.Contains(":");
        }

        public static bool IsCustomStyleSelector(string selectorStr)
        {
            return selectorStr.StartsWith(k_CustomGUIStyleSelectorPrefix) && !selectorStr.Contains(":");
        }

        public static bool IsAbstractStyleSelector(string selectorStr)
        {
            return selectorStr.StartsWith(k_CustomAbstractGUIStyleSelectorPrefix) && !selectorStr.Contains(":");
        }

        public static bool IsTypeStyleSelector(string guiStyleName)
        {
            return k_GuiStyleTypeNames.ContainsKey(guiStyleName.ToLower());
        }

        public static bool IsSkinStyleSelector(string guiStyleName)
        {
            return IsTypeStyleSelector(guiStyleName) || k_SkinStylePrefixes.Any(guiStyleName.StartsWith);
        }

        public static void GetFontStylePropertyValues(FontStyle style, out string fontStyle, out string fontWeight)
        {
            fontStyle = style == FontStyle.Italic || style == FontStyle.BoldAndItalic ? "italic" : "normal";
            fontWeight = style == FontStyle.Bold || style == FontStyle.BoldAndItalic ? "bold" : "normal";
        }

        public static bool TryGetFontStyle(string fontStyle, string weight, out FontStyle style)
        {
            if (fontStyle == "italic" && weight == "bold")
            {
                style = FontStyle.BoldAndItalic;
                return true;
            }

            if (fontStyle == "italic")
            {
                style = FontStyle.Italic;
                return true;
            }

            if (weight == "bold")
            {
                style = FontStyle.Bold;
                return true;
            }

            if (fontStyle == "normal")
            {
                style = FontStyle.Normal;
                return true;
            }

            style = FontStyle.Normal;
            return false;
        }

        public static void SelectAsset<T>(string path) where T : Object
        {
            var asset = ConverterUtils.LoadResource<T>(path);
            if (asset)
            {
                EditorGUIUtility.PingObject(asset);
            }
        }

        public static GUISkin CreateDefaultGUISkin()
        {
            var skin = ScriptableObject.CreateInstance<GUISkin>();
            // For some strange reason, the default GUISkin is created with a customStyles array of a single null element.
            // Remove this null element for testing purpose.
            skin.customStyles = new GUIStyle[0];
            return skin;
        }

        public static GUISkin LoadSkin(SkinTarget target)
        {
            return LoadResource<GUISkin>(GetSkinPath(target));
        }

        public static GUISkin LoadBundleSkin(SkinTarget target)
        {
            var editorAssetBundle = EditorGUIUtility.GetEditorAssetBundle();
            return editorAssetBundle.LoadAsset<GUISkin>(GetSkinPath(target, true).ToLower());
        }

        public static void ResetSkinToPristine(GUISkin skin, SkinTarget target)
        {
            var pristinePath = GetSkinPath(target, true, true);
            var pristineSkin = LoadResource<GUISkin>(pristinePath);
            if (pristineSkin != null)
            {
                var originalName = skin.name;
                skin.Assign(pristineSkin);
                skin.name = originalName;
            }
        }

        public static string GetSkinPath(SkinTarget target, bool bundle = false, bool pristine = false)
        {
            var skinName = target == SkinTarget.Dark ? "DarkSkin" : "LightSkin";
            if (pristine)
            {
                skinName += "_Pristine";
            }
            return $"{(bundle ? k_BundleSkinPath : k_GeneratedSkinPath)}/{skinName}.guiskin";
        }

        public static string[] GetSheetPathsFromRootFolders(IEnumerable<string> rootFolders, SkinTarget target, string sheetPostFix = "")
        {
            var skinSheetName = $"{(target == SkinTarget.Light ? "light" : "dark")}{sheetPostFix}.uss";
            var sheetPaths = rootFolders.Select(folderPath => Directory.GetFiles(EditorResources.ExpandPath(folderPath), "*.uss", SearchOption.AllDirectories))
                .SelectMany(p => p)
                .Where(p => p.EndsWith("common.uss") || p.EndsWith(skinSheetName))
                .Select(p => p.Replace("\\", "/"))
                .ToArray();
            return sheetPaths;
        }

        public static GUISkin CreatePackageSkinFromBundleSkin(SkinTarget target)
        {
            var packageSkin = CreateDefaultGUISkin();
            var bundleSkin = LoadBundleSkin(target);
            packageSkin.Assign(bundleSkin);
            ConvertToPackageAsset(packageSkin, bundleSkin);
            return packageSkin;
        }

        public static void ConvertToPackageAsset(GUISkin packageSkin, GUISkin bundleSkin)
        {
            // Switch all images and font to package resources:
            if (bundleSkin.font != null)
                packageSkin.font = ConvertToPackageAsset(bundleSkin.font);

            ConvertToPackageAsset(packageSkin.box, bundleSkin.box);
            ConvertToPackageAsset(packageSkin.label, bundleSkin.label);
            ConvertToPackageAsset(packageSkin.textField, bundleSkin.textField);
            ConvertToPackageAsset(packageSkin.textArea, bundleSkin.textArea);
            ConvertToPackageAsset(packageSkin.button, bundleSkin.button);
            ConvertToPackageAsset(packageSkin.toggle, bundleSkin.toggle);
            ConvertToPackageAsset(packageSkin.window, bundleSkin.window);
            ConvertToPackageAsset(packageSkin.horizontalSlider, bundleSkin.horizontalSlider);
            ConvertToPackageAsset(packageSkin.horizontalSliderThumb, bundleSkin.horizontalSliderThumb);
            ConvertToPackageAsset(packageSkin.verticalSlider, bundleSkin.verticalSlider);
            ConvertToPackageAsset(packageSkin.verticalSliderThumb, bundleSkin.verticalSliderThumb);
            ConvertToPackageAsset(packageSkin.horizontalScrollbar, bundleSkin.horizontalScrollbar);
            ConvertToPackageAsset(packageSkin.horizontalScrollbarThumb, bundleSkin.horizontalScrollbarThumb);
            ConvertToPackageAsset(packageSkin.horizontalScrollbarLeftButton, bundleSkin.horizontalScrollbarLeftButton);
            ConvertToPackageAsset(packageSkin.horizontalScrollbarRightButton, bundleSkin.horizontalScrollbarRightButton);
            ConvertToPackageAsset(packageSkin.verticalScrollbar, bundleSkin.verticalScrollbar);
            ConvertToPackageAsset(packageSkin.verticalScrollbarThumb, bundleSkin.verticalScrollbarThumb);
            ConvertToPackageAsset(packageSkin.verticalScrollbarUpButton, bundleSkin.verticalScrollbarUpButton);
            ConvertToPackageAsset(packageSkin.verticalScrollbarDownButton, bundleSkin.verticalScrollbarDownButton);
            ConvertToPackageAsset(packageSkin.scrollView, bundleSkin.scrollView);

            for (var i = 0; i < packageSkin.customStyles.Length; ++i)
            {
                var packageStyle = packageSkin.customStyles[i];
                var bundleStyle = bundleSkin.customStyles[i];
                ConvertToPackageAsset(packageStyle, bundleStyle);
            }
        }

        private static T ConvertToPackageAsset<T>(T bundleAsset) where T : Object
        {
            var partialPath = EditorResources.GetAssetPath(bundleAsset);
            return LoadResource<T>(partialPath);
        }

        private static void ConvertToPackageAsset(GUIStyle dst, GUIStyle src)
        {
            ConvertToPackageAsset(dst.normal, src.normal);
            ConvertToPackageAsset(dst.active, src.active);
            ConvertToPackageAsset(dst.focused, src.focused);
            ConvertToPackageAsset(dst.hover, src.hover);
            ConvertToPackageAsset(dst.onActive, src.onActive);
            ConvertToPackageAsset(dst.onFocused, src.onFocused);
            ConvertToPackageAsset(dst.onHover, src.onHover);
            ConvertToPackageAsset(dst.onNormal, src.onNormal);

            if (src.font != null)
                dst.font = ConvertToPackageAsset(src.font);
        }

        private static void ConvertToPackageAsset(GUIStyleState dst, GUIStyleState src)
        {
            if (src.background != null)
                dst.background = ConvertToPackageAsset(src.background);

            if (src.scaledBackgrounds != null && src.scaledBackgrounds.Length > 0 && src.scaledBackgrounds[0] != null)
                dst.scaledBackgrounds = new[] { ConvertToPackageAsset(src.scaledBackgrounds[0]) };
        }

        internal static StyleSheet ResolveSheets(params string[] sheetPaths)
        {
            var resolver = new StyleSheetResolver();
            resolver.AddStyleSheets(sheetPaths);
            return resolver.ResolvedSheet;
        }

        internal static StyleSheet ResolveSheets(params StyleSheet[] sheets)
        {
            var resolver = new StyleSheetResolver();
            resolver.AddStyleSheets(sheets);
            return resolver.ResolvedSheet;
        }

        internal static StyleSheetResolver ResolveFromSheetsFolder(string folder, SkinTarget target, StyleSheetResolver.ResolvingOptions options = null, string sheetPostFix = "")
        {
            return ResolveFromSheetsFolder(new[] { folder }, target, options, sheetPostFix);
        }

        internal static StyleSheetResolver ResolveFromSheetsFolder(IEnumerable<string> folders, SkinTarget target, StyleSheetResolver.ResolvingOptions options = null, string sheetPostFix = "")
        {
            var sheetPaths = ConverterUtils.GetSheetPathsFromRootFolders(folders, target, sheetPostFix);
            if (sheetPaths.Length == 0)
            {
                throw new Exception("Cannot find sheets to generate skin");
            }

            var resolver = new StyleSheetResolver(options ?? new StyleSheetResolver.ResolvingOptions() { ThrowIfCannotResolve = true });
            foreach (var sheet in sheetPaths)
            {
                resolver.AddStyleSheets(sheet);
            }

            return resolver;
        }

        internal static StyleSheet CompileStyleSheetContent(string styleSheetContent, bool disableValidation = true, bool reportErrors = false)
        {
            var importer = new StyleSheetImporterImpl();
            var styleSheet = ScriptableObject.CreateInstance<StyleSheet>();
            importer.disableValidation = disableValidation;
            importer.Import(styleSheet, styleSheetContent);
            if (reportErrors)
            {
                foreach (var err in importer.importErrors)
                    Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, styleSheet, err.ToString());
            }
            return styleSheet;
        }
    }
}
