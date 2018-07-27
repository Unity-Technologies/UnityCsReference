// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.StyleSheets;

namespace UnityEditor.StyleSheets
{
    [ExcludeFromDocs]
    public static class StyleSheetToGUISkin
    {
        #region ConversionAPI

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

        [ExcludeFromDocs]
        public static void PopulateSkinFromSheetFolders(string[] sheetFolders)
        {
            GUISkin currentSkin = null;
            SkinTarget currentTarget = SkinTarget.Light;

            if (EditorGUIUtility.isProSkin)
            {
                currentTarget = SkinTarget.Dark;
            }

            currentSkin = ConverterUtils.LoadBundleSkin(currentTarget);

            ConverterUtils.SetToDefault(currentSkin);

            foreach (string folder in sheetFolders)
            {
                StyleSheetToGUISkin.CreateGUISkinFromSheetsFolder(folder, currentTarget, currentSkin);
            }
        }

        internal static void PopulateSkinFromSheetFolder(GUISkin skin, string sheetFolder, SkinTarget target)
        {
            if (!Directory.Exists(sheetFolder))
                throw new Exception("Sheet Folder doesn't exists: " + sheetFolder);

            var commonPath = sheetFolder + "/common.uss";
            var skinSheetName = target == SkinTarget.Dark ? "dark.uss" : "light.uss";
            var skinSheetPath = sheetFolder + "/" + skinSheetName;

            StyleSheet mergedSheet;
            var themeDir = Path.GetDirectoryName(sheetFolder) + "/_Variables";
            if (Directory.Exists(themeDir))
            {
                var themeSheetPath = themeDir + "/" + skinSheetName;
                mergedSheet = ResolveSheets(themeSheetPath, commonPath, skinSheetPath);
            }
            else
            {
                mergedSheet = ResolveSheets(commonPath, skinSheetPath);
            }

            PopulateSkin(mergedSheet, skin);
        }

        internal static GUISkin CreateGUISkinFromSheetsFolder(string sheetsPath, SkinTarget target, GUISkin skin = null)
        {
            skin = skin ?? ConverterUtils.CreateDefaultGUISkin();

            var resolver = ResolveFromSheetsFolder(sheetsPath, target);

            PopulateSkin(resolver.ResolvedSheet, skin);

            Array.Sort(skin.customStyles, (s1, s2) => s1.name.CompareTo(s2.name));

            return skin;
        }

        internal static StyleSheetResolver ResolveFromSheetsFolder(string sheetsPath, SkinTarget target, StyleSheetResolver.ResolvingOptions options = null, string sheetPostFix = "")
        {
            var sheetFolders = AssetDatabase.GetSubFolders(sheetsPath);
            if (sheetFolders.Length == 0)
            {
                throw new Exception("Cannot find EditorStyles: " + sheetsPath);
            }

            var skinSheetName = string.Format("{0}{1}.uss", target == SkinTarget.Light ? "light" : "dark", sheetPostFix);
            var resolver = new StyleSheetResolver(options ?? new StyleSheetResolver.ResolvingOptions() { ThrowIfCannotResolve = true });
            var themeDir = sheetsPath + "/_Variables";
            if (Directory.Exists(themeDir))
            {
                var themeSheetPath = themeDir + "/" + skinSheetName;
                resolver.AddStyleSheet(themeSheetPath);
            }

            foreach (var sheetFolder in sheetFolders)
            {
                var commonPath = sheetFolder + "/" + string.Format("{0}{1}.uss", "common", sheetPostFix);
                var skinSheetPath = sheetFolder + "/" + skinSheetName;
                resolver.AddStyleSheets(commonPath, skinSheetPath);
            }

            return resolver;
        }

        internal static void PopulateStyle(StyleSheet sheet, string selectorStr, GUIStyle style, bool throwIfIncomplete = false)
        {
            var cache = new StyleSheetCache(sheet);
            PopulateStyle(cache, selectorStr, style, throwIfIncomplete);
        }

        internal static void PopulateStyle(StyleSheetCache cache, string selectorStr, GUIStyle style, bool throwIfIncomplete = false)
        {
            StyleComplexSelector complexSelector;
            if (cache.selectors.TryGetValue(selectorStr, out complexSelector))
            {
                PopulateStyle(cache, complexSelector, style, throwIfIncomplete);
            }
            else
            {
                if (throwIfIncomplete)
                {
                    throw new Exception("Cannot find style with selector: " + selectorStr);
                }
            }
        }

