// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // Suspends the coroutine execution for the given amount of seconds.
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    public sealed class WaitForSeconds : YieldInstruction
    {
        internal float m_Seconds;

        // Creates a yield instruction to wait for a given number of seconds
        public WaitForSeconds(float seconds) { m_Seconds = seconds; }
    }
}
