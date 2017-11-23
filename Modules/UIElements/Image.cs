// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEngine.Experimental.UIElements
{
    public class Image : VisualElement
    {
        private StyleValue<int> m_ScaleMode;
        private StyleValue<Texture> m_Image;

        public StyleValue<Texture> image
        {
            get { return m_Image; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref m_Image, value))
                {
                    Dirty(ChangeType.Repaint | ChangeType.Layout);
                }
            }
        }

        public StyleValue<ScaleMode> scaleMode
        {
            get { return new StyleValue<ScaleMode>((ScaleMode)m_ScaleMode.value, m_ScaleMode.specificity); }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref m_ScaleMode,
                        new StyleValue<int>((int)value.value, value.specificity)))
                {
                    Dirty(ChangeType.Layout);
                }
            }
        }

        public Image()
        {
            this.scaleMode = ScaleMode.ScaleAndCrop;
        }

        protected internal override Vector2 DoMeasure(float width, MeasureMode widthMode, float height, MeasureMode heightMode)
        {
            float measuredWidth = float.NaN;
            float measuredHeight = float.NaN;

            Texture current = image.GetSpecifiedValueOrDefault(null);
            if (current == null)
                return new Vector2(measuredWidth, measuredHeight);

            // covers the MeasureMode.Exactly case
            measuredWidth = current.width;
            measuredHeight = current.height;

            if (widthMode == MeasureMode.AtMost)
            {
                measuredWidth = Mathf.Min(measuredWidth, width);
            }

            if (heightMode == MeasureMode.AtMost)
            {
                measuredHeight = Mathf.Min(measuredHeight, height);
            }

            return new Vector2(measuredWidth, measuredHeight);
        }

        internal override void DoRepaint(IStylePainter painter)
        {
            //we paint the bg color, borders, etc
            base.DoRepaint(painter);

            Texture current = image.GetSpecifiedValueOrDefault(null);
            if (current == null)
            {
                Debug.LogWarning("null texture passed to GUI.DrawTexture");
                return;
            }

            var painterParams = new TextureStylePainterParameters
            {
                rect = contentRect,
                texture = current,
                color = GUI.color,
                scaleMode = scaleMode
            };
            painter.DrawTexture(painterParams);
        }

        protected override void OnStyleResolved(ICustomStyle elementStyle)
        {
            base.OnStyleResolved(elementStyle);
            elementStyle.ApplyCustomProperty("image", ref m_Image);
            elementStyle.ApplyCustomProperty("image-size", ref m_ScaleMode);
        }
    }
}
