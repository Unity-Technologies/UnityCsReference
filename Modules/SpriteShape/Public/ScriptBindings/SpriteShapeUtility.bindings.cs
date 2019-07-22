// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.U2D
{
    [StructLayoutAttribute(LayoutKind.Sequential)]
    [MovedFrom("UnityEngine.Experimental.U2D")]
    public struct SpriteShapeMetaData
    {
        public float height;
        public float bevelCutoff;
        public float bevelSize;
        public uint spriteIndex;
        public bool corner;
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    [MovedFrom("UnityEngine.Experimental.U2D")]
    public struct ShapeControlPoint
    {
        public Vector3 position;
        public Vector3 leftTangent;
        public Vector3 rightTangent;
        public int mode;
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    [MovedFrom("UnityEngine.Experimental.U2D")]
    public struct AngleRangeInfo
    {
        public float start;
        public float end;
        public uint order;
        public int[] sprites;
    }

    [NativeHeader("Modules/SpriteShape/Public/SpriteShapeUtility.h")]
    [MovedFrom("UnityEngine.Experimental.U2D")]
    public class SpriteShapeUtility
    {
        [NativeThrows]
        [FreeFunction("SpriteShapeUtility::Generate")]
        extern public static int[] Generate(Mesh mesh, SpriteShapeParameters shapeParams, ShapeControlPoint[] points, SpriteShapeMetaData[] metaData, AngleRangeInfo[] angleRange, Sprite[] sprites, Sprite[] corners);
        [NativeThrows]
        [FreeFunction("SpriteShapeUtility::GenerateSpriteShape")]
        extern public static void GenerateSpriteShape(SpriteShapeRenderer renderer, SpriteShapeParameters shapeParams, ShapeControlPoint[] points, SpriteShapeMetaData[] metaData, AngleRangeInfo[] angleRange, Sprite[] sprites, Sprite[] corners);
    }
}
