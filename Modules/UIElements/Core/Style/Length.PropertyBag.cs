// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;

namespace UnityEngine.UIElements
{
    public partial struct Length
    {
        internal class PropertyBag : ContainerPropertyBag<Length>
        {
            class ValueProperty : Property<Length, float>
            {
                public override string Name { get; } = nameof(value);
                public override bool IsReadOnly { get; } = false;
                public override float GetValue(ref Length container) => container.value;
                public override void SetValue(ref Length container, float value) => container.value = value;
            }

            class UnitProperty : Property<Length, LengthUnit>
            {
                public override string Name { get; } = nameof(unit);
                public override bool IsReadOnly { get; } = false;
                public override LengthUnit GetValue(ref Length container) => container.unit;
                public override void SetValue(ref Length container, LengthUnit value) => container.unit = value;
            }

            public PropertyBag()
            {
                AddProperty(new ValueProperty());
                AddProperty(new UnitProperty());
            }
        }
    }
}
