// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;
using UnityEngine.Scripting;

namespace UnityEngine.UIElements
{
    class StyleValuePropertyBag<TContainer, TValue> : ContainerPropertyBag<TContainer>
        where TContainer : IStyleValue<TValue>
    {
        class ValueProperty : Property<TContainer, TValue>
        {
            public override string Name { get; } = nameof(IStyleValue<TValue>.value);
            public override bool IsReadOnly { get; } = false;
            public override TValue GetValue(ref TContainer container) => container.value;
            public override void SetValue(ref TContainer container, TValue value) => container.value = value;
        }

        class KeywordProperty : Property<TContainer, StyleKeyword>
        {
            public override string Name { get; } = nameof(IStyleValue<TValue>.keyword);
            public override bool IsReadOnly { get; } = false;
            public override StyleKeyword GetValue(ref TContainer container) => container.keyword;
            public override void SetValue(ref TContainer container, StyleKeyword value) => container.keyword = value;
        }

        public StyleValuePropertyBag()
        {
            AddProperty(new ValueProperty());
            AddProperty(new KeywordProperty());
        }
    }
}
