// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;

namespace UnityEditor.Search
{
    internal interface IInstruction<T>
    {
        long estimatedCost { get; }
        IQueryNode node { get; }
    }
    internal interface IOperandInstruction<T> : IInstruction<T>
    {
        IInstruction<T> LeftInstruction { get; }
        IInstruction<T> RightInstruction { get; }
    }
    internal interface IAndInstruction<T> : IOperandInstruction<T>
    {
    }
    internal interface IOrInstruction<T> : IOperandInstruction<T>
    {
    }
    internal interface IResultInstruction<T> : IInstruction<T>
    {
    }
}
