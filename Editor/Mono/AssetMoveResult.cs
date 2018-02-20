// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor
{
    [Flags]
    public enum AssetMoveResult
    {
        // Tells the internal implementation that the asset was not moved physically on disk by the script
        DidNotMove = 0,

        // Tells the internal implementation that the script could not move the assets, and Unity should not attempt to move the asset
        FailedMove = 1,

        // Tells the internal implementation that the script moved the asset physically on disk. The internal implementation will
        DidMove = 2
    }
}
