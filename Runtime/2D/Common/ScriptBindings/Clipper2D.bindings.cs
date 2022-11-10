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
    [NativeType(Header = "Runtime/2D/Common/ClipperWrapper.h")]
    internal struct Clipper2D
    {
        public enum ClipType { ctIntersection, ctUnion, ctDifference, ctXor };
        public enum PolyType { ptSubject, ptClip };
        public enum PolyFillType { pftEvenOdd, pftNonZero, pftPositive, pftNegative };
        public enum InitOptions { ioDefault = 0, oReverseSolution = 1, ioStrictlySimple = 2, ioPreserveCollinear = 4 };

        [StructLayout(LayoutKind.Sequential)]
        [NativeType(Header = "Runtime/2D/Common/ClipperWrapper.h")]
        public struct PathArguments
        {
            // All members should be valid when their value is 0
            public PolyType polyType;
            public bool closed;

            // Default are set to the 0 enum value for continuity with default constructor
            public PathArguments(PolyType inPolyType = PolyType.ptSubject, bool inClosed = false)
            {
                polyType = inPolyType;
                closed = inClosed;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        [NativeType(Header = "Runtime/2D/Common/ClipperWrapper.h")]
        public struct ExecuteArguments
        {
            // All members should be valid when their value is 0
            public InitOptions initOption;
            public ClipType clipType;
            public PolyFillType subjFillType;
            public PolyFillType clipFillType;
            public bool reverseSolution;
            public bool strictlySimple;
            public bool preserveColinear;


            // Default are set to the 0 enum value for continuity with default constructor
            public ExecuteArguments(InitOptions inInitOption = InitOptions.ioDefault, ClipType inClipType = ClipType.ctIntersection, PolyFillType inSubjFillType = PolyFillType.pftEvenOdd, PolyFillType inClipFillType = PolyFillType.pftEvenOdd, bool inReverseSolution = false, bool inStrictlySimple = false, bool inPreserveColinear = false)
            {
                initOption = inInitOption;
                clipType = inClipType;
                subjFillType = inSubjFillType;
                clipFillType = inClipFillType;
                reverseSolution = inReverseSolution;
                strictlySimple = inStrictlySimple;
                preserveColinear = inPreserveColinear;
            }
        }

        public struct Solution : IDisposable
        {
            public NativeArray<Vector2> points;
            public NativeArray<int> pathSizes;
            public NativeArray<Rect> boundingRect;  // Only the first array element is valid (index 0)

            // This is an optional constructor when using execute. When using this constructor the allocated buffers will be used when calling execute. 
            public Solution(int pointsBufferSize, int pathSizesBufferSize, Allocator allocator)
            {
                points = new NativeArray<Vector2>(pointsBufferSize, allocator, NativeArrayOptions.ClearMemory);
                pathSizes = new NativeArray<int>(pathSizesBufferSize, allocator, NativeArrayOptions.ClearMemory);
                boundingRect = new NativeArray<Rect>(1, allocator);
            }

            public void Dispose()
            {
                if (points.IsCreated)
                    points.Dispose();
                if (pathSizes.IsCreated)
                    pathSizes.Dispose();
                if (boundingRect.IsCreated)
                    boundingRect.Dispose();
            }
        }


        // If solution has uncreated NativeArrays, they will be automatically created to fit the solution. Otherwise the existing arrays will be used.
        public static void Execute(ref Solution solution, NativeArray<Vector2> inPoints, NativeArray<int> inPathSizes, NativeArray<PathArguments> inPathArguments, ExecuteArguments inExecuteArguments, Allocator inSolutionAllocator, int inIntScale = 65536, bool useRounding = false)
        {
            IntPtr clipperPoints;
            IntPtr clipperPathSizes;
            int clipperPointCount;
            int clipperPathCount;

            unsafe
            {
                if (!solution.boundingRect.IsCreated)
                    solution.boundingRect = new NativeArray<Rect>(1, inSolutionAllocator);

                solution.boundingRect[0] = Internal_Execute(out clipperPoints, out clipperPointCount, out clipperPathSizes, out clipperPathCount, new IntPtr(inPoints.m_Buffer), inPoints.Length, new IntPtr(inPathSizes.m_Buffer), new IntPtr(inPathArguments.m_Buffer), inPathSizes.Length, inExecuteArguments, inIntScale, useRounding);
                if(clipperPointCount > 0)
                {
                    if(!solution.pathSizes.IsCreated)
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
                else
                {
                    if (!solution.pathSizes.IsCreated)
                        solution.points = new NativeArray<Vector2>(0, inSolutionAllocator);
                    if (!solution.points.IsCreated)
                        solution.pathSizes = new NativeArray<int>(0, inSolutionAllocator);
                }
            }
        }


        //---------------------------------
        // Extern Functions
        //---------------------------------
        [NativeMethod(Name = "Clipper2D::Execute", IsFreeFunction = true, IsThreadSafe = true)]
        private extern static unsafe Rect Internal_Execute(out IntPtr outClippedPoints, out int outClippedPointsCount, out IntPtr outClippedPathSizes, out int outClippedPathCount, IntPtr inPoints, int inPointCount, IntPtr inPathSizes, IntPtr inPathArguments, int inPathCount, ExecuteArguments inExecuteArguments, float inIntScale, bool useRounding);

        [NativeMethod(Name = "Clipper2D::Execute_Cleanup", IsFreeFunction = true, IsThreadSafe = true)]
        private extern static unsafe void Internal_Execute_Cleanup(IntPtr inPoints, IntPtr inPathSizes);
    }
}
