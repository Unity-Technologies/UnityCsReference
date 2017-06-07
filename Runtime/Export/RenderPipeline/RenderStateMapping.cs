// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.Rendering
{
    // Must match RenderStateMapping on C++ side
    [StructLayout(LayoutKind.Sequential)]
    public struct RenderStateMapping
    {
        public RenderStateMapping(string renderType, RenderStateBlock stateBlock)
        {
            m_RenderTypeID = Shader.TagToID(renderType);
            m_StateBlock = stateBlock;
        }

        public RenderStateMapping(RenderStateBlock stateBlock) : this(null, stateBlock) {}

        public string renderType
        {
            get { return Shader.IDToTag(m_RenderTypeID); }
            set { m_RenderTypeID = Shader.TagToID(value); }
        }

        public RenderStateBlock stateBlock
        {
            get { return m_StateBlock; }
            set { m_StateBlock = value; }
        }

        int m_RenderTypeID;
        RenderStateBlock m_StateBlock;
    }
}
