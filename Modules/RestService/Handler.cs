// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditorInternal;

namespace UnityEditor.RestService
{
    [UnityEngine.Scripting.RequiredByNativeCode]
    internal abstract class Handler
    {
        // The following methods are invoked from native code.
        protected abstract void InvokeGet(Request request, string payload, Response writeResponse);
        protected abstract void InvokePost(Request request, string payload, Response writeResponse);
        protected abstract void InvokeDelete(Request request, string payload, Response writeResponse);
    }
}
