// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngine.Bindings;
using UnityEngine.Internal;

namespace UnityEngine
{
    [NativeHeader("Runtime/Export/Gizmos/Gizmos.bindings.h")]
    [StaticAccessor("GizmoBindings", StaticAccessorType.DoubleColon)]
    public sealed partial class Gizmos
    {
        [NativeThrows]
        public static extern void DrawLine(Vector3 from, Vector3 to);

        [NativeThrows]
        public static extern void DrawWireSphere(Vector3 center, float radius);

        [NativeThrows]
        public static extern void DrawSphere(Vector3 center, float radius);

        [NativeThrows]
        public static extern void DrawWireCube(Vector3 center, Vector3 size);

        [NativeThrows]
        public static extern void DrawCube(Vector3 center, Vector3 size);

        [NativeThrows]
        public static extern void DrawMesh(Mesh mesh, int submeshIndex, [DefaultValue("Vector3.zero")] Vector3 position, [DefaultValue("Quaternion.identity")] Quaternion rotation, [DefaultValue("Vector3.one")] Vector3 scale);

        [NativeThrows]
        public static extern void DrawWireMesh(Mesh mesh, int submeshIndex, [DefaultValue("Vector3.zero")] Vector3 position, [DefaultValue("Quaternion.identity")] Quaternion rotation, [DefaultValue("Vector3.one")] Vector3 scale);

        [NativeThrows]
        public static void DrawIcon(Vector3 center, string name, [DefaultValue("true")] bool allowScaling)
        {
            DrawIcon(center, name, allowScaling, Color.white);
        }

        [NativeThrows]
        public static extern void DrawIcon(Vector3 center, string name, [DefaultValue("true")] bool allowScaling, [DefaultValue("Color(255,255,255,255)")] Color tint);

        [NativeThrows]
        public static extern void DrawGUITexture(Rect screenRect, Texture texture, int leftBorder, int rightBorder, int topBorder, int bottomBorder, [DefaultValue("null")] Material mat);

        public static extern Color color
        {
            get;
            set;
        }

        public static extern Matrix4x4 matrix
        {
            get;
            set;
        }

        public static extern Texture exposure
        {
            get;
            set;
        }

        public static extern float probeSize
        {
            get;
        }

        public static extern void DrawFrustum(Vector3 center, float fov, float maxRange, float minRange, float aspect);
    }
}
