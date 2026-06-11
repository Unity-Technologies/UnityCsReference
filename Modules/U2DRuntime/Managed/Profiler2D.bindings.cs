// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using UnityEngine.Assertions;
using UnityEngine.Bindings;
using UnityEngine.Profiling;
using UnityEngine.Scripting;

namespace UnityEngine.U2D
{


    [NativeHeader("Modules/U2DRuntime/Private/Analysis/Profiler2D.h")]
    internal static partial class Profiler2D
    {

        extern internal static string GetGUIDString();
        /// <summary>
        /// GUID for the 2D Profile Module definition. Category | Protocol Major | Minor
        /// </summary>
        internal static readonly Guid kProfilerU2D = new Guid(GetGUIDString());

        /// <summary>
        /// Add Custom Data
        /// </summary>
        internal static void EmitFrameMetaData2D<T>(int tag, NativeArray<T> data) where T : struct
        {
            if (tag > 7)
            {
                Profiler.EmitFrameMetaDataInternal<T>(kProfilerU2D, tag, data);
            }
            Debug.Assert(tag > 7, "Invalid tag " + tag + " was passed. Tag 0 - 7 are reserved for internal use. Please use Tag > 7");
        }

    }

}
