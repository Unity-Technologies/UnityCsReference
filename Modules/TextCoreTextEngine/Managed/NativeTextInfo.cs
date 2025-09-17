// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.TextCore.Text
{

    [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/TextCoreTextEngine/Native/TextInfo.h")]
    internal struct NativeTextInfo
    {
        private IntPtr m_MeshInfosPtr;
        public int meshInfoCount;
        public int totalWidth;
        public int totalHeight;
        public bool isElided;

        public unsafe Span<ATGMeshInfo> meshInfos
        {
            get
            {
                if (m_MeshInfosPtr == IntPtr.Zero || meshInfoCount <= 0)
                    return default;
                return new Span<ATGMeshInfo>(m_MeshInfosPtr.ToPointer(), meshInfoCount);
            }
        }
    }
}
