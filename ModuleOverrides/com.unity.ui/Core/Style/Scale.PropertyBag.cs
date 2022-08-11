// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;

namespace UnityEngine.UIElements
{
    public partial struct Scale
    {
        internal class PropertyBag : ContainerPropertyBag<Scale>
        {
            class ValueProperty : Property<Scale, Vector3>
            {
                public override string Name { get; } = nameof(value);
                public override bool IsReadOnly { get; } = false;
                public override Vector3 GetValue(ref Scale container) => container.value;
                public override void SetValue(ref Scale container, Vector3 value) => container.value = value;
            }

            public PropertyBag()
            {
                AddProperty(new ValueProperty());
            }
        }
    }
}
