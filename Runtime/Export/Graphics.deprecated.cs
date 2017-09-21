// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using ShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode;
using LightProbeUsage   = UnityEngine.Rendering.LightProbeUsage;


namespace UnityEngine
{
    partial class Mesh
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Property Mesh.uv1 has been deprecated. Use Mesh.uv2 instead (UnityUpgradable) -> uv2", true)]
        public Vector2[] uv1 { get { return null; } set {} }
    }

    partial class Renderer
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Property lightmapTilingOffset has been deprecated. Use lightmapScaleOffset (UnityUpgradable) -> lightmapScaleOffset", true)]
        public Vector4 lightmapTilingOffset { get { return Vector4.zero; } set {} }

        [Obsolete("Use probeAnchor instead (UnityUpgradable) -> probeAnchor", true)]
        public Transform lightProbeAnchor { get { return probeAnchor; } set { probeAnchor = value; } }
    }

    partial class Projector
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Property isOrthoGraphic has been deprecated. Use orthographic instead (UnityUpgradable) -> orthographic", true)]
        public bool isOrthoGraphic { get { return false; } set {} }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Property orthoGraphicSize has been deprecated. Use orthographicSize instead (UnityUpgradable) -> orthographicSize", true)]
        public float orthoGraphicSize { get { return -1f; } set {} }
    }

    partial class Graphics
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Method DrawMesh has been deprecated. Use Graphics.DrawMeshNow instead (UnityUpgradable) -> DrawMeshNow(*)", true)]
        static public void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation) {}

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Method DrawMesh has been deprecated. Use Graphics.DrawMeshNow instead (UnityUpgradable) -> DrawMeshNow(*)", true)]
        static public void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, int materialIndex) {}

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Method DrawMesh has been deprecated. Use Graphics.DrawMeshNow instead (UnityUpgradable) -> DrawMeshNow(*)", true)]
        static public void DrawMesh(Mesh mesh, Matrix4x4 matrix) {}

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Method DrawMesh has been deprecated. Use Graphics.DrawMeshNow instead (UnityUpgradable) -> DrawMeshNow(*)", true)]
        static public void DrawMesh(Mesh mesh, Matrix4x4 matrix, int materialIndex) {}

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Property deviceName has been deprecated. Use SystemInfo.graphicsDeviceName instead (UnityUpgradable) -> UnityEngine.SystemInfo.graphicsDeviceName", true)]
        static public string deviceName { get { return SystemInfo.graphicsDeviceName; } }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Property deviceVendor has been deprecated. Use SystemInfo.graphicsDeviceVendor instead (UnityUpgradable) -> UnityEngine.SystemInfo.graphicsDeviceVendor", true)]
        static public string deviceVendor { get { return SystemInfo.graphicsDeviceVendor; } }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Property deviceVersion has been deprecated. Use SystemInfo.graphicsDeviceVersion instead (UnityUpgradable) -> UnityEngine.SystemInfo.graphicsDeviceVersion", true)]
        static public string deviceVersion { get { return SystemInfo.graphicsDeviceVersion; } }
    }

    partial class Screen
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Property GetResolution has been deprecated. Use resolutions instead (UnityUpgradable) -> resolutions", true)]
        static public Resolution[] GetResolution { get { return null; } }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Property showCursor has been deprecated. Use Cursor.visible instead (UnityUpgradable) -> UnityEngine.Cursor.visible", true)]
        static public bool showCursor { get; set; }
    }

    partial class LightmapData
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Property LightmapData.lightmap has been deprecated. Use LightmapData.lightmapColor instead (UnityUpgradable) -> lightmapColor", true)]
        public Texture2D lightmap { get { return default(Texture2D); } set {} }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Property LightmapData.lightmapFar has been deprecated. Use LightmapData.lightmapColor instead (UnityUpgradable) -> lightmapColor", true)]
        public Texture2D lightmapFar { get { return null; } set {} }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Property LightmapData.lightmapNear has been deprecated. Use LightmapData.lightmapDir instead (UnityUpgradable) -> lightmapDir", true)]
        public Texture2D lightmapNear { get { return null; } set {} }
    }

    partial class Shader
    {
        [Obsolete("SetGlobalTexGenMode is not supported anymore. Use programmable shaders to achieve the same effect.", true)]
        public static void SetGlobalTexGenMode(string propertyName, TexGenMode mode)            {}
        [Obsolete("SetGlobalTextureMatrixName is not supported anymore. Use programmable shaders to achieve the same effect.", true)]
        public static void SetGlobalTextureMatrixName(string propertyName, string matrixName)   {}
    }

    public enum LightmapsModeLegacy
    {
        Single = 0,
        Dual = 1,
        Directional = 2,
    }

    partial class LightmapSettings
    {
        [Obsolete("Use lightmapsMode instead.", false)]
        public static LightmapsModeLegacy lightmapsModeLegacy { get { return LightmapsModeLegacy.Single; } set {} }
        [Obsolete("Use QualitySettings.desiredColorSpace instead.", false)]
        public static ColorSpace bakedColorSpace { get { return QualitySettings.desiredColorSpace; } set {} }
    }

    partial class LightProbes
    {
        [Obsolete("Use GetInterpolatedProbe instead.", true)]
        public void GetInterpolatedLightProbe(Vector3 position, Renderer renderer, float[] coefficients) {}
        [Obsolete("Use bakedProbes instead.", true)]
        public float[] coefficients { get { return new float[0]; } set {} }
    }

    partial class TrailRenderer
    {
        [Obsolete("Use positionCount instead (UnityUpgradable) -> positionCount", false)]
        public int numPositions { get { return positionCount; } }
    }

    partial class LineRenderer
    {
        [Obsolete("Use startWidth, endWidth or widthCurve instead.", false)]
        public void SetWidth(float start, float end)
        {
            startWidth = start;
            endWidth = end;
        }

        [Obsolete("Use startColor, endColor or colorGradient instead.", false)]
        public void SetColors(Color start, Color end)
        {
            startColor = start;
            endColor = end;
        }

        [Obsolete("Use positionCount instead.", false)]
        public void SetVertexCount(int count)
        {
            positionCount = count;
        }

        [Obsolete("Use positionCount instead (UnityUpgradable) -> positionCount", false)]
        public int numPositions { get { return positionCount; } set { positionCount = value; } }
    }

    partial class MaterialPropertyBlock
    {
        // TODO: effectively adding a property or setting a property should be the same, but SetFloat will be a bit slower due to an extra lookup...

        [Obsolete("Use SetFloat instead (UnityUpgradable) -> SetFloat(*)", false)]
        public void AddFloat(string name, float value) { SetFloat(Shader.PropertyToID(name), value); }
        [Obsolete("Use SetFloat instead (UnityUpgradable) -> SetFloat(*)", false)]
        public void AddFloat(int nameID, float value)  { SetFloat(nameID, value); }

        [Obsolete("Use SetVector instead (UnityUpgradable) -> SetVector(*)", false)]
        public void AddVector(string name, Vector4 value)   { SetVector(Shader.PropertyToID(name), value); }
        [Obsolete("Use SetVector instead (UnityUpgradable) -> SetVector(*)", false)]
        public void AddVector(int nameID, Vector4 value)    { SetVector(nameID, value); }

        [Obsolete("Use SetColor instead (UnityUpgradable) -> SetColor(*)", false)]
        public void AddColor(string name, Color value)  { SetColor(Shader.PropertyToID(name), value); }
        [Obsolete("Use SetColor instead (UnityUpgradable) -> SetColor(*)", false)]
        public void AddColor(int nameID, Color value)   { SetColor(nameID, value); }

        [Obsolete("Use SetMatrix instead (UnityUpgradable) -> SetMatrix(*)", false)]
        public void AddMatrix(string name, Matrix4x4 value) { SetMatrix(Shader.PropertyToID(name), value); }
        [Obsolete("Use SetMatrix instead (UnityUpgradable) -> SetMatrix(*)", false)]
        public void AddMatrix(int nameID, Matrix4x4 value)  { SetMatrix(nameID, value); }

        [Obsolete("Use SetTexture instead (UnityUpgradable) -> SetTexture(*)", false)]
        public void AddTexture(string name, Texture value)  { SetTexture(Shader.PropertyToID(name), value); }
        [Obsolete("Use SetTexture instead (UnityUpgradable) -> SetTexture(*)", false)]
        public void AddTexture(int nameID, Texture value)   { SetTexture(nameID, value); }
    }

    partial class QualitySettings
    {
        [Obsolete("Use GetQualityLevel and SetQualityLevel", false)]
        public static QualityLevel currentLevel { get { return (QualityLevel)GetQualityLevel(); } set { SetQualityLevel((int)value, true); } }
    }

    partial class Renderer
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use shadowCastingMode instead.", false)]
        public bool castShadows
        {
            get { return shadowCastingMode != ShadowCastingMode.Off; }
            set { shadowCastingMode = value ? ShadowCastingMode.On : ShadowCastingMode.Off; }
        }

        [Obsolete("Use motionVectorGenerationMode instead.", false)]
        public bool motionVectors
        {
            get { return motionVectorGenerationMode == MotionVectorGenerationMode.Object; }
            set { motionVectorGenerationMode = value ? MotionVectorGenerationMode.Object : MotionVectorGenerationMode.Camera; }
        }

        [Obsolete("Use lightProbeUsage instead.", false)]
        public bool useLightProbes
        {
            get { return lightProbeUsage != LightProbeUsage.Off; }
            set { lightProbeUsage = value ? LightProbeUsage.BlendProbes : LightProbeUsage.Off; }
        }
    }

    partial class RenderSettings
    {
        [Obsolete("Use RenderSettings.ambientIntensity instead (UnityUpgradable) -> ambientIntensity", false)]
        public static float ambientSkyboxAmount { get { return ambientIntensity; } set { ambientIntensity = value; } }
    }

    partial class Screen
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use Cursor.lockState and Cursor.visible instead.", false)]
        public static bool lockCursor
        {
            get { return CursorLockMode.Locked == Cursor.lockState; }
            set
            {
                if (value) { Cursor.visible = false; Cursor.lockState = CursorLockMode.Locked; }
                else        { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            }
        }
    }

    partial class Shader
    {
        [Obsolete("Use Graphics.activeTier instead (UnityUpgradable) -> UnityEngine.Graphics.activeTier", false)]
        public static UnityEngine.Rendering.ShaderHardwareTier globalShaderHardwareTier
        {
            get { return (UnityEngine.Rendering.ShaderHardwareTier)Graphics.activeTier; }
            set { Graphics.activeTier = (UnityEngine.Rendering.GraphicsTier)value; }
        }
    }
}

namespace UnityEngine.Rendering
{
    // deprecated in 5.5
    [Obsolete("ShaderHardwareTier was renamed to GraphicsTier (UnityUpgradable) -> GraphicsTier", false)]
    public enum ShaderHardwareTier
    {
        Tier1 = 0,
        Tier2 = 1,
        Tier3 = 2,
    }
}
