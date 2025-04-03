// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    [Serializable]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class StyleProperty
    {
        [SerializeField]
        string m_Name;

        public string name
        {
            get
            {
                return m_Name;
            }
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            internal set
            {
                m_Name = value;
            }
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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        [NonSerialized]
        internal bool isCustomProperty;

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        [NonSerialized]
        internal bool requireVariableResolve;

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
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void SetKeyword(StyleSheet styleSheet, StyleValueKeyword value)
        {
            SetSize(ref m_Values, 1);
            styleSheet.WriteKeyword(ref m_Values[0], value);
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
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void SetDimension(StyleSheet styleSheet, Dimension value)
        {
            SetSize(ref m_Values, 1);
            styleSheet.WriteDimension(ref m_Values[0], value);
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
        /// <param name="value">The path.</param>
        public void SetResourcePath(StyleSheet styleSheet, string value)
        {
            SetSize(ref m_Values, 1);
            styleSheet.WriteResourcePath(ref m_Values[0], value);
        }

        /// <summary>
        /// Tries to read a resource path from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetResourcePath(StyleSheet styleSheet, out string value)
        {
            if (handleCount == 1)
                return styleSheet.TryReadResourcePath(m_Values[0], out value);

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
                return TryReadSetKeyword(styleSheet, ref m_Values[0], out value);

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
                return;
            }

            SetSize(ref m_Values, 2);
            styleSheet.WriteEnum(ref values[0], value.keyword);
            styleSheet.WriteDimension(ref values[1], value.offset.ToDimension());
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
                return;
            }

            if (value.axis == Vector3.forward)
            {
                SetSize(ref m_Values, 1);
                styleSheet.WriteAngle(ref values[0], value.angle);
                return;
            }

            SetSize(ref m_Values, 4);
            var axis = value.axis;
            styleSheet.WriteFloat(ref values[0], axis.x);
            styleSheet.WriteFloat(ref values[1], axis.y);
            styleSheet.WriteFloat(ref values[2], axis.z);
            styleSheet.WriteAngle(ref values[3], value.angle);
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
                return;
            }

            if (Mathf.Approximately(value.value.z, 1.0f))
            {
                SetSize(ref m_Values, 2);
                styleSheet.WriteFloat(ref values[0], value.value.x);
                styleSheet.WriteFloat(ref values[1], value.value.y);
                return;
            }

            SetSize(ref m_Values, 3);
            styleSheet.WriteFloat(ref values[0], value.value.x);
            styleSheet.WriteFloat(ref values[1], value.value.y);
            styleSheet.WriteFloat(ref values[2], value.value.z);
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
                    return;
                }

                if (xOffset.HasValue && yOffset.HasValue && xOffset.Value == TransformOriginOffset.Center)
                {
                    SetSize(ref m_Values, 1);
                    styleSheet.WriteEnum(ref m_Values[0], yOffset.Value);
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
        /// Sets a <see cref="List{TimeValue}"/> as the current value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The value to store.</param>
        /// <remarks>
        /// </remarks>
        public void SetTimeValue(StyleSheet styleSheet, List<TimeValue> value)
        {
            SetSize(ref m_Values, value.Count * 2 - 1);
            for (var i = 0; i < value.Count; ++i)
            {
                var handleIndex = i * 2;
                styleSheet.WriteDimension(ref values[handleIndex], value[i].ToDimension());
                if (i < value.Count - 1)
                    styleSheet.WriteCommaSeparator(ref values[handleIndex + 1]);
            }
        }

        /// <summary>
        /// Tries to read a <see cref="List{TimeValue}"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetTimeValue(StyleSheet styleSheet, out List<TimeValue> value)
        {
            if (ContainsVariable())
            {
                value = null;
                return false;
            }

            value = new List<TimeValue>();
            return TryGetTimeValue(styleSheet, value);
        }

        /// <summary>
        /// Tries to read a <see cref="List{TimeValue}"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetTimeValue(StyleSheet styleSheet, List<TimeValue> value)
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
        /// <remarks>
        /// </remarks>
        public void SetStylePropertyName(StyleSheet styleSheet, List<StylePropertyName> value)
        {
            SetSize(ref m_Values, value.Count * 2 - 1);
            for (var i = 0; i < value.Count; ++i)
            {
                var handleIndex = i * 2;
                styleSheet.WriteStylePropertyName(ref values[handleIndex], value[i]);
                if (i < value.Count - 1)
                    styleSheet.WriteCommaSeparator(ref values[handleIndex + 1]);
            }
        }

        /// <summary>
        /// Tries to read a <see cref="List{StylePropertyName}"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetStylePropertyName(StyleSheet styleSheet, out List<StylePropertyName> value)
        {
            if (ContainsVariable())
            {
                value = null;
                return false;
            }

            value = new List<StylePropertyName>();
            return TryGetStylePropertyName(styleSheet, value);
        }

        /// <summary>
        /// Tries to read a <see cref="List{StylePropertyName}"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetStylePropertyName(StyleSheet styleSheet, List<StylePropertyName> value)
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
        /// <remarks>
        /// </remarks>
        public void SetEasingFunction(StyleSheet styleSheet, List<EasingFunction> value)
        {
            SetSize(ref m_Values, value.Count * 2 - 1);
            for (var i = 0; i < value.Count; ++i)
            {
                var handleIndex = i * 2;
                styleSheet.WriteEnum(ref values[handleIndex], value[i].mode);
                if (i < value.Count - 1)
                    styleSheet.WriteCommaSeparator(ref values[handleIndex + 1]);
            }
        }

        /// <summary>
        /// Tries to read a <see cref="List{EasingFunction}"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetEasingFunction(StyleSheet styleSheet, out List<EasingFunction> value)
        {
            if (ContainsVariable())
            {
                value = null;
                return false;
            }

            value = new List<EasingFunction>();
            return TryGetEasingFunction(styleSheet, value);
        }

        /// <summary>
        /// Tries to read a <see cref="List{EasingFunction}"/> from the <see cref="StyleProperty"/>'s value.
        /// </summary>
        /// <param name="styleSheet">The data store.</param>
        /// <param name="value">The read value.</param>
        /// <returns><see langword="true"/> if the value could be read; <see langword="false"/> otherwise.</returns>
        public bool TryGetEasingFunction(StyleSheet styleSheet, List<EasingFunction> value)
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

        private static void SetSize(ref StyleValueHandle[] store, int size)
        {
            if (store?.Length == size)
                return;
            store = new StyleValueHandle[size];
        }

        internal static bool TryReadSetKeyword(StyleSheet styleSheet, ref StyleValueHandle handle, out StyleKeyword value)
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
