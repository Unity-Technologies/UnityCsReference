// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;

namespace UnityEditor;

internal partial class CustomEditorAttributes
{
    [RequiredByNativeCode]
    internal static Type FindCustomEditorType(Object obj, bool multiEdit)
    {
        return obj == null ? null : FindCustomEditorTypeByType(obj.GetType(), multiEdit);
    }

    [RequiredByNativeCode]
    internal static Type FindCustomEditorTypeByType(Type type, bool multiEdit)
    {
        return instance.GetCustomEditorType(type, multiEdit);
    }
}
