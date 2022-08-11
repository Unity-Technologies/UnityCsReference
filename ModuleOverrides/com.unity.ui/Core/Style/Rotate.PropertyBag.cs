// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;

namespace UnityEngine.UIElements
{
    public partial struct Rotate
    {
        internal class PropertyBag : ContainerPropertyBag<Rotate>
        {
            class AngleProperty : Property<Rotate, Angle>
            {
                public override string Name { get; } = nameof(angle);
                public override bool IsReadOnly { get; } = false;
                public override Angle GetValue(ref Rotate container) => container.angle;
                public override void SetValue(ref Rotate container, Angle value) => container.angle = value;
            }

            class AxisProperty : Property<Rotate, Vector3>
            {
                public override string Name { get; } = nameof(axis);
                public override bool IsReadOnly { get; } = false;
                public override Vector3 GetValue(ref Rotate container) => container.axis;
                public override void SetValue(ref Rotate container, Vector3 value) => container.axis = value;
            }

            public PropertyBag()
            {
                AddProperty(new AngleProperty());
                AddProperty(new AxisProperty());
            }
        }
    }
}
