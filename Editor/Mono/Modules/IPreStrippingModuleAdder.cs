// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor.Modules;

[VisibleToOtherModules]
internal interface IPreStrippingModuleAdder
{
    /// <summary>
    /// Add a module to the list of modules to include in the build.
    /// </summary>
    /// <remarks>
    /// This happens right after the force includes and excludes are calculated,
    /// and allows a module to add itself to the force includes list.  Due to where this
    /// happens in the build pipeline, this will also cause the dependencies of
    /// that module to be force included.
    /// </remarks>
    /// <param name="moduleName">The name of the module to force add to the build.</param>
    public void AddModule(string moduleName);

    /// <summary>
    /// Check whether a given module is already on the list of modules that should be added.
    /// </summary>
    /// <param name="moduleName">The name of the module to check for.</param>
    /// <returns>True if the module is on the list of modules to add; false otherwise.</returns>
    public bool IsModuleIncluded(string moduleName);
}
