// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Runtime/Mono/MonoAttributeHelpers.h")]
    static partial class EditorAssemblies
    {
        [FreeFunction]
        extern internal static System.Reflection.MethodInfo[] GetClassMethodsWithAttribute(System.Type klassType, System.Type attrType, bool getStaticMethods, bool getInstanceMethods);
        [FreeFunction]
        extern internal static System.Reflection.MethodInfo[] GetAllMethodsWithAttribute(System.Type attrType, bool getStaticMethods, bool getInstanceMethods, bool editorOnly);
        [FreeFunction]
        extern internal static System.Type[] GetAllTypesWithAttribute(System.Type attrType, bool editorOnly);
        [FreeFunction]
        extern internal static System.Type[] GetAllTypesWithInterface(System.Type interfaceType, bool editorOnly);
    }
}
