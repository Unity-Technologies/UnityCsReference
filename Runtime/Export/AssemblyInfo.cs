// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// NOTE: the build system includes this source file in ALL modules (Runtime and Editor)

using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: UnityEngine.UnityEngineModuleAssembly]

// required for the type forwarders
[assembly: InternalsVisibleTo("UnityEngine")]
[assembly: InternalsVisibleTo("UnityEditor")]

// TODO: over time, remove the InternalsVisibleTo attributes from this section
// To remove a line in there, the target assembly must not depend on _any_ internal API from built-in modules
