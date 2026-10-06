// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Scripting.CodeReload
{
    // Detects AssemblyLoadContext leaks during Code Reloads: which ALCs failed to unload, which
    // objects keep them alive, and the path from a GC root to each leaked object.
    [NativeHeader("Runtime/ScriptingBackend/CoreClr/Profiler/LeakDetector.bindings.h")]
    internal sealed partial class AssemblyLoadContextLeakDetector
    {
        // Suppress warning about uninitialized readonly fields as we are initializing those fields
        // through native code in the bindings layer.
        #pragma warning disable CS0649
        // Type information of a leaked object.
        [NativeAsStruct]
        [StructLayout(LayoutKind.Sequential)]
        internal sealed class MemberType
        {
            internal readonly string assemblyName;
            internal readonly string typeName;

            internal MemberType() {}

            internal MemberType(string assemblyName, string typeName)
            {
                this.assemblyName = assemblyName;
                this.typeName = typeName;
            }

            internal string AssemblyName => assemblyName;
            internal string TypeName => typeName;
        }

        // Information about a root that keeps a leaked object alive.
        [NativeAsStruct]
        [StructLayout(LayoutKind.Sequential)]
        internal sealed class Root
        {
            internal readonly RootKind kind;
            internal readonly RootFlags flags;
            internal readonly IntPtr handle;
            internal readonly string details;

            internal Root() {}

            internal Root(RootKind kind, RootFlags flags, IntPtr handle, string details)
            {
                this.kind = kind;
                this.flags = flags;
                this.handle = handle;
                this.details = details;
            }

            internal RootKind Kind => kind;
            internal RootFlags Flags => flags;
            internal IntPtr Handle => handle;
            // If the root is a handle, this is the stack trace of the handle allocation (each
            // entry separated by '\n'); only populated when
            // "-record-managed-allocation-callstacks" is enabled. If the root is a static field,
            // this is the field name.
            internal string Details => details;
        }

        // Information about an object on the path from a leaked object to its root.
        internal readonly struct ObjectPath
        {
            internal readonly int typeIndex;
            internal readonly IntPtr address;

            // Index of this object's type in FullLeakDetectionResult.MemberTypes.
            internal int TypeIndex => typeIndex;
            // Address of the object at the time of the leak inspection.
            internal IntPtr Address => address;
        }

        // Information about a leaked object and its path to root.
        internal readonly struct Leak
        {
            internal readonly int rootIndex;
            internal readonly ObjectPath[] pathItems;

            // Index of this leak's root in FullLeakDetectionResult.Roots.
            internal int RootIndex => rootIndex;
            // Path from the leaked object itself to the root (last item is the root, if the root
            // is a handle).
            internal ObjectPath[] PathItems => pathItems;
        }

        // Root kind. Matches native COR_PRF_GC_ROOT_KIND enum.
        [UsedByNativeCode]
        internal enum RootKind
        {
            Stack = 1,
            Finalizer = 2,
            Handle = 3,
            Other = 0,
            StaticField = 4 // Our own kind to represent static fields
        }

        // Root flags. Matches native COR_PRF_GC_ROOT_FLAGS enum.
        [Flags]
        [UsedByNativeCode]
        internal enum RootFlags
        {
            Pinning = 0x1,
            WeakRef = 0x2,
            Interior = 0x4,
            RefCounted = 0x8
        }

        // Information about an AssemblyLoadContext.
        [NativeType]
        internal struct AssemblyLoadContextInfo
        {
            internal readonly string name;
            internal readonly IntPtr address;
            internal readonly IntPtr handle;

            internal string Name => name;
            // Address of the AssemblyLoadContext managed instance at the time of the leak inspection.
            internal IntPtr Address => address;
            // Handle identifying the AssemblyLoadContext, as stored by LoaderAllocator on the native side.
            internal IntPtr Handle => handle;
        }

        // Leak detection result: detailed information about AssemblyLoadContexts in unloading
        // state and the leaked objects that keep them alive.
        internal readonly struct FullLeakDetectionResult
        {
            internal readonly Leak[] leaks;
            internal readonly MemberType[] memberTypes;
            internal readonly Root[] roots;
            internal readonly AssemblyLoadContextInfo[] assemblyLoadContexts;

            // Leaked objects that belong to the leaked AssemblyLoadContext in unloading state.
            internal Leak[] Leaks => leaks;
            // Types of all the leaked objects and objects in the path to root.
            internal MemberType[] MemberTypes => memberTypes;
            // All roots that keep the leaked objects alive.
            internal Root[] Roots => roots;
            // AssemblyLoadContexts that are in unloading state and not yet unloaded.
            internal AssemblyLoadContextInfo[] AssemblyLoadContexts => assemblyLoadContexts;
        }

        // Basic leak detection result: counts only, no detail.
        [NativeType]
        internal readonly struct BasicLeakDetectionResult
        {
            internal readonly int leakedObjectCount;
            internal readonly int leakedALCCount;

            // Count of leaked objects that belong to AssemblyLoadContexts in the unloading state.
            internal int ObjectCount => leakedObjectCount;
            // Count of AssemblyLoadContexts in unloading state.
            internal int AssemblyLoadContextCount => leakedALCCount;
        }
        #pragma warning restore CS0649

        // Performs an AssemblyLoadContext leak detection and returns detailed result information.
        //
        // Identifies AssemblyLoadContexts that are in unloading state and the objects whose types
        // belong to these AssemblyLoadContexts. If outputReportFilePath is provided, the result is
        // saved in JSON format and the leaks summary is printed to the Editor log.
        //
        // The call is blocking: it disables concurrent GC, enables COR_PRF_MONITOR_GC profiler
        // flags, and forces a full GC to collect live objects and root information. Execute on a
        // thread or as a Task to minimize impact to the main thread; marshalling the result is
        // expensive, so use this only for diagnostic purposes.
        [NativeMethod(Name = "AssemblyLoadContextLeakDetector::RunLeakDetectionWithFullReport", IsFreeFunction = true, IsThreadSafe = true)]
        [NativeConditional("ENABLE_CORECLR && ENABLE_PROFILER")]
        internal static extern FullLeakDetectionResult RunLeakDetectionWithFullReport(string outputReportFilePath = null);

        // Performs an AssemblyLoadContext leak detection.
        //
        // Identifies the number of AssemblyLoadContexts in unloading state and the number of
        // objects whose types belong to these AssemblyLoadContexts. If outputReportFilePath is
        // provided, the result is saved in JSON format and the leaks summary is printed to the
        // Editor log.
        //
        // The call is blocking: it disables concurrent GC, enables COR_PRF_MONITOR_GC profiler
        // flags, and forces a full GC to collect live objects and root information. Execute on a
        // thread or as a Task to minimize impact to the main thread.
        [NativeMethod(Name = "AssemblyLoadContextLeakDetector::RunLeakDetection", IsFreeFunction = true, IsThreadSafe = true)]
        [NativeConditional("ENABLE_CORECLR && ENABLE_PROFILER")]
        internal static extern BasicLeakDetectionResult RunLeakDetection(string outputReportFilePath = null);
    }
}

