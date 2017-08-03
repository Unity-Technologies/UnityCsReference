// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using ShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEngine
{
    internal enum EnabledOrientation
    {
        kAutorotateToPortrait           = 1,
        kAutorotateToPortraitUpsideDown = 2,
        kAutorotateToLandscapeLeft      = 4,
        kAutorotateToLandscapeRight     = 8,
    };

    public sealed partial class SleepTimeout
    {
        public const int NeverSleep = -1;
        public const int SystemSetting = -2;
    }

    [NativeHeader("Runtime/Graphics/ScreenManager.h")]
    [StaticAccessor("GetScreenManager()", StaticAccessorType.Dot)]
    public sealed partial class Screen
    {
        extern public static int   width  {[NativeMethod(Name = "GetWidth",  IsThreadSafe = true)] get; }
        extern public static int   height {[NativeMethod(Name = "GetHeight", IsThreadSafe = true)] get; }
        extern public static float dpi    {[NativeMethod(Name = "GetDPI")]                         get; }

        extern public static ScreenOrientation orientation  {[NativeMethod(Name = "GetScreenOrientation")] get; [NativeMethod(Name = "RequestOrientation")] set; }
        [NativeProperty("ScreenTimeout")] extern public static int sleepTimeout { get; set; }

        [NativeName("GetIsOrientationEnabled")] extern private static bool IsOrientationEnabled(EnabledOrientation orient);
        [NativeName("SetIsOrientationEnabled")] extern private static void SetOrientationEnabled(EnabledOrientation orient, bool enabled);

        public static bool autorotateToPortrait
        {
            get { return IsOrientationEnabled(EnabledOrientation.kAutorotateToPortrait); }
            set { SetOrientationEnabled(EnabledOrientation.kAutorotateToPortrait, value); }
        }
        public static bool autorotateToPortraitUpsideDown
        {
            get { return IsOrientationEnabled(EnabledOrientation.kAutorotateToPortraitUpsideDown); }
            set { SetOrientationEnabled(EnabledOrientation.kAutorotateToPortraitUpsideDown, value); }
        }
        public static bool autorotateToLandscapeLeft
        {
            get { return IsOrientationEnabled(EnabledOrientation.kAutorotateToLandscapeLeft); }
            set { SetOrientationEnabled(EnabledOrientation.kAutorotateToLandscapeLeft, value); }
        }
        public static bool autorotateToLandscapeRight
        {
            get { return IsOrientationEnabled(EnabledOrientation.kAutorotateToLandscapeRight); }
            set { SetOrientationEnabled(EnabledOrientation.kAutorotateToLandscapeRight, value); }
        }
    }
}

namespace UnityEngine
{
    [NativeHeader("Runtime/GfxDevice/GfxDevice.h")]
    [StaticAccessor("GetGfxDevice()", StaticAccessorType.Dot)]
    public sealed partial class GL
    {
        [NativeName("ImmediateVertex")] extern public static void Vertex3(float x, float y, float z);
        public static void Vertex(Vector3 v) { Vertex3(v.x, v.y, v.z); }

        [NativeName("ImmediateTexCoordAll")] extern public static void TexCoord3(float x, float y, float z);
        public static void TexCoord(Vector3 v)          { TexCoord3(v.x, v.y, v.z); }
        public static void TexCoord2(float x, float y)  { TexCoord3(x, y, 0.0f); }

        [NativeName("ImmediateTexCoord")] extern public static void MultiTexCoord3(int unit, float x, float y, float z);
        public static void MultiTexCoord(int unit, Vector3 v)           { MultiTexCoord3(unit, v.x, v.y, v.z); }
        public static void MultiTexCoord2(int unit, float x, float y)   { MultiTexCoord3(unit, x, y, 0.0f); }

        [NativeName("ImmediateColor")] extern private static void ImmediateColor(float r, float g, float b, float a);
        public static void Color(Color c) { ImmediateColor(c.r, c.g, c.b, c.a); }

        extern public static bool wireframe     { get; set; }
        extern public static bool sRGBWrite     { get; set; }
        [NativeProperty("UserBackfaceMode")] extern public static bool invertCulling { get;  set; }

        extern public static void Flush();
        extern public static void RenderTargetBarrier();
    }

    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
    public sealed partial class GL
    {
        [FreeFunction("GLPushMatrixScript")]        extern public static void PushMatrix();
        [FreeFunction("GLPopMatrixScript")]         extern public static void PopMatrix();
        [FreeFunction("GLLoadIdentityScript")]      extern public static void LoadIdentity();
        [FreeFunction("GLLoadOrthoScript")]         extern public static void LoadOrtho();
        [FreeFunction("GLLoadPixelMatrixScript")]   extern public static void LoadPixelMatrix();

        [FreeFunction("GLLoadPixelMatrixScript")] extern private static void LoadPixelMatrixArgs(float left, float right, float bottom, float top);
        public static void LoadPixelMatrix(float left, float right, float bottom, float top)
        {
            LoadPixelMatrixArgs(left, right, bottom, top);
        }
    }
}
