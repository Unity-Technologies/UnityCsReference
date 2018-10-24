// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Experimental.U2D
{
    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct SpriteShapeMetaData
    {
        public float height;
        public float bevelCutoff;
        public float bevelSize;
        public uint spriteIndex;
        public bool corner;
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct ShapeControlPoint
    {
        public Vector3 position;
        public Vector3 leftTangent;
        public Vector3 rightTangent;
        public int mode;
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct AngleRangeInfo
    {
        public float start;
        public float end;
        public uint order;
        public int[] sprites;
    }

    [NativeHeader("Modules/SpriteShape/Public/SpriteShapeUtility.h")]
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
