// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.WSA
{
    // Must be in sync with MetroLauncher.cpp
    public enum Folder
    {
        Installation,
        Temporary,
        Local,
        Roaming,
        CameraRoll,
        DocumentsLibrary,
        HomeGroup,
        MediaServerDevices,
        MusicLibrary,
        PicturesLibrary,
        Playlists,
        RemovableDevices,
        SavedPictures,
        VideosLibrary
    }

    [NativeConditional("PLATFORM_METRO")]
    [NativeHeader("PlatformDependent/MetroPlayer/MetroLauncher.h")]
    [NativeHeader("Runtime/Export/WSA/WSALauncher.bindings.h")]
    [StaticAccessor("metro::Launcher", StaticAccessorType.DoubleColon)]
    public sealed class Launcher
    {
        public static extern void LaunchFile(Folder folder, string relativeFilePath, bool showWarning);

        public static void LaunchFileWithPicker(string fileExtension)
        {
            System.Diagnostics.Process.Start("explorer.exe");
        }

        public static void LaunchUri(string uri, bool showWarning)
        {
            System.Diagnostics.Process.Start(uri);
        }

        [NativeMethod("LaunchFileWithPicker")]
        private static extern void InternalLaunchFileWithPicker(string fileExtension);

        [NativeMethod("LaunchUri")]
        private static extern void InternalLaunchUri(string uri, bool showWarning);
    }
}
