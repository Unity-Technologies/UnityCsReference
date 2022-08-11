// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;

namespace UnityEngine.UIElements
{
    public partial struct TextShadow
    {
        internal class PropertyBag : ContainerPropertyBag<TextShadow>
        {
            class OffsetProperty : Property<TextShadow, Vector2>
            {
                public override string Name { get; } = nameof(offset);
                public override bool IsReadOnly { get; } = false;
                public override Vector2 GetValue(ref TextShadow container) => container.offset;
                public override void SetValue(ref TextShadow container, Vector2 value) => container.offset = value;
            }

            class BlurRadiusProperty : Property<TextShadow, float>
            {
                public override string Name { get; } = nameof(blurRadius);
                public override bool IsReadOnly { get; } = false;
                public override float GetValue(ref TextShadow container) => container.blurRadius;
                public override void SetValue(ref TextShadow container, float value) => container.blurRadius = value;
            }

            class ColorProperty : Property<TextShadow, Color>
            {
                public override string Name { get; } = nameof(color);
                public override bool IsReadOnly { get; } = false;
                public override Color GetValue(ref TextShadow container) => container.color;
                public override void SetValue(ref TextShadow container, Color value) => container.color = value;
            }

            public PropertyBag()
            {
                AddProperty(new OffsetProperty());
                AddProperty(new BlurRadiusProperty());
                AddProperty(new ColorProperty());
            }
        }
    }
}
