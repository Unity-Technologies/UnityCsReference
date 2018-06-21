// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Collections;

namespace UnityEngine
{
[UsedByNativeCode]
public sealed partial class Display
{
    
            internal IntPtr  nativeDisplay;
            internal Display()
        {
            this.nativeDisplay = new IntPtr(0);
        }
    
            internal Display(IntPtr nativeDisplay)   { this.nativeDisplay = nativeDisplay; }
    
    
    public int    renderingWidth
        {
            get
            {
                int w = 0, h = 0;
                GetRenderingExtImpl(nativeDisplay, out w, out h);
                return w;
            }
        }
    public int    renderingHeight
        {
            get
            {
                int w = 0, h = 0;
                GetRenderingExtImpl(nativeDisplay, out w, out h);
                return h;
            }
        }
    
    
    public int    systemWidth
        {
            get
            {
                int w = 0, h = 0;
                GetSystemExtImpl(nativeDisplay, out w, out h);
                return w;
            }
        }
    public int    systemHeight
        {
            get
            {
                int w = 0, h = 0;
                GetSystemExtImpl(nativeDisplay, out w, out h);
                return h;
            }
        }
    
    
    public RenderBuffer colorBuffer
        {
            get
            {
                RenderBuffer color, depth;
                GetRenderingBuffersImpl(nativeDisplay, out color, out depth);
                return color;
            }
        }
    
    
    public RenderBuffer depthBuffer
        {
            get
            {
                RenderBuffer color, depth;
                GetRenderingBuffersImpl(nativeDisplay, out color, out depth);
                return depth;
            }
        }
    
    
    public bool active
        {
            get
            {
                return GetActiveImp(nativeDisplay);
            }
        }
    
    
    public void Activate()
        {
            ActivateDisplayImpl(nativeDisplay, 0, 0, 60);
        }
    
    
    public void Activate(int width, int height, int refreshRate)
        {
            ActivateDisplayImpl(nativeDisplay, width, height, refreshRate);
        }
    
    
    public void SetParams(int width, int height, int x, int y)
        {
            SetParamsImpl(nativeDisplay, width, height, x, y);
        }
    
    
    public void SetRenderingResolution(int w, int h)
        {
            SetRenderingResolutionImpl(nativeDisplay, w, h);
        }
    
    
    [System.Obsolete("MultiDisplayLicense has been deprecated.", false)]
    public static bool MultiDisplayLicense()
        {
            return true;
        }
    
    
    public static Vector3 RelativeMouseAt(Vector3 inputMouseCoordinates)
        {
            Vector3 vec;
            int rx = 0, ry = 0;
            int x = (int)inputMouseCoordinates.x;
            int y = (int)inputMouseCoordinates.y;
            vec.z = (int)RelativeMouseAtImpl(x, y, out rx, out ry);
            vec.x = rx;
            vec.y = ry;
            return vec;
        }
    
            public static Display[] displays    = new Display[1] { new Display() };
            private static Display _mainDisplay = displays[0];
            public static Display   main        { get {return _mainDisplay; } }
    
    
    [RequiredByNativeCode]
    private static void RecreateDisplayList(IntPtr[] nativeDisplay)
        {
            if (nativeDisplay.Length == 0) 
                return;

            Display.displays = new Display[nativeDisplay.Length];
            for (int i = 0; i < nativeDisplay.Length; ++i)
                Display.displays[i] = new Display(nativeDisplay[i]);

            _mainDisplay = displays[0];
        }
    
    
    [RequiredByNativeCode]
    private static void FireDisplaysUpdated()
        {
            if (onDisplaysUpdated != null)
                onDisplaysUpdated();
        }
    
    
    public delegate void DisplaysUpdatedDelegate();
    public static event DisplaysUpdatedDelegate onDisplaysUpdated = null;
    
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void GetSystemExtImpl (IntPtr nativeDisplay, out int w, out int h) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void GetRenderingExtImpl (IntPtr nativeDisplay, out int w, out int h) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void GetRenderingBuffersImpl (IntPtr nativeDisplay, out RenderBuffer color, out RenderBuffer depth) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void SetRenderingResolutionImpl (IntPtr nativeDisplay, int w, int h) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void ActivateDisplayImpl (IntPtr nativeDisplay, int width, int height, int refreshRate) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void SetParamsImpl (IntPtr nativeDisplay, int width, int height, int x, int y) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int RelativeMouseAtImpl (int x, int y, out int rx, out int ry) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool GetActiveImp (IntPtr nativeDisplay) ;

}

}
