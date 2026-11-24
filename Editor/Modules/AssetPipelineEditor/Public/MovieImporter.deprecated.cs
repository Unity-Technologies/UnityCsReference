// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Obsolete("MovieImporter is removed. Use VideoClipImporter instead.", true)]
    public class MovieImporter
    {
        public float quality { get { return 1.0f; } set {} }
        public bool linearTexture { get { return false; } set {} }
        public float duration { get { return 1.0f; } }
    }
}
