// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor;

internal partial struct ProgressScope
{
    [FreeFunction("ProgressScope::PushScope")]
    internal static extern UIntPtr ProgressBarPushScope(string title, string info, float length = 1.0f, bool cancellable = false, bool skippable = false, bool forceUpdate = false, bool forceDisplay = false);
    [FreeFunction("ProgressScope::SetText")]
    internal static extern void ProgressBarSetText(string info, bool forceUpdate = false, bool forceDisplay = false);
    [FreeFunction("ProgressScope::PopScope")]
    internal static extern void ProgressBarPopScope(UIntPtr scope);
}
