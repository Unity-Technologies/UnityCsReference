// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Scripting.LifecycleManagement
{
    internal sealed class ScopedLazy<TValue, TScope>
        where TValue : class
    {
        Lazy<TValue> _data;

        public ScopedLazy(Func<TValue> factory, bool checkScopeActive = true)
        {
            _data = new Lazy<TValue>(factory);
        }

        public ScopedLazy(bool checkScopeActive = true) : this(Activator.CreateInstance<TValue>, checkScopeActive) { }

        public void Cleanup()
        {
            _data = null;
        }

        public TValue Value => _data.Value;
    }
}
