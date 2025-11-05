// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements.Layout;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    internal partial class StyleProperty
    {
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal readonly struct Manipulator
        {
            private readonly ref struct ValueSpan
            {
                public ValueSpan(int start, int length)
                {
                    this.start = start;
                    this.length = length;
                }

                public readonly int start;
                public readonly int length;
            }

            private readonly StyleSheet m_StyleSheet;
            private readonly StyleProperty m_Property;

            internal Manipulator(StyleSheet styleSheet, StyleProperty property)
            {
                m_StyleSheet = styleSheet;
                m_Property = property;
            }

            /// <summary>
            /// Returns the number of different values contained in this property.
            /// </summary>
            /// <returns>The value count.</returns>
            /// <remarks>
            /// A variable reference and its argument(s) will only count as 1 value.
            /// Comma separator are omitted from the value count.
            /// </remarks>
            public int GetValueCount()
            {
                using var _ = ListPool<int>.Get(out var indices);
                StyleSheetUtility.GetValueOffsets(m_StyleSheet, m_Property.values, indices);
                return indices.Count;
            }

            public void AddKeyword(StyleValueKeyword value)
            {
                var handle = default(StyleValueHandle);
                m_StyleSheet.WriteKeyword(ref handle, value);
                AddHandle(handle);
            }

            public void SetKeyword(int index, StyleValueKeyword value)
            {
                var span = GetValueSpan(index);
                ResizeValue(ref span, 1);
                m_StyleSheet.WriteKeyword(ref m_Property.values[span.start], value);
            }

            public void InsertKeyword(int index, StyleValueKeyword value)
            {
                var span = GetValueSpan(index, true);
                Insert(m_Property, span.start, 1);
                m_StyleSheet.WriteKeyword(ref m_Property.values[span.start], value);
            }

            public bool TryGetKeyword(int index, out StyleValueKeyword value)
            {
                var span = GetValueSpan(index);
                if (span.length != 1)
                {
                    value = default;
                    return false;
                }

                return m_StyleSheet.TryReadKeyword(m_Property.values[span.start], out value);
            }

            public void AddFloat(float value)
            {
                var handle = default(StyleValueHandle);
                m_StyleSheet.WriteFloat(ref handle, value);
                AddHandle(handle);
            }

            public void SetFloat(int index, float value)
            {
                var span = GetValueSpan(index);
                ResizeValue(ref span, 1);
                m_StyleSheet.WriteFloat(ref m_Property.values[span.start], value);
            }

            public void InsertFloat(int index, float value)
            {
                var span = GetValueSpan(index, true);
                Insert(m_Property, span.start, 1);
                m_StyleSheet.WriteFloat(ref m_Property.values[span.start], value);
            }

            public bool TryGetFloat(int index, out float value)
            {
                var span = GetValueSpan(index);
                if (span.length != 1)
                {
                    value = 0.0f;
                    return false;
                }

                return m_StyleSheet.TryReadFloat(m_Property.values[span.start], out value);
            }

            public void AddDimension(Dimension value)
            {
                var handle = default(StyleValueHandle);
                m_StyleSheet.WriteDimension(ref handle, value);
                AddHandle(handle);
            }

            public void SetDimension(int index, Dimension value)
            {
                var span = GetValueSpan(index);
                ResizeValue(ref span, 1);
                m_StyleSheet.WriteDimension(ref m_Property.values[span.start], value);
            }

            public void InsertDimension(int index, Dimension value)
            {
                var span = GetValueSpan(index, true);
                Insert(m_Property, span.start, 1);
                m_StyleSheet.WriteDimension(ref m_Property.values[span.start], value);
            }

            public bool TryGetDimension(int index, out Dimension value)
            {
                var span = GetValueSpan(index);
                if (span.length != 1)
                {
                    value = default;
                    return false;
                }

                return m_StyleSheet.TryReadDimension(m_Property.values[span.start], out value);
            }

            public void AddColor(Color value)
            {
                var handle = default(StyleValueHandle);
                m_StyleSheet.WriteColor(ref handle, value);
                AddHandle(handle);
            }

            public void SetColor(int index, Color value)
            {
                var span = GetValueSpan(index);
                ResizeValue(ref span, 1);
                m_StyleSheet.WriteColor(ref m_Property.values[span.start], value);
            }

            public void InsertColor(int index, Color value)
            {
                var span = GetValueSpan(index, true);
                Insert(m_Property, span.start, 1);
                m_StyleSheet.WriteColor(ref m_Property.values[span.start], value);
            }

            public bool TryGetColor(int index, out Color value)
            {
                var span = GetValueSpan(index);
                if (span.length != 1)
                {
                    value = default;
                    return false;
                }

                return m_StyleSheet.TryReadColor(m_Property.values[span.start], out value);
            }

            public void AddString(string value)
            {
                var handle = default(StyleValueHandle);
                m_StyleSheet.WriteString(ref handle, value);
                AddHandle(handle);
            }

            public void SetString(int index, string value)
            {
                var span = GetValueSpan(index);
                ResizeValue(ref span, 1);
                m_StyleSheet.WriteString(ref m_Property.values[span.start], value);
            }

            public void InsertString(int index, string value)
            {
                var span = GetValueSpan(index, true);
                Insert(m_Property, span.start, 1);
                m_StyleSheet.WriteString(ref m_Property.values[span.start], value);
            }

            public bool TryGetString(int index, out string value)
            {
                var span = GetValueSpan(index);
                if (span.length != 1)
                {
                    value = null;
                    return false;
                }

                return m_StyleSheet.TryReadString(m_Property.values[span.start], out value);
            }

            public void AddEnum(Enum value)
            {
                var handle = default(StyleValueHandle);
                m_StyleSheet.WriteEnum(ref handle, value);
                AddHandle(handle);
            }

            public void AddEnum<TEnum>(TEnum value)
                where TEnum: struct, Enum
            {
                var handle = default(StyleValueHandle);
                m_StyleSheet.WriteEnum(ref handle, value);
                AddHandle(handle);
            }

            public void AddEnum(string value)
            {
                var handle = default(StyleValueHandle);
                m_StyleSheet.WriteEnumAsString(ref handle, value);
                AddHandle(handle);
            }

            public void SetEnum(int index, Enum value)
            {
                var span = GetValueSpan(index);
                ResizeValue(ref span, 1);
                m_StyleSheet.WriteEnum(ref m_Property.values[span.start], value);
            }

            public void SetEnum<TEnum>(int index, TEnum value)
                where TEnum: struct, Enum
            {
                var span = GetValueSpan(index);
                ResizeValue(ref span, 1);
                m_StyleSheet.WriteEnum(ref m_Property.values[span.start], value);
            }

            public void SetEnum(int index, string value)
            {
                var span = GetValueSpan(index);
                ResizeValue(ref span, 1);
                m_StyleSheet.WriteEnumAsString(ref m_Property.values[span.start], value);
            }

            public void InsertEnum(int index, Enum value)
            {
                var span = GetValueSpan(index, true);
                Insert(m_Property, span.start, 1);
                m_StyleSheet.WriteEnum(ref m_Property.values[span.start], value);
            }

            public void InsertEnum<TEnum>(int index, TEnum value)
                where TEnum: struct, Enum
            {
                var span = GetValueSpan(index, true);
                Insert(m_Property, span.start, 1);
                m_StyleSheet.WriteEnum(ref m_Property.values[span.start], value);
            }

            public void InsertEnum(int index, string value)
            {
                var span = GetValueSpan(index, true);
                Insert(m_Property, span.start, 1);
                m_StyleSheet.WriteEnumAsString(ref m_Property.values[span.start], value);
            }

            public bool TryGetEnum<TEnum>(int index, out TEnum value)
                where TEnum: struct, Enum
            {
                var span = GetValueSpan(index);
                if (span.length != 1)
                {
                    value = default;
                    return false;
                }

                return m_StyleSheet.TryReadEnum(m_Property.values[span.start], out value);
            }

            public bool TryGetEnum(int index, out string value)
            {
                var span = GetValueSpan(index);
                if (span.length != 1)
                {
                    value = null;
                    return false;
                }

                return m_StyleSheet.TryReadEnum(m_Property.values[span.start], out value);
            }

            public void AddVariableReference(string value)
            {
                var start = m_Property.values.Length;
                Insert(m_Property, m_Property.values.Length, 3);
                m_StyleSheet.WriteFunction(ref m_Property.values[start], StyleValueFunction.Var);
                m_StyleSheet.WriteFloat(ref m_Property.values[start+1], 1);
                m_StyleSheet.WriteVariable(ref m_Property.values[start+2], value);
                m_Property.requireVariableResolve = true;
            }

            public void SetVariableReference(int index, string value)
            {
                var span = GetValueSpan(index);
                ResizeValue(ref span, 3);
                m_StyleSheet.WriteFunction(ref m_Property.values[span.start], StyleValueFunction.Var);
                m_StyleSheet.WriteFloat(ref m_Property.values[span.start+1], 1);
                m_StyleSheet.WriteVariable(ref m_Property.values[span.start+2], value);
                m_Property.requireVariableResolve = true;
            }

            public void InsertVariableReference(int index, string value)
            {
                var span = GetValueSpan(index, true);
                Insert(m_Property, span.start, 3);
                m_StyleSheet.WriteFunction(ref m_Property.values[span.start], StyleValueFunction.Var);
                m_StyleSheet.WriteFloat(ref m_Property.values[span.start+1], 1);
                m_StyleSheet.WriteVariable(ref m_Property.values[span.start+2], value);
                m_Property.requireVariableResolve = true;
            }

            public bool TryGetVariableReference(int index, out string value)
            {
                var span = GetValueSpan(index);
                if (span.length >= 3 &&
                    m_Property.values[span.start].valueType == StyleValueType.Function &&
                    m_Property.values[span.start+1].valueType == StyleValueType.Float &&
                    m_Property.values[span.start+2].valueType == StyleValueType.Variable)
                {
                    value = null;
                    return false;
                }

                return m_StyleSheet.TryReadVariable(m_Property.values[span.start+2], out value);
            }

            public void AddResourcePath(ResolvedResourcePath resolvedResourcePath)
            {
                var handle = default(StyleValueHandle);
                m_StyleSheet.WriteResourcePath(ref handle, resolvedResourcePath);
                AddHandle(handle);
            }

            public void SetResourcePath(int index, ResolvedResourcePath resolvedResourcePath)
            {
                var span = GetValueSpan(index);
                ResizeValue(ref span, 1);
                m_StyleSheet.WriteResourcePath(ref m_Property.values[span.start], resolvedResourcePath);
            }

            public void InsertResourcePath(int index, ResolvedResourcePath resolvedResourcePath)
            {
                var span = GetValueSpan(index, true);
                Insert(m_Property, span.start, 1);
                m_StyleSheet.WriteResourcePath(ref m_Property.values[span.start], resolvedResourcePath);
            }

            public bool TryGetResourcePath(int index, out string path, out string subAssetName)
            {
                path = null;
                subAssetName = null;

                var span = GetValueSpan(index);
                if (span.length != 1)
                {
                    return false;
                }

                if (m_StyleSheet.TryReadResourcePath(m_Property.values[span.start], out var resourcePath))
                {
                    path = resourcePath.path;
                    subAssetName = resourcePath.subAssetName;
                    return true;
                }
                return false;
            }

            public void AddAssetReference(Object value)
            {
                var handle = default(StyleValueHandle);
                m_StyleSheet.WriteAssetReference(ref handle, value);
                AddHandle(handle);
            }

            public void SetAssetReference(int index, Object value)
            {
                var span = GetValueSpan(index);
                ResizeValue(ref span, 1);
                m_StyleSheet.WriteAssetReference(ref m_Property.values[span.start], value);
            }

            public void InsertAssetReference(int index, Object value)
            {
                var span = GetValueSpan(index, true);
                Insert(m_Property, span.start, 1);
                m_StyleSheet.WriteAssetReference(ref m_Property.values[span.start], value);
            }

            public bool TryGetAssetReference(int index, out Object value)
            {
                var span = GetValueSpan(index);
                if (span.length != 1)
                {
                    value = null;
                    return false;
                }

                return m_StyleSheet.TryReadAssetReference(m_Property.values[span.start], out value);
            }

            public bool TryGetAssetReference<TObject>(int index, out TObject value)
                where TObject : Object
            {
                var span = GetValueSpan(index);
                if (span.length != 1)
                {
                    value = null;
                    return false;
                }

                if (m_StyleSheet.TryReadAssetReference(m_Property.values[span.start], out var objectValue) &&
                    objectValue is TObject typedValue)
                {
                    value = typedValue;
                    return true;
                }

                value = null;
                return false;
            }

            public void AddMissingAssetReferenceUrl(string value)
            {
                var handle = default(StyleValueHandle);
                m_StyleSheet.WriteMissingAssetReferenceUrl(ref handle, value);
                AddHandle(handle);
            }

            public void SetMissingAssetReferenceUrl(int index, string value)
            {
                var span = GetValueSpan(index);
                ResizeValue(ref span, 1);
                m_StyleSheet.WriteMissingAssetReferenceUrl(ref m_Property.values[span.start], value);
            }

            public void InsertMissingAssetReferenceUrl(int index, string value)
            {
                var span = GetValueSpan(index, true);
                Insert(m_Property, span.start, 1);
                m_StyleSheet.WriteMissingAssetReferenceUrl(ref m_Property.values[span.start], value);
            }

            public bool TryGetMissingAssetReferenceUrl(int index, out string value)
            {
                var span = GetValueSpan(index);
                if (span.length != 1)
                {
                    value = null;
                    return false;
                }

                return m_StyleSheet.TryReadMissingAssetReferenceUrl(m_Property.values[span.start], out value);
            }

            public void AddScalableImage(ScalableImage value)
            {
                var handle = default(StyleValueHandle);
                m_StyleSheet.WriteScalableImage(ref handle, value);
                AddHandle(handle);
            }

            public void SetScalableImage(int index, ScalableImage value)
            {
                var span = GetValueSpan(index);
                ResizeValue(ref span, 1);
                m_StyleSheet.WriteScalableImage(ref m_Property.values[span.start], value);
            }

            public void InsertScalableImage(int index, ScalableImage value)
            {
                var span = GetValueSpan(index, true);
                Insert(m_Property, span.start, 1);
                m_StyleSheet.WriteScalableImage(ref m_Property.values[span.start], value);
            }

            public bool TryGetScalableImage(int index, out ScalableImage value)
            {
                var span = GetValueSpan(index);
                if (span.length != 1)
                {
                    value = default;
                    return false;
                }

                return m_StyleSheet.TryReadScalableImage(m_Property.values[span.start], out value);
            }

            public void AddAngle(Angle value)
            {
                var handle = default(StyleValueHandle);
                m_StyleSheet.WriteAngle(ref handle, value);
                AddHandle(handle);
            }

            public void SetAngle(int index, Angle value)
            {
                var span = GetValueSpan(index);
                ResizeValue(ref span, 1);
                m_StyleSheet.WriteAngle(ref m_Property.values[span.start], value);
            }

            public void InsertAngle(int index, Angle value)
            {
                var span = GetValueSpan(index, true);
                Insert(m_Property, span.start, 1);
                m_StyleSheet.WriteAngle(ref m_Property.values[span.start], value);
            }

            public bool TryGetAngle(int index, out Angle value)
            {
                var span = GetValueSpan(index);
                if (span.length != 1)
                {
                    value = default;
                    return false;
                }

                return m_StyleSheet.TryReadAngle(m_Property.values[span.start], out value);
            }

            public void AddKeyword(StyleKeyword value)
            {
                var handle = default(StyleValueHandle);
                m_StyleSheet.WriteKeyword(ref handle, value.ToStyleValueKeyword());
                AddHandle(handle);
            }

            public void SetKeyword(int index, StyleKeyword value)
            {
                var span = GetValueSpan(index);
                ResizeValue(ref span, 1);
                m_StyleSheet.WriteKeyword(ref m_Property.values[span.start], value.ToStyleValueKeyword());
            }

            public void InsertKeyword(int index, StyleKeyword value)
            {
                var span = GetValueSpan(index, true);
                Insert(m_Property, span.start, 1);
                m_StyleSheet.WriteKeyword(ref m_Property.values[span.start], value.ToStyleValueKeyword());
            }

            public bool TryGetKeyword(int index, out StyleKeyword value)
            {
                var span = GetValueSpan(index);
                if (span.length != 1)
                {
                    value = default;
                    return false;
                }

                return TryReadKeyword(m_StyleSheet, ref m_Property.values[span.start], out value);
            }

            public void AddInt(int value)
            {
                AddFloat(value);
            }

            public void SetInt(int index, int value)
            {
                SetFloat(index, value);
            }

            public void InsertInt(int index, int value)
            {
                InsertFloat(index, value);
            }

            public bool TryGetInt(int index, out int value)
            {
                if (TryGetFloat(index, out var floatValue))
                {
                    value = (int)floatValue;
                    return true;
                }

                value = 0;
                return false;
            }

            public void AddLength(Length value)
            {
                var handle = default(StyleValueHandle);
                m_StyleSheet.WriteLength(ref handle, value);
                AddHandle(handle);
            }

            public void SetLength(int index, Length value)
            {
                var span = GetValueSpan(index);
                ResizeValue(ref span, 1);
                m_StyleSheet.WriteLength(ref m_Property.values[span.start], value);
            }

            public void InsertLength(int index, Length value)
            {
                var span = GetValueSpan(index, true);
                Insert(m_Property, span.start, 1);
                m_StyleSheet.WriteLength(ref m_Property.values[span.start], value);
            }

            public bool TryGetLength(int index, out Length value)
            {
                var span = GetValueSpan(index);
                if (span.length != 1)
                {
                    value = default;
                    return false;
                }

                return m_StyleSheet.TryReadLength(m_Property.values[span.start], out value);
            }

            public void AddTimeValue(TimeValue value)
            {
                var handle = default(StyleValueHandle);
                m_StyleSheet.WriteTimeValue(ref handle, value);
                AddHandle(handle);
            }

            public void SetTimeValue(int index, TimeValue value)
            {
                var span = GetValueSpan(index);
                ResizeValue(ref span, 1);
                m_StyleSheet.WriteTimeValue(ref m_Property.values[span.start], value);
            }

            public void InsertTimeValue(int index, TimeValue value)
            {
                var span = GetValueSpan(index, true);
                Insert(m_Property, span.start, 1);
                m_StyleSheet.WriteTimeValue(ref m_Property.values[span.start], value);
            }

            public bool TryGetTimeValue(int index, out TimeValue value)
            {
                var span = GetValueSpan(index);
                if (span.length != 1)
                {
                    value = default;
                    return false;
                }

                return m_StyleSheet.TryReadTimeValue(m_Property.values[span.start], out value);
            }

            public void AddStylePropertyName(StylePropertyName value)
            {
                var handle = default(StyleValueHandle);
                m_StyleSheet.WriteStylePropertyName(ref handle, value);
                AddHandle(handle);
            }

            public void SetStylePropertyName(int index, StylePropertyName value)
            {
                var span = GetValueSpan(index);
                ResizeValue(ref span, 1);
                m_StyleSheet.WriteStylePropertyName(ref m_Property.values[span.start], value);
            }

            public void InsertStylePropertyName(int index, StylePropertyName value)
            {
                var span = GetValueSpan(index, true);
                Insert(m_Property, span.start, 1);
                m_StyleSheet.WriteStylePropertyName(ref m_Property.values[span.start], value);
            }

            public bool TryGetStylePropertyName(int index, out StylePropertyName value)
            {
                var span = GetValueSpan(index);
                if (span.length != 1)
                {
                    value = default;
                    return false;
                }

                return m_StyleSheet.TryReadStylePropertyName(m_Property.values[span.start], out value);
            }

            public void AddEasingFunction(EasingFunction value)
            {
                var handle = default(StyleValueHandle);
                m_StyleSheet.WriteEnum(ref handle, value.mode);
                AddHandle(handle);
            }

            public void SetEasingFunction(int index, EasingFunction value)
            {
                var span = GetValueSpan(index);
                ResizeValue(ref span, 1);
                m_StyleSheet.WriteEnum(ref m_Property.values[span.start], value.mode);
            }

            public void InsertEasingFunction(int index, EasingFunction value)
            {
                var span = GetValueSpan(index, true);
                Insert(m_Property, span.start, 1);
                m_StyleSheet.WriteEnum(ref m_Property.values[span.start], value.mode);
            }

            public bool TryGetEasingFunction(int index, out EasingFunction value)
            {
                var span = GetValueSpan(index);
                if (span.length != 1)
                {
                    value = default;
                    return false;
                }

                if (m_StyleSheet.TryReadEnum(m_Property.values[span.start], out EasingMode easingMode))
                {
                    value = new EasingFunction(easingMode);
                    return true;
                }

                value = default;
                return false;
            }

            public void AddFilterFunction(FilterFunction value)
            {
                var hasCustomDefinition = value.type == FilterFunctionType.Custom && value.customDefinition;
                var parameterCount = value.parameterCount + (hasCustomDefinition ? 1 : 0);

                var handleCount = parameterCount + 2 /* Function + Args Count*/;
                var start = m_Property.values.Length;

                Insert(m_Property, m_Property.values.Length, handleCount);
                m_StyleSheet.WriteFunction(ref m_Property.values[start], ToStyleValueFunction(value.type));
                m_StyleSheet.WriteFloat(ref m_Property.values[start+1], parameterCount);

                var nextIndex = start + 2;
                if (hasCustomDefinition)
                {
                    m_StyleSheet.WriteAssetReference(ref m_Property.values[nextIndex], value.customDefinition);
                    ++nextIndex;
                }

                for (var i = 0; i < value.parameterCount; ++i, ++nextIndex)
                {
                    var p = value.GetParameter(i);
                    switch (p.type)
                    {
                        case FilterParameterType.Float:
                            m_StyleSheet.WriteFloat(ref m_Property.values[nextIndex], p.floatValue);
                            break;
                        case FilterParameterType.Color:
                            m_StyleSheet.WriteColor(ref m_Property.values[nextIndex], p.colorValue);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            public void SetFilterFunction(int index, FilterFunction value)
            {
                var hasCustomDefinition = value.type == FilterFunctionType.Custom && value.customDefinition;
                var parameterCount = value.parameterCount + (hasCustomDefinition ? 1 : 0);

                var handleCount = parameterCount + 2 /* Function + Args Count*/;
                var span = GetValueSpan(index);
                ResizeValue(ref span, handleCount);

                m_StyleSheet.WriteFunction(ref m_Property.values[span.start], ToStyleValueFunction(value.type));
                m_StyleSheet.WriteFloat(ref m_Property.values[span.start+1], parameterCount);

                var nextIndex = span.start + 2;
                if (hasCustomDefinition)
                {
                    m_StyleSheet.WriteAssetReference(ref m_Property.values[nextIndex], value.customDefinition);
                    ++nextIndex;
                }

                for (var i = 0; i < value.parameterCount; ++i, ++nextIndex)
                {
                    var p = value.GetParameter(i);
                    switch (p.type)
                    {
                        case FilterParameterType.Float:
                            m_StyleSheet.WriteFloat(ref m_Property.values[nextIndex], p.floatValue);
                            break;
                        case FilterParameterType.Color:
                            m_StyleSheet.WriteColor(ref m_Property.values[nextIndex], p.colorValue);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            public void InsertFilterFunction(int index, FilterFunction value)
            {
                var hasCustomDefinition = value.type == FilterFunctionType.Custom && value.customDefinition;
                var parameterCount = value.parameterCount + (hasCustomDefinition ? 1 : 0);

                var handleCount = parameterCount + 2 /* Function + Args Count*/;
                var span = GetValueSpan(index, true);

                Insert(m_Property, span.start, handleCount);
                m_StyleSheet.WriteFunction(ref m_Property.values[span.start], ToStyleValueFunction(value.type));
                m_StyleSheet.WriteFloat(ref m_Property.values[span.start+1], parameterCount);

                var nextIndex = span.start + 2;
                if (hasCustomDefinition)
                {
                    m_StyleSheet.WriteAssetReference(ref m_Property.values[nextIndex], value.customDefinition);
                    ++nextIndex;
                }

                for (var i = 0; i < value.parameterCount; ++i, ++nextIndex)
                {
                    var p = value.GetParameter(i);
                    switch (p.type)
                    {
                        case FilterParameterType.Float:
                            m_StyleSheet.WriteFloat(ref m_Property.values[nextIndex], p.floatValue);
                            break;
                        case FilterParameterType.Color:
                            m_StyleSheet.WriteColor(ref m_Property.values[nextIndex], p.colorValue);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            public bool TryGetFilterFunction(int index, out FilterFunction value)
            {
                var span = GetValueSpan(index);

                // Requires at least Function + ArgsCount
                if (span.length <= 1)
                {
                    value = default;
                    return false;
                }

                if (!m_StyleSheet.TryReadFunction(m_Property.values[span.start], out var function) ||
                    !m_StyleSheet.TryReadFloat(m_Property.values[span.start + 1], out var argsCount))
                {
                    value = default;
                    return false;
                }

                var parameterCount = (int)argsCount;
                var parameters = new FixedBuffer4<FilterParameter>();
                var currentIndex = span.start + 2;

                var filterFunctionType = ToFilterFunctionType(function);
                var filterFunctionDefinition = default(FilterFunctionDefinition);
                var isCustom = false;
                if (filterFunctionType == FilterFunctionType.Custom && parameterCount > 0)
                {
                    if (m_StyleSheet.TryReadAssetReference(m_Property.values[currentIndex], out var reference))
                    {
                        filterFunctionDefinition = (FilterFunctionDefinition)reference;
                        isCustom = true;
                    }
                    else if (m_StyleSheet.TryReadResourcePath(m_Property.values[currentIndex], out var resourcePath))
                    {
                        filterFunctionDefinition = resourcePath.LoadResource<FilterFunctionDefinition>(1.0f);
                        isCustom = true;
                    }
                    --parameterCount;
                }

                for (var i = 0; i < parameterCount; ++i, ++currentIndex)
                {
                    if (m_StyleSheet.TryReadColor(m_Property.values[currentIndex], out var color))
                    {
                        parameters[i] = new FilterParameter
                        {
                            type = FilterParameterType.Color,
                            colorValue = color
                        };
                    }
                    else if (m_StyleSheet.TryReadFloat(m_Property.values[currentIndex], out var dimValue))
                    {
                        parameters[i] = new FilterParameter()
                        {
                            type = FilterParameterType.Float,
                            floatValue = dimValue
                        };
                    }
                    else if (m_Property.values[currentIndex].valueType == StyleValueType.CommaSeparator)
                    {
                        // Not technically a valid syntax, but we'll allow it
                        continue;
                    }
                    else
                    {
                        Debug.LogError($"Unexpected value type {m_Property.values[currentIndex].valueType} in filter function argument");
                    }
                }

                value = isCustom
                    ? new FilterFunction(filterFunctionDefinition, parameters, parameterCount)
                    : new FilterFunction(filterFunctionType, parameters, parameterCount);
                return true;
            }

            void WriteMaterialPropertyValue(MaterialPropertyValue value, int index)
            {
                switch (value.type)
                {
                    case MaterialPropertyValueType.Float:
                        m_StyleSheet.WriteFloat(ref m_Property.values[index++], value.GetFloat());
                        break;
                    case MaterialPropertyValueType.Vector:
                        var v = value.GetVector();
                        m_StyleSheet.WriteFloat(ref m_Property.values[index++], v.x);
                        m_StyleSheet.WriteFloat(ref m_Property.values[index++], v.y);
                        m_StyleSheet.WriteFloat(ref m_Property.values[index++], v.z);
                        m_StyleSheet.WriteFloat(ref m_Property.values[index++], v.w);
                        break;
                    case MaterialPropertyValueType.Color:
                        m_StyleSheet.WriteColor(ref m_Property.values[index++], value.GetColor());
                        break;
                    case MaterialPropertyValueType.Texture:
                        m_StyleSheet.WriteAssetReference(ref m_Property.values[index++], value.textureValue);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            public bool TryGetMaterialPropertyValue(int index, out MaterialPropertyValue value)
            {
                value = default;

                // Get the span for the requested value (skipping material ref at index 0)
                var span = GetValueSpan(index + 1);
                if (span.length < 4) // Function + ArgCount + Name + at least 1 value
                    return false;

                // Check that the function is MaterialProperty
                if (!m_StyleSheet.TryReadFunction(m_Property.values[span.start], out var function) ||
                    function != StyleValueFunction.MaterialProperty)
                    return false;

                // Read argument count
                if (!m_StyleSheet.TryReadFloat(m_Property.values[span.start + 1], out var argCountf))
                    return false;
                int argCount = (int)argCountf;

                // Read property name
                if (!m_StyleSheet.TryReadString(m_Property.values[span.start + 2], out var propertyName))
                    return false;

                int valueStart = span.start + 3;
                int valueCount = argCount - 1; // 1 for name

                // Try to read float
                if (valueCount == 1 && m_StyleSheet.TryReadFloat(m_Property.values[valueStart], out var floatValue))
                {
                    value = new MaterialPropertyValue
                    {
                        name = propertyName,
                        type = MaterialPropertyValueType.Float,
                        packedValue = new Vector4(floatValue, 0, 0, 0)
                    };
                    return true;
                }
                // Try to read color
                else if (valueCount == 1 && m_StyleSheet.TryReadColor(m_Property.values[valueStart], out var colorValue))
                {
                    value = new MaterialPropertyValue
                    {
                        name = propertyName,
                        type = MaterialPropertyValueType.Color,
                        packedValue = new Vector4(colorValue.r, colorValue.g, colorValue.b, colorValue.a)
                    };
                    return true;
                }
                // Try to read vector
                else if (valueCount == 4 &&
                    m_StyleSheet.TryReadFloat(m_Property.values[valueStart], out var x) &&
                    m_StyleSheet.TryReadFloat(m_Property.values[valueStart + 1], out var y) &&
                    m_StyleSheet.TryReadFloat(m_Property.values[valueStart + 2], out var z) &&
                    m_StyleSheet.TryReadFloat(m_Property.values[valueStart + 3], out var w))
                {
                    value = new MaterialPropertyValue
                    {
                        name = propertyName,
                        type = MaterialPropertyValueType.Vector,
                        packedValue = new Vector4(x, y, z, w)

                    };
                    // value.SetVector(new Vector4(x, y, z, w)); // If available
                    return true;
                }
                // Try to read texture
                else if (valueCount == 1 && m_StyleSheet.TryReadAssetReference(m_Property.values[valueStart], out var textureObj))
                {
                    value = new MaterialPropertyValue
                    {
                        name = propertyName,
                        type = MaterialPropertyValueType.Texture,
                        textureValue = textureObj as Texture
                    };
                    return true;
                }

                return false;
            }

            public void AddMaterialPropertyValue(MaterialPropertyValue value)
            {
                var start = m_Property.values.Length;
                int argCount = 1 + ArgumentCountForMaterialPropertyValueType(value.type); // Name + Args

                Insert(m_Property, m_Property.values.Length, 2 + argCount); // Function + Arg count + Args

                m_StyleSheet.WriteFunction(ref m_Property.values[start], StyleValueFunction.MaterialProperty);
                m_StyleSheet.WriteFloat(ref m_Property.values[start + 1], argCount);
                m_StyleSheet.WriteString(ref m_Property.values[start + 2], value.name);

                WriteMaterialPropertyValue(value, start + 3);
            }

            public void SetMaterialPropertyValue(int index, MaterialPropertyValue value)
            {
                var argCount = ArgumentCountForMaterialPropertyValueType(value.type) + 1; // Name + Args
                var handleCount = 2 + argCount; // Function + Args count + Args
                var span = GetValueSpan(index + 1); // Skip material ref
                ResizeValue(ref span, handleCount);

                m_StyleSheet.WriteFunction(ref m_Property.values[span.start], StyleValueFunction.MaterialProperty);
                m_StyleSheet.WriteFloat(ref m_Property.values[span.start + 1], argCount);
                m_StyleSheet.WriteString(ref m_Property.values[span.start + 2], value.name);

                WriteMaterialPropertyValue(value, span.start + 3);
            }

            public void InsertMaterialPropertyValue(int index, MaterialPropertyValue value)
            {
                var argCount = ArgumentCountForMaterialPropertyValueType(value.type) + 1; // Name + Args
                var handleCount = 2 + argCount; // Function + Args count + Args
                var span = GetValueSpan(index + 1, true); // Skip material ref
                Insert(m_Property, span.start, handleCount);

                m_StyleSheet.WriteFunction(ref m_Property.values[span.start], StyleValueFunction.MaterialProperty);
                m_StyleSheet.WriteFloat(ref m_Property.values[span.start + 1], argCount);
                m_StyleSheet.WriteString(ref m_Property.values[span.start + 2], value.name);

                WriteMaterialPropertyValue(value, span.start + 3);
            }

            public void AddCommaSeparator()
            {
                var handle = default(StyleValueHandle);
                m_StyleSheet.WriteCommaSeparator(ref handle);
                AddHandle(handle);
            }

            public void RemoveValue(int index)
            {
                var span = GetValueSpan(index);
                var start = span.start;
                var length = span.length;
                // Followed by comma
                if (IsCommaSeparator(m_Property.values, span.start + span.length))
                {
                    length += 1;
                }
                // Preceded by comma
                else if (IsCommaSeparator(m_Property.values, span.start - 1))
                {
                    start -= 1;
                    length += 1;
                }

                Remove(m_Property, start, length);
            }

            private ValueSpan GetValueSpan(int index, bool insertMode = false)
            {
                using var _ = ListPool<int>.Get(out var indices);
                StyleSheetUtility.GetValueOffsets(m_StyleSheet, m_Property.values, indices);
                if (index < 0 || index > indices.Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                if (index == indices.Count)
                {
                    if (insertMode)
                        return new ValueSpan(m_Property.values.Length, 0);
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                var offset = indices[index];
                var next = index == indices.Count - 1 ? m_Property.values.Length : indices[index + 1];
                var isComma = next < m_Property.values.Length && m_Property.values[next - 1].valueType == StyleValueType.CommaSeparator;
                var commaOffset = isComma ? 1 : 0;
                return new ValueSpan(offset, next - offset - commaOffset);
            }

            private void ResizeValue(ref ValueSpan span, int count)
            {
                // Nothing to do
                if (span.length == count)
                    return;

                // Increase size
                if (count > span.length)
                {
                    var resize = count - span.length;
                    Insert(m_Property, span.start, resize);
                }
                // Reduce size
                else
                {
                    var resize = span.length - count;
                    Remove(m_Property, span.start, resize);
                }
            }

            private void AddHandle(StyleValueHandle handle)
            {
                Insert(m_Property, m_Property.values.Length, 1);
                m_Property.values[^1] = handle;
            }

            private static void Insert(StyleProperty property, int index, int count)
            {
                var current = property.values;
                property.values = new StyleValueHandle[current.Length + count];
                Array.Copy(current, 0, property.values, 0, index);
                Array.Copy(current, index, property.values, index + count, current.Length - index);
            }

            private static void Remove(StyleProperty property, int index, int count)
            {
                var current = property.values;
                property.values = new StyleValueHandle[current.Length - count];
                Array.Copy(current, 0, property.values, 0, index);
                Array.Copy(current, index + count, property.values, index, current.Length - (index + count));
            }

            private static bool IsCommaSeparator(StyleValueHandle[] array, int index)
            {
                if (index < 0 || index >= array.Length)
                    return false;
                return array[index].valueType == StyleValueType.CommaSeparator;
            }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal Manipulator GetManipulator(StyleSheet styleSheet)
        {
            return new Manipulator(styleSheet, this);
        }
    }
}

