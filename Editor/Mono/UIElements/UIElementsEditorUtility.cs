// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    internal static class UIElementsEditorUtility
    {
        internal static readonly string s_DefaultCommonDarkStyleSheetPath = "StyleSheets/Generated/DefaultCommonDark.uss.asset";
        internal static readonly string s_DefaultCommonLightStyleSheetPath = "StyleSheets/Generated/DefaultCommonLight.uss.asset";

        static StyleSheet s_DefaultCommonDarkStyleSheet;
        static StyleSheet s_DefaultCommonLightStyleSheet;

        public const string hiddenClassName = "unity-hidden";

        internal static string GetStyleSheetPathForFont(string sheetPath, string fontName)
        {
            return sheetPath.Replace(".uss", "_" + fontName.ToLowerInvariant() + ".uss");
        }

        internal static string GetStyleSheetPathForCurrentFont(string sheetPath)
        {
            return GetStyleSheetPathForFont(sheetPath, EditorResources.currentFontName);
        }

        internal static StyleSheet LoadSkinnedStyleSheetForFont(int skin, string fontName)
        {
            return EditorGUIUtility.Load(GetStyleSheetPathForFont(skin == EditorResources.darkSkinIndex ? s_DefaultCommonDarkStyleSheetPath : s_DefaultCommonLightStyleSheetPath, fontName)) as StyleSheet;
        }

        internal static bool IsCommonDarkStyleSheetLoaded()
        {
            return s_DefaultCommonDarkStyleSheet != null;
        }

        internal static StyleSheet GetCommonDarkStyleSheet()
        {
            if (s_DefaultCommonDarkStyleSheet == null)
            {
                s_DefaultCommonDarkStyleSheet = LoadSkinnedStyleSheetForFont(EditorResources.darkSkinIndex, EditorResources.currentFontName);
                if (s_DefaultCommonDarkStyleSheet != null)
                    s_DefaultCommonDarkStyleSheet.isDefaultStyleSheet = true;
            }

            return s_DefaultCommonDarkStyleSheet;
        }

        internal static bool IsCommonLightStyleSheetLoaded()
        {
            return s_DefaultCommonLightStyleSheet != null;
        }

        internal static StyleSheet GetCommonLightStyleSheet()
        {
            if (s_DefaultCommonLightStyleSheet == null)
            {
                s_DefaultCommonLightStyleSheet = LoadSkinnedStyleSheetForFont(EditorResources.normalSkinIndex, EditorResources.currentFontName);
                if (s_DefaultCommonLightStyleSheet != null)
                    s_DefaultCommonLightStyleSheet.isDefaultStyleSheet = true;
            }

            return s_DefaultCommonLightStyleSheet;
        }

        static UIElementsEditorUtility()
        {
        }

        internal static int GetCursorId(StyleSheet sheet, StyleValueHandle handle)
        {
            var value = sheet.ReadEnum(handle);
            if (string.Equals(value, "arrow", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.Arrow;
            else if (string.Equals(value, "text", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.Text;
            else if (string.Equals(value, "resize-vertical", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.ResizeVertical;
            else if (string.Equals(value, "resize-horizontal", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.ResizeHorizontal;
            else if (string.Equals(value, "link", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.Link;
            else if (string.Equals(value, "slide-arrow", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.SlideArrow;
            else if (string.Equals(value, "resize-up-right", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.ResizeUpRight;
            else if (string.Equals(value, "resize-up-left", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.ResizeUpLeft;
            else if (string.Equals(value, "move-arrow", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.MoveArrow;
            else if (string.Equals(value, "rotate-arrow", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.RotateArrow;
            else if (string.Equals(value, "scale-arrow", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.ScaleArrow;
            else if (string.Equals(value, "arrow-plus", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.ArrowPlus;
            else if (string.Equals(value, "arrow-minus", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.ArrowMinus;
            else if (string.Equals(value, "pan", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.Pan;
            else if (string.Equals(value, "orbit", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.Orbit;
            else if (string.Equals(value, "zoom", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.Zoom;
            else if (string.Equals(value, "fps", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.FPS;
            else if (string.Equals(value, "split-resize-up-down", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.SplitResizeUpDown;
            else if (string.Equals(value, "split-resize-left-right", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.SplitResizeLeftRight;
            else if (string.Equals(value, "not-allowed", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.NotAllowed;

            return (int)MouseCursor.Arrow;
        }

        static readonly string k_DefaultStylesAppliedPropertyName = "DefaultStylesApplied";
        internal static void AddDefaultEditorStyleSheets(VisualElement ve)
        {
            if (ve.styleSheets.count == 0 || ve.GetProperty(k_DefaultStylesAppliedPropertyName) == null)
            {
                if (EditorGUIUtility.isProSkin)
                {
                    ve.styleSheets.Add(GetCommonDarkStyleSheet());
                }
                else
                {
                    ve.styleSheets.Add(GetCommonLightStyleSheet());
                }

                ve.SetProperty(k_DefaultStylesAppliedPropertyName, true);
            }
        }

        internal static void ForceDarkStyleSheet(VisualElement ele)
        {
            if (!EditorGUIUtility.isProSkin)
            {
                var lightStyle = GetCommonLightStyleSheet();
                var darkStyle = GetCommonDarkStyleSheet();
                var e = ele;
                while (e != null)
                {
                    if (e.styleSheets.Contains(lightStyle))
                    {
                        e.styleSheets.Swap(lightStyle, darkStyle);
                        break;
                    }
                    e = e.parent;
                }
            }
        }

        internal static void SetVisibility(VisualElement element, bool isVisible)
        {
            element.EnableInClassList(hiddenClassName, !isVisible);
        }

        internal static Action CreateDynamicVisibilityCallback(VisualElement element, Func<bool> visibilityCheck)
        {
            var messageCheck = () => SetVisibility(element, visibilityCheck.Invoke());
            messageCheck();
            return messageCheck;
        }

        internal static Action BindSerializedProperty<T>(BaseField<T> field, SerializedProperty property, Func<SerializedProperty, T> getter, Action<T, SerializedProperty> setter)
            where T : struct
        {
            BindingsStyleHelpers.RegisterRightClickMenu(field, property);
            field.TrackPropertyValue(property);
            field.AddToClassList(BaseField<bool>.alignedFieldUssClassName);

            field.RegisterValueChangedCallback(e =>
            {
                setter.Invoke(e.newValue, property);
            });

            var updateCallback = () =>
            {
                field.value = getter.Invoke(property);
                field.schedule.Execute(() => BindingsStyleHelpers.UpdateElementStyle(field, property));
            };
            updateCallback?.Invoke();
            return updateCallback;
        }

        internal static Action BindSerializedProperty(DropdownField dropdown, SerializedProperty property, GUIContent[] stringValues, int[] values)
        {
            var boolProperty = property.type == "bool";

            dropdown.choices = new List<string>(stringValues.Length);

            foreach (var val in stringValues)
                dropdown.choices.Add(val.text);

            dropdown.TrackPropertyValue(property);

            return BindSerializedProperty(dropdown, property, p =>
                {
                    var value = boolProperty ? (property.boolValue ? 1 : 0) : property.intValue;
                    return Array.IndexOf(values, value);
                },
                (i, p) =>
                {
                    if (boolProperty)
                        property.boolValue = i == 1;
                    else
                        property.intValue = values[i];

                    property.serializedObject.ApplyModifiedProperties();
                    dropdown.schedule.Execute(() => BindingsStyleHelpers.UpdateElementStyle(dropdown, property));
                });
        }

        internal static Action BindSerializedProperty(DropdownField dropdown, SerializedProperty property, Func<SerializedProperty, int> getter, Action<int, SerializedProperty> setter)
        {
            BindingsStyleHelpers.RegisterRightClickMenu(dropdown, property);
            dropdown.TrackPropertyValue(property);
            dropdown.AddToClassList(BaseField<bool>.alignedFieldUssClassName);

            dropdown.RegisterValueChangedCallback(e =>
            {
                setter.Invoke(dropdown.index, property);
            });

            var updateCallback = () =>
            {
                dropdown.index = getter.Invoke(property);
                dropdown.schedule.Execute(() => BindingsStyleHelpers.UpdateElementStyle(dropdown, property));
            };
            updateCallback?.Invoke();
            return updateCallback;
        }

        internal static Action BindSerializedProperty<T>(EnumField enumField, SerializedProperty property, Action<T> onValueChange = null)
            where T : struct, Enum, IConvertible
        {
            var boolProperty = property.type == "bool";

            BindingsStyleHelpers.RegisterRightClickMenu(enumField, property);
            enumField.AddToClassList(BaseField<bool>.alignedFieldUssClassName);

            enumField.RegisterValueChangedCallback(e =>
            {
                if (boolProperty)
                    property.boolValue = Convert.ToInt32(e.newValue) == 1;
                else
                    property.intValue = Convert.ToInt32(e.newValue);

                property.serializedObject.ApplyModifiedProperties();
                onValueChange?.Invoke((T)e.newValue);
            });

            var updateCallback = () =>
            {
                foreach (T value in Enum.GetValues(typeof(T)))
                {
                    var propertyValue = boolProperty ? (property.boolValue ? 1 : 0) : property.intValue;

                    if (value.ToInt32(null) != propertyValue)
                        continue;

                    enumField.value = value;
                    break;
                }

                enumField.schedule.Execute(() => BindingsStyleHelpers.UpdateElementStyle(enumField, property));
            };
            updateCallback?.Invoke();
            return updateCallback;
        }
    }
}
