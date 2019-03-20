// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeType("Modules/SketchUpEditor/SketchUpImporter.h")]
    [System.Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct SketchUpImportCamera
    {
        public Vector3 position;
        public Vector3 lookAt;
        public Vector3 up;
        public float fieldOfView;
        public float aspectRatio;
        public float orthoSize;
        public float nearPlane;
        public float farPlane;
        public bool isPerspective;
    }

    [NativeType("Modules/SketchUpEditor/SketchUpImporter.h")]
    [System.Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct SketchUpImportScene
    {
        public SketchUpImportCamera camera;
        public string name;
    }

    [NativeType("Modules/SketchUpEditor/SketchUpNodeInfo.h")]
    [System.Serializable]
    [StructLayout(LayoutKind.Sequential)]
    internal struct SketchUpNodeInfo
    {
        public string name;
        public int parent;
        public bool enabled;
        public int nodeIndex;
    }

    [NativeHeader("Modules/SketchUpEditor/SketchUpImporter.h")]
    public sealed partial class SketchUpImporter : ModelImporter
    {
        extern public SketchUpImportScene[] GetScenes();

        extern public SketchUpImportCamera GetDefaultCamera();

        [NativeName("GetSketchUpImportNodes")]
        extern internal SketchUpNodeInfo[] GetNodes();

        public extern double latitude { get; }

        public extern double longitude { get; }

        public extern double northCorrection { get; }
    }
}
