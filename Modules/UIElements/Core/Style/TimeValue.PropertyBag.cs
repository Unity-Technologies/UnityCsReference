// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;

namespace UnityEngine.UIElements
{
    public partial struct TimeValue
    {
        internal class PropertyBag : ContainerPropertyBag<TimeValue>
        {
            class ValueProperty : Property<TimeValue, float>
            {
                public override string Name { get; } = nameof(value);
                public override bool IsReadOnly { get; } = false;
                public override float GetValue(ref TimeValue container) => container.value;
                public override void SetValue(ref TimeValue container, float value) => container.value = value;
            }

            class UnitProperty : Property<TimeValue, TimeUnit>
            {
                public override string Name { get; } = nameof(unit);
                public override bool IsReadOnly { get; } = false;
                public override TimeUnit GetValue(ref TimeValue container) => container.unit;
                public override void SetValue(ref TimeValue container, TimeUnit value) => container.unit = value;
            }

            public PropertyBag()
            {
                AddProperty(new ValueProperty());
                AddProperty(new UnitProperty());
            }
        }
    }
}
