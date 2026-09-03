// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;

// The module tests exercise the public API, with one exception: hostile-input tests need to wrap deliberately corrupted bytes in types that are opaque by design (e.g. a world snapshot image).
[assembly: InternalsVisibleTo("Unity.Modules.PhysicsCore2D.Tests.Runtime")]
