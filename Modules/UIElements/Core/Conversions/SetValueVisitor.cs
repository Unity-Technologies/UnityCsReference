// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using Unity.Properties;

namespace UnityEngine.UIElements
{
    internal partial class SetValueVisitor<TSrcValue> : PathVisitor
    {
        [NoAutoStaticsCleanup]
        public static readonly UnityEngine.Pool.ObjectPool<SetValueVisitor<TSrcValue>> Pool = new (() => new SetValueVisitor<TSrcValue>(), actionOnRelease: v => v.Reset());
        public TSrcValue Value;

        public ConverterGroup group { get; set; }
        public bool createTargetWriter { get; set; }
        public CachedTargetWriter<TSrcValue> targetWriter { get; set; }

        public override void Reset()
        {
            base.Reset();
            Value = default;
            group = default;
            createTargetWriter = false;
            targetWriter = null;
        }

        protected override void VisitPath<TContainer, TValue>(Property<TContainer, TValue> property, ref TContainer container, ref TValue value)
        {
            if (createTargetWriter)
                targetWriter = CachedTargetWriter<TSrcValue>.GetOrCreate(property);

            if (property.IsReadOnly)
            {
                ReturnCode = VisitReturnCode.AccessViolation;
                return;
            }

            if (null != group && group.TryConvert(ref Value, out TValue local))
            {
                property.SetValue(ref container, local);
            }
            else if (ConverterGroups.TryConvert(ref Value, out TValue global))
            {
                property.SetValue(ref container, global);
            }
            else
            {
                ReturnCode = VisitReturnCode.InvalidCast;
            }
        }
    }

    // Sets a property of a container without visiting it, for paths made of a single property name. Writers are
    // created by the regular visitation and are shared between all the writes of the same value type to the same
    // property of the same container type.
    internal abstract partial class CachedTargetWriter<TSrcValue>
    {
        [AutoStaticsCleanupOnCodeReload]
        static readonly Dictionary<(Type, string), CachedTargetWriter<TSrcValue>> s_Writers = new();

        public readonly Type containerType;
        public readonly string propertyName;

        protected CachedTargetWriter(Type containerType, string propertyName)
        {
            this.containerType = containerType;
            this.propertyName = propertyName;
        }

        // Returns false when the writer is stale and the value must be set through visitation instead.
        public abstract bool TrySetValue(ConverterGroup group, object target, TSrcValue value, out VisitReturnCode returnCode);

        // Returns the writer shared by all the writes of this type of value to the property of the container type, if any.
        public static CachedTargetWriter<TSrcValue> GetShared(Type containerType, string propertyName)
        {
            return s_Writers.TryGetValue((containerType, propertyName), out var writer) ? writer : null;
        }

        public static CachedTargetWriter<TSrcValue> GetOrCreate<TContainer, TValue>(Property<TContainer, TValue> property)
        {
            // Only container property bags have a fixed set of properties that doesn't depend on the instance.
            if (PropertyBag.GetPropertyBag<TContainer>() is not ContainerPropertyBag<TContainer> propertyBag)
                return null;

            var key = (typeof(TContainer), property.Name);
            if (s_Writers.TryGetValue(key, out var writer) &&
                writer is CachedTargetWriter<TSrcValue, TContainer, TValue> typedWriter &&
                typedWriter.Matches(propertyBag, property))
                return writer;

            writer = new CachedTargetWriter<TSrcValue, TContainer, TValue>(propertyBag, property);
            s_Writers[key] = writer;
            return writer;
        }
    }

    sealed class CachedTargetWriter<TSrcValue, TContainer, TValue> : CachedTargetWriter<TSrcValue>
    {
        readonly IPropertyBag<TContainer> m_PropertyBag;
        readonly Property<TContainer, TValue> m_Property;

        public CachedTargetWriter(IPropertyBag<TContainer> propertyBag, Property<TContainer, TValue> property)
            : base(typeof(TContainer), property.Name)
        {
            m_PropertyBag = propertyBag;
            m_Property = property;
        }

        public bool Matches(IPropertyBag<TContainer> propertyBag, Property<TContainer, TValue> property)
        {
            return ReferenceEquals(m_PropertyBag, propertyBag) && ReferenceEquals(m_Property, property);
        }

        // Same as SetValueVisitor.VisitPath.
        public override bool TrySetValue(ConverterGroup group, object target, TSrcValue value, out VisitReturnCode returnCode)
        {
            // The property bag of a type can be replaced, which would make the cached property stale.
            if (!ReferenceEquals(PropertyBag.GetPropertyBag<TContainer>(), m_PropertyBag))
            {
                returnCode = default;
                return false;
            }

            if (m_Property.IsReadOnly)
            {
                returnCode = VisitReturnCode.AccessViolation;
                return true;
            }

            var container = (TContainer)target;
            if (null != group && group.TryConvert(ref value, out TValue local))
            {
                m_Property.SetValue(ref container, local);
            }
            else if (ConverterGroups.TryConvert(ref value, out TValue global))
            {
                m_Property.SetValue(ref container, global);
            }
            else
            {
                returnCode = VisitReturnCode.InvalidCast;
                return true;
            }

            returnCode = VisitReturnCode.Ok;
            return true;
        }
    }
}
