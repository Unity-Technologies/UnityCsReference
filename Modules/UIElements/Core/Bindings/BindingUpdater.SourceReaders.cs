// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Properties;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.UIElements
{
    partial class BindingUpdater
    {
        // Reads the value at a data source path made of property names and list indices and passes it to a binding,
        // without visiting the data source. There is one step per path part. Each step checks the runtime type and the
        // property bag of its container and the path part it reads, so a reader can be kept between updates: any
        // mismatch makes the caller fall back to the regular visitation, which also reports the errors. Steps only
        // call the binding once every check passed.
        internal abstract class SourceReadStep<TContainer>
        {
            // Passing a null binding only checks whether the reader can read the value.
            public abstract bool TryUpdateUI(ref TContainer container, in PropertyPath path, int partIndex, DataBinding binding, in BindingContext context, out BindingResult result);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static bool TryCast<TInput, TTarget>(ref TInput input, out TTarget target)
            {
                if (typeof(TInput).IsValueType && typeof(TInput) == typeof(TTarget))
                {
                    target = UnsafeUtility.As<TInput, TTarget>(ref input);
                    return true;
                }

                // The visitation uses the property bag of the runtime type.
                object boxed = input;
                if (null == boxed || boxed.GetType() != typeof(TTarget))
                {
                    target = default;
                    return false;
                }

                target = (TTarget)boxed;
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static bool IsExpectedPart(in PropertyPath path, int partIndex, bool isLast)
            {
                return isLast ? partIndex == path.Length - 1 : partIndex < path.Length - 1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static bool Complete<TValue>(ref TValue value, SourceReadStep<TValue> next, in PropertyPath path, int partIndex, DataBinding binding, in BindingContext context, out BindingResult result)
            {
                if (null != next)
                    return next.TryUpdateUI(ref value, path, partIndex + 1, binding, context, out result);

                result = null != binding ? binding.UpdateUI(in context, ref value) : default;
                return true;
            }
        }

        sealed class NameReadStep<TInput, TContainer, TValue> : SourceReadStep<TInput>
        {
            readonly IPropertyBag<TContainer> m_PropertyBag;
            readonly Property<TContainer, TValue> m_Property;
            readonly string m_Name;
            readonly SourceReadStep<TValue> m_Next;

            public NameReadStep(IPropertyBag<TContainer> propertyBag, Property<TContainer, TValue> property, SourceReadStep<TValue> next)
            {
                m_PropertyBag = propertyBag;
                m_Property = property;
                m_Name = property.Name;
                m_Next = next;
            }

            public override bool TryUpdateUI(ref TInput input, in PropertyPath path, int partIndex, DataBinding binding, in BindingContext context, out BindingResult result)
            {
                result = default;
                if (!IsExpectedPart(path, partIndex, null == m_Next))
                    return false;

                var part = path[partIndex];
                if (part.Kind != PropertyPathPartKind.Name || part.Name != m_Name)
                    return false;

                // The property bag of a type can be replaced, which would make the cached property stale.
                if (!TryCast(ref input, out TContainer container) || !ReferenceEquals(PropertyBag.GetPropertyBag<TContainer>(), m_PropertyBag))
                    return false;

                var value = m_Property.GetValue(ref container);
                return Complete(ref value, m_Next, path, partIndex, binding, context, out result);
            }
        }

        sealed class IndexReadStep<TInput, TList, TElement> : SourceReadStep<TInput>
            where TList : IList<TElement>
        {
            readonly IPropertyBag<TList> m_PropertyBag;
            readonly SourceReadStep<TElement> m_Next;

            public IndexReadStep(IPropertyBag<TList> propertyBag, SourceReadStep<TElement> next)
            {
                m_PropertyBag = propertyBag;
                m_Next = next;
            }

            public override bool TryUpdateUI(ref TInput input, in PropertyPath path, int partIndex, DataBinding binding, in BindingContext context, out BindingResult result)
            {
                result = default;
                if (!IsExpectedPart(path, partIndex, null == m_Next))
                    return false;

                var part = path[partIndex];
                if (part.Kind != PropertyPathPartKind.Index)
                    return false;

                if (!TryCast(ref input, out TList list) || !ReferenceEquals(PropertyBag.GetPropertyBag<TList>(), m_PropertyBag))
                    return false;

                // Out of range indices are left to the visitation.
                var index = part.Index;
                if ((uint)index >= (uint)list.Count)
                    return false;

                var value = list[index];
                return Complete(ref value, m_Next, path, partIndex, binding, context, out result);
            }
        }

        // Kept on a binding whose path can be visited but not read by a reader, to avoid trying to build one on every
        // update. It is replaced when the type of the data source or the path changes.
        sealed class UnsupportedSourceRead : SourceReadStep<object>
        {
            public readonly Type sourceType;
            public readonly PropertyPath path;

            public UnsupportedSourceRead(Type sourceType, in PropertyPath path)
            {
                this.sourceType = sourceType;
                this.path = path;
            }

            public override bool TryUpdateUI(ref object container, in PropertyPath path, int partIndex, DataBinding binding, in BindingContext context, out BindingResult result)
            {
                result = default;
                return false;
            }
        }

        // Kept on a binding for which no shared reader existed, so it doesn't look for one on every update. A successful
        // visitation replaces it with a reader.
        sealed class NoSharedSourceRead : SourceReadStep<object>
        {
            public override bool TryUpdateUI(ref object container, in PropertyPath path, int partIndex, DataBinding binding, in BindingContext context, out BindingResult result)
            {
                result = default;
                return false;
            }
        }

        [NoAutoStaticsCleanup]
        static readonly SourceReadStep<object> s_NoSharedSourceRead = new NoSharedSourceRead();

        // Creates the steps for the remaining path parts, given the declared type of the container of the current part.
        interface ISourceReadStepFactory
        {
            SourceReadStep<TInput> Create<TInput>();
        }

        sealed class NameReadStepFactory<TContainer, TValue> : ISourceReadStepFactory
        {
            readonly IPropertyBag<TContainer> m_PropertyBag;
            readonly Property<TContainer, TValue> m_Property;
            readonly ISourceReadStepFactory m_Next;

            public NameReadStepFactory(IPropertyBag<TContainer> propertyBag, Property<TContainer, TValue> property, ISourceReadStepFactory next)
            {
                m_PropertyBag = propertyBag;
                m_Property = property;
                m_Next = next;
            }

            public SourceReadStep<TInput> Create<TInput>()
            {
                return new NameReadStep<TInput, TContainer, TValue>(m_PropertyBag, m_Property, m_Next?.Create<TValue>());
            }
        }

        sealed class IndexReadStepFactory<TList, TElement> : ISourceReadStepFactory
            where TList : IList<TElement>
        {
            readonly IPropertyBag<TList> m_PropertyBag;
            readonly ISourceReadStepFactory m_Next;

            public IndexReadStepFactory(IPropertyBag<TList> propertyBag, ISourceReadStepFactory next)
            {
                m_PropertyBag = propertyBag;
                m_Next = next;
            }

            public SourceReadStep<TInput> Create<TInput>()
            {
                return new IndexReadStep<TInput, TList, TElement>(m_PropertyBag, m_Next?.Create<TElement>());
            }
        }

        // Walks a path like PathVisitor does and returns the factory of the steps reading it, or null when a part can't
        // be read by a step. Only container property bags (whose properties don't depend on the instance) and indexed
        // collection property bags (lists and arrays) are supported.
        sealed class SourceReadStepBuilder : IPropertyBagVisitor, IPropertyVisitor, IListPropertyBagVisitor
        {
            PropertyPath m_Path;
            int m_PartIndex;
            object m_PropertyBag;
            ISourceReadStepFactory m_Result;

            public ISourceReadStepFactory Build(object source, in PropertyPath path)
            {
                m_Path = path;
                m_PartIndex = 0;
                m_Result = null;
                var result = PropertyContainer.TryAccept(this, ref source) ? m_Result : null;
                m_Path = default;
                m_PropertyBag = null;
                m_Result = null;
                return result;
            }

            void IPropertyBagVisitor.Visit<TContainer>(IPropertyBag<TContainer> properties, ref TContainer container)
            {
                m_Result = null;
                var part = m_Path[m_PartIndex];
                if (part.Kind != PropertyPathPartKind.Name ||
                    properties is not ContainerPropertyBag<TContainer> containerProperties ||
                    !containerProperties.TryGetProperty(ref container, part.Name, out var property))
                    return;

                m_PropertyBag = containerProperties;
                property.Accept(this, ref container);
            }

            void IPropertyVisitor.Visit<TContainer, TValue>(Property<TContainer, TValue> property, ref TContainer container)
            {
                var propertyBag = (IPropertyBag<TContainer>)m_PropertyBag;
                ISourceReadStepFactory next = null;
                if (m_PartIndex < m_Path.Length - 1)
                {
                    var value = property.GetValue(ref container);
                    next = BuildNext(ref value);
                    if (null == next)
                    {
                        m_Result = null;
                        return;
                    }
                }

                m_Result = new NameReadStepFactory<TContainer, TValue>(propertyBag, property, next);
            }

            void IListPropertyBagVisitor.Visit<TList, TElement>(IListPropertyBag<TList, TElement> properties, ref TList container)
            {
                m_Result = null;
                var part = m_Path[m_PartIndex];
                if (part.Kind != PropertyPathPartKind.Index ||
                    properties is not IndexedCollectionPropertyBag<TList, TElement> ||
                    (uint)part.Index >= (uint)container.Count)
                    return;

                ISourceReadStepFactory next = null;
                if (m_PartIndex < m_Path.Length - 1)
                {
                    var value = container[part.Index];
                    next = BuildNext(ref value);
                    if (null == next)
                        return;
                }

                m_Result = new IndexReadStepFactory<TList, TElement>(properties, next);
            }

            ISourceReadStepFactory BuildNext<TValue>(ref TValue value)
            {
                if (TypeTraits.CanBeNull(typeof(TValue)) && EqualityComparer<TValue>.Default.Equals(value, default))
                    return null;

                ++m_PartIndex;
                m_Result = null;
                var result = PropertyContainer.TryAccept(this, ref value) ? m_Result : null;
                --m_PartIndex;
                return result;
            }
        }

        [NoAutoStaticsCleanup]
        static readonly SourceReadStepBuilder s_SourceReadStepBuilder = new();

        // Readers are shared between bindings reading the same path shape (indices excluded) of the same source type,
        // to avoid allocating new ones when bindings are registered again.
        [AutoStaticsCleanupOnCodeReload]
        static readonly Dictionary<(Type, PropertyPath), SourceReadStep<object>> s_SourceReaders = new();

        static bool IsSupportedSourcePath(in PropertyPath path, out bool hasIndex)
        {
            hasIndex = false;
            if (path.IsEmpty)
                return false;

            for (var i = 0; i < path.Length; ++i)
            {
                var kind = path[i].Kind;
                if (kind == PropertyPathPartKind.Index)
                    hasIndex = true;
                else if (kind != PropertyPathPartKind.Name)
                    return false;
            }

            return true;
        }

        static PropertyPath GetSourceReaderKeyPath(in PropertyPath path, bool hasIndex)
        {
            if (!hasIndex)
                return path;

            var keyPath = default(PropertyPath);
            for (var i = 0; i < path.Length; ++i)
            {
                var part = path[i];
                keyPath = part.Kind == PropertyPathPartKind.Index
                    ? PropertyPath.AppendIndex(keyPath, 0)
                    : PropertyPath.AppendPart(keyPath, part);
            }

            return keyPath;
        }

        // Returns the reader shared by other bindings reading the same path shape of the same source type, if any.
        static SourceReadStep<object> GetSharedSourceReader(object source, in PropertyPath path)
        {
            if (!IsSupportedSourcePath(path, out var hasIndex))
                return null;

            return s_SourceReaders.TryGetValue((source.GetType(), GetSourceReaderKeyPath(path, hasIndex)), out var reader) ? reader : null;
        }

        // Called after a successful visitation of the path. Returns the reader to keep for the next update.
        static SourceReadStep<object> GetOrCreateSourceReader(object source, in PropertyPath path, SourceReadStep<object> previous, in BindingContext context)
        {
            if (!IsSupportedSourcePath(path, out var hasIndex))
                return null;

            var sourceType = source.GetType();
            if (previous is UnsupportedSourceRead unsupported && unsupported.sourceType == sourceType && unsupported.path == path)
                return previous;

            var key = (sourceType, GetSourceReaderKeyPath(path, hasIndex));
            if (s_SourceReaders.TryGetValue(key, out var reader) &&
                reader != previous &&
                reader.TryUpdateUI(ref source, path, 0, null, context, out _))
                return reader;

            var factory = s_SourceReadStepBuilder.Build(source, path);
            if (null == factory)
                return new UnsupportedSourceRead(sourceType, path);

            reader = factory.Create<object>();
            s_SourceReaders[key] = reader;
            return reader;
        }
    }
}
