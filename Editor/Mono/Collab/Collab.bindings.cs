// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Collaboration
{
    [NativeHeader("Editor/Src/Collab/Collab.h")]
    partial class Collab
    {
        [StaticAccessor("Collab::Get()", StaticAccessorType.Arrow)]
        public static extern int GetRevisionsData(
            bool withChanges, int startIndex, int numRevisions);

        [StaticAccessor("Collab::Get()", StaticAccessorType.Arrow)]
        public static extern RevisionsData PopulateRevisionsData(IntPtr nativeData);
    }
}
