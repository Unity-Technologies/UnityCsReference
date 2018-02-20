// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor
{
    [Flags]
    public enum AssetDeleteResult
    {
        // Tells the internal implementation that the callback did not delete the asset. The asset will be delete by the internal implementation.
        DidNotDelete = 0,

        // Tells Unity that the file cannot be deleted and Unity should leave it alone.
        FailedDelete = 1,

        //  Tells Unity that the asset was deleted by the callback. Unity will not try to delete the asset, but will delete the cached version and preview file.
        DidDelete = 2
    }
}
