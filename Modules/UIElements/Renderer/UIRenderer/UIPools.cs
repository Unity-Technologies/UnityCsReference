// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityEngine.UIElements.UIR
{
    abstract class PoolBase<T> where T : PoolItem, new()
    {
        int m_Count;
        int k_Capacity = 10000; // Avoids catastrophic memory retention.

        readonly protected Pool<T> m_Pool = new Pool<T>();

        public T Get()
        {
            var poolItem = m_Pool.TryGet();
            if (poolItem != null)
            {
                --m_Count;
                Reset(poolItem);
                return poolItem;
            }

            return CreateInstance();
        }

        public void Return(T poolItem)
        {
            if (m_Count < k_Capacity)
            {
                ++m_Count;
                m_Pool.Return(poolItem);
            }
        }

        protected abstract T CreateInstance();

        protected abstract void Reset(T poolItem);
    }

    sealed class StatePool : PoolBase<State>
    {
        protected override State CreateInstance() { return new State(); }
        protected override void Reset(State poolItem) { poolItem.Reset(); }
    }

    sealed class MeshRendererPool : PoolBase<MeshRenderer>
    {
        protected override MeshRenderer CreateInstance() { return new MeshRenderer(); }
        protected override void Reset(MeshRenderer poolItem) { poolItem.Reset(); }
    }

    sealed class MeshHandlePool : PoolBase<MeshHandle>
    {
        protected override MeshHandle CreateInstance() { return new MeshHandle(); }
        protected override void Reset(MeshHandle poolItem) { poolItem.Reset(); }
    }

    sealed class MeshNodePool : PoolBase<MeshNode>
    {
        protected override MeshNode CreateInstance() { return new MeshNode(); }
        protected override void Reset(MeshNode poolItem) { poolItem.Reset(); }
    }
}
