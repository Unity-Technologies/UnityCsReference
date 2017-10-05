// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine;

namespace UnityEngine.XR.Tango
{
    // This is mirrored in native code. See TangoTypes.h
    internal enum SegmentChange
    {
        Added = 0,
        Updated = 1
    }

    // Must match to Tango::MeshReconstruction::UpdateMethod in TangoTypes.h
    internal enum UpdateMethod
    {
        Traversal = 0,
        Projective
    }

    // Must match Tango::MeshReconstruction::Config in TangoTypes.h
    [NativeHeader("ARScriptingClasses.h")]
    [UsedByNativeCode]
    internal struct MeshReconstructionConfig
    {
        public double resolution;
        public double minDepth;
        public double maxDepth;
        public int minNumVertices;
        public bool useParallelIntegration;
        public bool generateColor;
        public bool useSpaceClearing;
        public UpdateMethod updateMethod;

        public static MeshReconstructionConfig GetDefault()
        {
            return new MeshReconstructionConfig
            {
                resolution = 0.03,
                minDepth = 0.6,
                maxDepth = 3.5,
                useParallelIntegration = false,
                generateColor = true,
                useSpaceClearing = false,
                minNumVertices = 1,
                updateMethod = UpdateMethod.Traversal,
            };
        }
    }

    //
    // A container for the grid index
    // Must match Tango::MeshReconstruction::GridIndex in TangoTypes.h
    //
    [UsedByNativeCode]
    internal struct GridIndex
    {
        public int i;
        public int j;
        public int k;
    }

    // A container for generation requests
    internal struct SegmentGenerationRequest
    {
        public GridIndex gridIndex;
        public MeshFilter destinationMeshFilter;
        public MeshCollider destinationMeshCollider;

        public bool provideNormals;
        public bool provideColors;
        public bool providePhysics;
    };

    // The resulting data from a generation request
    internal struct SegmentGenerationResult
    {
        public GridIndex gridIndex;
        public MeshFilter meshFilter;
        public MeshCollider meshCollider;

        public bool success;
        public double elapsedTimeSeconds;
    }

    internal partial class MeshReconstructionServer
    {
        internal delegate void SegmentChangedDelegate(GridIndex gridIndex, SegmentChange changeType, double updateTime);
        internal delegate void SegmentReadyDelegate(SegmentGenerationResult generatedSegmentData);

        // This must match Tango::MeshReconstruction::CreationStatus in TangoTypes.h
        internal enum Status
        {
            UnsupportedPlatform,
            Ok,
            MissingMeshReconstructionLibrary,
            FailedToCreateMeshReconstructionContext,
            FailedToSetDepthCalibration
        }

        private Status m_Status = Status.UnsupportedPlatform;
        internal Status status
        { get { return m_Status; } }


        internal MeshReconstructionServer(MeshReconstructionConfig config)
        {
            int status = 0;
            m_ServerPtr = Internal_Create(this, config, out status);
            m_Status = (Status)status;
        }

        ~MeshReconstructionServer()
        {
            if (m_ServerPtr != IntPtr.Zero)
            {
                DestroyThreaded(m_ServerPtr);
                m_ServerPtr = IntPtr.Zero;
                GC.SuppressFinalize(this);
            }
        }

        internal void ClearMeshes()
        {
            Internal_ClearMeshes(m_ServerPtr);
        }

        internal IntPtr GetNativeReconstructionContext()
        {
            return Internal_GetNativeReconstructionContextPtr(m_ServerPtr);
        }

        internal void GetChangedSegments(SegmentChangedDelegate onSegmentChanged)
        {
            if (onSegmentChanged == null)
            {
                throw new ArgumentNullException("onSegmentChanged");
            }

            Internal_GetChangedSegments(m_ServerPtr, onSegmentChanged);
        }

        internal void GenerateSegmentAsync(SegmentGenerationRequest request, SegmentReadyDelegate onSegmentReady)
        {
            if (onSegmentReady == null)
            {
                throw new ArgumentNullException("onSegmentRead");
            }

            Internal_GenerateSegmentAsync(
                m_ServerPtr,
                request.gridIndex,
                request.destinationMeshFilter,
                request.destinationMeshCollider,
                onSegmentReady,
                request.provideNormals,
                request.provideColors,
                request.providePhysics);
        }

        internal int generationRequests
        {
            get
            {
                return Internal_GetNumGenerationRequests(m_ServerPtr);
            }
        }

        internal bool enabled
        {
            get
            {
                return Internal_GetEnabled(m_ServerPtr);
            }
            set
            {
                Internal_SetEnabled(m_ServerPtr, value);
            }
        }
    }
}
