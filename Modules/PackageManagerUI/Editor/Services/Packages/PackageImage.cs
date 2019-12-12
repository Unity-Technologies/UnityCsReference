// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class PackageImage
    {
        public enum ImageType
        {
            Main,
            Screenshot,
            Sketchfab,
            Youtube,
            Vimeo
        }

        public ImageType type;
        public string thumbnailUrl;
        public string url;
    }
}
