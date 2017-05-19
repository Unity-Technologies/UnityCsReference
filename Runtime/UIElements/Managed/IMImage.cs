// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    class IMImage : IMElement
    {
        public Texture image { get; set; }
        public ScaleMode scaleMode { get; set; }
        public float imageAspect { get; set; }
        public bool alphaBlend { get; set; }

        public IMImage()
        {
            this.scaleMode = ScaleMode.ScaleAndCrop;
        }

        protected override int DoGenerateControlID()
        {
            return NonInteractiveControlID;
        }

        internal override void DoRepaint(IStylePainter args)
        {
            if (image == null)
            {
                Debug.LogWarning("null texture passed to GUI.DrawTexture");
                return;
            }

            if (imageAspect == 0)
                imageAspect = (float)image.width / image.height;

            Material mat = alphaBlend ? GUI.blendMaterial : GUI.blitMaterial;
            float destAspect = position.width / position.height;

            Internal_DrawTextureArguments arguments = new Internal_DrawTextureArguments()
            {
                leftBorder = 0, rightBorder = 0, topBorder = 0, bottomBorder = 0,
                color = GUI.color,
                texture = image,
                mat = mat,
            };

            switch (scaleMode)
            {
                case ScaleMode.StretchToFill:
                    arguments.screenRect = position;
                    arguments.sourceRect = new Rect(0, 0, 1, 1);
                    Graphics.Internal_DrawTexture(ref arguments);
                    break;

                case ScaleMode.ScaleAndCrop:
                    if (destAspect > imageAspect)
                    {
                        float stretch = imageAspect / destAspect;
                        arguments.screenRect = position;
                        arguments.sourceRect = new Rect(0, (1 - stretch) * .5f, 1, stretch);
                        Graphics.Internal_DrawTexture(ref arguments);
                    }
                    else
                    {
                        float stretch = destAspect / imageAspect;
                        arguments.screenRect = position;
                        arguments.sourceRect = new Rect(.5f - stretch * .5f, 0, stretch, 1);
                        Graphics.Internal_DrawTexture(ref arguments);
                    }
                    break;

                case ScaleMode.ScaleToFit:
                    if (destAspect > imageAspect)
                    {
                        float stretch = imageAspect / destAspect;
                        arguments.screenRect = new Rect(position.xMin + position.width * (1.0f - stretch) * .5f, position.yMin, stretch * position.width, position.height);
                        arguments.sourceRect = new Rect(0, 0, 1, 1);
                        Graphics.Internal_DrawTexture(ref arguments);
                    }
                    else
                    {
                        float stretch = destAspect / imageAspect;
                        arguments.screenRect = new Rect(position.xMin, position.yMin + position.height * (1.0f - stretch) * .5f, position.width, stretch * position.height);
                        arguments.sourceRect = new Rect(0, 0, 1, 1);
                        Graphics.Internal_DrawTexture(ref arguments);
                    }
                    break;
            }
        }
    }
}
