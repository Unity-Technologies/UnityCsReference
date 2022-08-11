// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;

namespace UnityEngine.UIElements
{
    public partial struct EasingFunction
    {
        internal class PropertyBag : ContainerPropertyBag<EasingFunction>
        {
            class ModeProperty : Property<EasingFunction, EasingMode>
            {
                public override string Name { get; } = nameof(mode);
                public override bool IsReadOnly { get; } = false;
                public override EasingMode GetValue(ref EasingFunction container) => container.mode;
                public override void SetValue(ref EasingFunction container, EasingMode value) => container.mode = value;
            }

            public PropertyBag()
            {
                AddProperty(new ModeProperty());
            }
        }
    }
}
