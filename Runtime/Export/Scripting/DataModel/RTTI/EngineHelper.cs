// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.DataModel;

internal sealed class EngineHelper
{
    internal static void AssertAreEqual<T>(T expected, T actual)
    {
        Assert.AreEqual(expected, actual);
    }

    internal static void AssertIsTrue(bool condition)
    {
        Assert.IsTrue(condition);
    }


    internal static void Log(string message)
    {
        Debug.Log(message);
    }

    internal static void LogWarning(string message)
    {
        Debug.LogWarning(message);
    }

    // TODO
    internal static int SizeOf(Type type)
    {
        return UnsafeUtility.SizeOf(type);
    }

    // TODO
    internal static int GetFieldOffset(FieldInfo field)
    {
        return UnsafeUtility.GetFieldOffset(field);
    }

    internal static T[] ExtractArrayFromList<T>(List<T> list)
    {
        return NoAllocHelpers.ExtractArrayFromList(list);
    }

    internal static Span<T> CreateSpan<T>(List<T> list)
    {
        return NoAllocHelpers.CreateSpan(list);
    }

    internal static void ResetListSize<T>(List<T> list, int size)
    {
        NoAllocHelpers.ResetListSize(list, size);
    }

    internal static void ResizeListContents(object list, Type elementType, int newSize)
    {
        NoAllocHelpers.ResizeListContents(list, elementType, newSize);
    }


}