        internal static void PopulateSkin(StyleSheetCache cache, GUISkin skin, bool throwIfIncomplete = false)
        {
            StyleComplexSelector globalSelector;
            if (cache.selectors.TryGetValue("*", out globalSelector))
            {
                var rule = globalSelector.rule;
                // GUISkin.font
                GetProperty(rule, ConverterUtils.k_Font, false, property =>
                {
                    skin.font = ReadResource<Font>(cache.sheet, property);
                });
            }

            skin.ForEachGUIStyleProperty((name, style) =>
            {
                PopulateStyle(cache, name.Capitalize(), style, throwIfIncomplete);
            });

            PopulateCustomStyles(cache, skin, throwIfIncomplete);

            // Settings
            var sheet = cache.sheet;
            StyleComplexSelector settingsSelector;
            if (cache.selectors.TryGetValue(ConverterUtils.k_GUISettingsSelector, out settingsSelector))
            {
                var rule = settingsSelector.rule;
                GetProperty(rule, ConverterUtils.k_SelectionColor, throwIfIncomplete, property =>
                {
                    skin.settings.selectionColor = sheet.ReadColor(property.values[0]);
                });
                GetProperty(rule, ConverterUtils.k_CursorColor, throwIfIncomplete, property =>
                {
                    skin.settings.cursorColor = sheet.ReadColor(property.values[0]);
                });
                GetProperty(rule, ConverterUtils.k_CursorFlashSpeed, throwIfIncomplete, property =>
                {
                    skin.settings.cursorFlashSpeed = sheet.ReadFloat(property.values[0]);
                });
                GetProperty(rule, ConverterUtils.k_DoubleClickSelectsWord, throwIfIncomplete, property =>
                {
                    skin.settings.doubleClickSelectsWord = ReadBool(sheet, property);
                });
                GetProperty(rule, ConverterUtils.k_TripleClickSelectsLine, throwIfIncomplete, property =>
                {
                    skin.settings.tripleClickSelectsLine = ReadBool(sheet, property);
                });
            }
            else
            {
                if (throwIfIncomplete)
                {
                    throw new Exception("Cannot find guiSettings rule with selector: " + ConverterUtils.k_GUISettingsSelector);
                }
            }
        }

        internal static void PopulateSkin(StyleSheet sheet, GUISkin skin, bool throwIfIncomplete = false)
        {
            var cache = new StyleSheetCache(sheet);
            PopulateSkin(cache, skin, throwIfIncomplete);
        }

        #endregion

        #region Implementation
        private static void ReadRectOffset(StyleSheetCache cache, StyleRule rule, string name, string suffix, bool throwIfNotFound, RectOffset offset)
        {
            var sheet = cache.sheet;
            GetProperty(rule, ConverterUtils.ToUssPropertyName(name, "left", suffix), throwIfNotFound, property =>
            {
                offset.left = (int)sheet.ReadFloat(property.values[0]);
            });
            GetProperty(rule, ConverterUtils.ToUssPropertyName(name, "right", suffix), throwIfNotFound, property =>
            {
                offset.right = (int)sheet.ReadFloat(property.values[0]);
            });
            GetProperty(rule, ConverterUtils.ToUssPropertyName(name, "top", suffix), throwIfNotFound, property =>
            {
                offset.top = (int)sheet.ReadFloat(property.values[0]);
            });
            GetProperty(rule, ConverterUtils.ToUssPropertyName(name, "bottom", suffix), throwIfNotFound, property =>
            {
                offset.bottom = (int)sheet.ReadFloat(property.values[0]);
            });
        }

        private static T ReadResource<T>(StyleSheet sheet, StyleProperty property) where T : UnityEngine.Object
        {
            if (property.values[0].valueType == StyleValueType.Keyword && sheet.ReadKeyword(property.values[0]) == StyleValueKeyword.None)
            {
                return null;
            }

            var path = sheet.ReadResourcePath(property.values[0]);
            return ConverterUtils.LoadResource<T>(path);
        }

        private static bool ReadBool(StyleSheet sheet, StyleProperty property)
        {
            return sheet.ReadKeyword(property.values[0]) == StyleValueKeyword.True;
        }

