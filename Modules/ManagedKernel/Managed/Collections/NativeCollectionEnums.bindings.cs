// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.Collections
{
    ///<summary>Sets which allocation type to use for a <see cref="NativeArray{T}" />.</summary>
    [UsedByNativeCode]
    public enum Allocator
    {
        // NOTE: The items must be kept in sync with Runtime/Export/Collections/NativeCollectionAllocator.h

        ///<summary>Use an invalid allocation.</summary>
        Invalid = 0,
        // NOTE: this is important to let Invalid = 0 so that new NativeArray<xxx>() will lead to an invalid allocation by default.

        ///<summary>Use no allocation.</summary>
        None = 1,
        ///<summary>Use a temporary allocation.</summary>
        ///<remarks>The fastest allocation. Use it for allocations with a lifespan of one frame or fewer. You can't use Temp to pass allocations to NativeContainer 
        ///instances stored in a job's member field.</remarks>
        Temp = 2,
        ///<summary>Use a temporary job allocation.</summary>
        ///<remarks>A slower allocation than <see cref="Temp" /> but faster than <see cref="Persistent" />. Use it for thread-safe allocations within a lifespan of four frames. 
        ///
        ///**Important**: You must Dispose of this allocation type within four frames, or the console prints a warning, generated from the native code. Most small jobs use this 
        ///allocation type.</remarks>
        TempJob = 3,
        ///<summary>Use a persistent allocation.</summary>
        ///<remarks>The slowest allocation but can last as long as you need it to, and if necessary, throughout the application's lifetime. It's a wrapper for a 
        ///direct call to malloc. Longer jobs can use this NativeContainer allocation type. Don't use Persistent where performance is essential.</remarks>
        Persistent = 4,
        ///<summary>Use an allocation associated with a DSPGraph audio kernel.</summary>
        AudioKernel = 5,
        ///<summary>Use an allocation associated with the lifetime of a domain.</summary>
        ///<remarks>You don't need to free this memory, because Unity frees it automatically at domain unload. However, to conserve memory, 
        ///you can free it at any time. Freeing a large number of such allocations in one frame, such that the array of freed pointers is larger than L2 cache, 
        ///might exhibit a measurable loss of performance.
        ///
        ///**Note**: The Domain allocator can track a maximum of 262144 (256 * 1024) individual allocations. If the number of allocations exceeds that number, Unity logs an error and does not free excessive allocations automatically.</remarks>
        Domain = 6,
        ///<summary>The first index that a custom allocator can get.</summary>
        FirstUserIndex = 64,
    }

    ///<summary>Sets which native leak memory leak detection mode to use.</summary>
    [UsedByNativeCode]
    public enum NativeLeakDetectionMode
    {
        // NOTE: Any changes to this enum must be kept in sync with Runtime\Export\Collections\NativeCollectionLeakDetectionMode.h
        ///<summary>Disable native memory leak detection.</summary>
        Disabled = 1,
        ///<summary>Enable native memory leak detection without stack traces.</summary>
        Enabled = 2,
        ///<summary>Enable native memory leak detection with full stack trace extraction and logging.</summary>
        EnabledWithStackTrace = 3
    }

    [UsedByNativeCode]
    [VisibleToOtherModules("UnityEngine.CoreModule", "UnityEngine.AIModule")]
    internal enum LeakCategory
    {
        // NOTE: Any changes to this enum must be kept in sync with Runtime\Export\Collections\NativeCollectionLeakCategory.h
        // and the strings in Runtime\Allocator\LeakDetection.cpp
        Invalid = 0,
        Malloc = 1,
        TempJob = 2,
        Persistent = 3,
        LightProbesQuery = 4,
        NativeTest = 5,
        MeshDataArray = 6,
        TransformAccessArray = 7,
        NavMeshQuery = 8,
        NavQueryBuffer = 9,
        UIElementsStyleData = 10
    }
}
