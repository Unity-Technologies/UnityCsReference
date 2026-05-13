// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Build
{
    ///<summary>Enum representing processor architectures that are supported by certain operating systems.</summary>
    ///<remarks>Currently only used when building for standalone desktop players.</remarks>
    public enum OSArchitecture
    {
        ///<summary>Supported for Windows and Linux. Deprecated for MacOS; use ARM64 instead.</summary>
        x64,
        ///<summary>Supported for MacOS and Windows.</summary>
        ARM64,
        ///<summary>Supported for MacOS. Deprecated for MacOS; use ARM64 instead.</summary>
        x64ARM64,
        ///<summary>Supported for Windows.</summary>
        x86,
    }
}
