// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Concurrent;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.SmartStrings.PersistentVariables;

[VisibleToOtherModules("UnityEditor.SmartStringsModule")]
internal static class VariableEditorHooks
{
    [NoAutoStaticsCleanup]
    static readonly ConcurrentQueue<Action> s_Pending = new ConcurrentQueue<Action>();

    internal static void Enqueue(Action action) => s_Pending.Enqueue(action);

    [VisibleToOtherModules("UnityEditor.SmartStringsModule")]
    internal static void SendPending()
    {
        while (s_Pending.TryDequeue(out var action))
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
