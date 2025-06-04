// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Properties;

namespace UnityEngine.UIElements
{
    public partial struct TextAutoSize
    {
        internal class PropertyBag : ContainerPropertyBag<TextAutoSize>
        {
            class ModeProperty : Property<TextAutoSize, TextAutoSizeMode>
            {
                public override string Name { get; } = nameof(mode);
                public override bool IsReadOnly { get; } = false;
                public override TextAutoSizeMode GetValue(ref TextAutoSize container) => container.mode;
                public override void SetValue(ref TextAutoSize container, TextAutoSizeMode value) => container.mode = value;
            }

            class MinSizeProperty : Property<TextAutoSize, Length>
            {
                public override string Name { get; } = nameof(minSize);
                public override bool IsReadOnly { get; } = false;
                public override Length GetValue(ref TextAutoSize container) => container.minSize;
                public override void SetValue(ref TextAutoSize container, Length value) => container.minSize = value;
            }

            class MaxSizeProperty : Property<TextAutoSize, Length>
            {
                public override string Name { get; } = nameof(maxSize);
                public override bool IsReadOnly { get; } = false;
                public override Length GetValue(ref TextAutoSize container) => container.maxSize;
                public override void SetValue(ref TextAutoSize container, Length value) => container.maxSize = value;
            }

            public PropertyBag()
            {
                AddProperty(new ModeProperty());
                AddProperty(new MinSizeProperty());
                AddProperty(new MaxSizeProperty());
            }
        }
    }
}
