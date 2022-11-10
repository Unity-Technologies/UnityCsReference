// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.InteropServices;

namespace UnityEngine.U2D
{
    [NativeType(Header = "Runtime/2D/Common/ClipperOffsetWrapper.h")]
    internal struct ClipperOffset2D
    {
        public enum JoinType { jtSquare, jtRound, jtMiter };
        public enum EndType { etClosedPolygon, etClosedLine, etOpenButt, etOpenSquare, etOpenRound };


        [StructLayout(LayoutKind.Sequential)]
        [NativeType(Header = "Runtime/2D/Common/ClipperOffsetWrapper.h")]
        public struct PathArguments
        {
            // All members should be valid when their value is 0
            public JoinType joinType;
            public EndType endType;

            // Default are set to the 0 enum value for continuity with default constructor
            public PathArguments(JoinType inJoinType = JoinType.jtSquare, EndType inEndType = EndType.etClosedPolygon)
            {
                joinType = inJoinType;
                endType = inEndType;
            }
        }

        public struct Solution
        {
            public NativeArray<Vector2> points;
            public NativeArray<int> pathSizes;

            public Solution(int pointsBufferSize, int pathSizesBufferSize, Allocator allocator)
            {
                points = new NativeArray<Vector2>(pointsBufferSize, allocator, NativeArrayOptions.ClearMemory);
                pathSizes = new NativeArray<int>(pathSizesBufferSize, allocator, NativeArrayOptions.ClearMemory);
            }

            public void Dispose()
            {
                if (points.IsCreated)
                    points.Dispose();
                if (pathSizes.IsCreated)
                    pathSizes.Dispose();
            }
        }

        public static void Execute(ref Solution solution, NativeArray<Vector2> inPoints, NativeArray<int> inPathSizes, NativeArray<PathArguments> inPathArguments, Allocator inSolutionAllocator, double inDelta = 0, double inMiterLimit = 2.0, double inRoundPrecision = 0.25, double inArcTolerance=0.0, double inIntScale = 65536, bool useRounding = false)
        {
            IntPtr clipperPoints;
            IntPtr clipperPathSizes;
            int clipperPointCount;
            int clipperPathCount;

            unsafe
            {
                Internal_Execute(out clipperPoints, out clipperPointCount, out clipperPathSizes, out clipperPathCount, new IntPtr(inPoints.m_Buffer), inPoints.Length, new IntPtr(inPathSizes.m_Buffer), new IntPtr(inPathArguments.m_Buffer), inPathSizes.Length, inDelta, inMiterLimit, inRoundPrecision, inArcTolerance, inIntScale, useRounding);
                if (!solution.pathSizes.IsCreated)
                    solution.pathSizes = new NativeArray<int>(clipperPathCount, inSolutionAllocator);
                if (!solution.points.IsCreated)
                    solution.points = new NativeArray<Vector2>(clipperPointCount, inSolutionAllocator);

                // Check for enough elements
                if (solution.points.Length >= clipperPointCount && solution.pathSizes.Length >= clipperPathCount)
                {
                    UnsafeUtility.MemCpy(solution.points.m_Buffer, clipperPoints.ToPointer(), clipperPointCount * sizeof(Vector2));
                    UnsafeUtility.MemCpy(solution.pathSizes.m_Buffer, clipperPathSizes.ToPointer(), clipperPathCount * sizeof(int));
                    Internal_Execute_Cleanup(clipperPoints, clipperPathSizes);
                }
                else
                {
                    Internal_Execute_Cleanup(clipperPoints, clipperPathSizes);
                    throw new IndexOutOfRangeException();
                }
            }
        }

        //---------------------------------
        // Extern Functions
        //---------------------------------
        [NativeMethod(Name = "ClipperOffset2D::Execute", IsFreeFunction = true, IsThreadSafe = true)]
        private extern static unsafe void Internal_Execute(out IntPtr outClippedPoints, out int outClippedPointsCount, out IntPtr outClippedPathSizes, out int outClippedPathCount, IntPtr inPoints, int inPointCount, IntPtr inPathSizes, IntPtr inPathArguments, int inPathCount, double inDelta, double inMiterLimit, double inRoundPrecision, double inArcTolerance, double inIntScale, bool useRounding);

        [NativeMethod(Name = "ClipperOffset2D::Execute_Cleanup", IsFreeFunction = true, IsThreadSafe = true)]
        private extern static unsafe void Internal_Execute_Cleanup(IntPtr inPoints, IntPtr inPathSizes);
    }
}
