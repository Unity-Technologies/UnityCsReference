// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;

namespace UnityEngine.UIElements
{
    public partial struct Curvature
    {
        internal class PropertyBag : ContainerPropertyBag<Curvature>
        {
            class XProperty : Property<Curvature, Angle>
            {
                public override string Name { get; } = nameof(x);
                public override bool IsReadOnly { get; } = false;
                public override Angle GetValue(ref Curvature container) => container.x;
                public override void SetValue(ref Curvature container, Angle value) => container.x = value;
            }

            class YProperty : Property<Curvature, Angle>
            {
                public override string Name { get; } = nameof(y);
                public override bool IsReadOnly { get; } = false;
                public override Angle GetValue(ref Curvature container) => container.y;
                public override void SetValue(ref Curvature container, Angle value) => container.y = value;
            }

            public PropertyBag()
                : base(2)
            {
                AddProperty(new XProperty());
                AddProperty(new YProperty());
            }
        }
    }
}
