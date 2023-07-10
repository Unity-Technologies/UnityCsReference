// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
        public Color32 flags;
        public Vector4 circle;
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
    /// VectorImage is an opaque type. You cannot use it to author vector images. To author vector images, use the <see cref="Painter2D" /> class, or use the SVGImporter in the Vector Graphics package.
    ///
    /// To get the latest Vector Graphics package documentation, see the Packages Documentation page in the <a href="https://docs.unity3d.com/Manual/PackagesList.html">Unity Manual</a>.
    ///
    /// </remarks>
    [Serializable]
    public sealed class VectorImage : ScriptableObject
    {
        [SerializeField] internal int version = 0; // For future upgrades using ISerializationCallbackReceiver
        [SerializeField] internal Texture2D atlas = null;
        [SerializeField] internal VectorImageVertex[] vertices = null;
        [SerializeField] internal UInt16[] indices = null;
        [SerializeField] internal GradientSettings[] settings = null;
        [SerializeField] internal Vector2 size = Vector2.zero;

        /// <summary>The width of the vector image.</summary>
        public float width => size.x;

        /// <summary>The height of the vector image.</summary>
        public float height => size.y;
    }
}
