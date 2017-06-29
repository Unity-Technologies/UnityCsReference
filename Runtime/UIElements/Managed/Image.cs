// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    public class Image : VisualElement
    {
        public Texture image { get; set; }
        public ScaleMode scaleMode { get; set; }

        public Image()
        {
            this.scaleMode = ScaleMode.ScaleAndCrop;
        }

        protected internal override Vector2 DoMeasure(float width, MeasureMode widthMode, float height, MeasureMode heightMode)
        {
            float measuredWidth = float.NaN;
            float measuredHeight = float.NaN;
            if (image == null)
                return new Vector2(measuredWidth, measuredHeight);

            // covers the MeasureMode.Exactly case
            measuredWidth = image.width;
            measuredHeight = image.height;

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
            if (image == null)
            {
                Debug.LogWarning("null texture passed to GUI.DrawTexture");
                return;
            }

            var painterParams = new TextureStylePainterParameters
            {
                layout = contentRect,
                texture = image,
                color = GUI.color,
                scaleMode = scaleMode
            };
            painter.DrawTexture(painterParams);
        }
    }
}
