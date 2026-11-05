// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;

// GetUnifiedRenderData() is internal because UnifiedRenderData itself is; these are the
// test assemblies that exercise it, matching the grants CoreModule already makes.
[assembly: InternalsVisibleTo("Assembly-CSharp-testable")]
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor-testable")]

// SetFillTexture(): the SpriteShape package builds the geometry itself and no longer
// calls Prepare(), so it needs the legacy fill texture setter without that being
// public API. Its editor tests also author UnifiedRenderData and hand it to the
// renderer. Both match the grants CoreModule makes for UnifiedRenderData.
[assembly: InternalsVisibleTo("Unity.2D.SpriteShape.Runtime")]
[assembly: InternalsVisibleTo("Unity.2D.SpriteShape.Tests.EditorTests")]
