// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using System;
namespace UnityEngine
{
    public partial class AsyncOperation : YieldInstruction
    {
        internal IntPtr m_Ptr;

        ~AsyncOperation()
        {
            InternalDestroy(m_Ptr);
        }

        private System.Action<AsyncOperation> m_completeCallback;

        [RequiredByNativeCode]
        internal void InvokeCompletionEvent()
        {
            if (m_completeCallback != null)
            {
                m_completeCallback(this);
                m_completeCallback = null;
            }
        }

        public event System.Action<AsyncOperation> completed
        {
            add
            {
                if (isDone)
                {
                    value(this);
                }
                else
                {
                    m_completeCallback += value;
                }
            }
            remove
            {
                m_completeCallback -= value;
            }
        }
    }
}
