// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;

namespace Unity.Entities.Serialization
{
    interface IPropertyWrapper
    {
        
    }

    struct PropertyWrapper<T> : IPropertyWrapper
    {
        class PropertyBag : ContainerPropertyBag<PropertyWrapper<T>>, IPropertyWrapper
        {
            Property Property { get; } = new Property();
            
            public PropertyBag()
                => AddProperty(Property);
        }
        
        class Property : Property<PropertyWrapper<T>, T>, IPropertyWrapper
        {
            public override string Name => nameof(Value);
            public override bool IsReadOnly => false;
            public override T GetValue(ref PropertyWrapper<T> container) => container.Value;
            public override void SetValue(ref PropertyWrapper<T> container, T value) => container.Value = value;
        }

        public T Value;

        public PropertyWrapper(T value)
        {
            Value = value;
        }

        // Kept out of the constructor: registering into the property bag store is a side effect on its statics.
        public static void EnsurePropertyBag()
        {
            if (!Unity.Properties.PropertyBag.Exists<PropertyWrapper<T>>())
            {
                Unity.Properties.PropertyBag.Register(new PropertyBag());
            }
        }
    }
}
