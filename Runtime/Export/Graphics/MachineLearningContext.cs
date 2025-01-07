// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;


namespace UnityEngine.Rendering
{
    public partial class MachineLearningContext : IDisposable
    {
#pragma warning disable 414
        internal IntPtr m_Ptr;
#pragma warning restore 414

        public MachineLearningContext()
        {
            m_Ptr = CreateContext();
        }

        public void Dispose()
        {
            DestroyContext(m_Ptr);
        }
    }
}
