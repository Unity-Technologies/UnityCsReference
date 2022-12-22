// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    readonly struct UIStyleValue<T>
    {
        public readonly T value;
        public readonly StyleValueKeyword keyword;
        public readonly bool isKeyword;

        public UIStyleValue(T v)
        {
            value = v;
            keyword = StyleValueKeyword.None;
            isKeyword = false;
        }

        public UIStyleValue(StyleValueKeyword k)
        {
            value = default;
            keyword = k;
            isKeyword = true;
        }

        public static implicit operator UIStyleValue<T>(T value) => new UIStyleValue<T>(value);
    }
}
