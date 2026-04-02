// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.AI.Navigation.LowLevel;

public struct NavNode : IEquatable<NavNode>
{
    internal ulong m_PolyRef;

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public static bool operator ==(NavNode left, NavNode right) { return left.m_PolyRef == right.m_PolyRef; }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public static bool operator !=(NavNode left, NavNode right) { return left.m_PolyRef != right.m_PolyRef; }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly bool Equals(NavNode other) { return m_PolyRef == other.m_PolyRef; }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly override bool Equals(object obj)
    {
        return obj is NavNode other && m_PolyRef == other.m_PolyRef;
    }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly override int GetHashCode() { return m_PolyRef.GetHashCode(); }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly bool IsNull() { return m_PolyRef == 0; }
}
