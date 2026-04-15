// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;

namespace UnityEngine.Bindings
{
    [VisibleToOtherModules]
    internal static class ReferenceTypeCreator
    {
        public static T CreateEmptyInstance<T>() where T : class
        {
            return (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        }
    }
}
