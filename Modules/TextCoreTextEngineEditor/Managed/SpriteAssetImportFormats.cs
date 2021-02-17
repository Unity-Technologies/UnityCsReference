// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;


namespace UnityEditor.TextCore.Text
{
    internal enum SpriteAssetImportFormats { None = 0, TexturePackerJsonArray = 0x1 };

    internal class TexturePacker_JsonArray
    {
        [System.Serializable]
        public struct SpriteFrame
        {
            public float x;
            public float y;
            public float w;
            public float h;

            public override string ToString()
            {
                string s = "x: " + x.ToString("f2") + " y: " + y.ToString("f2") + " h: " + h.ToString("f2") + " w: " + w.ToString("f2");
                return s;
            }
        }

        [System.Serializable]
        public struct SpriteSize
        {
            public float w;
            public float h;

            public override string ToString()
            {
                string s = "w: " + w.ToString("f2") + " h: " + h.ToString("f2");
                return s;
            }
        }

        [System.Serializable]
        public struct Frame
        {
            public string filename;
            public SpriteFrame frame;
            public bool rotated;
            public bool trimmed;
            public SpriteFrame spriteSourceSize;
            public SpriteSize sourceSize;
            public Vector2 pivot;
        }

        [System.Serializable]
        public struct Meta
        {
            public string app;
            public string version;
            public string image;
            public string format;
            public SpriteSize size;
            public float scale;
            public string smartupdate;
        }

        [System.Serializable]
        public class SpriteDataObject
        {
            public List<Frame> frames;
            public Meta meta;
        }
    }
}
