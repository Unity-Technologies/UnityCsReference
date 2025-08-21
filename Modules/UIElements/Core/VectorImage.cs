// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Specifies the type of gradient to use for color interpolation.
    /// </summary>
    /// <remarks>
    /// <see cref="Linear"/> creates a gradient that transitions colors along a straight line.
    /// <see cref="Radial"/> creates a gradient that radiates colors outward from a central point.
    /// </remarks>
    public enum GradientType
    {
        /// <summary>A gradient that transitions colors along a straight line.</summary>
        Linear,
        /// <summary>A gradient that radiates colors outward from a central point.</summary>
        Radial
    }

    /// <summary>
    /// Specifies how the gradient is sampled when UV coordinates are outside the [0, 1] range.
    /// </summary>
    /// <remarks>
    /// <see cref="Wrap"/> repeats the gradient, creating a tiled effect.
    /// <see cref="Clamp"/> extends the edge colors of the gradient beyond the [0, 1] range.
    /// <see cref="Mirror"/> repeats the gradient, but mirrors it at every integer boundary, creating a seamless mirrored tiling.
    /// </remarks>
    public enum AddressMode
    {
        /// <summary>
        /// Repeats the gradient when sampling outside the [0, 1] range, creating a tiled effect.
        /// </summary>
        Wrap,
        /// <summary>
        /// Extends the edge colors of the gradient beyond the [0, 1] range.
        /// </summary>
        Clamp,
        /// <summary>
        /// Repeats the gradient, but mirrors it at every integer boundary, creating a seamless mirrored tiling.
        /// </summary>
        Mirror
    }

    [VisibleToOtherModules("UnityEngine.VectorGraphicsModule", "UnityEditor.VectorGraphicsModule")]
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

    [VisibleToOtherModules("UnityEngine.VectorGraphicsModule", "UnityEditor.VectorGraphicsModule")]
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
        [VisibleToOtherModules("UnityEngine.VectorGraphicsModule")]
        [SerializeField] internal int version = 0; // For future upgrades using ISerializationCallbackReceiver

        [VisibleToOtherModules("UnityEngine.VectorGraphicsModule", "UnityEditor.VectorGraphicsModule")]
        [SerializeField] internal Texture2D atlas = null;

        [VisibleToOtherModules("UnityEngine.VectorGraphicsModule", "UnityEditor.VectorGraphicsModule")]
        [SerializeField] internal VectorImageVertex[] vertices = null;

        [VisibleToOtherModules("UnityEngine.VectorGraphicsModule")]
        [SerializeField] internal UInt16[] indices = null;

        [VisibleToOtherModules("UnityEngine.VectorGraphicsModule")]
        [SerializeField] internal GradientSettings[] settings = null;

        [VisibleToOtherModules("UnityEngine.VectorGraphicsModule")]
        [SerializeField] internal Vector2 size = Vector2.zero;

        /// <summary>The width of the vector image.</summary>
        public float width => size.x;

        /// <summary>The height of the vector image.</summary>
        public float height => size.y;

        private void OnDestroy()
        {
            if (atlas != null)
            {
                UIRUtility.Destroy(atlas);
            }
        }
    }
}
