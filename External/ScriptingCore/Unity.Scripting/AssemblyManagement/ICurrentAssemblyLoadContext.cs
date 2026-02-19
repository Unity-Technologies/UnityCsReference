using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Unity.Scripting.AssemblyManagement
{
    internal interface ICurrentAssemblyLoadContext
    {
        public static ICurrentAssemblyLoadContext? Instance { get; internal set; }

        /// <summary>
        /// Gets the assemblies that have been loaded by Unity into the current execution context.
        /// </summary>
        /// <returns>A collection of assemblies.</returns>
        public IReadOnlyList<Assembly> GetLoadedAssemblies();

        /// <summary>
        /// Use to load an assembly into the current context.
        /// </summary>
        /// <param name="assemblyPath">Path to assembly</param>
        /// <returns>Loaded assembly</returns>
        public Assembly LoadFromPath(string assemblyPath);

        /// <summary>
        /// Use to load an assembly into the current context.
        /// </summary>
        /// <param name="assemblyStream">Assembly data stream</param>
        /// <param name="symbolsStream">Assembly symbols data stream</param>
        /// <returns>Loaded assembly</returns>
        public Assembly LoadFromStream(Stream assemblyStream, Stream? symbolsStream);

        /// <summary>
        /// Checks if the assembly is in the Unity code ALC.
        /// </summary>
        /// <param name="assembly">Assembly to check</param>
        /// <returns>Assembly is loaded in user code ALC</returns>
        internal bool IsAssemblyInUserCodeALC(Assembly assembly);
    }
}
