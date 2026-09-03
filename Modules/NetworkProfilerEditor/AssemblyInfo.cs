// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;

// This module merges into UnityEditor.CoreModule (CreateEditorAssembly = false), so this grants the
// test assembly access to the internals it needs: the module type itself plus ProfilerWindow's
// internal GetProfilerModules / SetProfilerModuleActiveState and ProfilerModule.active.
[assembly: InternalsVisibleTo("Unity.Modules.NetworkProfilerEditor.Tests.Editor")]
