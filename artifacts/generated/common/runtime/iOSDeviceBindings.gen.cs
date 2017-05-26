// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;


namespace UnityEngine.iOS
{


public enum DeviceGeneration
{
    
    Unknown = 0,
    
    iPhone = 1,
    
    iPhone3G = 2,
    
    iPhone3GS = 3,
    
    iPodTouch1Gen = 4,
    
    iPodTouch2Gen = 5,
    
    iPodTouch3Gen = 6,
    
    iPad1Gen = 7,
    
    iPhone4 = 8,
    
    iPodTouch4Gen = 9,
    
    iPad2Gen = 10,
    
    iPhone4S = 11,
    
    iPad3Gen = 12,
    
    iPhone5 = 13,
    
    iPodTouch5Gen = 14,
    
    iPadMini1Gen = 15,
    
    iPad4Gen = 16,
    
    iPhone5C = 17,
    
    iPhone5S = 18,
    
    iPadAir1 = 19,
    
    iPadMini2Gen = 20,
    iPhone6 = 21,
    iPhone6Plus = 22,
    iPadMini3Gen = 23,
    iPadAir2 = 24,
    iPhone6S = 25,
    iPhone6SPlus = 26,
    iPadPro1Gen = 27,
    iPadMini4Gen = 28,
    iPhoneSE1Gen  = 29,
    iPadPro10Inch1Gen = 30,
    iPhone7 = 31,
    iPhone7Plus = 32,
    iPodTouch6Gen = 33,
    iPad5Gen = 34,
    
    iPhoneUnknown = 10001,
    
    iPadUnknown = 10002,
    
    iPodTouchUnknown = 10003,
}

public sealed partial class Device
{
    public extern static DeviceGeneration generation
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static string systemVersion
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetNoBackupFlag (string path) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ResetNoBackupFlag (string path) ;

    public extern static string vendorIdentifier
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public static string advertisingIdentifier
        {
            get
            {
                string advertisingId = GetAdvertisingIdentifier();
                Application.InvokeOnAdvertisingIdentifierCallback(advertisingId, advertisingTrackingEnabled);
                return advertisingId;
            }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  string GetAdvertisingIdentifier () ;

    public extern static bool advertisingTrackingEnabled
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

}

public enum ActivityIndicatorStyle
{
    DontShow = -1,
    WhiteLarge = 0,
    White = 1,
    Gray = 2,
}


}
