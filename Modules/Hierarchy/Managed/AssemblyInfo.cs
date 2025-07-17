// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;

// Modules
[assembly: InternalsVisibleTo("UnityEditor.HierarchyModule")]

// Temporary until `IHierarchyEntityIdConverter` API is made fully public.
[assembly: InternalsVisibleTo("UnityEditor.UIToolkitAuthoringModule")]

// Tests
[assembly: InternalsVisibleTo("Unity.Hierarchy.Tests")]
[assembly: InternalsVisibleTo("Unity.Hierarchy.Editor.Tests")]
[assembly: InternalsVisibleTo("Unity.Hierarchy.PerformanceTests")]
[assembly: InternalsVisibleTo("Unity.Hierarchy.Editor.PerformanceTests")]

// External
[assembly: InternalsVisibleTo("Unity.Entities.Editor.Tests")]
[assembly: InternalsVisibleTo("Unity.Modules.UIToolkitAuthoring.Tests.Editor")]
