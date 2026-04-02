// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Bindings;
using UnityEngine.UIElements.Layout;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.UIElements.Unmanaged;

namespace UnityEngine.UIElements
{
    [Serializable]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal partial class StyleProperty
    {
        [SerializeField]
        StylePropertyId m_Id;

        public StylePropertyId id
        {
            get => m_Id;
            internal set
            {
                if (value is StylePropertyId.Unknown or StylePropertyId.Custom)
                    throw new ArgumentException(nameof(value));
                m_Id = value;
                m_CustomName = null;
            }
        }

        [SerializeField]
        string m_CustomName;

        public string name
        {
            get
            {
                switch (id)
                {
                    case StylePropertyId.Unknown:
                    case StylePropertyId.Custom:
                        return m_CustomName;
                }

                return StylePropertyUtil.stylePropertyIdToPropertyName[id];
            }
            [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
            internal set => CacheId(value);
        }

        [SerializeField]
        int m_Line;

        public int line
        {
            get
            {
                return m_Line;
            }
            internal set
            {
                m_Line = value;
            }
        }

        [SerializeField]
        StyleValueHandle[] m_Values = Array.Empty<StyleValueHandle>();

        public StyleValueHandle[] values
        {
            get
            {
                return m_Values;
            }
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            internal set
            {
                m_Values = value;
            }
        }

        internal int handleCount
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get => m_Values?.Length ?? 0;
        }

        internal bool isCustomProperty
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
            get => id == StylePropertyId.Custom;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        [NonSerialized]
        internal bool requireVariableResolve;

        internal StyleProperty()
        {
        }

        internal void CacheId(string value)
        {
            m_Id = StylePropertyId.Unknown;
            m_CustomName = value;
            if (string.IsNullOrEmpty(value))
            {
                m_Id = StylePropertyId.Unknown;
            }
            else if (StringUtils.StartsWith(value, "--"))
            {
                m_Id = StylePropertyId.Custom;
            }
            else if (StylePropertyUtil.propertyNameToStylePropertyId.TryGetValue(value, out var valueId))
            {
                m_Id = valueId;
                m_CustomName = null;
            }
        }

        public bool ContainsVariable()
        {
            foreach (var value in values)
            {
                if (value.IsVarFunction())
                    return true;
            }

            return false;
        }

        public bool HasValue() => handleCount != 0;

        public void ClearValue()
        {
            m_Values = Array.Empty<StyleValueHandle>();
        }

        /// <summary>
        /// Sets a <see cref="StyleValueKeyword"/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The value to store.</param>
        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal void SetKeyword(StyleSheet styleSheet, StyleValueKeyword value)
        {
            SetSize(ref m_Values, 1);
            styleSheet.WriteKeyword(ref m_Values[0], value);
            requireVariableResolve = false;
        }

        /// <summary>
        /// Tries to read a <see cref="StyleValueKeyword"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetKeyword(StyleSheet styleSheet, out StyleValueKeyword value)
        {
            if (handleCount == 1)
                return styleSheet.TryReadKeyword(m_Values[0], out value);

            value = default;
            return false;
        }

        /// <summary>
        /// Sets a <see cref="float"/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The value to store.</param>
        public void SetFloat(StyleSheet styleSheet, float value)
        {
            SetSize(ref m_Values, 1);
            styleSheet.WriteFloat(ref m_Values[0], value);
            requireVariableResolve = false;
        }

        /// <summary>
        /// Tries to read a <see cref="float"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetFloat(StyleSheet styleSheet, out float value)
        {
            if (handleCount == 1)
                return styleSheet.TryReadFloat(m_Values[0], out value);

            value = 0.0f;
            return false;
        }

        /// <summary>
        /// Sets a <see cref="Dimension"/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The value to store.</param>
        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal void SetDimension(StyleSheet styleSheet, Dimension value)
        {
            SetSize(ref m_Values, 1);
            styleSheet.WriteDimension(ref m_Values[0], value);
            requireVariableResolve = false;
        }

        /// <summary>
        /// Tries to read a <see cref="Dimension"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetDimension(StyleSheet styleSheet, out Dimension value)
        {
            if (handleCount == 1)
                return styleSheet.TryReadDimension(m_Values[0], out value);

            value = default;
            return false;
        }

        /// <summary>
        /// Sets a <see cref="Color"/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The value to store.</param>
        public void SetColor(StyleSheet styleSheet, Color value)
        {
            SetSize(ref m_Values, 1);
            styleSheet.WriteColor(ref m_Values[0], value);
            requireVariableResolve = false;
        }

        /// <summary>
        /// Tries to read a <see cref="Color"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetColor(StyleSheet styleSheet, out Color value)
        {
            if (handleCount == 1)
                return styleSheet.TryReadColor(m_Values[0], out value);

            value = default;
            return false;
        }

        /// <summary>
        /// Sets a <see cref="string"/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The value to store.</param>
        public void SetString(StyleSheet styleSheet, string value)
        {
            SetSize(ref m_Values, 1);
            styleSheet.WriteString(ref values[0], value);
            requireVariableResolve = false;
        }

        /// <summary>
        /// Tries to read a <see cref="string"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetString(StyleSheet styleSheet, out string value)
        {
            if (handleCount == 1)
                return styleSheet.TryReadString(m_Values[0], out value);

            value = null;
            return false;
        }

        /// <summary>
        /// Sets a <see cref="Enum"/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The value to store.</param>
        public void SetEnum(StyleSheet styleSheet, Enum value)
        {
            SetSize(ref m_Values, 1);
            styleSheet.WriteEnum(ref m_Values[0], value);
            requireVariableResolve = false;
        }

        /// <summary>
        /// Sets a <see cref="Enum"/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="enumStr">The value to store.</param>
        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal void SetEnumAsString(StyleSheet styleSheet, string enumStr)
        {
            SetSize(ref m_Values, 1);
            styleSheet.WriteEnumAsString(ref m_Values[0], enumStr);
            requireVariableResolve = false;
        }

        /// <summary>
        /// Sets a <see cref="Enum"/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The value to store.</param>
        public void SetEnum<TEnum>(StyleSheet styleSheet, TEnum value)
            where TEnum: struct, Enum
        {
            SetSize(ref m_Values, 1);
            styleSheet.WriteEnum(ref m_Values[0], value);
            requireVariableResolve = false;
        }

        /// <summary>
        /// Tries to read a <see cref="Enum"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetEnumString(StyleSheet styleSheet, out string value)
        {
            if (handleCount == 1)
                return styleSheet.TryReadEnum(m_Values[0], out value);

            value = default;
            return false;
        }

        /// <summary>
        /// Tries to read a <see cref="TEnum"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetEnum<TEnum>(StyleSheet styleSheet, out TEnum value)
            where TEnum : struct, Enum
        {
            if (handleCount == 1)
                return styleSheet.TryReadEnum(m_Values[0], out value);

            value = default;
            return false;
        }

        /// <summary>
        /// Sets a variable reference as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="variableName">The name of the variable.</param>
        public void SetVariableReference(StyleSheet styleSheet, string variableName)
        {
            SetSize(ref m_Values, 3);
            styleSheet.WriteFunction(ref m_Values[0], StyleValueFunction.Var);
            styleSheet.WriteFloat(ref m_Values[1], 1);
            styleSheet.WriteVariable(ref m_Values[2], variableName);
            requireVariableResolve = true;
        }

        /// <summary>
        /// Tries to read a variable name from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="variableName">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetVariableReference(StyleSheet styleSheet, out string variableName)
        {
            if (handleCount == 3 &&
                styleSheet.TryReadFunction(m_Values[0], out var function) && function == StyleValueFunction.Var &&
                styleSheet.TryReadFloat(m_Values[1], out var argCount) && (int)argCount == 1)
            {
                return styleSheet.TryReadVariable(m_Values[2], out variableName);
            }

            variableName = null;
            return false;
        }

        /// <summary>
        /// Sets a resource path as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="resource">The resolved path value.</param>
        public void SetResourcePath(StyleSheet styleSheet, ResolvedResourcePath resource)
        {
            SetSize(ref m_Values, 1);
            styleSheet.WriteResourcePath(ref m_Values[0], resource);
            requireVariableResolve = false;
        }

        /// <summary>
        /// Tries to read a resource path from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="resourcePath">The resolved path value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetResourcePath(StyleSheet styleSheet, out ResolvedResourcePath resourcePath)
        {
            if (handleCount == 1)
                return styleSheet.TryReadResourcePath(m_Values[0], out resourcePath);
            resourcePath = default;
            return false;
        }

        /// <summary>
        /// Sets a <see cref="Background"/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The value to store.</param>
        public void SetBackground(StyleSheet styleSheet, Background value)
        {
            SetSize(ref m_Values, 1);
            styleSheet.WriteAssetReference(ref m_Values[0], value.GetSelectedImage());
            requireVariableResolve = false;
        }

        /// <summary>
        /// Tries to read a <see cref="Object"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetBackground(StyleSheet styleSheet, out Background value)
        {
            if (handleCount == 1)
            {
                if (styleSheet.TryReadAssetReference(m_Values[0], out var objectValue))
                {
                    value = Background.FromObject(objectValue);
                    return !value.IsEmpty();
                }
            }

            value = null;
            return false;
        }

        /// <summary>
        /// Sets a <see cref="Object"/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The value to store.</param>
        public void SetAssetReference(StyleSheet styleSheet, Object value)
        {
            SetSize(ref m_Values, 1);
            styleSheet.WriteAssetReference(ref m_Values[0], value);
            requireVariableResolve = false;
        }

        /// <summary>
        /// Tries to read a <see cref="Object"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetAssetReference(StyleSheet styleSheet, out Object value)
        {
            if (handleCount == 1)
                return styleSheet.TryReadAssetReference(m_Values[0], out value);

            value = null;
            return false;
        }

        /// <summary>
        /// Tries to read a <see cref="TObject"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetAssetReference<TObject>(StyleSheet styleSheet, out TObject value)
            where TObject : Object
        {
            if (TryGetAssetReference(styleSheet, out Object objectValue) &&
                objectValue is TObject typedValue)
            {
                value = typedValue;
                return true;
            }
            value = null;
            return false;
        }

        /// <summary>
        /// Sets a missing asset reference url as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The url.</param>
        public void SetMissingAssetReferenceUrl(StyleSheet styleSheet, string value)
        {
            SetSize(ref m_Values, 1);
            styleSheet.WriteMissingAssetReferenceUrl(ref m_Values[0], value);
            requireVariableResolve = false;
        }

        /// <summary>
        /// Tries to read a missing asset reference url from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetMissingAssetReferenceUrl(StyleSheet styleSheet, out string value)
        {
            if (handleCount == 1)
                return styleSheet.TryReadMissingAssetReferenceUrl(m_Values[0], out value);

            value = null;
            return false;
        }

        /// <summary>
        /// Sets a <see cref="ScalableImage"/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The url.</param>
        public void SetScalableImage(StyleSheet styleSheet, ScalableImage value)
        {
            SetSize(ref m_Values, 1);
            styleSheet.WriteScalableImage(ref m_Values[0], value);
            requireVariableResolve = false;
        }

        /// <summary>
        /// Tries to read a <see cref="ScalableImage"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetScalableImage(StyleSheet styleSheet, out ScalableImage value)
        {
            if (handleCount == 1)
                return styleSheet.TryReadScalableImage(m_Values[0], out value);

            value = default;
            return false;
        }

        /// <summary>
        /// Sets a <see cref="StyleKeyword"/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The value to store.</param>
        public void SetKeyword(StyleSheet styleSheet, StyleKeyword value)
        {
            SetKeyword(styleSheet, value.ToStyleValueKeyword());
            requireVariableResolve = false;
        }

        /// <summary>
        /// Tries to read a <see cref="StyleKeyword"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetKeyword(StyleSheet styleSheet, out StyleKeyword value)
        {
            if (handleCount == 1)
                return TryReadKeyword(styleSheet, ref m_Values[0], out value);

            value = default;
            return false;
        }

        /// <summary>
        /// Sets a <see cref="BackgroundRepeat"/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The value to store.</param>
        public void SetBackgroundRepeat(StyleSheet styleSheet, BackgroundRepeat value)
        {
            SetSize(ref m_Values, 2);
            styleSheet.WriteEnum(ref values[0], value.x);
            styleSheet.WriteEnum(ref values[1], value.y);
            requireVariableResolve = false;
        }

        /// <summary>
        /// Tries to read a <see cref="BackgroundRepeat"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetBackgroundRepeat(StyleSheet styleSheet, out BackgroundRepeat value)
        {
            if (handleCount is <= 0 or > 2)
            {
                value = default;
                return false;
            }

            var val1 = new StylePropertyValue { handle = values[0], sheet = styleSheet };
            var val2 = handleCount > 1 ? new StylePropertyValue { handle = values[1], sheet = styleSheet } : default;

            value = StylePropertyReader.ReadBackgroundRepeat(handleCount, val1, val2);
            return true;
        }

        /// <summary>
        /// Sets a <see cref="BackgroundSize"/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The value to store.</param>
        public void SetBackgroundSize(StyleSheet styleSheet, BackgroundSize value)
        {
            switch (value.sizeType)
            {
                case BackgroundSizeType.Length:
                    SetSize(ref m_Values, 2);
                    styleSheet.WriteLength(ref values[0], value.x);
                    styleSheet.WriteLength(ref values[1], value.y);
                    break;
                case BackgroundSizeType.Cover:
                    SetSize(ref m_Values, 1);
                    styleSheet.WriteKeyword(ref values[0], StyleValueKeyword.Cover);
                    break;
                case BackgroundSizeType.Contain:
                    SetSize(ref m_Values, 1);
                    styleSheet.WriteKeyword(ref values[0], StyleValueKeyword.Contain);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            requireVariableResolve = false;
        }

        /// <summary>
        /// Tries to read a <see cref="BackgroundSize"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetBackgroundSize(StyleSheet styleSheet, out BackgroundSize value)
        {
            if (handleCount is <= 0 or > 2)
            {
                value = default;
                return false;
            }

            var val1 = new StylePropertyValue() { handle = values[0], sheet = styleSheet };
            var val2 = handleCount > 1 ? new StylePropertyValue() { handle = values[1], sheet = styleSheet } : default;

            value = StylePropertyReader.ReadBackgroundSize(handleCount, val1, val2);
            return true;
        }

        /// <summary>
        /// Sets a <see cref="BackgroundPosition"/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The value to store.</param>
        public void SetBackgroundPosition(StyleSheet styleSheet, BackgroundPosition value)
        {
            if (value.keyword == BackgroundPositionKeyword.Center)
            {
                SetSize(ref m_Values, 1);
                styleSheet.WriteEnum(ref values[0], value.keyword);
                requireVariableResolve = false;
                return;
            }

            SetSize(ref m_Values, 2);
            styleSheet.WriteEnum(ref values[0], value.keyword);
            styleSheet.WriteDimension(ref values[1], value.offset.ToDimension());
            requireVariableResolve = false;
        }

        /// <summary>
        /// Tries to read a <see cref="BackgroundPosition"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <param name="axis">The axis to use if the value has not provided it.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal bool TryGetBackgroundPosition(StyleSheet styleSheet, out BackgroundPosition value, BackgroundPosition.Axis axis)
        {
            if (handleCount is <= 0 or > 2)
            {
                value = default;
                return false;
            }

            var val1 = new StylePropertyValue() { handle = values[0], sheet = styleSheet };
            var val2 = handleCount > 1 ? new StylePropertyValue { handle = values[1], sheet = styleSheet } : default;

            value = StylePropertyReader.ReadBackgroundPosition(handleCount, val1, val2, axis == BackgroundPosition.Axis.Horizontal
                ? BackgroundPositionKeyword.Left
                : BackgroundPositionKeyword.Top);
            return true;
        }

        /// <summary>
        /// Sets a <see cref="int"/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The value to store.</param>
        public void SetInt(StyleSheet styleSheet, int value)
        {
            SetFloat(styleSheet, value);
        }

        /// <summary>
        /// Tries to read a <see cref="int"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetInt(StyleSheet styleSheet, out int value)
        {
            if (TryGetFloat(styleSheet, out var floatValue))
            {
                value = (int)floatValue;
                return true;
            }

            value = 0;
            return false;
        }

        /// <summary>
        /// Sets a <see cref="Length"/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The value to store.</param>
        public void SetLength(StyleSheet styleSheet, Length value)
        {
            if (value.IsAuto())
                SetKeyword(styleSheet, StyleValueKeyword.Auto);
            else if (value.IsNone())
                SetKeyword(styleSheet, StyleValueKeyword.None);
            else
                SetDimension(styleSheet, value.ToDimension());
        }

        /// <summary>
        /// Tries to read a <see cref="Length"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetLength(StyleSheet styleSheet, out Length value)
        {
            if (handleCount != 1)
            {
                value = default;
                return false;
            }

            if (styleSheet.TryReadKeyword(m_Values[0], out var keyword))
            {
                switch (keyword)
                {
                    case StyleValueKeyword.Initial:
                        value = default;
                        return true;
                    case StyleValueKeyword.Auto:
                        value = Length.Auto();
                        return true;
                    case StyleValueKeyword.None:
                        value = Length.None();
                        return true;
                    default:
                        value = default;
                        return false;
                }
            }

            if (styleSheet.TryReadDimension(m_Values[0], out var dimension) && dimension.IsLength())
            {
                value = dimension.ToLength();
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Sets a <see cref="Translate"/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The value to store.</param>
        public void SetTranslate(StyleSheet styleSheet, Translate value)
        {
            if (value.IsNone())
            {
                SetSize(ref m_Values, 1);
                styleSheet.WriteKeyword(ref m_Values[0], StyleValueKeyword.None);
                return;
            }

            if (value.z == 0.0f)
            {
                SetSize(ref m_Values, 2);
                styleSheet.WriteDimension(ref m_Values[0], value.x.ToDimension());
                styleSheet.WriteDimension(ref m_Values[1], value.y.ToDimension());
                return;
            }

            SetSize(ref m_Values, 3);
            styleSheet.WriteDimension(ref m_Values[0], value.x.ToDimension());
            styleSheet.WriteDimension(ref m_Values[1], value.y.ToDimension());
            styleSheet.WriteDimension(ref m_Values[2], new Length(value.z).ToDimension());
            requireVariableResolve = false;
        }

        /// <summary>
        /// Tries to read a <see cref="Translate"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetTranslate(StyleSheet styleSheet, out Translate value)
        {
            if (handleCount is <= 0 or > 3)
            {
                value = default;
                return false;
            }

            var valCount = handleCount;
            var x = new StylePropertyValue() { handle = values[0], sheet = styleSheet };
            var y = valCount > 1 ? new StylePropertyValue { handle = values[1], sheet = styleSheet } : default;
            var z = valCount > 2 ? new StylePropertyValue { handle = values[2], sheet = styleSheet } : default;

            value = StylePropertyReader.ReadTranslate(valCount, x, y, z);
            return true;
        }

        /// <summary>
        /// Sets a <see cref="Ratio"/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The value to store.</param>
        public void SetRatio(StyleSheet styleSheet, Ratio value)
        {
            SetSize(ref m_Values, 1);
            styleSheet.WriteRatio(ref m_Values[0], value);
        }

        /// <summary>
        /// Tries to read a <see cref="Ratio"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetRatio(StyleSheet styleSheet, out Ratio value)
        {
            if (handleCount != 1)
            {
                value = default;
                return false;
            }

            if (styleSheet.TryReadRatio(m_Values[0], out value))
            {
                return true;
            }

            value = Ratio.Auto();
            return false;
        }

        /// <summary>
        /// Sets a <see cref="Rotate"/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The value to store.</param>
        public void SetRotate(StyleSheet styleSheet, Rotate value)
        {
            if (value.IsNone())
            {
                SetSize(ref m_Values, 1);
                styleSheet.WriteKeyword(ref values[0], StyleValueKeyword.None);
                requireVariableResolve = false;
                return;
            }

            if (value.axis == Vector3.forward)
            {
                SetSize(ref m_Values, 1);
                styleSheet.WriteAngle(ref values[0], value.angle);
                requireVariableResolve = false;
                return;
            }

            SetSize(ref m_Values, 4);
            var axis = value.axis;
            styleSheet.WriteFloat(ref values[0], axis.x);
            styleSheet.WriteFloat(ref values[1], axis.y);
            styleSheet.WriteFloat(ref values[2], axis.z);
            styleSheet.WriteAngle(ref values[3], value.angle);
            requireVariableResolve = false;
        }

        /// <summary>
        /// Tries to read a <see cref="Rotate"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetRotate(StyleSheet styleSheet, out Rotate value)
        {
            if (handleCount is <= 0 or > 4)
            {
                value = default;
                return false;
            }

            var valCount = handleCount;
            var val1 = new StylePropertyValue() { handle = values[0], sheet = styleSheet };
            var val2 = valCount > 1 ? new StylePropertyValue { handle = values[1], sheet = styleSheet } : default;
            var val3 = valCount > 2 ? new StylePropertyValue { handle = values[2], sheet = styleSheet } : default;
            var val4 = valCount > 2 ? new StylePropertyValue { handle = values[3], sheet = styleSheet } : default;

            value = StylePropertyReader.ReadRotate(valCount, val1, val2, val3, val4);
            return true;
        }

        /// <summary>
        /// Sets a <see cref="Scale"/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The value to store.</param>
        public void SetScale(StyleSheet styleSheet, Scale value)
        {
            if (value.IsNone())
            {
                SetSize(ref m_Values, 1);
                styleSheet.WriteKeyword(ref values[0], StyleValueKeyword.None);
                requireVariableResolve = false;
                return;
            }

            if (Mathf.Approximately(value.value.z, 1.0f))
            {
                SetSize(ref m_Values, 2);
                styleSheet.WriteFloat(ref values[0], value.value.x);
                styleSheet.WriteFloat(ref values[1], value.value.y);
                requireVariableResolve = false;
                return;
            }

            SetSize(ref m_Values, 3);
            styleSheet.WriteFloat(ref values[0], value.value.x);
            styleSheet.WriteFloat(ref values[1], value.value.y);
            styleSheet.WriteFloat(ref values[2], value.value.z);
            requireVariableResolve = false;
        }

        /// <summary>
        /// Tries to read a <see cref="Scale"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetScale(StyleSheet styleSheet, out Scale value)
        {
            if (handleCount is <= 0 or > 3)
            {
                value = default;
                return false;
            }

            var valCount = handleCount;
            var x = new StylePropertyValue() { handle = values[0], sheet = styleSheet };
            var y = valCount > 1 ? new StylePropertyValue { handle = values[1], sheet = styleSheet } : default;
            var z = valCount > 2 ? new StylePropertyValue { handle = values[2], sheet = styleSheet } : default;

            value = StylePropertyReader.ReadScale(valCount, x, y, z);
            return true;
        }

        /// <summary>
        /// Sets a <see cref="TextShadow "/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The value to store.</param>
        public void SetTextShadow(StyleSheet styleSheet, TextShadow value)
        {
            SetSize(ref m_Values, 4);
            styleSheet.WriteDimension(ref values[0], new Dimension { value = value.offset.x, unit = Dimension.Unit.Pixel });
            styleSheet.WriteDimension(ref values[1], new Dimension { value = value.offset.y, unit = Dimension.Unit.Pixel });
            styleSheet.WriteDimension(ref values[2], new Dimension { value = value.blurRadius, unit = Dimension.Unit.Pixel });
            styleSheet.WriteColor(ref values[3], value.color);
            requireVariableResolve = false;
        }

        /// <summary>
        /// Tries to read a <see cref="TextShadow "/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetTextShadow(StyleSheet styleSheet, out TextShadow  value)
        {
            if (handleCount is <= 0 or > 4)
            {
                value = default;
                return false;
            }

            var valCount = handleCount;
            var val1 = new StylePropertyValue() { handle = values[0], sheet = styleSheet };
            var val2 = valCount > 1 ? new StylePropertyValue { handle = values[1], sheet = styleSheet } : default;
            var val3 = valCount > 2 ? new StylePropertyValue { handle = values[2], sheet = styleSheet } : default;
            var val4 = valCount > 3 ? new StylePropertyValue { handle = values[3], sheet = styleSheet } : default;

            value = StylePropertyReader.ReadTextShadow(valCount, val1, val2, val3, val4);
            return true;
        }

        /// <summary>
        /// Sets a <see cref="TextAutoSize "/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The value to store.</param>
        public void SetTextAutoSize(StyleSheet styleSheet, TextAutoSize value)
        {
            if (value.mode == TextAutoSizeMode.None)
            {
                SetSize(ref m_Values, 1);
                styleSheet.WriteEnum(ref m_Values[0], value.mode);
                return;
            }

            SetSize(ref m_Values, 3);
            styleSheet.WriteEnum(ref m_Values[0], value.mode);
            styleSheet.WriteDimension(ref values[1], new Dimension { value = value.minSize.value, unit = Dimension.Unit.Pixel });
            styleSheet.WriteDimension(ref values[2], new Dimension { value = value.maxSize.value, unit = Dimension.Unit.Pixel });
        }

        /// <summary>
        /// Tries to read a <see cref="TextAutoSize"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetTextAutoSize(StyleSheet styleSheet, out TextAutoSize value)
        {
            if (handleCount is <= 0 or > 3)
            {
                value = TextAutoSize.None();
                return false;
            }

            var valCount = handleCount;
            var val1 = new StylePropertyValue() { handle = values[0], sheet = styleSheet };
            var val2 = valCount > 1 ? new StylePropertyValue { handle = values[1], sheet = styleSheet } : default;
            var val3 = valCount > 2 ? new StylePropertyValue { handle = values[2], sheet = styleSheet } : default;

            value = StylePropertyReader.ReadTextAutoSize(valCount, val1, val2, val3);
            return true;
        }

        /// <summary>
        /// Sets a <see cref="TransformOrigin"/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The value to store.</param>
        public void SetTransformOrigin(StyleSheet styleSheet, TransformOrigin value)
        {
            var xOffset = GetTransformOriginOffset(value.x, true);
            var yOffset = GetTransformOriginOffset(value.y, false);

            var writeZ = value.z != 0.0f;

            if (!writeZ)
            {
                if (yOffset is TransformOriginOffset.Center)
                {
                    SetSize(ref m_Values, 1);
                    if (xOffset.HasValue)
                        styleSheet.WriteEnum(ref m_Values[0], xOffset.Value);
                    else
                        styleSheet.WriteDimension(ref m_Values[0], value.x.ToDimension());
                    requireVariableResolve = false;
                    return;
                }

                if (xOffset.HasValue && yOffset.HasValue && xOffset.Value == TransformOriginOffset.Center)
                {
                    SetSize(ref m_Values, 1);
                    styleSheet.WriteEnum(ref m_Values[0], yOffset.Value);
                    requireVariableResolve = false;
                    return;
                }
            }

            SetSize(ref m_Values, 2 + (writeZ ? 1 : 0));
            if (xOffset.HasValue)
                styleSheet.WriteEnum(ref m_Values[0], xOffset.Value);
            else
                styleSheet.WriteDimension(ref m_Values[0], value.x.ToDimension());

            if (yOffset.HasValue)
                styleSheet.WriteEnum(ref m_Values[1], yOffset.Value);
            else
                styleSheet.WriteDimension(ref m_Values[1], value.y.ToDimension());

            if (writeZ)
                styleSheet.WriteDimension(ref m_Values[2], new Dimension(value.z, Dimension.Unit.Pixel));
            requireVariableResolve = false;
        }

        /// <summary>
        /// Tries to read a <see cref="TransformOrigin"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetTransformOrigin(StyleSheet styleSheet, out TransformOrigin value)
        {
            if (handleCount is <= 0 or > 3)
            {
                value = default;
                return false;
            }

            var valCount = handleCount;
            var val1 = new StylePropertyValue() { handle = values[0], sheet = styleSheet };
            var val2 = valCount > 1 ? new StylePropertyValue { handle = values[1], sheet = styleSheet } : default;
            var val3 = valCount > 2 ? new StylePropertyValue { handle = values[2], sheet = styleSheet } : default;

            value = StylePropertyReader.ReadTransformOrigin(valCount, val1, val2, val3);
            return true;
        }

        /// <summary>
        /// Sets a <see cref="Font"/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The value to store.</param>
        public void SetFont(StyleSheet styleSheet, Font value)
        {
            SetAssetReference(styleSheet, value);
        }

        /// <summary>
        /// Tries to read a <see cref="Font"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetFont(StyleSheet styleSheet, out Font value)
        {
            if (TryGetAssetReference(styleSheet, out var objectValue) && objectValue is Font fontValue)
            {
                value = fontValue;
                return true;
            }

            value = null;
            return false;
        }

        /// <summary>
        /// Sets a <see cref="FontDefinition"/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The value to store.</param>
        public void SetFontDefinition(StyleSheet styleSheet, FontDefinition value)
        {
            if (value.fontAsset)
                SetAssetReference(styleSheet, value.fontAsset);
            else if (value.font)
                SetAssetReference(styleSheet, value.font);
            else
                SetAssetReference(styleSheet, null);
        }

        /// <summary>
        /// Tries to read a <see cref="FontDefinition"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetFontDefinition(StyleSheet styleSheet, out FontDefinition value)
        {
            if (TryGetAssetReference(styleSheet, out var objectValue))
            {
                value = FontDefinition.FromObject(objectValue);
                return !value.IsEmpty();
            }

            value = default;
            return false;
        }
        internal static int ArgumentCountForMaterialPropertyValueType(MaterialPropertyValueType type)
        {
            switch (type)
            {
                case MaterialPropertyValueType.Color:
                case MaterialPropertyValueType.Float:
                case MaterialPropertyValueType.Texture:
                    return 1;
                case MaterialPropertyValueType.Vector:
                    return 4;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Sets a <see cref="MaterialDefinition"/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The value to store.</param>
        public void SetMaterialDefinition(StyleSheet styleSheet, MaterialDefinition value)
        {
            int totalSize = 1; // Material reference
            if (value.propertyValues != null)
            {
                foreach (var prop in value.propertyValues)
                    totalSize += ArgumentCountForMaterialPropertyValueType(prop.type) + 3; // Func + ArgCount + Name
            }

            SetSize(ref m_Values, totalSize);

            styleSheet.WriteAssetReference(ref values[0], value.material);

            if (value.propertyValues == null)
                return; // We're done here

            int i = 1;
            foreach (var prop in value.propertyValues)
            {
                styleSheet.WriteFunction(ref values[i++], StyleValueFunction.MaterialProperty);
                styleSheet.WriteFloat(ref values[i++], ArgumentCountForMaterialPropertyValueType(prop.type) + 1);
                styleSheet.WriteString(ref values[i++], prop.name);

                switch (prop.type)
                {
                    case MaterialPropertyValueType.Color:
                        var color = value.GetColor(prop.name);
                        styleSheet.WriteColor(ref values[i++], color);
                        break;
                    case MaterialPropertyValueType.Float:
                        var floatVal = value.GetFloat(prop.name);
                        styleSheet.WriteFloat(ref values[i++], floatVal);
                        break;
                    case MaterialPropertyValueType.Vector:
                        var vector = value.GetVector(prop.name);
                        styleSheet.WriteFloat(ref values[i++], vector.x);
                        styleSheet.WriteFloat(ref values[i++], vector.y);
                        styleSheet.WriteFloat(ref values[i++], vector.z);
                        styleSheet.WriteFloat(ref values[i++], vector.w);
                        break;
                    case MaterialPropertyValueType.Texture:
                        var tex = value.GetTexture(prop.name);
                        styleSheet.WriteAssetReference(ref values[i++], tex);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Tries to read a <see cref="MaterialDefinition"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetMaterialDefinition(StyleSheet styleSheet, ref UnmanagedMaterialDefinition value)
        {
            if (handleCount < 1)
                return false;

            if (!styleSheet.TryReadAssetReference(values[0], out var matObj))
            {
                if (!styleSheet.TryReadResourcePath(values[0], out var resourcePath))
                    return false;
                else
                    matObj = resourcePath.LoadResource<Material>(1.0f);
            }

            // Read material properties
            using (var tmpList = new UnmanagedTempList<UnmanagedMaterialPropertyValue>(2))
            {
                for (int i = 1; i < values.Length;)
                {
                    var fnType = (StyleValueFunction)values[i++].valueIndex;
                    if (fnType != StyleValueFunction.MaterialProperty)
                        break;

                    if (!styleSheet.TryReadFloat(values[i++], out var ac))
                        return false;

                    int argCount = (int)ac;

                    if (!styleSheet.TryReadString(values[i++], out string propertyName))
                        return false;

                    var valueType = values[i].valueType;
                    var propID = Shader.PropertyToID(propertyName);

                    UnmanagedMaterialPropertyValue propertyValue;

                    if (valueType == StyleValueType.Float)
                    {
                        int vecDim = argCount - 1; // -1 to account for the property name
                        var vec = Vector4.zero;
                        int vecIndex = 0;
                        while (vecIndex < vecDim)
                        {
                            if (!styleSheet.TryReadFloat(values[i++], out var f))
                                return false;
                            vec[vecIndex++] = f;
                        }

                        var type = (vecDim > 1) ? MaterialPropertyValueType.Vector : MaterialPropertyValueType.Float;

                        propertyValue = new UnmanagedMaterialPropertyValue()
                        {
                            name = propID,
                            type = type,
                            packedValue = vec
                        };
                    }
                    else if (valueType == StyleValueType.Color || valueType == StyleValueType.Enum)
                    {
                        if (!styleSheet.TryReadColor(values[i++], out var c))
                            return false;
                        propertyValue = new UnmanagedMaterialPropertyValue()
                        {
                            name = propID,
                            type = MaterialPropertyValueType.Color,
                            packedValue = new Vector4(c.r, c.g, c.b, c.a)
                        };
                    }
                    else if (valueType == StyleValueType.AssetReference || valueType == StyleValueType.ResourcePath || valueType == StyleValueType.MissingAssetReference)
                    {
                        Object tex = null;
                        if (valueType != StyleValueType.MissingAssetReference)
                        {
                            if (values[i].valueType == StyleValueType.AssetReference)
                            {
                                if (!styleSheet.TryReadAssetReference(values[i++], out tex))
                                    return false;
                            }
                            else if (values[i].valueType == StyleValueType.ResourcePath)
                            {
                                if (!styleSheet.TryReadResourcePath(values[i++], out var resourcePath))
                                    return false;
                                tex = resourcePath.LoadResource<Texture>(1.0f);
                            }
                        }
                        else
                            ++i;

                        propertyValue = new UnmanagedMaterialPropertyValue()
                        {
                            name = propID,
                            type = MaterialPropertyValueType.Texture,
                            textureValue = tex != null ? tex.GetEntityId() : EntityId.None
                        };
                    }
                    else
                    {
                        Debug.LogError($"Unexpected value type {valueType} in material property argument");
                        return false;
                    }

                    tmpList.Add(propertyValue);
                }

                var material = matObj as Material;

                value.material = material != null ? material.GetEntityId() : EntityId.None;
                value.propertyValues.CopyFrom(tmpList.Span);
            }

            return true;
        }

        /// <summary>
        /// Sets a <see cref="Cursor"/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The value to store.</param>
        public void SetCursor(StyleSheet styleSheet, Cursor value)
        {
            if (value.defaultCursorId != 0)
            {
                Debug.LogWarning("Runtime cursors other than the default cursor need to be defined using a texture.");
            }

            if (value.hotspot != Vector2.zero)
            {
                SetSize(ref m_Values, 3);
                styleSheet.WriteAssetReference(ref values[0], value.texture);
                styleSheet.WriteFloat(ref values[1], value.hotspot.x);
                styleSheet.WriteFloat(ref values[2], value.hotspot.y);
            }
            else
            {
                SetSize(ref m_Values, 1);
                styleSheet.WriteAssetReference(ref values[0], value.texture);
            }
        }

        /// <summary>
        /// Tries to read a <see cref="Cursor"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetCursor(StyleSheet styleSheet, out Cursor value)
        {
            if (handleCount is <= 0 or > 3)
            {
                value = default;
                return false;
            }

            var valCount = handleCount;
            var val1 = new StylePropertyValue() { handle = values[0], sheet = styleSheet };
            var val2 = valCount > 1 ? new StylePropertyValue { handle = values[1], sheet = styleSheet } : default;
            var val3 = valCount > 2 ? new StylePropertyValue { handle = values[2], sheet = styleSheet } : default;

            value = StylePropertyReader.ReadCursor(handleCount, val1, val2, val3);
            return true;
        }

        /// <summary>
        /// Sets a <see cref="List{TimeValue}"/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The value to store.</param>
        public void SetTimeValueList(StyleSheet styleSheet, List<TimeValue> value)
        {
            SetSize(ref m_Values, value.Count * 2 - 1);
            for (var i = 0; i < value.Count; ++i)
            {
                var handleIndex = i * 2;
                styleSheet.WriteDimension(ref values[handleIndex], value[i].ToDimension());
                if (i < value.Count - 1)
                    styleSheet.WriteCommaSeparator(ref values[handleIndex + 1]);
            }
            requireVariableResolve = false;
        }

        /// <summary>
        /// Tries to read a <see cref="List{TimeValue}"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetTimeValueList(StyleSheet styleSheet, out List<TimeValue> value)
        {
            if (ContainsVariable())
            {
                value = null;
                return false;
            }

            value = new List<TimeValue>();
            return TryGetTimeValueList(styleSheet, value);
        }

        /// <summary>
        /// Tries to read a <see cref="List{TimeValue}"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetTimeValueList(StyleSheet styleSheet, List<TimeValue> value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            value.Clear();

            if (ContainsVariable())
                return false;

            for (var i = 0; i < m_Values.Length; i+=2)
            {
                var commaIndex = i + 1;

                if (!styleSheet.TryReadTimeValue(m_Values[i], out TimeValue timeValue) ||
                    commaIndex < m_Values.Length && values[commaIndex].valueType != StyleValueType.CommaSeparator)
                {
                    value.Clear();
                    return false;
                }
                value.Add(timeValue);
            }

            return true;
        }

        /// <summary>
        /// Sets a <see cref="List{StylePropertyName}"/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The value to store.</param>
        public void SetStylePropertyNameList(StyleSheet styleSheet, List<StylePropertyName> value)
        {
            SetSize(ref m_Values, value.Count * 2 - 1);
            for (var i = 0; i < value.Count; ++i)
            {
                var handleIndex = i * 2;
                styleSheet.WriteStylePropertyName(ref values[handleIndex], value[i]);
                if (i < value.Count - 1)
                    styleSheet.WriteCommaSeparator(ref values[handleIndex + 1]);
            }
            requireVariableResolve = false;
        }

        /// <summary>
        /// Tries to read a <see cref="List{StylePropertyName}"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetStylePropertyNameList(StyleSheet styleSheet, out List<StylePropertyName> value)
        {
            if (ContainsVariable())
            {
                value = null;
                return false;
            }

            value = new List<StylePropertyName>();
            return TryGetStylePropertyNameList(styleSheet, value);
        }

        /// <summary>
        /// Tries to read a <see cref="List{StylePropertyName}"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetStylePropertyNameList(StyleSheet styleSheet, List<StylePropertyName> value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            value.Clear();

            if (ContainsVariable())
                return false;

            for (var i = 0; i < m_Values.Length; i+=2)
            {
                var commaIndex = i + 1;

                if (!styleSheet.TryReadStylePropertyName(m_Values[i], out var propertyName) ||
                    commaIndex < m_Values.Length && values[commaIndex].valueType != StyleValueType.CommaSeparator)
                {
                    value.Clear();
                    return false;
                }

                value.Add(propertyName);
            }

            return true;
        }

        /// <summary>
        /// Sets a <see cref="List{EasingFunction}"/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The value to store.</param>
        public void SetEasingFunctionList(StyleSheet styleSheet, List<EasingFunction> value)
        {
            SetSize(ref m_Values, value.Count * 2 - 1);
            for (var i = 0; i < value.Count; ++i)
            {
                var handleIndex = i * 2;
                styleSheet.WriteEnum(ref values[handleIndex], value[i].mode);
                if (i < value.Count - 1)
                    styleSheet.WriteCommaSeparator(ref values[handleIndex + 1]);
            }
            requireVariableResolve = false;
        }

        /// <summary>
        /// Tries to read a <see cref="List{EasingFunction}"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetEasingFunctionList(StyleSheet styleSheet, out List<EasingFunction> value)
        {
            if (ContainsVariable())
            {
                value = null;
                return false;
            }

            value = new List<EasingFunction>();
            return TryGetEasingFunctionList(styleSheet, value);
        }

        /// <summary>
        /// Tries to read a <see cref="List{EasingFunction}"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetEasingFunctionList(StyleSheet styleSheet, List<EasingFunction> value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            value.Clear();

            if (ContainsVariable())
                return false;

            for (var i = 0; i < m_Values.Length; i+=2)
            {
                var commaIndex = i + 1;

                if (!styleSheet.TryReadEnum(m_Values[i], out EasingMode easingMode) ||
                    commaIndex < m_Values.Length && values[commaIndex].valueType != StyleValueType.CommaSeparator)
                {
                    value.Clear();
                    return false;
                }

                value.Add(new EasingFunction(easingMode));
            }

            return true;
        }

        /// <summary>
        /// Sets a <see cref="List{FilterFunction}"/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The value to store.</param>
        /// <remarks>
        /// </remarks>
        public void SetFilterFunctionList(StyleSheet styleSheet, List<FilterFunction> value)
        {
            // Count the required number of values.
            int valueCount = 0;
            foreach (var ff in value)
                valueCount += GetNumberOfValuesForFilterFunction(ff);

            SetSize(ref m_Values, valueCount);

            int index = 0;
            foreach (var ff in value)
            {
                styleSheet.WriteFunction(ref values[index++], ToStyleValueFunction(ff.type));

                int argCount = ff.parameterCount;
                if (ff.customDefinition != null)
                    ++argCount;

                styleSheet.WriteFloat(ref values[index++], argCount);

                if (ff.customDefinition != null)
                    styleSheet.WriteAssetReference(ref values[index++], ff.customDefinition);

                for (int i = 0; i < ff.parameterCount; ++i)
                {
                    var p = ff.GetParameter(i);
                    if (p.type == FilterParameterType.Float)
                        styleSheet.WriteFloat(ref values[index++], p.floatValue);
                    else if (p.type == FilterParameterType.Color)
                        styleSheet.WriteColor(ref values[index++], p.colorValue);
                }
            }
        }

        /// <summary>
        /// Tries to read a <see cref="List{FilterFunction}"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetFilterFunctionList(StyleSheet styleSheet, out List<FilterFunction> value)
        {
            if (ContainsVariable())
            {
                value = null;
                return false;
            }

            value = new List<FilterFunction>();
            return TryGetFilterFunctionList(styleSheet, value);
        }

        /// <summary>
        /// Tries to read a <see cref="List{FilterFunction}"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetFilterFunctionList(StyleSheet styleSheet, List<FilterFunction> value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            value.Clear();

            if (ContainsVariable())
                return false;

            for (var i = 0; i < m_Values.Length; )
            {
                if (!styleSheet.TryReadFunction(m_Values[i++], out StyleValueFunction func))
                {
                    value.Clear();
                    return false;
                }

                if (!styleSheet.TryReadFloat(m_Values[i++], out float fCount))
                {
                    value.Clear();
                    return false;
                }

                int argCount = (int)fCount;

                FilterFunctionDefinition customDef = null;
                if (func == StyleValueFunction.CustomFilter && argCount > 0)
                {
                    if (!styleSheet.TryReadAssetReference(m_Values[i++], out var customDefObj))
                    {
                        value.Clear();
                        return false;
                    }

                    customDef = customDefObj as FilterFunctionDefinition;
                    if (customDef == null)
                    {
                        value.Clear();
                        return false;
                    }

                    --argCount;

                    if (customDef.parameters.Length != argCount)
                    {
                        value.Clear();
                        return false;
                    }
                }

                var args = new FixedBuffer4<FilterParameter>();
                for (int p = 0; p < argCount; ++p)
                {
                    var paramHandle = m_Values[i++];
                    if (styleSheet.TryReadDimension(paramHandle, out Dimension dim))
                    {
                        args[p] = new FilterParameter(ConvertDimensionToFilterFloat(dim));
                    }
                    else if (styleSheet.TryReadFloat(paramHandle, out float f))
                    {
                        args[p] = new FilterParameter(f);
                    }
                    else if (styleSheet.TryReadColor(paramHandle, out Color color))
                    {
                        args[p] = new FilterParameter(color);
                    }
                    else
                    {
                        value.Clear();
                        return false;
                    }
                }

                if (func == StyleValueFunction.CustomFilter)
                    value.Add(new FilterFunction(customDef, args, argCount));
                else
                    value.Add(new FilterFunction(StyleProperty.ToFilterFunctionType(func), args, argCount));
            }

            return true;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        static internal int GetNumberOfValuesForFilterFunction(FilterFunction ff)
        {
            int valueCount = 0;

            var def = ff.GetDefinition();
            int paramCount = def?.parameters.Length ?? 0;
            if (ff.customDefinition != null)
                ++valueCount; // Filter definition asset

            valueCount += paramCount + 2; // Function type and parameters

            return valueCount;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        static internal FilterFunctionType ToFilterFunctionType(StyleValueFunction function)
        {
            switch (function)
            {
                case StyleValueFunction.CustomFilter:    return FilterFunctionType.Custom;
                case StyleValueFunction.FilterTint:      return FilterFunctionType.Tint;
                case StyleValueFunction.FilterOpacity:   return FilterFunctionType.Opacity;
                case StyleValueFunction.FilterInvert:    return FilterFunctionType.Invert;
                case StyleValueFunction.FilterGrayscale: return FilterFunctionType.Grayscale;
                case StyleValueFunction.FilterSepia:     return FilterFunctionType.Sepia;
                case StyleValueFunction.FilterBlur:      return FilterFunctionType.Blur;
                case StyleValueFunction.FilterContrast:  return FilterFunctionType.Contrast;
                case StyleValueFunction.FilterHueRotate: return FilterFunctionType.HueRotate;
                default:
                    return FilterFunctionType.None;
            }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        static internal StyleValueFunction ToStyleValueFunction(FilterFunctionType type)
        {
            switch (type)
            {
                case FilterFunctionType.None:      return StyleValueFunction.NoneFilter;
                case FilterFunctionType.Tint:      return StyleValueFunction.FilterTint;
                case FilterFunctionType.Opacity:   return StyleValueFunction.FilterOpacity;
                case FilterFunctionType.Invert:    return StyleValueFunction.FilterInvert;
                case FilterFunctionType.Grayscale: return StyleValueFunction.FilterGrayscale;
                case FilterFunctionType.Sepia:     return StyleValueFunction.FilterSepia;
                case FilterFunctionType.Blur:      return StyleValueFunction.FilterBlur;
                case FilterFunctionType.Contrast:  return StyleValueFunction.FilterContrast;
                case FilterFunctionType.HueRotate: return StyleValueFunction.FilterHueRotate;
                default:
                    return StyleValueFunction.CustomFilter;
            }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        static internal float ConvertDimensionToFilterFloat(Dimension dim)
        {
            // Convert percentages to 0-1 range.
            // Convert angles to radians.
            // Convert time to seconds.
            switch (dim.unit)
            {
                case Dimension.Unit.Percent:     return dim.value * 0.01f;
                case Dimension.Unit.Degree:      return dim.value * Mathf.Deg2Rad;
                case Dimension.Unit.Turn:        return dim.value * Mathf.PI * 2.0f;
                case Dimension.Unit.Gradian:     return dim.value * Mathf.PI / 200.0f;
                case Dimension.Unit.Millisecond: return dim.value * 0.001f;
                default:
                    return dim.value;
            }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        static internal Dimension ConvertFilterFloatToDimension(float value, Dimension.Unit unit)
        {
            // Counterpart to ConvertDimensionToFilterFloat
            switch (unit)
            {
                case Dimension.Unit.Percent:     value *= 100.0f; break;
                case Dimension.Unit.Degree:      value *= Mathf.Rad2Deg; break;
                case Dimension.Unit.Turn:        value /= (Mathf.PI * 2.0f); break;
                case Dimension.Unit.Gradian:     value /= (Mathf.PI / 200.0f); break;
                case Dimension.Unit.Millisecond: value *= 1000.0f; break;
                default:
                    break;
            }
            return new Dimension(value, unit);
        }

        private static void SetSize(ref StyleValueHandle[] store, int size)
        {
            if (store?.Length == size)
                return;
            store = new StyleValueHandle[size];
        }

        internal static bool TryReadKeyword(StyleSheet styleSheet, ref StyleValueHandle handle, out StyleKeyword value)
        {
            if (handle.valueType == StyleValueType.Keyword)
            {
                var valueKeyword = (StyleValueKeyword)handle.valueIndex;
                switch (valueKeyword)
                {
                    case StyleValueKeyword.Initial:
                        value = StyleKeyword.Initial;
                        return true;
                    case StyleValueKeyword.Auto:
                        value = StyleKeyword.Auto;
                        return true;
                    case StyleValueKeyword.None:
                        value = StyleKeyword.None;
                        return true;
                }
            }

            value = default;
            return false;
        }

        static TransformOriginOffset? GetTransformOriginOffset(Length dim, bool horizontal)
        {
            TransformOriginOffset? offset = null;

            if (Mathf.Approximately(dim.value, 0))
            {
                offset = horizontal ? TransformOriginOffset.Left : TransformOriginOffset.Top;
            }
            else if (dim.unit == LengthUnit.Percent)
            {
                if (Mathf.Approximately(dim.value, 50))
                {
                    offset = TransformOriginOffset.Center;
                }
                else if (Mathf.Approximately(dim.value, 100))
                {
                    offset = horizontal ? TransformOriginOffset.Right : TransformOriginOffset.Bottom;
                }
            }

            return offset;
        }
    }
}
