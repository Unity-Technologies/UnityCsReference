// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using uei = UnityEngine.Internal;
using System.Runtime.InteropServices;
using LightProbeUsage = UnityEngine.Rendering.LightProbeUsage;
using ShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode;
using SphericalHarmonicsL2 = UnityEngine.Rendering.SphericalHarmonicsL2;

namespace UnityEngine
{
    internal enum EnabledOrientation
    {
        kAutorotateToPortrait           = 1,
        kAutorotateToPortraitUpsideDown = 2,
        kAutorotateToLandscapeLeft      = 4,
        kAutorotateToLandscapeRight     = 8,
    };

    public enum FullScreenMode
    {
        ExclusiveFullScreen = 0,
        FullScreenWindow = 1,
        MaximizedWindow = 2,
        Windowed = 3,
    }

    public sealed partial class SleepTimeout
    {
        public const int NeverSleep = -1;
        public const int SystemSetting = -2;
    }

    [NativeHeader("Runtime/Graphics/ScreenManager.h")]
    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
    [StaticAccessor("GetScreenManager()", StaticAccessorType.Dot)]
    public sealed partial class Screen
    {
        extern public static int   width  {[NativeMethod(Name = "GetWidth",  IsThreadSafe = true)] get; }
        extern public static int   height {[NativeMethod(Name = "GetHeight", IsThreadSafe = true)] get; }
        extern public static float dpi    {[NativeName("GetDPI")] get; }

        extern private static void RequestOrientation(ScreenOrientation orient);
        extern private static ScreenOrientation GetScreenOrientation();

        public static ScreenOrientation orientation
        {
            get { return GetScreenOrientation(); }
            set
            {
            #pragma warning disable 618 // UnityEngine.ScreenOrientation.Unknown is obsolete
                if (value == ScreenOrientation.Unknown)
            #pragma warning restore 649
                {
                    Debug.Log("ScreenOrientation.Unknown is deprecated. Please use ScreenOrientation.AutoRotation");
                    value = ScreenOrientation.AutoRotation;
                }
                RequestOrientation(value);
            }
        }
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

        extern public static Resolution currentResolution { get; }
        extern public static bool fullScreen {[NativeName("IsFullscreen")] get; [NativeName("RequestSetFullscreenFromScript")] set; }
        extern public static FullScreenMode fullScreenMode {[NativeName("GetFullscreenMode")] get; [NativeName("RequestSetFullscreenModeFromScript")] set; }

        extern public static Rect safeArea { get; }

        [NativeName("RequestResolution")]
        extern public static void SetResolution(int width, int height, FullScreenMode fullscreenMode, [uei.DefaultValue("0")] int preferredRefreshRate);

        public static void SetResolution(int width, int height, FullScreenMode fullscreenMode)
        {
            SetResolution(width, height, fullscreenMode, 0);
        }

        public static void SetResolution(int width, int height, bool fullscreen, [uei.DefaultValue("0")] int preferredRefreshRate)
        {
            SetResolution(width, height, fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed, preferredRefreshRate);
        }

        public static void SetResolution(int width, int height, bool fullscreen)
        {
            SetResolution(width, height, fullscreen, 0);
        }

        extern public static Resolution[] resolutions {[FreeFunction("ScreenScripting::GetResolutions")] get; }
    }
}

namespace UnityEngine
{
    [NativeHeader("Runtime/Camera/LightProbeProxyVolume.h")]
    [NativeHeader("Runtime/Graphics/CopyTexture.h")]
    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
    [NativeHeader("Runtime/Shaders/ComputeShader.h")]
    public partial class Graphics
    {
        [FreeFunction("CopyTexture")] extern private static void CopyTexture_Full(Texture src, Texture dst);
        [FreeFunction("CopyTexture")] extern private static void CopyTexture_Slice_AllMips(Texture src, int srcElement, Texture dst, int dstElement);
        [FreeFunction("CopyTexture")] extern private static void CopyTexture_Slice(Texture src, int srcElement, int srcMip, Texture dst, int dstElement, int dstMip);
        [FreeFunction("CopyTexture")] extern private static void CopyTexture_Region(Texture src, int srcElement, int srcMip, int srcX, int srcY, int srcWidth, int srcHeight, Texture dst, int dstElement, int dstMip, int dstX, int dstY);
        [FreeFunction("ConvertTexture")] extern private static bool ConvertTexture_Full(Texture src, Texture dst);
        [FreeFunction("ConvertTexture")] extern private static bool ConvertTexture_Slice(Texture src, int srcElement, Texture dst, int dstElement);

