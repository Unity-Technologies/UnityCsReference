// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Object = UnityEngine.Object;

namespace UnityEditor
{
    public partial class EditorUtility
    {
        internal static bool IsUnityAssembly(Object target)
        {
            if (target == null)
                return false;
            System.Type type = target.GetType();
            return IsUnityAssembly(type);
        }

        internal static bool IsUnityAssembly(System.Type type)
        {
            if (type == null)
                return false;
            string assembly_name = type.Assembly.GetName().Name;
            if (assembly_name.StartsWith("UnityEditor"))
            {
                return true;
            }
            if (assembly_name.StartsWith("UnityEngine"))
            {
                return true;
            }
            return false;
        }
    }
}
