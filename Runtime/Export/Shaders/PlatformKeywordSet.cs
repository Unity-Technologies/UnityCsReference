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
        private ulong ComputeKeywordMask(BuiltinShaderDefine define)
        {
            return (ulong)(1 << ((int)define % k_SizeInBits));
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

        const int k_SizeInBits = sizeof(ulong) * 8;
        internal ulong m_Bits;
    }
}
