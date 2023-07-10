// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    public partial struct StylePropertyName
    {
        internal class PropertyBag : ContainerPropertyBag<StylePropertyName>
        {
            class IdProperty : Property<StylePropertyName, StylePropertyId>
            {
                public override string Name { get; } = nameof(id);
                public override bool IsReadOnly { get; } = true;
                public override StylePropertyId GetValue(ref StylePropertyName container) => container.id;
                public override void SetValue(ref StylePropertyName container, StylePropertyId value) {}
            }

            class NameProperty : Property<StylePropertyName, string>
            {
                public override string Name { get; } = nameof(name);
                public override bool IsReadOnly { get; } = true;
                public override string GetValue(ref StylePropertyName container) => container.name;
                public override void SetValue(ref StylePropertyName container, string value) {}
            }

            public PropertyBag()
            {
                AddProperty(new IdProperty());
                AddProperty(new NameProperty());
            }
        }
    }
}
