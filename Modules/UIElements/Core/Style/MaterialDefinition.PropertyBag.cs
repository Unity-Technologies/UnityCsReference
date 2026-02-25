// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using Unity.Properties;

namespace UnityEngine.UIElements
{
    public partial struct MaterialDefinition
    {
        internal class PropertyBag : ContainerPropertyBag<MaterialDefinition>
        {
            class MaterialProperty : Property<MaterialDefinition, Material>
            {
                public override string Name { get; } = nameof(material);
                public override bool IsReadOnly { get; } = false;
                public override Material GetValue(ref MaterialDefinition container) => container.material;
                public override void SetValue(ref MaterialDefinition container, Material value) => container.material = value;
            }

            public PropertyBag()
                :base(1)
            {
                AddProperty(new MaterialProperty());
            }
        }
    }
}
