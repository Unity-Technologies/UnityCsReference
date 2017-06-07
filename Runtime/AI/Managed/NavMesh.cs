// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;

namespace UnityEngine.AI
{
    public static partial class NavMesh
    {
        public delegate void OnNavMeshPreUpdate();
        public static OnNavMeshPreUpdate onPreUpdate;

        [RequiredByNativeCode]
        private static void Internal_CallOnNavMeshPreUpdate()
        {
            if (onPreUpdate != null)
                onPreUpdate();
        }
    }
}
