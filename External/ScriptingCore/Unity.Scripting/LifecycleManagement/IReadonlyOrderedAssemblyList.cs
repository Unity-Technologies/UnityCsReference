using System;
using System.Collections.Generic;
using System.Reflection;

namespace Unity.Scripting.LifecycleManagement;

internal interface IReadonlyOrderedAssemblyList : IReadOnlyList<Assembly>
{
    bool Contains(string assemblyName);
    bool Contains(AssemblyName assemblyName) => Contains(assemblyName.Name!);
}
