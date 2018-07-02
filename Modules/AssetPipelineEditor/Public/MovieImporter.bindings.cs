// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    // AssetImporter for importing MovieTextures
    [System.Obsolete("MovieImporter is deprecated. Use VideoClipImporter instead.", false)]
    [NativeHeader("Modules/AssetPipelineEditor/Public/MovieImporter.h")]
    public partial class MovieImporter : AssetImporter
    {
        // Quality setting to use when importing the movie. This is a float value from 0 to 1.
        public extern float quality { get; set; }

        // Is this a linear texture or an sRGB texture (Only used when performing linear rendering)
        public extern bool linearTexture
        {
            [NativeName("GetLinearSampled")]
            get;
            [NativeName("SetLinearSampled")]
            set;
        }

        // Duration of the Movie to be imported in seconds
        public extern float duration { get; }
    }
}
