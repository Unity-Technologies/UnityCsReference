// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor;

internal interface IPlayerConnectionPlatformProperties : IPlatformProperties
{
    // The BuildPipeline.DoesBuildTargetSupportPlayerConnectionPlayerToEditor method uses
    // this property to determine if the player of a given build target supports connecting
    // back to the editor
    public bool SupportsConnect => false;

    // The BuildPipeline.DoesBuildTargetSupportPlayerConnectionListening method uses this
    // property to determine if the player of a given build target supports listening for a
    // connection from the editor.
    public bool SupportsListen => true;

    // The BuildPlayerWindow.DefaultBuildMethods.GetBuildPlayerOptionsInternal method uses this property to determine
    // whether or not to provide the "connect with profiler" option.
    bool ForceAllowProfilerConnection => false;
}
