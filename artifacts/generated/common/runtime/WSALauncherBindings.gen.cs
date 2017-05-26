// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngineInternal;

namespace UnityEngine.WSA
{
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

public sealed partial class Launcher
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void LaunchFile (Folder folder, string relativeFilePath, bool showWarning) ;

    public static void LaunchFileWithPicker(string fileExtension)
        {
            System.Diagnostics.Process.Start("explorer.exe");
        }
    
    
    public static void LaunchUri(string uri, bool showWarning)
        {
            System.Diagnostics.Process.Start(uri);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void InternalLaunchFileWithPicker (string fileExtension) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void InternalLaunchUri (string uri, bool showWarning) ;

}


}
