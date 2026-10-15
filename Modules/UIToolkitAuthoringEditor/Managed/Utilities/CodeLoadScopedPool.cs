// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using UnityEngine.Pool;

namespace Unity.UIToolkit.Editor;

/// <summary>An object pool that only takes back objects it created since its last <see cref="Clear"/>.</summary>
sealed class CodeLoadScopedPool<T> where T : class
{
    readonly ObjectPool<T> m_Pool;
    ConditionalWeakTable<T, object> m_CreatedThisLoad = new();

    public CodeLoadScopedPool(Func<T> createFunc, Action<T> actionOnGet = null, Action<T> actionOnRelease = null)
    {
        m_Pool = new ObjectPool<T>(() =>
        {
            var created = createFunc();
            m_CreatedThisLoad.Add(created, null);
            return created;
        }, actionOnGet, actionOnRelease);
    }

    public int CountInactive => m_Pool.CountInactive;

    public T Get() => m_Pool.Get();

    public void Release(T element)
    {
        if (m_CreatedThisLoad.TryGetValue(element, out _))
            m_Pool.Release(element);
    }

    public void Clear()
    {
        m_Pool.Clear();
        m_CreatedThisLoad = new ConditionalWeakTable<T, object>();
    }
}
