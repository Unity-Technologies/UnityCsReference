using System.ComponentModel;
using System.Reflection;

namespace Unity.Scripting.LifecycleManagement.CodeGen
{
    /// <summary>
    /// This class is not supposed to be used directly, but is supporting the codegen for lifecycle attributes.
    /// It is public to allow the code generator to be able to create subclassed of it in user code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class LifecycleMethodRegistration
    {
        public static void Register(Type lifecycleAttributeType, Assembly assembly, string methodFullName, Action callback)
        {
            LifecycleController.Instance.RegisterLifecycleMethod(
                lifecycleAttributeType,
                assembly,
                methodFullName,
                callback);
        }
    }
}