        private static void ReadFontStyle(StyleSheet sheet, StyleRule rule, bool throwIfNotFound, GUIStyle style)
        {
            string fontStyleStr = null;
            string weight = null;
            GetProperty(rule, ConverterUtils.k_FontStyle, throwIfNotFound, property =>
            {
                fontStyleStr = sheet.ReadEnum(property.values[0]);
            });
            GetProperty(rule, ConverterUtils.k_FontWeight, throwIfNotFound, property =>
            {
                weight = sheet.ReadEnum(property.values[0]);
            });

            FontStyle fontStyle;
            if (ConverterUtils.TryGetFontStyle(fontStyleStr, weight, out fontStyle))
            {
                style.fontStyle = fontStyle;
            }
        }

        private static void GetProperty(StyleRule rule, string name, bool throwIfNotFound, Action<StyleProperty> next)
        {
            var property = rule.properties.FirstOrDefault(prop => prop.name == name);
            if (property == null)
            {
                if (throwIfNotFound)
                {
                    throw new Exception("Cannot find the property: " + name);
                }
            }
            else
            {
                next(property);
            }
        }

        private static void ReadState(StyleSheetCache cache, StyleRule rule, GUIStyleState state, bool throwIfNotFound)
        {
            GetProperty(rule, ConverterUtils.k_TextColor, throwIfNotFound, property =>
            {
                state.textColor = cache.sheet.ReadColor(property.values[0]);
            });

            GetProperty(rule, ConverterUtils.k_BackgroundImage, false, property =>
            {
                state.background = ReadResource<Texture2D>(cache.sheet, property);
            });

            GetProperty(rule, ConverterUtils.k_ScaledBackground, false, property =>
            {
                var scaledBackground = ReadResource<Texture2D>(cache.sheet, property);
                if (scaledBackground != null)
                {
                    state.scaledBackgrounds = new[] { scaledBackground };
                }
            });
        }

        private static void ReadState(StyleSheetCache cache, string baseRuleSelector, GUIStyleState state, string stateId, bool throwIfNotFound)
        {
            var stateRuleSelector = ConverterUtils.GetStateRuleSelectorStr(baseRuleSelector, stateId);
            StyleComplexSelector complexSelector;
            if (cache.selectors.TryGetValue(stateRuleSelector, out complexSelector))
            {
                var rule = complexSelector.rule;
                ReadState(cache, rule, state, throwIfNotFound);
            }
            else
            {
                if (throwIfNotFound)
                {
                    throw new Exception("Cannot find rule with selector: " + stateRuleSelector);
                }
            }
        }

        private static void PopulateCustomStyles(StyleSheetCache cache, GUISkin skin, bool throwIfIncomplete = false)
        {
            var customStyleDict = new Dictionary<string, GUIStyle>();
            if (skin.customStyles != null)
            {
                // Add Existing style: ready to be overridden:
                foreach (var customStyle in skin.customStyles)
                {
                    // GUISkin by default adds a null Style
                    if (customStyle != null)
                    {
                        var customStyleName = ConverterUtils.ToGUIStyleSelectorName(customStyle.name);
                        customStyleDict.TryAdd(customStyleName, customStyle);
                    }
                }
            }

            // Look at all the complexSelector tagged as style
            foreach (var kvp in cache.customStyleSelectors)
            {
                GUIStyle customStyle;
                // Check if we are overriding an existing style or if we are creating a new custom style:
                if (!customStyleDict.TryGetValue(kvp.Key, out customStyle))
                {
                    // New style being added:
                    customStyle = new GUIStyle();
                    customStyleDict.Add(kvp.Key, customStyle);
                }
                PopulateStyle(cache, kvp.Value, customStyle, throwIfIncomplete);
            }

            skin.customStyles = customStyleDict.Values.ToArray();
        }

