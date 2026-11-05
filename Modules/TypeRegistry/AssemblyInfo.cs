// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;

// The test-only TypeManagerV2 surface (Produce, IsRegistered) and EntityId's internal bit
// accessors, which these assemblies used while the types lived in CoreModule and EntitiesModule.
[assembly: InternalsVisibleTo("Unity.Entities")]
[assembly: InternalsVisibleTo("Unity.Entities.Tests")]
[assembly: InternalsVisibleTo("Unity.Modules.Scripting.Scripting.Tests.Editor")]
