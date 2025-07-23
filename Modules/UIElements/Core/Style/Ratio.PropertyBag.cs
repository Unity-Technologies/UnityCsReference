// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;

namespace UnityEngine.UIElements
{
    public partial struct Ratio
    {
        internal class PropertyBag : ContainerPropertyBag<Ratio>
        {
            class ValueProperty : Property<Ratio, float>
            {
                public override string Name { get; } = nameof(value);
                public override bool IsReadOnly { get; } = false;
                public override float GetValue(ref Ratio container) => container.value;
                public override void SetValue(ref Ratio container, float value) => throw new System.InvalidOperationException();

            }

            class AutoProperty : Property<Ratio, bool>
            {
                public override string Name { get; } = nameof(IsAuto);
                public override bool IsReadOnly { get; } = true;
                public override bool GetValue(ref Ratio container) => container.IsAuto();
                public override void SetValue(ref Ratio container, bool value) => throw new System.InvalidOperationException();

            }

            public PropertyBag()
            {
                AddProperty(new ValueProperty());
                AddProperty(new AutoProperty());
            }
        }
    }
}
