// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;

namespace UnityEngine.UIElements
{
    public partial struct BackgroundRepeat
    {
        internal class PropertyBag : ContainerPropertyBag<BackgroundRepeat>
        {
            class XProperty : Property<BackgroundRepeat, Repeat>
            {
                public override string Name { get; } = nameof(x);
                public override bool IsReadOnly { get; } = false;
                public override Repeat GetValue(ref BackgroundRepeat container) => container.x;
                public override void SetValue(ref BackgroundRepeat container, Repeat value) => container.x = value;
            }

            class YProperty : Property<BackgroundRepeat, Repeat>
            {
                public override string Name { get; } = nameof(y);
                public override bool IsReadOnly { get; } = false;
                public override Repeat GetValue(ref BackgroundRepeat container) => container.y;
                public override void SetValue(ref BackgroundRepeat container, Repeat value) => container.y = value;
            }

            public PropertyBag()
            {
                AddProperty(new XProperty());
                AddProperty(new YProperty());
            }
        }
    }
}
