// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("UnityEditor.LocalizationRuntimeModule")]
[assembly: InternalsVisibleTo("Unity.Modules.LocalizationRuntime.Tests.Editor")]
// The editor module's tests reach runtime internals too, such as the variant entry kinds.
[assembly: InternalsVisibleTo("Unity.Modules.LocalizationRuntimeEditor.Tests.Editor")]
