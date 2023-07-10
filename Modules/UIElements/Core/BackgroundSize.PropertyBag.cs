// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;

namespace UnityEngine.UIElements
{
    public partial struct BackgroundSize
    {
        internal class PropertyBag : ContainerPropertyBag<BackgroundSize>
        {
            class SizeTypeProperty : Property<BackgroundSize, BackgroundSizeType>
            {
                public override string Name { get; } = nameof(sizeType);
                public override bool IsReadOnly { get; } = false;
                public override BackgroundSizeType GetValue(ref BackgroundSize container) => container.sizeType;
                public override void SetValue(ref BackgroundSize container, BackgroundSizeType value) => container.sizeType = value;
            }

            class XProperty : Property<BackgroundSize, Length>
            {
                public override string Name { get; } = nameof(x);
                public override bool IsReadOnly { get; } = false;
                public override Length GetValue(ref BackgroundSize container) => container.x;
                public override void SetValue(ref BackgroundSize container, Length value) => container.x = value;
            }

            class YProperty : Property<BackgroundSize, Length>
            {
                public override string Name { get; } = nameof(y);
                public override bool IsReadOnly { get; } = false;
                public override Length GetValue(ref BackgroundSize container) => container.y;
                public override void SetValue(ref BackgroundSize container, Length value) => container.y = value;
            }

            public PropertyBag()
            {
                AddProperty(new SizeTypeProperty());
                AddProperty(new XProperty());
                AddProperty(new YProperty());
            }
        }
    }
}
