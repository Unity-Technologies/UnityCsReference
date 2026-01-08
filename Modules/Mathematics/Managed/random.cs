// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using static Unity.Mathematics.math;
using System.Diagnostics;
using Unity.IL2CPP.CompilerServices;

namespace Unity.Mathematics
{
    /// <summary>
    /// Random Number Generator based on xorshift.
    /// Designed for minimal state (32bits) to be easily embeddable into components.
    /// Core functionality is integer multiplication free to improve vectorization
    /// on less capable SIMD instruction sets.
    /// </summary>
    [Il2CppEagerStaticClassConstruction]
    [Serializable]
    public partial struct Random
    {
        /// <summary>
        /// The random number generator state. It should not be zero.
        /// </summary>
        public uint state;

        /// <summary>
        /// Constructs a Random instance with a given seed value. The seed must be non-zero.
        /// </summary>
        /// <param name="seed">The seed to initialize with.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Random(uint seed)
        {
            state = seed;
            CheckInitState();
            NextState();
        }

        /// <summary>
        /// Constructs a <see cref="Random"/> instance with an index that gets hashed.  The index must not be uint.MaxValue.
        /// </summary>
        /// <remarks>
        /// Use this function when you expect to create several Random instances in a loop.
        /// </remarks>
        /// <example>
        /// <code>
        /// for (uint i = 0; i &lt; 4096; ++i)
        /// {
        ///     Random rand = Random.CreateFromIndex(i);
        ///
        ///     // Random numbers drawn from loop iteration j will be very different
        ///     // from every other loop iteration k.
        ///     rand.NextUInt();
        /// }
        /// </code>
        /// </example>
        /// <param name="index">An index that will be hashed for Random creation.  Must not be uint.MaxValue.</param>
        /// <returns><see cref="Random"/> created from an index.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Random CreateFromIndex(uint index)
        {
            CheckIndexForHash(index);

            // Wang hash will hash 61 to zero but we want uint.MaxValue to hash to zero.  To make this happen
            // we must offset by 62.
            return new Random(WangHash(index + 62u));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint WangHash(uint n)
        {
            // https://gist.github.com/badboy/6267743#hash-function-construction-principles
            // Wang hash: this has the property that none of the outputs will
            // collide with each other, which is important for the purposes of
            // seeding a random number generator.  This was verified empirically
            // by checking all 2^32 uints.
            n = (n ^ 61u) ^ (n >> 16);
            n *= 9u;
            n = n ^ (n >> 4);
            n *= 0x27d4eb2du;
            n = n ^ (n >> 15);

            return n;
        }

        /// <summary>
        /// Initialized the state of the Random instance with a given seed value. The seed must be non-zero.
        /// </summary>
        /// <param name="seed">The seed to initialize with.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InitState(uint seed = 0x6E624EB7u)
        {
            state = seed;
            NextState();
        }

        /// <summary>Returns a uniformly random bool value.</summary>
        /// <returns>A uniformly random boolean value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool NextBool()
        {
            return (NextState() & 1) == 1;
        }

        /// <summary>Returns a uniformly random bool2 value.</summary>
        /// <returns>A uniformly random bool2 value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool2 NextBool2()
        {
            uint v = NextState();
            return (uint2(v) & uint2(1, 2)) == 0;
        }

        /// <summary>Returns a uniformly random bool3 value.</summary>
        /// <returns>A uniformly random bool3 value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool3 NextBool3()
        {
            uint v = NextState();
            return (uint3(v) & uint3(1, 2, 4)) == 0;
        }

        /// <summary>Returns a uniformly random bool4 value.</summary>
        /// <returns>A uniformly random bool4 value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool4 NextBool4()
        {
            uint v = NextState();
            return (uint4(v) & uint4(1, 2, 4, 8)) == 0;
        }


        /// <summary>Returns a uniformly random int value in the interval [-2147483647, 2147483647].</summary>
        /// <returns>A uniformly random integer value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int NextInt()
        {
            return (int)NextState() ^ -2147483648;
        }

        /// <summary>Returns a uniformly random int2 value with all components in the interval [-2147483647, 2147483647].</summary>
        /// <returns>A uniformly random int2 value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int2 NextInt2()
        {
            return int2((int)NextState(), (int)NextState()) ^ -2147483648;
        }

        /// <summary>Returns a uniformly random int3 value with all components in the interval [-2147483647, 2147483647].</summary>
        /// <returns>A uniformly random int3 value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int3 NextInt3()
        {
            return int3((int)NextState(), (int)NextState(), (int)NextState()) ^ -2147483648;
        }

        /// <summary>Returns a uniformly random int4 value with all components in the interval [-2147483647, 2147483647].</summary>
        /// <returns>A uniformly random int4 value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int4 NextInt4()
        {
            return int4((int)NextState(), (int)NextState(), (int)NextState(), (int)NextState()) ^ -2147483648;
        }

        /// <summary>Returns a uniformly random int value in the interval [0, max).</summary>
        /// <param name="max">The maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random int value in the range [0, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int NextInt(int max)
        {
            CheckNextIntMax(max);
            return (int)((NextState() * (ulong)max) >> 32);
        }

        /// <summary>Returns a uniformly random int2 value with all components in the interval [0, max).</summary>
        /// <param name="max">The componentwise maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random int2 value with all components in the range [0, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int2 NextInt2(int2 max)
        {
            CheckNextIntMax(max.x);
            CheckNextIntMax(max.y);
            return int2((int)(NextState() * (ulong)max.x >> 32),
                        (int)(NextState() * (ulong)max.y >> 32));
        }

        /// <summary>Returns a uniformly random int3 value with all components in the interval [0, max).</summary>
        /// <param name="max">The componentwise maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random int3 value with all components in the range [0, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int3 NextInt3(int3 max)
        {
            CheckNextIntMax(max.x);
            CheckNextIntMax(max.y);
            CheckNextIntMax(max.z);
            return int3((int)(NextState() * (ulong)max.x >> 32),
                        (int)(NextState() * (ulong)max.y >> 32),
                        (int)(NextState() * (ulong)max.z >> 32));
        }

        /// <summary>Returns a uniformly random int4 value with all components in the interval [0, max).</summary>
        /// <param name="max">The componentwise maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random int4 value with all components in the range [0, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int4 NextInt4(int4 max)
        {
            CheckNextIntMax(max.x);
            CheckNextIntMax(max.y);
            CheckNextIntMax(max.z);
            CheckNextIntMax(max.w);
            return int4((int)(NextState() * (ulong)max.x >> 32),
                        (int)(NextState() * (ulong)max.y >> 32),
                        (int)(NextState() * (ulong)max.z >> 32),
                        (int)(NextState() * (ulong)max.w >> 32));
        }

        /// <summary>Returns a uniformly random int value in the interval [min, max).</summary>
        /// <param name="min">The minimum value to generate, inclusive.</param>
        /// <param name="max">The maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random integer between [min, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int NextInt(int min, int max)
        {
            CheckNextIntMinMax(min, max);
            uint range = (uint)(max - min);
            return (int)(NextState() * (ulong)range >> 32) + min;
        }

        /// <summary>Returns a uniformly random int2 value with all components in the interval [min, max).</summary>
        /// <param name="min">The componentwise minimum value to generate, inclusive.</param>
        /// <param name="max">The componentwise maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random int2 between [min, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int2 NextInt2(int2 min, int2 max)
        {
            CheckNextIntMinMax(min.x, max.x);
            CheckNextIntMinMax(min.y, max.y);
            uint2 range = (uint2)(max - min);
            return int2((int)(NextState() * (ulong)range.x >> 32),
                        (int)(NextState() * (ulong)range.y >> 32)) + min;
        }

        /// <summary>Returns a uniformly random int3 value with all components in the interval [min, max).</summary>
        /// <param name="min">The componentwise minimum value to generate, inclusive.</param>
        /// <param name="max">The componentwise maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random int3 between [min, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int3 NextInt3(int3 min, int3 max)
        {
            CheckNextIntMinMax(min.x, max.x);
            CheckNextIntMinMax(min.y, max.y);
            CheckNextIntMinMax(min.z, max.z);
            uint3 range = (uint3)(max - min);
            return int3((int)(NextState() * (ulong)range.x >> 32),
                        (int)(NextState() * (ulong)range.y >> 32),
                        (int)(NextState() * (ulong)range.z >> 32)) + min;
        }

        /// <summary>Returns a uniformly random int4 value with all components in the interval [min, max).</summary>
        /// <param name="min">The componentwise minimum value to generate, inclusive.</param>
        /// <param name="max">The componentwise maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random int4 between [min, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int4 NextInt4(int4 min, int4 max)
        {
            CheckNextIntMinMax(min.x, max.x);
            CheckNextIntMinMax(min.y, max.y);
            CheckNextIntMinMax(min.z, max.z);
            CheckNextIntMinMax(min.w, max.w);
            uint4 range = (uint4)(max - min);
            return int4((int)(NextState() * (ulong)range.x >> 32),
                        (int)(NextState() * (ulong)range.y >> 32),
                        (int)(NextState() * (ulong)range.z >> 32),
                        (int)(NextState() * (ulong)range.w >> 32)) + min;
        }

        /// <summary>Returns a uniformly random uint value in the interval [0, 4294967294].</summary>
        /// <returns>A uniformly random unsigned integer.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint NextUInt()
        {
            return NextState() - 1u;
        }

        /// <summary>Returns a uniformly random uint2 value with all components in the interval [0, 4294967294].</summary>
        /// <returns>A uniformly random uint2.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint2 NextUInt2()
        {
            return uint2(NextState(), NextState()) - 1u;
        }

        /// <summary>Returns a uniformly random uint3 value with all components in the interval [0, 4294967294].</summary>
        /// <returns>A uniformly random uint3.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint3 NextUInt3()
        {
            return uint3(NextState(), NextState(), NextState()) - 1u;
        }

        /// <summary>Returns a uniformly random uint4 value with all components in the interval [0, 4294967294].</summary>
        /// <returns>A uniformly random uint4.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint4 NextUInt4()
        {
            return uint4(NextState(), NextState(), NextState(), NextState()) - 1u;
        }


        /// <summary>Returns a uniformly random uint value in the interval [0, max).</summary>
        /// <param name="max">The maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random unsigned integer in the range [0, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint NextUInt(uint max)
        {
            return (uint)((NextState() * (ulong)max) >> 32);
        }

        /// <summary>Returns a uniformly random uint2 value with all components in the interval [0, max).</summary>
        /// <param name="max">The componentwise maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random uint2 in the range [0, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint2 NextUInt2(uint2 max)
        {
            return uint2(   (uint)(NextState() * (ulong)max.x >> 32),
                            (uint)(NextState() * (ulong)max.y >> 32));
        }

        /// <summary>Returns a uniformly random uint3 value with all components in the interval [0, max).</summary>
        /// <param name="max">The componentwise maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random uint3 in the range [0, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint3 NextUInt3(uint3 max)
        {
            return uint3(   (uint)(NextState() * (ulong)max.x >> 32),
                            (uint)(NextState() * (ulong)max.y >> 32),
                            (uint)(NextState() * (ulong)max.z >> 32));
        }

        /// <summary>Returns a uniformly random uint4 value with all components in the interval [0, max).</summary>
        /// <param name="max">The componentwise maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random uint4 in the range [0, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint4 NextUInt4(uint4 max)
        {
            return uint4(   (uint)(NextState() * (ulong)max.x >> 32),
                            (uint)(NextState() * (ulong)max.y >> 32),
                            (uint)(NextState() * (ulong)max.z >> 32),
                            (uint)(NextState() * (ulong)max.w >> 32));
        }

        /// <summary>Returns a uniformly random uint value in the interval [min, max).</summary>
        /// <param name="min">The minimum value to generate, inclusive.</param>
        /// <param name="max">The maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random unsigned integer in the range [min, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint NextUInt(uint min, uint max)
        {
            CheckNextUIntMinMax(min, max);
            uint range = max - min;
            return (uint)(NextState() * (ulong)range >> 32) + min;
        }

        /// <summary>Returns a uniformly random uint2 value with all components in the interval [min, max).</summary>
        /// <param name="min">The componentwise minimum value to generate, inclusive.</param>
        /// <param name="max">The componentwise maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random uint2 in the range [min, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint2 NextUInt2(uint2 min, uint2 max)
        {
            CheckNextUIntMinMax(min.x, max.x);
            CheckNextUIntMinMax(min.y, max.y);
            uint2 range = max - min;
            return uint2((uint)(NextState() * (ulong)range.x >> 32),
                         (uint)(NextState() * (ulong)range.y >> 32)) + min;
        }

        /// <summary>Returns a uniformly random uint3 value with all components in the interval [min, max).</summary>
        /// <param name="min">The componentwise minimum value to generate, inclusive.</param>
        /// <param name="max">The componentwise maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random uint3 in the range [min, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint3 NextUInt3(uint3 min, uint3 max)
        {
            CheckNextUIntMinMax(min.x, max.x);
            CheckNextUIntMinMax(min.y, max.y);
            CheckNextUIntMinMax(min.z, max.z);
            uint3 range = max - min;
            return uint3((uint)(NextState() * (ulong)range.x >> 32),
                         (uint)(NextState() * (ulong)range.y >> 32),
                         (uint)(NextState() * (ulong)range.z >> 32)) + min;
        }

        /// <summary>Returns a uniformly random uint4 value with all components in the interval [min, max).</summary>
        /// <param name="min">The componentwise minimum value to generate, inclusive.</param>
        /// <param name="max">The componentwise maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random uint4 in the range [min, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint4 NextUInt4(uint4 min, uint4 max)
        {
            CheckNextUIntMinMax(min.x, max.x);
            CheckNextUIntMinMax(min.y, max.y);
            CheckNextUIntMinMax(min.z, max.z);
            CheckNextUIntMinMax(min.w, max.w);
            uint4 range = (uint4)(max - min);
            return uint4((uint)(NextState() * (ulong)range.x >> 32),
                         (uint)(NextState() * (ulong)range.y >> 32),
                         (uint)(NextState() * (ulong)range.z >> 32),
                         (uint)(NextState() * (ulong)range.w >> 32)) + min;
        }

        /// <summary>Returns a uniformly random float value in the interval [0, 1).</summary>
        /// <returns>A uniformly random float value in the range [0, 1).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float NextFloat()
        {
            return asfloat(0x3f800000 | (NextState() >> 9)) - 1.0f;
        }

        /// <summary>Returns a uniformly random float2 value with all components in the interval [0, 1).</summary>
        /// <returns>A uniformly random float2 value in the range [0, 1).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 NextFloat2()
        {
            return asfloat(0x3f800000 | (uint2(NextState(), NextState()) >> 9)) - 1.0f;
        }

        /// <summary>Returns a uniformly random float3 value with all components in the interval [0, 1).</summary>
        /// <returns>A uniformly random float3 value in the range [0, 1).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 NextFloat3()
        {
            return asfloat(0x3f800000 | (uint3(NextState(), NextState(), NextState()) >> 9)) - 1.0f;
        }

        /// <summary>Returns a uniformly random float4 value with all components in the interval [0, 1).</summary>
        /// <returns>A uniformly random float4 value in the range [0, 1).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float4 NextFloat4()
        {
            return asfloat(0x3f800000 | (uint4(NextState(), NextState(), NextState(), NextState()) >> 9)) - 1.0f;
        }


        /// <summary>Returns a uniformly random float value in the interval [0, max).</summary>
        /// <param name="max">The maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random float value in the range [0, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float NextFloat(float max) { return NextFloat() * max; }

        /// <summary>Returns a uniformly random float2 value with all components in the interval [0, max).</summary>
        /// <param name="max">The componentwise maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random float2 value in the range [0, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 NextFloat2(float2 max) { return NextFloat2() * max; }

        /// <summary>Returns a uniformly random float3 value with all components in the interval [0, max).</summary>
        /// <param name="max">The componentwise maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random float3 value in the range [0, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 NextFloat3(float3 max) { return NextFloat3() * max; }

        /// <summary>Returns a uniformly random float4 value with all components in the interval [0, max).</summary>
        /// <param name="max">The componentwise maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random float4 value in the range [0, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float4 NextFloat4(float4 max) { return NextFloat4() * max; }


        /// <summary>Returns a uniformly random float value in the interval [min, max).</summary>
        /// <param name="min">The minimum value to generate, inclusive.</param>
        /// <param name="max">The maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random float value in the range [min, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float NextFloat(float min, float max) { return NextFloat() * (max - min) + min; }

        /// <summary>Returns a uniformly random float2 value with all components in the interval [min, max).</summary>
        /// <param name="min">The componentwise minimum value to generate, inclusive.</param>
        /// <param name="max">The componentwise maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random float2 value in the range [min, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 NextFloat2(float2 min, float2 max) { return NextFloat2() * (max - min) + min; }

        /// <summary>Returns a uniformly random float3 value with all components in the interval [min, max).</summary>
        /// <param name="min">The componentwise minimum value to generate, inclusive.</param>
        /// <param name="max">The componentwise maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random float3 value in the range [min, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 NextFloat3(float3 min, float3 max) { return NextFloat3() * (max - min) + min; }

        /// <summary>Returns a uniformly random float4 value with all components in the interval [min, max).</summary>
        /// <param name="min">The componentwise minimum value to generate, inclusive.</param>
        /// <param name="max">The componentwise maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random float4 value in the range [min, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float4 NextFloat4(float4 min, float4 max) { return NextFloat4() * (max - min) + min; }



        /// <summary>Returns a uniformly random double value in the interval [0, 1).</summary>
        /// <returns>A uniformly random double value in the range [0, 1).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double NextDouble()
        {
            ulong sx = ((ulong)NextState() << 20) ^ NextState();
            return asdouble(0x3ff0000000000000 | sx) - 1.0;
        }

        /// <summary>Returns a uniformly random double2 value with all components in the interval [0, 1).</summary>
        /// <returns>A uniformly random double2 value in the range [0, 1).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double2 NextDouble2()
        {
            ulong sx = ((ulong)NextState() << 20) ^ NextState();
            ulong sy = ((ulong)NextState() << 20) ^ NextState();
            return double2(asdouble(0x3ff0000000000000 | sx),
                           asdouble(0x3ff0000000000000 | sy)) - 1.0;
        }

        /// <summary>Returns a uniformly random double3 value with all components in the interval [0, 1).</summary>
        /// <returns>A uniformly random double3 value in the range [0, 1).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double3 NextDouble3()
        {
            ulong sx = ((ulong)NextState() << 20) ^ NextState();
            ulong sy = ((ulong)NextState() << 20) ^ NextState();
            ulong sz = ((ulong)NextState() << 20) ^ NextState();
            return double3(asdouble(0x3ff0000000000000 | sx),
                           asdouble(0x3ff0000000000000 | sy),
                           asdouble(0x3ff0000000000000 | sz)) - 1.0;
        }

        /// <summary>Returns a uniformly random double4 value with all components in the interval [0, 1).</summary>
        /// <returns>A uniformly random double4 value in the range [0, 1).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double4 NextDouble4()
        {
            ulong sx = ((ulong)NextState() << 20) ^ NextState();
            ulong sy = ((ulong)NextState() << 20) ^ NextState();
            ulong sz = ((ulong)NextState() << 20) ^ NextState();
            ulong sw = ((ulong)NextState() << 20) ^ NextState();
            return double4(asdouble(0x3ff0000000000000 | sx),
                           asdouble(0x3ff0000000000000 | sy),
                           asdouble(0x3ff0000000000000 | sz),
                           asdouble(0x3ff0000000000000 | sw)) - 1.0;
        }


        /// <summary>Returns a uniformly random double value in the interval [0, max).</summary>
        /// <param name="max">The maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random double value in the range [0, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double NextDouble(double max) { return NextDouble() * max; }

        /// <summary>Returns a uniformly random double2 value with all components in the interval [0, max).</summary>
        /// <param name="max">The componentwise maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random double2 value in the range [0, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double2 NextDouble2(double2 max) { return NextDouble2() * max; }

        /// <summary>Returns a uniformly random double3 value with all components in the interval [0, max).</summary>
        /// <param name="max">The componentwise maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random double3 value in the range [0, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double3 NextDouble3(double3 max) { return NextDouble3() * max; }

        /// <summary>Returns a uniformly random double4 value with all components in the interval [0, max).</summary>
        /// <param name="max">The componentwise maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random double4 value in the range [0, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double4 NextDouble4(double4 max) { return NextDouble4() * max; }


        /// <summary>Returns a uniformly random double value in the interval [min, max).</summary>
        /// <param name="min">The minimum value to generate, inclusive.</param>
        /// <param name="max">The maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random double value in the range [min, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double NextDouble(double min, double max) { return NextDouble() * (max - min) + min; }

        /// <summary>Returns a uniformly random double2 value with all components in the interval [min, max).</summary>
        /// <param name="min">The componentwise minimum value to generate, inclusive.</param>
        /// <param name="max">The componentwise maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random double2 value in the range [min, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double2 NextDouble2(double2 min, double2 max) { return NextDouble2() * (max - min) + min; }

        /// <summary>Returns a uniformly random double3 value with all components in the interval [min, max).</summary>
        /// <param name="min">The componentwise minimum value to generate, inclusive.</param>
        /// <param name="max">The componentwise maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random double3 value in the range [min, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double3 NextDouble3(double3 min, double3 max) { return NextDouble3() * (max - min) + min; }

        /// <summary>Returns a uniformly random double4 value with all components in the interval [min, max).</summary>
        /// <param name="min">The componentwise minimum value to generate, inclusive.</param>
        /// <param name="max">The componentwise maximum value to generate, exclusive.</param>
        /// <returns>A uniformly random double4 value in the range [min, max).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double4 NextDouble4(double4 min, double4 max) { return NextDouble4() * (max - min) + min; }

        /// <summary>Returns a unit length float2 vector representing a uniformly random 2D direction.</summary>
        /// <returns>A uniformly random unit length float2 vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 NextFloat2Direction()
        {
            float angle = NextFloat() * PI * 2.0f;
            float s, c;
            sincos(angle, out s, out c);
            return float2(c, s);
        }

        /// <summary>Returns a unit length double2 vector representing a uniformly random 2D direction.</summary>
        /// <returns>A uniformly random unit length double2 vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double2 NextDouble2Direction()
        {
            double angle = NextDouble() * PI_DBL * 2.0;
            double s, c;
            sincos(angle, out s, out c);
            return double2(c, s);
        }

        /// <summary>Returns a unit length float3 vector representing a uniformly random 3D direction.</summary>
        /// <returns>A uniformly random unit length float3 vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 NextFloat3Direction()
        {
            float2 rnd = NextFloat2();
            float z = rnd.x * 2.0f - 1.0f;
            float r = sqrt(max(1.0f - z * z, 0.0f));
            float angle = rnd.y * PI * 2.0f;
            float s, c;
            sincos(angle, out s, out c);
            return float3(c*r, s*r, z);
        }

        /// <summary>Returns a unit length double3 vector representing a uniformly random 3D direction.</summary>
        /// <returns>A uniformly random unit length double3 vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double3 NextDouble3Direction()
        {
            double2 rnd = NextDouble2();
            double z = rnd.x * 2.0 - 1.0;
            double r = sqrt(max(1.0 - z * z, 0.0));
            double angle = rnd.y * PI_DBL * 2.0;
            double s, c;
            sincos(angle, out s, out c);
            return double3(c * r, s * r, z);
        }

        /// <summary>Returns a unit length quaternion representing a uniformly 3D rotation.</summary>
        /// <returns>A uniformly random unit length quaternion.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public quaternion NextQuaternionRotation()
        {
            float3 rnd = NextFloat3(float3(2.0f * PI, 2.0f * PI, 1.0f));
            float u1 = rnd.z;
            float2 theta_rho = rnd.xy;

            float i = sqrt(1.0f - u1);
            float j = sqrt(u1);

            float2 sin_theta_rho;
            float2 cos_theta_rho;
            sincos(theta_rho, out sin_theta_rho, out cos_theta_rho);

            quaternion q = quaternion(i * sin_theta_rho.x, i * cos_theta_rho.x, j * sin_theta_rho.y, j * cos_theta_rho.y);
            return quaternion(select(q.value, -q.value, q.value.w < 0.0f));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint NextState()
        {
            CheckState();
            uint t = state;
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return t;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckInitState()
        {
            if (state == 0)
                throw new System.ArgumentException("Seed must be non-zero");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckIndexForHash(uint index)
        {
            if (index == uint.MaxValue)
                throw new System.ArgumentException("Index must not be uint.MaxValue");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckState()
        {
            if(state == 0)
                throw new System.ArgumentException("Invalid state 0. Random object has not been properly initialized");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckNextIntMax(int max)
        {
            if (max < 0)
                throw new System.ArgumentException("max must be positive");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckNextIntMinMax(int min, int max)
        {
            if (min > max)
                throw new System.ArgumentException("min must be less than or equal to max");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckNextUIntMinMax(uint min, uint max)
        {
            if (min > max)
                throw new System.ArgumentException("min must be less than or equal to max");
        }

    }
}
