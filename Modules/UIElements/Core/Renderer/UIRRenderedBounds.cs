// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements.UIR
{
    // The rendered bounds of an element contain everything it renders, including what it renders outside of its layout,
    // like shadows. A filtered element is drawn into a texture that must contain the rendered bounds of the elements
    // drawn into it.
    static class RenderedBounds
    {
        // Returns the rendered bounds of the render tree of a filtered element, in the element's local space.
        // The nested render trees are drawn by their own operations and are skipped.
        public static Rect ComputeNestedRenderTree(VisualElement root)
        {
            Debug.Assert(root.nestedRenderData != null);
            return ComputeRecursive(root);
        }

        static Rect ComputeRecursive(VisualElement ve)
        {
            var size = ve.layoutSize;
            if (float.IsNaN(size.x) || float.IsNaN(size.y))
                return Rect.zero;

            var rect = new Rect(0, 0, size.x, size.y);
            var bounds = BoxShadowBounds.EncapsulateOuterShadow(ve, rect);
            if (ve is TextElement)
                bounds = EncapsulateTextEffects(ve, rect, bounds);

            if (ve.ShouldClip() || ve.computedStyle.display == DisplayStyle.None)
                return bounds;

            int childCount = ve.hierarchy.childCount;
            for (int i = 0; i < childCount; ++i)
            {
                var child = ve.hierarchy[i];
                if (!child.areAncestorsAndSelfDisplayed || child.renderData == null || child.nestedRenderData != null)
                    continue;

                var childBounds = ComputeRecursive(child);
                child.TransformAlignedRectToParentSpace(ref childBounds);
                bounds = UIRUtility.Encapsulate(bounds, childBounds);
            }

            return bounds;
        }

        // Text effects are drawn in the glyph quads, which are grown to contain them. The outline grows half outside of
        // the glyphs. The shadow is taken as cast by the outlined glyphs, which can only overestimate it. The element's
        // rect grown by the outline, then moved by the shadow offset and grown by the blur, contains them unless the text
        // overflows the element.
        // The colors of the effects are ignored: Glyph.SetTints can make them visible on some glyphs even when they are
        // transparent in the style, like the glyph quads are grown regardless of the colors.
        static Rect EncapsulateTextEffects(VisualElement ve, Rect rect, Rect bounds)
        {
            ref var style = ref ve.computedStyle;

            float outline = Mathf.Max(0, style.unityTextOutlineWidth) * 0.5f;
            var outlined = new Rect(rect.x - outline, rect.y - outline, rect.width + 2 * outline, rect.height + 2 * outline);
            bounds = UIRUtility.Encapsulate(bounds, outlined);

            var textShadow = style.textShadow;
            float blur = Mathf.Abs(textShadow.blurRadius);
            var shadowBounds = new Rect(
                outlined.x + textShadow.offset.x - blur,
                outlined.y + textShadow.offset.y - blur,
                outlined.width + 2 * blur,
                outlined.height + 2 * blur);
            return UIRUtility.Encapsulate(bounds, shadowBounds);
        }
    }
}
