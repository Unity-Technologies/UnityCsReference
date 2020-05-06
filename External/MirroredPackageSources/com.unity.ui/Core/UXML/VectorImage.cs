using System;

namespace UnityEngine.UIElements
{
    internal enum GradientType
    {
        Linear,
        Radial
    }

    internal enum AddressMode
    {
        Wrap,
        Clamp,
        Mirror
    }

    [Serializable]
    internal struct VectorImageVertex
    {
        public Vector3 position;
        public Color32 tint;
        public Vector2 uv;
        public UInt32 settingIndex;
    }

    [Serializable]
    internal struct GradientSettings
    {
        public GradientType gradientType;
        public AddressMode addressMode;
        public Vector2 radialFocus;
        public RectInt location;
    }

    /// <summary>
    /// An asset that represents a vector image.
    /// </summary>
    /// <remarks>
    /// VectorImage is an opaque type. You cannot use it to author vector images. To author vector images, use the SVGImporter in the Vector Graphics package.
    ///
    /// To get the latest Vector Graphics package documentation, see the Packages Documentation page in the <a href="https://docs.unity3d.com/Manual/PackagesList.html">Unity Manual</a>.
    ///
    /// </remarks>
    [Serializable]
    public class VectorImage : ScriptableObject
    {
        [SerializeField] internal Texture2D atlas = null;
        [SerializeField] internal VectorImageVertex[] vertices = null;
        [SerializeField] internal UInt16[] indices = null;
        [SerializeField] internal GradientSettings[] settings = null;
        [SerializeField] internal Vector2 size = Vector2.zero;
    }
}
