// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using UnityEngine.Bindings;

namespace UnityEditor
{


[NativeType("Runtime/Serialize/SerializationMetaFlags.h")]
public enum BuildTarget
{
    
    StandaloneOSX = 2,
    [System.Obsolete ("Use StandaloneOSX instead (UnityUpgradable) -> StandaloneOSX", true)]
    StandaloneOSXUniversal = 2,
    [System.Obsolete ("StandaloneOSXIntel has been removed in 2017.3")]
    StandaloneOSXIntel = 4,
    
    StandaloneWindows = 5,
    
    [System.Obsolete ("WebPlayer has been removed in 5.4", true)]
    WebPlayer = 6,
    
    [System.Obsolete ("WebPlayerStreamed has been removed in 5.4", true)]
    WebPlayerStreamed = 7,
    
    iOS = 9,
    
    [System.Obsolete ("PS3 has been removed in >=5.5")]
    PS3 = 10,
    
    [System.Obsolete ("XBOX360 has been removed in 5.5")]
    XBOX360 = 11,
    
    
    Android = 13,
    
    
    
    
    StandaloneLinux = 17,
    
    StandaloneWindows64 = 19,
    
    WebGL = 20,
    
    WSAPlayer = 21,
    
    StandaloneLinux64 = 24,
    
    StandaloneLinuxUniversal = 25,
    [System.Obsolete ("Use WSAPlayer with Windows Phone 8.1 selected")]
    WP8Player = 26,
    [System.Obsolete ("StandaloneOSXIntel64 has been removed in 2017.3")]
    StandaloneOSXIntel64 = 27,
    [System.Obsolete ("BlackBerry has been removed in 5.4")]
    BlackBerry = 28,
    
    Tizen = 29,
    PSP2 = 30,
    PS4 = 31,
    PSM = 32,
    XboxOne = 33,
    [System.Obsolete ("SamsungTV has been removed in 2017.3")]
    SamsungTV = 34,
    N3DS = 35,
    WiiU = 36,
    tvOS = 37,
    Switch = 38,
    
    [System.Obsolete ("Use iOS instead (UnityUpgradable) -> iOS", true)]
    iPhone = -1,
    [System.Obsolete ("BlackBerry has been removed in 5.4")]
    BB10 = -1,
    [System.Obsolete ("Use WSAPlayer instead (UnityUpgradable) -> WSAPlayer", true)]
    MetroPlayer = -1,
    
    NoTarget = -2,
}

}
