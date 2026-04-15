// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.UIToolkit.Editor;
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

        internal struct StylePropertyPart : IDisposable
        {
            static readonly UnityEngine.Pool.ObjectPool<List<StylePropertyValue>> s_Pool =
                new UnityEngine.Pool.ObjectPool<List<StylePropertyValue>>(
                    () => new List<StylePropertyValue>(),
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
            public List<StylePropertyValue> handles;

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
                            yield return valueHandle.sheet.ReadKeyword(valueHandle.handle).ToUssString();
                            break;
                        case StyleValueType.Float:
                            yield return valueHandle.sheet.ReadFloat(valueHandle.handle).ToString();
                            break;
                        case StyleValueType.Dimension:
                            yield return valueHandle.sheet.ReadDimension(valueHandle.handle).ToString();
                            break;
                        case StyleValueType.Color:
                            yield return valueHandle.sheet.ReadColor(valueHandle.handle).ToString();
                            break;
                        case StyleValueType.ResourcePath:
                            yield return valueHandle.sheet.ReadResourcePath(valueHandle.handle).ToString();
                            break;
                        case StyleValueType.AssetReference:
                            yield return valueHandle.sheet.ReadAssetReference(valueHandle.handle).ToString();
                            break;
                        case StyleValueType.Enum:
                            yield return valueHandle.sheet.ReadEnum(valueHandle.handle);
                            break;
                        case StyleValueType.Variable:
                            yield return valueHandle.sheet.ReadVariable(valueHandle.handle);
                            break;
                        case StyleValueType.String:
                            yield return valueHandle.sheet.ReadString(valueHandle.handle);
                            break;
                        case StyleValueType.Function:
                            yield return valueHandle.sheet.ReadFunction(valueHandle.handle).ToString();
                            break;
                        case StyleValueType.CommaSeparator:
                            break;
                        case StyleValueType.ScalableImage:
                            yield return valueHandle.sheet.ReadScalableImage(valueHandle.handle).ToString();
                            break;
                        case StyleValueType.MissingAssetReference:
                            yield return valueHandle.sheet.ReadMissingAssetReferenceUrl(valueHandle.handle);
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

        public StylePropertyValue GetValueContextAtIndex(int index)
        {
            var indices = GetInternalIndices(index);
            // Unresolved variable
            return indices.valueIndex < 0 ? default : stylePropertyParts[indices.partIndex].handles[indices.valueIndex];
        }

        public void AddEnumAsString(string value)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            EnsureStylePropertyExists();

            var offset = styleProperty.values.Length;

            var manipulator = styleProperty.GetManipulator(styleSheet);
            if (offset > 0)
            {
                manipulator.AddCommaSeparator();
                ++offset;
            }

            manipulator.AddEnum(value);
            var handle = styleProperty.values[^1];
            var part = CreatePart(styleSheet, handle, offset);
            stylePropertyParts.Add(part);
        }

        public void AddStylePropertyName(StylePropertyName value)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            EnsureStylePropertyExists();

            var offset = styleProperty.values.Length;

            var manipulator = styleProperty.GetManipulator(styleSheet);
            if (offset > 0)
            {
                manipulator.AddCommaSeparator();
                ++offset;
            }

            manipulator.AddStylePropertyName(value);
            var handle = styleProperty.values[^1];
            var part = CreatePart(styleSheet, handle, offset);
            stylePropertyParts.Add(part);
        }

        public void AddTimeValue(TimeValue value)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            EnsureStylePropertyExists();

            var offset = styleProperty.values.Length;

            var manipulator = styleProperty.GetManipulator(styleSheet);
            if (offset > 0)
            {
                manipulator.AddCommaSeparator();
                ++offset;
            }

            manipulator.AddTimeValue(value);
            var handle = styleProperty.values[^1];
            var part = CreatePart(styleSheet, handle, offset);
            stylePropertyParts.Add(part);
        }

        public void AddEasingFunction(EasingFunction value)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            EnsureStylePropertyExists();

            var offset = styleProperty.values.Length;

            var manipulator = styleProperty.GetManipulator(styleSheet);
            if (offset > 0)
            {
                manipulator.AddCommaSeparator();
                ++offset;
            }

            manipulator.AddEasingFunction(value);
            var handle = styleProperty.values[^1];
            var part = CreatePart(styleSheet, handle, offset);
            stylePropertyParts.Add(part);
        }

        public void AddKeyword(StyleValueKeyword value)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            EnsureStylePropertyExists();

            var offset = styleProperty.values.Length;

            var manipulator = styleProperty.GetManipulator(styleSheet);
            if (offset > 0)
            {
                manipulator.AddCommaSeparator();
                ++offset;
            }

            manipulator.AddKeyword(value);
            var handle = styleProperty.values[^1];
            var part = CreatePart(styleSheet, handle, offset);
            stylePropertyParts.Add(part);
        }

        public void AddVariable(string variableName)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            EnsureStylePropertyExists();

            var offset = styleProperty.values.Length;

            var manipulator = styleProperty.GetManipulator(styleSheet);
            if (offset > 0)
            {
                manipulator.AddCommaSeparator();
                ++offset;
            }

            manipulator.AddVariableReference(variableName);

            var part = ResolveVariable(styleProperty.values[^3..]);
            part.offset = offset;
            stylePropertyParts.Add(part);
        }


        public void SetValueAtIndex<T>(int index, T value, StyleValueType type)
        {
            if (null == styleProperty)
                throw new InvalidOperationException();

            var indices = GetInternalIndices(index);

            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            SetValue(indices, value, type);
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
        }

        public void RemoveAtIndex(int index)
        {
            var indices = GetInternalIndices(index);

            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            RemoveValue(indices);
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
            }
        }

        public void ClearValues()
        {
            if (null == styleProperty)
                return;

            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            styleProperty.ClearValue();
            foreach (var part in stylePropertyParts)
            {
                part.Dispose();
            }
            stylePropertyParts.Clear();
        }

        StyleValueHandle TransferTypedValue(StylePropertyValue handle)
        {
            var manipulator = styleProperty.GetManipulator(styleSheet);
            switch (handle.handle.valueType)
            {
                case StyleValueType.Keyword:
                    manipulator.AddKeyword(handle.sheet.ReadKeyword(handle.handle));
                    break;
                case StyleValueType.Float:
                    manipulator.AddFloat(handle.sheet.ReadFloat(handle.handle));
                    break;
                case StyleValueType.Dimension:
                    manipulator.AddDimension(handle.sheet.ReadDimension(handle.handle));
                    break;
                case StyleValueType.Color:
                    manipulator.AddColor(handle.sheet.ReadColor(handle.handle));
                    break;
                case StyleValueType.ResourcePath:
                    manipulator.AddResourcePath(handle.sheet.ReadResourcePath(handle.handle));
                    break;
                case StyleValueType.AssetReference:
                    manipulator.AddAssetReference(handle.sheet.ReadAssetReference(handle.handle));
                    break;
                case StyleValueType.Enum:
                    manipulator.AddEnum(handle.sheet.ReadEnum(handle.handle));
                    break;
                case StyleValueType.String:
                    manipulator.AddString(handle.sheet.ReadString(handle.handle));
                    break;
                case StyleValueType.ScalableImage:
                    manipulator.AddScalableImage(handle.sheet.ReadScalableImage(handle.handle));
                    break;
                case StyleValueType.MissingAssetReference:
                    manipulator.AddMissingAssetReferenceUrl(handle.sheet.ReadMissingAssetReferenceUrl(handle.handle));
                    break;
                case StyleValueType.Invalid:
                case StyleValueType.Variable:
                case StyleValueType.Function:
                case StyleValueType.CommaSeparator:
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return styleProperty.values[^1];
        }

        void SetTypedValue<T>(StylePropertyValue handle, T value)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
            switch (handle.handle.valueType)
            {
                case StyleValueType.Keyword:
                    styleSheet.WriteKeyword(ref handle.handle, (StyleValueKeyword)(object) value);
                    break;
                case StyleValueType.Float:
                    styleSheet.WriteFloat(ref handle.handle, (float)(object) value);
                    break;
                case StyleValueType.Dimension:
                    styleSheet.WriteDimension(ref handle.handle, (Dimension)(object) value);
                    break;
                case StyleValueType.Color:
                    styleSheet.WriteColor(ref handle.handle, (Color)(object) value);
                    break;
                case StyleValueType.ResourcePath:
                    if (value is UnityEngine.Object resourceObject)
                    {
                        BuilderAssetUtilities.TryGetResourcesPathForAsset(resourceObject, out var resolvedResourcePath);
                        styleSheet.WriteResourcePath(ref handle.handle, resolvedResourcePath);
                    }
                    else
                    {
                        styleSheet.WriteResourcePath(ref handle.handle, new ResolvedResourcePath((string)(object)value, null));
                    }
                    break;
                case StyleValueType.AssetReference:
                    styleSheet.WriteAssetReference(ref handle.handle, (UnityEngine.Object)(object) value);
                    break;
                case StyleValueType.Enum:
                    if (typeof(T).IsEnum || value is Enum)
                        styleSheet.WriteEnum(ref handle.handle, (Enum)(object) value);
                    else if (typeof(T) == typeof(string) || value is string)
                        styleSheet.WriteEnumAsString(ref handle.handle, (string)(object) value);
                    break;
                case StyleValueType.String:
                    styleSheet.WriteString(ref handle.handle, (string)(object) value);
                    break;
                case StyleValueType.MissingAssetReference:
                    styleSheet.WriteMissingAssetReferenceUrl(ref handle.handle, (string)(object) value);
                    break;
                case StyleValueType.ScalableImage:
                    styleSheet.WriteScalableImage(ref handle.handle, (ScalableImage)(object) value);
                    break;
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
            var manipulator = styleProperty.GetManipulator(styleSheet);
            switch (type)
            {
                case StyleValueType.Keyword:
                    manipulator.AddKeyword((StyleValueKeyword)(object) value);
                    break;
                case StyleValueType.Float:
                    manipulator.AddFloat((float)(object) value);
                    break;
                case StyleValueType.Dimension:
                    manipulator.AddDimension((Dimension)(object) value);
                    break;
                case StyleValueType.Color:
                    manipulator.AddColor((Color)(object) value);
                    break;
                case StyleValueType.ResourcePath:
                    manipulator.AddResourcePath(new ResolvedResourcePath((string)(object) value, null));
                    break;
                case StyleValueType.AssetReference:
                    manipulator.AddAssetReference((UnityEngine.Object)(object) value);
                    break;
                case StyleValueType.Enum:
                {
                    if (value is string strValue)
                        manipulator.AddEnum(strValue);
                    else
                        manipulator.AddEnum((Enum) (object) value);
                    break;
                }
                case StyleValueType.String:
                    manipulator.AddString((string)(object) value);
                    break;

                case StyleValueType.MissingAssetReference:
                    manipulator.AddMissingAssetReferenceUrl((string)(object) value);
                    break;

                case StyleValueType.ScalableImage:
                    manipulator.AddScalableImage((ScalableImage)(object) value);
                    break;
                // These are not "values".
                case StyleValueType.Invalid:
                case StyleValueType.Variable:
                case StyleValueType.Function:
                case StyleValueType.CommaSeparator:
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            return styleProperty.values[^1];
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
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var list = styleProperty.values.ToList();
#pragma warning restore UA2001
            list.RemoveRange(initialOffset, range);

            var currentOffset = initialOffset;
            for (var i = 0; i < part.handles.Count; ++i, ++currentOffset)
            {
                var propertyValue = StylePropertyPart.Create();
                propertyValue.offset = currentOffset;

                if (i == indices.valueIndex && typeof(Variable).IsAssignableFrom(typeof(T)) && value is Variable variable)
                {
                    styleProperty.GetManipulator(styleSheet).AddVariableReference(variable.name);
                    var handles = styleProperty.values[^3..];

                    var ib = 0;
                    var newPart = ResolveValueOrVariable(styleSheet, element, styleRule, styleProperty.name, handles.AsSpan(), ref ib,
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

                    propertyValue.handles.Add(new StylePropertyValue { sheet = styleSheet, handle = handle });

                    newParts.Add(propertyValue);
                }
            }

            if (part.isVariableUnresolved && indices.valueIndex == -1)
            {
                if (typeof(Variable).IsAssignableFrom(typeof(T)) && value is Variable variable)
                {
                    styleProperty.GetManipulator(styleSheet).AddVariableReference(variable.name);
                    var handles = styleProperty.values[^3..];
                    var newPart = StylePropertyPart.Create();
                    newPart.offset = currentOffset;
                    newPart.handles.Add( new StylePropertyValue
                    {
                        sheet = styleSheet,
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
                    newPart.handles.Add(new StylePropertyValue
                    {
                        sheet = styleSheet,
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
                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    var list = styleProperty.values.ToList();
#pragma warning restore UA2001
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

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var list = styleProperty.values.ToList();
#pragma warning restore UA2001
            list.RemoveRange(initialOffset, range);
            styleProperty.GetManipulator(styleSheet).AddVariableReference(variableName);
            var handles = styleProperty.values[^3..];
            list.InsertRange(initialOffset, handles);

            styleProperty.values = list.ToArray();
            var i = part.offset;
            var newPart = ResolveValueOrVariable(styleSheet, element, styleRule, styleProperty.name, styleProperty.values.AsSpan(), ref i, editorExtensionMode);
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

                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    var valueList = styleProperty.values.ToList();
#pragma warning restore UA2001

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
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var list = styleProperty.values.ToList();
#pragma warning restore UA2001
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

                    propertyValue.handles.Add(new StylePropertyValue { sheet = styleSheet, handle = handle });

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

                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var list = styleProperty.values.ToList();
#pragma warning restore UA2001

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
            string propertyName,
            Span<StyleValueHandle> propertyHandles,
            ref int currentIndex,
            bool isEditorExtensionMode)
        {
            var handle = propertyHandles[currentIndex];

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
                    part.handles.Add(new StylePropertyValue
                    {
                        sheet = styleSheet,
                        handle = handle
                    });
                    return part;
                }

                case StyleValueType.Function:
                {
                    var argCountHandle = propertyHandles[++currentIndex];
                    var argCount = (int)styleSheet.ReadFloat(argCountHandle);

                    if (argCount > 0)
                    {
                        var varHandle = propertyHandles[++currentIndex];
                        if (varHandle.valueType != StyleValueType.Variable)
                            return StylePropertyPart.Create();

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

                                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                                part.handles.AddRange(manipulator.stylePropertyParts.SelectMany(o => o.handles));
#pragma warning restore UA2001
                                part.isVariable = true;
                                part.variableName = variable;
                                return part;
                            }
                        }

                        // Skip comma and point to next function argument.
                        currentIndex += 2;

                        var fallbackPart = ResolveValueOrVariable(styleSheet, element, styleRule, propertyName, propertyHandles, ref currentIndex, isEditorExtensionMode);
                        fallbackPart.isVariable = true;
                        fallbackPart.variableName = variable;

                        return fallbackPart;
                    }

                    return StylePropertyPart.Create();
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
            if (customStyles.TryGetValue((UniqueStyleString)variableName, out var stylePropertyValue))
            {
                var propValue = stylePropertyValue;
                if (!editorExtensionMode && propValue.sheet.isDefaultStyleSheet)
                    return null;

                if (propValue.sheet.isDefaultStyleSheet)
                {
                    StyleVariableUtility.editorVariableDescriptions.TryGetValue(variableName, out var variableDescription);
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
                        var offset = ResolveValueOrVariable(propStyleSheet, currentVisualElement, styleRule, property.name, property.values.AsSpan(), ref i, editorExtensionMode);
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
            for (var ruleIndex = sheet.rules.Length - 1; ruleIndex >= 0; --ruleIndex)
            {
                var rule = sheet.rules[ruleIndex];
                for (var selectorIndex = rule.complexSelectors.Length - 1; selectorIndex >= 0; --selectorIndex)
                {
                    var complexSelector = rule.complexSelectors[selectorIndex];
                    if (!complexSelector.isSimple)
                        continue;

                    var simpleSelector = complexSelector.selectors[0];
                    var selectorPart = simpleSelector.parts[0];

                    if (selectorPart.type != StyleSelectorType.Wildcard &&
                        selectorPart.type != StyleSelectorType.PseudoClass)
                        continue;

                    if (selectorPart.type == StyleSelectorType.PseudoClass && selectorPart.value != "root")
                        continue;

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
                            var newPart = ResolveValueOrVariable(sheet, currentVisualElement, styleRule, property.name, property.values.AsSpan(), ref i, editorExtensionMode);
                            newPart.offset = index;
                            newPart.isVariable = true;
                            newPart.variableName = variableName;
                            manipulator.stylePropertyParts.Add(newPart);
                        }

                        return manipulator;
                    }
                }
            }

            return null;
        }

        StylePropertyPart ResolveVariable(StyleValueHandle[] handles)
        {
            var index = 0;
            return ResolveValueOrVariable(styleSheet, element, styleRule, propertyName, handles.AsSpan(), ref index,
                editorExtensionMode);
        }

        void EnsureStylePropertyExists()
        {
            m_StyleProperty
                ??= GetStyleProperty(styleRule, propertyName)
                ?? styleSheet.AddProperty(styleRule, propertyName);
        }

        static StyleProperty GetStyleProperty(StyleRule rule, string propertyName)
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return rule?.properties.LastOrDefault(property => property.name == propertyName);
#pragma warning restore UA2001
        }

        static StylePropertyPart CreatePart(StyleSheet styleSheet, StyleValueHandle handle, int offset)
        {
            var part = StylePropertyPart.Create();
            part.offset = offset;
            part.handles.Add(
                new StylePropertyValue
                {
                    sheet = styleSheet,
                    handle = handle
                });
            return part;
        }
    }
}
