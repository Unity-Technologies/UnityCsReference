// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Burst;

namespace Unity.Scripting.Reflection
{
    // Fail is [BurstDiscard] so the throw compiles away and a guarded method stays callable from Burst
    [DebuggerStepThrough]
    static class ScriptingReflectionAssert
    {
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void IsTrue(bool condition)
        {
            if (!condition)
                Fail();
        }

        [BurstDiscard]
        static void Fail()
        {
            throw new InvalidOperationException("Assertion failed: Expected true but was false");
        }
    }
}
