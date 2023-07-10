// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;
using UnityEngine.TextCore.Text;

namespace UnityEngine.UIElements
{
    public partial struct FontDefinition
    {
        internal class PropertyBag : ContainerPropertyBag<FontDefinition>
        {
            class FontProperty : Property<FontDefinition, Font>
            {
                public override string Name { get; } = nameof(font);
                public override bool IsReadOnly { get; } = false;
                public override Font GetValue(ref FontDefinition container) => container.font;
                public override void SetValue(ref FontDefinition container, Font value) => container.font = value;
            }

            class FontAssetProperty : Property<FontDefinition, FontAsset>
            {
                public override string Name { get; } = nameof(fontAsset);
                public override bool IsReadOnly { get; } = false;
                public override FontAsset GetValue(ref FontDefinition container) => container.fontAsset;
                public override void SetValue(ref FontDefinition container, FontAsset value) => container.fontAsset = value;
            }

            public PropertyBag()
            {
                AddProperty(new FontProperty());
                AddProperty(new FontAssetProperty());
            }
        }
    }
}
