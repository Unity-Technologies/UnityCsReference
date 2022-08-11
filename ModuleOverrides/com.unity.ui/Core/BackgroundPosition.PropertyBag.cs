// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;

namespace UnityEngine.UIElements
{
    public partial struct BackgroundPosition
    {
        internal class PropertyBag : ContainerPropertyBag<BackgroundPosition>
        {
            class KeywordProperty : Property<BackgroundPosition, BackgroundPositionKeyword>
            {
                public override string Name { get; } = nameof(keyword);
                public override bool IsReadOnly { get; } = false;
                public override BackgroundPositionKeyword GetValue(ref BackgroundPosition container) => container.keyword;
                public override void SetValue(ref BackgroundPosition container, BackgroundPositionKeyword value) => container.keyword = value;
            }

            class OffsetProperty : Property<BackgroundPosition, Length>
            {
                public override string Name { get; } = nameof(offset);
                public override bool IsReadOnly { get; } = false;
                public override Length GetValue(ref BackgroundPosition container) => container.offset;
                public override void SetValue(ref BackgroundPosition container, Length value) => container.offset = value;
            }

            public PropertyBag()
            {
                AddProperty(new KeywordProperty());
                AddProperty(new OffsetProperty());
            }
        }
    }
}
