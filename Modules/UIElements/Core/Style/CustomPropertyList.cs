// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements;

[NativeHeader("Modules/UIElements/Core/Native/Style/CustomPropertyList.h")]
[NativeClass("CustomPropertyList")]
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct CustomPropertyList : IDisposable
{
    private static readonly CustomPropertyList k_Uncreated = new();

    [NativeName("data")]
    private IntPtr m_Data;

    public extern int Count { get; }

    public StylePropertyValue this[UniqueStyleString name]
    {
        readonly get => TryGetValue(name, out var value) ? value : throw new ArgumentOutOfRangeException(nameof(name));
        set => SetValue(name, value);
    }

    public readonly bool TryGetValue(UniqueStyleString name, out StylePropertyValue value)
    {
        UnmanagedStylePropertyValue outValue;
        if (TryGetValue(name.id, (IntPtr)(&outValue)))
        {
            value = outValue;
            return true;
        }
        value = default;
        return false;
    }

    public readonly bool ContainsKey(UniqueStyleString name)
    {
        return ContainsKey(name.id);
    }

    public readonly void Remove(UniqueStyleString name)
    {
        Remove(name.id);
    }

    public readonly void SetValue(UniqueStyleString name, StylePropertyValue value)
    {
        UnmanagedStylePropertyValue inValue = value;
        SetValue(name.id, (IntPtr)(&inValue));
    }

    [NativeName("Create")]
    private extern void _Create();

    public static CustomPropertyList None() => k_Uncreated;
    public static CustomPropertyList Create()
    {
        CustomPropertyList r = new();
        r._Create();
        return r;
    }
    public extern void Dispose();
    public readonly extern bool IsCreated();

    [NativeName("Acquire")]
    private readonly extern void _Acquire();
    public readonly CustomPropertyList Acquire()
    {
        _Acquire();
        return this;
    }
    public extern void Release();
    public void SafeRelease() => Release();

    public void CopyFrom(CustomPropertyList other)
    {
        // Acquire the new one first in case it's the same one.
        other.Acquire();
        Release();
        this = other;
    }

    public readonly extern bool ReferenceEquals(CustomPropertyList other);

    private readonly extern bool TryGetValue(int id, IntPtr outValue);
    private readonly extern bool ContainsKey(int id);
    private readonly extern void Remove(int id);
    private readonly extern void SetValue(int id, IntPtr inValue);
    private readonly extern void GetEnumerator(IntPtr e);

    public readonly Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    public struct Enumerator : IEnumerator<KeyValuePair<UniqueStyleString, StylePropertyValue>>
    {
        private CustomPropertyListEnumerator m_NativeEnum;
        private int m_CurrentId;
        private UnmanagedStylePropertyValue m_CurrentValue;

        public Enumerator(CustomPropertyList self)
        {
            m_NativeEnum = default;
            m_CurrentId = default;
            m_CurrentValue = default;

            fixed (CustomPropertyListEnumerator* e = &m_NativeEnum)
                self.GetEnumerator((IntPtr)e);
        }

        public bool MoveNext()
        {
            if (!m_NativeEnum.MoveNext())
                return false;

            fixed (UnmanagedStylePropertyValue* v = &m_CurrentValue)
                m_CurrentId = m_NativeEnum.GetCurrent((IntPtr)v);
            return true;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public KeyValuePair<UniqueStyleString, StylePropertyValue> Current => new(new(m_CurrentId), m_CurrentValue);

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }
    }
}

[NativeHeader("Modules/UIElements/Core/Native/Style/CustomPropertyList.h")]
[NativeClass("CustomPropertyListEnumerator")]
[StructLayout(LayoutKind.Sequential)]
internal struct CustomPropertyListEnumerator
{
    private IntPtr m_ItNode;
    private IntPtr m_ItEnd;
    private IntPtr m_EndNode;
    private IntPtr m_EndEnd;
    private IntPtr m_Current;

    public extern bool MoveNext();
    public extern int GetCurrent(IntPtr valuePtr);
}

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal readonly ref struct CustomPropertyListRef
{
    private readonly CustomPropertyList m_Self;

    public int Count => m_Self.Count;

    public CustomPropertyListRef(CustomPropertyList self)
    {
        m_Self = self;
    }

    public bool TryGetValue(UniqueStyleString name, out StylePropertyValue value) => m_Self.TryGetValue(name, out value);
    public bool ContainsKey(UniqueStyleString name) => m_Self.ContainsKey(name);
    public void Remove(UniqueStyleString name) => m_Self.Remove(name);
    public CustomPropertyList.Enumerator GetEnumerator() => m_Self.GetEnumerator();
    public bool ReferenceEquals(CustomPropertyListRef other) => m_Self.ReferenceEquals(other.m_Self);

    public static implicit operator CustomPropertyListRef(CustomPropertyList list) => new(list);
}
