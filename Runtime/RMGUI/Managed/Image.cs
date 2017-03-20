// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.RMGUI
{
    // TODO Make public. It's currently internal because it clashes in a doc tool with another class in another namespace.
    // Once the tool is fixed, this becomes public.
    internal class Image : VisualElement
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
