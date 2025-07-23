// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.TextCore.Text
{
    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/TextCoreTextEngine/Native/ATGMeshInfo.h")]
    internal struct ATGMeshInfo
    {
        private IntPtr m_TextElementInfosPtr;
        private int m_TextElementCount;
        public int textAssetId;

        public unsafe Span<NativeTextElementInfo> textElementInfos
        {
            get
            {
                if (m_TextElementInfosPtr == IntPtr.Zero || m_TextElementCount == 0)
                    return default;

                return new Span<NativeTextElementInfo>(m_TextElementInfosPtr.ToPointer(), m_TextElementCount);
            }
        }
    }
}
