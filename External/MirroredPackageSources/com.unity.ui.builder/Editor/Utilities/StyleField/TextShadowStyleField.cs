using JetBrains.Annotations;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal struct BuilderTextShadow
    {
    }

    [UsedImplicitly]
    class TextShadowStyleField : BaseField<BuilderTextShadow>
    {
        [UsedImplicitly]
        public new class UxmlFactory : UxmlFactory<TextShadowStyleField, UxmlTraits> {}

        public TextShadowStyleField() : this(null) {}

        public TextShadowStyleField(string label) : base(label)
        {}
    }
}
