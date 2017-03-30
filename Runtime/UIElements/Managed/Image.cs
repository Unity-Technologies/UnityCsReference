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

        public override void DoRepaint(IStylePainter painter)
        {
            if (image == null)
            {
                Debug.LogWarning("null texture passed to GUI.DrawTexture");
                return;
            }

            painter.DrawTexture(position, image, GUI.color, scaleMode);
        }
    }
}
