// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Scripting;

namespace UnityEditor
{
    public static class AssemblyReloadEvents
    {
        public delegate void AssemblyReloadCallback();
        public static event AssemblyReloadCallback beforeAssemblyReload;
        public static event AssemblyReloadCallback afterAssemblyReload;

        [RequiredByNativeCode]
        static void OnBeforeAssemblyReload()
        {
            if (beforeAssemblyReload != null)
                beforeAssemblyReload();
        }

        [RequiredByNativeCode]
        static void OnAfterAssemblyReload()
        {
            if (afterAssemblyReload != null)
                afterAssemblyReload();
        }
    }
}
