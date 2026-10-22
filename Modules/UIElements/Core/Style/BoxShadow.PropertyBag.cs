// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;

namespace UnityEngine.UIElements
{
    public partial struct BoxShadow
    {
        internal class PropertyBag : ContainerPropertyBag<BoxShadow>
        {
            class ColorProperty : Property<BoxShadow, Color>
            {
                public override string Name { get; } = nameof(color);
                public override bool IsReadOnly { get; } = false;
                public override Color GetValue(ref BoxShadow container) => container.color;
                public override void SetValue(ref BoxShadow container, Color value) => container.color = value;
            }

            class OffsetProperty : Property<BoxShadow, Vector2>
            {
                public override string Name { get; } = nameof(offset);
                public override bool IsReadOnly { get; } = false;
                public override Vector2 GetValue(ref BoxShadow container) => container.offset;
                public override void SetValue(ref BoxShadow container, Vector2 value) => container.offset = value;
            }

            class BlurRadiusProperty : Property<BoxShadow, float>
            {
                public override string Name { get; } = nameof(blurRadius);
                public override bool IsReadOnly { get; } = false;
                public override float GetValue(ref BoxShadow container) => container.blurRadius;
                public override void SetValue(ref BoxShadow container, float value) => container.blurRadius = value;
            }

            class SpreadRadiusProperty : Property<BoxShadow, float>
            {
                public override string Name { get; } = nameof(spreadRadius);
                public override bool IsReadOnly { get; } = false;
                public override float GetValue(ref BoxShadow container) => container.spreadRadius;
                public override void SetValue(ref BoxShadow container, float value) => container.spreadRadius = value;
            }

            class InsetProperty : Property<BoxShadow, bool>
            {
                public override string Name { get; } = nameof(inset);
                public override bool IsReadOnly { get; } = false;
                public override bool GetValue(ref BoxShadow container) => container.inset;
                public override void SetValue(ref BoxShadow container, bool value) => container.inset = value;
            }

            public PropertyBag()
                :base(5)
            {
                AddProperty(new ColorProperty());
                AddProperty(new OffsetProperty());
                AddProperty(new BlurRadiusProperty());
                AddProperty(new SpreadRadiusProperty());
                AddProperty(new InsetProperty());
            }
        }
    }
}
