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


[NativeType(Header = "Editor/Src/BuildPipeline/BuildTargetPlatformSpecific.h")]
public enum BuildTargetGroup
{
    
    Unknown = 0,
    
    Standalone = 1,
    
    [System.Obsolete ("WebPlayer was removed in 5.4, consider using WebGL", true)]
    WebPlayer = 2,
    
    [System.Obsolete ("Use iOS instead (UnityUpgradable) -> iOS", true)]
    iPhone = 4,
    iOS = 4,
    
    [System.Obsolete ("PS3 has been removed in >=5.5")]
    PS3 = 5,
    
    [System.Obsolete ("XBOX360 has been removed in 5.5")]
    XBOX360 = 6,
    
    Android = 7,
    
    
    
    
    WebGL = 13,
    WSA = 14,
    
    [System.Obsolete ("Use WSA instead")]
    Metro = 14,
    [System.Obsolete ("Use WSA instead")]
    WP8 = 15,
    
    [System.Obsolete ("BlackBerry has been removed as of 5.4")]
    BlackBerry = 16,
    Tizen = 17,
    PSP2 = 18,
    PS4 = 19,
    PSM = 20,
    XboxOne = 21,
    [System.Obsolete ("SamsungTV has been removed as of 2017.3")]
    SamsungTV = 22,
    N3DS = 23,
    WiiU = 24,
    tvOS = 25,
    Facebook = 26,
    Switch = 27,
}

}
