// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.AI.Navigation.LowLevel;

public readonly struct NavLocation : IEquatable<NavLocation>
{
    public NavNode node { get; }
    public Vector3 position { get; }

    internal NavLocation(Vector3 position, NavNode node)
    {
        this.position = position;
        this.node = node;
    }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public static bool operator ==(NavLocation left, NavLocation right)
    {
        return left.node.Equals(right.node) && left.position.Equals(right.position);
    }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public static bool operator !=(NavLocation left, NavLocation right)
    {
        return !left.node.Equals(right.node) || !left.position.Equals(right.position);
    }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly bool Equals(NavLocation other)
    {
        return node.Equals(other.node) && position.Equals(other.position);
    }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly override bool Equals(object obj)
    {
        return obj is NavLocation other && node.Equals(other.node) && position.Equals(other.position);
    }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = (hash * 31) + node.GetHashCode();
            hash = (hash * 31) + position.GetHashCode();
            return hash;
        }
    }
}
