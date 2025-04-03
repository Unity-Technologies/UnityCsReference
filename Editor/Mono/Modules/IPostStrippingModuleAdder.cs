// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
namespace UnityEditor.Modules;

[VisibleToOtherModules]
internal interface IPostStrippingModuleAdder
{
    /// <summary>
    /// Add a module to the list of modules to include in the build.
    /// </summary>
    /// <remarks>
    /// After UnityLinker has run, only limited modules are allowed to be added to the build:
    /// * The module must be purely native.
    /// * The module's dependencies must also not be stripped.
    ///
    /// It is still useful to add modules at this point in the build process because it allows us
    /// to conditionally add them depending on whether other modules were included - for example,
    /// only add a pure-native 'backend' module if the corresponding 'frontend' module is included
    /// (due to being used by user code / in user scenes / etc).
    /// </remarks>
    /// <param name="moduleName">The name of the module to not strip from the build.</param>
    public void AddModule(string moduleName);

    /// <summary>
    /// Check whether a given module is already on the list of modules that should not be stripped.
    /// </summary>
    /// <param name="moduleName">The name of the module to check for.</param>
    /// <returns>True if the module is on the list of modules to not strip; false otherwise.</returns>
    public bool IsModuleIncluded(string moduleName);
}
