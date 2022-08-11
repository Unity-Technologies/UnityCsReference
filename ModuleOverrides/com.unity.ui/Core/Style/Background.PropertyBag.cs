// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;

namespace UnityEngine.UIElements
{
    public partial struct Background
    {
        internal class PropertyBag : ContainerPropertyBag<Background>
        {
            class TextureProperty : Property<Background, Texture2D>
            {
                public override string Name { get; } = nameof(texture);
                public override bool IsReadOnly { get; } = false;
                public override Texture2D GetValue(ref Background container) => container.texture;
                public override void SetValue(ref Background container, Texture2D value) => container.texture = value;
            }

            class SpriteProperty : Property<Background, Sprite>
            {
                public override string Name { get; } = nameof(sprite);
                public override bool IsReadOnly { get; } = false;
                public override Sprite GetValue(ref Background container) => container.sprite;
                public override void SetValue(ref Background container, Sprite value) => container.sprite = value;
            }

            class RenderTextureProperty : Property<Background, RenderTexture>
            {
                public override string Name { get; } = nameof(renderTexture);
                public override bool IsReadOnly { get; } = false;
                public override RenderTexture GetValue(ref Background container) => container.renderTexture;
                public override void SetValue(ref Background container, RenderTexture value) => container.renderTexture = value;
            }

            class VectorImageProperty : Property<Background, VectorImage>
            {
                public override string Name { get; } = nameof(vectorImage);
                public override bool IsReadOnly { get; } = false;
                public override VectorImage GetValue(ref Background container) => container.vectorImage;
                public override void SetValue(ref Background container, VectorImage value) => container.vectorImage = value;
            }

            public PropertyBag()
            {
                AddProperty(new TextureProperty());
                AddProperty(new SpriteProperty());
                AddProperty(new RenderTextureProperty());
                AddProperty(new VectorImageProperty());
            }
        }
    }
}
