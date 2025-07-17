// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class DisplayStyleEnumField : StyleEnumField<DisplayStyle>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleEnumField<DisplayStyle>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleEnumField<DisplayStyle>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new DisplayStyleEnumField();
        }

        DisplayStyleEnumField()
            : base(true)
        {
        }
    }

    internal class VisibilityStyleEnumField : StyleEnumField<Visibility>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleEnumField<Visibility>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleEnumField<Visibility>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new VisibilityStyleEnumField();
        }

        public VisibilityStyleEnumField()
            : base(true)
        {
        }
    }

    internal class OverflowStyleEnumField : StyleEnumField<Overflow>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleEnumField<Overflow>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleEnumField<Overflow>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new OverflowStyleEnumField();
        }

        public OverflowStyleEnumField()
            : base(true)
        {
        }
    }

    internal class PositionStyleEnumField : StyleEnumField<Position>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleEnumField<Position>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleEnumField<Position>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new PositionStyleEnumField();
        }
    }

    internal class FlexDirectionStyleEnumField : StyleEnumField<FlexDirection>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleEnumField<FlexDirection>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleEnumField<FlexDirection>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new FlexDirectionStyleEnumField();
        }

        public FlexDirectionStyleEnumField()
            : base(true)
        {
        }
    }

    internal class WrapStyleEnumField : StyleEnumField<Wrap>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleEnumField<Wrap>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleEnumField<Wrap>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new WrapStyleEnumField();
        }

        public WrapStyleEnumField()
            : base(true)
        {
        }
    }

    internal class AlignStyleEnumField : StyleEnumField<Align>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleEnumField<Align>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleEnumField<Align>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new AlignStyleEnumField();
        }

        public AlignStyleEnumField()
            : base(true)
        {
        }
    }

    internal class JustifyStyleEnumField : StyleEnumField<Justify>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleEnumField<Justify>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleEnumField<Justify>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new JustifyStyleEnumField();
        }

        public JustifyStyleEnumField()
            : base(true)
        {
        }
    }

    internal class UnityTextAlignStyleEnumField : StyleEnumField<TextAnchor>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleEnumField<TextAnchor>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleEnumField<TextAnchor>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new UnityTextAlignStyleEnumField();
        }

        public UnityTextAlignStyleEnumField()
            : base(true)
        {
        }
    }

    internal class WhiteSpaceStyleEnumField : StyleEnumField<WhiteSpace>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleEnumField<WhiteSpace>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleEnumField<WhiteSpace>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new WhiteSpaceStyleEnumField();
        }

        public WhiteSpaceStyleEnumField()
            : base(true)
        {
        }
    }

    internal class TextOverflowStyleEnumField : StyleEnumField<TextOverflow>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleEnumField<TextOverflow>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleEnumField<TextOverflow>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new TextOverflowStyleEnumField();
        }

        public TextOverflowStyleEnumField()
            : base(true)
        {
        }
    }

    internal class TextOverflowPositionStyleEnumField : StyleEnumField<TextOverflowPosition>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleEnumField<TextOverflowPosition>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleEnumField<TextOverflowPosition>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new TextOverflowPositionStyleEnumField();
        }
    }

    internal class EditorTextRenderingModeStyleEnumField : StyleEnumField<EditorTextRenderingMode>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleEnumField<EditorTextRenderingMode>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleEnumField<EditorTextRenderingMode>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new EditorTextRenderingModeStyleEnumField();
        }
    }

    internal class TextGeneratorTypeStyleEnumField : StyleEnumField<TextGeneratorType>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleEnumField<TextGeneratorType>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleEnumField<TextGeneratorType>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new TextGeneratorTypeStyleEnumField();
        }
    }

    internal class FontStyleStyleEnumField : StylePropertyField<StyleEnum<FontStyle>, FontStyleToggleField, FontStyle>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StylePropertyField<StyleEnum<FontStyle>, FontStyleToggleField, FontStyle>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StylePropertyField<StyleEnum<FontStyle>, FontStyleToggleField, FontStyle>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new FontStyleStyleEnumField();
        }

        public FontStyleStyleEnumField()
            : this(null) { }

        public FontStyleStyleEnumField(string label)
            : base(label, new FontStyleToggleField()) { }
    }

    internal class TextAlignStyleEnumField : StylePropertyField<StyleEnum<TextAnchor>, TextAlignToggleField, TextAnchor>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StylePropertyField<StyleEnum<TextAnchor>, TextAlignToggleField, TextAnchor>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StylePropertyField<StyleEnum<TextAnchor>, TextAlignToggleField, TextAnchor>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new TextAlignStyleEnumField();
        }

        public TextAlignStyleEnumField()
            : this(null) { }

        public TextAlignStyleEnumField(string label)
            : base(label, new TextAlignToggleField()) { }
    }
}
