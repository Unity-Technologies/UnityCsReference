// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;

using UnityEngine;

namespace UnityEditor
{


public enum TizenOSVersion
{
    
    
    
    Version24 = 1
}

public enum TizenShowActivityIndicatorOnLoading
{
    
    Large = 0,
    
    InversedLarge = 1,
    
    Small = 2,
    
    InversedSmall = 3,
    
    DontShow = -1
}

public sealed partial class PlayerSettings : UnityEngine.Object
{
    public enum TizenCapability    
    {
        Location = 0,
        DataSharing = 1,
        NetworkGet = 2,
        WifiDirect = 3,
        CallHistoryRead = 4,
        Power = 5,
        ContactWrite = 6,
        MessageWrite = 7,
        ContentWrite = 8,
        Push = 9,
        AccountRead = 10,
        ExternalStorage = 11,
        Recorder = 12,
        PackageManagerInfo = 13,
        NFCCardEmulation = 14,
        CalendarWrite = 15,
        WindowPrioritySet = 16,
        VolumeSet = 17,
        CallHistoryWrite = 18,
        AlarmSet = 19,
        Call = 20,
        Email = 21,
        ContactRead = 22,
        Shortcut = 23,
        KeyManager = 24,
        LED = 25,
        NetworkProfile = 26,
        AlarmGet = 27,
        Display = 28,
        CalendarRead = 29,
        NFC = 30,
        AccountWrite = 31,
        Bluetooth = 32,
        Notification = 33,
        NetworkSet = 34,
        ExternalStorageAppData = 35,
        Download = 36,
        Telephony = 37,
        MessageRead = 38,
        MediaStorage = 39,
        Internet = 40,
        Camera = 41,
        Haptic = 42,
        AppManagerLaunch = 43,
        SystemSettings = 44
    }

    public sealed partial class Tizen    
    {
        public extern static string productDescription
        {
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            get;
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            set;
        }

        public extern static string productURL
        {
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            get;
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            set;
        }

        public extern static string signingProfileName
        {
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            get;
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            set;
        }

        public extern static string deploymentTarget
        {
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            get;
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            set;
        }

        public extern static int deploymentTargetType
        {
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            get;
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            set;
        }

        public extern static TizenOSVersion minOSVersion
        {
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            get;
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            set;
        }

        public extern static TizenShowActivityIndicatorOnLoading showActivityIndicatorOnLoading
        {
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            get;
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            set;
        }

        public static void SetCapability(TizenCapability capability, bool value)
            {
                InternalSetCapability(capability.ToString(), value.ToString());
            }
        
        
        public static bool GetCapability(TizenCapability capability)
            {
                string stringValue = InternalGetCapability(capability.ToString());

                if (string.IsNullOrEmpty(stringValue)) return false;

                try
                {
                    return (bool)System.ComponentModel.TypeDescriptor.GetConverter(typeof(bool)).ConvertFromString(stringValue);
                }
                catch
                {
                    Debug.LogError("Failed to parse value  ('" + capability.ToString() + "," + stringValue + "') to bool type.");
                    return false;
                }
            }
        
        
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void InternalSetCapability (string name, string value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  string InternalGetCapability (string name) ;

    }

}

}
