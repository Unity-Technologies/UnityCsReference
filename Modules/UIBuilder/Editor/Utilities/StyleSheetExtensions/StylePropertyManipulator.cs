// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    /// <summary>
    /// Transient type detailing which values were actually computed for a given <see cref="StyleProperty"/>. When a
    /// <see cref="StyleProperty"/> is set on <see cref="StyleSheet"/>, this manipulator can be used to retrieve the
    /// computed values and their origin as well as modifying the said values. When setting an explicit value, variables
    /// will be removed automatically.
    /// </summary>
    /// <remarks>
    /// Any external changes to the related <see cref="StyleSheet"/>s' internal structure may invalidate this data.
    /// </remarks>
    class StylePropertyManipulator : IDisposable
    {
        /// <summary>
        /// Helper type to help setting a string as a variable in a generic context.
        /// </summary>
        readonly struct Variable
        {
            public readonly string name;

            public Variable(string name)
            {
                this.name = name;
            }
        }

        /// <summary>
        /// Helper type to map a linear value index into the internal representation indices.
        /// </summary>
        /// <remarks>
        /// When trying to resolve a variable, it may lead to multiple values instead of a single one, so we treat the
        /// definition and the resolved states separately. For example:
        ///                           0      1                     2      (Parts)
        ///      transition-duration: 100ms, var(--my-value-list), 500ms;
        ///  could be resolved as:
        ///                           0      1      2      3      4       (Values)
        ///      transition-duration, 100ms, 200ms, 300ms, 400ms, 500ms;
        ///
        /// In the example above, asking the value at index=3 would return { 1, 2 }, because the value maps to the third
        /// value of the second part.
        /// </remarks>>
        readonly ref struct Index
        {
            public readonly int index;
            public readonly int partIndex;
            public readonly int valueIndex;

            public Index(int index, int partIndex, int valueIndex)
            {
                this.index = index;
                this.partIndex = partIndex;
                this.valueIndex = valueIndex;
            }
        }

        internal struct StyleValueHandleContext
        {
            /// <summary>
            /// The stylesheet in which the <see cref="handle"/> lives.
            /// </summary>
            public StyleSheet styleSheet;

            /// <summary>
            /// Handle to the actual value inside the <see cref="styleSheet"/>
            /// </summary>
            public StyleValueHandle handle;

            public Dimension AsDimension()
            {
                if (handle.valueType == StyleValueType.Dimension)
                    return styleSheet.ReadDimension(handle);

                throw new InvalidCastException(
                    $"Cannot cast value of type `{handle.valueType}` into a `{StyleValueType.Dimension}`.");
            }

            public StyleValueKeyword AsKeyword()
            {
                if (handle.valueType == StyleValueType.Keyword)
                    return styleSheet.ReadKeyword(handle);

                throw new InvalidCastException(
                    $"Cannot cast value of type `{handle.valueType}` into a `{StyleValueType.Keyword}`.");
            }

            public float AsFloat()
            {
                if (handle.valueType == StyleValueType.Float)
                    return styleSheet.ReadFloat(handle);

                throw new InvalidCastException(
                    $"Cannot cast value of type `{handle.valueType}` into a `{StyleValueType.Float}`.");
            }

            public Color AsColor()
            {
                if (handle.valueType == StyleValueType.Color)
                    return styleSheet.ReadColor(handle);
                throw new InvalidCastException(
                    $"Cannot cast value of type `{handle.valueType}` into a `{StyleValueType.Color}`.");
            }

            public string AsResourcePath()
            {
                if (handle.valueType == StyleValueType.ResourcePath)
                    return styleSheet.ReadResourcePath(handle);
                throw new InvalidCastException(
                    $"Cannot cast value of type `{handle.valueType}` into a `{StyleValueType.ResourcePath}`.");
            }

            public UnityEngine.Object AsAssetReference()
            {
                if (handle.valueType == StyleValueType.AssetReference)
                    return styleSheet.ReadAssetReference(handle);
                throw new InvalidCastException(
                    $"Cannot cast value of type `{handle.valueType}` into a `{StyleValueType.AssetReference}`.");
            }

            public T AsEnum<T>()
                where T : Enum
            {
                if (handle.valueType == StyleValueType.Enum)
                    return (T) Enum.Parse(typeof(T), styleSheet.ReadEnum(handle));
                throw new InvalidCastException(
                    $"Cannot cast value of type `{handle.valueType}` into a `{StyleValueType.Enum}`.");
            }

            public string AsEnum()
            {
                if (handle.valueType == StyleValueType.Enum)
                    return styleSheet.ReadEnum(handle);
                throw new InvalidCastException(
                    $"Cannot cast value of type `{handle.valueType}` into a `{StyleValueType.Enum}`.");
            }

            public string AsString()
            {
                if (handle.valueType == StyleValueType.String)
                    return styleSheet.ReadString(handle);
                throw new InvalidCastException(
                    $"Cannot cast value of type `{handle.valueType}` into a `{StyleValueType.String}`.");
            }

            public ScalableImage AsScalableImage()
            {
                if (handle.valueType == StyleValueType.ScalableImage)
                    return styleSheet.ReadScalableImage(handle);
                throw new InvalidCastException(
                    $"Cannot cast value of type `{handle.valueType}` into a `{StyleValueType.ScalableImage}`.");
            }

            public string AsMissingAssetReference()
            {
                if (handle.valueType == StyleValueType.MissingAssetReference)
                    return styleSheet.ReadMissingAssetReferenceUrl(handle);
                throw new InvalidCastException(
                    $"Cannot cast value of type `{handle.valueType}` into a `{StyleValueType.MissingAssetReference}`.");
            }
        }

        internal struct StylePropertyPart : IDisposable
        {
            static readonly UnityEngine.Pool.ObjectPool<List<StyleValueHandleContext>> s_Pool =
                new UnityEngine.Pool.ObjectPool<List<StyleValueHandleContext>>(
                    () => new List<StyleValueHandleContext>(),
                    null,
                    s => { s.Clear(); }
                );

            /// <summary>
            /// Helper method to create a<see cref="StylePropertyPart"/> with a pre-pooled list of handles.
            /// </summary>
            /// <returns></returns>
            public static StylePropertyPart Create()
            {
                return new StylePropertyPart {handles = s_Pool.Get()};
            }

            /// <summary>
            /// Offset in the originating <see cref="StyleProperty"/> handles array.
            /// </summary>
            public int offset;

            /// <summary>
            /// Indicates if the handles were resolved as part of a variable.
            /// </summary>
            public bool isVariable;

            /// <summary>
            /// If <see cref="isVariable"/> is set, contains the name of the main variable.
            /// </summary>
            public string variableName;

            /// <summary>
            /// Handles that are related to this style property value. For a variable, this can resolve to multiple values.
            /// </summary>
            public List<StyleValueHandleContext> handles;

            public bool isVariableUnresolved => isVariable && handles?.Count == 0;

            public void Dispose()
            {
                if (null != handles)
                    s_Pool.Release(handles);

                offset = 0;
                isVariable = false;
                variableName = string.Empty;
            }
        }

        static readonly UnityEngine.Pool.ObjectPool<StylePropertyManipulator> s_Pool =
            new UnityEngine.Pool.ObjectPool<StylePropertyManipulator>(
                () => new StylePropertyManipulator(),
                null,
                s =>
                {
                    s.styleSheet = null;
                    s.styleRule = null;
                    s.propertyName = null;
                    s.element = null;
                    s.m_StyleProperty = null;

                    foreach (var value in s.stylePropertyParts)
                    {
                        value.Dispose();
                    }

                    s.stylePropertyParts.Clear();
                }
            );

        public static StylePropertyManipulator GetPooled()
        {
            return s_Pool.Get();
        }

        // Intentionally defined to force usage of the pool
        StylePropertyManipulator()
        {
        }

        StyleProperty m_StyleProperty;

        public StyleSheet styleSheet;

        public string propertyName;

        public StyleProperty styleProperty => m_StyleProperty ??= GetStyleProperty(styleRule, propertyName);

        public StyleRule styleRule;
        public VisualElement element;
        public bool editorExtensionMode;
        internal List<StylePropertyPart> stylePropertyParts = new List<StylePropertyPart>();

        /// <summary>
        /// Returns all the values as their string representation.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public IEnumerable<string> GetValuesAsString()
        {
            foreach (var part in stylePropertyParts)
            {
                foreach (var valueHandle in part.handles)
                {
                    switch (valueHandle.handle.valueType)
                    {
                        case StyleValueType.Invalid:
                            yield return "<invalid>";
                            break;
                        case StyleValueType.Keyword:
                            yield return valueHandle.styleSheet.ReadKeyword(valueHandle.handle).ToUssString();
                            break;
                        case StyleValueType.Float:
                            yield return valueHandle.styleSheet.ReadFloat(valueHandle.handle).ToString();
                            break;
                        case StyleValueType.Dimension:
                            yield return valueHandle.styleSheet.ReadDimension(valueHandle.handle).ToString();
                            break;
                        case StyleValueType.Color:
                            yield return valueHandle.styleSheet.ReadColor(valueHandle.handle).ToString();
                            break;
                        case StyleValueType.ResourcePath:
                            yield return valueHandle.styleSheet.ReadResourcePath(valueHandle.handle);
                            break;
                        case StyleValueType.AssetReference:
                            yield return valueHandle.styleSheet.ReadAssetReference(valueHandle.handle).ToString();
                            break;
                        case StyleValueType.Enum:
                            yield return valueHandle.styleSheet.ReadEnum(valueHandle.handle);
                            break;
                        case StyleValueType.Variable:
                            yield return valueHandle.styleSheet.ReadVariable(valueHandle.handle);
                            break;
                        case StyleValueType.String:
                            yield return valueHandle.styleSheet.ReadString(valueHandle.handle);
                            break;
                        case StyleValueType.Function:
                            yield return valueHandle.styleSheet.ReadFunction(valueHandle.handle).ToString();
                            break;
                        case StyleValueType.CommaSeparator:
                            break;
                        case StyleValueType.ScalableImage:
                            yield return valueHandle.styleSheet.ReadScalableImage(valueHandle.handle).ToString();
                            break;
                        case StyleValueType.MissingAssetReference:
                            yield return valueHandle.styleSheet.ReadMissingAssetReferenceUrl(valueHandle.handle);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        public int GetPartsCount()
        {
            return stylePropertyParts.Count;
        }

        public int GetValuesCount()
        {
            var count = 0;
            foreach (var part in stylePropertyParts)
            {
                if (part.isVariable && part.isVariableUnresolved)
                    count += 1;
                else
                    count += part.handles.Count;
            }
            return count;
        }

        public bool IsVariableAtIndex(int index)
        {
            var indices = GetInternalIndices(index);
            return stylePropertyParts[indices.partIndex].isVariable;
        }

        public string GetVariableNameAtIndex(int index)
        {
            var indices = GetInternalIndices(index);
            return stylePropertyParts[indices.partIndex].variableName;
        }

        public bool IsKeywordAtIndex(int index)
        {
            return GetValueContextAtIndex(index).handle.valueType == StyleValueType.Keyword;
        }

        public StyleValueHandleContext GetValueContextAtIndex(int index)
        {
            var indices = GetInternalIndices(index);
            // Unresolved variable
            return indices.valueIndex < 0 ? default : stylePropertyParts[indices.partIndex].handles[indices.valueIndex];
        }

        public void AddValue<T>(T value, StyleValueType valueType)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            EnsureStylePropertyExists();

            var offset = styleProperty.values.Length;

            if (offset > 0)
            {
                AddCommaSeparator();
                ++offset;
            }

            var handle = AddTypedValue(value, valueType);
            var part = CreatePart(styleSheet, handle, offset);
            stylePropertyParts.Add(part);
            UpdateStylesheet();
        }

        public void AddVariable(string variableName)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            EnsureStylePropertyExists();

            var offset = styleProperty.values.Length;

            if (offset > 0)
            {
                AddCommaSeparator();
                ++offset;
            }

            var part = ResolveVariable(AddVariableToStyleSheet(variableName));
            part.offset = offset;
            stylePropertyParts.Add(part);
            UpdateStylesheet();
        }

        public void SetValueAtIndex<T>(int index, T value, StyleValueType type)
        {
            if (null == styleProperty)
                throw new InvalidOperationException();

            var indices = GetInternalIndices(index);

            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            SetValue(indices, value, type);
            UpdateStylesheet();
        }

        public void SetVariableAtIndex(int index, string variableName)
        {
            if (null == styleProperty)
                throw new InvalidOperationException();

            var indices = GetInternalIndices(index);

            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            if (stylePropertyParts[indices.partIndex].isVariable)
                OverrideVariableWithValue(indices, new Variable(variableName), StyleValueType.Variable);
            else
                SetVariable(indices, variableName);
            UpdateStylesheet();
        }

        public void RemoveAtIndex(int index)
        {
            var indices = GetInternalIndices(index);

            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            RemoveValue(indices);
            UpdateStylesheet();
        }

        public void RemoveProperty()
        {
            if (null != styleProperty)
            {
                styleSheet.RemoveProperty(styleRule, styleProperty);
                m_StyleProperty = null;
                foreach (var part in stylePropertyParts)
                {
                    part.Dispose();
                }
                stylePropertyParts.Clear();
                UpdateStylesheet();
            }
        }

        public void ClearValues()
        {
            if (null == styleProperty)
                return;

            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            styleProperty.values = Array.Empty<StyleValueHandle>();
            foreach (var part in stylePropertyParts)
            {
                part.Dispose();
            }
            stylePropertyParts.Clear();
            UpdateStylesheet();
        }

        StyleValueHandle TransferTypedValue(StyleValueHandleContext handle)
        {
            switch (handle.handle.valueType)
            {
                case StyleValueType.Keyword:
                    return AddTypedValue(handle.AsKeyword(), StyleValueType.Keyword);
                case StyleValueType.Float:
                    return AddTypedValue(handle.AsFloat(), StyleValueType.Float);
                case StyleValueType.Dimension:
                    return AddTypedValue(handle.AsDimension(), StyleValueType.Dimension);
                case StyleValueType.Color:
                    return AddTypedValue(handle.AsColor(), StyleValueType.Color);
                case StyleValueType.ResourcePath:
                    return AddTypedValue(handle.AsResourcePath(), StyleValueType.ResourcePath);
                case StyleValueType.AssetReference:
                    return AddTypedValue(handle.AsAssetReference(), StyleValueType.AssetReference);
                case StyleValueType.Enum:
                    return AddTypedValue(handle.AsEnum(), StyleValueType.Enum);
                case StyleValueType.String:
                    return AddTypedValue(handle.AsString(), StyleValueType.String);
                case StyleValueType.ScalableImage:
                    return AddTypedValue(handle.AsScalableImage(), StyleValueType.ScalableImage);
                case StyleValueType.MissingAssetReference:
                    return AddTypedValue(handle.AsString(), StyleValueType.MissingAssetReference);
                case StyleValueType.Invalid:
                case StyleValueType.Variable:
                case StyleValueType.Function:
                case StyleValueType.CommaSeparator:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void SetTypedValue<T>(StyleValueHandleContext handle, T value)
        {
            switch (handle.handle.valueType)
            {
                case StyleValueType.Keyword:
                    styleSheet.SetValue(handle.handle, (StyleValueKeyword)(object) value);
                    break;
                case StyleValueType.Float:
                    styleSheet.SetValue(handle.handle, (float)(object) value);
                    break;
                case StyleValueType.Dimension:
                    styleSheet.SetValue(handle.handle, (Dimension)(object) value);
                    break;
                case StyleValueType.Color:
                    styleSheet.SetValue(handle.handle, (Color)(object) value);
                    break;
                case StyleValueType.ResourcePath:
                    styleSheet.SetValue(handle.handle, (string)(object) value);
                    break;
                case StyleValueType.AssetReference:
                    styleSheet.SetValue(handle.handle, (UnityEngine.Object)(object) value);
                    break;
                case StyleValueType.Enum:
                    if (typeof(T).IsEnum || value is Enum)
                        styleSheet.SetValue(handle.handle, (Enum)(object) value);
                    else if (typeof(T) == typeof(string) || value is string)
                        styleSheet.SetValue(handle.handle, (string)(object) value);
                    break;
                case StyleValueType.String:
                    styleSheet.SetValue(handle.handle, (string)(object) value);
                    break;

                case StyleValueType.MissingAssetReference:
                    styleSheet.SetValue(handle.handle, (string)(object) value);
                    break;

                case StyleValueType.ScalableImage:
                    // Not actually supported
                    // styleSheet.SetValue(handle.handle, handle.AsScalableImage());
                    // break;
                // These are not "values".
                case StyleValueType.Invalid:
                case StyleValueType.Variable:
                case StyleValueType.Function:
                case StyleValueType.CommaSeparator:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        StyleValueHandle AddTypedValue<T>(T value, StyleValueType type)
        {
            switch (type)
            {
                case StyleValueType.Keyword:
                    return styleSheet.AddValue(styleProperty, (StyleValueKeyword)(object) value);
                case StyleValueType.Float:
                    return styleSheet.AddValue(styleProperty, (float)(object) value);
                case StyleValueType.Dimension:
                    return styleSheet.AddValue(styleProperty, (Dimension)(object) value);
                case StyleValueType.Color:
                    return styleSheet.AddValue(styleProperty, (Color)(object) value);
                case StyleValueType.ResourcePath:
                    return styleSheet.AddValue(styleProperty, (string)(object) value);
                case StyleValueType.AssetReference:
                    return styleSheet.AddValue(styleProperty, (UnityEngine.Object)(object) value);
                case StyleValueType.Enum:
                {
                    if (value is string strValue)
                    {
                        // Add value data to data array.
                        var index = styleSheet.AddValueToArray(strValue);

                        // Add value object to property.
                        return styleSheet.AddValueHandle(styleProperty, index, StyleValueType.Enum);

                    }
                    return styleSheet.AddValue(styleProperty, (Enum) (object) value);
                }
                case StyleValueType.String:
                    return styleSheet.AddValue(styleProperty, (string)(object) value);

                case StyleValueType.MissingAssetReference:
                    return styleSheet.AddValue(styleProperty, (string)(object) value);

                case StyleValueType.ScalableImage:
                    // Not actually supported
                    //return styleSheet.AddValue(styleProperty, (ScalableImage)(object) value);
                // These are not "values".
                case StyleValueType.Invalid:
                case StyleValueType.Variable:
                case StyleValueType.Function:
                case StyleValueType.CommaSeparator:
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        void OverrideVariableWithValue<T>(Index indices, T value, StyleValueType valueType)
        {
            if (!stylePropertyParts[indices.partIndex].isVariable)
                return;

            var part = stylePropertyParts[indices.partIndex];
            var initialOffset = part.offset;
            var nextOffset = indices.partIndex + 1 >= stylePropertyParts.Count
                ? -1
                : stylePropertyParts[indices.partIndex + 1].offset;

            // Range of handles to remove in the StyleProperty.values array
            var range = nextOffset < 0
                ? styleProperty.values.Length - initialOffset
                : nextOffset - initialOffset;

            var newParts = new List<StylePropertyPart>();

            // To set an explicit value on top of a variable, we must first remove the variable. In the case where
            // the variable points to a list of values, we must remove all values of the list and set them as
            // explicit values of the same type.
            var list = styleProperty.values.ToList();
            list.RemoveRange(initialOffset, range);

            var currentOffset = initialOffset;
            for (var i = 0; i < part.handles.Count; ++i, ++currentOffset)
            {
                var propertyValue = StylePropertyPart.Create();
                propertyValue.offset = currentOffset;

                if (i == indices.valueIndex && typeof(Variable).IsAssignableFrom(typeof(T)) && value is Variable variable)
                {
                    var handles = AddVariableToStyleSheet(variable.name);

                    var property = new StyleProperty
                    {
                        name = styleProperty.name,
                        values = handles
                    };

                    var ib = 0;
                    var newPart = ResolveValueOrVariable(styleSheet, element, styleRule, property, ref ib,
                        editorExtensionMode);

                    list.InsertRange(currentOffset, handles);
                    currentOffset += 2;
                    if (i < part.handles.Count - 1 || indices.partIndex < stylePropertyParts.Count - 1)
                        list.Insert(++currentOffset, new StyleValueHandle(-1, StyleValueType.CommaSeparator));

                    newParts.Add(newPart);
                }
                else
                {
                    var handle = i == indices.valueIndex
                        ? AddTypedValue(value, valueType)
                        : TransferTypedValue(part.handles[i]);
                    list.Insert(currentOffset, handle);

                    if (i < part.handles.Count - 1 || indices.partIndex < stylePropertyParts.Count - 1)
                        list.Insert(++currentOffset, new StyleValueHandle(-1, StyleValueType.CommaSeparator));

                    propertyValue.handles.Add(new StyleValueHandleContext { styleSheet = styleSheet, handle = handle });

                    newParts.Add(propertyValue);
                }
            }

            if (part.isVariableUnresolved && indices.valueIndex == -1)
            {
                if (typeof(Variable).IsAssignableFrom(typeof(T)) && value is Variable variable)
                {
                    var handles = AddVariableToStyleSheet(variable.name);
                    var newPart = StylePropertyPart.Create();
                    newPart.offset = currentOffset;
                    newPart.handles.Add( new StyleValueHandleContext
                    {
                        styleSheet = styleSheet,
                        handle = handles[2],
                    });
                    list.InsertRange(currentOffset, handles);
                    newParts.Add(newPart);
                }
                else
                {
                    var handle = AddTypedValue(value, valueType);

                    var newPart = StylePropertyPart.Create();
                    newPart.offset = currentOffset;
                    newPart.handles.Add(new StyleValueHandleContext
                    {
                        styleSheet = styleSheet,
                        handle = handle,
                    });
                    list.Insert(currentOffset, handle);
                    newParts.Add(newPart);
                }
            }

            styleProperty.values = list.ToArray();
            stylePropertyParts.RemoveAt(indices.partIndex);
            // Manually dispose removed part
            part.Dispose();

            stylePropertyParts.InsertRange(indices.partIndex, newParts);
            var partsCount = indices.partIndex + newParts.Count;

            var offset = currentOffset - nextOffset;
            for (var i = partsCount; i < stylePropertyParts.Count; ++i)
            {
                var nextPart = stylePropertyParts[i];
                nextPart.offset += offset;
                stylePropertyParts[i] = nextPart;
            }
        }

        void AddCommaSeparator()
        {
            styleSheet.AddValueHandle(styleProperty, -1, StyleValueType.CommaSeparator);
        }

        StyleValueHandle AddKeywordToStyleSheet(StyleValueKeyword keyword)
        {
            return styleSheet.AddValue(styleProperty, keyword);
        }

        StyleValueHandle[] AddVariableToStyleSheet(string variableName)
        {
            return styleSheet.AddVariable(styleProperty, variableName);
        }

        void SetValue<T>(Index indices, T value, StyleValueType valueType)
        {
            var part = stylePropertyParts[indices.partIndex];

            if (part.isVariable)
            {
                OverrideVariableWithValue(indices, value, valueType);
            }
            // The assumption here is that an individual value which is not a variable will always have a single
            // resolved value
            else
            {
                if (part.handles[0].handle.valueType == valueType)
                {
                    var valueHandle = part.handles[0];
                    SetTypedValue(valueHandle, value);
                }
                else
                {
                    var initialOffset = part.offset;
                    var list = styleProperty.values.ToList();
                    list.RemoveAt(initialOffset);
                    var handle = AddTypedValue(value, valueType);
                    list.Insert(initialOffset, handle);
                    styleProperty.values = list.ToArray();
                    var valueContext = part.handles[0];
                    valueContext.handle = handle;
                    part.handles[0] = valueContext;
                }
            }
        }

        void SetVariable(Index indices, string variableName)
        {
            var part = stylePropertyParts[indices.partIndex];
            var initialOffset = part.offset;

            var nextOffset = indices.partIndex + 1 >= stylePropertyParts.Count
                ? -1
                : stylePropertyParts[indices.partIndex + 1].offset /* To account for the comma */ - 1;

            // Range of handles to remove in the StyleProperty.values array
            var range = nextOffset < 0
                ? styleProperty.values.Length - initialOffset
                : nextOffset - initialOffset;

            var list = styleProperty.values.ToList();
            list.RemoveRange(initialOffset, range);
            var handles = AddVariableToStyleSheet(variableName);
            list.InsertRange(initialOffset, handles);

            styleProperty.values = list.ToArray();
            var i = part.offset;
            var newPart = ResolveValueOrVariable(styleSheet, element, styleRule, styleProperty, ref i, editorExtensionMode);
            stylePropertyParts.RemoveAt(indices.partIndex);
            stylePropertyParts.Insert(indices.partIndex, newPart);
            part.Dispose();

            AdjustOffsets(indices.partIndex, i - nextOffset);
        }

        void AdjustOffsets(int indicesPartIndex, int partOffset)
        {
            for (var i = indicesPartIndex; i < stylePropertyParts.Count; ++i)
            {
                var stylePropertyPart = stylePropertyParts[i];
                stylePropertyPart.offset += partOffset;
                stylePropertyParts[i] = stylePropertyPart;
            }
        }

        Index GetInternalIndices(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException();

            var current = index;
            var partIndex = 0;
            for (; partIndex < stylePropertyParts.Count; ++partIndex)
            {
                var valueIndex = 0;
                var part = stylePropertyParts[partIndex];

                for (; valueIndex < part.handles.Count; ++valueIndex)
                {
                    if (--current < 0)
                    {
                        return new Index(index, partIndex, valueIndex);
                    }
                }

                if (!part.isVariableUnresolved)
                    continue;

                if (--current < 0)
                    return new Index(index, partIndex, -1);
            }

            throw new ArgumentOutOfRangeException();
        }

        void RemoveValue(Index indices)
        {
            var part = stylePropertyParts[indices.partIndex];
            var initialOffset = part.offset;
            var nextOffset = indices.partIndex + 1 >= stylePropertyParts.Count
                ? -1
                : stylePropertyParts[indices.partIndex + 1].offset;

            // Range of handles to remove in the StyleProperty.values array
            var range = nextOffset < 0
                ? styleProperty.values.Length - initialOffset
                : nextOffset - initialOffset;

            if (part.isVariable)
            {
                if (part.isVariableUnresolved && indices.valueIndex == -1)
                {
                    // If there's only a single value, simply remove the property.
                    if (stylePropertyParts.Count == 1)
                    {
                        RemoveProperty();
                        return;
                    }

                    var valueList = styleProperty.values.ToList();

                    if (indices.partIndex > 0)
                        initialOffset -= 1;

                    if (indices.partIndex == stylePropertyParts.Count - 1)
                        range += 1;

                    valueList.RemoveRange(initialOffset, range);
                    var firstPropertyValue = stylePropertyParts[indices.partIndex];
                    for(var i = indices.partIndex + 1; i < stylePropertyParts.Count; ++i)
                    {
                        var propertyValue = stylePropertyParts[i];
                        propertyValue.offset -= 2;
                    }
                    firstPropertyValue.Dispose();
                    stylePropertyParts.RemoveAt(indices.partIndex);
                    styleProperty.values = valueList.ToArray();
                    return;
                }

                var newParts = new List<StylePropertyPart>();

                // To set an explicit value on top of a variable, we must first remove the variable. In the case where
                // the variable points to a list of values, we must remove all values of the list and set them as
                // explicit values of the same type.
                var list = styleProperty.values.ToList();
                list.RemoveRange(initialOffset, range);

                var currentOffset = initialOffset;
                for (var i = 0; i < part.handles.Count; ++i, ++currentOffset)
                {
                    var propertyValue = StylePropertyPart.Create();
                    propertyValue.offset = currentOffset;

                    var handle = TransferTypedValue(part.handles[i]);

                    list.Insert(currentOffset, handle);

                    if (i < part.handles.Count - 1 || indices.partIndex < stylePropertyParts.Count - 1)
                        list.Insert(++currentOffset, new StyleValueHandle(-1, StyleValueType.CommaSeparator));

                    propertyValue.handles.Add(new StyleValueHandleContext { styleSheet = styleSheet, handle = handle });

                    newParts.Add(propertyValue);
                }

                styleProperty.values = list.ToArray();
                stylePropertyParts.RemoveAt(indices.partIndex);
                // Manually dispose removed part
                part.Dispose();

                stylePropertyParts.InsertRange(indices.partIndex, newParts);
                var adjustIndex = indices.partIndex + newParts.Count;

                var offset = currentOffset - nextOffset;
                for (var i = adjustIndex; i < stylePropertyParts.Count; ++i)
                {
                    var nextPart = stylePropertyParts[i];
                    nextPart.offset += offset;
                    stylePropertyParts[i] = nextPart;
                }

                // Now that the variable has been transferred as explicit values, we can remove the actual value.
                if (indices.valueIndex >= 0)
                    RemoveAtIndex(indices.index);
            }
            else
            {
                // If there's only a single value, simply remove the property.
                if (stylePropertyParts.Count == 1)
                {
                    RemoveProperty();
                    return;
                }

                var list = styleProperty.values.ToList();

                if (indices.partIndex > 0)
                    initialOffset -= 1;

                if (indices.partIndex == stylePropertyParts.Count - 1)
                    range += 1;

                list.RemoveRange(initialOffset, range);
                var partToRemove = stylePropertyParts[indices.partIndex];
                partToRemove.Dispose();
                stylePropertyParts.RemoveAt(indices.partIndex);
                AdjustOffsets(indices.partIndex, -2);
                styleProperty.values = list.ToArray();
            }
        }

        public void Dispose()
        {
            s_Pool.Release(this);
        }

        internal static StylePropertyPart ResolveValueOrVariable(
            StyleSheet styleSheet,
            VisualElement element,
            StyleRule styleRule,
            StyleProperty property,
            ref int currentIndex,
            bool isEditorExtensionMode)
        {
            var handle = property.values[currentIndex];

            switch (handle.valueType)
            {
                case StyleValueType.Invalid:
                case StyleValueType.Keyword:
                case StyleValueType.Float:
                case StyleValueType.Dimension:
                case StyleValueType.Color:
                case StyleValueType.ResourcePath:
                case StyleValueType.AssetReference:
                case StyleValueType.Enum:
                case StyleValueType.String:
                case StyleValueType.ScalableImage:
                case StyleValueType.MissingAssetReference:
                {
                    // skip comma
                    ++currentIndex;

                    var part = StylePropertyPart.Create();
                    part.handles.Add(new StyleValueHandleContext
                    {
                        styleSheet = styleSheet,
                        handle = handle
                    });
                    return part;
                }

                case StyleValueType.Function:
                {
                    var argCountHandle = property.values[++currentIndex];
                    var argCount = (int) styleSheet.ReadFloat(argCountHandle);
                    var varHandle = property.values[++currentIndex];
                    var variable = styleSheet.ReadVariable(varHandle);
                    using (var manipulator = ResolveVariable(element, styleSheet, styleRule, variable, isEditorExtensionMode))
                    {
                        if (argCount == 1)
                        {
                            // Skip comma
                            ++currentIndex;
                            var part = StylePropertyPart.Create();
                            if (null == manipulator || manipulator.stylePropertyParts.Count == 0)
                            {
                                part.isVariable = true;
                                part.variableName = variable;
                                return part;
                            }

                            part.handles.AddRange(manipulator.stylePropertyParts.SelectMany(o => o.handles));
                            part.isVariable = true;
                            part.variableName = variable;
                            return part;
                        }
                    }

                    // Skip comma and point to next function argument.
                    currentIndex += 2;

                    var fallbackPart = ResolveValueOrVariable(styleSheet, element, styleRule, property, ref currentIndex, isEditorExtensionMode);
                    fallbackPart.isVariable = true;
                    fallbackPart.variableName = variable;

                    return fallbackPart;
                }

                // These should never be hit as they are being handled by the cases above
                case StyleValueType.Variable:
                case StyleValueType.CommaSeparator:
                    throw new InvalidOperationException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        static StylePropertyManipulator ResolveVariable(
            VisualElement currentVisualElement,
            StyleSheet styleSheet,
            StyleRule styleRule,
            string variableName,
            bool editorExtensionMode)
        {
            var customStyles = currentVisualElement.computedStyle.customProperties;
            if (customStyles != null && customStyles.TryGetValue(variableName, out var stylePropertyValue))
            {
                var propValue = stylePropertyValue;
                if (!editorExtensionMode && propValue.sheet.isDefaultStyleSheet)
                    return null;

                if (propValue.sheet.isDefaultStyleSheet)
                {
                    StyleVariableUtilities.editorVariableDescriptions.TryGetValue(variableName, out var variableDescription);
                }

                var propStyleSheet = propValue.sheet;

                for (var propertyIndex = styleRule.properties.Length - 1; propertyIndex >= 0; --propertyIndex)
                {
                    var property = styleRule.properties[propertyIndex];

                    if (property.name != variableName)
                        continue;

                    var manipulator = GetPooled();
                    for (var i = 0; i < property.values.Length; ++i)
                    {
                        var index = i;
                        var offset = ResolveValueOrVariable(propStyleSheet, currentVisualElement, styleRule, property, ref i, editorExtensionMode);
                        offset.offset = index;
                        offset.isVariable = true;
                        offset.variableName = variableName;
                        manipulator.stylePropertyParts.Add(offset);
                    }

                    return manipulator;
                }
            }

            // Look within :root selectors
            var styleSheets = styleSheet.flattenedRecursiveImports;
            var rootManipulator = ResolveVariableFromRootSelectorInStyleSheet(currentVisualElement, styleSheet, styleRule, variableName, editorExtensionMode);
            if (null != rootManipulator)
                return rootManipulator;

            for (var i = styleSheets.Count - 1; i >= 0; --i)
            {
                var sheet = styleSheets[i];
                rootManipulator =
                    ResolveVariableFromRootSelectorInStyleSheet(currentVisualElement, sheet, styleRule, variableName, editorExtensionMode);
                if (null != rootManipulator)
                    return rootManipulator;
            }

            return null;
        }

        static StylePropertyManipulator ResolveVariableFromRootSelectorInStyleSheet(
            VisualElement currentVisualElement,
            StyleSheet sheet,
            StyleRule styleRule,
            string variableName,
            bool editorExtensionMode)
        {
            for (var selectorIndex = sheet.complexSelectors.Length - 1; selectorIndex >= 0; --selectorIndex)
            {
                var complexSelector = sheet.complexSelectors[selectorIndex];
                if (!complexSelector.isSimple)
                    continue;

                var simpleSelector = complexSelector.selectors[0];
                var selectorPart = simpleSelector.parts[0];

                if (selectorPart.type != StyleSelectorType.Wildcard &&
                    selectorPart.type != StyleSelectorType.PseudoClass)
                    continue;

                if (selectorPart.type == StyleSelectorType.PseudoClass && selectorPart.value != "root")
                    continue;

                var rule = complexSelector.rule;
                for (var propertyIndex = rule.properties.Length - 1; propertyIndex >= 0; --propertyIndex)
                {
                    var property = rule.properties[propertyIndex];
                    if (property.name != variableName)
                    {
                        continue;
                    }

                    var manipulator = GetPooled();
                    for (var i = 0; i < property.values.Length; ++i)
                    {
                        var index = i;
                        var newPart = ResolveValueOrVariable(sheet, currentVisualElement, styleRule, property, ref i, editorExtensionMode);
                        newPart.offset = index;
                        newPart.isVariable = true;
                        newPart.variableName = variableName;
                        manipulator.stylePropertyParts.Add(newPart);
                    }

                    return manipulator;
                }
            }

            return null;
        }

        StylePropertyPart ResolveVariable(StyleValueHandle[] handles)
        {
            var property = new StyleProperty
            {
                name = propertyName,
                values = handles
            };

            var index = 0;
            return ResolveValueOrVariable(styleSheet, element, styleRule, property, ref index,
                editorExtensionMode);
        }

        void UpdateStylesheet()
        {
            // Set the contentHash to 0 if the style sheet is empty
            if (styleSheet.rules == null || styleSheet.rules.Length == 0)
                styleSheet.contentHash = 0;
            else
                // Use a random value instead of computing the real contentHash.
                // This is faster (for large content) and safe enough to avoid conflicts with other style sheets
                // since contentHash is used internally as a optimized way to compare style sheets.
                // However, note that the real contentHash will still be computed on import.

                styleSheet.contentHash = UnityEngine.Random.Range(1, int.MaxValue);
        }

        void EnsureStylePropertyExists()
        {
            m_StyleProperty
                ??= GetStyleProperty(styleRule, propertyName)
                ?? styleSheet.AddProperty(styleRule, propertyName);
        }

        static StyleProperty GetStyleProperty(StyleRule rule, string propertyName)
        {
            return rule?.properties.LastOrDefault(property => property.name == propertyName);
        }

        static StylePropertyPart CreatePart(StyleSheet styleSheet, StyleValueHandle handle, int offset)
        {
            var part = StylePropertyPart.Create();
            part.offset = offset;
            part.handles.Add(
                new StyleValueHandleContext
                {
                    styleSheet = styleSheet,
                    handle = handle
                });
            return part;
        }
    }
}
