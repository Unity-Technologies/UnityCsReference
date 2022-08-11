// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;

namespace UnityEngine.UIElements
{
    public partial struct Translate
    {
        internal class PropertyBag : ContainerPropertyBag<Translate>
        {
            class XProperty : Property<Translate, Length>
            {
                public override string Name { get; } = nameof(x);
                public override bool IsReadOnly { get; } = false;
                public override Length GetValue(ref Translate container) => container.x;
                public override void SetValue(ref Translate container, Length value) => container.x = value;
            }

            class YProperty : Property<Translate, Length>
            {
                public override string Name { get; } = nameof(y);
                public override bool IsReadOnly { get; } = false;
                public override Length GetValue(ref Translate container) => container.y;
                public override void SetValue(ref Translate container, Length value) => container.y = value;
            }

            class ZProperty : Property<Translate, float>
            {
                public override string Name { get; } = nameof(z);
                public override bool IsReadOnly { get; } = false;
                public override float GetValue(ref Translate container) => container.z;
                public override void SetValue(ref Translate container, float value) => container.z = value;
            }

            public PropertyBag()
            {
                AddProperty(new XProperty());
                AddProperty(new YProperty());
                AddProperty(new ZProperty());
            }
        }
    }
}
