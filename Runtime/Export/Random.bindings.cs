// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngineInternal;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // Class for generating random data.
    [NativeHeader("Runtime/Export/Random.bindings.h")]
    public sealed partial class Random
    {
        // Random number generator engine state struct
        [System.Serializable]
        public struct State
        {
#pragma warning disable 0169
            [SerializeField]
            private int s0;
            [SerializeField]
            private int s1;
            [SerializeField]
            private int s2;
            [SerializeField]
            private int s3;
        }

        // Gets/Sets the seed for the random number generator.
        [StaticAccessor("GetScriptingRand()", StaticAccessorType.Dot)]
        [Obsolete("Deprecated. Use InitState() function or Random.state property instead.")]
        extern public static int seed { get; set; }

        // Initializes the RNG state with a 32 bit seed
        [StaticAccessor("GetScriptingRand()", StaticAccessorType.Dot)]
        [NativeMethod("SetSeed")]
        extern public static void InitState(int seed);

        // Gets/Sets the state of the random number generator.
        [StaticAccessor("GetScriptingRand()", StaticAccessorType.Dot)]
        extern public static Random.State state { get; set; }

        // Returns a random float number between and /min/ [inclusive] and /max/ [inclusive] (RO).
        [FreeFunction]
        extern public static float Range(float min, float max);

        // Returns a random integer number between /min/ [inclusive] and /max/ [exclusive] (RO).
        public static int Range(int min, int max) { return RandomRangeInt(min, max); }

        [FreeFunction]
        extern private static int RandomRangeInt(int min, int max);

        // Returns a random number between 0.0 [inclusive] and 1.0 [inclusive] (RO).
        extern public static float value
        {
            [FreeFunction]
            get;
        }

        // Returns a random point inside a sphere with radius 1 (RO).
        extern public static Vector3 insideUnitSphere
        {
            [FreeFunction]
            get;
        }

        // Workaround for gcc/msvc where passing small mono structures by value does not work
        [FreeFunction]
        extern private static void GetRandomUnitCircle(out Vector2 output);

        // Returns a random point inside a circle with radius 1 (RO).
        public static Vector2 insideUnitCircle { get { Vector2 r; GetRandomUnitCircle(out r); return r; } }

        // Returns a random point on the surface of a sphere with radius 1 (RO).
        extern public static Vector3 onUnitSphere
        {
            [FreeFunction]
            get;
        }

        // Returns a random rotation (RO).
        extern public static Quaternion rotation
        {
            [FreeFunction]
            get;
        }

        // Returns a random rotation with uniform distribution(RO).
        extern public static Quaternion rotationUniform
        {
            [FreeFunction]
            get;
        }

        [Obsolete("Use Random.Range instead")]
        public static float RandomRange(float min, float max)  { return Range(min, max); }

        [Obsolete("Use Random.Range instead")]
        public static int RandomRange(int min, int max) { return Range(min, max); }
    }
}