        #region public static void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer, Camera camera = null, int submeshIndex = 0, MaterialPropertyBlock properties = null, bool castShadows = true, bool receiveShadows = true, bool useLightProbes = true)
        [uei.ExcludeFromDocs]
        public static void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer)
        {
            DrawMesh(mesh, Matrix4x4.TRS(position, rotation, Vector3.one), material, layer, null, 0, null, ShadowCastingMode.On, true, null, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer, Camera camera)
        {
            DrawMesh(mesh, Matrix4x4.TRS(position, rotation, Vector3.one), material, layer, camera, 0, null, ShadowCastingMode.On, true, null, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer, Camera camera, int submeshIndex)
        {
            DrawMesh(mesh, Matrix4x4.TRS(position, rotation, Vector3.one), material, layer, camera, submeshIndex, null, ShadowCastingMode.On, true, null, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock properties)
        {
            DrawMesh(mesh, Matrix4x4.TRS(position, rotation, Vector3.one), material, layer, camera, submeshIndex, properties, ShadowCastingMode.On, true, null, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock properties, bool castShadows)
        {
            DrawMesh(mesh, Matrix4x4.TRS(position, rotation, Vector3.one), material, layer, camera, submeshIndex, properties, castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off, true, null, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock properties, bool castShadows, bool receiveShadows)
        {
            DrawMesh(mesh, Matrix4x4.TRS(position, rotation, Vector3.one), material, layer, camera, submeshIndex, properties, castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off, receiveShadows, null, LightProbeUsage.BlendProbes, null);
        }

        public static void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer, [uei.DefaultValue("null")] Camera camera, [uei.DefaultValue("0")] int submeshIndex, [uei.DefaultValue("null")] MaterialPropertyBlock properties, [uei.DefaultValue("true")] bool castShadows, [uei.DefaultValue("true")] bool receiveShadows, [uei.DefaultValue("true")] bool useLightProbes)
        {
            DrawMesh(mesh, Matrix4x4.TRS(position, rotation, Vector3.one), material, layer, camera, submeshIndex, properties, castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off, receiveShadows, null, useLightProbes ? LightProbeUsage.BlendProbes : LightProbeUsage.Off, null);
        }

        #endregion

        #region public static void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows = true, Transform probeAnchor = null, bool useLightProbes = true)
        [uei.ExcludeFromDocs]
        public static void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock properties, ShadowCastingMode castShadows)
        {
            DrawMesh(mesh, Matrix4x4.TRS(position, rotation, Vector3.one), material, layer, camera, submeshIndex, properties, castShadows, true, null, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows)
        {
            DrawMesh(mesh, Matrix4x4.TRS(position, rotation, Vector3.one), material, layer, camera, submeshIndex, properties, castShadows, receiveShadows, null, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, Transform probeAnchor)
        {
            DrawMesh(mesh, Matrix4x4.TRS(position, rotation, Vector3.one), material, layer, camera, submeshIndex, properties, castShadows, receiveShadows, probeAnchor, LightProbeUsage.BlendProbes, null);
        }

        public static void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock properties, ShadowCastingMode castShadows, [uei.DefaultValue("true")] bool receiveShadows, [uei.DefaultValue("null")] Transform probeAnchor, [uei.DefaultValue("true")] bool useLightProbes)
        {
            DrawMesh(mesh, Matrix4x4.TRS(position, rotation, Vector3.one), material, layer, camera, submeshIndex, properties, castShadows, receiveShadows, probeAnchor, useLightProbes ? LightProbeUsage.BlendProbes : LightProbeUsage.Off, null);
        }

        #endregion

        #region public static void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int layer, Camera camera = null, int submeshIndex = 0, MaterialPropertyBlock properties = null, bool castShadows = true, bool receiveShadows = true, bool useLightProbes = true)
        [uei.ExcludeFromDocs]
        public static void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int layer)
        {
            DrawMesh(mesh, matrix, material, layer, null, 0, null, ShadowCastingMode.On, true, null, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int layer, Camera camera)
        {
            DrawMesh(mesh, matrix, material, layer, camera, 0, null, ShadowCastingMode.On, true, null, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int layer, Camera camera, int submeshIndex)
        {
            DrawMesh(mesh, matrix, material, layer, camera, submeshIndex, null, ShadowCastingMode.On, true, null, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock properties)
        {
            DrawMesh(mesh, matrix, material, layer, camera, submeshIndex, properties, ShadowCastingMode.On, true, null, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock properties, bool castShadows)
        {
            DrawMesh(mesh, matrix, material, layer, camera, submeshIndex, properties, castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off, true, null, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock properties, bool castShadows, bool receiveShadows)
        {
            DrawMesh(mesh, matrix, material, layer, camera, submeshIndex, properties, castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off, receiveShadows, null, LightProbeUsage.BlendProbes, null);
        }

        public static void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int layer, [uei.DefaultValue("null")] Camera camera, [uei.DefaultValue("0")] int submeshIndex, [uei.DefaultValue("null")] MaterialPropertyBlock properties, [uei.DefaultValue("true")] bool castShadows, [uei.DefaultValue("true")] bool receiveShadows, [uei.DefaultValue("true")] bool useLightProbes)
        {
            DrawMesh(mesh, matrix, material, layer, camera, submeshIndex, properties, castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off, receiveShadows, null, useLightProbes ? LightProbeUsage.BlendProbes : LightProbeUsage.Off, null);
        }

        #endregion

        #region public static void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows = true, Transform probeAnchor = null, bool useLightProbes = true)
        [uei.ExcludeFromDocs]
        public static void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock properties, ShadowCastingMode castShadows)
        {
            DrawMesh(mesh, matrix, material, layer, camera, submeshIndex, properties, castShadows, true, null, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows)
        {
            DrawMesh(mesh, matrix, material, layer, camera, submeshIndex, properties, castShadows, receiveShadows, null, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, Transform probeAnchor)
        {
            DrawMesh(mesh, matrix, material, layer, camera, submeshIndex, properties, castShadows, receiveShadows, probeAnchor, LightProbeUsage.BlendProbes, null);
        }

        public static void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock properties, ShadowCastingMode castShadows, [uei.DefaultValue("true")] bool receiveShadows, [uei.DefaultValue("null")] Transform probeAnchor, [uei.DefaultValue("true")] bool useLightProbes)
        {
            DrawMesh(mesh, matrix, material, layer, camera, submeshIndex, properties, castShadows, receiveShadows, probeAnchor, useLightProbes ? LightProbeUsage.BlendProbes : LightProbeUsage.Off, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, Transform probeAnchor, LightProbeUsage lightProbeUsage)
        {
            Internal_DrawMesh(mesh, submeshIndex, matrix, material, layer, camera, properties, castShadows, receiveShadows, probeAnchor, lightProbeUsage, null);
        }

        public static void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, Transform probeAnchor, LightProbeUsage lightProbeUsage, [uei.DefaultValue("null")] LightProbeProxyVolume lightProbeProxyVolume)
        {
            if (lightProbeUsage == LightProbeUsage.UseProxyVolume && lightProbeProxyVolume == null)
                throw new ArgumentException("lightProbeProxyVolume", "Argument lightProbeProxyVolume must not be null if lightProbeUsage is set to UseProxyVolume.");
            Internal_DrawMesh(mesh, submeshIndex, matrix, material, layer, camera, properties, castShadows, receiveShadows, probeAnchor, lightProbeUsage, lightProbeProxyVolume);
        }

        #endregion

        [FreeFunction("DrawMeshMatrixFromScript")]
        extern internal static void Internal_DrawMesh(Mesh mesh, int submeshIndex, Matrix4x4 matrix, Material material, int layer, Camera camera, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, Transform probeAnchor, LightProbeUsage lightProbeUsage, LightProbeProxyVolume lightProbeProxyVolume);

        // TODO: Migrate these dreadful overloads to default arguments.
        #region public static void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, Matrix4x4[] matrices, int count = matrices.Length, MaterialPropertyBlock properties = null, ShadowCastingMode castShadows = ShadowCastingMode.On, bool receiveShadows = true, int layer = 0, Camera camera = null, LightProbeUsage lightProbeUsage = LightProbeUsage.BlendProbes, LightProbeProxyVolume lightProbeProxyVolume = null)
        [uei.ExcludeFromDocs]
        public static void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, Matrix4x4[] matrices)
        {
            DrawMeshInstanced(mesh, submeshIndex, material, matrices, matrices.Length, null, ShadowCastingMode.On, true, 0, null, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, Matrix4x4[] matrices, int count)
        {
            DrawMeshInstanced(mesh, submeshIndex, material, matrices, count, null, ShadowCastingMode.On, true, 0, null, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, Matrix4x4[] matrices, int count, MaterialPropertyBlock properties)
        {
            DrawMeshInstanced(mesh, submeshIndex, material, matrices, count, properties, ShadowCastingMode.On, true, 0, null, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, Matrix4x4[] matrices, int count, MaterialPropertyBlock properties, ShadowCastingMode castShadows)
        {
            DrawMeshInstanced(mesh, submeshIndex, material, matrices, count, properties, castShadows, true, 0, null, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, Matrix4x4[] matrices, int count, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows)
        {
            DrawMeshInstanced(mesh, submeshIndex, material, matrices, count, properties, castShadows, receiveShadows, 0, null, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, Matrix4x4[] matrices, int count, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer)
        {
            DrawMeshInstanced(mesh, submeshIndex, material, matrices, count, properties, castShadows, receiveShadows, layer, null, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, Matrix4x4[] matrices, int count, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer, Camera camera)
        {
            DrawMeshInstanced(mesh, submeshIndex, material, matrices, count, properties, castShadows, receiveShadows, layer, camera, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, Matrix4x4[] matrices, int count, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer, Camera camera, LightProbeUsage lightProbeUsage)
        {
            DrawMeshInstanced(mesh, submeshIndex, material, matrices, count, properties, castShadows, receiveShadows, layer, camera, lightProbeUsage, null);
        }

        public static void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, Matrix4x4[] matrices, [uei.DefaultValue("matrices.Length")] int count, [uei.DefaultValue("null")] MaterialPropertyBlock properties, [uei.DefaultValue("ShadowCastingMode.On")] ShadowCastingMode castShadows, [uei.DefaultValue("true")] bool receiveShadows, [uei.DefaultValue("0")] int layer, [uei.DefaultValue("null")] Camera camera, [uei.DefaultValue("LightProbeUsage.BlendProbes")] LightProbeUsage lightProbeUsage, [uei.DefaultValue("null")] LightProbeProxyVolume lightProbeProxyVolume)
        {
            if (!SystemInfo.supportsInstancing)
                throw new InvalidOperationException("Instancing is not supported.");
            else if (mesh == null)
                throw new ArgumentNullException("mesh");
            else if (submeshIndex < 0 || submeshIndex >= mesh.subMeshCount)
                throw new ArgumentOutOfRangeException("submeshIndex", "submeshIndex out of range.");
            else if (material == null)
                throw new ArgumentNullException("material");
            else if (!material.enableInstancing)
                throw new InvalidOperationException("Material needs to enable instancing for use with DrawMeshInstanced.");
            else if (matrices == null)
                throw new ArgumentNullException("matrices");
            else if (count < 0 || count > Mathf.Min(kMaxDrawMeshInstanceCount, matrices.Length))
                throw new ArgumentOutOfRangeException("count", String.Format("Count must be in the range of 0 to {0}.", Mathf.Min(kMaxDrawMeshInstanceCount, matrices.Length)));
            else if (lightProbeUsage == LightProbeUsage.UseProxyVolume && lightProbeProxyVolume == null)
                throw new ArgumentException("lightProbeProxyVolume", "Argument lightProbeProxyVolume must not be null if lightProbeUsage is set to UseProxyVolume.");

            if (count > 0)
                Internal_DrawMeshInstanced(mesh, submeshIndex, material, matrices, count, properties, castShadows, receiveShadows, layer, camera, lightProbeUsage, lightProbeProxyVolume);
        }

        #endregion

        // TODO: Migrate these dreadful overloads to default arguments.
        #region public static void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, List<Matrix4x4> matrices, MaterialPropertyBlock properties = null, ShadowCastingMode castShadows = ShadowCastingMode.On, bool receiveShadows = true, int layer = 0, Camera camera = null, LightProbeUsage lightProbeUsage = LightProbeUsage.BlendProbes, LightProbeyProxyVolume lightProbeProxyVolume = null)
        [uei.ExcludeFromDocs]
        public static void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, List<Matrix4x4> matrices)
        {
            DrawMeshInstanced(mesh, submeshIndex, material, matrices, null, ShadowCastingMode.On, true, 0, null, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, List<Matrix4x4> matrices, MaterialPropertyBlock properties)
        {
            DrawMeshInstanced(mesh, submeshIndex, material, matrices, properties, ShadowCastingMode.On, true, 0, null, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, List<Matrix4x4> matrices, MaterialPropertyBlock properties, ShadowCastingMode castShadows)
        {
            DrawMeshInstanced(mesh, submeshIndex, material, matrices, properties, castShadows, true, 0, null, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, List<Matrix4x4> matrices, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows)
        {
            DrawMeshInstanced(mesh, submeshIndex, material, matrices, properties, castShadows, receiveShadows, 0, null, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, List<Matrix4x4> matrices, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer)
        {
            DrawMeshInstanced(mesh, submeshIndex, material, matrices, properties, castShadows, receiveShadows, layer, null, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, List<Matrix4x4> matrices, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer, Camera camera)
        {
            DrawMeshInstanced(mesh, submeshIndex, material, matrices, properties, castShadows, receiveShadows, layer, camera, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, List<Matrix4x4> matrices, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer, Camera camera, LightProbeUsage lightProbeUsage)
        {
            DrawMeshInstanced(mesh, submeshIndex, material, matrices, properties, castShadows, receiveShadows, layer, camera, lightProbeUsage, null);
        }

        public static void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, List<Matrix4x4> matrices, [uei.DefaultValue("null")] MaterialPropertyBlock properties, [uei.DefaultValue("ShadowCastingMode.On")] ShadowCastingMode castShadows, [uei.DefaultValue("true")] bool receiveShadows, [uei.DefaultValue("0")] int layer, [uei.DefaultValue("null")] Camera camera, [uei.DefaultValue("LightProbeUsage.BlendProbes")] LightProbeUsage lightProbeUsage, [uei.DefaultValue("null")] LightProbeProxyVolume lightProbeProxyVolume)
        {
            if (matrices == null)
                throw new ArgumentNullException("matrices");

            DrawMeshInstanced(mesh, submeshIndex, material, NoAllocHelpers.ExtractArrayFromListT(matrices), matrices.Count, properties, castShadows, receiveShadows, layer, camera, lightProbeUsage, lightProbeProxyVolume);
        }

        #endregion

        [FreeFunction("DrawMeshInstancedFromScript")]
        extern internal static void Internal_DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, Matrix4x4[] matrices, int count, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer, Camera camera, LightProbeUsage lightProbeUsage, LightProbeProxyVolume lightProbeProxyVolume);

        #region public static void DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, Bounds bounds, ComputeBuffer bufferWithArgs, int argsOffset = 0, MaterialPropertyBlock properties = null, ShadowCastingMode castShadows = ShadowCastingMode.On, bool receiveShadows = true, int layer = 0, Camera camera = null, LightProbeUsage lightProbeUsage = LightProbeUsage.BlendProbes, LightProbeProxyVolume lightProbeProxyVolume = null)
        [uei.ExcludeFromDocs]
        public static void DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, Bounds bounds, ComputeBuffer bufferWithArgs)
        {
            DrawMeshInstancedIndirect(mesh, submeshIndex, material, bounds, bufferWithArgs, 0, null, ShadowCastingMode.On, true, 0, null, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, Bounds bounds, ComputeBuffer bufferWithArgs, int argsOffset)
        {
            DrawMeshInstancedIndirect(mesh, submeshIndex, material, bounds, bufferWithArgs, argsOffset, null, ShadowCastingMode.On, true, 0, null, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, Bounds bounds, ComputeBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties)
        {
            DrawMeshInstancedIndirect(mesh, submeshIndex, material, bounds, bufferWithArgs, argsOffset, properties, ShadowCastingMode.On, true, 0, null, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, Bounds bounds, ComputeBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties, ShadowCastingMode castShadows)
        {
            DrawMeshInstancedIndirect(mesh, submeshIndex, material, bounds, bufferWithArgs, argsOffset, properties, castShadows, true, 0, null, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, Bounds bounds, ComputeBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows)
        {
            DrawMeshInstancedIndirect(mesh, submeshIndex, material, bounds, bufferWithArgs, argsOffset, properties, castShadows, receiveShadows, 0, null, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, Bounds bounds, ComputeBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer)
        {
            DrawMeshInstancedIndirect(mesh, submeshIndex, material, bounds, bufferWithArgs, argsOffset, properties, castShadows, receiveShadows, layer, null, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, Bounds bounds, ComputeBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer, Camera camera)
        {
            DrawMeshInstancedIndirect(mesh, submeshIndex, material, bounds, bufferWithArgs, argsOffset, properties, castShadows, receiveShadows, layer, camera, LightProbeUsage.BlendProbes, null);
        }

        [uei.ExcludeFromDocs]
        public static void DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, Bounds bounds, ComputeBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer, Camera camera, LightProbeUsage lightProbeUsage)
        {
            DrawMeshInstancedIndirect(mesh, submeshIndex, material, bounds, bufferWithArgs, argsOffset, properties, castShadows, receiveShadows, layer, camera, lightProbeUsage, null);
        }

        public static void DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, Bounds bounds, ComputeBuffer bufferWithArgs, [uei.DefaultValue("0")] int argsOffset, [uei.DefaultValue("null")] MaterialPropertyBlock properties, [uei.DefaultValue("ShadowCastingMode.On")] ShadowCastingMode castShadows, [uei.DefaultValue("true")] bool receiveShadows, [uei.DefaultValue("0")] int layer, [uei.DefaultValue("null")] Camera camera, [uei.DefaultValue("LightProbeUsage.BlendProbes")] LightProbeUsage lightProbeUsage, [uei.DefaultValue("null")] LightProbeProxyVolume lightProbeProxyVolume)
        {
            if (!SystemInfo.supportsInstancing)
                throw new InvalidOperationException("Instancing is not supported.");
            else if (mesh == null)
                throw new ArgumentNullException("mesh");
            else if (submeshIndex < 0 || submeshIndex >= mesh.subMeshCount)
                throw new ArgumentOutOfRangeException("submeshIndex", "submeshIndex out of range.");
            else if (material == null)
                throw new ArgumentNullException("material");
            else if (bufferWithArgs == null)
                throw new ArgumentNullException("bufferWithArgs");
            if (lightProbeUsage == LightProbeUsage.UseProxyVolume && lightProbeProxyVolume == null)
                throw new ArgumentException("lightProbeProxyVolume", "Argument lightProbeProxyVolume must not be null if lightProbeUsage is set to UseProxyVolume.");

            Internal_DrawMeshInstancedIndirect(mesh, submeshIndex, material, bounds, bufferWithArgs, argsOffset, properties, castShadows, receiveShadows, layer, camera, lightProbeUsage, lightProbeProxyVolume);
        }

        #endregion

        [FreeFunction("DrawMeshInstancedIndirectFromScript")]
        extern internal static void Internal_DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, Bounds bounds, ComputeBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer, Camera camera, LightProbeUsage lightProbeUsage, LightProbeProxyVolume lightProbeProxyVolume);

        [FreeFunction("GraphicsScripting::BlitMaterial")]
        extern private static void Internal_BlitMaterial(Texture source, RenderTexture dest, [NotNull] Material mat, int pass, bool setRT);

        [FreeFunction("GraphicsScripting::BlitMultitap")]
        extern private static void Internal_BlitMultiTap(Texture source, RenderTexture dest, [NotNull] Material mat, [NotNull] Vector2[] offsets);

        [FreeFunction("GraphicsScripting::Blit")]
        extern private static void Blit2(Texture source, RenderTexture dest);

        [FreeFunction("GraphicsScripting::Blit")]
        extern private static void Blit4(Texture source, RenderTexture dest, Vector2 scale, Vector2 offset);
    }
}

namespace UnityEngine
{
    public sealed partial class GL
    {
        public const int TRIANGLES      = 0x0004;
        public const int TRIANGLE_STRIP = 0x0005;
        public const int QUADS          = 0x0007;
        public const int LINES          = 0x0001;
        public const int LINE_STRIP     = 0x0002;
    }


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

        extern private static Matrix4x4 GetWorldViewMatrix();
        extern private static void SetViewMatrix(Matrix4x4 m);
        static public Matrix4x4 modelview { get { return GetWorldViewMatrix(); } set { SetViewMatrix(value); } }

        [NativeName("SetWorldMatrix")] extern public static void MultMatrix(Matrix4x4 m);

        [Obsolete("IssuePluginEvent(eventID) is deprecated. Use IssuePluginEvent(callback, eventID) instead.", false)]
        [NativeName("InsertCustomMarker")] extern public static void IssuePluginEvent(int eventID);

        [Obsolete("SetRevertBackfacing(revertBackFaces) is deprecated. Use invertCulling property instead.", false)]
        [NativeName("SetUserBackfaceMode")] extern public static void SetRevertBackfacing(bool revertBackFaces);
    }

    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
    [NativeHeader("Runtime/Camera/Camera.h")]
    [NativeHeader("Runtime/Camera/CameraUtil.h")]
    public sealed partial class GL
    {
        [FreeFunction("GLPushMatrixScript")]            extern public static void PushMatrix();
        [FreeFunction("GLPopMatrixScript")]             extern public static void PopMatrix();
        [FreeFunction("GLLoadIdentityScript")]          extern public static void LoadIdentity();
        [FreeFunction("GLLoadOrthoScript")]             extern public static void LoadOrtho();
        [FreeFunction("GLLoadPixelMatrixScript")]       extern public static void LoadPixelMatrix();
        [FreeFunction("GLLoadProjectionMatrixScript")]  extern public static void LoadProjectionMatrix(Matrix4x4 mat);
        [FreeFunction("GLInvalidateState")]             extern public static void InvalidateState();
        [FreeFunction("GLGetGPUProjectionMatrix")]      extern public static Matrix4x4 GetGPUProjectionMatrix(Matrix4x4 proj, bool renderIntoTexture);

        [FreeFunction] extern private static void GLLoadPixelMatrixScript(float left, float right, float bottom, float top);
        public static void LoadPixelMatrix(float left, float right, float bottom, float top)
        {
            GLLoadPixelMatrixScript(left, right, bottom, top);
        }

        [FreeFunction] extern private static void GLIssuePluginEvent(IntPtr callback, int eventID);
        public static void IssuePluginEvent(IntPtr callback, int eventID)
        {
            if (callback == IntPtr.Zero)
                throw new ArgumentException("Null callback specified.", "callback");
            GLIssuePluginEvent(callback, eventID);
        }

        [FreeFunction("GLBegin", ThrowsException = true)] extern public static void Begin(int mode);
        [FreeFunction("GLEnd")]                           extern public static void End();

        [FreeFunction] extern private static void GLClear(bool clearDepth, bool clearColor, Color backgroundColor, float depth);
        static public void Clear(bool clearDepth, bool clearColor, Color backgroundColor, [uei.DefaultValue("1.0f")] float depth)
        {
            GLClear(clearDepth, clearColor, backgroundColor, depth);
        }

        static public void Clear(bool clearDepth, bool clearColor, Color backgroundColor)
        {
            GLClear(clearDepth, clearColor, backgroundColor, 1.0f);
        }

        [FreeFunction("SetGLViewport")] extern public static void Viewport(Rect pixelRect);
        [FreeFunction("ClearWithSkybox")] extern public static void ClearWithSkybox(bool clearDepth, Camera camera);
    }
}

namespace UnityEngine
{
    // Scales render textures to support dynamic resolution.
    [NativeHeader("Runtime/GfxDevice/ScalableBufferManager.h")]
    [StaticAccessor("ScalableBufferManager::GetInstance()", StaticAccessorType.Dot)]
    static public class ScalableBufferManager
    {
        extern static public float widthScaleFactor { get; }
        extern static public float heightScaleFactor { get; }

        static extern public void ResizeBuffers(float widthScale, float heightScale);
    }

    [NativeHeader("Runtime/GfxDevice/FrameTiming.h")]
    [StructLayout(LayoutKind.Sequential)]
    public struct FrameTiming
    {
        // CPU events

        [NativeName("m_CPUTimePresentCalled")]  public UInt64 cpuTimePresentCalled;
        [NativeName("m_CPUFrameTime")]          public double cpuFrameTime;

        // GPU events

        //This is the time the GPU finishes rendering the frame and interrupts the CPU
        [NativeName("m_CPUTimeFrameComplete")]  public UInt64 cpuTimeFrameComplete;
        [NativeName("m_GPUFrameTime")]          public double gpuFrameTime;

        //Linked data

        [NativeName("m_HeightScale")]           public float heightScale;
        [NativeName("m_WidthScale")]            public float widthScale;
        [NativeName("m_SyncInterval")]          public UInt32 syncInterval;
    }

    [StaticAccessor("GetFrameTimingManager()", StaticAccessorType.Dot)]
    static public class FrameTimingManager
    {
        static extern public void CaptureFrameTimings();
        static extern public UInt32 GetLatestTimings(UInt32 numFrames, FrameTiming[] timings);

        static extern public float GetVSyncsPerSecond();
        static extern public UInt64 GetGpuTimerFrequency();
        static extern public UInt64 GetCpuTimerFrequency();
    }
}

namespace UnityEngine
{
    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
    public sealed partial class GeometryUtility
    {
        [FreeFunction("GeometryUtilityScripting::ExtractPlanes")]
        extern private static void Internal_ExtractPlanes([Out] Plane[] planes, Matrix4x4 worldToProjectionMatrix);
        [FreeFunction("GeometryUtilityScripting::TestPlanesAABB")]
        extern public static bool TestPlanesAABB(Plane[] planes, Bounds bounds);
        [FreeFunction("GeometryUtilityScripting::CalculateBounds")]
        extern private static Bounds Internal_CalculateBounds(Vector3[] positions, Matrix4x4 transform);
    }
}

namespace UnityEngine.Experimental.Rendering
{
    public enum VertexAttribute
    {
        Position = 0,   // Vertex (vector3)
        Normal,       // Normal (vector3)
        Tangent,      // Tangent (vector4)
        Color,        // Vertex color
        TexCoord0,    // Texcoord 0
        TexCoord1,    // Texcoord 1
        TexCoord2,    // Texcoord 2
        TexCoord3,    // Texcoord 3
        TexCoord4,    // Texcoord 4
        TexCoord5,    // Texcoord 5
        TexCoord6,    // Texcoord 6
        TexCoord7,    // Texcoord 7
    }
}
