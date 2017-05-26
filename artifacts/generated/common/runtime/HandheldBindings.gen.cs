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
using System.Collections;

namespace UnityEngine
{


[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
internal partial struct TouchScreenKeyboard_InternalConstructorHelperArguments
{
    public uint keyboardType;
    public uint autocorrection;
    public uint multiline;
    public uint secure;
    public uint alert;
}

public enum FullScreenMovieControlMode
{
    
    Full = 0,
    
    Minimal = 1,
    
    CancelOnInput = 2,
    
    Hidden = 3,
}

public enum FullScreenMovieScalingMode
{
    
    None = 0,
    
    AspectFit = 1,
    
    AspectFill = 2,
    
    Fill = 3
}

public enum AndroidActivityIndicatorStyle
{
    DontShow = -1,
    Large = 0,
    InversedLarge = 1,
    Small = 2,
    InversedSmall = 3,
}

public enum TizenActivityIndicatorStyle
{
    DontShow = -1,
    Large = 0,
    InversedLarge = 1,
    Small = 2,
    InversedSmall = 3,
}

public sealed partial class Handheld
{
    [System.Obsolete ("Property Handheld.use32BitDisplayBuffer has been deprecated. Modifying it has no effect, use PlayerSettings instead.")]
    public extern static bool use32BitDisplayBuffer
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SetActivityIndicatorStyleImpl (int style) ;

    public static void SetActivityIndicatorStyle(iOS.ActivityIndicatorStyle style)
        {
            SetActivityIndicatorStyleImpl((int)style);
        }
    
    
    public static void SetActivityIndicatorStyle(AndroidActivityIndicatorStyle style)
        {
            SetActivityIndicatorStyleImpl((int)style);
        }
    
    
    public static void SetActivityIndicatorStyle(TizenActivityIndicatorStyle style)
        {
            SetActivityIndicatorStyleImpl((int)style);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetActivityIndicatorStyle () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void StartActivityIndicator () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void StopActivityIndicator () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ClearShaderCache () ;

}

public sealed partial class TouchScreenKeyboard
{
    [System.NonSerialized]
            internal IntPtr m_Ptr;
    
    
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Destroy () ;

    ~TouchScreenKeyboard()
        {
            Destroy();
        }
    
    
    public TouchScreenKeyboard(string text, TouchScreenKeyboardType keyboardType, bool autocorrection, bool multiline, bool secure, bool alert, string textPlaceholder)
        {
            TouchScreenKeyboard_InternalConstructorHelperArguments arguments = new TouchScreenKeyboard_InternalConstructorHelperArguments();
            arguments.keyboardType = Convert.ToUInt32(keyboardType);
            arguments.autocorrection = Convert.ToUInt32(autocorrection);
            arguments.multiline = Convert.ToUInt32(multiline);
            arguments.secure = Convert.ToUInt32(secure);
            arguments.alert = Convert.ToUInt32(alert);
            TouchScreenKeyboard_InternalConstructorHelper(ref arguments, text, textPlaceholder);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void TouchScreenKeyboard_InternalConstructorHelper (ref TouchScreenKeyboard_InternalConstructorHelperArguments arguments, string text, string textPlaceholder) ;

    public static bool isSupported
        {
            get
            {
                RuntimePlatform platform = Application.platform;
                switch (platform)
                {
                    case RuntimePlatform.IPhonePlayer:
                    case RuntimePlatform.tvOS:
                    case RuntimePlatform.Android:
                    case RuntimePlatform.TizenPlayer:
                    case RuntimePlatform.WiiU:
                    case RuntimePlatform.Switch:
                    case RuntimePlatform.PSM:
                        return true;
                    case RuntimePlatform.WSAPlayerX86:
                    case RuntimePlatform.WSAPlayerX64:
                    case RuntimePlatform.WSAPlayerARM:
                        return false;
                    default:
                        return false;
                }
            }
        }
    
    
    [uei.ExcludeFromDocs]
public static TouchScreenKeyboard Open (string text, TouchScreenKeyboardType keyboardType , bool autocorrection , bool multiline , bool secure , bool alert ) {
    string textPlaceholder = "";
    return Open ( text, keyboardType, autocorrection, multiline, secure, alert, textPlaceholder );
}

[uei.ExcludeFromDocs]
public static TouchScreenKeyboard Open (string text, TouchScreenKeyboardType keyboardType , bool autocorrection , bool multiline , bool secure ) {
    string textPlaceholder = "";
    bool alert = false;
    return Open ( text, keyboardType, autocorrection, multiline, secure, alert, textPlaceholder );
}

[uei.ExcludeFromDocs]
public static TouchScreenKeyboard Open (string text, TouchScreenKeyboardType keyboardType , bool autocorrection , bool multiline ) {
    string textPlaceholder = "";
    bool alert = false;
    bool secure = false;
    return Open ( text, keyboardType, autocorrection, multiline, secure, alert, textPlaceholder );
}

[uei.ExcludeFromDocs]
public static TouchScreenKeyboard Open (string text, TouchScreenKeyboardType keyboardType , bool autocorrection ) {
    string textPlaceholder = "";
    bool alert = false;
    bool secure = false;
    bool multiline = false;
    return Open ( text, keyboardType, autocorrection, multiline, secure, alert, textPlaceholder );
}

[uei.ExcludeFromDocs]
public static TouchScreenKeyboard Open (string text, TouchScreenKeyboardType keyboardType ) {
    string textPlaceholder = "";
    bool alert = false;
    bool secure = false;
    bool multiline = false;
    bool autocorrection = true;
    return Open ( text, keyboardType, autocorrection, multiline, secure, alert, textPlaceholder );
}

[uei.ExcludeFromDocs]
public static TouchScreenKeyboard Open (string text) {
    string textPlaceholder = "";
    bool alert = false;
    bool secure = false;
    bool multiline = false;
    bool autocorrection = true;
    TouchScreenKeyboardType keyboardType = TouchScreenKeyboardType.Default;
    return Open ( text, keyboardType, autocorrection, multiline, secure, alert, textPlaceholder );
}

public static TouchScreenKeyboard Open(string text, [uei.DefaultValue("TouchScreenKeyboardType.Default")]  TouchScreenKeyboardType keyboardType , [uei.DefaultValue("true")]  bool autocorrection , [uei.DefaultValue("false")]  bool multiline , [uei.DefaultValue("false")]  bool secure , [uei.DefaultValue("false")]  bool alert , [uei.DefaultValue("\"\"")]  string textPlaceholder )
        {
            return new TouchScreenKeyboard(text, keyboardType, autocorrection, multiline, secure, alert, textPlaceholder);
        }

    
    
    public extern  string text
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static bool hideInput
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  bool active
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  bool done
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  bool wasCanceled
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  Status status
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  bool canGetSelection
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public RangeInt selection
        {
            get
            {
                RangeInt range;
                GetSelectionInternal(out range.start, out range.length);
                return range;
            }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void GetSelectionInternal (out int start, out int length) ;

    public extern  TouchScreenKeyboardType type
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  int targetDisplay
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public static Rect area
    {
        get { Rect tmp; INTERNAL_get_area(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_get_area (out Rect value) ;


    public extern static bool visible
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

}


}
