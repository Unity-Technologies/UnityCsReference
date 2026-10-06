// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;

// EntityIdStore's internal layout/allocation surface is consumed by the DOTS
// EntityComponentStore (see Packages/com.unity.entities), mirroring the internal
// access it had while EntityIdStore lived in the Core module.
[assembly: InternalsVisibleTo("Unity.Entities")]
[assembly: InternalsVisibleTo("Unity.Entities.Tests")]
