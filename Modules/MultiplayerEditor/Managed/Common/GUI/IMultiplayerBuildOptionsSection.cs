// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Build.Profile;

namespace Unity.Multiplayer.Editor
{
    internal interface IMultiplayerBuildOptionsSection
    {
        int Order { get; }
        void DrawBuildOptions(BuildProfile profile);
    }
}
