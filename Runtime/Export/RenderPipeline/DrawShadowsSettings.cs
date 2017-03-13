// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;

namespace UnityEngine.Experimental.Rendering
{
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct DrawShadowsSettings
    {
#pragma warning disable 414
        private IntPtr _cullResults;
#pragma warning restore 414
        public int lightIndex;
        public ShadowSplitData splitData;

        public CullResults cullResults
        {
            set { _cullResults = value.cullResults; }
        }

        public DrawShadowsSettings(CullResults cullResults, int lightIndex)
        {
            _cullResults = cullResults.cullResults;
            this.lightIndex = lightIndex;
            this.splitData.cullingPlaneCount = 0;
            this.splitData.cullingSphere = Vector4.zero;
        }
    }
}
