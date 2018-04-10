// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Scripting;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    public struct PlatformKeywordSet
    {
        private uint ComputeKeywordMask(BuiltinShaderDefine define)
        {
            return (uint)(1 << ((int)define % k_SizeInBits));
        }

        public bool IsEnabled(BuiltinShaderDefine define)
        {
            return (m_Bits & ComputeKeywordMask(define)) != 0;
        }

        public void Enable(BuiltinShaderDefine define)
        {
            m_Bits |= ComputeKeywordMask(define);
        }

        public void Disable(BuiltinShaderDefine define)
        {
            m_Bits &= ~ComputeKeywordMask(define);
        }

        // Note: We currently only have 28 Platform Keywords, once we go over 32,
        // we will need to use a ulong (64 bits, unsigned)
        const int k_SizeInBits = sizeof(uint) * 8;
        internal uint m_Bits;
    }
}