        private static void PopulateStyle(StyleSheetCache cache, StyleComplexSelector complexSelector, GUIStyle style, bool throwIfIncomplete = false)
        {
            var rule = complexSelector.rule;
            var complexSelectorStr = StyleSheetToUss.ToUssSelector(complexSelector);
            var sheet = cache.sheet;

            // GUIStyle.alignment
            GetProperty(rule, ConverterUtils.k_TextAlignment, throwIfIncomplete, property =>
            {
                style.alignment = ConverterUtils.ToTextAnchor(sheet.ReadEnum(property.values[0]));
            });

            // GUIStyle.border
            ReadRectOffset(cache, rule, ConverterUtils.k_Border, "", throwIfIncomplete, style.border);

            // GUIStyle.clipping
            GetProperty(rule, ConverterUtils.k_Clipping, throwIfIncomplete, property =>
            {
                style.clipping = ConverterUtils.ToTextClipping(sheet.ReadEnum(property.values[0]));
            });

            // GUIStyle.contentOffset
            GetProperty(rule, ConverterUtils.k_ContentOffset, throwIfIncomplete, property =>
            {
                style.contentOffset = StyleSheetBuilderHelper.ReadVector2(sheet, property);
            });

            // GUIStyle.fixedHeight
            GetProperty(rule, ConverterUtils.k_Height, throwIfIncomplete, property =>
            {
                style.fixedHeight = sheet.ReadFloat(property.values[0]);
            });

            // GUIStyle.fixedWidth
            GetProperty(rule, ConverterUtils.k_Width, throwIfIncomplete, property =>
            {
                style.fixedWidth = sheet.ReadFloat(property.values[0]);
            });

            // GUIStyle.font
            GetProperty(rule, ConverterUtils.k_Font, false, property =>
            {
                style.font = ReadResource<Font>(sheet, property);
            });

            // GUIStyle.fixedWidth
            GetProperty(rule, ConverterUtils.k_FontSize, throwIfIncomplete, property =>
            {
                style.fontSize = (int)sheet.ReadFloat(property.values[0]);
            });

            // GUIStyle.fontStyle
            ReadFontStyle(sheet, rule, throwIfIncomplete, style);

            // GUIStyle.imagePosition
            GetProperty(rule, ConverterUtils.k_ImagePosition, throwIfIncomplete, property =>
            {
                style.imagePosition = ConverterUtils.ToImagePosition(sheet.ReadEnum(property.values[0]));
            });

            // GUIStyle.margin
            ReadRectOffset(cache, rule, ConverterUtils.k_Margin, null, throwIfIncomplete, style.margin);

            // GUIStyle.name
            GetProperty(rule, ConverterUtils.k_Name, throwIfIncomplete, property =>
            {
                style.name = sheet.ReadString(property.values[0]);
            });

            // GUIStyle.overflow
            ReadRectOffset(cache, rule, ConverterUtils.k_Overflow, null, throwIfIncomplete, style.overflow);

            // GUIStyle.padding
            ReadRectOffset(cache, rule, ConverterUtils.k_Padding, null, throwIfIncomplete, style.padding);

            // GUIStyle.richText
            GetProperty(rule, ConverterUtils.k_RichText, throwIfIncomplete, property =>
            {
                style.richText = ReadBool(sheet, property);
            });

            // GUIStyle.stretchHeight
            GetProperty(rule, ConverterUtils.k_StretchHeight, throwIfIncomplete, property =>
            {
                style.stretchHeight = ReadBool(sheet, property);
            });

            // GUIStyle.stretchWidth
            GetProperty(rule, ConverterUtils.k_StretchWidth, throwIfIncomplete, property =>
            {
                style.stretchWidth = ReadBool(sheet, property);
            });

            // GUIStyle.wordWrap
            GetProperty(rule, ConverterUtils.k_WordWrap, throwIfIncomplete, property =>
            {
                style.wordWrap = ReadBool(sheet, property);
            });

            ReadState(cache, rule, style.normal, throwIfIncomplete);

            ReadState(cache, complexSelectorStr, style.active, "active", throwIfIncomplete);
            ReadState(cache, complexSelectorStr, style.focused, "focused", throwIfIncomplete);
            ReadState(cache, complexSelectorStr, style.hover, "hover", throwIfIncomplete);
            ReadState(cache, complexSelectorStr, style.onActive, "onActive", throwIfIncomplete);
            ReadState(cache, complexSelectorStr, style.onFocused, "onFocused", throwIfIncomplete);
            ReadState(cache, complexSelectorStr, style.onHover, "onHover", throwIfIncomplete);
            ReadState(cache, complexSelectorStr, style.onNormal, "onNormal", throwIfIncomplete);
        }

        #endregion
    }
}
