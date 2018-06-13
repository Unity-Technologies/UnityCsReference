// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Scripting;
using System.Runtime.InteropServices;
using System;

namespace UnityEngine.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    unsafe public struct ShaderKeywordSet
    {
        private void ComputeSliceAndMask(ShaderKeyword keyword, out uint slice, out uint mask)
        {
            int index = keyword.GetKeywordIndex();
            slice = (uint)(index / k_SizeInBits);
            mask = (uint)(1 << (index % k_SizeInBits));
        }

        public bool IsEnabled(ShaderKeyword keyword)
        {
            if (!keyword.IsValid())
                return false;

            uint slice, mask;
            ComputeSliceAndMask(keyword, out slice, out mask);
            fixed(uint* bits = m_Bits)
            {
                return (bits[slice] & mask) != 0;
            }
        }

        public void Enable(ShaderKeyword keyword)
        {
            if (!keyword.IsValid())
                return;

            uint slice, mask;
            ComputeSliceAndMask(keyword, out slice, out mask);
            fixed(uint* bits = m_Bits)
            {
                bits[slice] |= mask;
            }
        }

        public void Disable(ShaderKeyword keyword)
        {
            if (!keyword.IsValid())
                return;

            uint slice, mask;
            ComputeSliceAndMask(keyword, out slice, out mask);
            fixed(uint* bits = m_Bits)
            {
                bits[slice] &= ~mask;
            }
        }

        public ShaderKeyword[] GetShaderKeywords()
        {
            ShaderKeyword[] shaderKeywords = new ShaderKeyword[ShaderKeyword.k_MaxShaderKeywords];

            int keywordCount = 0;
            for (int keywordIndex = 0; keywordIndex < ShaderKeyword.k_MaxShaderKeywords; ++keywordIndex)
            {
                ShaderKeyword keyword = new ShaderKeyword(keywordIndex);
                if (IsEnabled(keyword))
                {
                    shaderKeywords[keywordCount] = keyword;
                    ++keywordCount;
                }
            }

            Array.Resize<ShaderKeyword>(ref shaderKeywords, keywordCount);
            return shaderKeywords;
        }

        const int k_SizeInBits = sizeof(uint) * 8;
        internal fixed uint m_Bits[8];
    }
}
