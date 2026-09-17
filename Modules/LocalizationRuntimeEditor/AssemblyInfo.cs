// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.Modules.LocalizationRuntimeEditor.Tests.Editor")]

// The bundled com.unity.localization package drives its upgrade-to-module path through these internals.
[assembly: InternalsVisibleTo("Unity.Localization.Editor")]
[assembly: InternalsVisibleTo("Unity.Localization.Editor.Tests")]
