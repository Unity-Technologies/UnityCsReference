// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using uei = UnityEngine.Internal;

namespace UnityEngine.Experimental.XR
{
    [NativeHeader("Runtime/VR/XRDeviceScriptApi.h")]

    public struct CompositorCapabilities
    {
        int m_MaxLayers;
        RenderTextureFormat m_ValidColorFormat; // temp workaround

        public int maxLayers { get { return m_MaxLayers; } internal set { m_MaxLayers = value; } }
        public RenderTextureFormat validColorFormat { get { return m_ValidColorFormat; } internal set { m_ValidColorFormat = value; } }
    }

    public struct CompositorLayerDescriptor
    {
        int m_Width;
        int m_Height;
        int m_SampleCount;
        int m_ColorFormatIndex;

        public int width { get { return m_Width; } set { m_Width = value; } }
        public int height { get { return m_Height; } set { m_Height = value; } }
        public int sampleCount { get { return m_SampleCount; } set { m_SampleCount = value; } }
        public int colorFormatIndex { get { return m_ColorFormatIndex; } set { m_ColorFormatIndex = value; } }
    }

    public enum CompositorLayerAnchor { TrackingOrigin, Viewer };

    public struct CompositorLayerState
    {
        RenderTexture m_TargetTexture;
        public Vector3 position;
        public Vector3 size;
        public Quaternion orientation;
        CompositorLayerAnchor m_Anchor;
        bool m_Visible;

        public RenderTexture targetTexture { get { return m_TargetTexture; } set { m_TargetTexture = value; } }
        public CompositorLayerAnchor anchor { get { return m_Anchor; } set { m_Anchor = value; } }
        public bool visible { get { return m_Visible; } set { m_Visible = value; } }
    }

    public static partial class XRDevice
    {
        extern public static CompositorCapabilities compositorCapabilities { get; }

        extern public static bool compositorCanRegisterLayers { get; }

        extern public static bool RegisterLayers(CompositorLayerDescriptor[] layerDescriptors, int descriptorCount);

        extern public static void UpdateLayerState(int layerIndex, CompositorLayerState layerState);

        extern public static RenderTexture GetNextTextureFromLayer(int layerIndex);
    }
}
