// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;

namespace UnityEngine.UIElements
{
    public partial struct Angle
    {
        internal class PropertyBag : ContainerPropertyBag<Angle>
        {
            class ValueProperty : Property<Angle, float>
            {
                public override string Name { get; } = nameof(value);
                public override bool IsReadOnly { get; } = false;
                public override float GetValue(ref Angle container) => container.value;
                public override void SetValue(ref Angle container, float value) => container.value = value;
            }

            class UnitProperty : Property<Angle, AngleUnit>
            {
                public override string Name { get; } = nameof(unit);
                public override bool IsReadOnly { get; } = false;
                public override AngleUnit GetValue(ref Angle container) => container.unit;
                public override void SetValue(ref Angle container, AngleUnit value) => container.unit = value;
            }

            public PropertyBag()
            {
                AddProperty(new ValueProperty());
                AddProperty(new UnitProperty());
            }
        }
    }
}
