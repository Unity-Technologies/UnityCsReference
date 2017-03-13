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


public sealed partial class TouchScreenKeyboard
{
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
            return null;
        }

    
    
    public string text
        {
            get { return string.Empty; }
            set {}
        }
    
    
    public static bool hideInput
        {
            get { return false; }
            set {}
        }
    
    
    public bool active
        {
            get { return false; }
            set {}
        }
    
    
    public bool done { get { return true; } }
    public bool wasCanceled { get { return false; } }
    static Rect area { get { return new Rect(); } }
    static bool visible { get { return false; } }
    public static bool isSupported { get { return false; } }
    public bool canGetSelection { get { return false; } }
    public RangeInt selection { get { return new RangeInt(0, 0); } }
}


}
