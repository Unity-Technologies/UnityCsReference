// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.GraphToolkit.InternalBridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Struct containing the position information of a <see cref="VisualElement"/> in its owner space.
    /// </summary>
    readonly struct GraphViewPartPositionInfo
    {
        public readonly StyleLength LeftStyle;
        public readonly StyleLength TopStyle;
        public readonly StyleLength RightStyle;
        public readonly StyleLength BottomStyle;
        public readonly StyleLength WidthStyle;
        public readonly StyleLength HeightStyle;
        public readonly StyleLength MarginLeftStyle;
        public readonly StyleLength MarginRightStyle;
        public readonly StyleLength MarginBottomStyle;
        public readonly StyleLength MarginTopStyle;
        public readonly StyleEnum<Position> PositionStyle;
        readonly Rect m_LayoutInOwnerSpace;
        readonly VisualElement m_Target;

        public static readonly GraphViewPartPositionInfo Invalid = new(null, null);

        public bool Valid => m_Target != null;

        public GraphViewPartPositionInfo(VisualElement element, VisualElement ownerElement)
        {
            m_Target = element;

            LeftStyle = element?.style.left ?? new StyleLength(StyleKeyword.Null);
            TopStyle = element?.style.top ?? new StyleLength(StyleKeyword.Null);
            RightStyle = element?.style.right ?? new StyleLength(StyleKeyword.Null);
            BottomStyle = element?.style.bottom ?? new StyleLength(StyleKeyword.Null);
            WidthStyle = element?.style.width ?? new StyleLength(StyleKeyword.Null);
            HeightStyle = element?.style.height ?? new StyleLength(StyleKeyword.Null);
            MarginLeftStyle = element?.style.marginLeft ?? new StyleLength(StyleKeyword.Null);
            MarginRightStyle = element?.style.marginRight ?? new StyleLength(StyleKeyword.Null);
            MarginBottomStyle = element?.style.marginBottom ?? new StyleLength(StyleKeyword.Null);
            MarginTopStyle = element?.style.marginTop ?? new StyleLength(StyleKeyword.Null);
            PositionStyle = element?.style.position ?? Position.Relative;

            m_LayoutInOwnerSpace = GetLayoutInOwnerSpace(element, ownerElement);
        }

        public void ApplyLayoutInOwnerSpace()
        {
            if (m_Target == null)
                return;

            var style = m_Target.style;
            style.position = Position.Absolute;
            style.marginLeft = 0.0f;
            style.marginRight = 0.0f;
            style.marginBottom = 0.0f;
            style.marginTop = 0.0f;
            style.left = m_LayoutInOwnerSpace.x;
            style.top = m_LayoutInOwnerSpace.y;
            style.right = float.NaN;
            style.bottom = float.NaN;
            style.width = m_LayoutInOwnerSpace.width;
            style.height = m_LayoutInOwnerSpace.height;
        }

        public void RevertPositionInOwnerSpace()
        {
            if (m_Target == null)
                return;

            var style = m_Target.style;
            style.left = LeftStyle;
            style.top = TopStyle;
            style.right = RightStyle;
            style.bottom = BottomStyle;
            style.width = WidthStyle;
            style.height = HeightStyle;
            style.marginLeft = MarginLeftStyle;
            style.marginRight = MarginRightStyle;
            style.marginBottom = MarginBottomStyle;
            style.marginTop = MarginTopStyle;
            style.position = PositionStyle;
        }

        static Rect GetLayoutInOwnerSpace(VisualElement element, VisualElement ownerElement)
        {
            if (element == null || ownerElement == null)
                return Rect.zero;
            return element.parent.ChangeCoordinatesTo(ownerElement.contentContainer, element.layout);
        }
    }
}
