// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Scripting;

namespace UnityEngine
{
    // Waits until the end of the frame after all cameras and GUI is rendered, just before displaying the frame on screen.
    [RequiredByNativeCode]
    public sealed class WaitForEndOfFrame : YieldInstruction
    {
    }
}
