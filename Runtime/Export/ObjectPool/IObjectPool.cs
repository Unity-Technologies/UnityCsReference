// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Pool
{
    public interface IObjectPool<T> where T : class
    {
        int CountInactive { get; }

        T Get();
        PooledObject<T> Get(out T v);
        void Release(T element);
        void Clear();
    }
}
