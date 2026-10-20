// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;

namespace Unity.IO.LowLevel.Unsafe
{
    [StructLayout(LayoutKind.Sequential)]
    unsafe public struct ReadCommand
    {
        public void* Buffer;
        public long Offset;
        public long Size;
    }

    // When adding a subsystem, keep in sync with AssetContext.h
    public enum AssetLoadingSubsystem
    {
        Other = 0,
        Texture,
        VirtualTexture,
        Mesh,
        Audio,
        Scripts,
        EntitiesScene,
        EntitiesStreamBinaryReader,
        FileInfo,
        ContentLoading
    }
}
