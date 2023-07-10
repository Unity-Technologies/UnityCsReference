// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;

namespace UnityEngine.UIElements
{
    public partial struct Cursor
    {
        internal class PropertyBag : ContainerPropertyBag<Cursor>
        {
            class TextureProperty : Property<Cursor, Texture2D>
            {
                public override string Name { get; } = nameof(texture);
                public override bool IsReadOnly { get; } = false;
                public override Texture2D GetValue(ref Cursor container) => container.texture;
                public override void SetValue(ref Cursor container, Texture2D value) => container.texture = value;
            }

            class HotspotProperty : Property<Cursor, Vector2>
            {
                public override string Name { get; } = nameof(hotspot);
                public override bool IsReadOnly { get; } = false;
                public override Vector2 GetValue(ref Cursor container) => container.hotspot;
                public override void SetValue(ref Cursor container, Vector2 value) => container.hotspot = value;
            }

            class DefaultCursorIdProperty : Property<Cursor, int>
            {
                public override string Name { get; } = nameof(defaultCursorId);
                public override bool IsReadOnly { get; } = false;
                public override int GetValue(ref Cursor container) => container.defaultCursorId;
                public override void SetValue(ref Cursor container, int value) => container.defaultCursorId = value;
            }

            public PropertyBag()
            {
                AddProperty(new TextureProperty());
                AddProperty(new HotspotProperty());
                AddProperty(new DefaultCursorIdProperty());
            }
        }
    }
}
